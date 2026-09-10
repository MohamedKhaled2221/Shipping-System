using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Infrastructure.Options;

namespace ShippingSystem.Infrastructure.BackgroundJobs;

/// <summary>
/// FR-5.6 — "Reserved-but-unconfirmed inventory shall expire and auto-release after a
/// configurable timeout." Wakes up every ScanIntervalMinutes, finds reservations whose
/// ExpiresAt has passed and are still Status=Reserved (i.e. never Consumed by a successful
/// payment nor Released by an explicit cancel), and releases their stock back to the
/// owning Product.
///
/// Each reservation is processed in its OWN DI scope + its own SaveChangesAsync, not one
/// batch commit for the whole sweep: a RowVersion conflict on one Product (e.g. a customer
/// is mid-checkout reserving the exact same product this same instant) must not roll back
/// the release of every other, unrelated expired reservation found in this tick — it should
/// just get picked up again on the next tick instead.
///
/// Unlike TrackingNumberGenerator/NotificationDispatcher, this job resolves the ordinary
/// Scoped IUnitOfWork/repositories (via IServiceScopeFactory) rather than a dedicated
/// IDbContextFactory context — there is no ambient caller/request whose in-flight
/// SaveChanges this could prematurely commit; each scope here is this job's own,
/// self-contained unit of work, so the standard BackgroundService-consumes-scoped-services
/// pattern applies cleanly.
/// </summary>
public sealed class InventoryReservationExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly InventoryReservationOptions _options;
    private readonly ILogger<InventoryReservationExpiryJob> _logger;

    public InventoryReservationExpiryJob(
        IServiceScopeFactory scopeFactory,
        IOptions<InventoryReservationOptions> options,
        ILogger<InventoryReservationExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.ScanIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        // Run once immediately on startup, then on every timer tick, so a reservation that
        // expired while the service was down doesn't wait a full extra interval to be swept.
        await RunSweepSafelyAsync(stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunSweepSafelyAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown (host stopping) — nothing to do.
        }
    }

    private async Task RunSweepSafelyAsync(CancellationToken stoppingToken)
    {
        try
        {
            await SweepExpiredReservationsAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A whole-sweep failure (e.g. DB momentarily unreachable) must not kill the
            // hosted service — log and simply try again next tick.
            _logger.LogError(ex, "Inventory reservation expiry sweep failed; will retry next tick.");
        }
    }

    private async Task SweepExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        List<Guid> expiredIds;
        {
            using var scope = _scopeFactory.CreateScope();
            var reservations = scope.ServiceProvider.GetRequiredService<IInventoryReservationRepository>();
            var dateTime = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

            var expired = await reservations.GetExpiredReservedAsync(dateTime.UtcNow, cancellationToken);
            expiredIds = expired.Select(r => r.Id).ToList();
        }

        if (expiredIds.Count == 0) return;

        _logger.LogInformation("Found {Count} expired inventory reservation(s) to release.", expiredIds.Count);

        foreach (var reservationId in expiredIds)
            await ReleaseOneAsync(reservationId, cancellationToken);
    }

    private async Task ReleaseOneAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reservations = scope.ServiceProvider.GetRequiredService<IInventoryReservationRepository>();
            var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var reservation = await reservations.GetByIdAsync(reservationId, cancellationToken);

            // Re-check status against a fresh load: another concurrent path (an explicit
            // CancelOrder, or a payment confirming) may have already Released/Consumed this
            // exact reservation between the sweep query above and this scope opening.
            if (reservation is null || reservation.Status != ReservationStatus.Reserved) return;

            var product = await products.GetByIdAsync(reservation.ProductId, cancellationToken);
            if (product is null)
            {
                _logger.LogWarning(
                    "Reservation {ReservationId} references missing Product {ProductId}; marking it Expired without a stock release.",
                    reservation.Id, reservation.ProductId);
                reservation.Expire();
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            reservation.Expire();
            product.ReleaseStock(reservation.QuantityReserved);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // FR-5.3 concurrency conflicts land here too — left Reserved-but-past-ExpiresAt,
            // this reservation is simply picked up again by the next sweep.
            _logger.LogError(ex, "Failed to auto-release inventory reservation {ReservationId}; will retry next scan.", reservationId);
        }
    }
}
