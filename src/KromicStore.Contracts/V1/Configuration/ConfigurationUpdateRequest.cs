namespace KromicStore.Contracts.V1.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request DTO for updating a configuration value.
/// </summary>
public class ConfigurationUpdateRequest
{
    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    [Required(ErrorMessage = "Configuration value is required")]
    [MaxLength(5000, ErrorMessage = "Configuration value cannot exceed 5000 characters")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the value should be encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// Gets or sets the optional reason for the change.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the optional expiration date for temporary overrides.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
