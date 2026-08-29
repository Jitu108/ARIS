using System.Security.Cryptography;
using System.Text;
using aris.BuildingBlocks.Logging;
using aris.BuildingBlocks.Results;
using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.Application.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private static readonly Error InvalidCredentialsError =
        new("Identity.InvalidCredentials", "Invalid username or password.");

    private static readonly Error InvalidRefreshTokenError =
        new("Identity.InvalidRefreshToken", "Invalid or expired refresh token.");

    // Verified against when no user exists, so an unknown username still pays the same BCrypt cost
    // as a real login attempt — without this, the short-circuited hash check would make login
    // attempts against unknown/inactive accounts measurably faster, turning FR-1.2's identical
    // response body into a distinguishable-by-timing enumeration oracle.
    private const string DummyPasswordHash = "$2a$11$dHAgPDKcCChgK3UK8HHhqOKuLKzTsolJQ4y65tB64fQIp0CgsEHp2";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPhiSafeLogger<AuthenticationService> _logger;
    private readonly int _refreshTokenExpiryDays;

    public AuthenticationService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IPhiSafeLogger<AuthenticationService> logger,
        int refreshTokenExpiryDays)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
        _refreshTokenExpiryDays = refreshTokenExpiryDays;
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.Username, cancellationToken);

        var passwordMatches = _passwordHasher.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);

        if (user is null || !user.IsActive || !passwordMatches)
        {
            _logger.LogWarning("Login attempt rejected.");
            return Result.Failure<LoginResponseDto>(InvalidCredentialsError);
        }

        var roles = GetRoleNames(user);

        var accessToken = _jwtTokenGenerator.Generate(user, roles);
        var (refreshTokenEntity, rawRefreshToken) = CreateRefreshToken(user.Id);

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        _logger.LogInformation("User {UserId} authenticated successfully.", user.Id);

        var response = new LoginResponseDto(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresInSeconds,
            new LoginUserDto(user.Id, user.DisplayName, roles),
            user.MustChangePassword);

        return Result.Success(response);
    }

    public async Task<Result<LoginResponseDto>> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure<LoginResponseDto>(InvalidRefreshTokenError);
        }

        var tokenHash = HashToken(refreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            return Result.Failure<LoginResponseDto>(InvalidRefreshTokenError);
        }

        if (existingToken.RevokedAtUtc is not null)
        {
            // A refresh token is single-use; presenting one that's already revoked (i.e. already
            // rotated) means it was captured and replayed by someone else. Treat the whole chain
            // as compromised rather than trusting just this one token (CLAUDE.md refresh-token rule).
            await _refreshTokenRepository.RevokeAllActiveForUserAsync(existingToken.UserId, cancellationToken);
            _logger.LogWarning("Refresh token reuse detected for user {UserId}; all active sessions revoked.", existingToken.UserId);
            return Result.Failure<LoginResponseDto>(InvalidRefreshTokenError);
        }

        if (existingToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Result.Failure<LoginResponseDto>(InvalidRefreshTokenError);
        }

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<LoginResponseDto>(InvalidRefreshTokenError);
        }

        var roles = GetRoleNames(user);

        var accessToken = _jwtTokenGenerator.Generate(user, roles);
        var (newRefreshTokenEntity, rawRefreshToken) = CreateRefreshToken(user.Id);

        var rotated = await _refreshTokenRepository.RotateAsync(existingToken, newRefreshTokenEntity, cancellationToken);

        if (!rotated)
        {
            // Another request rotated this same token first — a concurrent replay, not this
            // caller's fault, but it must fail the same generic way as any other invalid token.
            return Result.Failure<LoginResponseDto>(InvalidRefreshTokenError);
        }

        _logger.LogInformation("Refresh token rotated for user {UserId}.", user.Id);

        var response = new LoginResponseDto(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresInSeconds,
            new LoginUserDto(user.Id, user.DisplayName, roles),
            user.MustChangePassword);

        return Result.Success(response);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashToken(refreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        // An unknown or already-revoked token is a no-op, not an error — logout must not become an
        // oracle that reveals whether a given refresh token is (still) valid.
        if (existingToken is null || existingToken.RevokedAtUtc is not null)
        {
            return;
        }

        await _refreshTokenRepository.RevokeAsync(existingToken, cancellationToken);

        _logger.LogInformation("Refresh token revoked for user {UserId} via logout.", existingToken.UserId);
    }

    private static string[] GetRoleNames(User user)
    {
        return user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role!.Name)
            .ToArray();
    }

    private (RefreshToken Entity, string RawToken) CreateRefreshToken(Guid userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "system",
            RowVersion = Guid.NewGuid().ToByteArray(),
        };

        return (entity, rawToken);
    }

    private static string HashToken(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
