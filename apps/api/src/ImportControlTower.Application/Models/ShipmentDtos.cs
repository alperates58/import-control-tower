using System;
using System.Collections.Generic;

namespace ImportControlTower.Application.Models;

public record CreateShipmentDto(
    string TransportMode,
    string OriginLocation,
    string DestinationLocation,
    string OriginTimezoneId,
    string DestinationTimezoneId,
    string? BookingNumber,
    string? ForwarderName,
    string? CarrierName,
    string? TransportReference,
    string? VesselName,
    string? VoyageNumber,
    DateTime? Etd,
    DateTime? Eta,
    string? Notes
);

public record UpdateShipmentDto(
    string TransportMode,
    string OriginLocation,
    string DestinationLocation,
    string OriginTimezoneId,
    string DestinationTimezoneId,
    string? BookingNumber,
    string? ForwarderName,
    string? CarrierName,
    string? TransportReference,
    string? VesselName,
    string? VoyageNumber,
    DateTime? Etd,
    DateTime? Eta,
    DateTime? Atd,
    DateTime? Ata,
    DateTime? EstimatedWarehouseArrival,
    DateTime? ActualWarehouseArrival,
    string Status,
    string? Notes
);

public record ShipmentSummaryDto(
    Guid Id,
    Guid ImportCaseId,
    int ShipmentSequence,
    string ShipmentNumber,
    string TransportMode,
    string OriginLocation,
    string DestinationLocation,
    string OriginTimezoneId,
    string DestinationTimezoneId,
    string? BookingNumber,
    string? ForwarderName,
    string? CarrierName,
    string? TransportReference,
    string? VesselName,
    string? VoyageNumber,
    DateTime? Etd,
    DateTime? Eta,
    DateTime? Atd,
    DateTime? Ata,
    string Status,
    int ContainerCount,
    int LineAllocationCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record ShipmentDetailDto(
    Guid Id,
    Guid ImportCaseId,
    string CaseNumber,
    int ShipmentSequence,
    string ShipmentNumber,
    string TransportMode,
    string OriginLocation,
    string DestinationLocation,
    string OriginTimezoneId,
    string DestinationTimezoneId,
    string? BookingNumber,
    string? ForwarderName,
    string? CarrierName,
    string? TransportReference,
    string? VesselName,
    string? VoyageNumber,
    DateTime? Etd,
    DateTime? Eta,
    DateTime? Atd,
    DateTime? Ata,
    DateTime? EstimatedWarehouseArrival,
    DateTime? ActualWarehouseArrival,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint RowVersion,
    List<ShipmentLineAllocationDto> LineAllocations,
    List<ShipmentContainerDto> Containers,
    List<ShipmentMilestoneDto> Milestones
);

public record AllocateShipmentLineDto(
    Guid ImportCaseLineId,
    decimal AllocatedQuantity
);

public record UpdateShipmentLineAllocationDto(
    decimal AllocatedQuantity
);

public record ShipmentLineAllocationDto(
    Guid Id,
    Guid ShipmentId,
    Guid ImportCaseLineId,
    Guid ImportCaseId,
    string StockCode,
    string StockName,
    decimal CaseAllocatedQuantity,
    decimal AllocatedQuantity,
    decimal ReleasedQuantity,
    decimal EffectiveAllocatedQuantity,
    decimal ShippedQuantity,
    decimal ReceivedQuantity,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint RowVersion
);

public record AddContainerDto(
    string ContainerNumber,
    string ContainerType,
    string? SealNumber,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    int? PackageCount,
    bool OverrideCheckDigit = false,
    string? OverrideReason = null,
    string? Notes = null
);

public record UpdateContainerDto(
    string ContainerType,
    string? SealNumber,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    int? PackageCount,
    string Status,
    string? Notes
);

public record ShipmentContainerDto(
    Guid Id,
    Guid ShipmentId,
    string ContainerNumber,
    string NormalizedContainerNumber,
    string ContainerType,
    string? SealNumber,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    int? PackageCount,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint RowVersion
);

public record CreateMilestoneDto(
    int SequenceNumber,
    string MilestoneType,
    string TimezoneId,
    string? LocationName,
    DateTime? PlannedAt,
    DateTime? EstimatedAt,
    DateTime? ActualAt,
    string Status,
    string? Notes
);

public record UpdateMilestoneDto(
    int SequenceNumber,
    string TimezoneId,
    string? LocationName,
    DateTime? PlannedAt,
    DateTime? EstimatedAt,
    DateTime? ActualAt,
    string Status,
    string? Notes
);

public record ShipmentMilestoneDto(
    Guid Id,
    Guid ShipmentId,
    int SequenceNumber,
    string MilestoneType,
    string LocationName,
    string TimezoneId,
    DateTime? PlannedAtUtc,
    DateTime? EstimatedAtUtc,
    DateTime? ActualAtUtc,
    string Status,
    string Source,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint RowVersion
);
