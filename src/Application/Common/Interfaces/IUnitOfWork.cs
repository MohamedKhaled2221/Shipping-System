namespace ShippingSystem.Application.Common.Interfaces;

/// <summary>
/// Wraps a single database transaction spanning one or more repository operations.
/// Implemented in Infrastructure by wrapping EF Core's DbContext.SaveChangesAsync.
/// Every command handler that mutates state calls SaveChangesAsync exactly once,
/// at the end, so all repository writes commit atomically.
///
/// This is also where domain events get dispatched: the Infrastructure implementation
/// collects IDomainEvent instances from every tracked aggregate's DomainEvents
/// collection, publishes them via MediatR AFTER a successful commit, then calls
/// ClearDomainEvents() on each aggregate.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes. Throws Domain.Exceptions.ConcurrencyConflictException
    /// (translated from EF Core's DbUpdateConcurrencyException) when a RowVersion mismatch
    /// is detected on Product, Order, or Shipment (FR-5.3, FR-6.3, FR-6.5).
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
