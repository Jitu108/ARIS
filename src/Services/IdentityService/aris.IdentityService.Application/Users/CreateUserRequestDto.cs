namespace aris.IdentityService.Application.Users;

public sealed record CreateUserRequestDto(
    string Username,
    string Email,
    string Password,
    string DisplayName,
    string[] Roles);
