using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ImportControlTower.Application.Services;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly ApplicationDbContext _db;

    public IdempotencyService(ApplicationDbContext db)
    {
        _db = db;
    }

    public string ComputeRequestHash(string scopeKey, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var combined = $"{scopeKey}:{json}";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<IdempotencyCheckResult> CheckAndLockAsync(
        Guid userId,
        string operationType,
        string scopeKey,
        string idempotencyKey,
        string requestHash)
    {
        var existing = await _db.OperationIdempotencyRequests
            .FirstOrDefaultAsync(r => 
                r.RequestedByUserId == userId &&
                r.OperationType == operationType &&
                r.ScopeKey == scopeKey &&
                r.IdempotencyKey == idempotencyKey);

        if (existing != null)
        {
            if (existing.RequestHash != requestHash)
            {
                return new IdempotencyCheckResult(
                    IsCompleted: false,
                    IsProcessingConflict: false,
                    IsHashMismatch: true,
                    ResponseStatusCode: null,
                    ResponseJson: null,
                    RequestId: existing.Id
                );
            }

            if (existing.Status == "Completed")
            {
                return new IdempotencyCheckResult(
                    IsCompleted: true,
                    IsProcessingConflict: false,
                    IsHashMismatch: false,
                    ResponseStatusCode: existing.ResponseStatusCode,
                    ResponseJson: existing.ResponseJson,
                    RequestId: existing.Id
                );
            }

            if (existing.Status == "Processing")
            {
                return new IdempotencyCheckResult(
                    IsCompleted: false,
                    IsProcessingConflict: true,
                    IsHashMismatch: false,
                    ResponseStatusCode: null,
                    ResponseJson: null,
                    RequestId: existing.Id
                );
            }

            if (existing.Status == "Failed")
            {
                existing.Status = "Processing";
                existing.CreatedAtUtc = DateTime.UtcNow;
                existing.CompletedAtUtc = null;
                await _db.SaveChangesAsync();

                return new IdempotencyCheckResult(
                    IsCompleted: false,
                    IsProcessingConflict: false,
                    IsHashMismatch: false,
                    ResponseStatusCode: null,
                    ResponseJson: null,
                    RequestId: existing.Id
                );
            }
        }

        var newReq = new OperationIdempotencyRequest
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = userId,
            OperationType = operationType,
            ScopeKey = scopeKey,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            Status = "Processing",
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.OperationIdempotencyRequests.Add(newReq);
        await _db.SaveChangesAsync();

        return new IdempotencyCheckResult(
            IsCompleted: false,
            IsProcessingConflict: false,
            IsHashMismatch: false,
            ResponseStatusCode: null,
            ResponseJson: null,
            RequestId: newReq.Id
        );
    }

    public async Task SaveResponseAsync(Guid idempotencyRequestId, int statusCode, object responseObj)
    {
        var req = await _db.OperationIdempotencyRequests.FindAsync(idempotencyRequestId);
        if (req != null)
        {
            req.Status = "Completed";
            req.ResponseStatusCode = statusCode;
            req.ResponseJson = JsonSerializer.Serialize(responseObj);
            req.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
