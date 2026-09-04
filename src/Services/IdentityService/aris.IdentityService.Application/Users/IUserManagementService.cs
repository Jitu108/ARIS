namespace aris.IdentityService.Application.Users;

public interface IUserManagementService
{
    Task<CreateUserResponseDto> CreateUserAsync(
        CreateUserRequestDto request,
        Guid actorUserId,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken);
}
