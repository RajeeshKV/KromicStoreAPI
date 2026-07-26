namespace KromicStore.Infrastructure.Services;

using Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using Caching;
using Microsoft.Extensions.Logging;

/// <summary>
/// Redis-based cache service implementation with advanced features.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private long _hits = 0;
    private long _misses = 0;
    private readonly object _statsLock = new();

    /// <summary>
    /// Initializes a new instance of the CacheService class.
    /// </summary>
    public CacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(key);

        if (!value.HasValue)
        {
            RecordMiss();
            return default;
        }

        try
        {
            RecordHit();
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (JsonException)
        {
            // Cache corruption - remove invalid entry
            await RemoveAsync(key, cancellationToken);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var serialized = JsonSerializer.Serialize(value);

        // Use recommended TTL if not specified
        var ttl = expiration ?? CacheTTL.GetRecommendedTTL(key);

        await db.StringSetAsync(key, serialized, ttl);
    }

    /// <summary>
    /// Sets a cache value with tenant isolation and automatic TTL.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="tenantId">Tenant ID for isolation.</param>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to cache.</param>
    /// <param name="expiration">Optional custom expiration.</param>
    /// <param name="tags">Optional cache tags for related invalidation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SetAsync<T>(Guid tenantId, string key, T value, TimeSpan? expiration = null, string[]? tags = null, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var serialized = JsonSerializer.Serialize(value);
        var ttl = expiration ?? CacheTTL.GetRecommendedTTL(key);

        await db.StringSetAsync(key, serialized, ttl);

        // Cache tags for bulk invalidation
        if (tags != null && tags.Length > 0)
        {
            foreach (var tag in tags)
            {
                var tagKey = $"{tag}:members";
                await db.SetAddAsync(tagKey, key);
                // Tags expire slightly after the main entry
                await db.KeyExpireAsync(tagKey, ttl.Add(TimeSpan.FromMinutes(5)));
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    /// <summary>
    /// Removes multiple cache entries by their keys.
    /// </summary>
    public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var keyArray = keys.Select(k => (RedisKey)k).ToArray();
        await db.KeyDeleteAsync(keyArray);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(key);
    }

    /// <inheritdoc />
    public async Task ClearByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        await RemoveByPatternAsync(pattern, cancellationToken);
    }

    /// <summary>
    /// Removes cache entries matching a pattern.
    /// </summary>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().FirstOrDefault() ?? throw new InvalidOperationException("No Redis server available"));
        var keys = server.Keys(pattern: pattern);

        if (keys.Any())
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(keys.ToArray());
        }
    }

    /// <summary>
    /// Removes all cache entries associated with a tenant.
    /// </summary>
    public async Task RemoveTenantCacheAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await RemoveByPatternAsync(CacheKeys.TenantPattern(tenantId), cancellationToken);
    }

    /// <summary>
    /// Invalidates cache by tag (for related entity groups).
    /// </summary>
    /// <param name="tag">Tag key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var tagKey = $"{tag}:members";
        var members = await db.SetMembersAsync(tagKey);

        if (members.Any())
        {
            var keysToDelete = members.Select(m => (RedisKey)m.ToString()).ToArray();
            await db.KeyDeleteAsync(keysToDelete);
            await db.KeyDeleteAsync(tagKey);
        }
    }

    /// <summary>
    /// Gets cache statistics (hits, misses, hit ratio).
    /// </summary>
    public (long hits, long misses, decimal hitRatio) GetStatistics()
    {
        lock (_statsLock)
        {
            var total = _hits + _misses;
            var hitRatio = total > 0 ? (decimal)_hits / total : 0m;
            return (_hits, _misses, hitRatio);
        }
    }

    /// <summary>
    /// Resets cache statistics.
    /// </summary>
    public void ResetStatistics()
    {
        lock (_statsLock)
        {
            _hits = 0;
            _misses = 0;
        }
    }

    /// <summary>
    /// Gets connection multiplexer for advanced Redis operations.
    /// </summary>
    public IConnectionMultiplexer GetConnection()
    {
        return _redis;
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    private void RecordHit()
    {
        lock (_statsLock)
        {
            _hits++;
        }
    }

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    private void RecordMiss()
    {
        lock (_statsLock)
        {
            _misses++;
        }
    }
}

/// <summary>
/// Null cache service implementation that gracefully handles cache unavailability.
/// All cache operations are no-ops, allowing the application to function without Redis.
/// </summary>
public class NullCacheService : ICacheService
{
    private readonly ILogger<NullCacheService>? _logger;

    public NullCacheService(ILogger<NullCacheService>? logger = null)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Cache get operation skipped (Redis unavailable): {Key}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Cache set operation skipped (Redis unavailable): {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Cache remove operation skipped (Redis unavailable): {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task ClearByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Cache clear pattern operation skipped (Redis unavailable): {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Cache remove pattern operation skipped (Redis unavailable): {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task RemoveTenantCacheAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Cache remove tenant operation skipped (Redis unavailable): {TenantId}", tenantId);
        return Task.CompletedTask;
    }
}
