namespace ShippingSystem.Domain.Enums;

/// <summary>InventoryReservation.Status — FR-5.2, FR-5.4, FR-5.6.</summary>
public enum ReservationStatus
{
    Reserved = 0,
    Released = 1,
    Consumed = 2,
    Expired = 3
}
