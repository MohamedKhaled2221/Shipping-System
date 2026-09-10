using Microsoft.EntityFrameworkCore;
using ShippingSystem.Domain.Services;
using ShippingSystem.Infrastructure.Persistence;

namespace ShippingSystem.Infrastructure.Services;

/// <summary>
/// FR-9.1 — generates TRK-YYYY-NNNNNN.
///
/// Deliberately uses IDbContextFactory to create its OWN short-lived DbContext per call
/// instead of taking the ambient scoped AppDbContext as a constructor dependency. If it used
/// the ambient context, calling SaveChanges here (needed to atomically increment the
/// RowVersion-guarded counter) would prematurely commit whatever Order/Payment/Shipment
/// changes the calling command handler had already staged on that same context — silently
/// breaking the "every handler commits exactly once, via IUnitOfWork" rule the whole
/// Application layer is built on. A dedicated context has its own connection/transaction,
/// so this increment commits (or retries) in complete isolation from the caller's unit of
/// work — see DependencyInjection.cs for how both the factory and the ambient scoped
/// context are registered side by side.
///
/// Domain.Services.ITrackingNumberGenerator.Generate is synchronous by contract (the Domain
/// layer can't depend on Task/async patterns tied to a persistence technology); the brief
/// thread-blocking here is an acceptable trade-off for that.
/// </summary>
public sealed class TrackingNumberGenerator : ITrackingNumberGenerator
{
    private const int MaxRetries = 10;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TrackingNumberGenerator(IDbContextFactory<AppDbContext> dbContextFactory) =>
        _dbContextFactory = dbContextFactory;

    public string Generate(int year)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var counter = dbContext.TrackingNumberCounters.SingleOrDefault(c => c.Year == year);
            if (counter is null)
            {
                counter = new TrackingNumberCounter { Year = year, LastSequence = 0 };
                dbContext.TrackingNumberCounters.Add(counter);
            }

            counter.LastSequence += 1;
            var candidateSequence = counter.LastSequence;

            try
            {
                dbContext.SaveChanges();
                return $"TRK-{year}-{candidateSequence:D6}";
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request incremented the same year's counter first. This dedicated
                // context is discarded (its `using` disposes it) and the next loop iteration
                // opens a brand new one with a fresh read — no partial state carries over.
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate a unique tracking number for year {year} after {MaxRetries} attempts.");
    }
}
