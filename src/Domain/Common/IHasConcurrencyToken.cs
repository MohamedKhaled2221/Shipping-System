namespace ShippingSystem.Domain.Common;

/// <summary>
/// Implemented by every aggregate that requires optimistic concurrency control
/// (Product, Order, Shipment — per FR-5.3, FR-6.3, FR-6.5).
/// EF Core maps this to a SQL Server ROWVERSION/TIMESTAMP column.
/// The Application layer must catch DbUpdateConcurrencyException and translate
/// it to a ConcurrencyConflictException so the Domain stays persistence-agnostic.
/// </summary>
public interface IHasConcurrencyToken
{
    byte[] RowVersion { get; }
}
