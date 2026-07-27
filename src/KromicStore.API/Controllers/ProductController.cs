namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Products;
using KromicStore.Infrastructure.Services;

/// <summary>
/// Controller for managing products.
/// </summary>
[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
[Authorize]
public class ProductController : BaseController
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    /// <summary>
    /// Initializes a new instance of the ProductController class.
    /// </summary>
    public ProductController(
        ITenantProvider tenantProvider,
        IProductService productService,
        ILogger<ProductController> logger)
        : base(tenantProvider)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a paginated list of products for the current tenant.
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 20, max: 100).</param>
    /// <param name="status">Optional filter by product status (draft, published, archived).</param>
    /// <param name="categoryId">Optional filter by category ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Paginated list of products.</returns>
    /// <response code="200">List of products successfully retrieved.</response>
    /// <response code="400">Invalid pagination parameters.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
                return BadRequest(new { error = "Page number must be at least 1." });

            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { error = "Page size must be between 1 and 100." });

            _logger.LogInformation(
                "Getting products for tenant {TenantId}, page {PageNumber}, size {PageSize}, status filter: {Status}, category filter: {CategoryId}",
                CurrentTenantId, pageNumber, pageSize, status, categoryId);

            var result = await _productService.GetProductsAsync(
                CurrentTenantId,
                pageNumber,
                pageSize,
                status,
                categoryId,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving products." });
        }
    }

    /// <summary>
    /// Gets details for a specific product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Product details.</returns>
    /// <response code="200">Product details successfully retrieved.</response>
    /// <response code="404">Product not found.</response>
    /// <response code="401">User is not authenticated.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProductById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID must be a valid GUID." });

            _logger.LogInformation(
                "Getting product {ProductId} for tenant {TenantId}",
                id, CurrentTenantId);

            var product = await _productService.GetProductByIdAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (product == null)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found for tenant {TenantId}",
                    id, CurrentTenantId);
                return NotFound(new { error = "Product not found." });
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while retrieving the product." });
        }
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">The product creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created product.</returns>
    /// <response code="201">Product successfully created.</response>
    /// <response code="400">Invalid product data or duplicate SKU.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to create products.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Creating product with SKU {Sku} for tenant {TenantId}",
                request.Sku, CurrentTenantId);

            var result = await _productService.CreateProductAsync(
                CurrentTenantId,
                request,
                cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to create product: {Error}",
                    result.Error);
                return BadRequest(new { error = result.Error });
            }

            if (result.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Product creation returned null data." });
            }

            return CreatedAtAction(nameof(GetProductById), new { id = result.Data.Id }, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while creating the product." });
        }
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="request">The product update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated product.</returns>
    /// <response code="200">Product successfully updated.</response>
    /// <response code="400">Invalid product data.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to update products.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID must be a valid GUID." });

            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            _logger.LogInformation(
                "Updating product {ProductId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _productService.UpdateProductAsync(
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
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while updating the product." });
        }
    }

    /// <summary>
    /// Soft deletes a product (marks as archived).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Product successfully deleted.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to delete products.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID must be a valid GUID." });

            _logger.LogInformation(
                "Deleting product {ProductId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _productService.DeleteProductAsync(
                id,
                CurrentTenantId,
                cancellationToken);

            if (!result)
            {
                _logger.LogWarning(
                    "Product {ProductId} not found for deletion in tenant {TenantId}",
                    id, CurrentTenantId);
                return NotFound(new { error = "Product not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while deleting the product." });
        }
    }

    /// <summary>
    /// Publishes a product (makes it available for purchase).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The published product.</returns>
    /// <response code="200">Product successfully published.</response>
    /// <response code="400">Cannot publish product (e.g., zero stock).</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to publish products.</response>
    /// <response code="404">Product not found.</response>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishProduct(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID must be a valid GUID." });

            _logger.LogInformation(
                "Publishing product {ProductId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _productService.PublishProductAsync(
                id,
                CurrentTenantId,
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
            _logger.LogError(ex, "Error publishing product {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while publishing the product." });
        }
    }

    /// <summary>
    /// Unpublishes a product (makes it unavailable for purchase).
    /// </summary>
    /// <param name="id">The product ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unpublished product.</returns>
    /// <response code="200">Product successfully unpublished.</response>
    /// <response code="400">Cannot unpublish product.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized to unpublish products.</response>
    /// <response code="404">Product not found.</response>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize(Policy = Permissions.ProductsWrite)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnpublishProduct(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (id == Guid.Empty)
                return BadRequest(new { error = "Product ID must be a valid GUID." });

            _logger.LogInformation(
                "Unpublishing product {ProductId} for tenant {TenantId}",
                id, CurrentTenantId);

            var result = await _productService.UnpublishProductAsync(
                id,
                CurrentTenantId,
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
            _logger.LogError(ex, "Error unpublishing product {ProductId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An error occurred while unpublishing the product." });
        }
    }
}
