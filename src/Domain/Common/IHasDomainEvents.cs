namespace ShippingSystem.Domain.Common;

/// <summary>
/// Non-generic counterpart to Entity&lt;TId&gt;'s domain-event bookkeeping. Entity&lt;TId&gt;
/// implements this so Infrastructure's SaveChanges interceptor can enumerate
/// `context.ChangeTracker.Entries&lt;IHasDomainEvents&gt;()` across every aggregate type in one
/// pass, without reflection and without depending on the generic TId parameter.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
