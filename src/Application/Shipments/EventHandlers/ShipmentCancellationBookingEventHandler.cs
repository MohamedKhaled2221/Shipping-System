using MediatR;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Shipping;
using ShippingSystem.Application.Common.Interfaces;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Events;

namespace ShippingSystem.Application.Shipments.EventHandlers;

/// <summary>
/// FR-11.1 counterpart to ShipmentProviderBookingEventHandler: if a Shipment that was
/// actually booked with a carrier (ProviderReference set) is later cancelled — via
/// CancelShipmentCommand, whichever route — the carrier needs to hear about that too, not
/// just this system's own database. Reacts to ShipmentStatusChangedEvent generically rather
/// than being called directly from CancelShipmentCommandHandler for the same reason booking
/// itself isn't inline in ConfirmOrderPaymentCommandHandler: an outbound call to a
/// third-party carrier doesn't belong inside the same DB transaction as the cancellation
/// itself.
/// </summary>
public sealed class ShipmentCancellationBookingEventHandler : INotificationHandler<ShipmentStatusChangedEvent>
{
    private readonly IShipmentRepository _shipments;
    private readonly IShippingProviderResolver _providerResolver;
    private readonly ILogger<ShipmentCancellationBookingEventHandler> _logger;

    public ShipmentCancellationBookingEventHandler(
        IShipmentRepository shipments,
        IShippingProviderResolver providerResolver,
        ILogger<ShipmentCancellationBookingEventHandler> logger)
    {
        _shipments = shipments;
        _providerResolver = providerResolver;
        _logger = logger;
    }

    public async Task Handle(ShipmentStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.NewStatus != ShipmentStatus.Cancelled)
            return;

        try
        {
            var shipment = await _shipments.GetByIdAsync(notification.ShipmentId, cancellationToken);

            // Nothing to tell a carrier about if it was never successfully booked with one
            // in the first place (ProviderReference only gets set on a successful booking —
            // see ShipmentProviderBookingEventHandler).
            if (shipment?.ProviderReference is null)
                return;

            var provider = _providerResolver.Resolve(shipment.ProviderName!);
            await provider.CancelBookingAsync(shipment.ProviderReference, cancellationToken);

            _logger.LogInformation(
                "Cancelled provider booking {ProviderReference} with {ProviderName} for Shipment {ShipmentId}.",
                shipment.ProviderReference, provider.ProviderName, shipment.Id);
        }
        catch (Exception ex)
        {
            // The cancellation already committed on this system's side regardless of
            // whether the carrier acknowledges it — log for manual/ops follow-up rather
            // than leaving the customer-facing cancellation itself in a failed state.
            _logger.LogError(ex, "Failed to cancel provider booking for Shipment {ShipmentId}.", notification.ShipmentId);
        }
    }
}
