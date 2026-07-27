// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Domains;

/// <summary>
/// Response DTO for SSL provisioning.
/// </summary>
public class SslProvisioningResponse
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
    /// Gets or sets the SSL provisioning status (Pending, Provisioning, Active, Failed).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provisioning message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
