// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents a team invitation for a tenant.
/// </summary>
public class TeamInvitation : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the invited user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role to assign to the invited user.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the invitation token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

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
    /// Gets or sets the invitation status (Pending, Accepted, Declined, Expired, Cancelled).
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Factory method to create a new team invitation.
    /// </summary>
    public static TeamInvitation Create(
        Guid tenantId,
        string email,
        string role,
        Guid invitedBy,
        TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required.", nameof(role));

        var token = GenerateInvitationToken();
        var expiresAt = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromDays(7));

        return new TeamInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.ToLowerInvariant().Trim(),
            Role = role,
            Token = token,
            ExpiresAt = expiresAt,
            InvitedBy = invitedBy,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marks the invitation as accepted.
    /// </summary>
    public void Accept()
    {
        if (Status != "Pending")
            throw new InvalidOperationException($"Cannot accept invitation in status: {Status}");

        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidOperationException("Cannot accept expired invitation");

        Status = "Accepted";
        AcceptedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the invitation as declined.
    /// </summary>
    public void Decline()
    {
        if (Status != "Pending")
            throw new InvalidOperationException($"Cannot decline invitation in status: {Status}");

        Status = "Declined";
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the invitation.
    /// </summary>
    public void Cancel()
    {
        if (Status != "Pending")
            throw new InvalidOperationException($"Cannot cancel invitation in status: {Status}");

        Status = "Cancelled";
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the invitation as expired.
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status != "Pending")
            return;

        Status = "Expired";
        UpdateTimestamp();
    }

    /// <summary>
    /// Checks if the invitation is valid for acceptance.
    /// </summary>
    public bool IsValidForAcceptance()
    {
        return Status == "Pending" && DateTime.UtcNow <= ExpiresAt;
    }

    private static string GenerateInvitationToken()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}
