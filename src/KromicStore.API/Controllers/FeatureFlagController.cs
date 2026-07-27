// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.FeatureFlags;
using KromicStore.Domain.Entities;

/// <summary>
/// Controller for feature flag management.
/// </summary>
[ApiController]
[Route("api/v1/feature-flags")]
[Authorize(Roles = "TenantAdmin")]
[Produces("application/json")]
public class FeatureFlagController : BaseController
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<FeatureFlagController> _logger;

    public FeatureFlagController(
        ITenantProvider tenantProvider,
        IFeatureFlagService featureFlagService,
        IAuditLogService auditLogService,
        ILogger<FeatureFlagController> logger)
        : base(tenantProvider)
    {
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all feature flags for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of feature flags.</returns>
    /// <response code="200">Feature flags retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FeatureFlagResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting feature flags for tenant {TenantId}", CurrentTenantId);

            var flags = await _featureFlagService.GetTenantFeatureFlagsAsync(
                CurrentTenantId,
                cancellationToken);

            var responses = flags.Select(MapToResponse);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flags");
            return StatusCode(500, new { error = "An error occurred while retrieving feature flags" });
        }
    }

    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="key">The feature key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Feature status.</returns>
    /// <response code="200">Feature status retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("check/{key}")]
    [ProducesResponseType(typeof(FeatureCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckFeature(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Checking feature {Key} for tenant {TenantId}", key, CurrentTenantId);

            var isEnabled = await _featureFlagService.IsFeatureEnabledAsync(
                CurrentTenantId,
                key,
                cancellationToken);

            return Ok(new FeatureCheckResponse
            {
                Key = key,
                IsEnabled = isEnabled
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature {Key}", key);
            return StatusCode(500, new { error = "An error occurred while checking the feature" });
        }
    }

    /// <summary>
    /// Creates a new feature flag.
    /// </summary>
    /// <param name="request">The feature flag creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created feature flag.</returns>
    /// <response code="201">Feature flag created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="409">Feature flag already exists.</response>
    /// <response code="500">Server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFeatureFlag(
        [FromBody] CreateFeatureFlagRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating feature flag {Key} for tenant {TenantId}",
                request.Key, CurrentTenantId);

            var userId = GetCurrentUserId();
            var featureFlag = await _featureFlagService.CreateFeatureFlagAsync(
                CurrentTenantId,
                request.Key,
                request.IsEnabled,
                request.Description,
                request.Type,
                request.Plan,
                cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "FeatureFlag",
                featureFlag.Id,
                "Create",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Feature flag created successfully: {Id}", featureFlag.Id);

            return CreatedAtAction(
                nameof(GetFeatureFlag),
                new { id = featureFlag.Id },
                MapToResponse(featureFlag));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feature flag");
            return StatusCode(500, new { error = "An error occurred while creating the feature flag" });
        }
    }

    /// <summary>
    /// Gets a specific feature flag by ID.
    /// </summary>
    /// <param name="id">The feature flag ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The feature flag details.</returns>
    /// <response code="200">Feature flag retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Feature flag not found.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFeatureFlag(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting feature flag {Id}", id);

            var featureFlag = await _featureFlagService.GetFeatureFlagAsync(id, cancellationToken);
            if (featureFlag == null)
            {
                return NotFound(new { error = "Feature flag not found" });
            }

            return Ok(MapToResponse(featureFlag));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature flag {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the feature flag" });
        }
    }

    /// <summary>
    /// Updates a feature flag.
    /// </summary>
    /// <param name="id">The feature flag ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated feature flag.</returns>
    /// <response code="200">Feature flag updated successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Feature flag not found.</response>
    /// <response code="500">Server error.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateFeatureFlag(
        Guid id,
        [FromBody] UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating feature flag {Id}", id);

            var userId = GetCurrentUserId();
            await _featureFlagService.UpdateFeatureFlagAsync(
                id,
                request.IsEnabled,
                request.Description,
                cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "FeatureFlag",
                id,
                "Update",
                cancellationToken: cancellationToken);

            var featureFlag = await _featureFlagService.GetFeatureFlagAsync(id, cancellationToken);
            _logger.LogInformation("Feature flag {Id} updated successfully", id);

            return Ok(MapToResponse(featureFlag!));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature flag {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the feature flag" });
        }
    }

    /// <summary>
    /// Deletes a feature flag.
    /// </summary>
    /// <param name="id">The feature flag ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Feature flag deleted successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Feature flag not found.</response>
    /// <response code="500">Server error.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFeatureFlag(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting feature flag {Id}", id);

            var userId = GetCurrentUserId();
            await _featureFlagService.DeleteFeatureFlagAsync(id, cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "FeatureFlag",
                id,
                "Delete",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Feature flag {Id} deleted successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feature flag {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the feature flag" });
        }
    }

    private static FeatureFlagResponse MapToResponse(FeatureFlag flag)
    {
        return new FeatureFlagResponse
        {
            Id = flag.Id,
            Key = flag.Key,
            Description = flag.Description,
            IsEnabled = flag.IsEnabled,
            Type = flag.Type,
            Plan = flag.Plan,
            CreatedAt = flag.CreatedAt,
            UpdatedAt = flag.UpdatedAt
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value 
            ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
