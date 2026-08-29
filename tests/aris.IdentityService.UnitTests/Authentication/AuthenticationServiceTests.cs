using aris.BuildingBlocks.Logging;
using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Application.Authentication;
using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.UnitTests.Authentication;

public class AuthenticationServiceTests
{
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private static User CreateActiveAdminUser(bool mustChangePassword = false) => new()
    {
        Id = AdminUserId,
        Username = "admin",
        Email = "admin@aris.local",
        PasswordHash = "correct-hash",
        DisplayName = "System Administrator",
        IsActive = true,
        MustChangePassword = mustChangePassword,
        UserRoles = new List<UserRole>
        {
            new() { UserId = AdminUserId, RoleId = 1, Role = new Role { Id = 1, Name = "Administrator" } },
        },
    };

    private static AuthenticationService CreateSut(
        User? user,
        out FakeRefreshTokenRepository refreshTokenRepository,
        FakePasswordHasher? passwordHasher = null)
    {
        var userRepository = new FakeUserRepository(user);
        refreshTokenRepository = new FakeRefreshTokenRepository();

        return new AuthenticationService(
            userRepository,
            refreshTokenRepository,
            passwordHasher ?? new FakePasswordHasher(matches: true),
            new FakeJwtTokenGenerator(),
            new NullPhiSafeLogger<AuthenticationService>(),
            refreshTokenExpiryDays: 14);
    }

    [Fact] // UT-ID-01: valid credentials produce a token carrying the correct sub/roles/exp-bearing claims.
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenForCorrectUserAndRoles()
    {
        var user = CreateActiveAdminUser();
        var sut = CreateSut(user, out var refreshTokenRepository);

        var result = await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-access-token", result.Value.AccessToken);
        Assert.Equal(user.Id, result.Value.User.Id);
        Assert.Equal(user.DisplayName, result.Value.User.DisplayName);
        Assert.Equal(new[] { "Administrator" }, result.Value.User.Roles);
        Assert.False(result.Value.MustChangePassword);
        Assert.Single(refreshTokenRepository.Added);
        Assert.Equal(user.Id, refreshTokenRepository.Added[0].UserId);
    }

    [Fact] // UT-ID-03 (rejects a non-matching password): invalid password fails with the generic error.
    public async Task LoginAsync_WithWrongPassword_ReturnsGenericInvalidCredentialsError()
    {
        var user = CreateActiveAdminUser();
        var sut = CreateSut(user, out _, new FakePasswordHasher(matches: false));

        var result = await sut.LoginAsync(new LoginRequestDto("admin", "wrong"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid username or password.", result.Error.Message);
    }

    [Fact] // FR-1.2: an unknown username fails with the identical generic error (anti-enumeration).
    public async Task LoginAsync_WithUnknownUsername_ReturnsSameGenericErrorAsWrongPassword()
    {
        var sut = CreateSut(user: null, out _);

        var result = await sut.LoginAsync(new LoginRequestDto("nosuchuser", "whatever"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid username or password.", result.Error.Message);
    }

    [Fact] // FR-1.2: a deactivated account fails with the identical generic error, never a distinguishable one.
    public async Task LoginAsync_WithInactiveUser_ReturnsSameGenericErrorAsWrongPassword()
    {
        var user = CreateActiveAdminUser();
        user.IsActive = false;
        var sut = CreateSut(user, out _);

        var result = await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid username or password.", result.Error.Message);
    }

    [Fact] // FR-1.4: logging out with the token just issued by login revokes that exact token.
    public async Task LogoutAsync_WithTokenIssuedByLogin_RevokesThatToken()
    {
        var user = CreateActiveAdminUser();
        var sut = CreateSut(user, out var refreshTokenRepository);
        var loginResult = await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);

        await sut.LogoutAsync(loginResult.Value.RefreshToken, CancellationToken.None);

        var issuedToken = Assert.Single(refreshTokenRepository.Added);
        Assert.NotNull(issuedToken.RevokedAtUtc);
    }

    [Fact] // FR-1.4: logout must not error/leak validity for a token it doesn't recognize.
    public async Task LogoutAsync_WithUnknownToken_DoesNotThrowAndRevokesNothing()
    {
        var sut = CreateSut(CreateActiveAdminUser(), out var refreshTokenRepository);
        await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);

        await sut.LogoutAsync("not-a-real-token", CancellationToken.None);

        Assert.DoesNotContain(refreshTokenRepository.Added, token => token.RevokedAtUtc is not null);
    }

    [Fact] // FR-1.4: logging out twice with the same token is idempotent — the second call is a no-op.
    public async Task LogoutAsync_CalledTwiceWithSameToken_IsIdempotent()
    {
        var sut = CreateSut(CreateActiveAdminUser(), out var refreshTokenRepository);
        var loginResult = await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);

        await sut.LogoutAsync(loginResult.Value.RefreshToken, CancellationToken.None);
        var revokedAtUtc = refreshTokenRepository.Added.Single().RevokedAtUtc;
        await sut.LogoutAsync(loginResult.Value.RefreshToken, CancellationToken.None);

        Assert.Equal(revokedAtUtc, refreshTokenRepository.Added.Single().RevokedAtUtc);
    }

    [Fact] // FR-1.4: a missing/blank refresh token is a no-op rather than a crash.
    public async Task LogoutAsync_WithNullOrWhitespaceToken_DoesNotThrow()
    {
        var sut = CreateSut(CreateActiveAdminUser(), out _);

        await sut.LogoutAsync(null, CancellationToken.None);
        await sut.LogoutAsync("   ", CancellationToken.None);
    }

    [Fact] // UT-ID-04: refresh rotation issues a new token and marks the presented one revoked.
    public async Task RefreshAsync_WithValidToken_IssuesNewTokenAndRevokesPresentedOne()
    {
        var user = CreateActiveAdminUser();
        var sut = CreateSut(user, out var refreshTokenRepository);
        var loginResult = await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);
        var presentedToken = loginResult.Value.RefreshToken;

        var refreshResult = await sut.RefreshAsync(presentedToken, CancellationToken.None);

        Assert.True(refreshResult.IsSuccess);
        Assert.NotEqual(presentedToken, refreshResult.Value.RefreshToken);
        Assert.Equal(2, refreshTokenRepository.Added.Count);
        var oldToken = refreshTokenRepository.Added[0];
        var newToken = refreshTokenRepository.Added[1];
        Assert.NotNull(oldToken.RevokedAtUtc);
        Assert.Equal(newToken.Id, oldToken.ReplacedByTokenId);
        Assert.Null(newToken.RevokedAtUtc);
    }

    [Fact] // UT-ID-05: reuse of an already-revoked (rotated) refresh token is rejected and revokes the whole chain.
    public async Task RefreshAsync_WithAlreadyRotatedToken_IsRejectedAndRevokesAllActiveTokensForUser()
    {
        var user = CreateActiveAdminUser();
        var sut = CreateSut(user, out var refreshTokenRepository);
        var loginResult = await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);
        var originalToken = loginResult.Value.RefreshToken;
        await sut.RefreshAsync(originalToken, CancellationToken.None);

        var reuseResult = await sut.RefreshAsync(originalToken, CancellationToken.None);

        Assert.True(reuseResult.IsFailure);
        Assert.Equal("Invalid or expired refresh token.", reuseResult.Error.Message);
        Assert.All(refreshTokenRepository.Added, token => Assert.NotNull(token.RevokedAtUtc));
    }

    [Fact] // Refresh with an unrecognized token fails with the generic error, not a crash.
    public async Task RefreshAsync_WithUnknownToken_ReturnsGenericInvalidRefreshTokenError()
    {
        var sut = CreateSut(CreateActiveAdminUser(), out _);

        var result = await sut.RefreshAsync("not-a-real-token", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid or expired refresh token.", result.Error.Message);
    }

    [Fact] // A missing/blank refresh token is rejected rather than crashing.
    public async Task RefreshAsync_WithNullOrWhitespaceToken_ReturnsGenericInvalidRefreshTokenError()
    {
        var sut = CreateSut(CreateActiveAdminUser(), out _);

        var result = await sut.RefreshAsync(null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid or expired refresh token.", result.Error.Message);
    }

    [Fact] // A refresh token belonging to a now-deactivated user is rejected even though the token itself hasn't expired.
    public async Task RefreshAsync_ForDeactivatedUser_ReturnsGenericInvalidRefreshTokenError()
    {
        var user = CreateActiveAdminUser();
        var sut = CreateSut(user, out _);
        var loginResult = await sut.LoginAsync(new LoginRequestDto("admin", "Admin@12345"), CancellationToken.None);
        user.IsActive = false;

        var result = await sut.RefreshAsync(loginResult.Value.RefreshToken, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invalid or expired refresh token.", result.Error.Message);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(User? user) => _user = user;

        public Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken) =>
            Task.FromResult(_user is not null && _user.Username == usernameOrEmail ? _user : null);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_user is not null && _user.Id == id ? _user : null);
    }

    private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Added { get; } = new();

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            Added.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(Added.SingleOrDefault(token => token.TokenHash == tokenHash));
        }

        public Task RevokeAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task<bool> RotateAsync(RefreshToken currentToken, RefreshToken newToken, CancellationToken cancellationToken)
        {
            currentToken.RevokedAtUtc = DateTime.UtcNow;
            currentToken.ReplacedByTokenId = newToken.Id;
            Added.Add(newToken);
            return Task.FromResult(true);
        }

        public Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            var revokedAtUtc = DateTime.UtcNow;
            foreach (var token in Added.Where(token => token.UserId == userId && token.RevokedAtUtc is null))
            {
                token.RevokedAtUtc = revokedAtUtc;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        private readonly bool _matches;

        public FakePasswordHasher(bool matches) => _matches = matches;

        public bool Verify(string password, string passwordHash) => _matches;
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public GeneratedAccessToken Generate(User user, IReadOnlyCollection<string> roles) =>
            new("fake-access-token", DateTime.UtcNow.AddMinutes(30), 1800);
    }

    private sealed class NullPhiSafeLogger<T> : IPhiSafeLogger<T>
    {
        public void LogInformation(string messageTemplate, params object?[] args) { }
        public void LogWarning(string messageTemplate, params object?[] args) { }
        public void LogError(Exception? exception, string messageTemplate, params object?[] args) { }
    }
}
