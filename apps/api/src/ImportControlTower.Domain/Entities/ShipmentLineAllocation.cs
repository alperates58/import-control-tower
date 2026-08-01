using System;

namespace ImportControlTower.Domain.Entities;

public class ShipmentLineAllocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public Guid ImportCaseLineId { get; set; }
    public ImportCaseLine? ImportCaseLine { get; set; }
    public Guid ImportCaseId { get; set; }
    public ImportCase? ImportCase { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal ReleasedQuantity { get; set; } = 0;
    public decimal ShippedQuantity { get; set; } = 0;
    public decimal ReceivedQuantity { get; set; } = 0;
    public string Status { get; set; } = "Allocated";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }

    public decimal EffectiveAllocatedQuantity => AllocatedQuantity - ReleasedQuantity;
}
