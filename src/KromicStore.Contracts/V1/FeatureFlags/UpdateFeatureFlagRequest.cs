// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.FeatureFlags;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating a feature flag.
/// </summary>
public class UpdateFeatureFlagRequest
{
    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the feature flag description.
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}
