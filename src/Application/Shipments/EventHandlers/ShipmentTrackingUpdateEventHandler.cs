using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Realtime;
using ShippingSystem.Application.Common;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Shipments.EventHandlers;

/// <summary>
/// FR-9.2, FR-10.2, NFR Caching. Combines cache invalidation and the SignalR live push into
/// one handler — unlike ShipmentProviderBookingEventHandler vs. the Notifications module's
/// ShipmentCreatedEventHandler (deliberately split because booking-with-a-carrier and
/// notifying-a-customer are genuinely unrelated systems), invalidating the cached tracking
/// view and pushing the same fresh data to a live SignalR subscriber are two mechanisms
/// serving the exact same goal: "the public tracking view for this shipment must reflect
/// reality now." Splitting those into two handlers would just mean reloading the same
/// Shipment twice for no architectural benefit.
/// </summary>
public sealed class ShipmentTrackingUpdateEventHandler : INotificationHandler<ShipmentStatusChangedEvent>
{
    private readonly IShipmentRepository _shipments;
    private readonly ICacheService _cache;
    private readonly IShipmentTrackingNotifier _trackingNotifier;
    private readonly ILogger<ShipmentTrackingUpdateEventHandler> _logger;

    public ShipmentTrackingUpdateEventHandler(
        IShipmentRepository shipments,
        ICacheService cache,
        IShipmentTrackingNotifier trackingNotifier,
        ILogger<ShipmentTrackingUpdateEventHandler> logger)
    {
        _shipments = shipments;
        _cache = cache;
        _trackingNotifier = trackingNotifier;
        _logger = logger;
    }

    public async Task Handle(ShipmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await _shipments.GetByIdAsync(notification.ShipmentId, cancellationToken);
            if (shipment is null)
            {
                _logger.LogWarning("ShipmentStatusChangedEvent for missing Shipment {ShipmentId}; skipping tracking update.", notification.ShipmentId);
                return;
            }

            // Evict first, unconditionally: even if the SignalR broadcast below fails, the
            // NEXT GetShipmentTrackingQuery call must never serve the stale pre-change entry.
            await _cache.RemoveAsync(CacheKeys.ShipmentTracking(shipment.TrackingNumber), cancellationToken);

            var latestNote = shipment.StatusHistory
                .OrderByDescending(h => h.ChangedAt)
                .FirstOrDefault(h => h.Status == notification.NewStatus)
                ?.Notes;

            var update = new ShipmentTrackingUpdate(
                shipment.TrackingNumber,
                notification.NewStatus,
                DateTime.UtcNow,
                notification.ChangedBy,
                latestNote,
                shipment.DeliveryAgentId,
                shipment.EstimatedDeliveryDate);

            await _trackingNotifier.BroadcastStatusChangedAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            // Same defensive shape as every other event handler here: the status change
            // already committed successfully; a cache/SignalR hiccup must never surface as
            // a failure of the command that triggered it.
            _logger.LogError(ex, "Failed to update the public tracking view for Shipment {ShipmentId}.", notification.ShipmentId);
        }
    }
}
