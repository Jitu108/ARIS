namespace aris.IdentityService.Application.Users;

public sealed record ListUsersResponseDto(
    IReadOnlyList<UserSummaryDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
