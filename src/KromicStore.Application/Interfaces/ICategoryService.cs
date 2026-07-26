namespace KromicStore.Application.Interfaces;

using KromicStore.Contracts.V1.Products;

/// <summary>
/// Interface for category management services.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets all categories for a tenant in tree structure.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Hierarchical list of categories with subcategories.</returns>
    Task<CategoryListResponse> GetAllCategoriesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific category by ID with its subcategories.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The category details with subcategories or null if not found.</returns>
    Task<CategoryResponse?> GetCategoryByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The category creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the created category or error message.</returns>
    Task<ServiceResult<CategoryResponse>> CreateCategoryAsync(
        Guid tenantId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The category update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the updated category or error message.</returns>
    Task<ServiceResult<CategoryResponse>> UpdateCategoryAsync(
        Guid id,
        Guid tenantId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category and unassigns all associated products.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if deleted successfully, false if not found.</returns>
    Task<bool> DeleteCategoryAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the display order of a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="request">The reorder request with new display order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Result containing the updated category or error message.</returns>
    Task<ServiceResult<CategoryResponse>> ReorderCategoryAsync(
        Guid id,
        Guid tenantId,
        ReorderCategoryRequest request,
        CancellationToken cancellationToken = default);
}
