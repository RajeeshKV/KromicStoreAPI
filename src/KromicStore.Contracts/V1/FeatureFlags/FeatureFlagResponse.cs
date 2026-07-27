// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.FeatureFlags;

/// <summary>
/// Response DTO for feature flag.
/// </summary>
public class FeatureFlagResponse
{
    /// <summary>
    /// Gets or sets the feature flag ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the feature flag key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feature flag description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the feature flag type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plan that this flag applies to.
    /// </summary>
    public string? Plan { get; set; }

    /// <summary>
    /// Gets or sets the date when the feature flag was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the feature flag was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
