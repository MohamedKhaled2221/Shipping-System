namespace ShippingSystem.Domain.Enums;

/// <summary>
/// FR-3.5. Fully independent from ShipmentStatus and PaymentStatus — never conflate them.
/// Flow: Pending -> Confirmed -> Processing -> Completed
/// Any non-terminal state -> Cancelled
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Completed = 3,
    Cancelled = 4
}
