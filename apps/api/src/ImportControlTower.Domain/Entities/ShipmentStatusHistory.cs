using System;

namespace ImportControlTower.Domain.Entities;

public class ShipmentStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ImportCaseId { get; set; }
    public ImportCase? ImportCase { get; set; }
    public Guid? ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid ChangedByUserId { get; set; }
    public ApplicationUser? ChangedByUser { get; set; }
}
