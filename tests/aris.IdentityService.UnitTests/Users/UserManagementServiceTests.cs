using aris.BuildingBlocks.Exceptions;
using aris.BuildingBlocks.Logging;
using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Application.Users;
using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.UnitTests.Users;

public class UserManagementServiceTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    private static readonly Role AdministratorRole = new() { Id = 1, Name = "Administrator" };
    private static readonly Role CoderRole = new() { Id = 3, Name = "Coder" };
    private static readonly Role RiskAnalystRole = new() { Id = 4, Name = "RiskAnalyst" };

    private static UserManagementService CreateSut(
        out FakeUserRepository userRepository,
        out FakeAuthAuditEventRepository authAuditEventRepository,
        IReadOnlyCollection<Role>? seededRoles = null)
    {
        return CreateSut(out userRepository, out authAuditEventRepository, out _, seededRoles);
    }

    private static UserManagementService CreateSut(
        out FakeUserRepository userRepository,
        out FakeAuthAuditEventRepository authAuditEventRepository,
        out FakeRefreshTokenRepository refreshTokenRepository,
        IReadOnlyCollection<Role>? seededRoles = null)
    {
        var roles = seededRoles ?? new[] { AdministratorRole, CoderRole };
        userRepository = new FakeUserRepository(roles);
        authAuditEventRepository = new FakeAuthAuditEventRepository();
        refreshTokenRepository = new FakeRefreshTokenRepository();

        return new UserManagementService(
            userRepository,
            new FakeRoleRepository(roles),
            new FakePasswordHasher(),
            authAuditEventRepository,
            refreshTokenRepository,
            new NullPhiSafeLogger());
    }

    [Fact] // FR-6.1: valid, unique details create an active, immediately-usable account with the requested role(s), and record a UserCreated audit event.
    public async Task CreateUserAsync_WithValidUniqueDetails_CreatesActiveUserWithRolesAndAuditEvent()
    {
        var sut = CreateSut(out var userRepository, out var authAuditEventRepository);
        var request = new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" });

        var response = await sut.CreateUserAsync(request, ActorId, "127.0.0.1", "correlation-1", CancellationToken.None);

        Assert.Equal("jdoe", response.Username);
        Assert.Equal("jdoe@aris.local", response.Email);
        Assert.Equal("Jane Doe", response.DisplayName);
        Assert.True(response.IsActive);
        Assert.Equal(new[] { "Coder" }, response.Roles);

        var savedUser = Assert.Single(userRepository.Added);
        Assert.True(savedUser.IsActive);
        Assert.False(savedUser.MustChangePassword);
        Assert.Equal("hashed:P@ssword1", savedUser.PasswordHash);
        Assert.Equal(CoderRole.Id, Assert.Single(savedUser.UserRoles).RoleId);

        var auditEvent = Assert.Single(authAuditEventRepository.Added);
        Assert.Equal(AuthAuditEventType.UserCreated, auditEvent.EventType);
        Assert.Equal(savedUser.Id, auditEvent.UserId);
        Assert.Equal(ActorId, auditEvent.ActorUserId);
        Assert.Equal("127.0.0.1", auditEvent.IpAddress);
        Assert.Equal("correlation-1", auditEvent.CorrelationId);
    }

    [Fact] // FR-6.4: a duplicate username or email is rejected with a specific reason, not a generic failure.
    public async Task CreateUserAsync_WithDuplicateUsernameOrEmail_ThrowsConflict()
    {
        var sut = CreateSut(out var userRepository, out _);
        userRepository.ExistingUsernamesOrEmails.Add("jdoe");
        var request = new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" });

        var exception = await Assert.ThrowsAsync<ConflictAppException>(
            () => sut.CreateUserAsync(request, ActorId, null, null, CancellationToken.None));

        Assert.Equal("Username or email already in use.", exception.Message);
        Assert.Empty(userRepository.Added);
    }

    [Fact] // FR-6.1: an unrecognized role name is rejected rather than silently ignored.
    public async Task CreateUserAsync_WithUnknownRole_ThrowsValidation()
    {
        var sut = CreateSut(out var userRepository, out _);
        var request = new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "NotARole" });

        await Assert.ThrowsAsync<ValidationAppException>(
            () => sut.CreateUserAsync(request, ActorId, null, null, CancellationToken.None));

        Assert.Empty(userRepository.Added);
    }

    [Fact] // FR-6.1: "one or more roles" is a requirement, not an option — an empty role list is rejected.
    public async Task CreateUserAsync_WithNoRoles_ThrowsValidation()
    {
        var sut = CreateSut(out var userRepository, out _);
        var request = new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", Array.Empty<string>());

        await Assert.ThrowsAsync<ValidationAppException>(
            () => sut.CreateUserAsync(request, ActorId, null, null, CancellationToken.None));

        Assert.Empty(userRepository.Added);
    }

    [Theory]
    [InlineData("", "jdoe@aris.local", "P@ssword1", "Jane Doe")]
    [InlineData("jdoe", "", "P@ssword1", "Jane Doe")]
    [InlineData("jdoe", "jdoe@aris.local", "", "Jane Doe")]
    [InlineData("jdoe", "jdoe@aris.local", "P@ssword1", "")]
    public async Task CreateUserAsync_WithBlankRequiredField_ThrowsValidation(string username, string email, string password, string displayName)
    {
        var sut = CreateSut(out var userRepository, out _);
        var request = new CreateUserRequestDto(username, email, password, displayName, new[] { "Coder" });

        await Assert.ThrowsAsync<ValidationAppException>(
            () => sut.CreateUserAsync(request, ActorId, null, null, CancellationToken.None));

        Assert.Empty(userRepository.Added);
    }

    [Fact] // FR-6.7: browsing the user list returns each account's username/email, display name, roles, and active status.
    public async Task ListUsersAsync_WithNoQuery_ReturnsAllUsersWithRoles()
    {
        var sut = CreateSut(out _, out _);
        await SeedUserAsync(sut, "adiaz", "adiaz@aris.local", "Ana Diaz", "Coder");
        await SeedUserAsync(sut, "bsmith", "bsmith@aris.local", "Bob Smith", "Administrator");

        var response = await sut.ListUsersAsync(null, page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
        Assert.Contains(response.Items, item => item.Username == "adiaz" && item.Roles.Single() == "Coder" && item.IsActive);
    }

    [Fact] // FR-6.7: given any number of existing accounts, all are browsable via pagination.
    public async Task ListUsersAsync_WithPageSizeSmallerThanTotal_ReturnsRequestedPage()
    {
        var sut = CreateSut(out _, out _);
        await SeedUserAsync(sut, "auser", "auser@aris.local", "A User", "Coder");
        await SeedUserAsync(sut, "buser", "buser@aris.local", "B User", "Coder");
        await SeedUserAsync(sut, "cuser", "cuser@aris.local", "C User", "Coder");

        var response = await sut.ListUsersAsync(null, page: 2, pageSize: 2, CancellationToken.None);

        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.Page);
        Assert.Single(response.Items);
        Assert.Equal("cuser", response.Items[0].Username);
    }

    [Fact] // FR-6.7: the list is filterable by username/email/display name.
    public async Task ListUsersAsync_WithQuery_ReturnsOnlyMatchingUsers()
    {
        var sut = CreateSut(out _, out _);
        await SeedUserAsync(sut, "adiaz", "adiaz@aris.local", "Ana Diaz", "Coder");
        await SeedUserAsync(sut, "bsmith", "bsmith@aris.local", "Bob Smith", "Coder");

        var response = await sut.ListUsersAsync("diaz", page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(1, response.TotalCount);
        Assert.Equal("adiaz", response.Items[0].Username);
    }

    [Fact] // FR-6.7: a non-matching query is a correct empty result, not an error.
    public async Task ListUsersAsync_WithNonMatchingQuery_ReturnsEmptyResult()
    {
        var sut = CreateSut(out _, out _);
        await SeedUserAsync(sut, "adiaz", "adiaz@aris.local", "Ana Diaz", "Coder");

        var response = await sut.ListUsersAsync("nobody-matches-this", page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(0, response.TotalCount);
        Assert.Empty(response.Items);
    }

    [Fact] // FR-6.3: an existing user's account/role details are retrievable by id.
    public async Task GetUserByIdAsync_WithExistingUser_ReturnsSummary()
    {
        var sut = CreateSut(out _, out _);
        var created = await sut.CreateUserAsync(
            new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" }),
            ActorId, null, null, CancellationToken.None);

        var response = await sut.GetUserByIdAsync(created.Id, CancellationToken.None);

        Assert.Equal("jdoe", response.Username);
        Assert.Equal("jdoe@aris.local", response.Email);
        Assert.Equal(new[] { "Coder" }, response.Roles);
        Assert.True(response.IsActive);
    }

    [Fact] // FR-6.3: an unknown id is a 404, not a silent empty result.
    public async Task GetUserByIdAsync_WithUnknownId_ThrowsNotFound()
    {
        var sut = CreateSut(out _, out _);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => sut.GetUserByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact] // FR-6.2: an Administrator can replace an existing user's role(s), and the change is audited.
    public async Task ChangeUserRolesAsync_WithValidRoles_ReplacesRolesAndRecordsAuditEvent()
    {
        var sut = CreateSut(out var userRepository, out var authAuditEventRepository, new[] { AdministratorRole, CoderRole, RiskAnalystRole });
        var created = await sut.CreateUserAsync(
            new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" }),
            ActorId, null, null, CancellationToken.None);

        var response = await sut.ChangeUserRolesAsync(
            created.Id,
            new ChangeUserRolesRequestDto(new[] { "Coder", "RiskAnalyst" }),
            ActorId,
            "127.0.0.1",
            "correlation-2",
            CancellationToken.None);

        Assert.Equal(new[] { "Coder", "RiskAnalyst" }, response.Roles.OrderBy(role => role));

        var savedUser = Assert.Single(userRepository.Added);
        Assert.Equal(new[] { CoderRole.Id, RiskAnalystRole.Id }, savedUser.UserRoles.Select(userRole => userRole.RoleId).OrderBy(id => id));

        var auditEvent = Assert.Single(authAuditEventRepository.Added, e => e.EventType == AuthAuditEventType.UserRolesChanged);
        Assert.Equal(created.Id, auditEvent.UserId);
        Assert.Equal(ActorId, auditEvent.ActorUserId);
        Assert.Equal("127.0.0.1", auditEvent.IpAddress);
        Assert.Equal("correlation-2", auditEvent.CorrelationId);
    }

    [Fact] // FR-6.2: an unrecognized role name is rejected rather than silently ignored.
    public async Task ChangeUserRolesAsync_WithUnknownRole_ThrowsValidation()
    {
        var sut = CreateSut(out _, out _);
        var created = await sut.CreateUserAsync(
            new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" }),
            ActorId, null, null, CancellationToken.None);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => sut.ChangeUserRolesAsync(created.Id, new ChangeUserRolesRequestDto(new[] { "NotARole" }), ActorId, null, null, CancellationToken.None));
    }

    [Fact] // FR-6.2: "one or more roles" is a requirement, not an option — an empty role list is rejected.
    public async Task ChangeUserRolesAsync_WithNoRoles_ThrowsValidation()
    {
        var sut = CreateSut(out _, out _);
        var created = await sut.CreateUserAsync(
            new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" }),
            ActorId, null, null, CancellationToken.None);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => sut.ChangeUserRolesAsync(created.Id, new ChangeUserRolesRequestDto(Array.Empty<string>()), ActorId, null, null, CancellationToken.None));
    }

    [Fact] // FR-6.2: changing roles for a non-existent user is a 404, not a silent no-op.
    public async Task ChangeUserRolesAsync_WithUnknownUserId_ThrowsNotFound()
    {
        var sut = CreateSut(out _, out _);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => sut.ChangeUserRolesAsync(Guid.NewGuid(), new ChangeUserRolesRequestDto(new[] { "Coder" }), ActorId, null, null, CancellationToken.None));
    }

    [Fact] // FR-6.8: deactivating an active user flips the flag, revokes sessions, and records an audit event.
    public async Task DeactivateUserAsync_WithActiveUser_DeactivatesRevokesSessionsAndRecordsAuditEvent()
    {
        var sut = CreateSut(out var userRepository, out var authAuditEventRepository, out var refreshTokenRepository);
        var created = await sut.CreateUserAsync(
            new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" }),
            ActorId, null, null, CancellationToken.None);

        await sut.DeactivateUserAsync(created.Id, ActorId, "127.0.0.1", "correlation-3", CancellationToken.None);

        var savedUser = Assert.Single(userRepository.Added);
        Assert.False(savedUser.IsActive);
        Assert.Equal(created.Id, Assert.Single(refreshTokenRepository.RevokedForUserIds));

        var auditEvent = Assert.Single(authAuditEventRepository.Added, e => e.EventType == AuthAuditEventType.UserDeactivated);
        Assert.Equal(created.Id, auditEvent.UserId);
        Assert.Equal(ActorId, auditEvent.ActorUserId);
        Assert.Equal("127.0.0.1", auditEvent.IpAddress);
        Assert.Equal("correlation-3", auditEvent.CorrelationId);
    }

    [Fact] // FR-6.8: deactivating an already-inactive account is a conflict, not a silent no-op.
    public async Task DeactivateUserAsync_WithAlreadyInactiveUser_ThrowsConflict()
    {
        var sut = CreateSut(out _, out _, out var refreshTokenRepository);
        var created = await sut.CreateUserAsync(
            new CreateUserRequestDto("jdoe", "jdoe@aris.local", "P@ssword1", "Jane Doe", new[] { "Coder" }),
            ActorId, null, null, CancellationToken.None);
        await sut.DeactivateUserAsync(created.Id, ActorId, null, null, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictAppException>(
            () => sut.DeactivateUserAsync(created.Id, ActorId, null, null, CancellationToken.None));

        Assert.Single(refreshTokenRepository.RevokedForUserIds);
    }

    [Fact] // FR-6.8: deactivating a non-existent user is a 404, not a silent no-op.
    public async Task DeactivateUserAsync_WithUnknownUserId_ThrowsNotFound()
    {
        var sut = CreateSut(out _, out _);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => sut.DeactivateUserAsync(Guid.NewGuid(), ActorId, null, null, CancellationToken.None));
    }

    private static async Task SeedUserAsync(
        UserManagementService sut,
        string username,
        string email,
        string displayName,
        string role)
    {
        await sut.CreateUserAsync(
            new CreateUserRequestDto(username, email, "P@ssword1", displayName, new[] { role }),
            ActorId,
            null,
            null,
            CancellationToken.None);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly IReadOnlyCollection<Role> _roles;

        public FakeUserRepository(IReadOnlyCollection<Role> roles)
        {
            _roles = roles;
        }

        public List<User> Added { get; } = new();
        public HashSet<string> ExistingUsernamesOrEmails { get; } = new();

        public Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken)
        {
            return Task.FromResult(Added.SingleOrDefault(user => user.Username == usernameOrEmail || user.Email == usernameOrEmail));
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(Added.SingleOrDefault(user => user.Id == id));
        }

        public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingUsernamesOrEmails.Contains(username) || ExistingUsernamesOrEmails.Contains(email));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            foreach (var userRole in user.UserRoles)
            {
                userRole.Role ??= _roles.Single(role => role.Id == userRole.RoleId);
            }

            Added.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
        {
            foreach (var userRole in user.UserRoles)
            {
                userRole.Role ??= _roles.Single(role => role.Id == userRole.RoleId);
            }

            return Task.CompletedTask;
        }

        public Task<(IReadOnlyCollection<User> Users, int TotalCount)> SearchAsync(
            string? query, int page, int pageSize, CancellationToken cancellationToken)
        {
            var matches = Added
                .Where(user => string.IsNullOrWhiteSpace(query)
                    || user.Username.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || user.Email.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || user.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(user => user.Username)
                .ToList();

            var pageItems = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return Task.FromResult<(IReadOnlyCollection<User> Users, int TotalCount)>((pageItems, matches.Count));
        }
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        private readonly IReadOnlyCollection<Role> _roles;

        public FakeRoleRepository(IReadOnlyCollection<Role> roles)
        {
            _roles = roles;
        }

        public Task<IReadOnlyCollection<Role>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken)
        {
            var nameSet = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
            IReadOnlyCollection<Role> matches = _roles.Where(role => nameSet.Contains(role.Name)).ToList();
            return Task.FromResult(matches);
        }
    }

    private sealed class FakeAuthAuditEventRepository : IAuthAuditEventRepository
    {
        public List<AuthAuditEvent> Added { get; } = new();

        public Task AddAsync(AuthAuditEvent authAuditEvent, CancellationToken cancellationToken)
        {
            Added.Add(authAuditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<Guid> RevokedForUserIds { get; } = new();

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Not used by UserManagementService tests.");
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Not used by UserManagementService tests.");
        }

        public Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Not used by UserManagementService tests.");
        }

        public Task<bool> RotateAsync(RefreshToken currentToken, RefreshToken newToken, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Not used by UserManagementService tests.");
        }

        public Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            RevokedForUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public bool Verify(string password, string passwordHash)
        {
            throw new NotSupportedException("Not used by UserManagementService tests.");
        }

        public string Hash(string password)
        {
            return $"hashed:{password}";
        }
    }

    private sealed class NullPhiSafeLogger : IPhiSafeLogger<UserManagementService>
    {
        public void LogInformation(string messageTemplate, params object?[] args) { }
        public void LogWarning(string messageTemplate, params object?[] args) { }
        public void LogError(Exception? exception, string messageTemplate, params object?[] args) { }
    }
}
