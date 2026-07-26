namespace KromicStore.Infrastructure.Services.StorefrontServices;

using Domain.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for populating default/placeholder data for new storefronts.
/// Creates placeholder categories, products, and collections during storefront initialization.
/// </summary>
public class DefaultDataPopulator
{
    private readonly ILogger<DefaultDataPopulator> _logger;

    /// <summary>
    /// Initializes a new instance of the DefaultDataPopulator class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DefaultDataPopulator(ILogger<DefaultDataPopulator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Populates default data for a storefront including placeholder categories, products, and collections.
    /// Updates mandatory fields status to mark defaults as non-placeholders.
    /// </summary>
    /// <param name="storefront">The storefront to populate.</param>
    /// <param name="tenantId">The tenant ID for the storefront.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="ArgumentNullException">Thrown when storefront is null.</exception>
    public Task PopulateDefaultDataAsync(
        Storefront storefront,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (storefront == null)
            throw new ArgumentNullException(nameof(storefront));

        _logger.LogInformation("Populating default data for storefront {StorefrontId}", storefront.Id);

        try
        {
            // Populate storefront with default mandatory field values
            PopulateMandatoryFields(storefront);

            _logger.LogInformation("Default data population completed for storefront {StorefrontId}", storefront.Id);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating default data for storefront {StorefrontId}", storefront.Id);
            throw;
        }
    }

    /// <summary>
    /// Populates default values for mandatory storefront fields.
    /// Sets default placeholder URLs, email, phone, address, currency, country, and copyright.
    /// </summary>
    private void PopulateMandatoryFields(Storefront storefront)
    {
        // Update storefront with default values
        storefront.UpdateInfo(
            name: storefront.Name,
            logoUrl: "https://via.placeholder.com/200x60?text=Logo",
            contactEmail: "support@store.com",
            contactPhone: "+1 (555) 000-0000",
            address: "123 Main Street, City, State 12345",
            currency: storefront.Currency, // Use existing or default
            country: "US",
            brandColor: "#000000",
            copyright: $"© {DateTime.UtcNow.Year} {storefront.Name}. All rights reserved.");

        // Mark all mandatory fields as provided (not placeholders)
        storefront.UpdateMandatoryFieldsStatus(
            storeNameProvided: true,
            logoProvided: true,
            emailProvided: true,
            phoneProvided: true,
            addressProvided: true,
            currencyProvided: true,
            countryProvided: true,
            brandColorProvided: true,
            copyrightProvided: true);

        _logger.LogInformation("Mandatory fields populated for storefront {StorefrontId}", storefront.Id);
    }

    /// <summary>
    /// Creates placeholder categories for a new storefront.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>A list of placeholder categories.</returns>
    public List<Category> CreatePlaceholderCategories(Guid tenantId)
    {
        _logger.LogInformation("Creating placeholder categories for tenant {TenantId}", tenantId);

        var categories = new List<Category>
        {
            Category.Create(
                tenantId,
                "Electronics",
                "Electronic devices and accessories",
                displayOrder: 0),
            
            Category.Create(
                tenantId,
                "Clothing",
                "Apparel and fashion items",
                displayOrder: 1),
            
            Category.Create(
                tenantId,
                "Home & Garden",
                "Home and garden products",
                displayOrder: 2),
            
            Category.Create(
                tenantId,
                "Sports",
                "Sports and outdoor equipment",
                displayOrder: 3)
        };

        return categories;
    }

    /// <summary>
    /// Creates placeholder products for a new storefront.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>A list of placeholder products.</returns>
    public List<Product> CreatePlaceholderProducts(Guid tenantId)
    {
        _logger.LogInformation("Creating placeholder products for tenant {TenantId}", tenantId);

        var products = new List<Product>
        {
            Product.Create(
                tenantId,
                "PROD-001",
                "Premium Wireless Headphones",
                "High-quality wireless headphones with noise cancellation. Perfect for music lovers and professionals.",
                price: 9999,
                stockQuantity: 50,
                categoryId: null),
            
            Product.Create(
                tenantId,
                "PROD-002",
                "Organic Cotton T-Shirt",
                "Comfortable and eco-friendly organic cotton t-shirt available in multiple sizes and colors.",
                price: 1299,
                stockQuantity: 100,
                categoryId: null),
            
            Product.Create(
                tenantId,
                "PROD-003",
                "Stainless Steel Water Bottle",
                "Durable and eco-friendly water bottle that keeps drinks hot or cold for hours.",
                price: 1599,
                stockQuantity: 75,
                categoryId: null),
            
            Product.Create(
                tenantId,
                "PROD-004",
                "Portable USB-C Charger",
                "Fast-charging portable charger with multiple USB ports. Essential for travelers.",
                price: 2499,
                stockQuantity: 60,
                categoryId: null),
            
            Product.Create(
                tenantId,
                "PROD-005",
                "Yoga Mat with Carrying Strap",
                "Premium yoga mat with non-slip surface and convenient carrying strap.",
                price: 1899,
                stockQuantity: 40,
                categoryId: null),
            
            Product.Create(
                tenantId,
                "PROD-006",
                "Digital Kitchen Scale",
                "Accurate digital kitchen scale perfect for cooking and baking enthusiasts.",
                price: 999,
                stockQuantity: 35,
                categoryId: null),
            
            Product.Create(
                tenantId,
                "PROD-007",
                "Smart LED Desk Lamp",
                "Adjustable LED desk lamp with smart controls and energy-efficient technology.",
                price: 2199,
                stockQuantity: 45,
                categoryId: null),
            
            Product.Create(
                tenantId,
                "PROD-008",
                "Canvas Tote Bag",
                "Spacious and stylish canvas tote bag perfect for shopping, travel, or everyday use.",
                price: 799,
                stockQuantity: 80,
                categoryId: null)
        };

        return products;
    }

    /// <summary>
    /// Creates placeholder collections for a storefront.
    /// </summary>
    /// <returns>Collection names and descriptions.</returns>
    public Dictionary<string, string> GetPlaceholderCollections()
    {
        return new Dictionary<string, string>
        {
            { "New Arrivals", "Check out our latest and greatest products" },
            { "Best Sellers", "Our most popular items loved by customers" },
            { "On Sale", "Fantastic deals and discounts on selected items" },
            { "Coming Soon", "Exciting new products arriving soon" }
        };
    }
}
