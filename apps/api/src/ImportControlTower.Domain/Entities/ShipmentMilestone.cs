using System;

namespace ImportControlTower.Domain.Entities;

public class ShipmentMilestone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public int SequenceNumber { get; set; }
    public string MilestoneType { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public string TimezoneId { get; set; } = "UTC";
    public DateTime? PlannedAtUtc { get; set; }
    public DateTime? EstimatedAtUtc { get; set; }
    public DateTime? ActualAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public string Source { get; set; } = "Manual";
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}
