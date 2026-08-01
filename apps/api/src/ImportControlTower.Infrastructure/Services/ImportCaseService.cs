using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.Models;
using ImportControlTower.Application.Services;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Domain.Enums;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Infrastructure.Services;

public class ImportCaseService : IImportCaseService
{
    private readonly ApplicationDbContext _db;
    private readonly IDocumentNumberGenerator _numberGenerator;
    private readonly IAuditLogService _auditLog;

    public ImportCaseService(
        ApplicationDbContext db,
        IDocumentNumberGenerator numberGenerator,
        IAuditLogService auditLog)
    {
        _db = db;
        _numberGenerator = numberGenerator;
        _auditLog = auditLog;
    }

    public async Task<ImportCaseDetailDto> CreateCaseAsync(CreateImportCaseDto dto, Guid userId, string? correlationId = null)
    {
        var normSupplier = dto.SupplierName.Trim().ToUpperInvariant();

        var poSupplier = await _db.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.NormalizedSupplierName == normSupplier || p.SupplierName.ToUpper() == normSupplier);

        string supplierName = poSupplier?.SupplierName ?? dto.SupplierName.Trim();

        using var tx = await _db.Database.BeginTransactionAsync();

        var year = DateTime.UtcNow.Year;
        var caseNumber = await _numberGenerator.GenerateCaseNumberAsync(_db, year);

        var importCase = new ImportCase
        {
            Id = Guid.NewGuid(),
            CaseNumber = caseNumber,
            Title = dto.Title.Trim(),
            Status = ImportCaseStatus.Draft,
            SupplierName = supplierName,
            NormalizedSupplierName = normSupplier,
            OriginCountry = dto.OriginCountry?.Trim(),
            DefaultTransportMode = dto.DefaultTransportMode?.Trim(),
            Incoterm = dto.Incoterm?.Trim(),
            ResponsibleUserId = dto.ResponsibleUserId,
            Notes = dto.Notes?.Trim(),
            ProductionStatus = ProductionStatus.NotStarted,
            EstimatedProductionCompletionDate = dto.EstimatedProductionCompletionDate,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _db.ImportCases.Add(importCase);
        await _db.SaveChangesAsync();

        _db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ImportCaseId = importCase.Id,
            EntityType = "ImportCase",
            OldStatus = null,
            NewStatus = ImportCaseStatus.Draft,
            Reason = "İthalat dosyası oluşturuldu.",
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = userId
        });
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "ImportCase.Created",
            entityType: "ImportCase",
            entityId: importCase.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { importCase.CaseNumber, importCase.SupplierName, importCase.Title }
        );

        return (await GetCaseByIdAsync(importCase.Id))!;
    }

    public async Task<PagedResultDto<ImportCaseSummaryDto>> GetCasesAsync(
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
        string? sort)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.ImportCases
            .Include(c => c.ResponsibleUser)
            .Include(c => c.Lines)
            .Include(c => c.Shipments)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(productionStatus))
        {
            query = query.Where(c => c.ProductionStatus == productionStatus);
        }

        if (responsibleUserId.HasValue)
        {
            query = query.Where(c => c.ResponsibleUserId == responsibleUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(defaultTransportMode))
        {
            query = query.Where(c => c.DefaultTransportMode == defaultTransportMode);
        }

        if (!string.IsNullOrWhiteSpace(supplier))
        {
            var normSup = supplier.Trim().ToUpperInvariant();
            query = query.Where(c => c.NormalizedSupplierName.Contains(normSup));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normSearch = search.Trim().ToUpperInvariant();
            query = query.Where(c => 
                c.CaseNumber.Contains(normSearch) || 
                c.Title.ToUpper().Contains(normSearch) || 
                c.NormalizedSupplierName.Contains(normSearch));
        }

        if (etdStart.HasValue)
        {
            query = query.Where(c => c.Shipments.Any(s => s.Etd >= etdStart.Value));
        }

        if (etdEnd.HasValue)
        {
            query = query.Where(c => c.Shipments.Any(s => s.Etd <= etdEnd.Value));
        }

        if (etaStart.HasValue)
        {
            query = query.Where(c => c.Shipments.Any(s => s.Eta >= etaStart.Value));
        }

        if (etaEnd.HasValue)
        {
            query = query.Where(c => c.Shipments.Any(s => s.Eta <= etaEnd.Value));
        }

        var casesList = await query.ToListAsync();
        var now = DateTime.UtcNow;

        var items = casesList.Select(c =>
        {
            var activeShipments = c.Shipments.Where(s => s.Status != ShipmentStatus.Cancelled).ToList();
            
            string computedDerivedStatus = c.Status;
            if (c.Status == ImportCaseStatus.Active)
            {
                if (activeShipments.Any(s => s.Status == ShipmentStatus.InTransit))
                {
                    computedDerivedStatus = ShipmentStatus.InTransit;
                }
                else if (activeShipments.Any(s => s.Status == ShipmentStatus.Arrived))
                {
                    computedDerivedStatus = ShipmentStatus.Arrived;
                }
                else if (activeShipments.Any(s => s.Status == ShipmentStatus.Delivered))
                {
                    computedDerivedStatus = ShipmentStatus.Delivered;
                }
                else if (activeShipments.Any(s => s.Status == ShipmentStatus.Booked || s.Status == ShipmentStatus.Loading))
                {
                    computedDerivedStatus = ShipmentStatus.Booked;
                }
                else if (c.ProductionStatus == ProductionStatus.ReadyForShipment)
                {
                    computedDerivedStatus = ProductionStatus.ReadyForShipment;
                }
                else if (c.ProductionStatus == ProductionStatus.Started || c.ProductionStatus == ProductionStatus.Delayed)
                {
                    computedDerivedStatus = "Production";
                }
            }

            DateTime? minEtd = activeShipments.Where(s => s.Etd.HasValue).Min(s => s.Etd);
            DateTime? maxEta = activeShipments.Where(s => s.Eta.HasValue).Max(s => s.Eta);

            bool isDelayed = false;
            if (c.ProductionStatus == ProductionStatus.Delayed) isDelayed = true;
            if (c.EstimatedProductionCompletionDate.HasValue && c.EstimatedProductionCompletionDate.Value < now.Date && c.ProductionStatus != ProductionStatus.Completed && c.ProductionStatus != ProductionStatus.ReadyForShipment) isDelayed = true;
            if (activeShipments.Any(s => s.Etd.HasValue && s.Atd == null && s.Etd.Value < now)) isDelayed = true;
            if (activeShipments.Any(s => s.Eta.HasValue && s.Ata == null && s.Eta.Value < now)) isDelayed = true;

            return new
            {
                Case = c,
                DerivedStatus = computedDerivedStatus,
                MinEtd = minEtd,
                MaxEta = maxEta,
                IsDelayed = isDelayed
            };
        });

        if (!string.IsNullOrWhiteSpace(derivedStatus))
        {
            items = items.Where(x => x.DerivedStatus.Equals(derivedStatus, StringComparison.OrdinalIgnoreCase));
        }

        if (delayedOnly == true)
        {
            items = items.Where(x => x.IsDelayed);
        }

        items = sort?.ToLowerInvariant() switch
        {
            "etd" => items.OrderBy(x => x.MinEtd ?? DateTime.MaxValue),
            "eta" => items.OrderBy(x => x.MaxEta ?? DateTime.MaxValue),
            "casenumber" => items.OrderBy(x => x.Case.CaseNumber),
            "status" => items.OrderBy(x => x.Case.Status),
            "updatedat" => items.OrderByDescending(x => x.Case.UpdatedAtUtc),
            _ => items.OrderByDescending(x => x.Case.CreatedAtUtc)
        };

        var totalCount = items.Count();
        var pagedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ImportCaseSummaryDto(
                x.Case.Id,
                x.Case.CaseNumber,
                x.Case.Title,
                x.Case.Status,
                x.DerivedStatus,
                x.Case.SupplierName,
                x.Case.DefaultTransportMode,
                x.Case.ProductionStatus,
                x.Case.ResponsibleUser?.FullName,
                x.Case.EstimatedProductionCompletionDate,
                x.Case.ReadyForShipmentDate,
                x.MinEtd,
                x.MaxEta,
                x.IsDelayed,
                x.Case.Lines.Count(l => l.Status != ImportCaseLineStatus.Cancelled),
                x.Case.Shipments.Count(s => s.Status != ShipmentStatus.Cancelled),
                x.Case.CreatedAtUtc,
                x.Case.UpdatedAtUtc
            ))
            .ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        return new PagedResultDto<ImportCaseSummaryDto>(pagedItems, totalCount, page, pageSize, totalPages);
    }

    public async Task<ImportCaseDetailDto?> GetCaseByIdAsync(Guid id)
    {
        var c = await _db.ImportCases
            .Include(x => x.ResponsibleUser)
            .Include(x => x.PurchasingOwnerUser)
            .Include(x => x.OperationsOwnerUser)
            .Include(x => x.Lines).ThenInclude(l => l.PurchaseOrderLine).ThenInclude(pol => pol!.PurchaseOrder)
            .Include(x => x.Lines).ThenInclude(l => l.ShipmentAllocations)
            .Include(x => x.Shipments).ThenInclude(s => s.Containers)
            .Include(x => x.Shipments).ThenInclude(s => s.Milestones)
            .Include(x => x.Shipments).ThenInclude(s => s.LineAllocations)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return null;

        var activeShipments = c.Shipments.Where(s => s.Status != ShipmentStatus.Cancelled).ToList();
        string computedDerivedStatus = c.Status;
        if (c.Status == ImportCaseStatus.Active)
        {
            if (activeShipments.Any(s => s.Status == ShipmentStatus.InTransit)) computedDerivedStatus = ShipmentStatus.InTransit;
            else if (activeShipments.Any(s => s.Status == ShipmentStatus.Arrived)) computedDerivedStatus = ShipmentStatus.Arrived;
            else if (activeShipments.Any(s => s.Status == ShipmentStatus.Delivered)) computedDerivedStatus = ShipmentStatus.Delivered;
            else if (activeShipments.Any(s => s.Status == ShipmentStatus.Booked || s.Status == ShipmentStatus.Loading)) computedDerivedStatus = ShipmentStatus.Booked;
            else if (c.ProductionStatus == ProductionStatus.ReadyForShipment) computedDerivedStatus = ProductionStatus.ReadyForShipment;
            else if (c.ProductionStatus == ProductionStatus.Started || c.ProductionStatus == ProductionStatus.Delayed) computedDerivedStatus = "Production";
        }

        var linesDto = c.Lines
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l =>
            {
                var rowVersion = _db.Entry(l).Property<uint>("xmin").CurrentValue;
                var activeShipmentAllocations = l.ShipmentAllocations
                    .Where(sa => sa.Status != ShipmentLineAllocationStatus.Cancelled)
                    .ToList();

                decimal shippedQty = activeShipmentAllocations.Sum(sa => sa.ShippedQuantity);
                decimal receivedQty = activeShipmentAllocations.Sum(sa => sa.ReceivedQuantity);

                return new ImportCaseLineDto(
                    l.Id,
                    l.ImportCaseId,
                    l.PurchaseOrderLineId,
                    l.PurchaseOrderLine?.PurchaseOrder?.OrderNumber ?? "",
                    l.PurchaseOrderLine?.LineNumber ?? 0,
                    l.PurchaseOrderLine?.StockCode ?? "",
                    l.PurchaseOrderLine?.StockName ?? "",
                    l.PurchaseOrderLine?.OrderedQuantity ?? 0,
                    l.AllocatedQuantity,
                    l.ReleasedQuantity,
                    l.EffectiveAllocatedQuantity,
                    shippedQty,
                    receivedQty,
                    l.Status,
                    l.PlannedShipmentDate,
                    l.Notes,
                    l.CreatedAtUtc,
                    l.UpdatedAtUtc,
                    rowVersion
                );
            }).ToList();

        var shipmentsDto = c.Shipments
            .OrderBy(s => s.ShipmentSequence)
            .Select(s => new ShipmentSummaryDto(
                s.Id,
                s.ImportCaseId,
                s.ShipmentSequence,
                s.ShipmentNumber,
                s.TransportMode,
                s.OriginLocation,
                s.DestinationLocation,
                s.OriginTimezoneId,
                s.DestinationTimezoneId,
                s.BookingNumber,
                s.ForwarderName,
                s.CarrierName,
                s.TransportReference,
                s.VesselName,
                s.VoyageNumber,
                s.Etd,
                s.Eta,
                s.Atd,
                s.Ata,
                s.Status,
                s.Containers.Count(cont => cont.Status != ContainerStatus.Cancelled),
                s.LineAllocations.Count(la => la.Status != ShipmentLineAllocationStatus.Cancelled),
                s.CreatedAtUtc,
                s.UpdatedAtUtc
            )).ToList();

        var caseRowVersion = _db.Entry(c).Property<uint>("xmin").CurrentValue;

        return new ImportCaseDetailDto(
            c.Id,
            c.CaseNumber,
            c.Title,
            c.Status,
            computedDerivedStatus,
            c.SupplierName,
            c.OriginCountry,
            c.DefaultTransportMode,
            c.Incoterm,
            c.ResponsibleUserId,
            c.ResponsibleUser?.FullName,
            c.PurchasingOwnerUserId,
            c.PurchasingOwnerUser?.FullName,
            c.OperationsOwnerUserId,
            c.OperationsOwnerUser?.FullName,
            c.ProductionStatus,
            c.EstimatedProductionCompletionDate,
            c.ReadyForShipmentDate,
            c.Notes,
            c.ClosedAtUtc,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            caseRowVersion,
            linesDto,
            shipmentsDto
        );
    }

    public async Task<ImportCaseDetailDto> UpdateCaseAsync(Guid id, UpdateImportCaseDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null)
    {
        var importCase = await _db.ImportCases.FirstOrDefaultAsync(c => c.Id == id);
        if (importCase == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_NOT_FOUND: İthalat dosyası bulunamadı.");
        }

        if (importCase.Status == ImportCaseStatus.Closed || importCase.Status == ImportCaseStatus.Cancelled)
        {
            throw new InvalidOperationException("IMPORT_CASE_ALREADY_CLOSED: Kapanmış veya iptal edilmiş dosya güncellenemez.");
        }

        var currentRowVersion = _db.Entry(importCase).Property<uint>("xmin").CurrentValue;
        if (currentRowVersion != expectedRowVersion)
        {
            throw new DbUpdateConcurrencyException("CONCURRENCY_CONFLICT: Kayıt başka bir kullanıcı tarafından güncellenmiştir.");
        }

        importCase.Title = dto.Title.Trim();
        importCase.DefaultTransportMode = dto.DefaultTransportMode?.Trim();
        importCase.OriginCountry = dto.OriginCountry?.Trim();
        importCase.Incoterm = dto.Incoterm?.Trim();
        importCase.ResponsibleUserId = dto.ResponsibleUserId;
        importCase.PurchasingOwnerUserId = dto.PurchasingOwnerUserId;
        importCase.OperationsOwnerUserId = dto.OperationsOwnerUserId;
        importCase.ProductionStatus = dto.ProductionStatus;
        importCase.EstimatedProductionCompletionDate = dto.EstimatedProductionCompletionDate;
        importCase.ReadyForShipmentDate = dto.ReadyForShipmentDate;
        importCase.Notes = dto.Notes?.Trim();
        importCase.UpdatedAtUtc = DateTime.UtcNow;
        importCase.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "ImportCase.Updated",
            entityType: "ImportCase",
            entityId: importCase.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { importCase.CaseNumber, importCase.ProductionStatus }
        );

        return (await GetCaseByIdAsync(importCase.Id))!;
    }

    public async Task<ImportCaseDetailDto> CloseCaseAsync(Guid id, Guid userId, string? correlationId = null)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var importCase = await _db.ImportCases
            .Include(c => c.Shipments)
            .Include(c => c.Lines).ThenInclude(l => l.ShipmentAllocations)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (importCase == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_NOT_FOUND: İthalat dosyası bulunamadı.");
        }

        if (importCase.Status == ImportCaseStatus.Closed)
        {
            throw new InvalidOperationException("IMPORT_CASE_ALREADY_CLOSED: Dosya zaten kapatılmış.");
        }

        var nonTerminalShipments = importCase.Shipments
            .Where(s => s.Status != ShipmentStatus.Delivered &&
                        s.Status != ShipmentStatus.Cancelled &&
                        s.Status != ShipmentStatus.Aborted)
            .ToList();

        if (nonTerminalShipments.Any())
        {
            throw new InvalidOperationException("IMPORT_CASE_CLOSE_CONDITIONS_NOT_MET: Teslim edilmemiş veya devam eden sevkiyatlar bulunduğundan dosya kapatılamaz.");
        }

        foreach (var line in importCase.Lines.Where(l => l.Status != ImportCaseLineStatus.Cancelled))
        {
            var activeAllocations = line.ShipmentAllocations
                .Where(sa => sa.Status != ShipmentLineAllocationStatus.Cancelled)
                .ToList();

            foreach (var sa in activeAllocations)
            {
                sa.ReleasedQuantity = sa.AllocatedQuantity - sa.ShippedQuantity;
                sa.UpdatedAtUtc = DateTime.UtcNow;
                sa.UpdatedByUserId = userId;
            }

            decimal totalShippedQty = activeAllocations.Sum(sa => sa.ShippedQuantity);

            line.ReleasedQuantity = line.AllocatedQuantity - totalShippedQty;
            if (totalShippedQty == 0)
            {
                line.Status = ImportCaseLineStatus.Cancelled;
            }
            else
            {
                line.Status = ImportCaseLineStatus.FullyShipped;
            }
            line.UpdatedAtUtc = DateTime.UtcNow;
            line.UpdatedByUserId = userId;
        }

        string oldStatus = importCase.Status;
        importCase.Status = ImportCaseStatus.Closed;
        importCase.ClosedAtUtc = DateTime.UtcNow;
        importCase.UpdatedAtUtc = DateTime.UtcNow;
        importCase.UpdatedByUserId = userId;

        _db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ImportCaseId = importCase.Id,
            EntityType = "ImportCase",
            OldStatus = oldStatus,
            NewStatus = ImportCaseStatus.Closed,
            Reason = "İthalat dosyası kapatıldı.",
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = userId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "ImportCase.Closed",
            entityType: "ImportCase",
            entityId: importCase.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { importCase.CaseNumber }
        );

        return (await GetCaseByIdAsync(importCase.Id))!;
    }

    public async Task<ImportCaseDetailDto> CancelCaseAsync(Guid id, Guid userId, string? correlationId = null)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var importCase = await _db.ImportCases
            .Include(c => c.Shipments)
            .Include(c => c.Lines).ThenInclude(l => l.ShipmentAllocations)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (importCase == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_NOT_FOUND: İthalat dosyası bulunamadı.");
        }

        if (importCase.Shipments.Any(s => s.Status == ShipmentStatus.InTransit || s.Status == ShipmentStatus.Arrived))
        {
            throw new InvalidOperationException("IMPORT_CASE_CANCEL_NOT_ALLOWED: Yolda veya varış yapmış sevkiyatı bulunan dosya iptal edilemez.");
        }

        foreach (var shipment in importCase.Shipments.Where(s => s.Status != ShipmentStatus.Cancelled && s.Status != ShipmentStatus.Aborted))
        {
            shipment.Status = ShipmentStatus.Cancelled;
            shipment.UpdatedAtUtc = DateTime.UtcNow;
            shipment.UpdatedByUserId = userId;
        }

        foreach (var line in importCase.Lines.Where(l => l.Status != ImportCaseLineStatus.Cancelled))
        {
            var activeAllocations = line.ShipmentAllocations
                .Where(sa => sa.Status != ShipmentLineAllocationStatus.Cancelled)
                .ToList();

            foreach (var sa in activeAllocations)
            {
                sa.ReleasedQuantity = sa.AllocatedQuantity - sa.ShippedQuantity;
                sa.Status = ShipmentLineAllocationStatus.Cancelled;
                sa.UpdatedAtUtc = DateTime.UtcNow;
                sa.UpdatedByUserId = userId;
            }

            decimal totalShippedQty = activeAllocations.Sum(sa => sa.ShippedQuantity);
            line.ReleasedQuantity = line.AllocatedQuantity - totalShippedQty;
            line.Status = ImportCaseLineStatus.Cancelled;
            line.UpdatedAtUtc = DateTime.UtcNow;
            line.UpdatedByUserId = userId;
        }

        string oldStatus = importCase.Status;
        importCase.Status = ImportCaseStatus.Cancelled;
        importCase.UpdatedAtUtc = DateTime.UtcNow;
        importCase.UpdatedByUserId = userId;

        _db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ImportCaseId = importCase.Id,
            EntityType = "ImportCase",
            OldStatus = oldStatus,
            NewStatus = ImportCaseStatus.Cancelled,
            Reason = "İthalat dosyası iptal edildi.",
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = userId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "ImportCase.Cancelled",
            entityType: "ImportCase",
            entityId: importCase.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { importCase.CaseNumber }
        );

        return (await GetCaseByIdAsync(importCase.Id))!;
    }

    public async Task<List<AvailablePurchaseOrderLineDto>> GetAvailablePurchaseOrdersAsync(Guid caseId, string? search)
    {
        var importCase = await _db.ImportCases.AsNoTracking().FirstOrDefaultAsync(c => c.Id == caseId);
        if (importCase == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_NOT_FOUND: İthalat dosyası bulunamadı.");
        }

        var normSupplier = importCase.NormalizedSupplierName;

        var query = _db.PurchaseOrderLines
            .Include(pol => pol.PurchaseOrder)
            .Where(pol => pol.PurchaseOrder!.NormalizedSupplierName == normSupplier && pol.RemainingQuantity > 0)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normSearch = search.Trim().ToUpperInvariant();
            query = query.Where(pol => 
                pol.PurchaseOrder!.NormalizedOrderNumber.Contains(normSearch) ||
                pol.NormalizedStockCode.Contains(normSearch) ||
                pol.StockName.ToUpper().Contains(normSearch));
        }

        var lines = await query.ToListAsync();
        var lineIds = lines.Select(l => l.Id).ToList();

        var activeCaseAllocations = await _db.ImportCaseLines
            .Where(icl => lineIds.Contains(icl.PurchaseOrderLineId) && icl.Status != ImportCaseLineStatus.Cancelled)
            .AsNoTracking()
            .ToListAsync();

        var result = new List<AvailablePurchaseOrderLineDto>();
        foreach (var pol in lines)
        {
            decimal activeEffectiveAllocated = activeCaseAllocations
                .Where(icl => icl.PurchaseOrderLineId == pol.Id)
                .Sum(icl => icl.EffectiveAllocatedQuantity);

            decimal effectiveAvailable = pol.RemainingQuantity - activeEffectiveAllocated;
            if (effectiveAvailable > 0)
            {
                result.Add(new AvailablePurchaseOrderLineDto(
                    pol.Id,
                    pol.PurchaseOrderId,
                    pol.PurchaseOrder!.OrderNumber,
                    pol.LineNumber,
                    pol.StockCode,
                    pol.StockName,
                    pol.PurchaseOrder.SupplierName,
                    pol.PurchaseOrder.OrderDate,
                    pol.OrderedQuantity,
                    pol.RemainingQuantity,
                    activeEffectiveAllocated,
                    effectiveAvailable
                ));
            }
        }

        return result;
    }

    public async Task<ImportCaseLineDto> AllocateOrderLineAsync(Guid caseId, AllocateOrderLineDto dto, Guid userId, string? correlationId = null)
    {
        if (dto.AllocatedQuantity <= 0)
        {
            throw new ArgumentException("ALLOCATED_QUANTITY_INVALID: Tahsis miktarı 0'dan büyük olmalıdır.");
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var importCase = await _db.ImportCases.FirstOrDefaultAsync(c => c.Id == caseId);
        if (importCase == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_NOT_FOUND: İthalat dosyası bulunamadı.");
        }

        if (importCase.Status == ImportCaseStatus.Closed || importCase.Status == ImportCaseStatus.Cancelled)
        {
            throw new InvalidOperationException("IMPORT_CASE_ALREADY_CLOSED: Kapanmış dosyaya sipariş eklenemez.");
        }

        var poLine = await _db.PurchaseOrderLines
            .FromSqlInterpolated($"SELECT *, xmin FROM purchase_order_lines WHERE \"Id\" = {dto.PurchaseOrderLineId} FOR UPDATE")
            .Include(pol => pol.PurchaseOrder)
            .FirstOrDefaultAsync();

        if (poLine == null)
        {
            throw new KeyNotFoundException("PURCHASE_ORDER_LINE_NOT_FOUND: Sipariş kalemi bulunamadı.");
        }

        if (poLine.PurchaseOrder!.NormalizedSupplierName != importCase.NormalizedSupplierName)
        {
            throw new InvalidOperationException($"PURCHASE_ORDER_SUPPLIER_MISMATCH: Sipariş kalemi tedarikçisi ({poLine.PurchaseOrder.SupplierName}) ile dosya tedarikçisi ({importCase.SupplierName}) eşleşmiyor.");
        }

        var existingCaseLine = await _db.ImportCaseLines
            .FirstOrDefaultAsync(l => l.ImportCaseId == caseId && l.PurchaseOrderLineId == dto.PurchaseOrderLineId);

        if (existingCaseLine != null && existingCaseLine.Status != ImportCaseLineStatus.Cancelled)
        {
            throw new InvalidOperationException("PURCHASE_ORDER_LINE_ALREADY_ASSIGNED: Bu sipariş kalemi dosyaya zaten eklenmiş.");
        }

        var otherActiveAllocationsSum = await _db.ImportCaseLines
            .Where(l => l.PurchaseOrderLineId == dto.PurchaseOrderLineId && l.Status != ImportCaseLineStatus.Cancelled)
            .SumAsync(l => l.AllocatedQuantity - l.ReleasedQuantity);

        if ((otherActiveAllocationsSum + dto.AllocatedQuantity) > poLine.RemainingQuantity)
        {
            decimal available = poLine.RemainingQuantity - otherActiveAllocationsSum;
            throw new InvalidOperationException($"PURCHASE_ORDER_LINE_ALLOCATION_EXCEEDED: Tahsis miktarı ({dto.AllocatedQuantity}) kalan kullanılabilir bakiyeyi ({available}) aşıyor.");
        }

        ImportCaseLine line;
        if (existingCaseLine != null)
        {
            line = existingCaseLine;
            line.AllocatedQuantity = dto.AllocatedQuantity;
            line.ReleasedQuantity = 0;
            line.Status = ImportCaseLineStatus.Allocated;
            line.PlannedShipmentDate = dto.PlannedShipmentDate;
            line.Notes = dto.Notes?.Trim();
            line.UpdatedAtUtc = DateTime.UtcNow;
            line.UpdatedByUserId = userId;
        }
        else
        {
            line = new ImportCaseLine
            {
                Id = Guid.NewGuid(),
                ImportCaseId = caseId,
                PurchaseOrderLineId = dto.PurchaseOrderLineId,
                AllocatedQuantity = dto.AllocatedQuantity,
                ReleasedQuantity = 0,
                Status = ImportCaseLineStatus.Allocated,
                PlannedShipmentDate = dto.PlannedShipmentDate,
                Notes = dto.Notes?.Trim(),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = userId
            };
            _db.ImportCaseLines.Add(line);
        }

        if (importCase.Status == ImportCaseStatus.Draft)
        {
            importCase.Status = ImportCaseStatus.Active;
            importCase.UpdatedAtUtc = DateTime.UtcNow;
            importCase.UpdatedByUserId = userId;
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "PurchaseOrderLine.Allocated",
            entityType: "ImportCaseLine",
            entityId: line.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { caseId, dto.PurchaseOrderLineId, dto.AllocatedQuantity }
        );

        var caseDetail = await GetCaseByIdAsync(caseId);
        return caseDetail!.Lines.First(l => l.Id == line.Id);
    }

    public async Task<ImportCaseLineDto> UpdateOrderLineAllocationAsync(Guid caseId, Guid lineId, UpdateImportCaseLineDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null)
    {
        if (dto.AllocatedQuantity <= 0)
        {
            throw new ArgumentException("ALLOCATED_QUANTITY_INVALID: Tahsis miktarı 0'dan büyük olmalıdır.");
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var line = await _db.ImportCaseLines
            .Include(l => l.ShipmentAllocations)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.ImportCaseId == caseId);

        if (line == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_LINE_NOT_FOUND: Dosya sipariş kalemi bulunamadı.");
        }

        var currentRowVersion = _db.Entry(line).Property<uint>("xmin").CurrentValue;
        if (currentRowVersion != expectedRowVersion)
        {
            throw new DbUpdateConcurrencyException("CONCURRENCY_CONFLICT: Kayıt başka bir kullanıcı tarafından güncellenmiştir.");
        }

        var poLine = await _db.PurchaseOrderLines
            .FromSqlInterpolated($"SELECT *, xmin FROM purchase_order_lines WHERE \"Id\" = {line.PurchaseOrderLineId} FOR UPDATE")
            .FirstOrDefaultAsync();

        decimal totalShippedQty = line.ShipmentAllocations
            .Where(sa => sa.Status != ShipmentLineAllocationStatus.Cancelled)
            .Sum(sa => sa.ShippedQuantity);

        if (dto.AllocatedQuantity < totalShippedQty)
        {
            throw new InvalidOperationException($"ALLOCATION_CANNOT_BE_REMOVED_AFTER_SHIPMENT: Yeni tahsis miktarı ({dto.AllocatedQuantity}) sevk edilmiş miktardan ({totalShippedQty}) az olamaz.");
        }

        var otherActiveAllocationsSum = await _db.ImportCaseLines
            .Where(l => l.PurchaseOrderLineId == line.PurchaseOrderLineId && l.Id != lineId && l.Status != ImportCaseLineStatus.Cancelled)
            .SumAsync(l => l.AllocatedQuantity - l.ReleasedQuantity);

        if ((otherActiveAllocationsSum + dto.AllocatedQuantity) > poLine!.RemainingQuantity)
        {
            decimal available = poLine.RemainingQuantity - otherActiveAllocationsSum;
            throw new InvalidOperationException($"PURCHASE_ORDER_LINE_ALLOCATION_EXCEEDED: Yeni miktar ({dto.AllocatedQuantity}) kalan bakiyeyi ({available}) aşıyor.");
        }

        line.AllocatedQuantity = dto.AllocatedQuantity;
        line.PlannedShipmentDate = dto.PlannedShipmentDate;
        line.Notes = dto.Notes?.Trim();
        line.UpdatedAtUtc = DateTime.UtcNow;
        line.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "PurchaseOrderLine.AllocationUpdated",
            entityType: "ImportCaseLine",
            entityId: line.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { caseId, lineId, dto.AllocatedQuantity }
        );

        var caseDetail = await GetCaseByIdAsync(caseId);
        return caseDetail!.Lines.First(l => l.Id == line.Id);
    }

    public async Task CancelOrderLineAllocationAsync(Guid caseId, Guid lineId, Guid userId, string? correlationId = null)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var line = await _db.ImportCaseLines
            .Include(l => l.ShipmentAllocations)
            .FirstOrDefaultAsync(l => l.Id == lineId && l.ImportCaseId == caseId);

        if (line == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_LINE_NOT_FOUND: Dosya sipariş kalemi bulunamadı.");
        }

        decimal totalShippedQty = line.ShipmentAllocations
            .Where(sa => sa.Status != ShipmentLineAllocationStatus.Cancelled)
            .Sum(sa => sa.ShippedQuantity);

        if (totalShippedQty > 0)
        {
            throw new InvalidOperationException($"ALLOCATION_CANNOT_BE_REMOVED_AFTER_SHIPMENT: Fiilen sevk edilmiş ({totalShippedQty}) sipariş kaleminin tahsisi iptal edilemez.");
        }

        line.ReleasedQuantity = line.AllocatedQuantity;
        line.Status = ImportCaseLineStatus.Cancelled;
        line.UpdatedAtUtc = DateTime.UtcNow;
        line.UpdatedByUserId = userId;

        foreach (var sa in line.ShipmentAllocations.Where(sa => sa.Status != ShipmentLineAllocationStatus.Cancelled))
        {
            sa.ReleasedQuantity = sa.AllocatedQuantity;
            sa.Status = ShipmentLineAllocationStatus.Cancelled;
            sa.UpdatedAtUtc = DateTime.UtcNow;
            sa.UpdatedByUserId = userId;
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "PurchaseOrderLine.AllocationCancelled",
            entityType: "ImportCaseLine",
            entityId: line.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { caseId, lineId }
        );
    }

    public async Task<List<SupplierLookupDto>> GetAvailableSuppliersAsync(string? search)
    {
        var query = _db.PurchaseOrders
            .Where(p => p.Status == "Open")
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normSearch = search.Trim().ToUpperInvariant();
            query = query.Where(p => p.NormalizedSupplierName.Contains(normSearch) || p.SupplierName.ToUpper().Contains(normSearch));
        }

        var rawSuppliers = await query
            .GroupBy(p => new { p.SupplierName, p.NormalizedSupplierName })
            .Select(g => new {
                SupplierName = g.Key.SupplierName,
                NormalizedSupplierName = g.Key.NormalizedSupplierName,
                OrderCount = g.Count()
            })
            .OrderBy(s => s.SupplierName)
            .Take(50)
            .ToListAsync();

        return rawSuppliers.Select(s => new SupplierLookupDto(s.SupplierName, s.NormalizedSupplierName, s.OrderCount)).ToList();
    }

    public async Task<ImportCaseOperationalSummaryDto> GetOperationalSummaryAsync()
    {
        var cases = await _db.ImportCases
            .Include(c => c.Shipments)
            .AsNoTracking()
            .ToListAsync();

        var now = DateTime.UtcNow;
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday);
        var endOfWeek = startOfWeek.AddDays(7);

        int activeCases = cases.Count(c => c.Status == ImportCaseStatus.Active);
        int productionDelayed = cases.Count(c => c.ProductionStatus == ProductionStatus.Delayed || (c.EstimatedProductionCompletionDate.HasValue && c.EstimatedProductionCompletionDate.Value < now.Date && c.ProductionStatus != ProductionStatus.Completed && c.ProductionStatus != ProductionStatus.ReadyForShipment));
        int readyForShipment = cases.Count(c => c.ProductionStatus == ProductionStatus.ReadyForShipment);
        int bookingPending = cases.SelectMany(c => c.Shipments).Count(s => s.Status == ShipmentStatus.BookingPending);
        int inTransitShipments = cases.SelectMany(c => c.Shipments).Count(s => s.Status == ShipmentStatus.InTransit);

        int delayedShipments = cases.SelectMany(c => c.Shipments)
            .Count(s => s.Status != ShipmentStatus.Cancelled && s.Status != ShipmentStatus.Delivered && s.Status != ShipmentStatus.Aborted &&
                        ((s.Etd.HasValue && s.Atd == null && s.Etd.Value < now) || (s.Eta.HasValue && s.Ata == null && s.Eta.Value < now)));

        int etaThisWeek = cases.SelectMany(c => c.Shipments)
            .Count(s => s.Status != ShipmentStatus.Cancelled && s.Eta.HasValue && s.Eta.Value >= startOfWeek && s.Eta.Value < endOfWeek);

        var poLines = await _db.PurchaseOrderLines.Where(pol => pol.RemainingQuantity > 0).AsNoTracking().ToListAsync();
        var poLineIds = poLines.Select(l => l.Id).ToList();
        var activeAllocations = await _db.ImportCaseLines.Where(l => poLineIds.Contains(l.PurchaseOrderLineId) && l.Status != ImportCaseLineStatus.Cancelled).AsNoTracking().ToListAsync();

        int unallocatedLines = 0;
        foreach (var pol in poLines)
        {
            decimal activeEffectiveAllocated = activeAllocations.Where(l => l.PurchaseOrderLineId == pol.Id).Sum(l => l.EffectiveAllocatedQuantity);
            if (pol.RemainingQuantity - activeEffectiveAllocated > 0)
            {
                unallocatedLines++;
            }
        }

        return new ImportCaseOperationalSummaryDto(
            activeCases,
            productionDelayed,
            readyForShipment,
            bookingPending,
            inTransitShipments,
            delayedShipments,
            etaThisWeek,
            unallocatedLines
        );
    }

    public async Task<List<AuditLog>> GetCaseHistoryAsync(Guid id)
    {
        var logs = await _db.AuditLogs
            .Where(a => a.EntityId == id.ToString() || a.MetadataJson!.Contains(id.ToString()))
            .OrderByDescending(a => a.TimestampUtc)
            .Take(100)
            .AsNoTracking()
            .ToListAsync();

        return logs;
    }
}
