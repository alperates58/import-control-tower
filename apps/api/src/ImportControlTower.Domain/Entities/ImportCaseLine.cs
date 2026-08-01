using System;
using System.Collections.Generic;

namespace ImportControlTower.Domain.Entities;

public class ImportCaseLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ImportCaseId { get; set; }
    public ImportCase? ImportCase { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal ReleasedQuantity { get; set; } = 0;
    public string Status { get; set; } = "Allocated";
    public DateTime? PlannedShipmentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }

    public decimal EffectiveAllocatedQuantity => AllocatedQuantity - ReleasedQuantity;

    public ICollection<ShipmentLineAllocation> ShipmentAllocations { get; set; } = new List<ShipmentLineAllocation>();
}
