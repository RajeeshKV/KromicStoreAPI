namespace KromicStore.Infrastructure.Services;

using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Products;
using KromicStore.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

/// <summary>
/// Service for managing product categories.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CategoryService> _logger;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the CategoryService class.
    /// </summary>
    public CategoryService(
        IUnitOfWork unitOfWork,
        ILogger<CategoryService> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Gets all categories for a tenant in tree structure.
    /// </summary>
    public async Task<CategoryListResponse> GetAllCategoriesAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all categories for tenant {TenantId}", tenantId);

            // Try cache first
            var cacheKey = $"categories:tree:{tenantId}";
            var cachedResponse = await _cacheService.GetAsync<CategoryListResponse>(cacheKey, cancellationToken);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Cache hit for category tree for tenant {TenantId}", tenantId);
                return cachedResponse;
            }

            // Get all categories for this tenant
            var allCategories = await _unitOfWork.Categories.FindAsync(
                c => c.TenantId == tenantId,
                cancellationToken);

            // Build tree structure
            var topLevelCategories = allCategories
                .Where(c => !c.ParentCategoryId.HasValue)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            var categoryResponses = new List<CategoryResponse>();
            foreach (var category in topLevelCategories)
            {
                var response = await BuildCategoryResponseAsync(category, allCategories, tenantId, cancellationToken);
                categoryResponses.Add(response);
            }

            var result = new CategoryListResponse
            {
                Data = categoryResponses,
                TotalCount = topLevelCategories.Count
            };

            // Cache for 1 hour
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromHours(1), cancellationToken);

            _logger.LogInformation(
                "Successfully fetched {Count} top-level categories for tenant {TenantId}",
                categoryResponses.Count, tenantId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all categories for tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific category by ID with its subcategories.
    /// </summary>
    public async Task<CategoryResponse?> GetCategoryByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try cache first
            var cacheKey = $"category:{tenantId}:{id}";
            var cachedCategory = await _cacheService.GetAsync<CategoryResponse>(cacheKey, cancellationToken);
            if (cachedCategory != null)
            {
                _logger.LogInformation("Cache hit for category {CategoryId}", id);
                return cachedCategory;
            }

            _logger.LogInformation(
                "Fetching category {CategoryId} for tenant {TenantId}",
                id, tenantId);

            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

            if (category == null || category.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Category {CategoryId} not found for tenant {TenantId}",
                    id, tenantId);
                return null;
            }

            // Get all categories for tree building
            var allCategories = await _unitOfWork.Categories.FindAsync(
                c => c.TenantId == tenantId,
                cancellationToken);

            var response = await BuildCategoryResponseAsync(category, allCategories, tenantId, cancellationToken);

            // Cache for 1 hour
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(1), cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category {CategoryId}", id);
            throw;
        }
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    public async Task<ServiceResult<CategoryResponse>> CreateCategoryAsync(
        Guid tenantId,
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return ServiceResult<CategoryResponse>.FailureResult("Request cannot be null.");

            _logger.LogInformation(
                "Creating category {Name} for tenant {TenantId}",
                request.Name, tenantId);

            // Validate parent category exists (if specified)
            if (request.ParentCategoryId.HasValue && request.ParentCategoryId.Value != Guid.Empty)
            {
                var parentCategory = await _unitOfWork.Categories.GetByIdAsync(
                    request.ParentCategoryId.Value,
                    cancellationToken);

                if (parentCategory == null || parentCategory.TenantId != tenantId)
                {
                    _logger.LogWarning(
                        "Parent category {ParentCategoryId} not found for tenant {TenantId}",
                        request.ParentCategoryId, tenantId);
                    return ServiceResult<CategoryResponse>.FailureResult("Parent category not found.");
                }

                // Get all categories to validate nesting
                var allCategories = await _unitOfWork.Categories.FindAsync(
                    c => c.TenantId == tenantId,
                    cancellationToken);

                // Check nesting level (max 3 levels: 0, 1, 2)
                if (parentCategory.NestingLevel >= 2)
                {
                    _logger.LogWarning(
                        "Cannot create category under parent {ParentCategoryId}: exceeds 3-level nesting limit",
                        request.ParentCategoryId);
                    return ServiceResult<CategoryResponse>.FailureResult(
                        "Cannot exceed 3 levels of category nesting.");
                }
            }

            // Create the category using domain entity factory
            var category = Category.Create(
                tenantId,
                request.Name,
                request.Description,
                request.ParentCategoryId != Guid.Empty ? request.ParentCategoryId : null,
                request.DisplayOrder);

            // If there's a parent, validate and set it
            if (category.ParentCategoryId.HasValue)
            {
                var allCategories = await _unitOfWork.Categories.FindAsync(
                    c => c.TenantId == tenantId,
                    cancellationToken);

                try
                {
                    category.SetParentCategory(category.ParentCategoryId, allCategories);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(ex, "Invalid parent category for tenant {TenantId}", tenantId);
                    return ServiceResult<CategoryResponse>.FailureResult(ex.Message);
                }
            }

            await _unitOfWork.Categories.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Category {CategoryId} created successfully for tenant {TenantId}",
                category.Id, tenantId);

            // Invalidate category tree cache
            await InvalidateCategoryCache(tenantId, cancellationToken);

            var response = await MapToCategoryResponseAsync(category, new List<Category>(), tenantId, cancellationToken);
            return ServiceResult<CategoryResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category for tenant {TenantId}", tenantId);
            return ServiceResult<CategoryResponse>.FailureResult("An error occurred while creating the category.");
        }
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    public async Task<ServiceResult<CategoryResponse>> UpdateCategoryAsync(
        Guid id,
        Guid tenantId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return ServiceResult<CategoryResponse>.FailureResult("Request cannot be null.");

            _logger.LogInformation(
                "Updating category {CategoryId} for tenant {TenantId}",
                id, tenantId);

            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

            if (category == null || category.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Category {CategoryId} not found for update in tenant {TenantId}",
                    id, tenantId);
                return ServiceResult<CategoryResponse>.FailureResult("Category not found.");
            }

            // Validate parent category if changing it
            if (request.ParentCategoryId != category.ParentCategoryId)
            {
                if (request.ParentCategoryId.HasValue && request.ParentCategoryId.Value != Guid.Empty)
                {
                    var parentCategory = await _unitOfWork.Categories.GetByIdAsync(
                        request.ParentCategoryId.Value,
                        cancellationToken);

                    if (parentCategory == null || parentCategory.TenantId != tenantId)
                    {
                        _logger.LogWarning(
                            "Parent category {ParentCategoryId} not found for tenant {TenantId}",
                            request.ParentCategoryId, tenantId);
                        return ServiceResult<CategoryResponse>.FailureResult("Parent category not found.");
                    }

                    // Get all categories to validate nesting and circular references
                    var allCategories = await _unitOfWork.Categories.FindAsync(
                        c => c.TenantId == tenantId,
                        cancellationToken);

                    try
                    {
                        category.SetParentCategory(request.ParentCategoryId, allCategories);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex, "Invalid parent category change for category {CategoryId}", id);
                        return ServiceResult<CategoryResponse>.FailureResult(ex.Message);
                    }
                }
                else
                {
                    // Setting parent to null
                    category.SetParentCategory(null, new List<Category>());
                }
            }

            // Update basic information
            category.Update(request.Name, request.Description, request.DisplayOrder);

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Category {CategoryId} updated successfully for tenant {TenantId}",
                id, tenantId);

            // Invalidate caches
            await InvalidateCategoryCache(tenantId, cancellationToken);
            await _cacheService.RemoveAsync($"category:{tenantId}:{id}", cancellationToken);

            var allCats = await _unitOfWork.Categories.FindAsync(
                c => c.TenantId == tenantId,
                cancellationToken);
            var response = await MapToCategoryResponseAsync(category, allCats, tenantId, cancellationToken);
            return ServiceResult<CategoryResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return ServiceResult<CategoryResponse>.FailureResult("An error occurred while updating the category.");
        }
    }

    /// <summary>
    /// Deletes a category and unassigns all associated products.
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Deleting category {CategoryId} for tenant {TenantId}",
                id, tenantId);

            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

            if (category == null || category.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Category {CategoryId} not found for deletion in tenant {TenantId}",
                    id, tenantId);
                return false;
            }

            // Unassign all products from this category
            var productsInCategory = await _unitOfWork.Products.FindAsync(
                p => p.TenantId == tenantId && p.CategoryId == id,
                cancellationToken);

            foreach (var product in productsInCategory)
            {
                product.UnassignCategory();
                _unitOfWork.Products.Update(product);
            }

            // If this category has children, move them to root level
            var allCategories = await _unitOfWork.Categories.FindAsync(
                c => c.TenantId == tenantId && c.ParentCategoryId == id,
                cancellationToken);

            foreach (var childCategory in allCategories)
            {
                childCategory.SetParentCategory(null, new List<Category>());
                _unitOfWork.Categories.Update(childCategory);
            }

            // Delete the category
            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Category {CategoryId} deleted successfully for tenant {TenantId}. Unassigned {ProductCount} products.",
                id, tenantId, productsInCategory.Count);

            // Invalidate caches
            await InvalidateCategoryCache(tenantId, cancellationToken);
            await _cacheService.RemoveAsync($"category:{tenantId}:{id}", cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            throw;
        }
    }

    /// <summary>
    /// Updates the display order of a category.
    /// </summary>
    public async Task<ServiceResult<CategoryResponse>> ReorderCategoryAsync(
        Guid id,
        Guid tenantId,
        ReorderCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Reordering category {CategoryId} to display order {DisplayOrder} for tenant {TenantId}",
                id, request.DisplayOrder, tenantId);

            var category = await _unitOfWork.Categories.GetByIdAsync(id, cancellationToken);

            if (category == null || category.TenantId != tenantId)
            {
                _logger.LogWarning(
                    "Category {CategoryId} not found for reordering in tenant {TenantId}",
                    id, tenantId);
                return ServiceResult<CategoryResponse>.FailureResult("Category not found.");
            }

            category.Update(category.Name, category.Description, request.DisplayOrder);

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Category {CategoryId} reordered successfully to {DisplayOrder} for tenant {TenantId}",
                id, request.DisplayOrder, tenantId);

            // Invalidate caches
            await InvalidateCategoryCache(tenantId, cancellationToken);
            await _cacheService.RemoveAsync($"category:{tenantId}:{id}", cancellationToken);

            var allCategories = await _unitOfWork.Categories.FindAsync(
                c => c.TenantId == tenantId,
                cancellationToken);
            var response = await MapToCategoryResponseAsync(category, allCategories, tenantId, cancellationToken);
            return ServiceResult<CategoryResponse>.SuccessResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering category {CategoryId}", id);
            return ServiceResult<CategoryResponse>.FailureResult("An error occurred while reordering the category.");
        }
    }

    /// <summary>
    /// Builds a category response with subcategories recursively.
    /// </summary>
    private async Task<CategoryResponse> BuildCategoryResponseAsync(
        Category category,
        IEnumerable<Category> allCategories,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var categoryList = allCategories.ToList();
        var subcategories = categoryList
            .Where(c => c.ParentCategoryId == category.Id)
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        var productCount = (await _unitOfWork.Products.FindAsync(
            p => p.TenantId == tenantId && p.CategoryId == category.Id,
            cancellationToken)).Count;

        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            DisplayOrder = category.DisplayOrder,
            NestingLevel = category.NestingLevel,
            SubcategoryCount = subcategories.Count,
            ProductCount = productCount,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        // Recursively build subcategories
        foreach (var subcategory in subcategories)
        {
            var subResponse = await BuildCategoryResponseAsync(
                subcategory, categoryList, tenantId, cancellationToken);
            response.Subcategories.Add(subResponse);
        }

        return response;
    }

    /// <summary>
    /// Maps a category entity to a response DTO.
    /// </summary>
    private async Task<CategoryResponse> MapToCategoryResponseAsync(
        Category category,
        IEnumerable<Category> allCategories,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var categoryList = allCategories.Any() ? allCategories.ToList() : 
            (await _unitOfWork.Categories.FindAsync(
                c => c.TenantId == tenantId,
                cancellationToken)).ToList();

        return await BuildCategoryResponseAsync(category, categoryList, tenantId, cancellationToken);
    }

    /// <summary>
    /// Invalidates category-related caches.
    /// </summary>
    private async Task InvalidateCategoryCache(Guid tenantId, CancellationToken cancellationToken)
    {
        await _cacheService.RemoveAsync($"categories:tree:{tenantId}", cancellationToken);
    }
}
