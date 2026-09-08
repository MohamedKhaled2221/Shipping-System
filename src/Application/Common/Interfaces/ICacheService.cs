namespace ShippingSystem.Application.Common.Interfaces;

/// <summary>
/// NFR: "Caching strategy (Redis usage points)." Deliberately generic/technology-agnostic —
/// Application code calls Get/Set/Remove and never mentions Redis, `IDistributedCache`, or
/// serialization, matching every other abstraction in this layer (IPaymentGatewayService,
/// IShippingProvider, etc.). Every cache entry here is a read-side optimization only: nothing
/// in the solution's correctness (inventory reservation, optimistic concurrency, idempotency)
/// is allowed to depend on the cache being present, consistent, or even reachable — a Redis
/// outage must degrade to "every read hits the database," never to incorrect behavior.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
