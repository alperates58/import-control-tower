using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Constants;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/purchase-order-imports")]
public class PurchaseOrderImportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IExcelParserService _excelParser;
    private readonly IAuditLogService _auditLog;

    public PurchaseOrderImportsController(
        ApplicationDbContext db,
        IExcelParserService excelParser,
        IAuditLogService auditLog)
    {
        _db = db;
        _excelParser = excelParser;
        _auditLog = auditLog;
    }

    private Guid GetUserId()
    {
        var subClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(subClaim, out var userId) ? userId : Guid.Empty;
    }

    private string GetUsername()
    {
        return User.Identity?.Name ?? "system";
    }

    [HttpPost("upload")]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersImport)]
    [EnableRateLimiting("upload-policy")]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dosya Seçilmedi",
                Detail = "Lütfen yüklemek için geçerli bir .xlsx dosyası seçiniz."
            });
        }

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new ProblemDetails
            {
                Status = StatusCodes.Status415UnsupportedMediaType,
                Title = "Desteklenmeyen Dosya Türü",
                Detail = "Yalnızca .xlsx uzantılı Excel dosyaları yüklenebilir."
            });
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "Dosya Çok Büyük",
                Detail = "Yüklenen dosya 10 MB sınırını aşıyor."
            });
        }

        // Read file bytes into memory
        using var fileStream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        // Calculate SHA-256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileBytes);
        var fileSha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Deterministic 2-key 64-bit PostgreSQL Advisory Lock
        int lockKey1 = BitConverter.ToInt32(hashBytes, 0);
        int lockKey2 = BitConverter.ToInt32(hashBytes, 4);

        var userId = GetUserId();
        var sanitizedFileName = Path.GetFileName(file.FileName);

        using var lockTx = await _db.Database.BeginTransactionAsync();
        try
        {
            var lockAcquired = await _db.Database.SqlQueryRaw<bool>(
                "SELECT pg_try_advisory_xact_lock({0}, {1}) AS \"Value\"", lockKey1, lockKey2)
                .SingleOrDefaultAsync();

            if (!lockAcquired)
            {
                await lockTx.RollbackAsync();
                return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "İşlem Kilitli",
                    Detail = "Aynı dosya başka bir istek tarafından işleniyor. Lütfen bekleyiniz."
                });
            }

            // Re-check existing hash status inside lock
            var existingBatch = await _db.ImportBatches
                .AsNoTracking()
                .Where(b => b.FileSha256 == fileSha256)
                .OrderByDescending(b => b.StartedAtUtc)
                .FirstOrDefaultAsync();

            if (existingBatch != null)
            {
                if (existingBatch.Status == "Completed")
                {
                    await lockTx.RollbackAsync();
                    return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Mükerrer Aktarım",
                        Detail = $"Bu dosya daha önce başarıyla aktarılmıştır (Batch ID: {existingBatch.Id}).",
                        Extensions = { ["batchId"] = existingBatch.Id }
                    });
                }
                else if (existingBatch.Status is "Uploaded" or "Parsing" or "MappingRequired" or "Validating" or "ReadyForConfirmation" or "Importing")
                {
                    await lockTx.RollbackAsync();
                    return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Devam Eden Aktarım",
                        Detail = $"Bu dosya için devam eden bir içe aktarım mevcuttur (Batch ID: {existingBatch.Id}).",
                        Extensions = { ["batchId"] = existingBatch.Id }
                    });
                }
            }

            // Run Excel Parser
            ms.Position = 0;
            var parseResult = await _excelParser.ParseAndValidateAsync(ms, sanitizedFileName);

            if (parseResult.SecurityErrors.Count > 0)
            {
                await lockTx.RollbackAsync();
                return UnprocessableEntity(new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Güvenlik / Yapısal Dosya Hatası",
                    Detail = string.Join("; ", parseResult.SecurityErrors)
                });
            }

            // Determine Batch Status
            string batchStatus;
            if (parseResult.MissingRequiredColumns.Count > 0 || parseResult.AutoColumnMapping.Values.Contains("AMBIGUOUS"))
            {
                batchStatus = "MappingRequired";
            }
            else if (parseResult.Rows.Any(r => r.ErrorCodes.Count > 0))
            {
                batchStatus = "ValidationFailed";
            }
            else
            {
                batchStatus = "ReadyForConfirmation";
            }

            var correlationId = Guid.NewGuid().ToString();

            var batch = new ImportBatch
            {
                Id = Guid.NewGuid(),
                OriginalFileName = sanitizedFileName,
                FileSha256 = fileSha256,
                FileSizeBytes = file.Length,
                TotalRowCount = parseResult.Rows.Count,
                ValidRowCount = parseResult.Rows.Count(r => r.ErrorCodes.Count == 0),
                InvalidRowCount = parseResult.Rows.Count(r => r.ErrorCodes.Count > 0),
                WarningRowCount = parseResult.Rows.Count(r => r.WarningCodes.Count > 0),
                Status = batchStatus,
                StartedAtUtc = DateTime.UtcNow,
                UploadedByUserId = userId,
                CorrelationId = correlationId,
                ParserVersion = "v1.0",
                TemplateVersion = "v1.0"
            };

            _db.ImportBatches.Add(batch);

            foreach (var r in parseResult.Rows)
            {
                var valStatus = r.ErrorCodes.Count > 0 ? "Error" : (r.WarningCodes.Count > 0 ? "Warning" : "Valid");
                var action = r.ErrorCodes.Count > 0 ? "Invalid" : "CreateOrder";

                var rowEntity = new ImportBatchRow
                {
                    Id = Guid.NewGuid(),
                    ImportBatchId = batch.Id,
                    RowNumber = r.RowNumber,
                    RawDataJson = JsonSerializer.Serialize(r.RawValues),
                    NormalizedDataJson = JsonSerializer.Serialize(r.NormalizedValues),
                    ValidationStatus = valStatus,
                    ErrorCodesJson = r.ErrorCodes.Count > 0 ? JsonSerializer.Serialize(r.ErrorCodes) : null,
                    WarningCodesJson = r.WarningCodes.Count > 0 ? JsonSerializer.Serialize(r.WarningCodes) : null,
                    ImportAction = action,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.ImportBatchRows.Add(rowEntity);
            }

            await _db.SaveChangesAsync();
            await lockTx.CommitAsync();

            await _auditLog.LogAsync(
                action: "PurchaseOrderImport.Uploaded",
                entityType: "ImportBatch",
                entityId: batch.Id.ToString(),
                actorUserId: userId,
                actorUsername: GetUsername(),
                actorType: "User",
                metadata: new
                {
                    batchId = batch.Id,
                    fileName = sanitizedFileName,
                    fileHashShort = fileSha256.Substring(0, 8),
                    totalRows = batch.TotalRowCount,
                    status = batch.Status,
                    correlationId
                });

            var summary = new ImportBatchSummaryDto(
                batch.Id,
                batch.OriginalFileName,
                batch.FileSha256,
                batch.FileSizeBytes,
                batch.TotalRowCount,
                batch.ValidRowCount,
                batch.InvalidRowCount,
                batch.WarningRowCount,
                batch.ImportedOrderCount,
                batch.ImportedLineCount,
                batch.Status,
                batch.StartedAtUtc,
                batch.CompletedAtUtc,
                batch.UploadedByUserId,
                GetUsername(),
                batch.ConfirmedByUserId,
                null,
                batch.CorrelationId,
                batch.FailureReason
            );

            var detail = new ImportBatchDetailDto(
                summary,
                parseResult.AutoColumnMapping,
                parseResult.UnmappedColumns,
                parseResult.MissingRequiredColumns
            );

            return Created($"/api/v1/purchase-order-imports/{batch.Id}", detail);
        }
        catch (Exception)
        {
            await lockTx.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "İçe Aktarma Hatası",
                Detail = "Excel dosyası işlenirken sunucu tarafında bir hata oluştu."
            });
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersImport)]
    public async Task<IActionResult> GetBatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _db.ImportBatches
            .Include(b => b.UploadedByUser)
            .Include(b => b.ConfirmedByUser)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new ImportBatchSummaryDto(
                b.Id,
                b.OriginalFileName,
                b.FileSha256,
                b.FileSizeBytes,
                b.TotalRowCount,
                b.ValidRowCount,
                b.InvalidRowCount,
                b.WarningRowCount,
                b.ImportedOrderCount,
                b.ImportedLineCount,
                b.Status,
                b.StartedAtUtc,
                b.CompletedAtUtc,
                b.UploadedByUserId,
                b.UploadedByUser != null ? b.UploadedByUser.FullName : null,
                b.ConfirmedByUserId,
                b.ConfirmedByUser != null ? b.ConfirmedByUser.FullName : null,
                b.CorrelationId,
                b.FailureReason
            ))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new PagedResultDto<ImportBatchSummaryDto>(items, totalCount, page, pageSize, totalPages));
    }

    [HttpGet("{batchId}")]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersImport)]
    public async Task<IActionResult> GetBatchDetail(Guid batchId)
    {
        var batch = await _db.ImportBatches
            .Include(b => b.UploadedByUser)
            .Include(b => b.ConfirmedByUser)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Batch Bulunamadı",
                Detail = $"ID'si {batchId} olan içe aktarma kaydı bulunamadı."
            });
        }

        var summary = new ImportBatchSummaryDto(
            batch.Id,
            batch.OriginalFileName,
            batch.FileSha256,
            batch.FileSizeBytes,
            batch.TotalRowCount,
            batch.ValidRowCount,
            batch.InvalidRowCount,
            batch.WarningRowCount,
            batch.ImportedOrderCount,
            batch.ImportedLineCount,
            batch.Status,
            batch.StartedAtUtc,
            batch.CompletedAtUtc,
            batch.UploadedByUserId,
            batch.UploadedByUser?.FullName,
            batch.ConfirmedByUserId,
            batch.ConfirmedByUser?.FullName,
            batch.CorrelationId,
            batch.FailureReason
        );

        var detail = new ImportBatchDetailDto(summary, new(), new(), new());
        return Ok(detail);
    }

    [HttpGet("{batchId}/rows")]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersImport)]
    public async Task<IActionResult> GetBatchRows(
        Guid batchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 500) pageSize = 50;

        var query = _db.ImportBatchRows
            .AsNoTracking()
            .Where(r => r.ImportBatchId == batchId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.ValidationStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.RawDataJson.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var rows = await query
            .OrderBy(r => r.RowNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = rows.Select(r => new ImportBatchRowDto(
            r.Id,
            r.ImportBatchId,
            r.RowNumber,
            r.RawDataJson,
            r.NormalizedDataJson,
            r.ValidationStatus,
            !string.IsNullOrEmpty(r.ErrorCodesJson) ? JsonSerializer.Deserialize<List<string>>(r.ErrorCodesJson) ?? new() : new(),
            !string.IsNullOrEmpty(r.WarningCodesJson) ? JsonSerializer.Deserialize<List<string>>(r.WarningCodesJson) ?? new() : new(),
            r.MatchedOrderId,
            r.MatchedLineId,
            r.ImportAction,
            r.CreatedAtUtc
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new PagedResultDto<ImportBatchRowDto>(dtos, totalCount, page, pageSize, totalPages));
    }

    [HttpPost("{batchId}/confirm")]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersImport)]
    public async Task<IActionResult> Confirm(Guid batchId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Idempotency-Key Eksik",
                Detail = "Onaylama işlemi için HTTP header içinde 'Idempotency-Key' zorunludur."
            });
        }

        var userId = GetUserId();

        // 1. Check idempotency request table
        var existingReq = await _db.ImportConfirmationRequests
            .FirstOrDefaultAsync(r => r.ImportBatchId == batchId && r.IdempotencyKey == idempotencyKey);

        if (existingReq != null)
        {
            if (existingReq.Status == "Completed" && existingReq.ResponseStatusCode.HasValue)
            {
                if (!string.IsNullOrEmpty(existingReq.ResponseJson))
                {
                    var cachedResp = JsonSerializer.Deserialize<ConfirmImportResponseDto>(existingReq.ResponseJson);
                    return StatusCode(existingReq.ResponseStatusCode.Value, cachedResp);
                }
            }
            else if (existingReq.Status == "Processing")
            {
                return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "İşlem Devam Ediyor",
                    Detail = "Bu onay isteği şu anda işlenmektedir."
                });
            }
        }

        // 2. Create processing confirmation request record
        var confirmReq = new ImportConfirmationRequest
        {
            Id = Guid.NewGuid(),
            ImportBatchId = batchId,
            IdempotencyKey = idempotencyKey,
            Status = "Processing",
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ImportConfirmationRequests.Add(confirmReq);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Concurrent same batch + key
            return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Eşzamanlı İstek",
                Detail = "Aynı onay isteği eşzamanlı olarak gönderildi."
            });
        }

        // 3. Main Transaction Execution
        using var mainTx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Row lock import batch with SELECT FOR UPDATE
            var batch = await _db.ImportBatches
                .FromSqlRaw("SELECT * FROM import_batches WHERE \"Id\" = {0} FOR UPDATE", batchId)
                .SingleOrDefaultAsync();

            if (batch == null)
            {
                await mainTx.RollbackAsync();
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Batch Bulunamadı",
                    Detail = $"ID'si {batchId} olan aktarım kaydı bulunamadı."
                });
            }

            if (batch.Status != "ReadyForConfirmation")
            {
                await mainTx.RollbackAsync();
                return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "İşlem Durumu Geçersiz",
                    Detail = $"Bu aktarım kaydı onaylanabilir durumda değil (Mevcut Durum: {batch.Status})."
                });
            }

            batch.Status = "Importing";
            await _db.SaveChangesAsync();

            // Load all rows
            var rows = await _db.ImportBatchRows
                .Where(r => r.ImportBatchId == batchId)
                .OrderBy(r => r.RowNumber)
                .ToListAsync();

            if (rows.Any(r => r.ValidationStatus == "Error"))
            {
                await mainTx.RollbackAsync();
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Hatalı Satırlar Mevcut",
                    Detail = "Hatalı satır içeren aktarım dosyaları onaylanamaz."
                });
            }

            // Load existing POs into memory dictionary for fast lookup
            var existingPos = await _db.PurchaseOrders
                .Include(po => po.Lines)
                .ToListAsync();

            var poDict = existingPos.ToDictionary(
                po => $"{po.NormalizedOrderNumber}|{po.NormalizedSupplierName}",
                StringComparer.OrdinalIgnoreCase);

            int createdOrderCount = 0;
            int createdLineCount = 0;
            int skippedDuplicateCount = 0;

            // Group rows by NormalizedOrderNumber + NormalizedSupplierName
            var parsedRowsData = rows.Select(r => new
            {
                Row = r,
                Norm = !string.IsNullOrEmpty(r.NormalizedDataJson)
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(r.NormalizedDataJson) ?? new()
                    : new()
            }).ToList();

            var groupedOrders = parsedRowsData
                .Where(x => x.Norm.ContainsKey("NormalizedOrderNumber") && x.Norm.ContainsKey("NormalizedSupplierName"))
                .GroupBy(x => $"{x.Norm["NormalizedOrderNumber"]}|{x.Norm["NormalizedSupplierName"]}");

            foreach (var group in groupedOrders)
            {
                var firstRow = group.First();
                var normOrderNum = firstRow.Norm["NormalizedOrderNumber"];
                var normSuppName = firstRow.Norm["NormalizedSupplierName"];
                var rawOrderNum = firstRow.Norm.GetValueOrDefault("OrderNumber", normOrderNum);
                var rawSuppName = firstRow.Norm.GetValueOrDefault("SupplierName", normSuppName);
                var orderDateStr = firstRow.Norm.GetValueOrDefault("OrderDate", string.Empty);

                if (!DateTime.TryParse(orderDateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var orderDate))
                {
                    orderDate = DateTime.UtcNow;
                }
                else
                {
                    orderDate = DateTime.SpecifyKind(orderDate, DateTimeKind.Utc);
                }

                // Check group header consistency
                foreach (var item in group)
                {
                    var itemOrderDate = item.Norm.GetValueOrDefault("OrderDate", orderDateStr);
                    if (!string.Equals(itemOrderDate, orderDateStr, StringComparison.OrdinalIgnoreCase))
                    {
                        await mainTx.RollbackAsync();
                        return UnprocessableEntity(new ProblemDetails
                        {
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Title = "Sipariş Başlık Çakışması",
                            Detail = $"Sipariş No '{rawOrderNum}' için farklı satırlarda farklı Sipariş Tarihleri tespit edildi."
                        });
                    }
                }

                // Find or create PurchaseOrder
                var groupKey = $"{normOrderNum}|{normSuppName}";
                if (!poDict.TryGetValue(groupKey, out var po))
                {
                    po = new PurchaseOrder
                    {
                        Id = Guid.NewGuid(),
                        OrderNumber = rawOrderNum,
                        NormalizedOrderNumber = normOrderNum,
                        SupplierName = rawSuppName,
                        NormalizedSupplierName = normSuppName,
                        OrderDate = orderDate,
                        Status = "Open",
                        Source = "ExcelImport",
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = userId
                    };
                    _db.PurchaseOrders.Add(po);
                    poDict[groupKey] = po;
                    createdOrderCount++;
                }

                int lineNumber = po.Lines.Count > 0 ? po.Lines.Max(l => l.LineNumber) : 0;

                foreach (var item in group)
                {
                    var normStockCode = item.Norm.GetValueOrDefault("NormalizedStockCode", string.Empty);
                    var rawStockCode = item.Norm.GetValueOrDefault("StockCode", normStockCode);
                    var rawStockName = item.Norm.GetValueOrDefault("StockName", string.Empty);

                    decimal.TryParse(item.Norm.GetValueOrDefault("OrderedQuantity", "0"), CultureInfo.InvariantCulture, out var ordQty);
                    decimal.TryParse(item.Norm.GetValueOrDefault("RemainingQuantity", "0"), CultureInfo.InvariantCulture, out var remQty);

                    DateTime? sasDate = null;
                    if (item.Norm.TryGetValue("SasDate", out var sasStr) && DateTime.TryParse(sasStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedSas))
                    {
                        sasDate = DateTime.SpecifyKind(parsedSas, DateTimeKind.Utc);
                    }

                    var existingLine = po.Lines.FirstOrDefault(l => string.Equals(l.NormalizedStockCode, normStockCode, StringComparison.OrdinalIgnoreCase));

                    if (existingLine != null)
                    {
                        // Check exact duplicate vs conflict
                        if (existingLine.OrderedQuantity == ordQty && existingLine.RemainingQuantity == remQty)
                        {
                            item.Row.ImportAction = "SkipDuplicate";
                            item.Row.MatchedOrderId = po.Id;
                            item.Row.MatchedLineId = existingLine.Id;
                            skippedDuplicateCount++;
                        }
                        else
                        {
                            await mainTx.RollbackAsync();
                            return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails
                            {
                                Status = StatusCodes.Status409Conflict,
                                Title = "Mevcut Kayıt Çakışması",
                                Detail = $"Sipariş '{rawOrderNum}', Stok '{rawStockCode}' veritabanında farklı miktar ile mevcut. Üzerine yazma yapılmaz."
                            });
                        }
                    }
                    else
                    {
                        lineNumber++;
                        var line = new PurchaseOrderLine
                        {
                            Id = Guid.NewGuid(),
                            PurchaseOrderId = po.Id,
                            LineNumber = lineNumber,
                            StockCode = rawStockCode,
                            NormalizedStockCode = normStockCode,
                            StockName = rawStockName,
                            OrderedQuantity = ordQty,
                            RemainingQuantity = remQty,
                            SasDate = sasDate,
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow
                        };

                        po.Lines.Add(line);
                        _db.PurchaseOrderLines.Add(line);

                        item.Row.ImportAction = "CreateLine";
                        item.Row.MatchedOrderId = po.Id;
                        item.Row.MatchedLineId = line.Id;
                        createdLineCount++;
                    }
                }
            }

            // Update batch and request status
            batch.Status = "Completed";
            batch.ImportedOrderCount = createdOrderCount;
            batch.ImportedLineCount = createdLineCount;
            batch.CompletedAtUtc = DateTime.UtcNow;
            batch.ConfirmedByUserId = userId;

            var responseDto = new ConfirmImportResponseDto(
                batch.Id,
                "Completed",
                createdOrderCount,
                createdLineCount,
                skippedDuplicateCount,
                batch.CompletedAtUtc.Value
            );

            confirmReq.Status = "Completed";
            confirmReq.ResponseStatusCode = StatusCodes.Status200OK;
            confirmReq.ResponseJson = JsonSerializer.Serialize(responseDto);
            confirmReq.CompletedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await mainTx.CommitAsync();

            await _auditLog.LogAsync(
                action: "PurchaseOrderImport.Confirmed",
                entityType: "ImportBatch",
                entityId: batch.Id.ToString(),
                actorUserId: userId,
                actorUsername: GetUsername(),
                actorType: "User",
                metadata: new
                {
                    batchId = batch.Id,
                    createdOrders = createdOrderCount,
                    createdLines = createdLineCount,
                    skippedDuplicates = skippedDuplicateCount,
                    correlationId = batch.CorrelationId
                });

            return Ok(responseDto);
        }
        catch (Exception)
        {
            await mainTx.RollbackAsync();

            // Execute separate brief transaction to set batch & request status to Failed
            using var failureTx = await _db.Database.BeginTransactionAsync();
            try
            {
                var batchToFail = await _db.ImportBatches.FindAsync(batchId);
                if (batchToFail != null)
                {
                    batchToFail.Status = "Failed";
                    batchToFail.FailureReason = "CONFIRMATION_TRANSACTION_FAILED";
                }

                var reqToFail = await _db.ImportConfirmationRequests
                    .FirstOrDefaultAsync(r => r.ImportBatchId == batchId && r.IdempotencyKey == idempotencyKey);
                if (reqToFail != null)
                {
                    reqToFail.Status = "Failed";
                }

                await _db.SaveChangesAsync();
                await failureTx.CommitAsync();

                await _auditLog.LogAsync(
                    action: "PurchaseOrderImport.Failed",
                    entityType: "ImportBatch",
                    entityId: batchId.ToString(),
                    actorUserId: userId,
                    actorUsername: GetUsername(),
                    actorType: "User",
                    metadata: new { batchId, failureReason = "CONFIRMATION_TRANSACTION_FAILED" });
            }
            catch { }

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Onaylama Hatası",
                Detail = "Siparişler içe aktarılırken bir veritabanı hatası oluştu. Tüm veriler geri alındı."
            });
        }
    }

    [HttpPost("{batchId}/cancel")]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersImport)]
    public async Task<IActionResult> Cancel(Guid batchId)
    {
        var batch = await _db.ImportBatches.FindAsync(batchId);

        if (batch == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Batch Bulunamadı",
                Detail = $"ID'si {batchId} olan aktarım kaydı bulunamadı."
            });
        }

        var userId = GetUserId();
        bool isSystemAdmin = User.IsInRole("SystemAdmin");

        if (batch.UploadedByUserId != userId && !isSystemAdmin)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Yetkisiz İptal",
                Detail = "Yalnızca yükleyen kullanıcı veya SystemAdmin aktarım kaydını iptal edebilir."
            });
        }

        // Allowed state transitions to Cancelled
        string[] allowedStates = { "Uploaded", "MappingRequired", "ValidationFailed", "ReadyForConfirmation" };
        if (!allowedStates.Contains(batch.Status))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Geçersiz İptal İsteği",
                Detail = $"'{batch.Status}' durumundaki aktarım kayıtları iptal edilemez."
            });
        }

        batch.Status = "Cancelled";
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(
            action: "PurchaseOrderImport.Cancelled",
            entityType: "ImportBatch",
            entityId: batch.Id.ToString(),
            actorUserId: userId,
            actorUsername: GetUsername(),
            actorType: "User",
            metadata: new { batchId = batch.Id, cancelledBy = GetUsername() });

        return Ok(new { message = "İçerik aktarımı başarıyla iptal edildi." });
    }

    [HttpGet("template")]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersImport)]
    public IActionResult DownloadTemplate()
    {
        var fileBytes = _excelParser.GenerateTemplateWorkbook();
        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "purchase-order-import-template.xlsx"
        );
    }
}
