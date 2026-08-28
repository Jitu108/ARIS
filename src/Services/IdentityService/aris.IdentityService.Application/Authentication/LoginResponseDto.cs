namespace aris.IdentityService.Application.Authentication;

public sealed record LoginResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    LoginUserDto User,
    bool MustChangePassword);

public sealed record LoginUserDto(Guid Id, string DisplayName, IReadOnlyCollection<string> Roles);
