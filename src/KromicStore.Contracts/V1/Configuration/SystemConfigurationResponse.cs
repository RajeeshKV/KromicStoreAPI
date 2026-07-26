namespace KromicStore.Contracts.V1.Configuration;

/// <summary>
/// Response DTO for system-wide (platform) configuration values.
/// </summary>
public class SystemConfigurationResponse
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    public string ConfigValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the value is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Gets or sets the expiration date for temporary overrides.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets when the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the configuration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
