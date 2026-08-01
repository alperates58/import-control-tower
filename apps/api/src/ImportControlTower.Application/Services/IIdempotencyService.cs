using System;
using System.Threading.Tasks;

namespace ImportControlTower.Application.Services;

public record IdempotencyCheckResult(
    bool IsCompleted,
    bool IsProcessingConflict,
    bool IsHashMismatch,
    int? ResponseStatusCode,
    string? ResponseJson,
    Guid? RequestId
);

public interface IIdempotencyService
{
    string ComputeRequestHash(string scopeKey, object payload);
    Task<IdempotencyCheckResult> CheckAndLockAsync(
        Guid userId,
        string operationType,
        string scopeKey,
        string idempotencyKey,
        string requestHash);
    Task SaveResponseAsync(Guid idempotencyRequestId, int statusCode, object responseObj);
}
