using aris.BuildingBlocks.Exceptions;
using aris.IdentityService.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aris.IdentityService.Api.Controllers;

[ApiController]
[Route("identity")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.LoginAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            throw new UnauthorizedAppException(result.Error.Message);
        }

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Refresh(RefreshRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.RefreshAsync(request.RefreshToken, cancellationToken);

        if (result.IsFailure)
        {
            throw new UnauthorizedAppException(result.Error.Message);
        }

        return Ok(result.Value);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutRequestDto request, CancellationToken cancellationToken)
    {
        await _authenticationService.LogoutAsync(request.RefreshToken, cancellationToken);

        return NoContent();
    }
}
