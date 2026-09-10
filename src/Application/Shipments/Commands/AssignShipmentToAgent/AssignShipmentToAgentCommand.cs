using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Shipments.Commands.AssignShipmentToAgent;

/// <summary>
/// FR-6.3. Assigning an agent is modeled as the same business event as the shipment's
/// Preparing -> AssignedToCourier transition (FR-6.7) — a shipment isn't "assigned to a
/// courier" in name only while still sitting in Preparing. AssignedBy is the acting
/// Admin's identity, recorded on the resulting status-history row.
/// </summary>
public sealed record AssignShipmentToAgentCommand(
    Guid ShipmentId,
    Guid DeliveryAgentId,
    string AssignedBy) : IRequest<ShipmentDto>;
