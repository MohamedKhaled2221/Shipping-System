using ShippingSystem.Domain.Common;

namespace ShippingSystem.Domain.Events;

public sealed record InventoryReservedEvent(Guid ProductId, Guid OrderId, int Quantity) : DomainEventBase;

public sealed record InventoryReleasedEvent(Guid ProductId, Guid OrderId, int Quantity, string Reason) : DomainEventBase;
