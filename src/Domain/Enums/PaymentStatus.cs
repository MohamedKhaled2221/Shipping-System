namespace ShippingSystem.Domain.Enums;

/// <summary>FR-4.2. Tracked independently from OrderStatus and ShipmentStatus.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3,
    PartiallyRefunded = 4
}
