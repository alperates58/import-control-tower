using System;
using System.Collections.Generic;

namespace ImportControlTower.Application.Models;

public record ImportBatchSummaryDto(
    Guid Id,
    string OriginalFileName,
    string FileSha256,
    long FileSizeBytes,
    int TotalRowCount,
    int ValidRowCount,
    int InvalidRowCount,
    int WarningRowCount,
    int ImportedOrderCount,
    int ImportedLineCount,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    Guid UploadedByUserId,
    string? UploadedByFullName,
    Guid? ConfirmedByUserId,
    string? ConfirmedByFullName,
    string CorrelationId,
    string? FailureReason
);

public record ImportBatchRowDto(
    Guid Id,
    Guid ImportBatchId,
    int RowNumber,
    string RawDataJson,
    string? NormalizedDataJson,
    string ValidationStatus,
    List<string> ErrorCodes,
    List<string> WarningCodes,
    Guid? MatchedOrderId,
    Guid? MatchedLineId,
    string ImportAction,
    DateTime CreatedAtUtc
);

public record ImportBatchDetailDto(
    ImportBatchSummaryDto Batch,
    Dictionary<string, string> ColumnMapping,
    List<string> UnmappedColumns,
    List<string> MissingRequiredColumns
);

public record UpdateColumnMappingRequestDto(
    Dictionary<string, string> ColumnMapping
);

public record ConfirmImportResponseDto(
    Guid BatchId,
    string Status,
    int ImportedOrderCount,
    int ImportedLineCount,
    int SkippedDuplicateCount,
    DateTime CompletedAtUtc
);

public record PurchaseOrderDto(
    Guid Id,
    string OrderNumber,
    string SupplierName,
    DateTime OrderDate,
    string Status,
    string Source,
    int LineCount,
    decimal TotalOrderedQuantity,
    decimal TotalRemainingQuantity,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<PurchaseOrderLineDto> Lines
);

public record PurchaseOrderLineDto(
    Guid Id,
    Guid PurchaseOrderId,
    int LineNumber,
    string StockCode,
    string StockName,
    decimal OrderedQuantity,
    decimal RemainingQuantity,
    DateTime? SasDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record PagedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
