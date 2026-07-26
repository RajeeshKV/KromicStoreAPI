namespace KromicStore.Infrastructure.Services.Caching;

using Domain.Events;
using Microsoft.Extensions.Logging;
using Application.Interfaces;

/// <summary>
/// Service for handling cache invalidation based on domain events.
/// Automatically invalidates related caches when entities change.
/// </summary>
public class CacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationService> _logger;

    /// <summary>
    /// Initializes a new instance of CacheInvalidationService.
    /// </summary>
    public CacheInvalidationService(ICacheService cacheService, ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Handles domain event and invalidates related caches.
    /// </summary>
    /// <param name="domainEvent">Domain event that triggered the invalidation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task HandleEventAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (domainEvent)
            {
                // Product events - invalidate product and category caches
                case ProductCreatedEvent e:
                    await InvalidateProductCacheAsync(e.TenantId, e.EntityId, e.CategoryId, cancellationToken);
                    break;

                case ProductUpdatedEvent e:
                    await InvalidateProductCacheAsync(e.TenantId, e.EntityId, e.CategoryId, cancellationToken);
                    // Also invalidate previous category if it changed
                    if (e.PreviousCategoryId.HasValue && e.PreviousCategoryId != e.CategoryId)
                    {
                        await InvalidateCategoryCacheAsync(e.TenantId, e.PreviousCategoryId.Value, cancellationToken);
                    }
                    break;

                case ProductPublishedEvent e:
                    await InvalidateProductCacheAsync(e.TenantId, e.EntityId, e.CategoryId, cancellationToken);
                    break;

                case ProductUnpublishedEvent e:
                    await InvalidateProductCacheAsync(e.TenantId, e.EntityId, e.CategoryId, cancellationToken);
                    break;

                case ProductDeletedEvent e:
                    await InvalidateProductCacheAsync(e.TenantId, e.EntityId, e.CategoryId, cancellationToken);
                    break;

                // Order events - invalidate order and customer order list caches
                case OrderCreatedEvent e:
                    await InvalidateOrderCacheAsync(e.TenantId, e.EntityId, e.CustomerId, cancellationToken);
                    break;

                case OrderStatusChangedEvent e:
                    await InvalidateOrderCacheAsync(e.TenantId, e.EntityId, e.CustomerId, cancellationToken);
                    break;

                case OrderConfirmedEvent e:
                    await InvalidateOrderCacheAsync(e.TenantId, e.EntityId, e.CustomerId, cancellationToken);
                    break;

                case OrderPaidEvent e:
                    await InvalidateOrderCacheAsync(e.TenantId, e.EntityId, e.CustomerId, cancellationToken);
                    break;

                case OrderShippedEvent e:
                    await InvalidateOrderCacheAsync(e.TenantId, e.EntityId, e.CustomerId, cancellationToken);
                    break;

                case OrderDeliveredEvent e:
                    await InvalidateOrderCacheAsync(e.TenantId, e.EntityId, e.CustomerId, cancellationToken);
                    break;

                case OrderCancelledEvent e:
                    await InvalidateOrderCacheAsync(e.TenantId, e.EntityId, e.CustomerId, cancellationToken);
                    break;

                default:
                    _logger.LogDebug("No cache invalidation handler for event type: {EventType}", domainEvent.GetType().Name);
                    break;
            }

            _logger.LogInformation(
                "Cache invalidation handled for event: {EventType}, TenantId: {TenantId}, EntityId: {EntityId}",
                domainEvent.GetEventType(),
                domainEvent.TenantId,
                domainEvent.EntityId);
        }
        catch (Exception ex)
        {
            // Log but don't fail - cache invalidation failures should not break the transaction
            _logger.LogError(
                ex,
                "Error handling cache invalidation for event: {EventType}, TenantId: {TenantId}",
                domainEvent.GetEventType(),
                domainEvent.TenantId);
        }
    }

    /// <summary>
    /// Invalidates product and related category caches.
    /// </summary>
    private async Task InvalidateProductCacheAsync(Guid tenantId, Guid productId, Guid? categoryId, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate specific product
            await _cacheService.RemoveAsync(CacheKeys.Product(tenantId, productId), cancellationToken);

            // Invalidate product lists
            await _cacheService.RemoveAsync(CacheKeys.ProductList(tenantId), cancellationToken);

            // Invalidate status-based lists
            await _cacheService.RemoveByPatternAsync(CacheKeys.ProductsByStatus(tenantId, "*"), cancellationToken);

            // Invalidate search results
            await _cacheService.RemoveByPatternAsync($"{CacheKeys.Product(tenantId, Guid.Empty)}*search*", cancellationToken);

            // Invalidate category if product belongs to one
            if (categoryId.HasValue)
            {
                await InvalidateCategoryCacheAsync(tenantId, categoryId.Value, cancellationToken);
            }

            _logger.LogDebug("Product cache invalidated: ProductId: {ProductId}, CategoryId: {CategoryId}", productId, categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating product cache: {ProductId}", productId);
        }
    }

    /// <summary>
    /// Invalidates category and related caches.
    /// </summary>
    private async Task InvalidateCategoryCacheAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate specific category
            await _cacheService.RemoveAsync(CacheKeys.Category(tenantId, categoryId), cancellationToken);

            // Invalidate category list
            await _cacheService.RemoveAsync(CacheKeys.CategoryList(tenantId), cancellationToken);

            // Invalidate category hierarchy
            await _cacheService.RemoveAsync(CacheKeys.CategoryHierarchy(tenantId), cancellationToken);

            // Invalidate products in this category
            await _cacheService.RemoveAsync(CacheKeys.ProductsByCategory(tenantId, categoryId), cancellationToken);

            _logger.LogDebug("Category cache invalidated: CategoryId: {CategoryId}", categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating category cache: {CategoryId}", categoryId);
        }
    }

    /// <summary>
    /// Invalidates order and related customer cache.
    /// </summary>
    private async Task InvalidateOrderCacheAsync(Guid tenantId, Guid orderId, Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            // Invalidate specific order
            await _cacheService.RemoveAsync(CacheKeys.Order(tenantId, orderId), cancellationToken);

            // Invalidate order list
            await _cacheService.RemoveAsync(CacheKeys.OrderList(tenantId), cancellationToken);

            // Invalidate status-based lists
            await _cacheService.RemoveByPatternAsync(CacheKeys.OrdersByStatus(tenantId, "*"), cancellationToken);

            // Invalidate customer's order list
            await _cacheService.RemoveAsync(CacheKeys.CustomerOrdersList(tenantId, customerId), cancellationToken);

            _logger.LogDebug("Order cache invalidated: OrderId: {OrderId}, CustomerId: {CustomerId}", orderId, customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating order cache: {OrderId}", orderId);
        }
    }

    /// <summary>
    /// Invalidates configuration cache globally.
    /// Used when platform-wide configuration changes.
    /// </summary>
    public async Task InvalidateConfigurationCacheAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (tenantId.HasValue)
            {
                // Invalidate specific tenant's configuration
                await _cacheService.RemoveByPatternAsync(
                    CacheKeys.ConfigSection(tenantId.Value, "*"),
                    cancellationToken);
            }
            else
            {
                // Invalidate all platform configuration
                await _cacheService.RemoveByPatternAsync(CacheKeys.ConfigPattern(), cancellationToken);
            }

            _logger.LogInformation("Configuration cache invalidated for TenantId: {TenantId}", tenantId?.ToString() ?? "Platform-wide");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating configuration cache");
        }
    }

    /// <summary>
    /// Invalidates all customer-related caches.
    /// </summary>
    public async Task InvalidateCustomerCacheAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveAsync(CacheKeys.Customer(tenantId, customerId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.CustomerList(tenantId), cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.CustomerOrdersList(tenantId, customerId), cancellationToken);

            _logger.LogDebug("Customer cache invalidated: CustomerId: {CustomerId}", customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating customer cache: {CustomerId}", customerId);
        }
    }

    /// <summary>
    /// Invalidates entire tenant's cache.
    /// Used during tenant deletion or data reset.
    /// </summary>
    public async Task InvalidateTenantCacheAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cacheService.RemoveTenantCacheAsync(tenantId, cancellationToken);
            _logger.LogInformation("Entire tenant cache invalidated: {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tenant cache: {TenantId}", tenantId);
        }
    }
}
