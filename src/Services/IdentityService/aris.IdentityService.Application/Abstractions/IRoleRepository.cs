using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.Application.Abstractions;

public interface IRoleRepository
{
    Task<IReadOnlyCollection<Role>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken);
}
