using aris.BuildingBlocks.Results;

namespace aris.IdentityService.Application.Authentication;

public interface IAuthenticationService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<Result<LoginResponseDto>> RefreshAsync(string? refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken);
}
