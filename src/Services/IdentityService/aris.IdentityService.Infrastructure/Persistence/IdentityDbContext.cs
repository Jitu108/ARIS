using Microsoft.EntityFrameworkCore;

namespace aris.IdentityService.Infrastructure.Persistence;

/// <summary>
/// Owns IdentityDb exclusively — no other service reaches into this database (CLAUDE.md
/// service-ownership rule). Entity sets (User, Role, RefreshToken, AuthAuditEvent,
/// PasswordResetToken — Detailed Plan §4.2) land here as their tickets implement them.
/// </summary>
public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }
}
