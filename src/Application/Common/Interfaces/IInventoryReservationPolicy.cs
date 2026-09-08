namespace ShippingSystem.Application.Common.Interfaces;

/// <summary>
/// FR-5.6 — "configurable and defaults to a short window (e.g., 15-30 minutes)".
/// Implemented in Infrastructure by reading appsettings/IOptions so the value can
/// change without a code deploy.
/// </summary>
public interface IInventoryReservationPolicy
{
    TimeSpan ExpiryWindow { get; }
}

/// <summary>
/// Generic idempotency guard for CLIENT-initiated requests (as opposed to
/// IProcessedWebhookEventRepository, which guards inbound PROVIDER webhooks).
/// Backs the NFR requirement that "critical order/shipment/payment creation
/// operations must be idempotent" — e.g. a client retrying a POST /orders call
/// after a timeout must not create a second order.
///
/// Callers pass a client-supplied key (e.g. an `Idempotency-Key` header) plus the
/// request's own type name so the same key can't collide across different command
/// types.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>Returns the id of the aggregate already created for this key, if any.</summary>
    Task<Guid?> TryGetExistingResultAsync(string requestType, string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Records that this key produced the given aggregate id, as part of the same transaction/SaveChanges.</summary>
    void StoreResult(string requestType, string idempotencyKey, Guid resultId);
}
