// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Domains;

/// <summary>
/// Response DTO for domain verification.
/// </summary>
public class DomainVerificationResponse
{
    /// <summary>
    /// Gets or sets the domain ID.
    /// </summary>
    public Guid DomainId { get; set; }

    /// <summary>
    /// Gets or sets the domain name.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the domain is verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the date when the domain was verified.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the verification message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
