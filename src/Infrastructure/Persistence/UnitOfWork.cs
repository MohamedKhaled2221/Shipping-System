using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Infrastructure.Persistence;

/// <summary>
/// The only class in the solution allowed to know that "concurrency conflict" means
/// DbUpdateConcurrencyException and "duplicate webhook" can mean a unique-index violation.
/// Application and Domain only ever see ConcurrencyConflictException.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // FR-5.3, FR-6.3, FR-6.5 — a stale RowVersion on Product/Order/Shipment.
            var entry = ex.Entries.FirstOrDefault();
            var entityType = entry?.Entity.GetType().Name ?? "Unknown";
            var entityId = entry?.Property("Id").CurrentValue ?? "unknown";
            throw new ConcurrencyConflictException(entityType, entityId);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // FR-11.4, FR-4.7 — two near-simultaneous deliveries of the same webhook both
            // reached this point past the upstream ExistsAsync check; the unique index on
            // (ProviderName, ExternalEventId) is what actually decides the race. The loser's
            // entire transaction rolls back here — but that's fine specifically because
            // ConfirmOrderPaymentCommand returns no data to its caller: the winner already
            // applied the identical business effect (mark payment, consume/release
            // reservations, create the shipment), so this attempt is a true no-op, not a
            // partial failure that silently drops work.
            return 0;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx && sqlEx.Number is 2601 or 2627;
}
