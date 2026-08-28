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

        var roles = user.UserRoles
            .Where(userRole => userRole.Role is not null)
            .Select(userRole => userRole.Role!.Name)
            .ToArray();

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

    private (RefreshToken Entity, string RawToken) CreateRefreshToken(Guid userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "system",
        };

        return (entity, rawToken);
    }
}
