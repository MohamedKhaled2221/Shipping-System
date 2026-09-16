using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Notifications;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Notifications.EventHandlers;

/// <summary>FR-10.1 "shipment created". Fired once, from Shipment.Create.</summary>
public sealed class ShipmentCreatedEventHandler : INotificationHandler<ShipmentCreatedEvent>
{
    private readonly IOrderRepository _orders;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ShipmentCreatedEventHandler> _logger;

    public ShipmentCreatedEventHandler(IOrderRepository orders, INotificationDispatcher dispatcher, ILogger<ShipmentCreatedEventHandler> logger)
    {
        _orders = orders;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(ShipmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _orders.GetByIdAsync(notification.OrderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning(
                    "ShipmentCreatedEvent for Shipment {ShipmentId} references missing Order {OrderId}; skipping notification.",
                    notification.ShipmentId, notification.OrderId);
                return;
            }

            var message = $"Your shipment has been created. Track it with {notification.TrackingNumber}.";
            await _dispatcher.DispatchAsync(order.CustomerId, NotificationType.ShipmentCreated, message, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // A notification-delivery failure must never fail the shipment-creation
            // transaction that already committed successfully — log and move on.
            _logger.LogError(ex, "Failed to dispatch ShipmentCreated notification for Shipment {ShipmentId}.", notification.ShipmentId);
        }
    }
}
