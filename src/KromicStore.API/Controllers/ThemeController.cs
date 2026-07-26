namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Storefront;
using System.Text.Json;

/// <summary>
/// Controller for retrieving themes (public - no tenant authorization required).
/// Themes are platform-wide resources available to all tenants.
/// </summary>
[ApiController]
[Route("api/v1/themes")]
[Produces("application/json")]
public class ThemeController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ThemeController> _logger;

    /// <summary>
    /// Initializes a new instance of the ThemeController class.
    /// </summary>
    public ThemeController(
        IUnitOfWork unitOfWork,
        ILogger<ThemeController> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all active themes available for storefront creation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of active themes.</returns>
    /// <response code="200">Themes retrieved successfully (may be empty list).</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> ListThemes(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing active themes");

            var themes = await _unitOfWork.Themes.GetActiveAsync(cancellationToken);

            var responses = themes.Select(MapThemeToResponse).ToList();

            _logger.LogInformation("Retrieved {ThemeCount} active themes", responses.Count);

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing themes");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving themes" });
        }
    }

    /// <summary>
    /// Retrieves a specific theme by ID.
    /// </summary>
    /// <param name="id">The theme ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The theme details.</returns>
    /// <response code="200">Theme retrieved successfully.</response>
    /// <response code="404">Theme not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetThemeById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving theme {ThemeId}", id);

            var theme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);

            if (theme == null)
            {
                _logger.LogWarning("Theme {ThemeId} not found", id);
                return NotFound();
            }

            var response = MapThemeToResponse(theme);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving theme {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving the theme" });
        }
    }

    /// <summary>
    /// Retrieves a theme by slug identifier.
    /// </summary>
    /// <param name="slug">The theme slug (e.g., "minimal", "modern", "pro").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The theme details.</returns>
    /// <response code="200">Theme retrieved successfully.</response>
    /// <response code="404">Theme not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetThemeBySlug(
        string slug,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest(new { error = "Slug cannot be empty" });
            }

            _logger.LogInformation("Retrieving theme by slug: {Slug}", slug);

            var theme = await _unitOfWork.Themes.GetBySlugAsync(slug.ToLowerInvariant(), cancellationToken);

            if (theme == null)
            {
                _logger.LogWarning("Theme with slug {Slug} not found", slug);
                return NotFound();
            }

            var response = MapThemeToResponse(theme);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving theme by slug: {Slug}", slug);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving the theme" });
        }
    }

    /// <summary>
    /// Maps a ThemeEntity to a ThemeResponse DTO.
    /// </summary>
    private static ThemeResponse MapThemeToResponse(Domain.Entities.ThemeEntity theme)
    {
        // Parse the definition JSON
        object? definitionObject = null;
        if (!string.IsNullOrEmpty(theme.DefinitionJson))
        {
            try
            {
                definitionObject = JsonSerializer.Deserialize<object>(theme.DefinitionJson);
            }
            catch
            {
                // If parsing fails, leave as null
            }
        }

        return new ThemeResponse
        {
            Id = theme.Id,
            Slug = theme.Slug,
            Name = theme.Name,
            Description = theme.Description,
            Version = theme.Version,
            IsActive = theme.IsActive,
            Definition = definitionObject,
            CreatedAt = theme.CreatedAt,
            UpdatedAt = theme.UpdatedAt
        };
    }
}
