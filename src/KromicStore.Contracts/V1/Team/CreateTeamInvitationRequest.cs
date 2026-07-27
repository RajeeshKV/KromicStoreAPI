// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Team;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a team invitation.
/// </summary>
public class CreateTeamInvitationRequest
{
    /// <summary>
    /// Gets or sets the email address of the user to invite.
    /// </summary>
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role to assign to the invited user.
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    [StringLength(50, ErrorMessage = "Role cannot exceed 50 characters.")]
    public string Role { get; set; } = string.Empty;
}
