using System;
using System.Security.Claims;
using System.Threading.Tasks;
using ImportControlTower.Application.Models;
using ImportControlTower.Application.Services;
using ImportControlTower.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/import-cases")]
public class ImportCasesController : ControllerBase
{
    private readonly IImportCaseService _caseService;
    private readonly IIdempotencyService _idempotencyService;

    public ImportCasesController(
        IImportCaseService caseService,
        IIdempotencyService idempotencyService)
    {
        _caseService = caseService;
        _idempotencyService = idempotencyService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var id)) return id;
        throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği.");
    }

    [HttpPost]
    [Authorize(Policy = PermissionsCatalog.ImportCasesCreate)]
    public async Task<IActionResult> CreateImportCase([FromBody] CreateImportCaseDto dto)
    {
        var userId = GetUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Idempotency-Key Eksik",
                Detail = "Yeni ithalat dosyası oluşturmak için 'Idempotency-Key' HTTP başlığı zorunludur."
            });
        }

        var requestHash = _idempotencyService.ComputeRequestHash("global", dto);
        var checkResult = await _idempotencyService.CheckAndLockAsync(userId, "CreateImportCase", "global", idempotencyKey, requestHash);

        if (checkResult.IsHashMismatch)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Idempotency Key Yeniden Kullanıldı",
                Detail = "IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_REQUEST: Bu idempotency anahtarı farklı bir istek gövdesi ile kullanılmıştır."
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
            var storedObj = System.Text.Json.JsonSerializer.Deserialize<ImportCaseDetailDto>(checkResult.ResponseJson);
            return StatusCode(checkResult.ResponseStatusCode ?? 201, storedObj);
        }

        try
        {
            var result = await _caseService.CreateCaseAsync(dto, userId, HttpContext.TraceIdentifier);
            await _idempotencyService.SaveResponseAsync(checkResult.RequestId!.Value, StatusCodes.Status201Created, result);
            return CreatedAtAction(nameof(GetImportCaseById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz Veri", Detail = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = PermissionsCatalog.ImportCasesView)]
    public async Task<IActionResult> GetImportCases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? supplier = null,
        [FromQuery] string? status = null,
        [FromQuery] string? derivedStatus = null,
        [FromQuery] string? productionStatus = null,
        [FromQuery] Guid? responsibleUserId = null,
        [FromQuery] string? defaultTransportMode = null,
        [FromQuery] DateTime? etdStart = null,
        [FromQuery] DateTime? etdEnd = null,
        [FromQuery] DateTime? etaStart = null,
        [FromQuery] DateTime? etaEnd = null,
        [FromQuery] bool? delayedOnly = null,
        [FromQuery] string? sort = null)
    {
        var result = await _caseService.GetCasesAsync(
            page, pageSize, search, supplier, status, derivedStatus, productionStatus,
            responsibleUserId, defaultTransportMode, etdStart, etdEnd, etaStart, etaEnd, delayedOnly, sort);
        return Ok(result);
    }

    [HttpGet("available-suppliers")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesCreate)]
    public async Task<IActionResult> GetAvailableSuppliers([FromQuery] string? search = null)
    {
        var suppliers = await _caseService.GetAvailableSuppliersAsync(search);
        return Ok(suppliers);
    }

    [HttpGet("summary")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesView)]
    public async Task<IActionResult> GetOperationalSummary()
    {
        var summary = await _caseService.GetOperationalSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesView)]
    public async Task<IActionResult> GetImportCaseById(Guid id)
    {
        var c = await _caseService.GetCaseByIdAsync(id);
        if (c == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Dosya Bulunamadı",
                Detail = $"IMPORT_CASE_NOT_FOUND: ID'si {id} olan ithalat dosyası bulunamadı."
            });
        }
        Response.Headers["ETag"] = $"\"{c.RowVersion}\"";
        return Ok(c);
    }

    [HttpPatch("{id}")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesEdit)]
    public async Task<IActionResult> UpdateImportCase(Guid id, [FromBody] UpdateImportCaseDto dto)
    {
        var userId = GetUserId();
        var ifMatch = Request.Headers["If-Match"].ToString()?.Replace("\"", "").Trim();

        if (string.IsNullOrWhiteSpace(ifMatch))
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new ProblemDetails
            {
                Status = StatusCodes.Status428PreconditionRequired,
                Title = "If-Match Başlığı Gerekli",
                Detail = "PRECONDITION_REQUIRED: Güncelleme işlemi için 'If-Match' HTTP başlığı zorunludur."
            });
        }

        if (!uint.TryParse(ifMatch, out var expectedRowVersion))
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new ProblemDetails
            {
                Status = StatusCodes.Status412PreconditionFailed,
                Title = "Geçersiz ETag",
                Detail = "CONCURRENCY_CONFLICT: Gönderilen ETag/xmin değeri geçersizdir."
            });
        }

        try
        {
            var updated = await _caseService.UpdateCaseAsync(id, dto, userId, expectedRowVersion, HttpContext.TraceIdentifier);
            Response.Headers["ETag"] = $"\"{updated.RowVersion}\"";
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new ProblemDetails { Status = StatusCodes.Status412PreconditionFailed, Title = "Çakışma Hatası", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz İşlem", Detail = ex.Message });
        }
    }

    [HttpPost("{id}/close")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesClose)]
    public async Task<IActionResult> CloseImportCase(Guid id)
    {
        var userId = GetUserId();
        try
        {
            var closed = await _caseService.CloseCaseAsync(id, userId, HttpContext.TraceIdentifier);
            return Ok(closed);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Kapatma Engeli", Detail = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesCancel)]
    public async Task<IActionResult> CancelImportCase(Guid id)
    {
        var userId = GetUserId();
        try
        {
            var cancelled = await _caseService.CancelCaseAsync(id, userId, HttpContext.TraceIdentifier);
            return Ok(cancelled);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "İptal Engeli", Detail = ex.Message });
        }
    }

    [HttpGet("{id}/history")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesView)]
    public async Task<IActionResult> GetCaseHistory(Guid id)
    {
        var history = await _caseService.GetCaseHistoryAsync(id);
        return Ok(history);
    }

    [HttpGet("{id}/available-purchase-orders")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesAssignOrders)]
    public async Task<IActionResult> GetAvailablePurchaseOrders(Guid id, [FromQuery] string? search = null)
    {
        try
        {
            var available = await _caseService.GetAvailablePurchaseOrdersAsync(id, search);
            return Ok(available);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
    }

    [HttpPost("{id}/lines")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesAssignOrders)]
    public async Task<IActionResult> AllocateOrderLine(Guid id, [FromBody] AllocateOrderLineDto dto)
    {
        var userId = GetUserId();
        try
        {
            var line = await _caseService.AllocateOrderLineAsync(id, dto, userId, HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status201Created, line);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Tahsis Hatası", Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Geçersiz Miktar", Detail = ex.Message });
        }
    }

    [HttpPatch("{id}/lines/{lineId}")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesAssignOrders)]
    public async Task<IActionResult> UpdateOrderLineAllocation(Guid id, Guid lineId, [FromBody] UpdateImportCaseLineDto dto)
    {
        var userId = GetUserId();
        var ifMatch = Request.Headers["If-Match"].ToString()?.Replace("\"", "").Trim();

        if (string.IsNullOrWhiteSpace(ifMatch) || !uint.TryParse(ifMatch, out var expectedRowVersion))
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new ProblemDetails
            {
                Status = StatusCodes.Status428PreconditionRequired,
                Title = "If-Match Başlığı Gerekli",
                Detail = "PRECONDITION_REQUIRED: Güncelleme işlemi için 'If-Match' HTTP başlığı zorunludur."
            });
        }

        try
        {
            var updated = await _caseService.UpdateOrderLineAllocationAsync(id, lineId, dto, userId, expectedRowVersion, HttpContext.TraceIdentifier);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new ProblemDetails { Status = StatusCodes.Status412PreconditionFailed, Title = "Çakışma Hatası", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Tahsis Hatası", Detail = ex.Message });
        }
    }

    [HttpPost("{id}/lines/{lineId}/cancel")]
    [Authorize(Policy = PermissionsCatalog.ImportCasesAssignOrders)]
    public async Task<IActionResult> CancelOrderLineAllocation(Guid id, Guid lineId)
    {
        var userId = GetUserId();
        try
        {
            await _caseService.CancelOrderLineAllocationAsync(id, lineId, userId, HttpContext.TraceIdentifier);
            return Ok(new { Message = "Sipariş kalemi tahsisi başarıyla iptal edildi." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "İptal Engeli", Detail = ex.Message });
        }
    }
}
