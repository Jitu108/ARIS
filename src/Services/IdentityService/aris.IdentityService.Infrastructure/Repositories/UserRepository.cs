using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Domain.Entities;
using aris.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace aris.IdentityService.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(
                user => user.Username == usernameOrEmail || user.Email == usernameOrEmail,
                cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }
}
