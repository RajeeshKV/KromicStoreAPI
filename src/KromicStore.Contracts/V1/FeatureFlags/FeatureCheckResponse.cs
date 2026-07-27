// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Contracts.V1.FeatureFlags;

/// <summary>
/// Response DTO for feature check.
/// </summary>
public class FeatureCheckResponse
{
    /// <summary>
    /// Gets or sets the feature key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the feature is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
}
