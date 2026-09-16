using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Notifications;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Notifications.EventHandlers;

/// <summary>
/// FR-10.1 "failed" (FR-7.5). Carries the actual failure Reason, unlike the generic
/// ShipmentStatusChangedEvent(NewStatus=Failed) that fires alongside it (see
/// NotificationTypeMapping) — this is the richer event, so it's the one that owns sending
/// the customer-facing notification for a failed delivery.
/// </summary>
public sealed class DeliveryFailedEventHandler : INotificationHandler<DeliveryFailedEvent>
{
    private readonly IShipmentRepository _shipments;
    private readonly IOrderRepository _orders;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<DeliveryFailedEventHandler> _logger;

    public DeliveryFailedEventHandler(
        IShipmentRepository shipments, IOrderRepository orders, INotificationDispatcher dispatcher, ILogger<DeliveryFailedEventHandler> logger)
    {
        _shipments = shipments;
        _orders = orders;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(DeliveryFailedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // DeliveryFailedEvent only carries ShipmentId — OrderId (and from it, the
            // Customer to notify) has to be resolved via the Shipment aggregate first.
            var shipment = await _shipments.GetByIdAsync(notification.ShipmentId, cancellationToken);
            if (shipment is null)
            {
                _logger.LogWarning("DeliveryFailedEvent references missing Shipment {ShipmentId}; skipping notification.", notification.ShipmentId);
                return;
            }

            var order = await _orders.GetByIdAsync(shipment.OrderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning(
                    "DeliveryFailedEvent for Shipment {ShipmentId} references missing Order {OrderId}; skipping notification.",
                    notification.ShipmentId, shipment.OrderId);
                return;
            }

            var message = $"Delivery attempt failed: {notification.Reason}";
            await _dispatcher.DispatchAsync(order.CustomerId, NotificationType.ShipmentFailed, message, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch ShipmentFailed notification for Shipment {ShipmentId}.", notification.ShipmentId);
        }
    }
}
