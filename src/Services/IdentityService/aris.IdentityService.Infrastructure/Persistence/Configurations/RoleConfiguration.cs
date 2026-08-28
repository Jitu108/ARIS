using aris.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aris.IdentityService.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public const int AdministratorId = 1;
    public const int ClinicianId = 2;
    public const int CoderId = 3;
    public const int RiskAnalystId = 4;
    public const int AuditorId = 5;
    public const int ResearcherId = 6;

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Name).HasMaxLength(64).IsRequired();
        builder.HasIndex(role => role.Name).IsUnique();

        var seededAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new { Id = AdministratorId, Name = "Administrator", CreatedAtUtc = seededAtUtc, CreatedBy = "system" },
            new { Id = ClinicianId, Name = "Clinician", CreatedAtUtc = seededAtUtc, CreatedBy = "system" },
            new { Id = CoderId, Name = "Coder", CreatedAtUtc = seededAtUtc, CreatedBy = "system" },
            new { Id = RiskAnalystId, Name = "RiskAnalyst", CreatedAtUtc = seededAtUtc, CreatedBy = "system" },
            new { Id = AuditorId, Name = "Auditor", CreatedAtUtc = seededAtUtc, CreatedBy = "system" },
            new { Id = ResearcherId, Name = "Researcher", CreatedAtUtc = seededAtUtc, CreatedBy = "system" });
    }
}
