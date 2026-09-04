namespace aris.IdentityService.Application.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string Username,
    string Email,
    string DisplayName,
    string[] Roles,
    bool IsActive);
