using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ShippingSystem.Application.Common.Interfaces;

namespace ShippingSystem.Infrastructure.Caching;

/// <summary>
/// NFR Caching. Backed by IDistributedCache, which DependencyInjection.cs wires to Redis via
/// AddStackExchangeRedisCache — Application/Domain never see either type, only ICacheService.
///
/// Every method swallows exceptions and logs a warning instead of propagating: per
/// ICacheService's own doc comment, a Redis outage must degrade every caller straight back
/// to "always hits the database," never surface as a 500 to a customer refreshing a tracking
/// page or browsing products. A cache is allowed to be unavailable; it is never allowed to
/// be the reason a request fails.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes is null || bytes.Length == 0) return null;

            return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for key {CacheKey}; falling back to source.", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
            var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration };

            await _cache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            // A failed write just means the next read is a cache miss too — no correctness
            // impact, only a missed optimization.
            _logger.LogWarning(ex, "Cache write failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            // Worth logging louder than a read/write miss: a failed invalidation can leave a
            // stale entry alive for its full TTL. Still must not throw — see class doc comment.
            _logger.LogError(ex, "Cache invalidation failed for key {CacheKey}; a stale value may persist until its TTL expires.", key);
        }
    }
}
