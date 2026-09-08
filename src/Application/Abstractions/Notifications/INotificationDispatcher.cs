using ShippingSystem.Domain.Enums;

namespace ShippingSystem.Application.Abstractions.Notifications;

/// <summary>
/// FR-10.1, FR-10.2. Application code raises a notification intent through this single
/// interface; Infrastructure fans it out to the right channel-specific sender
/// (Email/SMS/Push/SignalR) and persists a Notification row per attempt.
/// Typically implemented as a MediatR INotificationHandler that reacts to the domain
/// events in ShippingSystem.Domain.Events (ShipmentCreatedEvent, PaymentStatusChangedEvent, etc.).
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(
        Guid recipientId,
        NotificationType type,
        string message,
        IReadOnlyCollection<NotificationChannel>? channels = null,
        CancellationToken cancellationToken = default);
}
