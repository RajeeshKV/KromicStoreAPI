// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.ApiKeys;

/// <summary>
/// Response DTO for creating an API key (includes plain key shown once).
/// </summary>
public class CreateApiKeyResponse
{
    /// <summary>
    /// Gets or sets the API key ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the API key name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plain API key (only shown once).
    /// </summary>
    public string PlainKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last 4 characters of the key for display.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key scopes.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
