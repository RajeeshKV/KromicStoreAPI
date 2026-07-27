// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.Domains;

/// <summary>
/// Response DTO for DNS configuration check.
/// </summary>
public class DnsCheckResponse
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
    /// Gets or sets whether the DNS is correctly configured.
    /// </summary>
    public bool IsConfigured { get; set; }

    /// <summary>
    /// Gets or sets the DNS check message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
