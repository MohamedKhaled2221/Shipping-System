using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>FR-10.1, FR-10.2. RecipientId is a Customer, DeliveryAgent, or Admin user id.</summary>
public sealed class Notification : AggregateRoot<Guid>
{
    public Guid RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Message { get; private set; } = default!;
    public NotificationStatus Status { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { } // EF Core

    public static Notification Create(Guid recipientId, NotificationType type, NotificationChannel channel, string message)
    {
        if (recipientId == Guid.Empty) throw new DomainException("Notification must have a recipient.");
        if (string.IsNullOrWhiteSpace(message)) throw new DomainException("Notification message is required.");

        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Type = type,
            Channel = channel,
            Message = message,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkFailed() => Status = NotificationStatus.Failed;
}
