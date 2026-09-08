using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Events;
using ShippingSystem.Domain.Exceptions;
using ShippingSystem.Domain.StateMachines;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// FR-6.1 to FR-6.7, FR-9.x. Owns its own status-history log. Deliberately does NOT hold a
/// reference to the Order entity (separate aggregate, separate consistency boundary) — only
/// OrderId. Cross-aggregate rules such as "prepaid orders can't progress past Confirmed
/// unless Paid" (FR-4.3) are enforced by Domain.Services.ShipmentPaymentGuard, which the
/// Application layer calls with both aggregates loaded, BEFORE invoking TransitionTo here.
/// </summary>
public sealed class Shipment : AggregateRoot<Guid>, IHasConcurrencyToken
{
    private readonly List<ShipmentStatusHistory> _statusHistory = new();
    private readonly List<FailedDeliveryReason> _failedDeliveryReasons = new();

    public Guid OrderId { get; private set; }
    public string TrackingNumber { get; private set; } = default!;
    public Guid ShippingAddressId { get; private set; }
    public decimal ShippingFee { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public Guid? DeliveryAgentId { get; private set; }
    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// FR-11.1/11.2 gap-fill — the SRS's core data model (§6) doesn't list these, but
    /// CancelBookingAsync(providerReference) on IShippingProvider is meaningless without
    /// somewhere to have stored the reference a booking call returned. Both null until
    /// RecordProviderBooking runs (Application's Shipment module, after ShipmentCreatedEvent),
    /// which is why Shipment can briefly exist with a TrackingNumber but no provider booking yet.
    /// </summary>
    public string? ProviderName { get; private set; }
    public string? ProviderReference { get; private set; }

    public IReadOnlyCollection<ShipmentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<FailedDeliveryReason> FailedDeliveryReasons => _failedDeliveryReasons.AsReadOnly();

    private Shipment() { } // EF Core

    /// <summary>
    /// FR-6.1 — created only once the order is in a valid state to ship (checked by the
    /// Application layer using ShipmentPaymentGuard before calling this factory).
    /// </summary>
    public static Shipment Create(
        Guid orderId, string trackingNumber, Guid shippingAddressId,
        decimal shippingFee, DateTime? estimatedDeliveryDate = null)
    {
        if (orderId == Guid.Empty) throw new DomainException("Shipment must reference an order.");
        if (string.IsNullOrWhiteSpace(trackingNumber)) throw new DomainException("Tracking number is required.");
        if (shippingAddressId == Guid.Empty) throw new DomainException("Shipment must have a shipping address.");
        if (shippingFee < 0) throw new DomainException("Shipping fee cannot be negative.");

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TrackingNumber = trackingNumber,
            ShippingAddressId = shippingAddressId,
            ShippingFee = shippingFee,
            Status = ShipmentStatus.Pending,
            EstimatedDeliveryDate = estimatedDeliveryDate,
            CreatedAt = DateTime.UtcNow
        };

        shipment._statusHistory.Add(ShipmentStatusHistory.Create(shipment.Id, ShipmentStatus.Pending, "System", "Shipment created."));
        shipment.AddDomainEvent(new ShipmentCreatedEvent(shipment.Id, shipment.OrderId, shipment.TrackingNumber));
        return shipment;
    }

    /// <summary>
    /// FR-6.5, FR-6.7 — validated against ShipmentStateMachine and recorded to history.
    /// Callers must invoke ShipmentPaymentGuard.EnsureCanProgress(order, newStatus) first
    /// when the shipment belongs to a prepaid order (FR-4.3).
    /// </summary>
    public void TransitionTo(ShipmentStatus newStatus, string changedBy, string? notes = null)
    {
        if (!ShipmentStateMachine.CanTransition(Status, newStatus))
            throw new InvalidStateTransitionException(nameof(Shipment), Status.ToString(), newStatus.ToString());

        var old = Status;
        Status = newStatus;
        _statusHistory.Add(ShipmentStatusHistory.Create(Id, newStatus, changedBy, notes));
        AddDomainEvent(new ShipmentStatusChangedEvent(Id, OrderId, old, newStatus, changedBy));
    }

    /// <summary>FR-6.3 — assignment itself is guarded by RowVersion at the persistence layer to prevent double-assignment.</summary>
    public void AssignToAgent(Guid deliveryAgentId)
    {
        if (deliveryAgentId == Guid.Empty) throw new DomainException("A valid delivery agent must be provided.");
        if (ShipmentStateMachine.IsTerminal(Status))
            throw new DomainException($"Cannot assign an agent to a shipment in terminal status '{Status}'.");

        DeliveryAgentId = deliveryAgentId;
        AddDomainEvent(new ShipmentAssignedEvent(Id, deliveryAgentId));
    }

    /// <summary>FR-7.5 — transitions to Failed and records the reason atomically.</summary>
    public void RecordFailure(string reason, string changedBy)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Failure reason is required.");

        TransitionTo(ShipmentStatus.Failed, changedBy, reason);
        _failedDeliveryReasons.Add(FailedDeliveryReason.Create(Id, reason));
        AddDomainEvent(new DeliveryFailedEvent(Id, reason));
    }

    /// <summary>FR-6.4 — return with reason, recorded as a status-history note.</summary>
    public void ReturnShipment(string reason, string changedBy) =>
        TransitionTo(ShipmentStatus.Returned, changedBy, reason);

    /// <summary>
    /// FR-11.1 — recorded once IShippingProvider.BookShipmentAsync succeeds (Application's
    /// Shipment module, reacting to ShipmentCreatedEvent). Deliberately does not validate
    /// providerReference is non-empty: some providers/flows may legitimately book without
    /// returning one (see LocalShippingProvider), and the booking's Success flag — not the
    /// presence of a reference — is what the caller already checked before calling this.
    /// </summary>
    public void RecordProviderBooking(string providerName, string? providerReference)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new DomainException("Provider name is required to record a booking.");

        ProviderName = providerName;
        ProviderReference = providerReference;
    }

    /// <summary>
    /// FR-9.2 (estimated delivery date) — settable independently of Create because the real
    /// estimate often only becomes available from IShippingProvider.EstimateDeliveryDateAsync
    /// after booking, not at the moment the Shipment row itself is created.
    /// </summary>
    public void SetEstimatedDeliveryDate(DateTime estimatedDeliveryDate) =>
        EstimatedDeliveryDate = estimatedDeliveryDate;

    public bool IsTerminal() => ShipmentStateMachine.IsTerminal(Status);
}
