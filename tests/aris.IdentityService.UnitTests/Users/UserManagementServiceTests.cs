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

    private static UserManagementService CreateSut(
        out FakeUserRepository userRepository,
        out FakeAuthAuditEventRepository authAuditEventRepository,
        IReadOnlyCollection<Role>? seededRoles = null)
    {
        userRepository = new FakeUserRepository();
        authAuditEventRepository = new FakeAuthAuditEventRepository();

        return new UserManagementService(
            userRepository,
            new FakeRoleRepository(seededRoles ?? new[] { AdministratorRole, CoderRole }),
            new FakePasswordHasher(),
            authAuditEventRepository,
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

    private sealed class FakeUserRepository : IUserRepository
    {
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
            Added.Add(user);
            return Task.CompletedTask;
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
