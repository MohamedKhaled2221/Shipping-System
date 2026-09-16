using Microsoft.AspNetCore.SignalR;
using ShippingSystem.Application.Abstractions.Realtime;

namespace ShippingSystem.Infrastructure.Realtime;

public sealed class SignalRShipmentTrackingNotifier : IShipmentTrackingNotifier
{
    private readonly IHubContext<TrackingHub> _hubContext;

    public SignalRShipmentTrackingNotifier(IHubContext<TrackingHub> hubContext) => _hubContext = hubContext;

    public Task BroadcastStatusChangedAsync(ShipmentTrackingUpdate update, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(TrackingHub.GroupName(update.TrackingNumber))
            .SendAsync("ShipmentStatusChanged", update, cancellationToken);
}
