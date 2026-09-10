using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Shipments.Queries.GetShipmentsByAgent;

/// <summary>FR-7.2, FR-7.4 — an agent's assigned shipments (also serves as their delivery history, since it's not status-filtered).</summary>
public sealed record GetShipmentsByAgentQuery(Guid DeliveryAgentId) : IRequest<IReadOnlyList<ShipmentDto>>;
