// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KromicStore.Application.Interfaces;
using KromicStore.Contracts.V1.Team;
using KromicStore.Domain.Entities;

/// <summary>
/// Controller for team invitation and role management.
/// </summary>
[ApiController]
[Route("api/v1/team")]
[Authorize(Roles = "TenantAdmin")]
[Produces("application/json")]
public class TeamInvitationController : BaseController
{
    private readonly ITeamInvitationService _invitationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TeamInvitationController> _logger;

    public TeamInvitationController(
        ITenantProvider tenantProvider,
        ITeamInvitationService invitationService,
        IAuditLogService auditLogService,
        ILogger<TeamInvitationController> logger)
        : base(tenantProvider)
    {
        _invitationService = invitationService ?? throw new ArgumentNullException(nameof(invitationService));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates and sends a team invitation.
    /// </summary>
    /// <param name="request">The invitation request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created invitation.</returns>
    /// <response code="201">Invitation created successfully.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="409">Invitation already exists for this email.</response>
    /// <response code="500">Server error.</response>
    [HttpPost("invitations")]
    [ProducesResponseType(typeof(TeamInvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateInvitation(
        [FromBody] CreateTeamInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating team invitation for tenant {TenantId}, email {Email}",
                CurrentTenantId, request.Email);

            var userId = GetCurrentUserId();
            var invitation = await _invitationService.CreateInvitationAsync(
                CurrentTenantId,
                request.Email,
                request.Role,
                userId,
                cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "TeamInvitation",
                invitation.Id,
                "Create",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Team invitation created successfully: {InvitationId}", invitation.Id);

            return CreatedAtAction(
                nameof(GetInvitation),
                new { id = invitation.Id },
                MapToResponse(invitation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team invitation");
            return StatusCode(500, new { error = "An error occurred while creating the invitation" });
        }
    }

    /// <summary>
    /// Gets all invitations for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of invitations.</returns>
    /// <response code="200">Invitations retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("invitations")]
    [ProducesResponseType(typeof(IEnumerable<TeamInvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInvitations(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting team invitations for tenant {TenantId}", CurrentTenantId);

            var invitations = await _invitationService.GetTenantInvitationsAsync(
                CurrentTenantId,
                cancellationToken);

            var responses = invitations.Select(MapToResponse);
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team invitations");
            return StatusCode(500, new { error = "An error occurred while retrieving invitations" });
        }
    }

    /// <summary>
    /// Gets a specific invitation by ID.
    /// </summary>
    /// <param name="id">The invitation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The invitation details.</returns>
    /// <response code="200">Invitation retrieved successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Invitation not found.</response>
    /// <response code="500">Server error.</response>
    [HttpGet("invitations/{id}")]
    [ProducesResponseType(typeof(TeamInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInvitation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting team invitation {InvitationId}", id);

            var invitations = await _invitationService.GetTenantInvitationsAsync(
                CurrentTenantId,
                cancellationToken);

            var invitation = invitations.FirstOrDefault(i => i.Id == id);
            if (invitation == null)
            {
                return NotFound(new { error = "Invitation not found" });
            }

            return Ok(MapToResponse(invitation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team invitation {InvitationId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the invitation" });
        }
    }

    /// <summary>
    /// Cancels a team invitation.
    /// </summary>
    /// <param name="id">The invitation ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Invitation cancelled successfully.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="403">User is not authorized.</response>
    /// <response code="404">Invitation not found.</response>
    /// <response code="500">Server error.</response>
    [HttpDelete("invitations/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelInvitation(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling team invitation {InvitationId}", id);

            var userId = GetCurrentUserId();
            await _invitationService.CancelInvitationAsync(id, userId, cancellationToken);

            // Log audit entry
            await _auditLogService.LogActionAsync(
                CurrentTenantId,
                userId,
                "User",
                "TeamInvitation",
                id,
                "Cancel",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Team invitation {InvitationId} cancelled successfully", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling team invitation {InvitationId}", id);
            return StatusCode(500, new { error = "An error occurred while cancelling the invitation" });
        }
    }

    private static TeamInvitationResponse MapToResponse(TeamInvitation invitation)
    {
        return new TeamInvitationResponse
        {
            Id = invitation.Id,
            Email = invitation.Email,
            Role = invitation.Role,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = invitation.AcceptedAt,
            InvitedBy = invitation.InvitedBy,
            CreatedAt = invitation.CreatedAt
        };
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirst("sub")?.Value 
            ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
