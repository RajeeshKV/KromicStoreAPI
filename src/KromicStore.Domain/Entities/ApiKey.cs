// Copyright (c) 2024 KromicStore. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents an API key for tenant integrations.
/// </summary>
public class ApiKey : BaseEntity
{
    /// <summary>
    /// Gets or sets the tenant ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the API key name/description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed API key value.
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last 4 characters of the key for display purposes.
    /// </summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key scopes/permissions.
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date when the key expires (null for no expiration).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date when the key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the key.
    /// </summary>
    public new Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets whether the key is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Factory method to create a new API key.
    /// </summary>
    public static ApiKey Create(
        Guid tenantId,
        string name,
        string scopes,
        Guid createdBy,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("API key name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(scopes))
            throw new ArgumentException("API key scopes are required.", nameof(scopes));

        var (key, keyHash, keyPrefix) = GenerateApiKey();

        return new ApiKey
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Scopes = scopes,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Revokes the API key.
    /// </summary>
    public void Revoke()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the last used timestamp.
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    /// <summary>
    /// Checks if the key is expired.
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    /// <summary>
    /// Checks if the key is valid for use.
    /// </summary>
    public bool IsValid()
    {
        return IsActive && !IsExpired();
    }

    /// <summary>
    /// Generates a new API key.
    /// </summary>
    public static (string key, string keyHash, string keyPrefix) GenerateApiKey()
    {
        var key = $"kromic_{Guid.NewGuid():N}";
        var keyHash = Hash(key);
        var keyPrefix = key[^4..]; // Last 4 characters
        return (key, keyHash, keyPrefix);
    }

    /// <summary>
    /// Hashes an API key.
    /// </summary>
    public static string Hash(string key)
    {
        // In production, use proper hashing (e.g., SHA256)
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
