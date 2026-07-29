namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using KromicStore.API.Authorization;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Storefront;
using KromicStore.Infrastructure.Services.StorefrontServices;
using System.Text.Json;

/// <summary>
/// Controller for managing storefronts (TenantAdmin+ authorization required).
/// </summary>
[ApiController]
[Route("api/v1/storefronts")]
[Authorize]
[Produces("application/json")]
public class StorefrontController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorefrontCreationService _storefrontCreationService;
    private readonly ILogger<StorefrontController> _logger;

    /// <summary>
    /// Initializes a new instance of the StorefrontController class.
    /// </summary>
    public StorefrontController(
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        IStorefrontCreationService storefrontCreationService,
        ILogger<StorefrontController> logger)
        : base(tenantProvider)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _storefrontCreationService = storefrontCreationService ?? throw new ArgumentNullException(nameof(storefrontCreationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates or returns the storefront for the current tenant.
    /// Since one tenant = one storefront, this endpoint creates it on first call
    /// and returns 200 OK if already exists (idempotent).
    /// </summary>
    /// <param name="request">The request containing initial storefront details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The storefront (newly created or existing).</returns>
    /// <response code="201">Storefront created successfully.</response>
    /// <response code="200">Storefront already exists (returned as-is).</response>
    /// <response code="400">Validation failed or invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="422">Operation failed.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrGetStorefront(
        [FromBody] CreateStorefrontRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Create/Get storefront for tenant {TenantId}",
                CurrentTenantId);

            // Check if storefront already exists for this tenant
            var existingStorefronts = await _unitOfWork.Storefronts.GetByTenantAsync(
                CurrentTenantId,
                cancellationToken);

            if (existingStorefronts.Any())
            {
                var existingStorefront = existingStorefronts.First();
                _logger.LogInformation(
                    "Storefront {StorefrontId} already exists for tenant {TenantId}",
                    existingStorefront.Id,
                    CurrentTenantId);
                
                var existingResponse = MapStorefrontToResponse(existingStorefront);
                return Ok(existingResponse);
            }

            // Create new storefront
            Guid storefrontId;
            if (!string.IsNullOrEmpty(request.ThemeId) && Guid.TryParse(request.ThemeId, out var themeId))
            {
                // Create from theme
                storefrontId = await _storefrontCreationService.CreateFromThemeAsync(
                    CurrentTenantId,
                    themeId,
                    request.Name,
                    cancellationToken);
            }
            else
            {
                // Create from scratch
                storefrontId = await _storefrontCreationService.CreateFromScratchAsync(
                    CurrentTenantId,
                    request.Name,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Storefront {StorefrontId} created for tenant {TenantId}",
                storefrontId,
                CurrentTenantId);

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(storefrontId, CurrentTenantId, cancellationToken);
            var response = MapStorefrontToResponse(storefront);
            
            return CreatedAtAction(
                nameof(GetTenantStorefront),
                new { },
                response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating storefront");
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storefront");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while creating the storefront" });
        }
    }

    /// <summary>
    /// Gets the current tenant's single storefront.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant's storefront, or 404 if none created yet.</returns>
    /// <response code="200">Storefront retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Storefront not found (not yet created).</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTenantStorefront(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving storefront for tenant {TenantId}", CurrentTenantId);

            var storefronts = await _unitOfWork.Storefronts.GetByTenantAsync(
                CurrentTenantId,
                cancellationToken);

            if (!storefronts.Any())
            {
                _logger.LogWarning("No storefront found for tenant {TenantId}", CurrentTenantId);
                return NotFound(new { error = "Storefront not found. Create one using POST /api/v1/storefronts" });
            }

            var storefront = storefronts.First();
            var response = MapStorefrontToResponse(storefront);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storefront for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving the storefront" });
        }
    }

    /// <summary>
    /// Updates the current tenant's single storefront.
    /// Since each tenant has only one storefront, no ID is needed.
    /// </summary>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated storefront.</returns>
    /// <response code="200">Storefront updated successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="404">Storefront not found.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTenantStorefront(
        [FromBody] UpdateStorefrontRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating storefront for tenant {TenantId}", CurrentTenantId);

            var storefronts = await _unitOfWork.Storefronts.GetByTenantAsync(
                CurrentTenantId,
                cancellationToken);

            if (!storefronts.Any())
            {
                _logger.LogWarning("No storefront found for tenant {TenantId} to update", CurrentTenantId);
                return NotFound(new { error = "Storefront not found. Create one using POST /api/v1/storefronts" });
            }

            var storefront = storefronts.First();

            // Update storefront
            storefront.UpdateInfo(
                request.Name ?? storefront.Name,
                request.LogoUrl,
                request.ContactEmail,
                request.ContactPhone,
                request.Address,
                request.Currency,
                request.Country,
                request.BrandColor,
                request.Copyright,
                request.FacebookUrl,
                request.TwitterUrl,
                request.InstagramUrl,
                request.LinkedInUrl);

            _unitOfWork.Storefronts.Update(storefront);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Storefront for tenant {TenantId} updated successfully", CurrentTenantId);

            var response = MapStorefrontToResponse(storefront);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storefront for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while updating the storefront" });
        }
    }

    /// <summary>
    /// Validates mandatory fields for storefront publication.
    /// </summary>
    /// <param name="id">The storefront ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Validation result with any missing fields.</returns>
    /// <response code="200">Validation completed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this storefront.</response>
    /// <response code="404">Storefront not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}/validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateMandatoryFields(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating storefront {StorefrontId} for tenant {TenantId}", id, CurrentTenantId);

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(id, CurrentTenantId, cancellationToken);

            if (storefront == null)
            {
                return NotFound();
            }

            if (storefront.TenantId != CurrentTenantId)
            {
                return Forbid();
            }

            var validationResult = await _storefrontCreationService.ValidateMandatoryFieldsAsync(id, cancellationToken);

            var result = new ValidationResultResponse
            {
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating storefront {StorefrontId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred during validation" });
        }
    }

    /// <summary>
    /// Checks for pending changes between draft and published storefront.
    /// </summary>
    /// <param name="id">The storefront ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Pending changes status and details.</returns>
    /// <response code="200">Pending changes check completed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this storefront.</response>
    /// <response code="404">Storefront not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}/pending-changes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPendingChanges(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking pending changes for storefront {StorefrontId} for tenant {TenantId}", id, CurrentTenantId);

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(id, CurrentTenantId, cancellationToken);

            if (storefront == null)
            {
                return NotFound();
            }

            if (storefront.TenantId != CurrentTenantId)
            {
                return Forbid();
            }

            var hasPendingChanges = storefront.Status == Domain.Enums.StorefrontStatus.Draft ||
                                  storefront.UpdatedAt > storefront.PublishedAt;

            var result = new PendingChangesResponse
            {
                HasPendingChanges = hasPendingChanges,
                Status = storefront.Status.ToString(),
                LastUpdated = storefront.UpdatedAt,
                LastPublished = storefront.PublishedAt,
                Changes = hasPendingChanges ? new List<string>
                {
                    storefront.Status == Domain.Enums.StorefrontStatus.Draft ? "Storefront is in draft state" : "Storefront has unpublished changes since last publish"
                } : new List<string>()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking pending changes for storefront {StorefrontId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while checking pending changes" });
        }
    }

    /// <summary>
    /// Publishes a storefront (makes it publicly accessible).
    /// Validates that all mandatory fields are provided before publishing.
    /// </summary>
    /// <param name="id">The storefront ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The published storefront.</returns>
    /// <response code="200">Storefront published successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this storefront.</response>
    /// <response code="404">Storefront not found.</response>
    /// <response code="422">Cannot publish: missing mandatory fields or invalid state.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost("{id}/publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PublishStorefront(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Publishing storefront {StorefrontId} for tenant {TenantId}", id, CurrentTenantId);

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(id, CurrentTenantId, cancellationToken);

            if (storefront == null)
            {
                return NotFound();
            }

            if (storefront.TenantId != CurrentTenantId)
            {
                return Forbid();
            }

            // Publish (this will throw if mandatory fields are missing)
            storefront.Publish();

            _unitOfWork.Storefronts.Update(storefront);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Storefront {StorefrontId} published successfully", id);

            var response = MapStorefrontToResponse(storefront);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot publish storefront {StorefrontId}: {Reason}", id, ex.Message);
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing storefront {StorefrontId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while publishing the storefront" });
        }
    }

    /// <summary>
    /// Deletes/archives a storefront.
    /// </summary>
    /// <param name="id">The storefront ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Storefront deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this storefront.</response>
    /// <response code="404">Storefront not found.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteStorefront(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting storefront {StorefrontId} for tenant {TenantId}", id, CurrentTenantId);

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(id, CurrentTenantId, cancellationToken);

            if (storefront == null)
            {
                return NotFound();
            }

            if (storefront.TenantId != CurrentTenantId)
            {
                return Forbid();
            }

            storefront.Archive();

            _unitOfWork.Storefronts.Update(storefront);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Storefront {StorefrontId} deleted successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting storefront {StorefrontId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while deleting the storefront" });
        }
    }

    /// <summary>
    /// Maps a Storefront entity to a StorefrontResponse DTO.
    /// </summary>
    private static StorefrontResponse MapStorefrontToResponse(Domain.Entities.Storefront storefront)
    {
        return new StorefrontResponse
        {
            Id = storefront.Id,
            Name = storefront.Name,
            Status = storefront.Status.ToString(),
            ThemeId = storefront.ThemeId,
            LogoUrl = storefront.LogoUrl,
            ContactEmail = storefront.ContactEmail,
            ContactPhone = storefront.ContactPhone,
            Address = storefront.Address,
            Currency = storefront.Currency,
            Country = storefront.Country,
            BrandColor = storefront.BrandColor,
            Copyright = storefront.Copyright,
            FacebookUrl = storefront.FacebookUrl,
            TwitterUrl = storefront.TwitterUrl,
            InstagramUrl = storefront.InstagramUrl,
            LinkedInUrl = storefront.LinkedInUrl,
            MandatoryFieldsStatus = MapMandatoryFieldsStatus(storefront.MandatoryFields),
            Pages = storefront.Pages?.Select(MapPageToResponse).ToList() ?? new(),
            CreatedAt = storefront.CreatedAt,
            UpdatedAt = storefront.UpdatedAt
        };
    }

    /// <summary>
    /// Maps a StorefrontPage entity to a StorefrontPageResponse DTO.
    /// </summary>
    private static StorefrontPageResponse MapPageToResponse(Domain.Entities.StorefrontPage page)
    {
        return new StorefrontPageResponse
        {
            Id = page.Id,
            Name = page.Name,
            Slug = page.Slug,
            Description = page.Description,
            Visibility = page.Visibility.ToString(),
            LayoutType = page.LayoutType,
            DisplayOrder = page.DisplayOrder,
            MetaKeywords = page.MetaKeywords,
            IsFeatured = page.IsFeatured,
            Sections = page.Sections?.Select(MapSectionToResponse).ToList() ?? new()
        };
    }

    /// <summary>
    /// Maps a StorefrontSection entity to a StorefrontSectionResponse DTO.
    /// </summary>
    private static StorefrontSectionResponse MapSectionToResponse(Domain.Entities.StorefrontSection section)
    {
        return new StorefrontSectionResponse
        {
            Id = section.Id,
            Name = section.Name,
            Description = section.Description,
            IsVisible = section.IsVisible,
            DisplayOrder = section.DisplayOrder,
            CssClass = section.CssClass,
            BackgroundColor = section.BackgroundColor,
            BackgroundImageUrl = section.BackgroundImageUrl,
            Components = section.Components?.Select(MapComponentToResponse).ToList() ?? new()
        };
    }

    /// <summary>
    /// Maps a StorefrontComponent entity to a StorefrontComponentResponse DTO.
    /// </summary>
    private static StorefrontComponentResponse MapComponentToResponse(Domain.Entities.StorefrontComponent component)
    {
        return new StorefrontComponentResponse
        {
            Id = component.Id,
            Type = component.Type.ToString(),
            Config = component.Config != null ? JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(component.Config)) : null,
            IsVisible = component.IsVisible,
            DisplayOrder = component.DisplayOrder,
            CssClass = component.CssClass,
            TrackingId = component.TrackingId
        };
    }

    /// <summary>
    /// Maps MandatoryFields to MandatoryFieldsStatusResponse DTO.
    /// </summary>
    private static MandatoryFieldsStatusResponse MapMandatoryFieldsStatus(Domain.ValueObjects.MandatoryFields fields)
    {
        return new MandatoryFieldsStatusResponse
        {
            StoreNameIsPlaceholder = fields.IsStoreNamePlaceholder,
            LogoIsPlaceholder = fields.IsLogoPlaceholder,
            EmailIsPlaceholder = fields.IsEmailPlaceholder,
            PhoneIsPlaceholder = fields.IsPhonePlaceholder,
            AddressIsPlaceholder = fields.IsAddressPlaceholder,
            CurrencyIsPlaceholder = fields.IsCurrencyPlaceholder,
            CountryIsPlaceholder = fields.IsCountryPlaceholder,
            BrandColorIsPlaceholder = fields.IsBrandColorPlaceholder,
            CopyrightIsPlaceholder = fields.IsCopyrightPlaceholder
        };
    }
}
