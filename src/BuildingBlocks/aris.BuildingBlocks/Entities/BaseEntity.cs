namespace aris.BuildingBlocks.Entities;

public abstract class BaseEntity<TId>
{
    public required TId Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}
