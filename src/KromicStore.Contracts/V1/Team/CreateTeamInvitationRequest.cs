// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Team;

/// <summary>
/// Request DTO for creating a team invitation.
/// </summary>
public class CreateTeamInvitationRequest
{
    /// <summary>
    /// Gets or sets the email address of the user to invite.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role to assign to the invited user.
    /// </summary>
    public string Role { get; set; } = string.Empty;
}
