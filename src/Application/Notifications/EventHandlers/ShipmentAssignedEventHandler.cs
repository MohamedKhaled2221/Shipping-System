using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Notifications;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Notifications.EventHandlers;

/// <summary>
/// Deliberately separate from ShipmentStatusChangedEventHandler's ShipmentAssignedToCourier
/// notification (which tells the CUSTOMER their order moved to a courier) — this one tells
/// the AGENT themselves that a new shipment landed on their list (supports FR-7.2's "view
/// assigned shipments"). Two different audiences for the same underlying business moment,
/// so both firing from the same Assign action is intentional, not a duplicate.
/// </summary>
public sealed class ShipmentAssignedEventHandler : INotificationHandler<ShipmentAssignedEvent>
{
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<ShipmentAssignedEventHandler> _logger;

    public ShipmentAssignedEventHandler(INotificationDispatcher dispatcher, ILogger<ShipmentAssignedEventHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(ShipmentAssignedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var message = "A new shipment has been assigned to you.";
            await _dispatcher.DispatchAsync(
                notification.DeliveryAgentId, NotificationType.ShipmentAssignedToCourier, message, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch agent-assignment notification for Shipment {ShipmentId}.", notification.ShipmentId);
        }
    }
}
