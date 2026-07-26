namespace KromicStore.Application.Interfaces;

using KromicStore.Contracts.V1.Products;

/// <summary>
/// Interface for product management services.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Gets a paginated list of products for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="status">Optional status filter (draft, published, archived).</param>
    /// <param name="categoryId">Optional category ID filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated product list.</returns>
    Task<ProductListResponse> GetProductsAsync(
        Guid tenantId,
        int pageNumber,
        int pageSize,
        string? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific product by ID.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The product details or null if not found.</returns>
    Task<ProductDto?> GetProductByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The product creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the created product or error message.</returns>
    Task<ServiceResult<ProductDto>> CreateProductAsync(
        Guid tenantId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The product update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the updated product or error message.</returns>
    Task<ServiceResult<ProductDto>> UpdateProductAsync(
        Guid id,
        Guid tenantId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a product (marks as archived).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if deleted successfully, false if not found.</returns>
    Task<bool> DeleteProductAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a product (makes it available for purchase).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the published product or error message.</returns>
    Task<ServiceResult<ProductDto>> PublishProductAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublishes a product (makes it unavailable for purchase).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the unpublished product or error message.</returns>
    Task<ServiceResult<ProductDto>> UnpublishProductAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic service result wrapper for operation outcomes.
/// </summary>
public class ServiceResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets the result data if the operation was successful.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ServiceResult<T> SuccessResult(T data) => new() { Success = true, Data = data };

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static ServiceResult<T> FailureResult(string error) => new() { Success = false, Error = error };
}
