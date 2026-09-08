namespace ShippingSystem.Domain.Enums;

/// <summary>FR-10.1 — the events a notification can represent.</summary>
public enum NotificationType
{
    ShipmentCreated = 0,
    ShipmentConfirmed = 1,
    ShipmentAssignedToCourier = 2,
    ShipmentPickedUp = 3,
    ShipmentOutForDelivery = 4,
    ShipmentDelivered = 5,
    ShipmentFailed = 6,
    ShipmentCancelled = 7,
    ShipmentReturned = 8,
    PaymentPaid = 9,
    PaymentFailed = 10,
    PaymentRefunded = 11
}

/// <summary>FR-10.2.</summary>
public enum NotificationChannel
{
    Email = 0,
    Sms = 1,
    Push = 2,
    SignalR = 3
}

public enum NotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
