using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Services;

/// <summary>
/// FR-4.3, FR-4.4. A domain service (not a method on either aggregate) because it needs
/// both Order and Shipment loaded at once — Order and Shipment are separate consistency
/// boundaries, so neither aggregate may hold a live reference to the other.
///
/// The Application layer MUST call EnsureCanProgress(order, targetStatus) immediately
/// before calling Shipment.TransitionTo(targetStatus, ...) for every transition.
/// </summary>
public static class ShipmentPaymentGuard
{
    /// <summary>
    /// Statuses a prepaid shipment may not reach unless the order's PaymentStatus is Paid.
    /// Pending/Confirmed remain reachable regardless (FR-4.3 wording: "not progress PAST
    /// Pending/Confirmed").
    /// </summary>
    private static readonly ShipmentStatus[] GatedStatuses =
    {
        ShipmentStatus.Preparing,
        ShipmentStatus.AssignedToCourier,
        ShipmentStatus.PickedUp,
        ShipmentStatus.OutForDelivery,
        ShipmentStatus.Delivered
    };

    public static void EnsureCanProgress(Order order, ShipmentStatus targetStatus)
    {
        // FR-4.4 — COD orders progress normally regardless of PaymentStatus.
        if (order.PaymentMethod == PaymentMethod.CashOnDelivery) return;

        // FR-4.3 — prepaid orders must be Paid before advancing past Pending/Confirmed.
        if (GatedStatuses.Contains(targetStatus) && order.PaymentStatus != PaymentStatus.Paid)
        {
            throw new DomainException(
                $"Order '{order.Id}' is prepaid and not yet Paid (current: {order.PaymentStatus}); " +
                $"shipment cannot progress to '{targetStatus}'.");
        }
    }

    /// <summary>
    /// FR-4.5 — a failed payment blocks the order from reaching Confirmed.
    /// Call before Order.TransitionTo(OrderStatus.Confirmed, ...).
    /// </summary>
    public static void EnsureOrderCanBeConfirmed(Order order)
    {
        if (order.PaymentMethod != PaymentMethod.CashOnDelivery && order.PaymentStatus == PaymentStatus.Failed)
        {
            throw new DomainException(
                $"Order '{order.Id}' cannot be confirmed because its payment has failed.");
        }
    }
}
