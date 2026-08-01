using System;
using System.Collections.Generic;

namespace ImportControlTower.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ImportCaseId { get; set; }
    public ImportCase? ImportCase { get; set; }
    public int ShipmentSequence { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public string TransportMode { get; set; } = "Sea";
    public string? BookingNumber { get; set; }
    public string OriginLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string? ForwarderName { get; set; }
    public string? CarrierName { get; set; }
    public string? TransportReference { get; set; }
    public string? VesselName { get; set; }
    public string? VoyageNumber { get; set; }
    public string OriginTimezoneId { get; set; } = "UTC";
    public string DestinationTimezoneId { get; set; } = "UTC";
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public DateTime? Atd { get; set; }
    public DateTime? Ata { get; set; }
    public DateTime? EstimatedWarehouseArrival { get; set; }
    public DateTime? ActualWarehouseArrival { get; set; }
    public string? ModeSpecificMetadata { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<ShipmentLineAllocation> LineAllocations { get; set; } = new List<ShipmentLineAllocation>();
    public ICollection<ShipmentContainer> Containers { get; set; } = new List<ShipmentContainer>();
    public ICollection<ShipmentMilestone> Milestones { get; set; } = new List<ShipmentMilestone>();
}
