using aris.BuildingBlocks.Entities;

namespace aris.IdentityService.Domain.Entities;

public sealed class RefreshToken : BaseEntity<Guid>
{
    public required Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public required DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public User? User { get; set; }
}
