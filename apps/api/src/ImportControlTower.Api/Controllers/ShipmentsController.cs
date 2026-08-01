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
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly IIdempotencyService _idempotencyService;

    public ShipmentsController(
        IShipmentService shipmentService,
        IIdempotencyService idempotencyService)
    {
        _shipmentService = shipmentService;
        _idempotencyService = idempotencyService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var id)) return id;
        throw new UnauthorizedAccessException("Geçersiz kullanıcı kimliği.");
    }

    [HttpGet("api/v1/import-cases/{caseId}/shipments")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsView)]
    public async Task<IActionResult> GetShipmentsByCaseId(Guid caseId)
    {
        var shipments = await _shipmentService.GetShipmentsByCaseIdAsync(caseId);
        return Ok(shipments);
    }

    [HttpPost("api/v1/import-cases/{caseId}/shipments")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsCreate)]
    public async Task<IActionResult> CreateShipment(Guid caseId, [FromBody] CreateShipmentDto dto)
    {
        var userId = GetUserId();
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Idempotency-Key Eksik",
                Detail = "Yeni sevkiyat oluşturmak için 'Idempotency-Key' HTTP başlığı zorunludur."
            });
        }

        var scopeKey = caseId.ToString();
        var requestHash = _idempotencyService.ComputeRequestHash(scopeKey, dto);
        var checkResult = await _idempotencyService.CheckAndLockAsync(userId, "CreateShipment", scopeKey, idempotencyKey, requestHash);

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
            var storedObj = System.Text.Json.JsonSerializer.Deserialize<ShipmentDetailDto>(checkResult.ResponseJson);
            return StatusCode(checkResult.ResponseStatusCode ?? 201, storedObj);
        }

        try
        {
            var result = await _shipmentService.CreateShipmentAsync(caseId, dto, userId, HttpContext.TraceIdentifier);
            await _idempotencyService.SaveResponseAsync(checkResult.RequestId!.Value, StatusCodes.Status201Created, result);
            return CreatedAtAction(nameof(GetShipmentById), new { shipmentId = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz Veri", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz İşlem", Detail = ex.Message });
        }
    }

    [HttpGet("api/v1/shipments/{shipmentId}")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsView)]
    public async Task<IActionResult> GetShipmentById(Guid shipmentId)
    {
        var s = await _shipmentService.GetShipmentByIdAsync(shipmentId);
        if (s == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Sevkiyat Bulunamadı",
                Detail = $"SHIPMENT_NOT_FOUND: ID'si {shipmentId} olan sevkiyat bulunamadı."
            });
        }
        Response.Headers["ETag"] = $"\"{s.RowVersion}\"";
        return Ok(s);
    }

    [HttpPatch("api/v1/shipments/{shipmentId}")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsEdit)]
    public async Task<IActionResult> UpdateShipment(Guid shipmentId, [FromBody] UpdateShipmentDto dto)
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
            var updated = await _shipmentService.UpdateShipmentAsync(shipmentId, dto, userId, expectedRowVersion, HttpContext.TraceIdentifier);
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
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz Veri", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz İşlem", Detail = ex.Message });
        }
    }

    [HttpPost("api/v1/shipments/{shipmentId}/cancel")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsCancel)]
    public async Task<IActionResult> CancelShipment(Guid shipmentId)
    {
        var userId = GetUserId();
        try
        {
            var cancelled = await _shipmentService.CancelShipmentAsync(shipmentId, userId, HttpContext.TraceIdentifier);
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

    [HttpPost("api/v1/shipments/{shipmentId}/abort")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsCancel)]
    public async Task<IActionResult> AbortShipment(Guid shipmentId, [FromBody] AbortShipmentDto dto)
    {
        var userId = GetUserId();
        var ifMatch = Request.Headers["If-Match"].ToString()?.Replace("\"", "").Trim();

        if (string.IsNullOrWhiteSpace(ifMatch) || !uint.TryParse(ifMatch, out var expectedRowVersion))
        {
            return StatusCode(StatusCodes.Status428PreconditionRequired, new ProblemDetails
            {
                Status = StatusCodes.Status428PreconditionRequired,
                Title = "If-Match Başlığı Gerekli",
                Detail = "PRECONDITION_REQUIRED: Abort işlemi için 'If-Match' HTTP başlığı zorunludur."
            });
        }

        try
        {
            var aborted = await _shipmentService.AbortShipmentAsync(shipmentId, dto, userId, expectedRowVersion);
            return Ok(aborted);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new ProblemDetails { Status = StatusCodes.Status412PreconditionFailed, Title = "Çakışma Hatası", Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz Gerekçe", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Abort Engeli", Detail = ex.Message });
        }
    }

    [HttpGet("api/v1/shipments/{shipmentId}/lines")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsView)]
    public async Task<IActionResult> GetShipmentLines(Guid shipmentId)
    {
        try
        {
            var lines = await _shipmentService.GetShipmentLinesAsync(shipmentId);
            return Ok(lines);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
    }

    [HttpPost("api/v1/shipments/{shipmentId}/lines")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsEdit)]
    public async Task<IActionResult> AllocateShipmentLine(Guid shipmentId, [FromBody] AllocateShipmentLineDto dto)
    {
        var userId = GetUserId();
        try
        {
            var allocation = await _shipmentService.AllocateShipmentLineAsync(shipmentId, dto, userId, HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status201Created, allocation);
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

    [HttpPatch("api/v1/shipments/{shipmentId}/lines/{allocationId}")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsEdit)]
    public async Task<IActionResult> UpdateShipmentLineAllocation(Guid shipmentId, Guid allocationId, [FromBody] UpdateShipmentLineAllocationDto dto)
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
            var updated = await _shipmentService.UpdateShipmentLineAllocationAsync(shipmentId, allocationId, dto, userId, expectedRowVersion, HttpContext.TraceIdentifier);
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

    [HttpPost("api/v1/shipments/{shipmentId}/lines/{allocationId}/cancel")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsEdit)]
    public async Task<IActionResult> CancelShipmentLineAllocation(Guid shipmentId, Guid allocationId)
    {
        var userId = GetUserId();
        try
        {
            await _shipmentService.CancelShipmentLineAllocationAsync(shipmentId, allocationId, userId, HttpContext.TraceIdentifier);
            return Ok(new { Message = "Sevkiyat tahsis kaydı başarıyla iptal edildi." });
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

    [HttpPost("api/v1/shipments/{shipmentId}/containers")]
    [Authorize(Policy = PermissionsCatalog.ContainersEdit)]
    public async Task<IActionResult> AddContainer(Guid shipmentId, [FromBody] AddContainerDto dto)
    {
        var userId = GetUserId();
        try
        {
            var container = await _shipmentService.AddContainerAsync(shipmentId, dto, userId, HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status201Created, container);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Konteyner Doğrulama Hatası", Detail = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.StartsWith("CONTAINER_NOT_ALLOWED_FOR_TRANSPORT_MODE"))
            {
                return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Mod Hatası", Detail = ex.Message });
            }
            return StatusCode(StatusCodes.Status409Conflict, new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Çakışma Hatası", Detail = ex.Message });
        }
    }

    [HttpPatch("api/v1/shipments/{shipmentId}/containers/{containerId}")]
    [Authorize(Policy = PermissionsCatalog.ContainersEdit)]
    public async Task<IActionResult> UpdateContainer(Guid shipmentId, Guid containerId, [FromBody] UpdateContainerDto dto)
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
            var updated = await _shipmentService.UpdateContainerAsync(shipmentId, containerId, dto, userId, expectedRowVersion, HttpContext.TraceIdentifier);
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
    }

    [HttpPost("api/v1/shipments/{shipmentId}/containers/{containerId}/cancel")]
    [Authorize(Policy = PermissionsCatalog.ContainersEdit)]
    public async Task<IActionResult> CancelContainer(Guid shipmentId, Guid containerId)
    {
        var userId = GetUserId();
        try
        {
            await _shipmentService.CancelContainerAsync(shipmentId, containerId, userId, HttpContext.TraceIdentifier);
            return Ok(new { Message = "Konteyner kaydı başarıyla iptal edildi." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
    }

    [HttpGet("api/v1/shipments/{shipmentId}/milestones")]
    [Authorize(Policy = PermissionsCatalog.ShipmentsView)]
    public async Task<IActionResult> GetMilestones(Guid shipmentId)
    {
        try
        {
            var milestones = await _shipmentService.GetMilestonesAsync(shipmentId);
            return Ok(milestones);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
    }

    [HttpPost("api/v1/shipments/{shipmentId}/milestones")]
    [Authorize(Policy = PermissionsCatalog.MilestonesEdit)]
    public async Task<IActionResult> CreateMilestone(Guid shipmentId, [FromBody] CreateMilestoneDto dto)
    {
        var userId = GetUserId();
        try
        {
            var milestone = await _shipmentService.CreateMilestoneAsync(shipmentId, dto, userId, HttpContext.TraceIdentifier);
            return StatusCode(StatusCodes.Status201Created, milestone);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Bulunamadı", Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz Veri", Detail = ex.Message });
        }
    }

    [HttpPatch("api/v1/shipments/{shipmentId}/milestones/{milestoneId}")]
    [Authorize(Policy = PermissionsCatalog.MilestonesEdit)]
    public async Task<IActionResult> UpdateMilestone(Guid shipmentId, Guid milestoneId, [FromBody] UpdateMilestoneDto dto)
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
            var updated = await _shipmentService.UpdateMilestoneAsync(shipmentId, milestoneId, dto, userId, expectedRowVersion, HttpContext.TraceIdentifier);
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
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, Title = "Geçersiz Veri", Detail = ex.Message });
        }
    }
}
