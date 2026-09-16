using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShippingSystem.Infrastructure.HealthChecks;

/// <summary>
/// Readiness check for the Redis-backed cache. Deliberately injects IDistributedCache
/// directly rather than Application's ICacheService: RedisCacheService (Caching module) is
/// explicitly designed to swallow every exception and fail open — exactly the behavior that
/// makes it safe for request-handling code, and exactly why it would be USELESS here. A
/// health check exists specifically to surface the failure ICacheService is built to hide,
/// so this is the one place in the solution that's allowed to let a Redis exception mean
/// something.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private const string ProbeKey = "shippingsystem:healthcheck:probe";
    private static readonly byte[] ProbeValue = Encoding.UTF8.GetBytes("ok");

    private readonly IDistributedCache _cache;

    public RedisHealthCheck(IDistributedCache cache) => _cache = cache;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // A real set+get round trip, not just "can we open a socket" — proves both
            // directions of the exact operation RedisCacheService performs on every call.
            await _cache.SetAsync(
                ProbeKey,
                ProbeValue,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10) },
                cancellationToken);

            var readBack = await _cache.GetAsync(ProbeKey, cancellationToken);

            return readBack is not null
                ? HealthCheckResult.Healthy("Redis is reachable.")
                : HealthCheckResult.Unhealthy("Redis accepted a write but the read-back returned nothing.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check threw an exception.", ex);
        }
    }
}
