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
using Microsoft.EntityFrameworkCore.Storage;

namespace ImportControlTower.Infrastructure.Services;

public class ShipmentService : IShipmentService
{
    private readonly ApplicationDbContext _db;
    private readonly ITimezoneService _timezoneService;
    private readonly IContainerValidationService _containerValidator;
    private readonly IAuditLogService _auditLog;

    public ShipmentService(
        ApplicationDbContext db,
        ITimezoneService timezoneService,
        IContainerValidationService containerValidator,
        IAuditLogService auditLog)
    {
        _db = db;
        _timezoneService = timezoneService;
        _containerValidator = containerValidator;
        _auditLog = auditLog;
    }

    public async Task<ShipmentDetailDto> CreateShipmentAsync(Guid caseId, CreateShipmentDto dto, Guid userId, string? correlationId = null)
    {
        if (!_timezoneService.IsValidTimezoneId(dto.OriginTimezoneId))
        {
            throw new ArgumentException("INVALID_TIMEZONE_ID: Çıkış zaman dilimi kimliği geçersizdir.");
        }
        if (!_timezoneService.IsValidTimezoneId(dto.DestinationTimezoneId))
        {
            throw new ArgumentException("INVALID_TIMEZONE_ID: Varış zaman dilimi kimliği geçersizdir.");
        }

        DateTime? etdUtc = null;
        if (dto.Etd.HasValue)
        {
            etdUtc = _timezoneService.ConvertLocalToUtc(dto.Etd.Value, dto.OriginTimezoneId, out string? err);
            if (err != null) throw new ArgumentException($"{err}: ETD tarihi dönüştürülemedi.");
        }

        DateTime? etaUtc = null;
        if (dto.Eta.HasValue)
        {
            etaUtc = _timezoneService.ConvertLocalToUtc(dto.Eta.Value, dto.DestinationTimezoneId, out string? err);
            if (err != null) throw new ArgumentException($"{err}: ETA tarihi dönüştürülemedi.");
        }

        if (etdUtc.HasValue && etaUtc.HasValue && etdUtc.Value > etaUtc.Value)
        {
            throw new ArgumentException("ETD_AFTER_ETA: Tahmini kalkış tarihi varış tarihinden sonra olamaz.");
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var importCase = await _db.ImportCases
            .FromSqlInterpolated($"SELECT *, xmin FROM import_cases WHERE \"Id\" = {caseId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (importCase == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_NOT_FOUND: İthalat dosyası bulunamadı.");
        }

        if (importCase.Status == ImportCaseStatus.Closed || importCase.Status == ImportCaseStatus.Cancelled)
        {
            throw new InvalidOperationException("IMPORT_CASE_ALREADY_CLOSED: Kapanmış veya iptal edilmiş dosyaya sevkiyat eklenemez.");
        }

        int nextSequence = importCase.LastShipmentSequence + 1;
        importCase.LastShipmentSequence = nextSequence;

        string shipmentNumber = $"{importCase.CaseNumber}-S{nextSequence:D2}";

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            ImportCaseId = caseId,
            ShipmentSequence = nextSequence,
            ShipmentNumber = shipmentNumber,
            TransportMode = dto.TransportMode,
            BookingNumber = dto.BookingNumber?.Trim(),
            OriginLocation = dto.OriginLocation.Trim(),
            DestinationLocation = dto.DestinationLocation.Trim(),
            ForwarderName = dto.ForwarderName?.Trim(),
            CarrierName = dto.CarrierName?.Trim(),
            TransportReference = dto.TransportReference?.Trim(),
            VesselName = dto.VesselName?.Trim(),
            VoyageNumber = dto.VoyageNumber?.Trim(),
            OriginTimezoneId = dto.OriginTimezoneId,
            DestinationTimezoneId = dto.DestinationTimezoneId,
            Etd = etdUtc,
            Eta = etaUtc,
            Status = ShipmentStatus.Draft,
            Notes = dto.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        _db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            EntityType = "Shipment",
            OldStatus = null,
            NewStatus = ShipmentStatus.Draft,
            Reason = "Sevkiyat oluşturuldu.",
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = userId
        });
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "Shipment.Created",
            entityType: "Shipment",
            entityId: shipment.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipment.ShipmentNumber, caseId, dto.TransportMode }
        );

        return (await GetShipmentByIdAsync(shipment.Id))!;
    }

    public async Task<List<ShipmentSummaryDto>> GetShipmentsByCaseIdAsync(Guid caseId)
    {
        var shipments = await _db.Shipments
            .Where(s => s.ImportCaseId == caseId)
            .Include(s => s.Containers)
            .Include(s => s.LineAllocations)
            .OrderBy(s => s.ShipmentSequence)
            .AsNoTracking()
            .ToListAsync();

        return shipments.Select(s => new ShipmentSummaryDto(
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
            s.Containers.Count(c => c.Status != ContainerStatus.Cancelled),
            s.LineAllocations.Count(la => la.Status != ShipmentLineAllocationStatus.Cancelled),
            s.CreatedAtUtc,
            s.UpdatedAtUtc
        )).ToList();
    }

    public async Task<ShipmentDetailDto?> GetShipmentByIdAsync(Guid shipmentId)
    {
        var s = await _db.Shipments
            .Include(x => x.ImportCase)
            .Include(x => x.Containers)
            .Include(x => x.Milestones)
            .Include(x => x.LineAllocations).ThenInclude(la => la.ImportCaseLine).ThenInclude(icl => icl!.PurchaseOrderLine)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == shipmentId);

        if (s == null) return null;

        var lineAllocationsDto = s.LineAllocations
            .Where(la => la.Status != ShipmentLineAllocationStatus.Cancelled)
            .Select(la =>
            {
                var rv = _db.Entry(la).Property<uint>("xmin").CurrentValue;
                return new ShipmentLineAllocationDto(
                    la.Id,
                    la.ShipmentId,
                    la.ImportCaseLineId,
                    la.ImportCaseId,
                    la.ImportCaseLine?.PurchaseOrderLine?.StockCode ?? "",
                    la.ImportCaseLine?.PurchaseOrderLine?.StockName ?? "",
                    la.ImportCaseLine?.AllocatedQuantity ?? 0,
                    la.AllocatedQuantity,
                    la.ReleasedQuantity,
                    la.EffectiveAllocatedQuantity,
                    la.ShippedQuantity,
                    la.ReceivedQuantity,
                    la.Status,
                    la.CreatedAtUtc,
                    la.UpdatedAtUtc,
                    rv
                );
            }).ToList();

        var containersDto = s.Containers
            .Where(c => c.Status != ContainerStatus.Cancelled)
            .Select(c =>
            {
                var rv = _db.Entry(c).Property<uint>("xmin").CurrentValue;
                return new ShipmentContainerDto(
                    c.Id,
                    c.ShipmentId,
                    c.ContainerNumber,
                    c.NormalizedContainerNumber,
                    c.ContainerType,
                    c.SealNumber,
                    c.GrossWeightKg,
                    c.NetWeightKg,
                    c.PackageCount,
                    c.Status,
                    c.Notes,
                    c.CreatedAtUtc,
                    c.UpdatedAtUtc,
                    rv
                );
            }).ToList();

        var milestonesDto = s.Milestones
            .OrderBy(m => m.SequenceNumber)
            .Select(m =>
            {
                var rv = _db.Entry(m).Property<uint>("xmin").CurrentValue;
                return new ShipmentMilestoneDto(
                    m.Id,
                    m.ShipmentId,
                    m.SequenceNumber,
                    m.MilestoneType,
                    m.LocationName ?? "",
                    m.TimezoneId,
                    m.PlannedAtUtc,
                    m.EstimatedAtUtc,
                    m.ActualAtUtc,
                    m.Status,
                    m.Source,
                    m.Notes,
                    m.CreatedAtUtc,
                    m.UpdatedAtUtc,
                    rv
                );
            }).ToList();

        var shipmentRowVersion = _db.Entry(s).Property<uint>("xmin").CurrentValue;

        return new ShipmentDetailDto(
            s.Id,
            s.ImportCaseId,
            s.ImportCase?.CaseNumber ?? "",
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
            s.EstimatedWarehouseArrival,
            s.ActualWarehouseArrival,
            s.Status,
            s.Notes,
            s.CreatedAtUtc,
            s.UpdatedAtUtc,
            shipmentRowVersion,
            lineAllocationsDto,
            containersDto,
            milestonesDto
        );
    }

    public async Task<ShipmentDetailDto> UpdateShipmentAsync(Guid shipmentId, UpdateShipmentDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null)
    {
        var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.Id == shipmentId);
        if (shipment == null)
        {
            throw new KeyNotFoundException("SHIPMENT_NOT_FOUND: Sevkiyat bulunamadı.");
        }

        if (shipment.Status == ShipmentStatus.Cancelled || shipment.Status == ShipmentStatus.Aborted)
        {
            throw new InvalidOperationException("SHIPMENT_CANCEL_NOT_ALLOWED: İptal veya abort edilmiş sevkiyat güncellenemez.");
        }

        var currentRowVersion = _db.Entry(shipment).Property<uint>("xmin").CurrentValue;
        if (currentRowVersion != expectedRowVersion)
        {
            throw new DbUpdateConcurrencyException("CONCURRENCY_CONFLICT: Kayıt başka bir kullanıcı tarafından güncellenmiştir.");
        }

        if (!_timezoneService.IsValidTimezoneId(dto.OriginTimezoneId))
        {
            throw new ArgumentException("INVALID_TIMEZONE_ID: Çıkış zaman dilimi kimliği geçersizdir.");
        }
        if (!_timezoneService.IsValidTimezoneId(dto.DestinationTimezoneId))
        {
            throw new ArgumentException("INVALID_TIMEZONE_ID: Varış zaman dilimi kimliği geçersizdir.");
        }

        DateTime? etdUtc = dto.Etd.HasValue ? _timezoneService.ConvertLocalToUtc(dto.Etd.Value, dto.OriginTimezoneId, out _) : null;
        DateTime? etaUtc = dto.Eta.HasValue ? _timezoneService.ConvertLocalToUtc(dto.Eta.Value, dto.DestinationTimezoneId, out _) : null;
        DateTime? atdUtc = dto.Atd.HasValue ? _timezoneService.ConvertLocalToUtc(dto.Atd.Value, dto.OriginTimezoneId, out _) : null;
        DateTime? ataUtc = dto.Ata.HasValue ? _timezoneService.ConvertLocalToUtc(dto.Ata.Value, dto.DestinationTimezoneId, out _) : null;

        if (etdUtc.HasValue && etaUtc.HasValue && etdUtc.Value > etaUtc.Value)
        {
            throw new ArgumentException("ETD_AFTER_ETA: Tahmini kalkış tarihi varış tarihinden sonra olamaz.");
        }
        if (atdUtc.HasValue && ataUtc.HasValue && atdUtc.Value > ataUtc.Value)
        {
            throw new ArgumentException("ATA_BEFORE_ATD: Gerçekleşen varış tarihi kalkış tarihinden önce olamaz.");
        }

        string oldStatus = shipment.Status;
        shipment.TransportMode = dto.TransportMode;
        shipment.OriginLocation = dto.OriginLocation.Trim();
        shipment.DestinationLocation = dto.DestinationLocation.Trim();
        shipment.OriginTimezoneId = dto.OriginTimezoneId;
        shipment.DestinationTimezoneId = dto.DestinationTimezoneId;
        shipment.BookingNumber = dto.BookingNumber?.Trim();
        shipment.ForwarderName = dto.ForwarderName?.Trim();
        shipment.CarrierName = dto.CarrierName?.Trim();
        shipment.TransportReference = dto.TransportReference?.Trim();
        shipment.VesselName = dto.VesselName?.Trim();
        shipment.VoyageNumber = dto.VoyageNumber?.Trim();
        shipment.Etd = etdUtc;
        shipment.Eta = etaUtc;
        shipment.Atd = atdUtc;
        shipment.Ata = ataUtc;
        shipment.EstimatedWarehouseArrival = dto.EstimatedWarehouseArrival;
        shipment.ActualWarehouseArrival = dto.ActualWarehouseArrival;
        shipment.Status = dto.Status;
        shipment.Notes = dto.Notes?.Trim();
        shipment.UpdatedAtUtc = DateTime.UtcNow;
        shipment.UpdatedByUserId = userId;

        if (oldStatus != dto.Status)
        {
            _db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment.Id,
                EntityType = "Shipment",
                OldStatus = oldStatus,
                NewStatus = dto.Status,
                Reason = "Sevkiyat durumu güncellendi.",
                ChangedAtUtc = DateTime.UtcNow,
                ChangedByUserId = userId
            });
        }

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Shipment.Updated",
            entityType: "Shipment",
            entityId: shipment.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipment.ShipmentNumber, shipment.Status }
        );

        return (await GetShipmentByIdAsync(shipment.Id))!;
    }

    public async Task<ShipmentDetailDto> CancelShipmentAsync(Guid shipmentId, Guid userId, string? correlationId = null)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var shipment = await _db.Shipments
            .Include(s => s.LineAllocations)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new KeyNotFoundException("SHIPMENT_NOT_FOUND: Sevkiyat bulunamadı.");
        }

        if (shipment.Atd.HasValue || shipment.LineAllocations.Any(la => la.ShippedQuantity > 0))
        {
            throw new InvalidOperationException("SHIPMENT_CANCEL_NOT_ALLOWED: Kalkış yapmış veya yüklemesi tamamlanmış sevkiyat normal iptal edilemez. Lütfen Abort iş akışını kullanınız.");
        }

        foreach (var la in shipment.LineAllocations.Where(la => la.Status != ShipmentLineAllocationStatus.Cancelled))
        {
            la.ReleasedQuantity = la.AllocatedQuantity;
            la.Status = ShipmentLineAllocationStatus.Cancelled;
            la.UpdatedAtUtc = DateTime.UtcNow;
            la.UpdatedByUserId = userId;
        }

        string oldStatus = shipment.Status;
        shipment.Status = ShipmentStatus.Cancelled;
        shipment.UpdatedAtUtc = DateTime.UtcNow;
        shipment.UpdatedByUserId = userId;

        _db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            EntityType = "Shipment",
            OldStatus = oldStatus,
            NewStatus = ShipmentStatus.Cancelled,
            Reason = "Sevkiyat iptal edildi.",
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = userId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "Shipment.Cancelled",
            entityType: "Shipment",
            entityId: shipment.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipment.ShipmentNumber }
        );

        return (await GetShipmentByIdAsync(shipment.Id))!;
    }

    public async Task<ShipmentDetailDto> AbortShipmentAsync(Guid shipmentId, AbortShipmentDto dto, Guid userId, uint expectedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 10)
        {
            throw new ArgumentException("SHIPMENT_ABORT_REASON_REQUIRED: Sevkiyatı olağanüstü kapatmak için en az 10 karakter açıklama girilmelidir.");
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var shipment = await _db.Shipments
            .Include(s => s.LineAllocations)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new KeyNotFoundException("SHIPMENT_NOT_FOUND: Sevkiyat bulunamadı.");
        }

        var currentRowVersion = _db.Entry(shipment).Property<uint>("xmin").CurrentValue;
        if (currentRowVersion != expectedRowVersion)
        {
            throw new DbUpdateConcurrencyException("CONCURRENCY_CONFLICT: Kayıt başka bir kullanıcı tarafından güncellenmiştir.");
        }

        if (shipment.Status != ShipmentStatus.Loading && shipment.Status != ShipmentStatus.InTransit && shipment.Status != ShipmentStatus.Arrived)
        {
            throw new InvalidOperationException("SHIPMENT_ABORT_NOT_ALLOWED: Yalnızca yükleme yapılan, yoldaki veya limana varmış sevkiyatlar abort edilebilir.");
        }

        foreach (var la in shipment.LineAllocations.Where(la => la.Status != ShipmentLineAllocationStatus.Cancelled))
        {
            la.ReleasedQuantity = la.AllocatedQuantity - la.ShippedQuantity;
            la.UpdatedAtUtc = DateTime.UtcNow;
            la.UpdatedByUserId = userId;
        }

        string oldStatus = shipment.Status;
        shipment.Status = ShipmentStatus.Aborted;
        shipment.UpdatedAtUtc = DateTime.UtcNow;
        shipment.UpdatedByUserId = userId;

        var sanitizedReason = dto.Reason.Trim();

        _db.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            EntityType = "Shipment",
            OldStatus = oldStatus,
            NewStatus = ShipmentStatus.Aborted,
            Reason = sanitizedReason,
            ChangedAtUtc = DateTime.UtcNow,
            ChangedByUserId = userId
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "Shipment.Aborted",
            entityType: "Shipment",
            entityId: shipment.Id.ToString(),
            actorUserId: userId,
            correlationId: dto.CorrelationId,
            metadata: new { shipment.ShipmentNumber, Reason = sanitizedReason }
        );

        return (await GetShipmentByIdAsync(shipment.Id))!;
    }

    public async Task<List<ShipmentLineAllocationDto>> GetShipmentLinesAsync(Guid shipmentId)
    {
        var s = await GetShipmentByIdAsync(shipmentId);
        if (s == null) throw new KeyNotFoundException("SHIPMENT_NOT_FOUND: Sevkiyat bulunamadı.");
        return s.LineAllocations;
    }

    public async Task<ShipmentLineAllocationDto> AllocateShipmentLineAsync(Guid shipmentId, AllocateShipmentLineDto dto, Guid userId, string? correlationId = null)
    {
        if (dto.AllocatedQuantity <= 0)
        {
            throw new ArgumentException("ALLOCATED_QUANTITY_INVALID: Sevkiyat tahsis miktarı 0'dan büyük olmalıdır.");
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.Id == shipmentId);
        if (shipment == null)
        {
            throw new KeyNotFoundException("SHIPMENT_NOT_FOUND: Sevkiyat bulunamadı.");
        }

        if (shipment.Status == ShipmentStatus.Cancelled || shipment.Status == ShipmentStatus.Aborted || shipment.Status == ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException("SHIPMENT_CANCEL_NOT_ALLOWED: Tamamlanmış veya iptal edilmiş sevkiyata satır eklenemez.");
        }

        var caseLine = await _db.ImportCaseLines
            .FromSqlInterpolated($"SELECT *, xmin FROM import_case_lines WHERE \"Id\" = {dto.ImportCaseLineId} FOR UPDATE")
            .FirstOrDefaultAsync();

        if (caseLine == null)
        {
            throw new KeyNotFoundException("IMPORT_CASE_LINE_NOT_FOUND: Dosya sipariş kalemi bulunamadı.");
        }

        if (caseLine.ImportCaseId != shipment.ImportCaseId)
        {
            throw new InvalidOperationException("SHIPMENT_LINE_IMPORT_CASE_MISMATCH: Sipariş kalemi ile sevkiyat farklı ithalat dosyalarına aittir.");
        }

        var otherActiveShipmentAllocationsSum = await _db.ShipmentLineAllocations
            .Where(la => la.ImportCaseLineId == dto.ImportCaseLineId && la.Status != ShipmentLineAllocationStatus.Cancelled)
            .SumAsync(la => la.AllocatedQuantity - la.ReleasedQuantity);

        decimal availableInCase = caseLine.EffectiveAllocatedQuantity - otherActiveShipmentAllocationsSum;

        if (dto.AllocatedQuantity > availableInCase)
        {
            throw new InvalidOperationException($"SHIPMENT_LINE_ALLOCATION_EXCEEDED: Sevkiyat tahsis miktarı ({dto.AllocatedQuantity}) dosyadaki kullanılabilir miktar bakiyesini ({availableInCase}) aşıyor.");
        }

        var existingAllocation = await _db.ShipmentLineAllocations
            .FirstOrDefaultAsync(la => la.ShipmentId == shipmentId && la.ImportCaseLineId == dto.ImportCaseLineId);

        ShipmentLineAllocation allocation;
        if (existingAllocation != null)
        {
            allocation = existingAllocation;
            allocation.AllocatedQuantity = dto.AllocatedQuantity;
            allocation.ReleasedQuantity = 0;
            allocation.Status = ShipmentLineAllocationStatus.Allocated;
            allocation.UpdatedAtUtc = DateTime.UtcNow;
            allocation.UpdatedByUserId = userId;
        }
        else
        {
            allocation = new ShipmentLineAllocation
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                ImportCaseLineId = dto.ImportCaseLineId,
                ImportCaseId = shipment.ImportCaseId,
                AllocatedQuantity = dto.AllocatedQuantity,
                ReleasedQuantity = 0,
                ShippedQuantity = 0,
                ReceivedQuantity = 0,
                Status = ShipmentLineAllocationStatus.Allocated,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = userId
            };
            _db.ShipmentLineAllocations.Add(allocation);
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "ShipmentLine.Allocated",
            entityType: "ShipmentLineAllocation",
            entityId: allocation.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, dto.ImportCaseLineId, dto.AllocatedQuantity }
        );

        var shipmentDetail = await GetShipmentByIdAsync(shipmentId);
        return shipmentDetail!.LineAllocations.First(la => la.Id == allocation.Id);
    }

    public async Task<ShipmentLineAllocationDto> UpdateShipmentLineAllocationAsync(Guid shipmentId, Guid allocationId, UpdateShipmentLineAllocationDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null)
    {
        if (dto.AllocatedQuantity <= 0)
        {
            throw new ArgumentException("ALLOCATED_QUANTITY_INVALID: Sevkiyat tahsis miktarı 0'dan büyük olmalıdır.");
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var allocation = await _db.ShipmentLineAllocations.FirstOrDefaultAsync(la => la.Id == allocationId && la.ShipmentId == shipmentId);
        if (allocation == null)
        {
            throw new KeyNotFoundException("SHIPMENT_LINE_ALLOCATION_NOT_FOUND: Sevkiyat tahsis kaydı bulunamadı.");
        }

        var currentRowVersion = _db.Entry(allocation).Property<uint>("xmin").CurrentValue;
        if (currentRowVersion != expectedRowVersion)
        {
            throw new DbUpdateConcurrencyException("CONCURRENCY_CONFLICT: Kayıt başka bir kullanıcı tarafından güncellenmiştir.");
        }

        if (dto.AllocatedQuantity < allocation.ShippedQuantity)
        {
            throw new InvalidOperationException($"ALLOCATION_CANNOT_BE_REMOVED_AFTER_SHIPMENT: Yeni miktar ({dto.AllocatedQuantity}) sevk edilmiş miktardan ({allocation.ShippedQuantity}) az olamaz.");
        }

        var caseLine = await _db.ImportCaseLines
            .FromSqlInterpolated($"SELECT *, xmin FROM import_case_lines WHERE \"Id\" = {allocation.ImportCaseLineId} FOR UPDATE")
            .FirstOrDefaultAsync();

        var otherAllocationsSum = await _db.ShipmentLineAllocations
            .Where(la => la.ImportCaseLineId == allocation.ImportCaseLineId && la.Id != allocationId && la.Status != ShipmentLineAllocationStatus.Cancelled)
            .SumAsync(la => la.AllocatedQuantity - la.ReleasedQuantity);

        decimal availableInCase = caseLine!.EffectiveAllocatedQuantity - otherAllocationsSum;
        if (dto.AllocatedQuantity > availableInCase)
        {
            throw new InvalidOperationException($"SHIPMENT_LINE_ALLOCATION_EXCEEDED: Yeni miktar ({dto.AllocatedQuantity}) dosyadaki bakiyeyi ({availableInCase}) aşıyor.");
        }

        allocation.AllocatedQuantity = dto.AllocatedQuantity;
        allocation.UpdatedAtUtc = DateTime.UtcNow;
        allocation.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "ShipmentLine.Updated",
            entityType: "ShipmentLineAllocation",
            entityId: allocation.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, allocationId, dto.AllocatedQuantity }
        );

        var shipmentDetail = await GetShipmentByIdAsync(shipmentId);
        return shipmentDetail!.LineAllocations.First(la => la.Id == allocation.Id);
    }

    public async Task CancelShipmentLineAllocationAsync(Guid shipmentId, Guid allocationId, Guid userId, string? correlationId = null)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var allocation = await _db.ShipmentLineAllocations.FirstOrDefaultAsync(la => la.Id == allocationId && la.ShipmentId == shipmentId);
        if (allocation == null)
        {
            throw new KeyNotFoundException("SHIPMENT_LINE_ALLOCATION_NOT_FOUND: Sevkiyat tahsis kaydı bulunamadı.");
        }

        if (allocation.ShippedQuantity > 0)
        {
            throw new InvalidOperationException($"ALLOCATION_CANNOT_BE_REMOVED_AFTER_SHIPMENT: Sevk edilmiş ({allocation.ShippedQuantity}) tahsis kaydı iptal edilemez.");
        }

        allocation.ReleasedQuantity = allocation.AllocatedQuantity;
        allocation.Status = ShipmentLineAllocationStatus.Cancelled;
        allocation.UpdatedAtUtc = DateTime.UtcNow;
        allocation.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _auditLog.LogAsync(
            action: "ShipmentLine.Cancelled",
            entityType: "ShipmentLineAllocation",
            entityId: allocation.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, allocationId }
        );
    }

    public async Task<ShipmentContainerDto> AddContainerAsync(Guid shipmentId, AddContainerDto dto, Guid userId, string? correlationId = null)
    {
        var shipment = await _db.Shipments.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shipmentId);
        if (shipment == null)
        {
            throw new KeyNotFoundException("SHIPMENT_NOT_FOUND: Sevkiyat bulunamadı.");
        }

        if (shipment.TransportMode != TransportMode.Sea && shipment.TransportMode != TransportMode.Multimodal)
        {
            throw new InvalidOperationException($"CONTAINER_NOT_ALLOWED_FOR_TRANSPORT_MODE: Konteyner ekleme yalnızca Deniz veya Multimodal taşımalarda geçerlidir ({shipment.TransportMode} taşımalarında konteyner kullanılamaz).");
        }

        var normalizedNumber = _containerValidator.NormalizeContainerNumber(dto.ContainerNumber);

        if (!_containerValidator.IsValidFormat(normalizedNumber))
        {
            throw new ArgumentException("CONTAINER_NUMBER_INVALID: Konteyner numarası ISO 6346 formatına uymuyor (4 harf + 7 rakam).");
        }

        bool validCheckDigit = _containerValidator.VerifyCheckDigit(normalizedNumber);
        if (!validCheckDigit && !dto.OverrideCheckDigit)
        {
            throw new ArgumentException("CONTAINER_CHECK_DIGIT_INVALID: Konteyner numarası ISO 6346 check digit doğrulamasından geçemedi.");
        }

        if (!validCheckDigit && dto.OverrideCheckDigit)
        {
            if (string.IsNullOrWhiteSpace(dto.OverrideReason) || dto.OverrideReason.Trim().Length < 10)
            {
                throw new ArgumentException("CONTAINER_OVERRIDE_REASON_REQUIRED: Geçersiz check digit için en az 10 karakter açıklama yazılmalıdır.");
            }
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        var hashCmd = _db.Database.GetDbConnection().CreateCommand();
        hashCmd.Transaction = tx.GetDbTransaction();
        hashCmd.CommandText = $"SELECT pg_try_advisory_xact_lock(hashtext('{normalizedNumber}'))";
        await hashCmd.ExecuteScalarAsync();

        var activeDuplicate = await _db.ShipmentContainers
            .Include(c => c.Shipment)
            .FirstOrDefaultAsync(c => c.NormalizedContainerNumber == normalizedNumber &&
                                       c.Status != ContainerStatus.Cancelled &&
                                       c.Shipment!.Status != ShipmentStatus.Cancelled &&
                                       c.Shipment!.Status != ShipmentStatus.Aborted);

        if (activeDuplicate != null)
        {
            throw new InvalidOperationException($"CONTAINER_ALREADY_ASSIGNED: Konteyner ({normalizedNumber}) {activeDuplicate.Shipment!.ShipmentNumber} nolu aktif sevkiyata zaten tanımlanmış.");
        }

        var container = new ShipmentContainer
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            ContainerNumber = dto.ContainerNumber.Trim(),
            NormalizedContainerNumber = normalizedNumber,
            ContainerType = dto.ContainerType,
            SealNumber = dto.SealNumber?.Trim(),
            GrossWeightKg = dto.GrossWeightKg,
            NetWeightKg = dto.NetWeightKg,
            PackageCount = dto.PackageCount,
            Status = ContainerStatus.Assigned,
            Notes = dto.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _db.ShipmentContainers.Add(container);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        if (!validCheckDigit && dto.OverrideCheckDigit)
        {
            await _auditLog.LogAsync(
                action: "Container.CheckDigitOverridden",
                entityType: "ShipmentContainer",
                entityId: container.Id.ToString(),
                actorUserId: userId,
                correlationId: correlationId,
                metadata: new { shipmentId, container.ContainerNumber, dto.OverrideReason }
            );
        }

        await _auditLog.LogAsync(
            action: "Container.Added",
            entityType: "ShipmentContainer",
            entityId: container.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, container.ContainerNumber, container.ContainerType }
        );

        var sDetail = await GetShipmentByIdAsync(shipmentId);
        return sDetail!.Containers.First(c => c.Id == container.Id);
    }

    public async Task<ShipmentContainerDto> UpdateContainerAsync(Guid shipmentId, Guid containerId, UpdateContainerDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null)
    {
        var container = await _db.ShipmentContainers.FirstOrDefaultAsync(c => c.Id == containerId && c.ShipmentId == shipmentId);
        if (container == null)
        {
            throw new KeyNotFoundException("CONTAINER_NOT_FOUND: Konteyner bulunamadı.");
        }

        var currentRowVersion = _db.Entry(container).Property<uint>("xmin").CurrentValue;
        if (currentRowVersion != expectedRowVersion)
        {
            throw new DbUpdateConcurrencyException("CONCURRENCY_CONFLICT: Kayıt başka bir kullanıcı tarafından güncellenmiştir.");
        }

        container.ContainerType = dto.ContainerType;
        container.SealNumber = dto.SealNumber?.Trim();
        container.GrossWeightKg = dto.GrossWeightKg;
        container.NetWeightKg = dto.NetWeightKg;
        container.PackageCount = dto.PackageCount;
        container.Status = dto.Status;
        container.Notes = dto.Notes?.Trim();
        container.UpdatedAtUtc = DateTime.UtcNow;
        container.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Container.Updated",
            entityType: "ShipmentContainer",
            entityId: container.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, containerId, container.Status }
        );

        var sDetail = await GetShipmentByIdAsync(shipmentId);
        return sDetail!.Containers.First(c => c.Id == container.Id);
    }

    public async Task CancelContainerAsync(Guid shipmentId, Guid containerId, Guid userId, string? correlationId = null)
    {
        var container = await _db.ShipmentContainers.FirstOrDefaultAsync(c => c.Id == containerId && c.ShipmentId == shipmentId);
        if (container == null)
        {
            throw new KeyNotFoundException("CONTAINER_NOT_FOUND: Konteyner bulunamadı.");
        }

        container.Status = ContainerStatus.Cancelled;
        container.UpdatedAtUtc = DateTime.UtcNow;
        container.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Container.Cancelled",
            entityType: "ShipmentContainer",
            entityId: container.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, containerId }
        );
    }

    public async Task<List<ShipmentMilestoneDto>> GetMilestonesAsync(Guid shipmentId)
    {
        var s = await GetShipmentByIdAsync(shipmentId);
        if (s == null) throw new KeyNotFoundException("SHIPMENT_NOT_FOUND: Sevkiyat bulunamadı.");
        return s.Milestones;
    }

    public async Task<ShipmentMilestoneDto> CreateMilestoneAsync(Guid shipmentId, CreateMilestoneDto dto, Guid userId, string? correlationId = null)
    {
        if (!_timezoneService.IsValidTimezoneId(dto.TimezoneId))
        {
            throw new ArgumentException("INVALID_TIMEZONE_ID: Zaman dilimi kimliği geçersizdir.");
        }

        DateTime? plannedUtc = dto.PlannedAt.HasValue ? _timezoneService.ConvertLocalToUtc(dto.PlannedAt.Value, dto.TimezoneId, out _) : null;
        DateTime? estimatedUtc = dto.EstimatedAt.HasValue ? _timezoneService.ConvertLocalToUtc(dto.EstimatedAt.Value, dto.TimezoneId, out _) : null;
        DateTime? actualUtc = dto.ActualAt.HasValue ? _timezoneService.ConvertLocalToUtc(dto.ActualAt.Value, dto.TimezoneId, out _) : null;

        if (dto.Status == MilestoneStatus.Completed && !actualUtc.HasValue)
        {
            throw new ArgumentException("MILESTONE_DATE_SEQUENCE_INVALID: Tamamlanmış (Completed) milestone için gerçekleşen tarih (actualAt) girmek zorunludur.");
        }

        var milestone = new ShipmentMilestone
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            SequenceNumber = dto.SequenceNumber,
            MilestoneType = dto.MilestoneType,
            LocationName = dto.LocationName?.Trim(),
            TimezoneId = dto.TimezoneId,
            PlannedAtUtc = plannedUtc,
            EstimatedAtUtc = estimatedUtc,
            ActualAtUtc = actualUtc,
            Status = dto.Status,
            Source = MilestoneSource.Manual,
            Notes = dto.Notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _db.ShipmentMilestones.Add(milestone);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Milestone.Created",
            entityType: "ShipmentMilestone",
            entityId: milestone.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, dto.MilestoneType, dto.SequenceNumber }
        );

        var sDetail = await GetShipmentByIdAsync(shipmentId);
        return sDetail!.Milestones.First(m => m.Id == milestone.Id);
    }

    public async Task<ShipmentMilestoneDto> UpdateMilestoneAsync(Guid shipmentId, Guid milestoneId, UpdateMilestoneDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null)
    {
        var milestone = await _db.ShipmentMilestones.FirstOrDefaultAsync(m => m.Id == milestoneId && m.ShipmentId == shipmentId);
        if (milestone == null)
        {
            throw new KeyNotFoundException("MILESTONE_NOT_FOUND: Milestone bulunamadı.");
        }

        var currentRowVersion = _db.Entry(milestone).Property<uint>("xmin").CurrentValue;
        if (currentRowVersion != expectedRowVersion)
        {
            throw new DbUpdateConcurrencyException("CONCURRENCY_CONFLICT: Kayıt başka bir kullanıcı tarafından güncellenmiştir.");
        }

        if (!_timezoneService.IsValidTimezoneId(dto.TimezoneId))
        {
            throw new ArgumentException("INVALID_TIMEZONE_ID: Zaman dilimi kimliği geçersizdir.");
        }

        DateTime? plannedUtc = dto.PlannedAt.HasValue ? _timezoneService.ConvertLocalToUtc(dto.PlannedAt.Value, dto.TimezoneId, out _) : null;
        DateTime? estimatedUtc = dto.EstimatedAt.HasValue ? _timezoneService.ConvertLocalToUtc(dto.EstimatedAt.Value, dto.TimezoneId, out _) : null;
        DateTime? actualUtc = dto.ActualAt.HasValue ? _timezoneService.ConvertLocalToUtc(dto.ActualAt.Value, dto.TimezoneId, out _) : null;

        if (dto.Status == MilestoneStatus.Completed && !actualUtc.HasValue)
        {
            throw new ArgumentException("MILESTONE_DATE_SEQUENCE_INVALID: Tamamlanmış (Completed) milestone için gerçekleşen tarih (actualAt) girmek zorunludur.");
        }

        milestone.SequenceNumber = dto.SequenceNumber;
        milestone.TimezoneId = dto.TimezoneId;
        milestone.LocationName = dto.LocationName?.Trim();
        milestone.PlannedAtUtc = plannedUtc;
        milestone.EstimatedAtUtc = estimatedUtc;
        milestone.ActualAtUtc = actualUtc;
        milestone.Status = dto.Status;
        milestone.Notes = dto.Notes?.Trim();
        milestone.UpdatedAtUtc = DateTime.UtcNow;
        milestone.UpdatedByUserId = userId;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "Milestone.Updated",
            entityType: "ShipmentMilestone",
            entityId: milestone.Id.ToString(),
            actorUserId: userId,
            correlationId: correlationId,
            metadata: new { shipmentId, milestoneId, dto.Status }
        );

        var sDetail = await GetShipmentByIdAsync(shipmentId);
        return sDetail!.Milestones.First(m => m.Id == milestone.Id);
    }
}
