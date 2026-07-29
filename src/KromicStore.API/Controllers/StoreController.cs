using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KromicStore.Application.Interfaces;
using KromicStore.API.Authorization;

namespace KromicStore.API.Controllers;

/// <summary>
/// Controller for storefront-related endpoints.
/// </summary>
[ApiController]
[Route("api/v1/store")]
public class StoreController : ControllerBase
{
    private readonly IStoreBootstrapService _bootstrapService;
    private readonly ILogger<StoreController> _logger;
    private readonly ITenantContext _tenantContext;

    public StoreController(
        IStoreBootstrapService bootstrapService,
        ILogger<StoreController> logger,
        ITenantContext tenantContext)
    {
        _bootstrapService = bootstrapService;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Gets the complete bootstrap data for the published storefront.
    /// This endpoint is used by the public storefront to initialize with all required data.
    /// Only returns data for published storefronts.
    /// Supports ETag for conditional requests and caching.
    /// Public endpoint - can be accessed without authentication.
    /// </summary>
    /// <param name="tenantId">The tenant ID to load storefront data for (required for public access).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bootstrap response containing tenant, theme, navigation, homepage, features, and SEO data.</returns>
    /// <response code="200">Bootstrap data retrieved successfully.</response>
    /// <response code="304">Not modified - client has cached version (ETag match).</response>
    /// <response code="404">Tenant not found, not resolved, or storefront not published.</response>
    /// <response code="500">Server error during bootstrap data retrieval.</response>
    [HttpGet("bootstrap/{tenantId}")]
    [ProducesResponseType(typeof(StoreBootstrapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetBootstrap(string tenantId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Public bootstrap data request received for tenant: {TenantId}", tenantId);

            // Manually set tenant context for public access (no JWT required)
            _tenantContext.SetContext(
                tenantId: Guid.Parse(tenantId),
                tenantName: tenantId,
                slug: tenantId,
                domain: Request.Host.Host,
                locale: "en-IN",
                currency: "INR",
                timezone: "Asia/Kolkata"
            );

            var response = await _bootstrapService.GetBootstrapDataAsync(cancellationToken);

            // Generate ETag based on timestamp
            var eTag = $"\"{DateTime.UtcNow:yyyyMMddHHmm}\"";

            // Check if client has cached version
            if (Request.Headers.TryGetValue("If-None-Match", out var clientEtag) && clientEtag == eTag)
            {
                _logger.LogInformation("Bootstrap data not modified - returning 304");
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = eTag;

            _logger.LogInformation("Public bootstrap data retrieved successfully");

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bootstrap failed: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (FormatException)
        {
            _logger.LogWarning("Invalid tenant ID format: {TenantId}", tenantId);
            return BadRequest(new { error = "Invalid tenant ID format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootstrap error");
            return StatusCode(500, new { error = "An error occurred while fetching bootstrap data" });
        }
    }

    /// <summary>
    /// Gets the complete bootstrap data for storefront preview (draft state).
    /// This endpoint is used by tenant admins to preview their storefront before publishing.
    /// Shows the current draft state including unpublished changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bootstrap response containing tenant, theme, navigation, homepage, features, and SEO data.</returns>
    /// <response code="200">Preview bootstrap data retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Tenant not found or storefront not configured.</response>
    /// <response code="500">Server error during bootstrap data retrieval.</response>
    [HttpGet("preview")]
    [Authorize]
    [ProducesResponseType(typeof(StoreBootstrapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPreview(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Preview bootstrap data request received");

            var response = await _bootstrapService.GetPreviewDataAsync(cancellationToken);

            _logger.LogInformation("Preview bootstrap data retrieved successfully");

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Preview bootstrap failed: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview bootstrap error");
            return StatusCode(500, new { error = "An error occurred while fetching preview data" });
        }
    }
}
