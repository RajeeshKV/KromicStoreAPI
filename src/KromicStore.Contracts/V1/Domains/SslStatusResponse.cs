// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Domains;

/// <summary>
/// Response DTO for SSL status check.
/// </summary>
public class SslStatusResponse
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
    /// Gets or sets the SSL certificate status (Pending, Provisioning, Active, Failed, Expired).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SSL status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
