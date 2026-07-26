namespace KromicStore.Domain.Entities;

/// <summary>
/// Represents platform-wide configuration for super user (contact details, etc).
/// </summary>
public class SuperUserConfig : BaseEntity
{
    /// <summary>Gets the configuration key (unique identifier).</summary>
    public string ConfigKey { get; private set; } = string.Empty;

    /// <summary>Gets the configuration value.</summary>
    public string ConfigValue { get; private set; } = string.Empty;

    /// <summary>Gets the configuration description.</summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Creates a new instance of SuperUserConfig.
    /// </summary>
    public static SuperUserConfig Create(string configKey, string configValue, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(configKey))
            throw new ArgumentException("Config key is required.", nameof(configKey));
        if (string.IsNullOrWhiteSpace(configValue))
            throw new ArgumentException("Config value is required.", nameof(configValue));

        return new SuperUserConfig
        {
            ConfigKey = configKey,
            ConfigValue = configValue,
            Description = description
        };
    }

    /// <summary>
    /// Updates the configuration value.
    /// </summary>
    public void UpdateValue(string configValue)
    {
        if (string.IsNullOrWhiteSpace(configValue))
            throw new ArgumentException("Config value is required.", nameof(configValue));
        ConfigValue = configValue;
    }

    /// <summary>
    /// Updates the configuration description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}
