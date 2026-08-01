using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Entities;

namespace ImportControlTower.Application.Services;

public interface IImportCaseService
{
    Task<ImportCaseDetailDto> CreateCaseAsync(CreateImportCaseDto dto, Guid userId, string? correlationId = null);
    Task<PagedResultDto<ImportCaseSummaryDto>> GetCasesAsync(
        int page,
        int pageSize,
        string? search,
        string? supplier,
        string? status,
        string? derivedStatus,
        string? productionStatus,
        Guid? responsibleUserId,
        string? defaultTransportMode,
        DateTime? etdStart,
        DateTime? etdEnd,
        DateTime? etaStart,
        DateTime? etaEnd,
        bool? delayedOnly,
        string? sort);
    Task<ImportCaseDetailDto?> GetCaseByIdAsync(Guid id);
    Task<ImportCaseDetailDto> UpdateCaseAsync(Guid id, UpdateImportCaseDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null);
    Task<ImportCaseDetailDto> CloseCaseAsync(Guid id, Guid userId, string? correlationId = null);
    Task<ImportCaseDetailDto> CancelCaseAsync(Guid id, Guid userId, string? correlationId = null);

    Task<List<AvailablePurchaseOrderLineDto>> GetAvailablePurchaseOrdersAsync(Guid caseId, string? search);
    Task<ImportCaseLineDto> AllocateOrderLineAsync(Guid caseId, AllocateOrderLineDto dto, Guid userId, string? correlationId = null);
    Task<ImportCaseLineDto> UpdateOrderLineAllocationAsync(Guid caseId, Guid lineId, UpdateImportCaseLineDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null);
    Task CancelOrderLineAllocationAsync(Guid caseId, Guid lineId, Guid userId, string? correlationId = null);

    Task<List<SupplierLookupDto>> GetAvailableSuppliersAsync(string? search);
    Task<ImportCaseOperationalSummaryDto> GetOperationalSummaryAsync();
    Task<List<AuditLog>> GetCaseHistoryAsync(Guid id);
}
