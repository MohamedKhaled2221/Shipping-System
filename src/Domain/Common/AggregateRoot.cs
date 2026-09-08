namespace ShippingSystem.Domain.Common;

/// <summary>
/// Marks an entity as an aggregate root — the only kind of entity the
/// Application layer is allowed to load/save directly via a repository.
/// Order, Product, Customer, Shipment, DeliveryAgent, Payment,
/// ShippingProviderConfig and ProcessedWebhookEvent are aggregate roots.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
}
