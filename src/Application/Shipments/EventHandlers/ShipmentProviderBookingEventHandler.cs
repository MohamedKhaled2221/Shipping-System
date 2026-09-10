using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Shipping;
using ShippingSystem.Application.Common;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Shipments.EventHandlers;

/// <summary>
/// FR-11.1, FR-11.3, FR-3.4. This is the handler ConfirmOrderPaymentCommandHandler's own
/// doc comment (Orders module) pointed to from the very beginning: "a separate
/// INotificationHandler in the Shipment module reacts to [ShipmentCreatedEvent] and calls
/// IShippingProviderResolver/IShippingProvider with its own retry policy." Kept as its own
/// handler — not folded into Notifications.EventHandlers.ShipmentCreatedEventHandler —
/// because booking with an external carrier and notifying the customer are two unrelated
/// side effects of the same event; one failing must never block or be entangled with the
/// other, and MediatR happily runs both handlers for the same notification independently.
///
/// Runs AFTER the original CreateShipment transaction has already committed (this fires
/// from DomainEventDispatchInterceptor's post-commit publish step), so — unlike
/// TrackingNumberGenerator or NotificationDispatcher — there is no ambient in-flight
/// transaction this handler's own SaveChangesAsync could prematurely commit. Reusing the
/// normal scoped IShipmentRepository/IUnitOfWork here is therefore correct, not a repeat of
/// that earlier mistake — it matches exactly how the sibling notification handler for this
/// same event already behaves.
/// </summary>
public sealed class ShipmentProviderBookingEventHandler : INotificationHandler<ShipmentCreatedEvent>
{
    private readonly IShipmentRepository _shipments;
    private readonly IShippingAddressRepository _addresses;
    private readonly IShippingProviderConfigRepository _providerConfigs;
    private readonly IShippingProviderResolver _providerResolver;
    private readonly ICacheService _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShipmentProviderBookingEventHandler> _logger;

    public ShipmentProviderBookingEventHandler(
        IShipmentRepository shipments,
        IShippingAddressRepository addresses,
        IShippingProviderConfigRepository providerConfigs,
        IShippingProviderResolver providerResolver,
        ICacheService cache,
        IUnitOfWork unitOfWork,
        ILogger<ShipmentProviderBookingEventHandler> logger)
    {
        _shipments = shipments;
        _addresses = addresses;
        _providerConfigs = providerConfigs;
        _providerResolver = providerResolver;
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ShipmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipments.GetByIdAsync(notification.ShipmentId, cancellationToken);
            if (shipment is null)
            {
                _logger.LogWarning("ShipmentCreatedEvent for missing Shipment {ShipmentId}; skipping provider booking.", notification.ShipmentId);
                return;
            }

            var address = await _addresses.GetByIdAsync(shipment.ShippingAddressId, cancellationToken);
            if (address is null)
            {
                _logger.LogError(
                    "Shipment {ShipmentId} references missing ShippingAddress {AddressId}; cannot book with a provider.",
                    shipment.Id, shipment.ShippingAddressId);
                return;
            }

            var provider = await ResolveActiveProviderAsync(cancellationToken);

            var bookingRequest = new ShipmentBookingRequest(
                shipment.Id,
                shipment.TrackingNumber,
                address.Country,
                address.City,
                address.Area,
                address.Street,
                address.Phone,
                shipment.ShippingFee);

            var result = await provider.BookShipmentAsync(bookingRequest, cancellationToken);

            if (!result.Success)
            {
                // Left un-booked on purpose rather than forced into some Shipment status —
                // ShipmentStateMachine has no legal Pending -> Failed transition (FR-6.7;
                // Failed is only reachable from OutForDelivery), and a booking failure isn't
                // a delivery failure. An admin retry action for "book with provider" is a
                // reasonable follow-up, not something this handler can safely force through
                // the state machine itself.
                _logger.LogError(
                    "Provider {ProviderName} declined to book Shipment {ShipmentId}: {Reason}",
                    provider.ProviderName, shipment.Id, result.FailureReason);
                return;
            }

            shipment.RecordProviderBooking(provider.ProviderName, result.ProviderReference);

            if (shipment.EstimatedDeliveryDate is null)
            {
                var estimatedDeliveryDate = await provider.EstimateDeliveryDateAsync(bookingRequest, cancellationToken);
                shipment.SetEstimatedDeliveryDate(estimatedDeliveryDate);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // A provider outage or unexpected error must never fail the shipment-creation
            // transaction that already committed successfully — log and move on, same
            // defensive shape as every other event handler in this module.
            _logger.LogError(ex, "Unhandled failure booking Shipment {ShipmentId} with a shipping provider.", notification.ShipmentId);
        }
    }

    /// <summary>
    /// FR-11.2/11.3, NFR Caching — picks the first row with IsActive=true from
    /// ShippingProviderConfig, cached briefly since this runs on every single shipment
    /// booking against a value that changes only when an admin reconfigures providers (no
    /// such management endpoint exists yet — see the Api module's README — so there is
    /// currently no write path to proactively invalidate this key; the short TTL is the
    /// only staleness guard until one exists). Falls back to "Local" (Assumptions §7: the
    /// only fully-implemented provider in v1) when no config rows exist yet, which is the
    /// expected state on a freshly-migrated database.
    /// </summary>
    private async Task<IShippingProvider> ResolveActiveProviderAsync(CancellationToken cancellationToken)
    {
        var providerName = await _cache.GetAsync<string>(CacheKeys.ActiveShippingProviderName, cancellationToken);

        if (providerName is null)
        {
            var activeConfigs = await _providerConfigs.GetActiveAsync(cancellationToken);
            providerName = activeConfigs.FirstOrDefault()?.ProviderName ?? "Local";

            await _cache.SetAsync(CacheKeys.ActiveShippingProviderName, providerName, TimeSpan.FromMinutes(5), cancellationToken);
        }

        return _providerResolver.Resolve(providerName);
    }
}
