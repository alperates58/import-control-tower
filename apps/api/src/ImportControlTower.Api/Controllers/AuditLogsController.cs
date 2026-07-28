using ImportControlTower.Application.Models;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [Authorize(Policy = "audit.view")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var logs = await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.TimestampUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var result = logs.Select(a => new AuditLogDto(
            a.Id,
            a.ActorUserId,
            a.ActorUsername,
            a.ActorType,
            a.Action,
            a.EntityType,
            a.EntityId,
            a.TimestampUtc,
            a.IpAddress,
            a.UserAgent,
            a.CorrelationId,
            a.MetadataJson
        ));

        return Ok(result);
    }
}
