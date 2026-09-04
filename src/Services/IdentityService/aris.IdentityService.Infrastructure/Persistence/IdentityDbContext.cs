using aris.IdentityService.Domain.Entities;
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

    public DbSet<User> Users
    {
        get { return Set<User>(); }
    }

    public DbSet<Role> Roles
    {
        get { return Set<Role>(); }
    }

    public DbSet<UserRole> UserRoles
    {
        get { return Set<UserRole>(); }
    }

    public DbSet<RefreshToken> RefreshTokens
    {
        get { return Set<RefreshToken>(); }
    }

    public DbSet<AuthAuditEvent> AuthAuditEvents
    {
        get { return Set<AuthAuditEvent>(); }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
