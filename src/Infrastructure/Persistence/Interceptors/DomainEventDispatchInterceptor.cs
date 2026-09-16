using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShippingSystem.Domain.Common;

namespace ShippingSystem.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Closes the loop described in Application's IUnitOfWork docs: collects every tracked
/// aggregate's DomainEvents just BEFORE SaveChanges runs (while the ChangeTracker still has
/// them), then — only once SaveChanges has actually succeeded — publishes them through
/// MediatR's IPublisher and clears them off the aggregates.
///
/// Registered as Scoped (see DependencyInjection.cs) so IPublisher resolves the same
/// MediatR pipeline/handlers as everything else in the current request scope.
/// </summary>
public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;
    private readonly List<IDomainEvent> _pendingEvents = new();

    public DomainEventDispatchInterceptor(IPublisher publisher) => _publisher = publisher;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CollectPendingEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // Snapshot-then-clear so a slow/failed publish on one event can't cause the same
        // event to be re-collected and re-published on a later, unrelated SaveChanges call.
        var eventsToPublish = _pendingEvents.ToList();
        _pendingEvents.Clear();

        foreach (var domainEvent in eventsToPublish)
            await _publisher.Publish(domainEvent, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CollectPendingEvents(DbContext? context)
    {
        if (context is null) return;

        var entitiesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            _pendingEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
        }
    }
}
