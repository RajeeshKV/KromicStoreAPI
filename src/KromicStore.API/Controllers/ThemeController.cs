namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Storefront;
using System.Text.Json;

/// <summary>
/// Controller for managing themes.
/// - Tenants: Can manage only their own themes and clone public themes
/// - SuperUsers: Can manage platform themes (TenantId=null, IsPublic=true)
/// </summary>
[ApiController]
[Route("api/v1/themes")]
[Produces("application/json")]
public class ThemeController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ThemeController> _logger;

    public ThemeController(
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ILogger<ThemeController> logger)
        : base(tenantProvider)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists themes:
    /// - For tenants: Their own themes + public themes (for browsing/cloning)
    /// - For SU: Platform themes only (TenantId=null)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListThemes(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing themes");
            var themes = await _unitOfWork.Themes.GetActiveAsync(cancellationToken);
            var responses = themes.Select(MapThemeToResponse).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing themes");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve themes" });
        }
    }

    /// <summary>Gets theme by ID (readable by anyone).</summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetThemeById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);
            if (theme == null)
                return NotFound();
            return Ok(MapThemeToResponse(theme));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving theme {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve theme" });
        }
    }

    /// <summary>Gets theme by slug (readable by anyone).</summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetThemeBySlug(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
                return BadRequest(new { error = "Slug cannot be empty" });

            var theme = await _unitOfWork.Themes.GetBySlugAsync(slug.ToLowerInvariant(), cancellationToken);
            if (theme == null)
                return NotFound();
            return Ok(MapThemeToResponse(theme));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving theme by slug: {Slug}", slug);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve theme" });
        }
    }

    /// <summary>
    /// Creates a new theme.
    /// - Tenants: Creates tenant-specific theme (TenantId = CurrentTenantId)
    /// - SU: Can create via dedicated SU endpoint only
    /// </summary>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTheme([FromBody] CreateThemeRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            var userId = HttpContext.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var createdByUserId))
                return Unauthorized(new { error = "Invalid user context" });

            // Only tenants can create themes via this endpoint
            // SU creates themes via SuperUserPlatformController
            _logger.LogInformation("Creating theme for tenant {TenantId}", CurrentTenantId);

            var theme = Domain.Entities.Theme.CreateTenantTheme(
                CurrentTenantId,
                request.Name,
                request.DefinitionJson,
                request.IsPublic,
                createdByUserId);

            if (!string.IsNullOrWhiteSpace(request.Description))
                theme.UpdateMetadata(request.Name, request.Description, request.Version, createdByUserId);

            await _unitOfWork.Themes.AddAsync(theme, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Theme {ThemeId} created for tenant {TenantId}", theme.Id, CurrentTenantId);
            return CreatedAtAction(nameof(GetThemeById), new { id = theme.Id }, MapThemeToResponse(theme));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating theme");
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating theme");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to create theme" });
        }
    }

    /// <summary>
    /// Updates an existing theme.
    /// Access: Tenants can only update their own themes. SU can update public themes.
    /// </summary>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTheme(Guid id, [FromBody] UpdateThemeRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            var theme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);
            if (theme == null)
                return NotFound();

            // Access control: Tenants can only edit their own themes
            if (theme.OwnerTenantId != CurrentTenantId)
                return Forbid();

            var userId = HttpContext.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var modifiedByUserId))
                return Unauthorized(new { error = "Invalid user context" });

            if (!string.IsNullOrWhiteSpace(request.DefinitionJson))
                theme.UpdateDefinition(request.DefinitionJson, modifiedByUserId);

            if (!string.IsNullOrWhiteSpace(request.Name) || !string.IsNullOrWhiteSpace(request.Description) || !string.IsNullOrWhiteSpace(request.Version))
                theme.UpdateMetadata(request.Name ?? theme.Name, request.Description ?? theme.Description, request.Version ?? theme.Version, modifiedByUserId);

            _unitOfWork.Themes.Update(theme);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Theme {ThemeId} updated by tenant {TenantId}", id, CurrentTenantId);
            return Ok(MapThemeToResponse(theme));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while updating theme");
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating theme {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to update theme" });
        }
    }

    /// <summary>
    /// Activates a theme for the tenant (deactivates all others).
    /// Access: Tenants can only activate their own themes.
    /// </summary>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActivateTheme(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);
            if (theme == null)
                return NotFound();

            if (theme.OwnerTenantId != CurrentTenantId)
                return Forbid();

            var userId = HttpContext.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var modifiedByUserId))
                return Unauthorized(new { error = "Invalid user context" });

            // Deactivate all other themes for this tenant
            var allThemes = await _unitOfWork.Themes.GetByTenantAsync(CurrentTenantId, cancellationToken);
            foreach (var t in allThemes.Where(t => t.Id != id && t.IsActive))
            {
                t.Deactivate(modifiedByUserId);
                _unitOfWork.Themes.Update(t);
            }

            theme.Activate(modifiedByUserId);
            _unitOfWork.Themes.Update(theme);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Theme {ThemeId} activated for tenant {TenantId}", id, CurrentTenantId);
            return Ok(MapThemeToResponse(theme));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating theme {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to activate theme" });
        }
    }

    /// <summary>
    /// Clones a public theme for the tenant to customize.
    /// Creates a new tenant-specific theme with reference to source.
    /// Access: Any tenant can clone public themes.
    /// </summary>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost("{id}/clone")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CloneTheme(Guid id, [FromBody] CloneThemeRequest? request, CancellationToken cancellationToken = default)
    {
        try
        {
            var sourceTheme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);
            if (sourceTheme == null)
                return NotFound();

            // Can only clone public themes or own themes
            if (!sourceTheme.IsPublic && sourceTheme.OwnerTenantId != CurrentTenantId)
                return Forbid();

            var userId = HttpContext.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var createdByUserId))
                return Unauthorized(new { error = "Invalid user context" });

            var clonedTheme = sourceTheme.Clone(CurrentTenantId, createdByUserId, request?.NewName);
            await _unitOfWork.Themes.AddAsync(clonedTheme, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Theme {SourceThemeId} cloned to {ClonedThemeId} for tenant {TenantId}", id, clonedTheme.Id, CurrentTenantId);
            return CreatedAtAction(nameof(GetThemeById), new { id = clonedTheme.Id }, MapThemeToResponse(clonedTheme));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while cloning theme");
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning theme {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to clone theme" });
        }
    }

    /// <summary>
    /// Makes a theme public (shared with other tenants).
    /// Access: Tenants can only make their own themes public.
    /// </summary>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost("{id}/make-public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MakeThemePublic(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);
            if (theme == null)
                return NotFound();

            if (theme.OwnerTenantId != CurrentTenantId)
                return Forbid();

            var userId = HttpContext.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var modifiedByUserId))
                return Unauthorized(new { error = "Invalid user context" });

            theme.MakePublic(modifiedByUserId);
            _unitOfWork.Themes.Update(theme);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Theme {ThemeId} made public by tenant {TenantId}", id, CurrentTenantId);
            return Ok(MapThemeToResponse(theme));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making theme public {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to make theme public" });
        }
    }

    /// <summary>
    /// Makes a theme private (not shared).
    /// Access: Tenants can only make their own themes private.
    /// </summary>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost("{id}/make-private")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MakeThemePrivate(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);
            if (theme == null)
                return NotFound();

            if (theme.OwnerTenantId != CurrentTenantId)
                return Forbid();

            var userId = HttpContext.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var modifiedByUserId))
                return Unauthorized(new { error = "Invalid user context" });

            theme.MakePrivate(modifiedByUserId);
            _unitOfWork.Themes.Update(theme);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Theme {ThemeId} made private by tenant {TenantId}", id, CurrentTenantId);
            return Ok(MapThemeToResponse(theme));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making theme private {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to make theme private" });
        }
    }

    /// <summary>
    /// Deletes/deactivates a theme.
    /// Access: Tenants can only delete their own themes.
    /// </summary>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTheme(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = await _unitOfWork.Themes.GetByIdAsync(id, cancellationToken);
            if (theme == null)
                return NotFound();

            if (theme.OwnerTenantId != CurrentTenantId)
                return Forbid();

            var userId = HttpContext.User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userId, out var modifiedByUserId))
                return Unauthorized(new { error = "Invalid user context" });

            theme.Deactivate(modifiedByUserId);
            _unitOfWork.Themes.Update(theme);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Theme {ThemeId} deleted by tenant {TenantId}", id, CurrentTenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting theme {ThemeId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to delete theme" });
        }
    }

    private static ThemeResponse MapThemeToResponse(Domain.Entities.Theme theme)
    {
        object? definitionObject = null;
        if (!string.IsNullOrEmpty(theme.DefinitionJson))
        {
            try
            {
                definitionObject = JsonSerializer.Deserialize<object>(theme.DefinitionJson);
            }
            catch { }
        }

        return new ThemeResponse
        {
            Id = theme.Id,
            Slug = theme.Slug,
            Name = theme.Name,
            Description = theme.Description,
            Version = theme.Version,
            IsActive = theme.IsActive,
            IsPublic = theme.IsPublic,
            SourceThemeId = theme.SourceThemeId,
            TenantId = theme.OwnerTenantId,
            Definition = definitionObject,
            CreatedAt = theme.CreatedAt,
            UpdatedAt = theme.UpdatedAt
        };
    }
}
