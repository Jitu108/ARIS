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
}
