using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// FR-5.2, FR-5.4, FR-5.6. Tracks a hold placed against a Product for a specific Order.
/// One row per (Order, Product) pair. Not an aggregate root by itself — created and
/// released together with its owning Product/Order via an application-layer transaction.
/// </summary>
public sealed class InventoryReservation : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid OrderId { get; private set; }
    public int QuantityReserved { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private InventoryReservation() { } // EF Core

    public static InventoryReservation Create(Guid productId, Guid orderId, int quantity, TimeSpan expiryWindow)
    {
        if (quantity <= 0) throw new DomainException("Reserved quantity must be positive.");
        if (expiryWindow <= TimeSpan.Zero) throw new DomainException("Expiry window must be positive.");

        var now = DateTime.UtcNow;
        return new InventoryReservation
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            OrderId = orderId,
            QuantityReserved = quantity,
            Status = ReservationStatus.Reserved,
            CreatedAt = now,
            ExpiresAt = now.Add(expiryWindow)
        };
    }

    public bool IsExpired(DateTime asOfUtc) => Status == ReservationStatus.Reserved && asOfUtc >= ExpiresAt;

    /// <summary>FR-5.4 — order cancelled or payment failed.</summary>
    public void Release()
    {
        EnsureActive();
        Status = ReservationStatus.Released;
    }

    /// <summary>FR-5.6 — auto-release timeout elapsed for an abandoned order.</summary>
    public void Expire()
    {
        EnsureActive();
        Status = ReservationStatus.Expired;
    }

    /// <summary>Order successfully paid/confirmed — the hold becomes a permanent decrement.</summary>
    public void Consume()
    {
        EnsureActive();
        Status = ReservationStatus.Consumed;
    }

    private void EnsureActive()
    {
        if (Status != ReservationStatus.Reserved)
            throw new DomainException($"Reservation '{Id}' is already '{Status}' and cannot change state.");
    }
}
