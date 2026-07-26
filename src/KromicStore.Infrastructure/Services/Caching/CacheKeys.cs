namespace KromicStore.Infrastructure.Services.Caching;

/// <summary>
/// Static class containing cache key schemes with tenant isolation.
/// 
/// Key Format Strategy:
/// - Singles: {Prefix}:{TenantId}:{EntityType}:{EntityId}
/// - Collections: {Prefix}:{TenantId}:{EntityType}:list
/// - Searches: {Prefix}:{TenantId}:{EntityType}:search:{SearchHash}
/// - Tags: {Prefix}:{TenantId}:{EntityType}:tag
/// </summary>
public static class CacheKeys
{
    /// <summary>Global cache prefix.</summary>
    private const string Prefix = "kromic";

    /// <summary>Product cache prefix.</summary>
    private const string ProductPrefix = $"{Prefix}:product";

    /// <summary>Customer cache prefix.</summary>
    private const string CustomerPrefix = $"{Prefix}:customer";

    /// <summary>Order cache prefix.</summary>
    private const string OrderPrefix = $"{Prefix}:order";

    /// <summary>Configuration cache prefix.</summary>
    private const string ConfigPrefix = $"{Prefix}:config";

    /// <summary>Role cache prefix.</summary>
    private const string RolePrefix = $"{Prefix}:role";

    /// <summary>Category cache prefix.</summary>
    private const string CategoryPrefix = $"{Prefix}:category";

    /// <summary>User cache prefix.</summary>
    private const string UserPrefix = $"{Prefix}:user";

    /// <summary>Webhook cache prefix.</summary>
    private const string WebhookPrefix = $"{Prefix}:webhook";

    // ==================== Product Cache Keys ====================

    /// <summary>Gets cache key for a single product.</summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="productId">Product ID.</param>
    public static string Product(Guid tenantId, Guid productId) 
        => $"{ProductPrefix}:{tenantId}:{productId}";

    /// <summary>Gets cache key for all products of a tenant.</summary>
    public static string ProductList(Guid tenantId) 
        => $"{ProductPrefix}:{tenantId}:list";

    /// <summary>Gets cache key for products by category.</summary>
    public static string ProductsByCategory(Guid tenantId, Guid categoryId) 
        => $"{ProductPrefix}:{tenantId}:category:{categoryId}:list";

    /// <summary>Gets cache key for products by status.</summary>
    public static string ProductsByStatus(Guid tenantId, string status) 
        => $"{ProductPrefix}:{tenantId}:status:{status}:list";

    /// <summary>Gets cache key for product search results.</summary>
    public static string ProductSearch(Guid tenantId, string searchHash) 
        => $"{ProductPrefix}:{tenantId}:search:{searchHash}";

    /// <summary>Gets cache tag key for all products (for bulk invalidation).</summary>
    public static string ProductTag(Guid tenantId) 
        => $"{ProductPrefix}:{tenantId}:tag";

    // ==================== Category Cache Keys ====================

    /// <summary>Gets cache key for a single category.</summary>
    public static string Category(Guid tenantId, Guid categoryId) 
        => $"{CategoryPrefix}:{tenantId}:{categoryId}";

    /// <summary>Gets cache key for all categories of a tenant.</summary>
    public static string CategoryList(Guid tenantId) 
        => $"{CategoryPrefix}:{tenantId}:list";

    /// <summary>Gets cache key for category hierarchy.</summary>
    public static string CategoryHierarchy(Guid tenantId) 
        => $"{CategoryPrefix}:{tenantId}:hierarchy";

    /// <summary>Gets cache tag key for all categories (for bulk invalidation).</summary>
    public static string CategoryTag(Guid tenantId) 
        => $"{CategoryPrefix}:{tenantId}:tag";

    // ==================== Customer Cache Keys ====================

    /// <summary>Gets cache key for a single customer.</summary>
    public static string Customer(Guid tenantId, Guid customerId) 
        => $"{CustomerPrefix}:{tenantId}:{customerId}";

    /// <summary>Gets cache key for all customers of a tenant.</summary>
    public static string CustomerList(Guid tenantId) 
        => $"{CustomerPrefix}:{tenantId}:list";

    /// <summary>Gets cache key for customer by email.</summary>
    public static string CustomerByEmail(Guid tenantId, string email) 
        => $"{CustomerPrefix}:{tenantId}:email:{ComputeHash(email)}";

    /// <summary>Gets cache key for customer search results.</summary>
    public static string CustomerSearch(Guid tenantId, string searchHash) 
        => $"{CustomerPrefix}:{tenantId}:search:{searchHash}";

    /// <summary>Gets cache key for customer's orders list.</summary>
    public static string CustomerOrdersList(Guid tenantId, Guid customerId) 
        => $"{CustomerPrefix}:{tenantId}:{customerId}:orders:list";

    /// <summary>Gets cache tag key for all customers (for bulk invalidation).</summary>
    public static string CustomerTag(Guid tenantId) 
        => $"{CustomerPrefix}:{tenantId}:tag";

    // ==================== Order Cache Keys ====================

    /// <summary>Gets cache key for a single order.</summary>
    public static string Order(Guid tenantId, Guid orderId) 
        => $"{OrderPrefix}:{tenantId}:{orderId}";

    /// <summary>Gets cache key for all orders of a tenant.</summary>
    public static string OrderList(Guid tenantId) 
        => $"{OrderPrefix}:{tenantId}:list";

    /// <summary>Gets cache key for orders by status.</summary>
    public static string OrdersByStatus(Guid tenantId, string status) 
        => $"{OrderPrefix}:{tenantId}:status:{status}:list";

    /// <summary>Gets cache key for customer's orders.</summary>
    public static string CustomerOrders(Guid tenantId, Guid customerId) 
        => $"{OrderPrefix}:{tenantId}:customer:{customerId}:list";

    /// <summary>Gets cache tag key for all orders (for bulk invalidation).</summary>
    public static string OrderTag(Guid tenantId) 
        => $"{OrderPrefix}:{tenantId}:tag";

    // ==================== Configuration Cache Keys ====================

    /// <summary>Gets cache key for a single configuration value.</summary>
    public static string Config(Guid? tenantId, string key) 
        => $"{ConfigPrefix}:{(tenantId?.ToString() ?? "platform")}:{key}";

    /// <summary>Gets cache key for a configuration section.</summary>
    public static string ConfigSection(Guid? tenantId, string sectionPrefix) 
        => $"{ConfigPrefix}:{(tenantId?.ToString() ?? "platform")}:section:{sectionPrefix}:list";

    /// <summary>Gets cache tag key for all configurations (for bulk invalidation).</summary>
    public static string ConfigTag(Guid? tenantId) 
        => $"{ConfigPrefix}:{(tenantId?.ToString() ?? "platform")}:tag";

    // ==================== User/Role Cache Keys ====================

    /// <summary>Gets cache key for a single user.</summary>
    public static string User(Guid tenantId, Guid userId) 
        => $"{UserPrefix}:{tenantId}:{userId}";

    /// <summary>Gets cache key for user by email.</summary>
    public static string UserByEmail(Guid tenantId, string email) 
        => $"{UserPrefix}:{tenantId}:email:{ComputeHash(email)}";

    /// <summary>Gets cache key for user roles.</summary>
    public static string UserRoles(Guid tenantId, Guid userId) 
        => $"{UserPrefix}:{tenantId}:{userId}:roles";

    /// <summary>Gets cache key for roles list.</summary>
    public static string RoleList(Guid tenantId) 
        => $"{RolePrefix}:{tenantId}:list";

    /// <summary>Gets cache tag key for all users (for bulk invalidation).</summary>
    public static string UserTag(Guid tenantId) 
        => $"{UserPrefix}:{tenantId}:tag";

    // ==================== Webhook Cache Keys ====================

    /// <summary>Gets cache key for webhook configuration.</summary>
    public static string WebhookConfig(Guid tenantId, Guid webhookId) 
        => $"{WebhookPrefix}:{tenantId}:{webhookId}";

    /// <summary>Gets cache key for all webhooks of a tenant.</summary>
    public static string WebhookList(Guid tenantId) 
        => $"{WebhookPrefix}:{tenantId}:list";

    /// <summary>Gets cache key for active webhooks by event type.</summary>
    public static string WebhooksByEventType(Guid tenantId, string eventType) 
        => $"{WebhookPrefix}:{tenantId}:event:{eventType}:list";

    /// <summary>Gets cache tag key for all webhooks (for bulk invalidation).</summary>
    public static string WebhookTag(Guid tenantId) 
        => $"{WebhookPrefix}:{tenantId}:tag";

    // ==================== Cache Pattern Matching ====================

    /// <summary>Gets pattern for all product-related caches of a tenant.</summary>
    public static string ProductPattern(Guid tenantId) 
        => $"{ProductPrefix}:{tenantId}:*";

    /// <summary>Gets pattern for all category-related caches of a tenant.</summary>
    public static string CategoryPattern(Guid tenantId) 
        => $"{CategoryPrefix}:{tenantId}:*";

    /// <summary>Gets pattern for all customer-related caches of a tenant.</summary>
    public static string CustomerPattern(Guid tenantId) 
        => $"{CustomerPrefix}:{tenantId}:*";

    /// <summary>Gets pattern for all order-related caches of a tenant.</summary>
    public static string OrderPattern(Guid tenantId) 
        => $"{OrderPrefix}:{tenantId}:*";

    /// <summary>Gets pattern for all configuration caches (platform-wide invalidation).</summary>
    public static string ConfigPattern() 
        => $"{ConfigPrefix}:*";

    /// <summary>Gets pattern for all caches of a specific tenant.</summary>
    public static string TenantPattern(Guid tenantId) 
        => $"{Prefix}:*:{tenantId}:*";

    // ==================== Helper Methods ====================

    /// <summary>
    /// Computes a simple hash for sensitive values like emails to use in cache keys.
    /// </summary>
    private static string ComputeHash(string value)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value));
        return System.BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant()[..8];
    }
}
