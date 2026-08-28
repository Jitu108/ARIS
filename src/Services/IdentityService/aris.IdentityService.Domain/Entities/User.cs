using aris.BuildingBlocks.Entities;

namespace aris.IdentityService.Domain.Entities;

public sealed class User : BaseEntity<Guid>
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
