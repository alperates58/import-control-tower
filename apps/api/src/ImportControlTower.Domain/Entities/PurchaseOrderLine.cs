using System;

namespace ImportControlTower.Domain.Entities;

public class PurchaseOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public int LineNumber { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public string NormalizedStockCode { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTime? SasDate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
