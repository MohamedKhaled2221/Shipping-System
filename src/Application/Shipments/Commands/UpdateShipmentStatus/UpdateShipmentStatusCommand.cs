using MediatR;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Shipments.Commands.UpdateShipmentStatus;

/// <summary>
/// FR-6.2, FR-6.5, FR-7.3, FR-7.5 — the single generic "advance the shipment" command used
/// for every progression step (Preparing, PickedUp, OutForDelivery, Delivered, the
/// Failed/retry loop, etc.). ShipmentStateMachine (FR-6.7) is the only place that decides
/// which NewStatus values are legal from the shipment's current status; this command does
/// not special-case individual target statuses beyond routing Failed through
/// Shipment.RecordFailure so the failure reason (Notes) is also persisted as a
/// FailedDeliveryReason row (FR-7.5), not just a status-history note.
/// </summary>
public sealed record UpdateShipmentStatusCommand(
    Guid ShipmentId,
    ShipmentStatus NewStatus,
    string ChangedBy,
    string? Notes) : IRequest<ShipmentDto>;
