using aris.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aris.IdentityService.Infrastructure.Persistence.Configurations;

public sealed class AuthAuditEventConfiguration : IEntityTypeConfiguration<AuthAuditEvent>
{
    public void Configure(EntityTypeBuilder<AuthAuditEvent> builder)
    {
        builder.ToTable("AuthAuditEvents");
        builder.HasKey(authAuditEvent => authAuditEvent.Id);
        builder.Property(authAuditEvent => authAuditEvent.EventType).HasMaxLength(32).IsRequired();
        builder.Property(authAuditEvent => authAuditEvent.IpAddress).HasMaxLength(64);
        builder.Property(authAuditEvent => authAuditEvent.CorrelationId).HasMaxLength(64);

        builder.HasIndex(authAuditEvent => new { authAuditEvent.UserId, authAuditEvent.TimestampUtc });
    }
}
