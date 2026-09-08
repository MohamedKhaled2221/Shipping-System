using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Domain.StateMachines;

/// <summary>
/// FR-6.7. Single source of truth for which Shipment status transitions are legal.
/// Kept entirely independent from OrderStateMachine — never share or infer state between the two.
///
/// Main flow: Pending -> Confirmed -> Preparing -> AssignedToCourier -> PickedUp -> OutForDelivery -> Delivered
/// Alt flows: OutForDelivery -> Failed, Failed -> OutForDelivery (retry), Failed -> Returned
/// Any non-terminal state -> Cancelled
/// Any transition not listed here is rejected by the domain layer.
/// </summary>
public static class ShipmentStateMachine
{
    private static readonly Dictionary<ShipmentStatus, ShipmentStatus[]> Transitions = new()
    {
        [ShipmentStatus.Pending] = new[] { ShipmentStatus.Confirmed, ShipmentStatus.Cancelled },
        [ShipmentStatus.Confirmed] = new[] { ShipmentStatus.Preparing, ShipmentStatus.Cancelled },
        [ShipmentStatus.Preparing] = new[] { ShipmentStatus.AssignedToCourier, ShipmentStatus.Cancelled },
        [ShipmentStatus.AssignedToCourier] = new[] { ShipmentStatus.PickedUp, ShipmentStatus.Cancelled },
        [ShipmentStatus.PickedUp] = new[] { ShipmentStatus.OutForDelivery, ShipmentStatus.Cancelled },
        [ShipmentStatus.OutForDelivery] = new[] { ShipmentStatus.Delivered, ShipmentStatus.Failed, ShipmentStatus.Cancelled },
        [ShipmentStatus.Failed] = new[] { ShipmentStatus.OutForDelivery, ShipmentStatus.Returned, ShipmentStatus.Cancelled },
        [ShipmentStatus.Delivered] = Array.Empty<ShipmentStatus>(),
        [ShipmentStatus.Cancelled] = Array.Empty<ShipmentStatus>(),
        [ShipmentStatus.Returned] = Array.Empty<ShipmentStatus>()
    };

    public static bool CanTransition(ShipmentStatus from, ShipmentStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool IsTerminal(ShipmentStatus status) =>
        status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled or ShipmentStatus.Returned;

    public static IReadOnlyCollection<ShipmentStatus> GetAllowedNextStates(ShipmentStatus from) =>
        Transitions.TryGetValue(from, out var allowed) ? allowed : Array.Empty<ShipmentStatus>();
}
