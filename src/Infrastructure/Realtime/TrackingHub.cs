using Microsoft.AspNetCore.SignalR;

namespace ShippingSystem.Infrastructure.Realtime;

/// <summary>
/// SRS §5 — "SignalR Hub for real-time shipment status push to customer-facing clients."
/// Anonymous by design, matching TrackingController/GetShipmentTrackingQuery (FR-9.2):
/// tracking is public-by-tracking-number, not authenticated, so this hub imposes no
/// [Authorize] either — whoever has the tracking number can subscribe to its group, exactly
/// as they could already poll the REST endpoint with it.
///
/// Deliberately a thin transport shell with no business logic: clients only ever call
/// JoinTrackingGroup/LeaveTrackingGroup, and the actual push happens server-side from
/// SignalRShipmentTrackingNotifier via IHubContext&lt;TrackingHub&gt;, never from within the
/// Hub's own methods — this hub is a pure connection/group manager.
/// </summary>
public sealed class TrackingHub : Hub
{
    public Task JoinTrackingGroup(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Task.CompletedTask;

        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(trackingNumber));
    }

    public Task LeaveTrackingGroup(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Task.CompletedTask;

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(trackingNumber));
    }

    /// <summary>
    /// Single source of truth for the group-naming convention, shared with
    /// SignalRShipmentTrackingNotifier so a client joining "TRK-2026-000001" and a server
    /// broadcast for the same tracking number can never end up in different groups due to
    /// a casing mismatch or a formatting drift between the two call sites.
    /// </summary>
    public static string GroupName(string trackingNumber) => $"tracking:{trackingNumber.ToUpperInvariant()}";
}
