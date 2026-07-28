namespace ImportControlTower.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
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
        CancellationToken cancellationToken = default);
}
