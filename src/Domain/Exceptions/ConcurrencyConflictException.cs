namespace ShippingSystem.Domain.Exceptions;

/// <summary>
/// Thrown by the Application layer (translated from EF Core's DbUpdateConcurrencyException)
/// when a RowVersion mismatch is detected on Product, Order, or Shipment updates
/// (FR-5.3, FR-6.3, FR-6.5). Kept in Domain so Application/Domain services can throw it
/// without referencing EF Core types.
/// </summary>
public sealed class ConcurrencyConflictException : DomainException
{
    public string EntityType { get; }
    public object EntityId { get; }

    public ConcurrencyConflictException(string entityType, object entityId)
        : base($"{entityType} '{entityId}' was modified by another request. Please retry with the latest version.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
