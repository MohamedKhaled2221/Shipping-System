using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Infrastructure.Persistence;

namespace ShippingSystem.Infrastructure.Services;

/// <summary>
/// NFR Idempotency (client-retry side, as opposed to ProcessedWebhookEventRepository which
/// guards inbound provider webhooks). StoreResult only stages the row via DbContext.Add —
/// it's added to AppDbContext.IdempotencyRecords and only actually persisted when the
/// caller's IUnitOfWork.SaveChangesAsync runs, so it commits atomically with the rest of
/// the command's changes (e.g. the new Order + its InventoryReservations).
/// </summary>
public sealed class IdempotencyService : IIdempotencyService
{
    private readonly AppDbContext _dbContext;
    public IdempotencyService(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Guid?> TryGetExistingResultAsync(string requestType, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.RequestType == requestType && r.IdempotencyKey == idempotencyKey, cancellationToken);
        return record?.ResultId;
    }

    public void StoreResult(string requestType, string idempotencyKey, Guid resultId)
    {
        _dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            RequestType = requestType,
            IdempotencyKey = idempotencyKey,
            ResultId = resultId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
