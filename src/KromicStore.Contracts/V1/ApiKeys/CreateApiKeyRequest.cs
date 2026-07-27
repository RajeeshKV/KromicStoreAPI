// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.ApiKeys;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating an API key.
/// </summary>
public class CreateApiKeyRequest
{
    /// <summary>
    /// Gets or sets the API key name/description.
    /// </summary>
    [Required(ErrorMessage = "API key name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "API key name must be between 1 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key scopes/permissions (comma-separated).
    /// </summary>
    [Required(ErrorMessage = "API key scopes are required.")]
    [StringLength(500, ErrorMessage = "Scopes cannot exceed 500 characters.")]
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional expiration date for the API key.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
