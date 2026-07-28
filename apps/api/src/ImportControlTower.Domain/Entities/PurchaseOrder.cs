using System;
using System.Collections.Generic;

namespace ImportControlTower.Domain.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = string.Empty;
    public string NormalizedOrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string NormalizedSupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Open";
    public string Source { get; set; } = "ExcelImport";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
