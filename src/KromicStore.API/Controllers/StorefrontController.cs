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
    /// Creates a new storefront from an existing theme template.
    /// </summary>
    /// <param name="request">The request containing theme ID and storefront name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created storefront with ID.</returns>
    /// <response code="201">Storefront created successfully.</response>
    /// <response code="400">Validation failed or invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="422">Invalid theme ID or operation failed.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost("from-theme")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFromTheme(
        [FromBody] CreateStorefrontFromThemeRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating storefront from theme for tenant {TenantId} with theme {ThemeId}",
                CurrentTenantId,
                request.ThemeId);

            var storefrontId = await _storefrontCreationService.CreateFromThemeAsync(
                CurrentTenantId,
                request.ThemeId,
                request.StoreName,
                cancellationToken);

            _logger.LogInformation(
                "Storefront {StorefrontId} created from theme for tenant {TenantId}",
                storefrontId,
                CurrentTenantId);

            return CreatedAtAction(
                nameof(GetStorefront),
                new { id = storefrontId },
                new { id = storefrontId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating storefront from theme");
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storefront from theme");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while creating the storefront" });
        }
    }

    /// <summary>
    /// Creates a new storefront from scratch without a theme template.
    /// </summary>
    /// <param name="request">The request containing storefront name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created storefront with ID.</returns>
    /// <response code="201">Storefront created successfully.</response>
    /// <response code="400">Validation failed or invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPost("from-scratch")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFromScratch(
        [FromBody] CreateStorefrontFromScratchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating storefront from scratch for tenant {TenantId} with name {StoreName}",
                CurrentTenantId,
                request.StoreName);

            var storefrontId = await _storefrontCreationService.CreateFromScratchAsync(
                CurrentTenantId,
                request.StoreName,
                cancellationToken);

            _logger.LogInformation(
                "Storefront {StorefrontId} created from scratch for tenant {TenantId}",
                storefrontId,
                CurrentTenantId);

            return CreatedAtAction(
                nameof(GetStorefront),
                new { id = storefrontId },
                new { id = storefrontId });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating storefront from scratch");
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storefront from scratch");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while creating the storefront" });
        }
    }

    /// <summary>
    /// Retrieves a storefront by ID (tenant-scoped).
    /// </summary>
    /// <param name="id">The storefront ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The storefront details.</returns>
    /// <response code="200">Storefront retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this storefront (different tenant).</response>
    /// <response code="404">Storefront not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStorefront(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving storefront {StorefrontId} for tenant {TenantId}", id, CurrentTenantId);

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(id, CurrentTenantId, cancellationToken);

            if (storefront == null)
            {
                _logger.LogWarning("Storefront {StorefrontId} not found for tenant {TenantId}", id, CurrentTenantId);
                return NotFound();
            }

            if (storefront.TenantId != CurrentTenantId)
            {
                _logger.LogWarning(
                    "Access denied: user from tenant {UserTenant} attempted to access storefront from tenant {StorefrontTenant}",
                    CurrentTenantId,
                    storefront.TenantId);
                return Forbid();
            }

            var response = MapStorefrontToResponse(storefront);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving storefront {StorefrontId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving the storefront" });
        }
    }

    /// <summary>
    /// Lists all storefronts for the tenant with pagination.
    /// </summary>
    /// <param name="page">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 10, max: 100).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of storefronts.</returns>
    /// <response code="200">Storefronts retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListStorefronts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Enforce max page size
            const int maxPageSize = 100;
            pageSize = Math.Min(Math.Max(pageSize, 1), maxPageSize);

            _logger.LogInformation(
                "Listing storefronts for tenant {TenantId}",
                CurrentTenantId);

            var storefronts = await _unitOfWork.Storefronts.GetByTenantAsync(
                CurrentTenantId,
                cancellationToken);

            // Apply pagination manually
            var paginatedStorefronts = storefronts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var responses = paginatedStorefronts.Select(MapStorefrontToResponse).ToList();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing storefronts for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving storefronts" });
        }
    }

    /// <summary>
    /// Updates storefront metadata and configuration.
    /// </summary>
    /// <param name="id">The storefront ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated storefront.</returns>
    /// <response code="200">Storefront updated successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User does not have access to this storefront.</response>
    /// <response code="404">Storefront not found.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize(Policy = Permissions.StoreWrite)]
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStorefront(
        Guid id,
        [FromBody] UpdateStorefrontRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating storefront {StorefrontId} for tenant {TenantId}", id, CurrentTenantId);

            var storefront = await _unitOfWork.Storefronts.GetByIdAsync(id, CurrentTenantId, cancellationToken);

            if (storefront == null)
            {
                _logger.LogWarning("Storefront {StorefrontId} not found for tenant {TenantId}", id, CurrentTenantId);
                return NotFound();
            }

            if (storefront.TenantId != CurrentTenantId)
            {
                _logger.LogWarning("Access denied for storefront {StorefrontId}", id);
                return Forbid();
            }

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
                request.Copyright);

            _unitOfWork.Storefronts.Update(storefront);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Storefront {StorefrontId} updated successfully", id);

            var response = MapStorefrontToResponse(storefront);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating storefront {StorefrontId}", id);
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
