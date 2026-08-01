using System;
using System.Collections.Generic;

namespace ImportControlTower.Application.Models;

public record CreateImportCaseDto(
    string Title,
    string SupplierName,
    string? DefaultTransportMode,
    string? OriginCountry,
    string? Incoterm,
    Guid? ResponsibleUserId,
    string? Notes,
    DateTime? EstimatedProductionCompletionDate
);

public record UpdateImportCaseDto(
    string Title,
    string? DefaultTransportMode,
    string? OriginCountry,
    string? Incoterm,
    Guid? ResponsibleUserId,
    Guid? PurchasingOwnerUserId,
    Guid? OperationsOwnerUserId,
    string ProductionStatus,
    DateTime? EstimatedProductionCompletionDate,
    DateTime? ReadyForShipmentDate,
    string? Notes
);

public record ImportCaseSummaryDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Status,
    string DerivedOperationalStatus,
    string SupplierName,
    string? DefaultTransportMode,
    string ProductionStatus,
    string? ResponsibleUserName,
    DateTime? EstimatedProductionCompletionDate,
    DateTime? ReadyForShipmentDate,
    DateTime? MinEtd,
    DateTime? MaxEta,
    bool IsDelayed,
    int LineCount,
    int ShipmentCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record ImportCaseDetailDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Status,
    string DerivedOperationalStatus,
    string SupplierName,
    string? OriginCountry,
    string? DefaultTransportMode,
    string? Incoterm,
    Guid? ResponsibleUserId,
    string? ResponsibleUserName,
    Guid? PurchasingOwnerUserId,
    string? PurchasingOwnerUserName,
    Guid? OperationsOwnerUserId,
    string? OperationsOwnerUserName,
    string ProductionStatus,
    DateTime? EstimatedProductionCompletionDate,
    DateTime? ReadyForShipmentDate,
    string? Notes,
    DateTime? ClosedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint RowVersion,
    List<ImportCaseLineDto> Lines,
    List<ShipmentSummaryDto> Shipments
);

public record AllocateOrderLineDto(
    Guid PurchaseOrderLineId,
    decimal AllocatedQuantity,
    DateTime? PlannedShipmentDate,
    string? Notes
);

public record UpdateImportCaseLineDto(
    decimal AllocatedQuantity,
    DateTime? PlannedShipmentDate,
    string? Notes
);

public record ImportCaseLineDto(
    Guid Id,
    Guid ImportCaseId,
    Guid PurchaseOrderLineId,
    string OrderNumber,
    int LineNumber,
    string StockCode,
    string StockName,
    decimal OrderedQuantity,
    decimal AllocatedQuantity,
    decimal ReleasedQuantity,
    decimal EffectiveAllocatedQuantity,
    decimal ShippedQuantity,
    decimal ReceivedQuantity,
    string Status,
    DateTime? PlannedShipmentDate,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint RowVersion
);

public record AvailablePurchaseOrderLineDto(
    Guid PurchaseOrderLineId,
    Guid PurchaseOrderId,
    string OrderNumber,
    int LineNumber,
    string StockCode,
    string StockName,
    string SupplierName,
    DateTime OrderDate,
    decimal OrderedQuantity,
    decimal RemainingQuantity,
    decimal AllocatedToOtherCases,
    decimal EffectiveAvailableQuantity
);

public record SupplierLookupDto(
    string SupplierName,
    string NormalizedSupplierName,
    int ActiveOrderCount
);

public record ImportCaseOperationalSummaryDto(
    int ActiveCaseCount,
    int ProductionDelayedCount,
    int ReadyForShipmentCount,
    int BookingPendingCount,
    int InTransitShipmentCount,
    int DelayedShipmentCount,
    int EtaThisWeekCount,
    int UnallocatedLineCount
);

public record AbortShipmentDto(
    string Reason,
    string? CorrelationId
);
