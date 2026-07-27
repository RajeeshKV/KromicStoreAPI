// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.FeatureFlags;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for creating a feature flag.
/// </summary>
public class CreateFeatureFlagRequest
{
    /// <summary>
    /// Gets or sets the feature flag key (e.g., "wishlist_enabled").
    /// </summary>
    [Required(ErrorMessage = "Feature flag key is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Feature flag key must be between 1 and 100 characters.")]
    [RegularExpression("^[a-z_][a-z0-9_]*$", ErrorMessage = "Feature flag key must contain only lowercase letters, numbers, and underscores, and must start with a letter or underscore.")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the feature flag description.
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the feature flag type (Global, Tenant, Plan).
    /// </summary>
    [Required(ErrorMessage = "Feature flag type is required.")]
    [RegularExpression("^(Global|Tenant|Plan)$", ErrorMessage = "Feature flag type must be Global, Tenant, or Plan.")]
    public string Type { get; set; } = "Tenant";

    /// <summary>
    /// Gets or sets the plan that this flag applies to (if Type is Plan).
    /// </summary>
    [StringLength(50, ErrorMessage = "Plan cannot exceed 50 characters.")]
    public string? Plan { get; set; }
}
