using aris.BuildingBlocks.Exceptions;
using aris.BuildingBlocks.Logging;
using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.Application.Users;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthAuditEventRepository _authAuditEventRepository;
    private readonly IPhiSafeLogger<UserManagementService> _logger;

    public UserManagementService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IAuthAuditEventRepository authAuditEventRepository,
        IPhiSafeLogger<UserManagementService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _authAuditEventRepository = authAuditEventRepository;
        _logger = logger;
    }

    public async Task<CreateUserResponseDto> CreateUserAsync(
        CreateUserRequestDto request,
        Guid actorUserId,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        ValidateRequestShape(request);

        var roles = await _roleRepository.GetByNamesAsync(request.Roles, cancellationToken);
        EnsureAllRolesResolved(request.Roles, roles);

        var alreadyExists = await _userRepository.ExistsByUsernameOrEmailAsync(request.Username, request.Email, cancellationToken);
        if (alreadyExists)
        {
            throw new ConflictAppException("Username or email already in use.");
        }

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            DisplayName = request.DisplayName,
            IsActive = true,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = actorUserId.ToString(),
            UserRoles = roles.Select(role => new UserRole { UserId = userId, RoleId = role.Id }).ToList(),
        };

        await _userRepository.AddAsync(user, cancellationToken);

        await _authAuditEventRepository.AddAsync(
            new AuthAuditEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventType = AuthAuditEventType.UserCreated,
                ActorUserId = actorUserId,
                TimestampUtc = DateTime.UtcNow,
                IpAddress = ipAddress,
                CorrelationId = correlationId,
            },
            cancellationToken);

        _logger.LogInformation("User {UserId} created by administrator {ActorUserId}.", userId, actorUserId);

        return new CreateUserResponseDto(
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            roles.Select(role => role.Name).ToArray(),
            user.IsActive);
    }

    public async Task<ListUsersResponseDto> ListUsersAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var (users, totalCount) = await _userRepository.SearchAsync(query, normalizedPage, normalizedPageSize, cancellationToken);

        var items = users
            .Select(user => new UserSummaryDto(
                user.Id,
                user.Username,
                user.Email,
                user.DisplayName,
                user.UserRoles.Select(userRole => userRole.Role!.Name).ToArray(),
                user.IsActive))
            .ToList();

        return new ListUsersResponseDto(items, normalizedPage, normalizedPageSize, totalCount);
    }

    public async Task<UserSummaryDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new NotFoundAppException("User not found.");
        }

        return ToSummaryDto(user);
    }

    public async Task<UserSummaryDto> ChangeUserRolesAsync(
        Guid id,
        ChangeUserRolesRequestDto request,
        Guid actorUserId,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (request.Roles is null || request.Roles.Length == 0)
        {
            throw new ValidationAppException("At least one role must be specified.");
        }

        var roles = await _roleRepository.GetByNamesAsync(request.Roles, cancellationToken);
        EnsureAllRolesResolved(request.Roles, roles);

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new NotFoundAppException("User not found.");
        }

        user.UserRoles = roles.Select(role => new UserRole { UserId = user.Id, RoleId = role.Id }).ToList();
        user.ModifiedAtUtc = DateTime.UtcNow;
        user.ModifiedBy = actorUserId.ToString();

        await _userRepository.UpdateAsync(user, cancellationToken);

        await _authAuditEventRepository.AddAsync(
            new AuthAuditEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EventType = AuthAuditEventType.UserRolesChanged,
                ActorUserId = actorUserId,
                TimestampUtc = DateTime.UtcNow,
                IpAddress = ipAddress,
                CorrelationId = correlationId,
            },
            cancellationToken);

        _logger.LogInformation("User {UserId} roles changed by administrator {ActorUserId}.", user.Id, actorUserId);

        return ToSummaryDto(user);
    }

    private static UserSummaryDto ToSummaryDto(User user)
    {
        return new UserSummaryDto(
            user.Id,
            user.Username,
            user.Email,
            user.DisplayName,
            user.UserRoles.Select(userRole => userRole.Role!.Name).ToArray(),
            user.IsActive);
    }

    private static void ValidateRequestShape(CreateUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ValidationAppException("Username, email, password, and display name are all required.");
        }

        if (request.Roles is null || request.Roles.Length == 0)
        {
            throw new ValidationAppException("At least one role must be specified.");
        }
    }

    private static void EnsureAllRolesResolved(string[] requestedRoles, IReadOnlyCollection<Role> resolvedRoles)
    {
        var resolvedNames = resolvedRoles.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknownRoles = requestedRoles.Where(name => !resolvedNames.Contains(name)).ToArray();

        if (unknownRoles.Length > 0)
        {
            throw new ValidationAppException($"Unknown role(s): {string.Join(", ", unknownRoles)}.");
        }
    }
}
