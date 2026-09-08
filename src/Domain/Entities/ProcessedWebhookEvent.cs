using ShippingSystem.Domain.Common;
using ShippingSystem.Domain.Exceptions;

namespace ShippingSystem.Domain.Entities;

/// <summary>
/// FR-11.4, FR-4.7. The idempotency ledger: before processing any inbound webhook
/// (shipping provider status callback OR payment gateway callback), the Application
/// layer must check for an existing row with the same (ProviderName, ExternalEventId)
/// pair via a unique DB index — if found, discard the duplicate without side effects.
/// </summary>
public sealed class ProcessedWebhookEvent : AggregateRoot<Guid>
{
    public string ProviderName { get; private set; } = default!;
    public string ExternalEventId { get; private set; } = default!;
    public DateTime ProcessedAt { get; private set; }

    private ProcessedWebhookEvent() { } // EF Core

    public static ProcessedWebhookEvent Create(string providerName, string externalEventId)
    {
        if (string.IsNullOrWhiteSpace(providerName)) throw new DomainException("Provider name is required.");
        if (string.IsNullOrWhiteSpace(externalEventId)) throw new DomainException("External event id is required.");

        return new ProcessedWebhookEvent
        {
            Id = Guid.NewGuid(),
            ProviderName = providerName,
            ExternalEventId = externalEventId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
