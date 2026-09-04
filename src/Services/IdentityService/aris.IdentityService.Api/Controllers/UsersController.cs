using System.IdentityModel.Tokens.Jwt;
using aris.BuildingBlocks.Middleware;
using aris.IdentityService.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace aris.IdentityService.Api.Controllers;

[ApiController]
[Route("identity/users")]
[Authorize(Roles = "Administrator")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateUserResponseDto>> CreateUser(CreateUserRequestDto request, CancellationToken cancellationToken)
    {
        var actorUserId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var correlationId = HttpContext.GetCorrelationId();

        var response = await _userManagementService.CreateUserAsync(request, actorUserId, ipAddress, correlationId, cancellationToken);

        return CreatedAtAction(nameof(CreateUser), new { id = response.Id }, response);
    }

    [HttpGet]
    public async Task<ActionResult<ListUsersResponseDto>> ListUsers(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await _userManagementService.ListUsersAsync(query, page, pageSize, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var response = await _userManagementService.GetUserByIdAsync(id, cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<UserSummaryDto>> ChangeUserRoles(
        Guid id,
        ChangeUserRolesRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorUserId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var correlationId = HttpContext.GetCorrelationId();

        var response = await _userManagementService.ChangeUserRolesAsync(
            id, request, actorUserId, ipAddress, correlationId, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var actorUserId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var correlationId = HttpContext.GetCorrelationId();

        await _userManagementService.DeactivateUserAsync(id, actorUserId, ipAddress, correlationId, cancellationToken);

        return NoContent();
    }
}
