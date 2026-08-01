using System;
using System.Collections.Generic;

namespace ImportControlTower.Domain.Entities;

public class ImportCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string SupplierName { get; set; } = string.Empty;
    public string NormalizedSupplierName { get; set; } = string.Empty;
    public string? OriginCountry { get; set; }
    public string? DefaultTransportMode { get; set; }
    public string? Incoterm { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public ApplicationUser? ResponsibleUser { get; set; }
    public Guid? PurchasingOwnerUserId { get; set; }
    public ApplicationUser? PurchasingOwnerUser { get; set; }
    public Guid? OperationsOwnerUserId { get; set; }
    public ApplicationUser? OperationsOwnerUser { get; set; }
    public string? Notes { get; set; }
    public string ProductionStatus { get; set; } = "NotStarted";
    public DateTime? EstimatedProductionCompletionDate { get; set; }
    public DateTime? ReadyForShipmentDate { get; set; }
    public int LastShipmentSequence { get; set; } = 0;
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<ImportCaseLine> Lines { get; set; } = new List<ImportCaseLine>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
