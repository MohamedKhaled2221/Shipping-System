using MediatR;

namespace ShippingSystem.Application.Shipments.Commands.ReturnShipment;

/// <summary>FR-6.4, FR-6.7 (Failed -> Returned is the only path into this status).</summary>
public sealed record ReturnShipmentCommand(Guid ShipmentId, string ReturnedBy, string Reason) : IRequest;
