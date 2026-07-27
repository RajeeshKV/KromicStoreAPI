namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Products;

/// <summary>
/// Controller for managing product categories.
/// </summary>
[ApiController]
[Route("api/v1/categories")]
[Produces("application/json")]
[Authorize]
public class CategoryController : BaseController
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    /// <summary>
    /// Initializes a new instance of the CategoryController class.
    /// </summary>
    public CategoryController(
        ITenantProvider tenantProvider,
        ICategoryService categoryService,
        ILogger<CategoryController> logger)
        : base(tenantProvider)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all categories for the current tenant in hierarchical tree structure.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Hierarchical list of categories with subcategories.</returns>
    /// <response code="200">Categories successfully retrieved.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategories(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Getting category hierarchy for tenant {TenantId}",
                CurrentTenantId);

            var result = await _categoryService.GetAllCategoriesAsync(
                CurrentTenantId,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving categories." });
        }
    }

    /// <summary>
    /// Gets details for a specific category including its subcategories and product count.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Category details with subcategories.</returns>
    /// <response code="200">Category details successfully retrieved.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategoryById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Category ID must be a valid GUID." });

            _logger.LogInformation(
                "Getting category {CategoryId} for tenant {TenantId}",
                id, CurrentTenantId);

            var category = await _categoryService.GetCategoryByIdAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (category == null)
            {
                _logger.LogWarning(
                    "Category {CategoryId} not found for tenant {TenantId}",
                    id, CurrentTenantId);
                return NotFound(new { error = "Category not found." });
            }

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the category." });
        }
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="request">The category creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created category.</returns>
    /// <response code="201">Category successfully created.</response>
    /// <response code="400">Invalid category data (e.g., exceeds nesting limit, invalid parent).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to create categories.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Creating category {Name} for tenant {TenantId}",
                request.Name, CurrentTenantId);

            var result = await _categoryService.CreateCategoryAsync(
                CurrentTenantId,
                request,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to create category: {Error}",
                    result.Error);
                return BadRequest(new { error = result.Error });
            }

            if (result.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Category creation returned null data." });
            }

            return CreatedAtAction(nameof(GetCategoryById), new { id = result.Data.Id }, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the category." });
        }
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="request">The category update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Category successfully updated.</response>
    /// <response code="400">Invalid category data (e.g., circular reference, exceeds nesting limit).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to update categories.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Category ID must be a valid GUID." });

            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Updating category {CategoryId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _categoryService.UpdateCategoryAsync(
                id,
                CurrentTenantId,
                request,
                cancellationToken);

            if (!result.Success)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the category." });
        }
    }

    /// <summary>
    /// Deletes a category and unassigns all associated products to null (root category).
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Category successfully deleted.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to delete categories.</response>
    /// <response code="404">Category not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Category ID must be a valid GUID." });

            _logger.LogInformation(
                "Deleting category {CategoryId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _categoryService.DeleteCategoryAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (!result)
            {
                _logger.LogWarning(
                    "Category {CategoryId} not found for deletion in tenant {TenantId}",
                    id, CurrentTenantId);
                return NotFound(new { error = "Category not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the category." });
        }
    }

    /// <summary>
    /// Updates the display order of a category for UI sorting.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="request">The reorder request with new display order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Category successfully reordered.</response>
    /// <response code="400">Invalid display order.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to reorder categories.</response>
    /// <response code="404">Category not found.</response>
    [HttpPost("{id:guid}/reorder")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderCategory(
        [FromRoute] Guid id,
        [FromBody] ReorderCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Category ID must be a valid GUID." });

            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (request.DisplayOrder < 0)
                return BadRequest(new { error = "Display order must be non-negative." });

            _logger.LogInformation(
                "Reordering category {CategoryId} to display order {DisplayOrder} for tenant {TenantId}",
                id, request.DisplayOrder, CurrentTenantId);

            var result = await _categoryService.ReorderCategoryAsync(
                id,
                CurrentTenantId,
                request,
                cancellationToken);

            if (!result.Success)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return NotFound(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering category {CategoryId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while reordering the category." });
        }
    }
}
