using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Notifications.Common;

/// <summary>
/// FR-10.1's list ("shipment created, confirmed, assigned to courier, picked up, out for
/// delivery, delivered, failed, cancelled, returned") maps onto Domain events as follows:
///  - "created"  -> ShipmentCreatedEvent (its own handler, not this map)
///  - "failed"   -> DeliveryFailedEvent (its own handler — richer, carries the Reason text;
///                  deliberately excluded below so ShipmentStatusChangedEventHandler doesn't
///                  ALSO fire a notification for the same Failed transition)
///  - everything else -> ShipmentStatusChangedEvent, mapped here
/// Preparing has no corresponding NotificationType because FR-10.1 doesn't list it either —
/// it's an internal ops-visible status, not a customer-facing milestone.
/// </summary>
internal static class NotificationTypeMapping
{
    public static NotificationType? ForShipmentStatus(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Confirmed => NotificationType.ShipmentConfirmed,
        ShipmentStatus.AssignedToCourier => NotificationType.ShipmentAssignedToCourier,
        ShipmentStatus.PickedUp => NotificationType.ShipmentPickedUp,
        ShipmentStatus.OutForDelivery => NotificationType.ShipmentOutForDelivery,
        ShipmentStatus.Delivered => NotificationType.ShipmentDelivered,
        ShipmentStatus.Cancelled => NotificationType.ShipmentCancelled,
        ShipmentStatus.Returned => NotificationType.ShipmentReturned,
        _ => null // Pending (initial, covered by ShipmentCreated instead), Preparing, Failed (see above)
    };

    /// <summary>FR-10.1 lists "paid, failed, refunded"; PartiallyRefunded reuses PaymentRefunded (still "you got money back") since there's no dedicated enum value for it. Pending is the initial no-op state and never notified.</summary>
    public static NotificationType? ForPaymentStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Paid => NotificationType.PaymentPaid,
        PaymentStatus.Failed => NotificationType.PaymentFailed,
        PaymentStatus.Refunded => NotificationType.PaymentRefunded,
        PaymentStatus.PartiallyRefunded => NotificationType.PaymentRefunded,
        _ => null
    };
}
