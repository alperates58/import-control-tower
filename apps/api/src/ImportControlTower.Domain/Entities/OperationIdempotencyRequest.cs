using System;

namespace ImportControlTower.Domain.Entities;

public class OperationIdempotencyRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequestedByUserId { get; set; }
    public ApplicationUser? RequestedByUser { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string ScopeKey { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public int? ResponseStatusCode { get; set; }
    public string? ResponseJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
