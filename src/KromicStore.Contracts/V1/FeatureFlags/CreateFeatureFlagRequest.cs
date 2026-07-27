// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.FeatureFlags;

/// <summary>
/// Request DTO for creating a feature flag.
/// </summary>
public class CreateFeatureFlagRequest
{
    /// <summary>
    /// Gets or sets the feature flag key (e.g., "wishlist_enabled").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the feature flag description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the feature flag type (Global, Tenant, Plan).
    /// </summary>
    public string Type { get; set; } = "Tenant";

    /// <summary>
    /// Gets or sets the plan that this flag applies to (if Type is Plan).
    /// </summary>
    public string? Plan { get; set; }
}
