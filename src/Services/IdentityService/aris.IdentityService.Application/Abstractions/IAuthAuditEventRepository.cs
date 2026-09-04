using aris.IdentityService.Domain.Entities;

namespace aris.IdentityService.Application.Abstractions;

public interface IAuthAuditEventRepository
{
    Task AddAsync(AuthAuditEvent authAuditEvent, CancellationToken cancellationToken);
}
