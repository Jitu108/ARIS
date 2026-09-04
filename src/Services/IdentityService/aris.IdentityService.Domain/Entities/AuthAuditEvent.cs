namespace aris.IdentityService.Domain.Entities;

public sealed class AuthAuditEvent
{
    public required Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public required string EventType { get; set; }
    public Guid? ActorUserId { get; set; }
    public required DateTime TimestampUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
}
