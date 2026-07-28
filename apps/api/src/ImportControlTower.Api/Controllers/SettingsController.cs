using System.Security.Claims;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.Models;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/admin/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IMemoryCache _cache;

    public SettingsController(
        ApplicationDbContext dbContext,
        IAuditLogService auditLogService,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _cache = cache;
    }

    [HttpGet]
    [Authorize(Policy = "settings.view")]
    [ProducesResponseType(typeof(IEnumerable<SystemSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.SystemSettings.AsNoTracking().ToListAsync(cancellationToken);
        var result = settings.Select(s => new SystemSettingDto(
            s.Key,
            s.IsSensitive ? "********" : s.Value,
            s.ValueType,
            s.Description,
            s.IsSensitive,
            s.UpdatedAtUtc,
            s.UpdatedByUserId
        ));

        return Ok(result);
    }

    [HttpPut("{key}")]
    [Authorize(Policy = "settings.manage")]
    [ProducesResponseType(typeof(SystemSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        var setting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting == null) return NotFound(new { detail = "Ayar bulunamadı." });

        setting.Value = request.Value;
        setting.UpdatedAtUtc = DateTime.UtcNow;
        setting.UpdatedByUserId = GetCurrentUserId();

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Immediate Cache Invalidation
        _cache.Remove($"SystemSetting:{key}");

        await _auditLogService.LogAsync(
            action: "Setting.Updated",
            entityType: "SystemSetting",
            entityId: key,
            actorUserId: setting.UpdatedByUserId,
            actorUsername: User.Identity?.Name,
            metadata: new { key, newValue = setting.IsSensitive ? "********" : request.Value },
            cancellationToken: cancellationToken);

        return Ok(new SystemSettingDto(
            setting.Key,
            setting.IsSensitive ? "********" : setting.Value,
            setting.ValueType,
            setting.Description,
            setting.IsSensitive,
            setting.UpdatedAtUtc,
            setting.UpdatedByUserId
        ));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
