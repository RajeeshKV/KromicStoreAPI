// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents a feature flag for enabling/disabling features.
/// </summary>
public class FeatureFlag : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID (null for global/platform-wide flags).
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the feature flag key (e.g., "wishlist_enabled", "blog_enabled").
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
    /// Gets or sets the feature flag type (Global, Tenant, Plan).
    /// </summary>
    public string Type { get; set; } = "Tenant";

    /// <summary>
    /// Gets or sets the plan that this flag applies to (if Type is Plan).
    /// </summary>
    public string? Plan { get; set; }

    /// <summary>
    /// Factory method to create a new feature flag.
    /// </summary>
    public static FeatureFlag Create(
        Guid? tenantId,
        string key,
        bool isEnabled,
        string? description = null,
        string type = "Tenant",
        string? plan = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Feature flag key is required.", nameof(key));

        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = key.ToLowerInvariant().Trim(),
            Description = description,
            IsEnabled = isEnabled,
            Type = type,
            Plan = plan,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Enables the feature flag.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Disables the feature flag.
    /// </summary>
    public void Disable()
    {
        IsEnabled = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Toggles the feature flag.
    /// </summary>
    public void Toggle()
    {
        IsEnabled = !IsEnabled;
        UpdateTimestamp();
    }
}
