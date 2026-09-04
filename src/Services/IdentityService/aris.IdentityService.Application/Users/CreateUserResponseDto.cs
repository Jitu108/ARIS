namespace aris.IdentityService.Application.Users;

public sealed record CreateUserResponseDto(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    string[] Roles,
    bool IsActive);
