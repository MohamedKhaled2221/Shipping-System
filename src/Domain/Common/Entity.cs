namespace ShippingSystem.Domain.Common;

/// <summary>
/// Base class for all entities. Identity-based equality (never structural).
/// </summary>
public abstract class Entity<TId> : IHasDomainEvents where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>Events raised by this entity, pending dispatch after a successful commit.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Called by the persistence layer (Infrastructure) after events have been dispatched.</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (Id.Equals(default(TId))) return false;
        if (other.Id.Equals(default(TId))) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => (GetType(), Id).GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
