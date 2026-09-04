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

    public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken)
    {
        return _dbContext.Users.AnyAsync(user => user.Username == username || user.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyCollection<User> Users, int TotalCount)> SearchAsync(
        string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var usersQuery = _dbContext.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            usersQuery = usersQuery.Where(user =>
                EF.Functions.Like(user.Username, $"%{query}%")
                || EF.Functions.Like(user.Email, $"%{query}%")
                || EF.Functions.Like(user.DisplayName, $"%{query}%"));
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);

        var users = await usersQuery
            .OrderBy(user => user.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }
}
