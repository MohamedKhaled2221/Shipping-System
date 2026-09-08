using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Abstractions.Realtime;

/// <summary>FR-9.2, FR-10.2 (SignalR channel). Mirrors ShipmentTrackingDto's shape closely enough for a live UI to just replace its current view with this on receipt.</summary>
public sealed record ShipmentTrackingUpdate(
    string TrackingNumber,
    ShipmentStatus Status,
    DateTime ChangedAtUtc,
    string ChangedBy,
    string? Notes,
    Guid? DeliveryAgentId,
    DateTime? EstimatedDeliveryDate);

/// <summary>
/// FR-10.2 — "SignalR (real-time in-app)" as a notification channel, scoped specifically to
/// shipment tracking (the one place SRS §5 calls out a SignalR Hub explicitly: "real-time
/// shipment status push to customer-facing clients"). Deliberately narrower than a generic
/// "send anything over SignalR" interface — Application code should never need to know hub
/// names, group naming conventions, or connection-management details; it just says "this
/// tracking number's status changed" and Infrastructure decides how that reaches a browser.
/// </summary>
public interface IShipmentTrackingNotifier
{
    Task BroadcastStatusChangedAsync(ShipmentTrackingUpdate update, CancellationToken cancellationToken = default);
}
