using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Domain.Events;

public sealed record OrderCreatedEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount) : DomainEventBase;

public sealed record OrderStatusChangedEvent(Guid OrderId, OrderStatus OldStatus, OrderStatus NewStatus) : DomainEventBase;

/// <summary>Consumed by the Inventory module to release reservations (FR-3.2 §2.2 step 11, FR-5.4).</summary>
public sealed record OrderCancelledEvent(Guid OrderId) : DomainEventBase;

public sealed record PaymentStatusChangedEvent(Guid OrderId, PaymentStatus NewStatus) : DomainEventBase;
