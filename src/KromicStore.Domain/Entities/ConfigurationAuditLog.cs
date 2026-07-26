// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents an audit log entry for configuration changes.
/// </summary>
public class ConfigurationAuditLog : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the configuration key that was changed.
    /// </summary>
    public string ConfigurationKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the old configuration value before the change.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new configuration value after the change.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who made the change.
    /// </summary>
    public Guid ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the change was made.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for the configuration change.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who made the change (computed field, not persisted).
    /// </summary>
    public string? ChangedByName { get; set; }

    /// <summary>
    /// Factory method to create a new ConfigurationAuditLog.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="configurationKey">The configuration key that was changed.</param>
    /// <param name="oldValue">The old value.</param>
    /// <param name="newValue">The new value.</param>
    /// <param name="changedBy">The ID of the user who made the change.</param>
    /// <param name="reason">Optional reason for the change.</param>
    /// <returns>A new ConfigurationAuditLog instance.</returns>
    public static ConfigurationAuditLog Create(
        Guid tenantId,
        string configurationKey,
        string? oldValue,
        string? newValue,
        Guid changedBy,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(configurationKey))
        {
            throw new ArgumentException("Configuration key cannot be empty.", nameof(configurationKey));
        }

        return new ConfigurationAuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConfigurationKey = configurationKey.Trim(),
            OldValue = oldValue,
            NewValue = newValue,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
