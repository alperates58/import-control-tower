using System;
using System.Collections.Generic;

namespace ImportControlTower.Domain.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ImportCaseId { get; set; }
    public ImportCase? ImportCase { get; set; }

    public Guid? ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    public Guid? ShipmentContainerId { get; set; }
    public ShipmentContainer? ShipmentContainer { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}
