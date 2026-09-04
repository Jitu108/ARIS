using aris.IdentityService.Application.Abstractions;
using aris.IdentityService.Domain.Entities;
using aris.IdentityService.Infrastructure.Persistence;

namespace aris.IdentityService.Infrastructure.Repositories;

public sealed class AuthAuditEventRepository : IAuthAuditEventRepository
{
    private readonly IdentityDbContext _dbContext;

    public AuthAuditEventRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuthAuditEvent authAuditEvent, CancellationToken cancellationToken)
    {
        _dbContext.AuthAuditEvents.Add(authAuditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
