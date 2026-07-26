namespace KromicStore.API.Configuration;

/// <summary>
/// Extension methods for loading and validating configuration from environment variables and appsettings.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds configuration loading and validation to the service collection.
    /// Loads configuration from environment variables with fallback to appsettings.json.
    /// Validates all required configuration is present on startup.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder</param>
    /// <param name="environment">The hosting environment</param>
    public static void ConfigureApplicationSettings(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;

        // Clear existing providers to ensure proper precedence
        builder.Configuration.Sources.Clear();

        // Add configuration sources in order of precedence (highest to lowest)
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddEnvironmentVariables("KromicStore_");

        // Bind to AppSettings class for strongly-typed configuration
        builder.Services.Configure<AppSettings>(builder.Configuration);

        // Validate required configuration values
        ValidateRequiredConfiguration(builder.Configuration, environment);
    }

    /// <summary>
    /// Validates that all required environment variables are present and have valid values.
    /// Throws InvalidOperationException if validation fails.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="environment">The hosting environment name</param>
    private static void ValidateRequiredConfiguration(IConfiguration configuration, string environment)
    {
        var missingVars = new List<string>();
        var invalidVars = new List<string>();

        // Required variables that must be set
        var requiredVars = new[] 
        { 
            "DATABASE_URL",
            "JWT_SECRET",
            "SECURITY_ENCRYPTION_KEY",
            "RAZORPAY_KEY_ID",
            "RAZORPAY_KEY_SECRET",
            "GOOGLE_CLIENT_ID",
            "GOOGLE_CLIENT_SECRET",
            "CLOUDINARY_CLOUD_NAME",
            "CLOUDINARY_API_KEY",
            "CLOUDINARY_API_SECRET",
            "BREVO_API_KEY",
            "REDIS_URL"
        };

        foreach (var envVar in requiredVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrWhiteSpace(value))
            {
                missingVars.Add(envVar);
            }
        }

        // Validate specific formats and values
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JWT_SECRET")))
        {
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (jwtSecret?.Length < 32)
            {
                invalidVars.Add($"JWT_SECRET (must be at least 32 characters, current: {jwtSecret?.Length ?? 0})");
            }
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SECURITY_ENCRYPTION_KEY")))
        {
            var encKey = Environment.GetEnvironmentVariable("SECURITY_ENCRYPTION_KEY");
            if (encKey?.Length < 32)
            {
                invalidVars.Add($"SECURITY_ENCRYPTION_KEY (must be at least 32 characters, current: {encKey?.Length ?? 0})");
            }
        }

        // Validate integer configurations
        var intConfigs = new[]
        {
            ("JWT_EXPIRATION_MINUTES", 1, 10080), // 1 minute to 7 days
            ("REFRESH_TOKEN_EXPIRATION_DAYS", 1, 365), // 1 to 365 days
            ("PASSWORD_MIN_LENGTH", 6, 128),
            ("DB_CONNECTION_POOL_MIN", 1, 100),
            ("DB_CONNECTION_POOL_MAX", 5, 1000),
            ("CACHE_TTL_PRODUCTS_MINUTES", 1, 1440), // 1 minute to 24 hours
            ("RAZORPAY_TIMEOUT_SECONDS", 5, 300), // 5 to 300 seconds
        };

        foreach (var (key, min, max) in intConfigs)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (int.TryParse(value, out var intValue))
                {
                    if (intValue < min || intValue > max)
                    {
                        invalidVars.Add($"{key} (must be between {min} and {max}, current: {intValue})");
                    }
                }
                else
                {
                    invalidVars.Add($"{key} (must be a valid integer, current: {value})");
                }
            }
        }

        // Build error message if validation failed
        if (missingVars.Count > 0 || invalidVars.Count > 0)
        {
            var errorMessage = "Configuration validation failed:\n";

            if (missingVars.Count > 0)
            {
                errorMessage += $"\nMissing required environment variables:\n  - {string.Join("\n  - ", missingVars)}";
            }

            if (invalidVars.Count > 0)
            {
                errorMessage += $"\nInvalid configuration values:\n  - {string.Join("\n  - ", invalidVars)}";
            }

            errorMessage += $"\n\nPlease set the required environment variables and restart the application.";
            errorMessage += $"\nFor detailed documentation, see: docs/Environment-Setup.md";

            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Gets a required configuration value or throws if not found.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="key">The configuration key</param>
    /// <returns>The configuration value</returns>
    public static string GetRequiredConfigValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required configuration '{key}' is not set");
        }
        return value;
    }

    /// <summary>
    /// Gets a configuration value with a default fallback.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found</param>
    /// <returns>The configuration value or default</returns>
    public static string GetConfigValueOrDefault(this IConfiguration configuration, string key, string defaultValue)
    {
        return configuration[key] ?? defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as boolean with a default fallback.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found or invalid</param>
    /// <returns>The configuration value as boolean or default</returns>
    public static bool GetBoolConfigValueOrDefault(this IConfiguration configuration, string key, bool defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value.ToLower() switch
        {
            "true" or "1" or "yes" or "on" => true,
            "false" or "0" or "no" or "off" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// Gets a configuration value as integer with a default fallback.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found or invalid</param>
    /// <returns>The configuration value as integer or default</returns>
    public static int GetIntConfigValueOrDefault(this IConfiguration configuration, string key, int defaultValue)
    {
        var value = configuration[key];
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as decimal with a default fallback.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">The default value if not found or invalid</param>
    /// <returns>The configuration value as decimal or default</returns>
    public static decimal GetDecimalConfigValueOrDefault(this IConfiguration configuration, string key, decimal defaultValue)
    {
        var value = configuration[key];
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Validates a configuration URL is valid URI format.
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <returns>True if valid URI, false otherwise</returns>
    public static bool IsValidUri(this string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    /// <summary>
    /// Masks sensitive parts of a connection string for logging.
    /// </summary>
    /// <param name="connectionString">The connection string to mask</param>
    /// <returns>The masked connection string</returns>
    public static string MaskConnectionString(this string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "[empty]";

        // Mask password in PostgreSQL connection strings
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password=)[^;]*",
            "$1***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
