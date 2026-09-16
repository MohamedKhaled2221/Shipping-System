using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Notifications;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Notifications.Common;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Notifications.EventHandlers;

/// <summary>FR-10.1 "confirmed, assigned to courier, picked up, out for delivery, delivered, cancelled, returned" — see NotificationTypeMapping for why Failed is excluded here.</summary>
public sealed class ShipmentStatusChangedEventHandler : INotificationHandler<ShipmentStatusChangedEvent>
{
    private readonly IOrderRepository _orders;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ShipmentStatusChangedEventHandler> _logger;

    public ShipmentStatusChangedEventHandler(IOrderRepository orders, INotificationDispatcher dispatcher, ILogger<ShipmentStatusChangedEventHandler> logger)
    {
        _orders = orders;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(ShipmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var notificationType = NotificationTypeMapping.ForShipmentStatus(notification.NewStatus);
        if (notificationType is null) return; // Preparing, Failed (handled by DeliveryFailedEventHandler) — nothing to send.

        try
        {
            var order = await _orders.GetByIdAsync(notification.OrderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning(
                    "ShipmentStatusChangedEvent for Shipment {ShipmentId} references missing Order {OrderId}; skipping notification.",
                    notification.ShipmentId, notification.OrderId);
                return;
            }

            var message = $"Your shipment status changed to {notification.NewStatus}.";
            await _dispatcher.DispatchAsync(order.CustomerId, notificationType.Value, message, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch {NotificationType} notification for Shipment {ShipmentId}.", notificationType, notification.ShipmentId);
        }
    }
}
