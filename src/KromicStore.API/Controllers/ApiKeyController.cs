// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.ApiKeys;
using KromicStore.Domain.Entities;

/// <summary>
/// Controller for API key management.
/// </summary>
[ApiController]
[Route("api/v1/api-keys")]
[Authorize(Roles = "TenantAdmin")]
[Produces("application/json")]
public class ApiKeyController : BaseController
{
    private readonly IApiKeyService _apiKeyService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ApiKeyController> _logger;

    public ApiKeyController(
        ITenantProvider tenantProvider,
        IApiKeyService apiKeyService,
        IAuditLogService auditLogService,
        ILogger<ApiKeyController> logger)
        : base(tenantProvider)
    {
        _apiKeyService = apiKeyService ?? throw new ArgumentNullException(nameof(apiKeyService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new API key for the tenant.
    /// </summary>
    /// <param name="request">The API key creation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created API key with the plain key (only shown once).</returns>
    /// <response code="201">API key created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateApiKey(
        [FromBody] CreateApiKeyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating API key for tenant {TenantId}: {Name}",
                CurrentTenantId, request.Name);

            var userId = GetCurrentUserId();
            var (apiKey, plainKey) = await _apiKeyService.CreateApiKeyAsync(
                CurrentTenantId,
                request.Name,
                request.Scopes,
                userId,
                request.ExpiresAt,
                cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "ApiKey",
                apiKey.Id,
                "Create",
                cancellationToken: cancellationToken);

            _logger.LogInformation("API key created successfully: {Id}", apiKey.Id);

            return CreatedAtAction(
                nameof(GetApiKey),
                new { id = apiKey.Id },
                new CreateApiKeyResponse
                {
                    Id = apiKey.Id,
                    Name = apiKey.Name,
                    PlainKey = plainKey,
                    KeyPrefix = apiKey.KeyPrefix,
                    Scopes = apiKey.Scopes,
                    ExpiresAt = apiKey.ExpiresAt,
                    CreatedAt = apiKey.CreatedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, new { error = "An error occurred while creating the API key" });
        }
    }

    /// <summary>
    /// Gets all API keys for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of API keys (without plain keys).</returns>
    /// <response code="200">API keys retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiKeyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetApiKeys(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting API keys for tenant {TenantId}", CurrentTenantId);

            var apiKeys = await _apiKeyService.GetTenantApiKeysAsync(
                CurrentTenantId,
                cancellationToken);

            var responses = apiKeys.Select(MapToResponse);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API keys");
            return StatusCode(500, new { error = "An error occurred while retrieving API keys" });
        }
    }

    /// <summary>
    /// Gets a specific API key by ID.
    /// </summary>
    /// <param name="id">The API key ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The API key details (without plain key).</returns>
    /// <response code="200">API key retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">API key not found.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetApiKey(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting API key {Id}", id);

            var apiKey = await _apiKeyService.GetApiKeyAsync(id, cancellationToken);
            if (apiKey == null || apiKey.TenantId != CurrentTenantId)
            {
                return NotFound(new { error = "API key not found" });
            }

            return Ok(MapToResponse(apiKey));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the API key" });
        }
    }

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="id">The API key ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">API key revoked successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">API key not found.</response>
    /// <response code="500">Server error.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RevokeApiKey(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Revoking API key {Id}", id);

            var apiKey = await _apiKeyService.GetApiKeyAsync(id, cancellationToken);
            if (apiKey == null || apiKey.TenantId != CurrentTenantId)
            {
                return NotFound(new { error = "API key not found" });
            }

            var userId = GetCurrentUserId();
            await _apiKeyService.RevokeApiKeyAsync(id, cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "ApiKey",
                id,
                "Revoke",
                cancellationToken: cancellationToken);

            _logger.LogInformation("API key {Id} revoked successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {Id}", id);
            return StatusCode(500, new { error = "An error occurred while revoking the API key" });
        }
    }

    private static ApiKeyResponse MapToResponse(ApiKey apiKey)
    {
        return new ApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            KeyPrefix = apiKey.KeyPrefix,
            Scopes = apiKey.Scopes,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            IsActive = apiKey.IsActive,
            CreatedAt = apiKey.CreatedAt,
            UpdatedAt = apiKey.UpdatedAt
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value 
            ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
