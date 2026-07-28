namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Products;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;
using KromicStore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

/// <summary>
/// Service for managing products.
/// </summary>
public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProductService> _logger;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the ProductService class.
    /// </summary>
    public ProductService(
        IUnitOfWork unitOfWork,
        ILogger<ProductService> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Gets a paginated list of products for a tenant.
    /// </summary>
    public async Task<ProductListResponse> GetProductsAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        string? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Fetching products for tenant {TenantId}, page {PageNumber}, size {PageSize}",
                tenantId, pageNumber, pageSize);

            // Enforce max page size
            if (pageSize > 100)
                pageSize = 100;

            // Build predicate for filtering
            Expression<Func<Product, bool>> predicate = p => p.TenantId == tenantId;

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusUpper = status.ToUpperInvariant();
                predicate = statusUpper switch
                {
                    "DRAFT" => p => p.TenantId == tenantId && p.Status == ProductStatus.Inactive,
                    "PUBLISHED" => p => p.TenantId == tenantId && p.Status == ProductStatus.Active,
                    "ARCHIVED" => p => p.TenantId == tenantId && p.Status == ProductStatus.Archived,
                    _ => predicate
                };
            }

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                var currentPredicate = predicate;
                predicate = p => currentPredicate.Compile()(p) && p.CategoryId == categoryId.Value;
            }

            // Get all products matching the filter
            var allProducts = await _unitOfWork.Products.FindAsync(predicate, cancellationToken);

            // Apply pagination
            var totalCount = allProducts.Count;
            var skip = (pageNumber - 1) * pageSize;
            var products = allProducts
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(MapToProductDto)
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (decimal)pageSize);

            var response = new ProductListResponse
            {
                Data = products,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };

            _logger.LogInformation(
                "Successfully fetched {Count} products for tenant {TenantId}",
                products.Count, tenantId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific product by ID.
    /// </summary>
    public async Task<ProductDto?> GetProductByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            var cacheKey = $"product:{tenantId}:{id}";
            var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
            if (cachedProduct != null)
            {
                _logger.LogInformation("Cache hit for product {ProductId}", id);
                return cachedProduct;
            }

            _logger.LogInformation(
                "Fetching product {ProductId} for tenant {TenantId}",
                id, tenantId);

            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);

            if (product == null || product.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found for tenant {TenantId}",
                    id, tenantId);
                return null;
            }

            var dto = MapToProductDto(product);

            // Cache the product for 1 hour
            await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromHours(1), cancellationToken);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", id);
            throw;
        }
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    public async Task<ServiceResult<ProductDto>> CreateProductAsync(
        Guid tenantId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return ServiceResult<ProductDto>.FailureResult("Request cannot be null.");

            _logger.LogInformation(
                "Creating product with SKU {Sku} for tenant {TenantId}",
                request.Sku, tenantId);

            // Check for duplicate SKU within tenant
            var existingProduct = await _unitOfWork.Products.FindAsync(
                p => p.TenantId == tenantId && p.Sku == request.Sku, 
                cancellationToken);

            if (existingProduct.Any())
            {
                _logger.LogWarning(
                    "Product with SKU {Sku} already exists for tenant {TenantId}",
                    request.Sku, tenantId);
                return ServiceResult<ProductDto>.FailureResult($"A product with SKU '{request.Sku}' already exists.");
            }

            // Create the product using domain entity factory
            var price = new Money(request.Price, "USD");
            var product = Product.Create(
                tenantId,
                request.Sku,
                request.Name,
                request.Description,
                price,
                request.StockQuantity,
                request.CategoryId);

            // Add product images if provided
            if (request.Images != null && request.Images.Any())
            {
                foreach (var imageRequest in request.Images)
                {
                    var productImage = ProductImage.Create(
                        product.Id,
                        imageRequest.Url,
                        imageRequest.CloudinaryPublicId,
                        imageRequest.DisplayOrder,
                        imageRequest.IsPrimary,
                        imageRequest.AltText);
                    product.Images.Add(productImage);
                }

                // Set legacy ImageUrl to primary image for backward compatibility
                var primaryImage = request.Images.FirstOrDefault(i => i.IsPrimary) ?? request.Images.FirstOrDefault();
                if (primaryImage != null)
                {
                    // Use reflection to set the private ImageUrl field
                    var imageUrlProperty = typeof(Product).GetProperty("ImageUrl");
                    if (imageUrlProperty != null)
                    {
                        imageUrlProperty.SetValue(product, primaryImage.Url);
                    }
                }
            }

            await _unitOfWork.Products.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product {ProductId} created successfully with SKU {Sku} for tenant {TenantId}",
                product.Id, product.Sku, tenantId);

            var dto = MapToProductDto(product);
            return ServiceResult<ProductDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product for tenant {TenantId}", tenantId);
            return ServiceResult<ProductDto>.FailureResult("An error occurred while creating the product.");
        }
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    public async Task<ServiceResult<ProductDto>> UpdateProductAsync(
        Guid id,
        Guid tenantId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return ServiceResult<ProductDto>.FailureResult("Request cannot be null.");

            _logger.LogInformation(
                "Updating product {ProductId} for tenant {TenantId}",
                id, tenantId);

            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);

            if (product == null || product.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found for update in tenant {TenantId}",
                    id, tenantId);
                return ServiceResult<ProductDto>.FailureResult("Product not found.");
            }

            // Update using domain entity method
            var price = new Money(request.Price, "USD");
            product.Update(
                request.Name,
                request.Description,
                price,
                request.CategoryId);

            // Handle product images if provided
            if (request.Images != null)
            {
                // Remove all existing images
                var existingImages = await _unitOfWork.ProductImages.FindAsync(
                    pi => pi.ProductId == id, cancellationToken);
                foreach (var existingImage in existingImages)
                {
                    _unitOfWork.ProductImages.Delete(existingImage);
                }

                // Add new images
                foreach (var imageRequest in request.Images)
                {
                    var productImage = ProductImage.Create(
                        id,
                        imageRequest.Url,
                        imageRequest.CloudinaryPublicId,
                        imageRequest.DisplayOrder,
                        imageRequest.IsPrimary,
                        imageRequest.AltText);
                    product.Images.Add(productImage);
                }

                // Update legacy ImageUrl to primary image for backward compatibility
                var primaryImage = request.Images.FirstOrDefault(i => i.IsPrimary) ?? request.Images.FirstOrDefault();
                if (primaryImage != null)
                {
                    var imageUrlProperty = typeof(Product).GetProperty("ImageUrl");
                    if (imageUrlProperty != null)
                    {
                        imageUrlProperty.SetValue(product, primaryImage.Url);
                    }
                }
            }

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product {ProductId} updated successfully for tenant {TenantId}",
                id, tenantId);

            // Invalidate cache
            var cacheKey = $"product:{tenantId}:{id}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            var dto = MapToProductDto(product);
            return ServiceResult<ProductDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return ServiceResult<ProductDto>.FailureResult("An error occurred while updating the product.");
        }
    }

    /// <summary>
    /// Soft deletes a product (marks as archived).
    /// </summary>
    public async Task<bool> DeleteProductAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Soft deleting product {ProductId} for tenant {TenantId}",
                id, tenantId);

            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);

            if (product == null || product.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found for deletion in tenant {TenantId}",
                    id, tenantId);
                return false;
            }

            // Soft delete by archiving
            product.Archive();

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product {ProductId} soft deleted (archived) for tenant {TenantId}",
                id, tenantId);

            // Invalidate cache
            var cacheKey = $"product:{tenantId}:{id}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            throw;
        }
    }

    /// <summary>
    /// Publishes a product (makes it available for purchase).
    /// </summary>
    public async Task<ServiceResult<ProductDto>> PublishProductAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Publishing product {ProductId} for tenant {TenantId}",
                id, tenantId);

            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);

            if (product == null || product.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found for publishing in tenant {TenantId}",
                    id, tenantId);
                return ServiceResult<ProductDto>.FailureResult("Product not found.");
            }

            // Validate stock before publishing
            if (product.StockQuantity <= 0)
            {
                _logger.LogWarning(
                    "Cannot publish product {ProductId} with zero or negative stock for tenant {TenantId}",
                    id, tenantId);
                return ServiceResult<ProductDto>.FailureResult(
                    "Cannot publish product with zero or negative stock quantity.");
            }

            // Publish using domain entity method
            product.Publish();

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product {ProductId} published successfully for tenant {TenantId}",
                id, tenantId);

            // Invalidate cache
            var cacheKey = $"product:{tenantId}:{id}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            var dto = MapToProductDto(product);
            return ServiceResult<ProductDto>.SuccessResult(dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while publishing product {ProductId}", id);
            return ServiceResult<ProductDto>.FailureResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing product {ProductId}", id);
            return ServiceResult<ProductDto>.FailureResult("An error occurred while publishing the product.");
        }
    }

    /// <summary>
    /// Unpublishes a product (makes it unavailable for purchase).
    /// </summary>
    public async Task<ServiceResult<ProductDto>> UnpublishProductAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Unpublishing product {ProductId} for tenant {TenantId}",
                id, tenantId);

            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);

            if (product == null || product.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found for unpublishing in tenant {TenantId}",
                    id, tenantId);
                return ServiceResult<ProductDto>.FailureResult("Product not found.");
            }

            // Unpublish using domain entity method
            product.Unpublish();

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product {ProductId} unpublished successfully for tenant {TenantId}",
                id, tenantId);

            // Invalidate cache
            var cacheKey = $"product:{tenantId}:{id}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            var dto = MapToProductDto(product);
            return ServiceResult<ProductDto>.SuccessResult(dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while unpublishing product {ProductId}", id);
            return ServiceResult<ProductDto>.FailureResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing product {ProductId}", id);
            return ServiceResult<ProductDto>.FailureResult("An error occurred while unpublishing the product.");
        }
    }

    /// <summary>
    /// Maps a Product entity to a ProductDto.
    /// </summary>
    private static ProductDto MapToProductDto(Product product)
    {
        var images = product.Images?.Select(img => new ProductImageDto(
            Id: img.Id,
            Url: img.Url,
            CloudinaryPublicId: img.CloudinaryPublicId,
            DisplayOrder: img.DisplayOrder,
            IsPrimary: img.IsPrimary,
            AltText: img.AltText
        )).ToList() ?? new List<ProductImageDto>();

        return new ProductDto(
            Id: product.Id,
            Sku: product.Sku,
            Name: product.Name,
            Description: product.Description,
            Price: product.Price.Amount,
            CostPrice: product.CostPrice?.Amount,
            StockQuantity: product.StockQuantity,
            Status: product.Status.ToString(),
            CategoryId: product.CategoryId,
            ImageUrl: product.ImageUrl,
            Images: images,
            Weight: product.Weight,
            TaxPercentage: product.TaxPercentage,
            CreatedAt: product.CreatedAt,
            UpdatedAt: product.UpdatedAt);
    }
}
