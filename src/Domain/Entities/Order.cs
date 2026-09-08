using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Events;
using ShippingSystem.Domain.Exceptions;
using ShippingSystem.Domain.StateMachines;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// FR-3.2 to FR-3.5, FR-4.1. OrderStatus and PaymentStatus are tracked side by side but
/// are NEVER derived from one another inside this class — cross-cutting rules that need
/// both (e.g. FR-4.3/4.5) live in Domain.Services, not here, to keep this aggregate's
/// invariants local and easy to reason about.
/// </summary>
public sealed class Order : AggregateRoot<Guid>, IHasConcurrencyToken
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public Guid ShippingAddressId { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Core

    public static Order Create(
        Guid customerId,
        Guid shippingAddressId,
        PaymentMethod paymentMethod,
        IEnumerable<(Guid ProductId, int Quantity, decimal UnitPrice)> items)
    {
        if (customerId == Guid.Empty) throw new DomainException("Order must belong to a customer.");
        if (shippingAddressId == Guid.Empty) throw new DomainException("Order must have a shipping address.");

        var itemList = items.Select(i => OrderItem.Create(i.ProductId, i.Quantity, i.UnitPrice)).ToList();
        if (itemList.Count == 0) throw new DomainException("Order must contain at least one item.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ShippingAddressId = shippingAddressId,
            PaymentMethod = paymentMethod,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in itemList)
        {
            item.AssignToOrder(order.Id);
            order._items.Add(item);
        }
        order.TotalAmount = order._items.Sum(i => i.LineTotal);

        order.AddDomainEvent(new OrderCreatedEvent(order.Id, order.CustomerId, order.TotalAmount));
        return order;
    }

    /// <summary>FR-3.5 — validated purely against OrderStateMachine; no payment logic here.</summary>
    public void TransitionTo(OrderStatus newStatus)
    {
        if (!OrderStateMachine.CanTransition(Status, newStatus))
            throw new InvalidStateTransitionException(nameof(Order), Status.ToString(), newStatus.ToString());

        var old = Status;
        Status = newStatus;
        AddDomainEvent(new OrderStatusChangedEvent(Id, old, newStatus));

        if (newStatus == OrderStatus.Cancelled)
            AddDomainEvent(new OrderCancelledEvent(Id));
    }

    /// <summary>
    /// FR-4.1 — updates PaymentStatus only. Does NOT touch OrderStatus; the Application layer
    /// (or a Domain.Services policy) decides whether a payment event should also drive
    /// an Order transition (e.g. Failed payment -> Cancelled per FR-4.5).
    /// </summary>
    public void UpdatePaymentStatus(PaymentStatus newStatus)
    {
        if (PaymentStatus == newStatus) return; // idempotent no-op, see FR-4.7
        PaymentStatus = newStatus;
        AddDomainEvent(new PaymentStatusChangedEvent(Id, newStatus));
    }

    public bool IsTerminal() => OrderStateMachine.IsTerminal(Status);
}
