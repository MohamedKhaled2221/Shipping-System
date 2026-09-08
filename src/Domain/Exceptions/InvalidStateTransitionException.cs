namespace ShippingSystem.Domain.Exceptions;

/// <summary>
/// FR-6.7 / FR-3.5 — thrown whenever a caller attempts a transition not present
/// in the OrderStateMachine or ShipmentStateMachine transition graph.
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public string EntityType { get; }
    public string FromStatus { get; }
    public string ToStatus { get; }

    public InvalidStateTransitionException(string entityType, string fromStatus, string toStatus)
        : base($"{entityType} cannot transition from '{fromStatus}' to '{toStatus}'.")
    {
        EntityType = entityType;
        FromStatus = fromStatus;
        ToStatus = toStatus;
    }
}
