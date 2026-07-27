using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using System.Threading.Tasks;

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

    public StoreController(
        IStoreBootstrapService bootstrapService,
        ILogger<StoreController> logger)
    {
        _bootstrapService = bootstrapService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the complete bootstrap data for the storefront.
    /// This endpoint is used by the frontend to initialize the storefront with all required data.
    /// Supports ETag for conditional requests and caching.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bootstrap response containing tenant, theme, navigation, homepage, features, and SEO data.</returns>
    /// <response code="200">Bootstrap data retrieved successfully.</response>
    /// <response code="304">Not modified - client has cached version (ETag match).</response>
    /// <response code="404">Tenant not found or not resolved.</response>
    /// <response code="500">Server error during bootstrap data retrieval.</response>
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(StoreBootstrapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetBootstrap(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Bootstrap data request received");

            var response = await _bootstrapService.GetBootstrapDataAsync(cancellationToken);

            // Generate ETag based on tenant ID and last updated timestamp
            var eTag = $"\"{response.Tenant?.Id}-{DateTime.UtcNow:yyyyMMddHHmm}\"";
            
            // Check if client has cached version
            if (Request.Headers.TryGetValue("If-None-Match", out var clientEtag) && clientEtag == eTag)
            {
                _logger.LogInformation("Bootstrap data not modified - returning 304");
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = eTag;

            _logger.LogInformation("Bootstrap data retrieved successfully");

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bootstrap failed: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootstrap error");
            return StatusCode(500, new { error = "An error occurred while fetching bootstrap data" });
        }
    }
}
