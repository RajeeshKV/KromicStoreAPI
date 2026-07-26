namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for caching services.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cache entries matching a pattern.
    /// </summary>
    Task ClearByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries matching a pattern.
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries for a specific tenant.
    /// </summary>
    Task RemoveTenantCacheAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
