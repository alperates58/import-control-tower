using System;
using System.Collections.Generic;

namespace ImportControlTower.Domain.Entities;

public class ImportBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileSha256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int TotalRowCount { get; set; }
    public int ValidRowCount { get; set; }
    public int InvalidRowCount { get; set; }
    public int WarningRowCount { get; set; }
    public int ImportedOrderCount { get; set; }
    public int ImportedLineCount { get; set; }
    public string Status { get; set; } = "Uploaded";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public Guid UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public ApplicationUser? ConfirmedByUser { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string ParserVersion { get; set; } = "v1.0";
    public string TemplateVersion { get; set; } = "v1.0";

    public ICollection<ImportBatchRow> Rows { get; set; } = new List<ImportBatchRow>();
    public ICollection<ImportConfirmationRequest> ConfirmationRequests { get; set; } = new List<ImportConfirmationRequest>();
}
