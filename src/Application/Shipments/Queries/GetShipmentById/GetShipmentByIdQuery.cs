using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Shipments.Queries.GetShipmentById;

/// <summary>FR-6.2, FR-9.2 (Admin/Agent view — the public customer tracking endpoint is a separate, unauthenticated query in the Tracking module).</summary>
public sealed record GetShipmentByIdQuery(Guid ShipmentId) : IRequest<ShipmentDto>;
