using aris.BuildingBlocks.Entities;

namespace aris.IdentityService.Domain.Entities;

public sealed class Role : BaseEntity<int>
{
    public required string Name { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
