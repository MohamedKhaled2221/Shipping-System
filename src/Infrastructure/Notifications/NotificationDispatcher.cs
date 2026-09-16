using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Abstractions.Notifications;
using ShippingSystem.Domain.Entities;
using ShippingSystem.Domain.Enums;
using ShippingSystem.Infrastructure.Persistence;

namespace ShippingSystem.Infrastructure.Notifications;

/// <summary>
/// FR-10.1, FR-10.2. Uses its own short-lived DbContext via IDbContextFactory rather than
/// the ambient scoped one, for the same reason as TrackingNumberGenerator: a command
/// handler that raises a notification mid-transaction shouldn't have that notification's
/// persistence tied to (or rolled back by) the outcome of unrelated changes still pending
/// on the caller's unit of work. Safe here specifically because Notification never raises
/// its own domain events (see Domain.Entities.Notification) — this isolated context never
/// needs DomainEventDispatchInterceptor attached, unlike the main scoped AppDbContext.
///
/// Only stages the row and marks it Sent immediately — actual channel delivery (SMTP relay,
/// SMS gateway, push service, SignalR hub broadcast) is a later Notification module's job;
/// swapping this stub for real per-channel senders means calling notification.MarkSent()/
/// MarkFailed() based on the real outcome instead of always succeeding as done here.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private static readonly IReadOnlyDictionary<NotificationType, NotificationChannel[]> DefaultChannels =
        new Dictionary<NotificationType, NotificationChannel[]>
        {
            [NotificationType.ShipmentCreated] = new[] { NotificationChannel.Email, NotificationChannel.SignalR },
            [NotificationType.ShipmentConfirmed] = new[] { NotificationChannel.SignalR },
            [NotificationType.ShipmentAssignedToCourier] = new[] { NotificationChannel.Push, NotificationChannel.SignalR },
            [NotificationType.ShipmentPickedUp] = new[] { NotificationChannel.SignalR },
            [NotificationType.ShipmentOutForDelivery] = new[] { NotificationChannel.Push, NotificationChannel.Sms, NotificationChannel.SignalR },
            [NotificationType.ShipmentDelivered] = new[] { NotificationChannel.Email, NotificationChannel.Push, NotificationChannel.SignalR },
            [NotificationType.ShipmentFailed] = new[] { NotificationChannel.Email, NotificationChannel.Push, NotificationChannel.SignalR },
            [NotificationType.ShipmentCancelled] = new[] { NotificationChannel.Email, NotificationChannel.SignalR },
            [NotificationType.ShipmentReturned] = new[] { NotificationChannel.Email, NotificationChannel.SignalR },
            [NotificationType.PaymentPaid] = new[] { NotificationChannel.Email },
            [NotificationType.PaymentFailed] = new[] { NotificationChannel.Email, NotificationChannel.Push },
            [NotificationType.PaymentRefunded] = new[] { NotificationChannel.Email }
        };

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<NotificationDispatcher> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(
        Guid recipientId,
        NotificationType type,
        string message,
        IReadOnlyCollection<NotificationChannel>? channels = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedChannels = channels is { Count: > 0 }
            ? channels
            : DefaultChannels.GetValueOrDefault(type, new[] { NotificationChannel.Email });

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        foreach (var channel in resolvedChannels)
        {
            var notification = Notification.Create(recipientId, type, channel, message);
            notification.MarkSent(); // stub — replace with a real per-channel send result
            dbContext.Notifications.Add(notification);

            _logger.LogInformation(
                "Notification {Type} for recipient {RecipientId} queued via {Channel}",
                type, recipientId, channel);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
