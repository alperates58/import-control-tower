using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.DTOs;
using ImportControlTower.Application.Services;
using ImportControlTower.Domain.Constants;
using ImportControlTower.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly IIdempotencyService _idempotencyService;

    public DocumentsController(
        DocumentService documentService,
        IIdempotencyService idempotencyService)
    {
        _documentService = documentService;
        _idempotencyService = idempotencyService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var id)) return id;
        throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği.");
    }

    private uint? GetIfMatchRowVersion()
    {
        var ifMatch = Request.Headers["If-Match"].ToString();
        if (string.IsNullOrWhiteSpace(ifMatch)) return null;
        var clean = ifMatch.Replace("\"", "").Trim();
        if (uint.TryParse(clean, out var rv)) return rv;
        return null;
    }

    // 1. Case Document Upload
    [HttpPost("import-cases/{caseId}/documents")]
    [Authorize(Policy = PermissionsCatalog.DocumentsUpload)]
    [RequestSizeLimit(26_214_400)] // 25 MB
    public async Task<IActionResult> CreateCaseDocument(
        Guid caseId,
        [FromForm] CreateDocumentDto dto,
        IFormFile file)
    {
        dto.ImportCaseId = caseId;
        return await ProcessDocumentUpload(dto, file);
    }

    // 2. Shipment Document Upload
    [HttpPost("shipments/{shipmentId}/documents")]
    [Authorize(Policy = PermissionsCatalog.DocumentsUpload)]
    [RequestSizeLimit(26_214_400)]
    public async Task<IActionResult> CreateShipmentDocument(
        Guid shipmentId,
        [FromForm] CreateDocumentDto dto,
        IFormFile file)
    {
        dto.ShipmentId = shipmentId;
        return await ProcessDocumentUpload(dto, file);
    }

    // 3. Container Document Upload
    [HttpPost("containers/{containerId}/documents")]
    [Authorize(Policy = PermissionsCatalog.DocumentsUpload)]
    [RequestSizeLimit(26_214_400)]
    public async Task<IActionResult> CreateContainerDocument(
        Guid containerId,
        [FromForm] CreateDocumentDto dto,
        IFormFile file)
    {
        dto.ShipmentContainerId = containerId;
        return await ProcessDocumentUpload(dto, file);
    }

    // Shared Upload Helper
    private async Task<IActionResult> ProcessDocumentUpload(CreateDocumentDto dto, IFormFile file)
    {
        var userId = GetUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Idempotency-Key Eksik",
                Detail = "Yeni belge yüklemek için 'Idempotency-Key' HTTP başlığı zorunludur."
            });
        }

        if (file == null || file.Length == 0)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Geçersiz Dosya",
                Detail = "Yüklenen dosya boş olamaz."
            });
        }

        if (file.Length > 25 * 1024 * 1024)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "Dosya Boyutu Sınırı Aşıldı",
                Detail = "Yüklenen dosya 25 MB sınırını aşamaz."
            });
        }

        try
        {
            using var streamForHash = file.OpenReadStream();
            var valResult = await FileSecurityValidator.ValidateStreamAsync(streamForHash, file.FileName, file.ContentType);

            string rawPayload = $"{dto.Title}|{dto.DocumentType}|{valResult.Sha256Hash}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            string payloadHash = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawPayload))).ToLowerInvariant();

            var scopeKey = dto.ImportCaseId?.ToString() ?? dto.ShipmentId?.ToString() ?? dto.ShipmentContainerId?.ToString() ?? "global";
            var checkResult = await _idempotencyService.CheckAndLockAsync(userId, "CreateDocument", scopeKey, idempotencyKey, payloadHash);

            if (checkResult.IsHashMismatch)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Idempotency-Key Çakışması",
                    Detail = "Aynı Idempotency-Key farklı istek verisi ile tekrar kullanılamaz (IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_REQUEST)."
                });
            }

            if (checkResult.IsProcessingConflict)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "İşlem Devam Ediyor",
                    Detail = "IDEMPOTENCY_REQUEST_IN_PROGRESS: Bu idempotency anahtarı ile devam eden bir işlem bulunmaktadır."
                });
            }

            if (checkResult.IsCompleted && checkResult.ResponseJson != null)
            {
                var storedObj = System.Text.Json.JsonSerializer.Deserialize<DocumentDto>(checkResult.ResponseJson);
                return StatusCode(checkResult.ResponseStatusCode ?? 201, storedObj);
            }

            using var stream = file.OpenReadStream();
            var created = await _documentService.CreateDocumentAsync(dto, stream, file.FileName, file.ContentType, userId);
            await _idempotencyService.SaveResponseAsync(checkResult.RequestId!.Value, StatusCodes.Status201Created, created);

            return CreatedAtAction(nameof(GetDocumentById), new { documentId = created.Id }, created);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("STORAGE_UNAVAILABLE"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "STORAGE_UNAVAILABLE",
                Detail = "Depolama servisine (MinIO/S3) ulaşılamıyor. Lütfen daha sonra tekrar deneyiniz."
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("FILE_VALIDATION_FAILED") || ex.Message.Contains("S3_STORAGE_COPY_FAILED"))
        {
            var parts = ex.Message.Split(':');
            string errCode = parts.Length > 1 ? parts[1] : "FILE_SECURITY_ERROR";
            string errDetail = parts.Length > 2 ? parts[2] : ex.Message;

            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = errCode,
                Detail = errDetail
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DOCUMENT_DUPLICATE_HASH"))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Tekrarlanan Belge Versiyonu",
                Detail = ex.Message.Replace("DOCUMENT_DUPLICATE_HASH:", "")
            });
        }
    }

    // 4. Document Version Upload
    [HttpPost("documents/{documentId}/versions")]
    [Authorize(Policy = PermissionsCatalog.DocumentsVersion)]
    [RequestSizeLimit(26_214_400)]
    public async Task<IActionResult> AddDocumentVersion(
        Guid documentId,
        IFormFile file)
    {
        var userId = GetUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Idempotency-Key Eksik",
                Detail = "Yeni versiyon yüklemek için 'Idempotency-Key' HTTP başlığı zorunludur."
            });
        }

        if (file == null || file.Length == 0)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Geçersiz Dosya",
                Detail = "Yüklenen dosya boş olamaz."
            });
        }

        try
        {
            using var streamForHash = file.OpenReadStream();
            var valResult = await FileSecurityValidator.ValidateStreamAsync(streamForHash, file.FileName, file.ContentType);

            string rawPayload = $"{documentId}|{valResult.Sha256Hash}";
            using var shaVer = System.Security.Cryptography.SHA256.Create();
            string payloadHash = Convert.ToHexString(shaVer.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawPayload))).ToLowerInvariant();

            var checkResult = await _idempotencyService.CheckAndLockAsync(userId, "AddDocumentVersion", documentId.ToString(), idempotencyKey, payloadHash);

            if (checkResult.IsHashMismatch)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Idempotency-Key Çakışması",
                    Detail = "Aynı Idempotency-Key farklı istek verisi ile tekrar kullanılamaz (IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_REQUEST)."
                });
            }

            if (checkResult.IsProcessingConflict)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "İşlem Devam Ediyor",
                    Detail = "IDEMPOTENCY_REQUEST_IN_PROGRESS: Bu idempotency anahtarı ile devam eden bir işlem bulunmaktadır."
                });
            }

            if (checkResult.IsCompleted && checkResult.ResponseJson != null)
            {
                var storedObj = System.Text.Json.JsonSerializer.Deserialize<DocumentDto>(checkResult.ResponseJson);
                return StatusCode(checkResult.ResponseStatusCode ?? 200, storedObj);
            }

            using var stream = file.OpenReadStream();
            var updated = await _documentService.AddDocumentVersionAsync(documentId, stream, file.FileName, file.ContentType, userId);
            await _idempotencyService.SaveResponseAsync(checkResult.RequestId!.Value, StatusCodes.Status200OK, updated);

            return Ok(updated);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("STORAGE_UNAVAILABLE"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "STORAGE_UNAVAILABLE",
                Detail = "Depolama servisine (MinIO/S3) ulaşılamıyor. Lütfen daha sonra tekrar deneyiniz."
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("FILE_VALIDATION_FAILED") || ex.Message.Contains("S3_STORAGE_COPY_FAILED"))
        {
            var parts = ex.Message.Split(':');
            string errCode = parts.Length > 1 ? parts[1] : "FILE_SECURITY_ERROR";
            string errDetail = parts.Length > 2 ? parts[2] : ex.Message;

            return UnprocessableEntity(new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = errCode,
                Detail = errDetail
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DOCUMENT_DUPLICATE_HASH"))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Tekrarlanan Belge Versiyonu",
                Detail = ex.Message.Replace("DOCUMENT_DUPLICATE_HASH:", "")
            });
        }
    }

    // 5. Query / List Documents
    [HttpGet("import-cases/{caseId}/documents")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetCaseDocuments(Guid caseId)
    {
        var list = await _documentService.GetDocumentsAsync(caseId, null, null, null, null, null);
        return Ok(list);
    }

    [HttpGet("shipments/{shipmentId}/documents")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetShipmentDocuments(Guid shipmentId)
    {
        var list = await _documentService.GetDocumentsAsync(null, shipmentId, null, null, null, null);
        return Ok(list);
    }

    [HttpGet("containers/{containerId}/documents")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetContainerDocuments(Guid containerId)
    {
        var list = await _documentService.GetDocumentsAsync(null, null, containerId, null, null, null);
        return Ok(list);
    }

    [HttpGet("documents")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] Guid? importCaseId,
        [FromQuery] Guid? shipmentId,
        [FromQuery] Guid? containerId,
        [FromQuery] string? documentType,
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        var list = await _documentService.GetDocumentsAsync(importCaseId, shipmentId, containerId, documentType, status, search);
        return Ok(list);
    }

    [HttpGet("documents/{documentId}")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetDocumentById(Guid documentId)
    {
        var doc = await _documentService.GetDocumentByIdAsync(documentId);
        Response.Headers["ETag"] = $"\"{doc.RowVersion}\"";
        return Ok(doc);
    }

    [HttpGet("documents/{documentId}/versions")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetDocumentVersions(Guid documentId)
    {
        var versions = await _documentService.GetDocumentVersionsAsync(documentId);
        return Ok(versions);
    }

    // 6. Download Presigned URL Endpoints
    [HttpGet("documents/{documentId}/download")]
    [Authorize(Policy = PermissionsCatalog.DocumentsDownload)]
    public async Task<IActionResult> DownloadCurrentVersion(Guid documentId)
    {
        var userId = GetUserId();
        try
        {
            string url = await _documentService.GenerateDownloadUrlAsync(documentId, null, userId);

            Response.Headers["Cache-Control"] = "no-store, private";
            return Ok(new { downloadUrl = url, expiresMinutes = 15 });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("STORAGE_UNAVAILABLE"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "STORAGE_UNAVAILABLE",
                Detail = "Depolama servisine (MinIO/S3) ulaşılamıyor. Lütfen daha sonra tekrar deneyiniz."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "İndirme Başarısız",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("documents/{documentId}/versions/{versionId}/download")]
    [Authorize(Policy = PermissionsCatalog.DocumentsDownload)]
    public async Task<IActionResult> DownloadSpecificVersion(Guid documentId, Guid versionId)
    {
        var userId = GetUserId();
        try
        {
            string url = await _documentService.GenerateDownloadUrlAsync(documentId, versionId, userId);

            Response.Headers["Cache-Control"] = "no-store, private";
            return Ok(new { downloadUrl = url, expiresMinutes = 15 });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("STORAGE_UNAVAILABLE"))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "STORAGE_UNAVAILABLE",
                Detail = "Depolama servisine (MinIO/S3) ulaşılamıyor. Lütfen daha sonra tekrar deneyiniz."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "İndirme Başarısız",
                Detail = ex.Message
            });
        }
    }

    // 7. Update Document Metadata
    [HttpPatch("documents/{documentId}")]
    [Authorize(Policy = PermissionsCatalog.DocumentsUpload)]
    public async Task<IActionResult> UpdateDocument(Guid documentId, [FromBody] UpdateDocumentDto dto)
    {
        var userId = GetUserId();
        var rowVersion = GetIfMatchRowVersion();

        if (!rowVersion.HasValue)
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new ProblemDetails
            {
                Status = StatusCodes.Status428PreconditionRequired,
                Title = "If-Match Başlığı Eksik",
                Detail = "Belge güncellemesi yapmak için 'If-Match' HTTP başlığı zorunludur."
            });
        }

        try
        {
            var updated = await _documentService.UpdateDocumentAsync(documentId, dto, rowVersion.Value, userId);
            Response.Headers["ETag"] = $"\"{updated.RowVersion}\"";
            return Ok(updated);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new ProblemDetails
            {
                Status = StatusCodes.Status412PreconditionFailed,
                Title = "Eşzamanlılık Çakışması",
                Detail = "Belge başka bir kullanıcı tarafından güncellendi (CONCURRENCY_CONFLICT)."
            });
        }
    }

    // 8. Cancel Document
    [HttpPost("documents/{documentId}/cancel")]
    [Authorize(Policy = PermissionsCatalog.DocumentsCancel)]
    public async Task<IActionResult> CancelDocument(Guid documentId)
    {
        var userId = GetUserId();
        var rowVersion = GetIfMatchRowVersion();

        if (!rowVersion.HasValue)
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new ProblemDetails
            {
                Status = StatusCodes.Status428PreconditionRequired,
                Title = "If-Match Başlığı Eksik",
                Detail = "Belge iptali yapmak için 'If-Match' HTTP başlığı zorunludur."
            });
        }

        try
        {
            var cancelled = await _documentService.CancelDocumentAsync(documentId, rowVersion.Value, userId);
            Response.Headers["ETag"] = $"\"{cancelled.RowVersion}\"";
            return Ok(cancelled);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new ProblemDetails
            {
                Status = StatusCodes.Status412PreconditionFailed,
                Title = "Eşzamanlılık Çakışması",
                Detail = "Belge başka bir kullanıcı tarafından güncellendi (CONCURRENCY_CONFLICT)."
            });
        }
    }

    // 9. Checklists
    [HttpGet("import-cases/{caseId}/document-checklist")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetCaseDocumentChecklist(Guid caseId)
    {
        var checklist = await _documentService.GetDocumentChecklistAsync("ImportCase", caseId);
        return Ok(checklist);
    }

    [HttpGet("shipments/{shipmentId}/document-checklist")]
    [Authorize(Policy = PermissionsCatalog.DocumentsView)]
    public async Task<IActionResult> GetShipmentDocumentChecklist(Guid shipmentId)
    {
        var checklist = await _documentService.GetDocumentChecklistAsync("Shipment", shipmentId);
        return Ok(checklist);
    }
}
