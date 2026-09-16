using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Notifications;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Application.Notifications.Common;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Notifications.EventHandlers;

/// <summary>FR-10.1 "key payment events (paid, failed, refunded)".</summary>
public sealed class PaymentStatusChangedEventHandler : INotificationHandler<PaymentStatusChangedEvent>
{
    private readonly IOrderRepository _orders;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<PaymentStatusChangedEventHandler> _logger;

    public PaymentStatusChangedEventHandler(IOrderRepository orders, INotificationDispatcher dispatcher, ILogger<PaymentStatusChangedEventHandler> logger)
    {
        _orders = orders;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(PaymentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var notificationType = NotificationTypeMapping.ForPaymentStatus(notification.NewStatus);
        if (notificationType is null) return; // Pending — nothing to notify about.

        try
        {
            var order = await _orders.GetByIdAsync(notification.OrderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning(
                    "PaymentStatusChangedEvent references missing Order {OrderId}; skipping notification.", notification.OrderId);
                return;
            }

            var message = $"Your payment status changed to {notification.NewStatus}.";
            await _dispatcher.DispatchAsync(order.CustomerId, notificationType.Value, message, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch {NotificationType} notification for Order {OrderId}.", notificationType, notification.OrderId);
        }
    }
}
