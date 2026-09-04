using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Domain.Entities;
using aris.IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace aris.IdentityService.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _dbContext;

    public RoleRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Role>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken)
    {
        var nameList = names.ToList();

        return await _dbContext.Roles
            .Where(role => nameList.Contains(role.Name))
            .ToListAsync(cancellationToken);
    }
}
