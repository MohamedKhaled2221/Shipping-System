using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Tracking.Queries.GetShipmentTracking;

/// <summary>
/// FR-9.2. Deliberately keyed by TrackingNumber alone, not by an authenticated Customer's
/// OrderId/ShipmentId — this is the same "enter your tracking number, no login required"
/// model every real carrier (DHL/Aramex/FedEx/local couriers) exposes publicly, and the
/// SRS's own Actors table lists no "must be logged in" qualifier on FR-9.2 the way FR-2.3
/// (order/shipment *history*) implicitly requires being the owning Customer. See
/// TrackingController for the corresponding [AllowAnonymous] endpoint.
/// </summary>
public sealed record GetShipmentTrackingQuery(string TrackingNumber) : IRequest<ShipmentTrackingDto>;
