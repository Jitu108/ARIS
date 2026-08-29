using aris.IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace aris.IdentityService.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        // App-managed (not IsRowVersion()) so the optimistic-concurrency guard behaves identically
        // on every provider this context runs against, including SQLite in tests, which has no
        // native auto-updating rowversion column type — see RefreshTokenRepository for where this
        // value is actually bumped on every write.
        builder.Property(token => token.RowVersion).IsConcurrencyToken();

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
