using System.Text.Json;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;

namespace ImportControlTower.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        string entityId,
        Guid? actorUserId = null,
        string? actorUsername = null,
        string actorType = "User",
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        string metadataJson = "{}";
        if (metadata != null)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = false };
                metadataJson = JsonSerializer.Serialize(metadata, options);
            }
            catch
            {
                metadataJson = "{}";
            }
        }

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorUsername = actorUsername,
            ActorType = actorType,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            TimestampUtc = DateTime.UtcNow,
            IpAddress = ipAddress ?? "Unknown",
            UserAgent = userAgent ?? "Unknown",
            CorrelationId = correlationId ?? Guid.NewGuid().ToString(),
            MetadataJson = metadataJson
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
