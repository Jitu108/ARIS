using aris.BuildingBlocks.Results;

namespace aris.IdentityService.Application.Authentication;

public interface IAuthenticationService
{
    Task<Result<LoginResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
}
