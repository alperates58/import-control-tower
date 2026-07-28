using System;

namespace ImportControlTower.Domain.Entities;

public class ImportConfirmationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public int? ResponseStatusCode { get; set; }
    public string? ResponseJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
