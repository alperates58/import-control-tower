namespace ImportControlTower.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ActorUserId { get; set; }
    public virtual ApplicationUser? ActorUser { get; set; }

    public string? ActorUsername { get; set; }
    public string ActorType { get; set; } = "User"; // User, Anonymous, System
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}
