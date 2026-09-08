using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Domain.Events;

/// <summary>Consumed by the Notification module (FR-10.1) and Tracking module.</summary>
public sealed record ShipmentCreatedEvent(Guid ShipmentId, Guid OrderId, string TrackingNumber) : DomainEventBase;

public sealed record ShipmentStatusChangedEvent(
    Guid ShipmentId,
    Guid OrderId,
    ShipmentStatus OldStatus,
    ShipmentStatus NewStatus,
    string ChangedBy) : DomainEventBase;

public sealed record ShipmentAssignedEvent(Guid ShipmentId, Guid DeliveryAgentId) : DomainEventBase;

public sealed record DeliveryFailedEvent(Guid ShipmentId, string Reason) : DomainEventBase;
