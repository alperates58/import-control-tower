using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.DTOs;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImportControlTower.Infrastructure.Services;

public class DocumentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IObjectStorageService _storageService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext dbContext,
        IObjectStorageService storageService,
        IAuditLogService auditLogService,
        ILogger<DocumentService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<DocumentDto> CreateDocumentAsync(
        CreateDocumentDto dto,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // 1. Exact-One Scope Check
        int scopeCount = (dto.ImportCaseId.HasValue ? 1 : 0) +
                         (dto.ShipmentId.HasValue ? 1 : 0) +
                         (dto.ShipmentContainerId.HasValue ? 1 : 0);
        if (scopeCount != 1)
        {
            throw new ArgumentException("Belge tam olarak tek bir alana (ImportCase, Shipment veya Container) bağlanmalıdır.");
        }

        // Validate Scope Existence
        string scopeType = "";
        Guid scopeId = Guid.Empty;
        if (dto.ImportCaseId.HasValue)
        {
            scopeType = "import-case";
            scopeId = dto.ImportCaseId.Value;
            if (!await _dbContext.ImportCases.AnyAsync(c => c.Id == scopeId, cancellationToken))
                throw new KeyNotFoundException("Belirtilen İthalat Dosyası bulunamadı.");
        }
        else if (dto.ShipmentId.HasValue)
        {
            scopeType = "shipment";
            scopeId = dto.ShipmentId.Value;
            if (!await _dbContext.Shipments.AnyAsync(s => s.Id == scopeId, cancellationToken))
                throw new KeyNotFoundException("Belirtilen Sevkiyat bulunamadı.");
        }
        else if (dto.ShipmentContainerId.HasValue)
        {
            scopeType = "container";
            scopeId = dto.ShipmentContainerId.Value;
            if (!await _dbContext.ShipmentContainers.AnyAsync(c => c.Id == scopeId, cancellationToken))
                throw new KeyNotFoundException("Belirtilen Konteyner bulunamadı.");
        }

        // 2. Security & File Validation
        var validation = await FileSecurityValidator.ValidateStreamAsync(fileStream, fileName, contentType);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"FILE_VALIDATION_FAILED:{validation.ErrorCode}:{validation.ErrorMessage}");
        }

        // Check Duplicate SHA-256 Hash for same scope
        bool duplicateHash = await _dbContext.DocumentVersions
            .AnyAsync(v => v.Sha256Hash == validation.Sha256Hash && v.Status == "Active" &&
                           ((dto.ImportCaseId.HasValue && v.Document!.ImportCaseId == dto.ImportCaseId) ||
                            (dto.ShipmentId.HasValue && v.Document!.ShipmentId == dto.ShipmentId) ||
                            (dto.ShipmentContainerId.HasValue && v.Document!.ShipmentContainerId == dto.ShipmentContainerId)), cancellationToken);

        if (duplicateHash)
        {
            throw new InvalidOperationException("DOCUMENT_DUPLICATE_HASH:Bu belgenin birebir aynı versiyonu zaten yüklüdür.");
        }

        // 3. Upload Temp Object to S3
        fileStream.Position = 0;
        string tempKey = await _storageService.UploadTempObjectAsync(fileStream, fileName, contentType, cancellationToken);

        // 4. Short DB Transaction 1: Create Document & Pending Version
        var document = new Document
        {
            Id = Guid.NewGuid(),
            ImportCaseId = dto.ImportCaseId,
            ShipmentId = dto.ShipmentId,
            ShipmentContainerId = dto.ShipmentContainerId,
            DocumentType = dto.DocumentType,
            Title = dto.Title,
            DocumentNumber = dto.DocumentNumber,
            DocumentDate = dto.DocumentDate,
            ExpiryDate = dto.ExpiryDate,
            Status = "Active",
            Notes = dto.Notes,
            CreatedByUserId = currentUserId
        };

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            VersionNumber = 1,
            OriginalFileName = fileName,
            StoredObjectKey = tempKey,
            ContentType = contentType,
            FileExtension = ext,
            FileSizeBytes = validation.FileSizeBytes,
            Sha256Hash = validation.Sha256Hash,
            StorageStatus = "Pending",
            IsCurrent = false,
            Status = "Active",
            UploadedByUserId = currentUserId
        };

        try
        {
            _dbContext.Documents.Add(document);
            _dbContext.DocumentVersions.Add(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB Transaction 1 failed for document creation. Cleaning temp object.");
            await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
            throw;
        }

        // 5. S3 CopyObject: temp -> final key
        string finalKey = $"documents/{scopeType}/{scopeId}/{document.Id}/v1/{Sanitize(fileName)}";
        try
        {
            await _storageService.CopyObjectAsync(tempKey, finalKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 CopyObject failed. Marking version Failed.");
            version.StorageStatus = "Failed";
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
            await _auditLogService.LogAsync("Document.UploadFailed", "Document", document.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"S3 Copy failed for document {document.Title}" }, cancellationToken: cancellationToken);
            throw new InvalidOperationException("S3_STORAGE_COPY_FAILED:Dosya kopyalama işlemi başarısız oldu.");
        }

        // 6. Short DB Transaction 2: Activate Version
        try
        {
            version.StoredObjectKey = finalKey;
            version.StorageStatus = "Active";
            version.IsCurrent = true;
            document.UpdatedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB Transaction 2 failed. Attempting S3 cleanup.");
            try
            {
                await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
                await _storageService.DeleteObjectAsync(finalKey, cancellationToken);
            }
            catch (Exception s3Ex)
            {
                _logger.LogError(s3Ex, "S3 Cleanup failed. Setting StorageStatus=CleanupRequired.");
                version.StorageStatus = "CleanupRequired";
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                await _auditLogService.LogAsync("Document.StorageCleanupFailed", "Document", document.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"Cleanup failed for key {finalKey}" }, cancellationToken: cancellationToken);
            }
            throw;
        }

        await _auditLogService.LogAsync("Document.Created", "Document", document.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"Document {document.Title} (Type: {document.DocumentType}) created." }, cancellationToken: cancellationToken);

        return await GetDocumentByIdAsync(document.Id, cancellationToken);
    }

    public async Task<DocumentDto> AddDocumentVersionAsync(
        Guid documentId,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document == null)
            throw new KeyNotFoundException("Belge bulunamadı.");

        if (document.Status == "Cancelled")
            throw new InvalidOperationException("İptal edilmiş belgeye yeni versiyon eklenemez.");

        var validation = await FileSecurityValidator.ValidateStreamAsync(fileStream, fileName, contentType);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"FILE_VALIDATION_FAILED:{validation.ErrorCode}:{validation.ErrorMessage}");
        }

        // Duplicate SHA-256 check for same Document
        bool duplicateHash = await _dbContext.DocumentVersions
            .AnyAsync(v => v.DocumentId == documentId && v.Sha256Hash == validation.Sha256Hash && v.Status == "Active", cancellationToken);
        if (duplicateHash)
        {
            throw new InvalidOperationException("DOCUMENT_DUPLICATE_HASH:Bu belgenin birebir aynı versiyonu zaten yüklüdür.");
        }

        int maxVer = await _dbContext.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0;
        int nextVer = maxVer + 1;

        string scopeType = document.ImportCaseId.HasValue ? "import-case" : document.ShipmentId.HasValue ? "shipment" : "container";
        Guid scopeId = document.ImportCaseId ?? document.ShipmentId ?? document.ShipmentContainerId ?? Guid.Empty;

        // Upload Temp
        fileStream.Position = 0;
        string tempKey = await _storageService.UploadTempObjectAsync(fileStream, fileName, contentType, cancellationToken);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            VersionNumber = nextVer,
            OriginalFileName = fileName,
            StoredObjectKey = tempKey,
            ContentType = contentType,
            FileExtension = ext,
            FileSizeBytes = validation.FileSizeBytes,
            Sha256Hash = validation.Sha256Hash,
            StorageStatus = "Pending",
            IsCurrent = false,
            Status = "Active",
            UploadedByUserId = currentUserId
        };

        try
        {
            _dbContext.DocumentVersions.Add(version);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
            throw;
        }

        // S3 Copy
        string finalKey = $"documents/{scopeType}/{scopeId}/{document.Id}/v{nextVer}/{Sanitize(fileName)}";
        try
        {
            await _storageService.CopyObjectAsync(tempKey, finalKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 Copy failed for version upload.");
            version.StorageStatus = "Failed";
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
            throw new InvalidOperationException("S3_STORAGE_COPY_FAILED:Versiyon dosya kopyalaması başarısız oldu.");
        }

        // Short DB Transaction 2: Atomic Activation
        try
        {
            var oldVersions = await _dbContext.DocumentVersions
                .Where(v => v.DocumentId == documentId && v.IsCurrent && v.Id != version.Id)
                .ToListAsync(cancellationToken);

            foreach (var oldV in oldVersions)
            {
                oldV.IsCurrent = false;
                oldV.Status = "Replaced";
            }

            version.StoredObjectKey = finalKey;
            version.StorageStatus = "Active";
            version.IsCurrent = true;
            document.UpdatedAtUtc = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
        }
        catch (Exception)
        {
            await _storageService.DeleteObjectAsync(tempKey, cancellationToken);
            await _storageService.DeleteObjectAsync(finalKey, cancellationToken);
            throw;
        }

        await _auditLogService.LogAsync("DocumentVersion.Uploaded", "DocumentVersion", version.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"Version v{nextVer} uploaded for document {document.Title}." }, cancellationToken: cancellationToken);
        await _auditLogService.LogAsync("DocumentVersion.Activated", "DocumentVersion", version.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"Version v{nextVer} set as active current version." }, cancellationToken: cancellationToken);

        return await GetDocumentByIdAsync(documentId, cancellationToken);
    }

    public async Task<string> GenerateDownloadUrlAsync(
        Guid documentId,
        Guid? versionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document == null) throw new KeyNotFoundException("Belge bulunamadı.");
        if (document.Status == "Cancelled") throw new InvalidOperationException("İptal edilmiş belgeler indirilemez.");

        DocumentVersion? version = null;
        if (versionId.HasValue)
        {
            version = await _dbContext.DocumentVersions
                .FirstOrDefaultAsync(v => v.Id == versionId.Value && v.DocumentId == documentId, cancellationToken);
        }
        else
        {
            version = await _dbContext.DocumentVersions
                .FirstOrDefaultAsync(v => v.DocumentId == documentId && v.IsCurrent && v.Status == "Active" && v.StorageStatus == "Active", cancellationToken);
        }

        if (version == null || version.StorageStatus != "Active" || version.Status == "Cancelled")
        {
            throw new InvalidOperationException("Geçerli veya aktif depolama durumunda versiyon bulunamadı.");
        }

        bool isInline = version.FileExtension == ".pdf" || version.FileExtension == ".png" || version.FileExtension == ".jpg" || version.FileExtension == ".jpeg";
        string url = await _storageService.GeneratePresignedDownloadUrlAsync(
            version.StoredObjectKey,
            version.OriginalFileName,
            TimeSpan.FromMinutes(15),
            isInline,
            cancellationToken);

        await _auditLogService.LogAsync("Document.Downloaded", "Document", document.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"Download URL generated for document {document.Title} v{version.VersionNumber}." }, cancellationToken: cancellationToken);

        return url;
    }

    public async Task<DocumentDto> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.Documents
            .Include(d => d.CreatedByUser)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (doc == null) throw new KeyNotFoundException("Belge bulunamadı.");

        var currentVer = await _dbContext.DocumentVersions
            .Include(v => v.UploadedByUser)
            .FirstOrDefaultAsync(v => v.DocumentId == id && v.IsCurrent && v.Status == "Active" && v.StorageStatus == "Active", cancellationToken);

        var xminObj = _dbContext.Entry(doc).Property("xmin").CurrentValue;
        uint xmin = xminObj is uint val ? val : 1;

        return MapToDto(doc, currentVer, xmin);
    }

    public async Task<List<DocumentDto>> GetDocumentsAsync(
        Guid? caseId,
        Guid? shipmentId,
        Guid? containerId,
        string? documentType,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Documents
            .Include(d => d.CreatedByUser)
            .AsQueryable();

        if (caseId.HasValue) query = query.Where(d => d.ImportCaseId == caseId.Value);
        if (shipmentId.HasValue) query = query.Where(d => d.ShipmentId == shipmentId.Value);
        if (containerId.HasValue) query = query.Where(d => d.ShipmentContainerId == containerId.Value);
        if (!string.IsNullOrWhiteSpace(documentType)) query = query.Where(d => d.DocumentType == documentType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d => d.Title.ToLower().Contains(s) || (d.DocumentNumber != null && d.DocumentNumber.ToLower().Contains(s)));
        }

        var docs = await query.OrderByDescending(d => d.CreatedAtUtc).ToListAsync(cancellationToken);
        var docIds = docs.Select(d => d.Id).ToList();

        var currentVers = await _dbContext.DocumentVersions
            .Include(v => v.UploadedByUser)
            .Where(v => docIds.Contains(v.DocumentId) && v.IsCurrent && v.Status == "Active" && v.StorageStatus == "Active")
            .ToDictionaryAsync(v => v.DocumentId, cancellationToken);

        var dtos = new List<DocumentDto>();
        foreach (var doc in docs)
        {
            currentVers.TryGetValue(doc.Id, out var curV);
            var xminObj = _dbContext.Entry(doc).Property("xmin").CurrentValue;
            uint xmin = xminObj is uint val ? val : 1;
            dtos.Add(MapToDto(doc, curV, xmin));
        }

        return dtos;
    }

    public async Task<List<DocumentVersionDto>> GetDocumentVersionsAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var versions = await _dbContext.DocumentVersions
            .Include(v => v.UploadedByUser)
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(v => new DocumentVersionDto
        {
            Id = v.Id,
            DocumentId = v.DocumentId,
            VersionNumber = v.VersionNumber,
            OriginalFileName = v.OriginalFileName,
            StoredObjectKey = v.StoredObjectKey,
            ContentType = v.ContentType,
            FileExtension = v.FileExtension,
            FileSizeBytes = v.FileSizeBytes,
            Sha256Hash = v.Sha256Hash,
            StorageStatus = v.StorageStatus,
            IsCurrent = v.IsCurrent,
            Status = v.Status,
            UploadedAtUtc = v.UploadedAtUtc,
            UploadedByUserId = v.UploadedByUserId,
            UploadedByUserName = v.UploadedByUser?.FullName
        }).ToList();
    }

    public async Task<DocumentDto> UpdateDocumentAsync(
        Guid id,
        UpdateDocumentDto dto,
        uint rowVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (doc == null) throw new KeyNotFoundException("Belge bulunamadı.");

        _dbContext.Entry(doc).Property("xmin").OriginalValue = rowVersion;

        doc.Title = dto.Title;
        doc.DocumentNumber = dto.DocumentNumber;
        doc.DocumentDate = dto.DocumentDate;
        doc.ExpiryDate = dto.ExpiryDate;
        doc.Notes = dto.Notes;
        doc.UpdatedAtUtc = DateTime.UtcNow;
        doc.UpdatedByUserId = currentUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("Document.Updated", "Document", doc.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"Document {doc.Title} updated." }, cancellationToken: cancellationToken);

        return await GetDocumentByIdAsync(id, cancellationToken);
    }

    public async Task<DocumentDto> CancelDocumentAsync(
        Guid id,
        uint rowVersion,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var doc = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (doc == null) throw new KeyNotFoundException("Belge bulunamadı.");

        _dbContext.Entry(doc).Property("xmin").OriginalValue = rowVersion;

        doc.Status = "Cancelled";
        doc.UpdatedAtUtc = DateTime.UtcNow;
        doc.UpdatedByUserId = currentUserId;

        var versions = await _dbContext.DocumentVersions
            .Where(v => v.DocumentId == id)
            .ToListAsync(cancellationToken);

        foreach (var v in versions)
        {
            v.IsCurrent = false;
            v.Status = "Cancelled";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("Document.Cancelled", "Document", doc.Id.ToString(), actorUserId: currentUserId, metadata: new { Details = $"Document {doc.Title} cancelled." }, cancellationToken: cancellationToken);

        return await GetDocumentByIdAsync(id, cancellationToken);
    }

    public async Task<DocumentChecklistDto> GetDocumentChecklistAsync(
        string scopeType,
        Guid scopeId,
        CancellationToken cancellationToken = default)
    {
        string? transportMode = null;
        if (scopeType.Equals("Shipment", StringComparison.OrdinalIgnoreCase))
        {
            var shipment = await _dbContext.Shipments.FirstOrDefaultAsync(s => s.Id == scopeId, cancellationToken);
            if (shipment != null) transportMode = shipment.TransportMode;
        }
        else if (scopeType.Equals("ImportCase", StringComparison.OrdinalIgnoreCase))
        {
            var ic = await _dbContext.ImportCases.FirstOrDefaultAsync(c => c.Id == scopeId, cancellationToken);
            if (ic != null) transportMode = ic.DefaultTransportMode;
        }

        var reqs = await _dbContext.DocumentRequirements
            .Where(r => r.ScopeType.ToLower() == scopeType.ToLower() &&
                        (r.TransportMode == null || (transportMode != null && r.TransportMode.ToLower() == transportMode.ToLower())))
            .OrderBy(r => r.SortOrder)
            .ToListAsync(cancellationToken);

        var docs = await GetDocumentsAsync(
            scopeType.Equals("ImportCase", StringComparison.OrdinalIgnoreCase) ? scopeId : null,
            scopeType.Equals("Shipment", StringComparison.OrdinalIgnoreCase) ? scopeId : null,
            scopeType.Equals("Container", StringComparison.OrdinalIgnoreCase) ? scopeId : null,
            null, "Active", null, cancellationToken);

        var checklist = new DocumentChecklistDto
        {
            ScopeType = scopeType,
            ScopeId = scopeId,
            TotalRequiredCount = reqs.Count(r => r.IsRequired)
        };

        int completed = 0;
        int missing = 0;

        foreach (var req in reqs)
        {
            var matchingDoc = docs.FirstOrDefault(d => d.DocumentType.Equals(req.DocumentType, StringComparison.OrdinalIgnoreCase) && d.Status == "Active");
            string itemStatus = "Missing";
            Guid? docId = null;
            string? docTitle = null;
            string? docNum = null;
            DateTime? expDate = null;

            if (matchingDoc != null)
            {
                docId = matchingDoc.Id;
                docTitle = matchingDoc.Title;
                docNum = matchingDoc.DocumentNumber;
                expDate = matchingDoc.ExpiryDate;

                if (expDate.HasValue && expDate.Value.Date < DateTime.UtcNow.Date)
                {
                    itemStatus = "Expired";
                }
                else
                {
                    itemStatus = "Complete";
                    completed++;
                }
            }
            else
            {
                missing++;
            }

            checklist.Items.Add(new DocumentChecklistItemDto
            {
                DocumentType = req.DocumentType,
                Description = req.Description ?? req.DocumentType,
                IsRequired = req.IsRequired,
                Status = itemStatus,
                LinkedDocumentId = docId,
                DocumentTitle = docTitle,
                DocumentNumber = docNum,
                ExpiryDate = expDate
            });
        }

        checklist.CompletedCount = completed;
        checklist.MissingCount = missing;
        checklist.Status = missing > 0 ? "Missing" : "Complete";

        return checklist;
    }

    private static DocumentDto MapToDto(Document d, DocumentVersion? curV, uint xmin)
    {
        return new DocumentDto
        {
            Id = d.Id,
            ImportCaseId = d.ImportCaseId,
            ShipmentId = d.ShipmentId,
            ShipmentContainerId = d.ShipmentContainerId,
            DocumentType = d.DocumentType,
            Title = d.Title,
            DocumentNumber = d.DocumentNumber,
            DocumentDate = d.DocumentDate,
            ExpiryDate = d.ExpiryDate,
            Status = d.Status,
            Notes = d.Notes,
            CreatedAtUtc = d.CreatedAtUtc,
            UpdatedAtUtc = d.UpdatedAtUtc,
            CreatedByUserId = d.CreatedByUserId,
            CreatedByUserName = d.CreatedByUser?.FullName,
            RowVersion = xmin,
            CurrentVersion = curV == null ? null : new DocumentVersionDto
            {
                Id = curV.Id,
                DocumentId = curV.DocumentId,
                VersionNumber = curV.VersionNumber,
                OriginalFileName = curV.OriginalFileName,
                StoredObjectKey = curV.StoredObjectKey,
                ContentType = curV.ContentType,
                FileExtension = curV.FileExtension,
                FileSizeBytes = curV.FileSizeBytes,
                Sha256Hash = curV.Sha256Hash,
                StorageStatus = curV.StorageStatus,
                IsCurrent = curV.IsCurrent,
                Status = curV.Status,
                UploadedAtUtc = curV.UploadedAtUtc,
                UploadedByUserId = curV.UploadedByUserId,
                UploadedByUserName = curV.UploadedByUser?.FullName
            }
        };
    }

    private static string Sanitize(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        return safe.Replace(" ", "_").Replace("#", "_").Replace("&", "_");
    }
}
