using aris.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aris.IdentityService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>
    /// Dev-only seed account so the login slice is verifiable before Administrator-driven user
    /// creation (FR-6.1) exists. Username "admin", password "Admin@12345" — never used outside
    /// local development, must not be relied on in any deployed environment.
    /// </summary>
    public static readonly Guid SeededAdminUserId = new("11111111-1111-1111-1111-111111111111");

    private const string SeededAdminPasswordHash = "$2a$11$dHAgPDKcCChgK3UK8HHhqOKuLKzTsolJQ4y65tB64fQIp0CgsEHp2";

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Username).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(user => user.PasswordHash).IsRequired();

        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();

        var seededAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(new User
        {
            Id = SeededAdminUserId,
            Username = "admin",
            Email = "admin@aris.local",
            PasswordHash = SeededAdminPasswordHash,
            DisplayName = "System Administrator",
            IsActive = true,
            MustChangePassword = false,
            CreatedAtUtc = seededAtUtc,
            CreatedBy = "system",
        });
    }
}
