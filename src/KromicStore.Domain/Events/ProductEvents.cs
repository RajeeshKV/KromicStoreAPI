namespace KromicStore.Domain.Events;

/// <summary>
/// Domain event published when a product is created.
/// </summary>
public class ProductCreatedEvent : DomainEvent
{
    /// <summary>Gets the product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets the product SKU.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets the category ID (if assigned).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Initializes a new instance of ProductCreatedEvent.
    /// </summary>
    public ProductCreatedEvent(Guid tenantId, Guid productId, string productName, string sku, Guid? categoryId = null)
    {
        TenantId = tenantId;
        EntityId = productId;
        ProductName = productName;
        Sku = sku;
        CategoryId = categoryId;
    }
}

/// <summary>
/// Domain event published when a product is updated.
/// </summary>
public class ProductUpdatedEvent : DomainEvent
{
    /// <summary>Gets the product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets the category ID (if changed).</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Gets the previous category ID.</summary>
    public Guid? PreviousCategoryId { get; set; }

    /// <summary>
    /// Initializes a new instance of ProductUpdatedEvent.
    /// </summary>
    public ProductUpdatedEvent(Guid tenantId, Guid productId, string productName, Guid? categoryId, Guid? previousCategoryId)
    {
        TenantId = tenantId;
        EntityId = productId;
        ProductName = productName;
        CategoryId = categoryId;
        PreviousCategoryId = previousCategoryId;
    }
}

/// <summary>
/// Domain event published when a product is published (status changes to Active).
/// </summary>
public class ProductPublishedEvent : DomainEvent
{
    /// <summary>Gets the product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets the category ID.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Initializes a new instance of ProductPublishedEvent.
    /// </summary>
    public ProductPublishedEvent(Guid tenantId, Guid productId, string productName, Guid? categoryId)
    {
        TenantId = tenantId;
        EntityId = productId;
        ProductName = productName;
        CategoryId = categoryId;
    }
}

/// <summary>
/// Domain event published when a product is unpublished or archived.
/// </summary>
public class ProductUnpublishedEvent : DomainEvent
{
    /// <summary>Gets the product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets the category ID.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Initializes a new instance of ProductUnpublishedEvent.
    /// </summary>
    public ProductUnpublishedEvent(Guid tenantId, Guid productId, string productName, Guid? categoryId)
    {
        TenantId = tenantId;
        EntityId = productId;
        ProductName = productName;
        CategoryId = categoryId;
    }
}

/// <summary>
/// Domain event published when a product is deleted.
/// </summary>
public class ProductDeletedEvent : DomainEvent
{
    /// <summary>Gets the product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets the category ID.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Initializes a new instance of ProductDeletedEvent.
    /// </summary>
    public ProductDeletedEvent(Guid tenantId, Guid productId, string productName, Guid? categoryId)
    {
        TenantId = tenantId;
        EntityId = productId;
        ProductName = productName;
        CategoryId = categoryId;
    }
}
