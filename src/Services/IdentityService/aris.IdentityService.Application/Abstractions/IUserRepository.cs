using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<User> Users, int TotalCount)> SearchAsync(
        string? query, int page, int pageSize, CancellationToken cancellationToken);
}
