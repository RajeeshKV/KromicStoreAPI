using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;

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
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Bootstrap response containing tenant, theme, navigation, homepage, features, and SEO data.</returns>
    /// <response code="200">Bootstrap data retrieved successfully.</response>
    /// <response code="404">Tenant not found or not resolved.</response>
    /// <response code="500">Server error during bootstrap data retrieval.</response>
    [HttpGet("bootstrap")]
    [ProducesResponseType(typeof(StoreBootstrapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBootstrap(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Bootstrap data request received");

            var response = await _bootstrapService.GetBootstrapDataAsync(cancellationToken);

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
