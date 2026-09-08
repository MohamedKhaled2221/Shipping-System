using MediatR;

namespace ShippingSystem.Domain.Common;

/// <summary>
/// Marker contract for all domain events raised by aggregates.
/// Dispatched by the Application layer (e.g. via MediatR) after a successful
/// unit-of-work commit — the Domain layer never dispatches events itself.
/// Implements MediatR.INotification (from the dependency-free MediatR.Contracts
/// package — see Domain.csproj) purely so Infrastructure's
/// DomainEventDispatchInterceptor can pass instances straight to IPublisher.Publish;
/// Domain itself never touches IPublisher/IMediator/the MediatR pipeline.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
