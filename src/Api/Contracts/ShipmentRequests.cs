using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Api.Contracts;

public sealed record AssignShipmentRequest(Guid DeliveryAgentId);

/// <summary>
/// Notes is required by UpdateShipmentStatusCommandValidator when NewStatus is Failed
/// (FR-7.5) and optional for every other allowed target status.
/// </summary>
public sealed record UpdateShipmentStatusRequest(ShipmentStatus NewStatus, string? Notes);

public sealed record CancelShipmentRequest(string? Reason);

public sealed record ReturnShipmentRequest(string Reason);
