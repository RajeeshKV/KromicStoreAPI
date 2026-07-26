namespace KromicStore.Infrastructure.Services.Caching;

/// <summary>
/// Cache TTL (Time To Live) strategy configuration.
/// Defines default expiration times for different cache entry types.
/// </summary>
public static class CacheTTL
{
    /// <summary>Default cache TTL for products (1 hour).</summary>
    public static readonly TimeSpan ProductDefault = TimeSpan.FromHours(1);

    /// <summary>Extended cache TTL for product lists (30 minutes).</summary>
    public static readonly TimeSpan ProductListExtended = TimeSpan.FromMinutes(30);

    /// <summary>Short cache TTL for product search results (15 minutes).</summary>
    public static readonly TimeSpan ProductSearchShort = TimeSpan.FromMinutes(15);

    /// <summary>Default cache TTL for categories (1 hour).</summary>
    public static readonly TimeSpan CategoryDefault = TimeSpan.FromHours(1);

    /// <summary>Extended cache TTL for category hierarchy (2 hours).</summary>
    public static readonly TimeSpan CategoryHierarchyExtended = TimeSpan.FromHours(2);

    /// <summary>Default cache TTL for customers (1 hour).</summary>
    public static readonly TimeSpan CustomerDefault = TimeSpan.FromHours(1);

    /// <summary>Short cache TTL for customer search results (10 minutes).</summary>
    public static readonly TimeSpan CustomerSearchShort = TimeSpan.FromMinutes(10);

    /// <summary>Short cache TTL for orders (5 minutes).</summary>
    public static readonly TimeSpan OrderDefault = TimeSpan.FromMinutes(5);

    /// <summary>Very short cache TTL for order status (2 minutes).</summary>
    public static readonly TimeSpan OrderStatusVeryShort = TimeSpan.FromMinutes(2);

    /// <summary>Medium cache TTL for configuration (30 minutes).</summary>
    public static readonly TimeSpan ConfigDefault = TimeSpan.FromMinutes(30);

    /// <summary>Extended cache TTL for platform-wide configuration (1 hour).</summary>
    public static readonly TimeSpan ConfigPlatformExtended = TimeSpan.FromHours(1);

    /// <summary>Medium cache TTL for roles (15 minutes).</summary>
    public static readonly TimeSpan RoleDefault = TimeSpan.FromMinutes(15);

    /// <summary>Default cache TTL for user profile (30 minutes).</summary>
    public static readonly TimeSpan UserDefault = TimeSpan.FromMinutes(30);

    /// <summary>Extended cache TTL for user roles (1 hour).</summary>
    public static readonly TimeSpan UserRolesExtended = TimeSpan.FromHours(1);

    /// <summary>Medium cache TTL for webhook configurations (30 minutes).</summary>
    public static readonly TimeSpan WebhookDefault = TimeSpan.FromMinutes(30);

    /// <summary>Short cache TTL for active webhooks list (10 minutes).</summary>
    public static readonly TimeSpan WebhookListShort = TimeSpan.FromMinutes(10);

    /// <summary>Very short cache TTL for frequently changing data (1 minute).</summary>
    public static readonly TimeSpan VeryShort = TimeSpan.FromMinutes(1);

    /// <summary>Short cache TTL (5 minutes).</summary>
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);

    /// <summary>Medium cache TTL (15 minutes).</summary>
    public static readonly TimeSpan Medium = TimeSpan.FromMinutes(15);

    /// <summary>Long cache TTL (1 hour).</summary>
    public static readonly TimeSpan Long = TimeSpan.FromHours(1);

    /// <summary>Extended cache TTL (2 hours).</summary>
    public static readonly TimeSpan Extended = TimeSpan.FromHours(2);

    /// <summary>Very long cache TTL (1 day) - for static/rarely changing data.</summary>
    public static readonly TimeSpan VeryLong = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets the recommended TTL for the given cache key type.
    /// Returns a reasonable default based on the entity type.
    /// </summary>
    /// <param name="cacheKey">The cache key to determine TTL for.</param>
    /// <returns>TimeSpan representing the recommended cache duration.</returns>
    public static TimeSpan GetRecommendedTTL(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
            return Medium;

        // Extract entity type from key format: prefix:tenantId:entityType...
        var parts = cacheKey.Split(':');
        if (parts.Length < 3)
            return Medium;

        var entityType = parts[2].ToLowerInvariant();

        return entityType switch
        {
            "product" => cacheKey.Contains("search") ? ProductSearchShort : ProductDefault,
            "category" => cacheKey.Contains("hierarchy") ? CategoryHierarchyExtended : CategoryDefault,
            "customer" => cacheKey.Contains("search") ? CustomerSearchShort : CustomerDefault,
            "order" => cacheKey.Contains("status") ? OrderStatusVeryShort : OrderDefault,
            "config" => ConfigDefault,
            "role" => RoleDefault,
            "user" => cacheKey.Contains("roles") ? UserRolesExtended : UserDefault,
            "webhook" => WebhookDefault,
            _ => Medium
        };
    }

    /// <summary>
    /// Gets the TTL for a cache key as a nullable TimeSpan.
    /// </summary>
    /// <param name="cacheKey">The cache key.</param>
    /// <returns>TimeSpan? - TTL value, or null to use no expiration.</returns>
    public static TimeSpan? GetTTL(string cacheKey)
    {
        return GetRecommendedTTL(cacheKey);
    }

    /// <summary>
    /// Gets the TTL for a specific cache value type.
    /// </summary>
    /// <param name="ttlType">The type of TTL duration.</param>
    /// <returns>TimeSpan representing the cache duration.</returns>
    public static TimeSpan Get(CacheTTLType ttlType) => ttlType switch
    {
        CacheTTLType.VeryShort => VeryShort,
        CacheTTLType.Short => Short,
        CacheTTLType.Medium => Medium,
        CacheTTLType.Long => Long,
        CacheTTLType.Extended => Extended,
        CacheTTLType.VeryLong => VeryLong,
        _ => Medium
    };
}

/// <summary>
/// Enumeration of cache TTL types.
/// </summary>
public enum CacheTTLType
{
    /// <summary>Very short duration (1 minute).</summary>
    VeryShort = 0,

    /// <summary>Short duration (5 minutes).</summary>
    Short = 1,

    /// <summary>Medium duration (15 minutes).</summary>
    Medium = 2,

    /// <summary>Long duration (1 hour).</summary>
    Long = 3,

    /// <summary>Extended duration (2 hours).</summary>
    Extended = 4,

    /// <summary>Very long duration (1 day).</summary>
    VeryLong = 5
}
