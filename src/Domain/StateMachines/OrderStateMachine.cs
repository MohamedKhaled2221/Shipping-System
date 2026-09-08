using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Domain.StateMachines;

/// <summary>
/// FR-3.5. Single source of truth for which Order status transitions are legal.
/// Pending -> Confirmed -> Processing -> Completed
/// Any non-terminal state -> Cancelled
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
        [OrderStatus.Confirmed] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
        [OrderStatus.Processing] = new[] { OrderStatus.Completed, OrderStatus.Cancelled },
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Completed or OrderStatus.Cancelled;

    public static IReadOnlyCollection<OrderStatus> GetAllowedNextStates(OrderStatus from) =>
        Transitions.TryGetValue(from, out var allowed) ? allowed : Array.Empty<OrderStatus>();
}
