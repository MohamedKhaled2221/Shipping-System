namespace ShippingSystem.Domain.Enums;

/// <summary>
/// FR-6.6. Distinct state machine from OrderStatus — never inferred from it, never overwrites it.
/// Main flow: Pending -> Confirmed -> Preparing -> AssignedToCourier -> PickedUp -> OutForDelivery -> Delivered
/// Alt flows: OutForDelivery -> Failed, Failed -> OutForDelivery (retry), Failed -> Returned
/// Any non-terminal state -> Cancelled
/// </summary>
public enum ShipmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    AssignedToCourier = 3,
    PickedUp = 4,
    OutForDelivery = 5,
    Delivered = 6,
    Failed = 7,
    Cancelled = 8,
    Returned = 9
}
