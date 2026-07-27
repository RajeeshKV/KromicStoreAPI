// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of team invitation service.
/// </summary>
public class TeamInvitationService : ITeamInvitationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TeamInvitationService> _logger;

    public TeamInvitationService(AppDbContext context, ILogger<TeamInvitationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TeamInvitation> CreateInvitationAsync(
        Guid tenantId,
        string email,
        string role,
        Guid invitedBy,
        CancellationToken cancellationToken = default)
    {
        // Check if there's already a pending invitation for this email
        var existingInvitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.TenantId == tenantId 
                && i.Email == email 
                && i.Status == "Pending", 
                cancellationToken);

        if (existingInvitation != null)
        {
            // Cancel the existing invitation
            existingInvitation.Cancel();
        }

        var invitation = TeamInvitation.Create(tenantId, email, role, invitedBy);
        await _context.TeamInvitations.AddAsync(invitation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Team invitation created for tenant {TenantId}, email {Email}, role {Role}",
            tenantId, email, role);

        // In production, send email with invitation link containing the token
        // await _emailService.SendInvitationEmailAsync(email, invitation.Token, tenantId);

        return invitation;
    }

    public async Task<IEnumerable<TeamInvitation>> GetTenantInvitationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TeamInvitations
            .Where(i => i.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeamInvitation?> GetInvitationByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    public async Task AcceptInvitationAsync(
        Guid invitationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        if (invitation == null)
            throw new ArgumentException("Invitation not found", nameof(invitationId));

        invitation.Accept();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Team invitation {InvitationId} accepted by user {UserId}",
            invitationId, userId);

        // In production, assign the role to the user for the tenant
        // await _userService.AssignRoleToUserAsync(userId, invitation.TenantId, invitation.Role);
    }

    public async Task DeclineInvitationAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        if (invitation == null)
            throw new ArgumentException("Invitation not found", nameof(invitationId));

        invitation.Decline();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Team invitation {InvitationId} declined", invitationId);
    }

    public async Task CancelInvitationAsync(
        Guid invitationId,
        Guid cancelledBy,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        if (invitation == null)
            throw new ArgumentException("Invitation not found", nameof(invitationId));

        invitation.Cancel();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Team invitation {InvitationId} cancelled by user {UserId}",
            invitationId, cancelledBy);
    }

    public async Task CleanupExpiredInvitationsAsync(CancellationToken cancellationToken = default)
    {
        var expiredInvitations = await _context.TeamInvitations
            .Where(i => i.Status == "Pending" && i.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var invitation in expiredInvitations)
        {
            invitation.MarkAsExpired();
        }

        if (expiredInvitations.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} expired invitations", expiredInvitations.Count);
        }
    }
}
