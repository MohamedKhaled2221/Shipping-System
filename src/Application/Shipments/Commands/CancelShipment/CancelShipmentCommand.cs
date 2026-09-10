using MediatR;

namespace ShippingSystem.Application.Shipments.Commands.CancelShipment;

/// <summary>
/// FR-6.2, FR-6.7 ("Any valid (non-terminal) state -> Cancelled"). Deliberately does not
/// touch inventory: by the time a Shipment exists, its reservation was already Consumed
/// (permanently decremented) in ConfirmOrderPaymentCommandHandler, not left Reserved — the
/// order-cancellation-releases-inventory rule (FR-5.4, SRS §2.2 step 11) applies before
/// shipment/fulfillment, which is a separate command (CancelOrderCommand).
/// </summary>
public sealed record CancelShipmentCommand(Guid ShipmentId, string CancelledBy, string? Reason) : IRequest;
