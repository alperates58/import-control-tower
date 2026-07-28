using System;

namespace ImportControlTower.Domain.Entities;

public class ImportBatchRow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int RowNumber { get; set; }
    public string RawDataJson { get; set; } = "{}";
    public string? NormalizedDataJson { get; set; }
    public string ValidationStatus { get; set; } = "Valid";
    public string? ErrorCodesJson { get; set; }
    public string? WarningCodesJson { get; set; }
    public Guid? MatchedOrderId { get; set; }
    public PurchaseOrder? MatchedOrder { get; set; }
    public Guid? MatchedLineId { get; set; }
    public PurchaseOrderLine? MatchedLine { get; set; }
    public string ImportAction { get; set; } = "CreateOrder";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
