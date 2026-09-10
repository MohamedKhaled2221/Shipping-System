namespace ShippingSystem.Infrastructure.Persistence;

/// <summary>
/// Backs Application.Common.Interfaces.IIdempotencyService — the CLIENT-retry idempotency
/// guard (as opposed to ProcessedWebhookEvent, which is a Domain entity guarding inbound
/// PROVIDER webhooks). Kept out of Domain for the same reason as TrackingNumberCounter:
/// it's "how do we make a retried POST safe," not a business concept.
/// Unique index on (RequestType, IdempotencyKey) — see IdempotencyRecordConfiguration.
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public string RequestType { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public Guid ResultId { get; set; }
    public DateTime CreatedAt { get; set; }
}
