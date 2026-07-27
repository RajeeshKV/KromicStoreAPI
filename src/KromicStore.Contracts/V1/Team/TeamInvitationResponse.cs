// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Team;

/// <summary>
/// Response DTO for team invitation.
/// </summary>
public class TeamInvitationResponse
{
    /// <summary>
    /// Gets or sets the invitation ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the email address of the invited user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role to assign to the invited user.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invitation status (Pending, Accepted, Declined, Expired, Cancelled).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the invitation expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the invitation was accepted.
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who sent the invitation.
    /// </summary>
    public Guid InvitedBy { get; set; }

    /// <summary>
    /// Gets or sets the date when the invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
