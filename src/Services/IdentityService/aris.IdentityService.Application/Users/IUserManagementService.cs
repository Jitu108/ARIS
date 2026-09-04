namespace aris.IdentityService.Application.Users;

public interface IUserManagementService
{
    Task<CreateUserResponseDto> CreateUserAsync(
        CreateUserRequestDto request,
        Guid actorUserId,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken);

    Task<ListUsersResponseDto> ListUsersAsync(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<UserSummaryDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserSummaryDto> ChangeUserRolesAsync(
        Guid id,
        ChangeUserRolesRequestDto request,
        Guid actorUserId,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken);

    Task DeactivateUserAsync(
        Guid id,
        Guid actorUserId,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken);
}
