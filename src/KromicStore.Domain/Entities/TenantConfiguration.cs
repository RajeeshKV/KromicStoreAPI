// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;
using KromicStore.Domain.Enums;

/// <summary>
/// Represents a configuration setting for a tenant or the platform.
/// </summary>
public class TenantConfiguration : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID (null for platform-wide configurations).
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the configuration key (e.g., "notifications:email_enabled").
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value as a JSON string.
    /// </summary>
    public string ConfigValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration scope (Platform or Tenant).
    /// </summary>
    public ConfigScope Scope { get; set; } = ConfigScope.Tenant;

    /// <summary>
    /// Gets or sets a value indicating whether the value is encrypted at rest.
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Gets or sets the expiration date for temporary configuration overrides.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Factory method to create a new TenantConfiguration.
    /// </summary>
    /// <param name="tenantId">The tenant ID (null for platform-wide configurations).</param>
    /// <param name="configKey">The configuration key.</param>
    /// <param name="configValue">The configuration value as JSON.</param>
    /// <param name="scope">The configuration scope.</param>
    /// <param name="isEncrypted">Whether the value should be encrypted.</param>
    /// <param name="expiresAt">Optional expiration date for temporary overrides.</param>
    /// <returns>A new TenantConfiguration instance.</returns>
    public static TenantConfiguration Create(
        Guid? tenantId,
        string configKey,
        string configValue,
        ConfigScope scope = ConfigScope.Tenant,
        bool isEncrypted = false,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(configKey))
        {
            throw new ArgumentException("Configuration key cannot be empty.", nameof(configKey));
        }

        if (string.IsNullOrWhiteSpace(configValue))
        {
            throw new ArgumentException("Configuration value cannot be empty.", nameof(configValue));
        }

        return new TenantConfiguration
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConfigKey = configKey.Trim(),
            ConfigValue = configValue,
            Scope = scope,
            IsEncrypted = isEncrypted,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Updates the configuration value.
    /// </summary>
    /// <param name="newValue">The new configuration value.</param>
    /// <param name="isEncrypted">Whether the new value should be encrypted.</param>
    /// <param name="expiresAt">Optional expiration date for the override.</param>
    public void Update(string newValue, bool isEncrypted = false, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(newValue))
        {
            throw new ArgumentException("Configuration value cannot be empty.", nameof(newValue));
        }

        ConfigValue = newValue;
        IsEncrypted = isEncrypted;
        ExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this configuration has expired.
    /// </summary>
    /// <returns>True if the configuration has expired; otherwise, false.</returns>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && ExpiresAt <= DateTime.UtcNow;
    }
}
