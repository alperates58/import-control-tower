using System;

namespace ImportControlTower.Domain.Entities;

public class ShipmentContainer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public string ContainerNumber { get; set; } = string.Empty;
    public string NormalizedContainerNumber { get; set; } = string.Empty;
    public string ContainerType { get; set; } = "40HC";
    public string? SealNumber { get; set; }
    public decimal? GrossWeightKg { get; set; }
    public decimal? NetWeightKg { get; set; }
    public int? PackageCount { get; set; }
    public string Status { get; set; } = "Assigned";
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}
