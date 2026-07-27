// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Application.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface for team invitation management.
/// </summary>
public interface ITeamInvitationService
{
    /// <summary>
    /// Creates and sends a team invitation.
    /// </summary>
    Task<Domain.Entities.TeamInvitation> CreateInvitationAsync(
        Guid tenantId,
        string email,
        string role,
        Guid invitedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all invitations for a tenant.
    /// </summary>
    Task<IEnumerable<Domain.Entities.TeamInvitation>> GetTenantInvitationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an invitation by token.
    /// </summary>
    Task<Domain.Entities.TeamInvitation?> GetInvitationByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a team invitation.
    /// </summary>
    Task AcceptInvitationAsync(
        Guid invitationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Declines a team invitation.
    /// </summary>
    Task DeclineInvitationAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a team invitation.
    /// </summary>
    Task CancelInvitationAsync(
        Guid invitationId,
        Guid cancelledBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired invitations.
    /// </summary>
    Task CleanupExpiredInvitationsAsync(CancellationToken cancellationToken = default);
}
