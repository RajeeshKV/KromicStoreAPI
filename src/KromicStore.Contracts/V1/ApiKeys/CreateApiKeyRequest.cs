// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.ApiKeys;

/// <summary>
/// Request DTO for creating an API key.
/// </summary>
public class CreateApiKeyRequest
{
    /// <summary>
    /// Gets or sets the API key name/description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key scopes/permissions (comma-separated).
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional expiration date for the API key.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
