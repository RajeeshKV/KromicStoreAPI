#pragma warning disable CS8601, CS8625
#nullable enable

namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for configuration service providing runtime configuration management with caching and audit trail.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value by key with type conversion and caching.
    /// </summary>
    /// <typeparam name="T">The type to convert the configuration value to</typeparam>
    /// <param name="tenantId">The tenant ID (or null for platform-wide configuration)</param>
    /// <param name="key">The configuration key (e.g., "notifications:enabled")</param>
    /// <param name="defaultValue">Default value if configuration is not found</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The configuration value or default value</returns>
    Task<T> GetAsync<T>(Guid? tenantId, string key, T defaultValue = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a configuration value with automatic audit logging.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value</typeparam>
    /// <param name="tenantId">The tenant ID (or null for platform-wide configuration)</param>
    /// <param name="key">The configuration key</param>
    /// <param name="value">The configuration value</param>
    /// <param name="userId">The user ID performing the change</param>
    /// <param name="reason">Optional reason for the change</param>
    /// <param name="isEncrypted">Whether to encrypt the value at rest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(Guid? tenantId, string key, T value, Guid userId, string reason = null, bool isEncrypted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration values for a section (wildcard pattern matching).
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="sectionPrefix">Section prefix (e.g., "notifications:" matches all notifications.* configs)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of configuration key-value pairs matching the section</returns>
    Task<IDictionary<string, string>> GetSectionAsync(Guid? tenantId, string sectionPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached configuration entries.
    /// </summary>
    /// <param name="tenantId">The tenant ID (or null for all tenants)</param>
    /// <param name="keyPattern">Optional key pattern to invalidate specific configurations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateCacheAsync(Guid? tenantId, string keyPattern = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration audit log with filtering and pagination.
    /// </summary>
    /// <param name="tenantId">The tenant ID (or null for platform audit)</param>
    /// <param name="from">Start date for filtering</param>
    /// <param name="to">End date for filtering</param>
    /// <param name="key">Optional configuration key filter</param>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audit log entries with total count</returns>
    Task<(List<ConfigurationAuditLogDto> logs, int total)> GetAuditLogAsync(Guid? tenantId, DateTime? from = null, DateTime? to = null, string key = null, Guid? userId = null, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a configuration to its default or platform value.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="key">The configuration key</param>
    /// <param name="userId">The user ID performing the reset</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetAsync(Guid tenantId, string key, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports configuration audit log as CSV.
    /// </summary>
    /// <param name="tenantId">The tenant ID (or null for platform)</param>
    /// <param name="from">Start date for filtering</param>
    /// <param name="to">End date for filtering</param>
    /// <param name="configKey">Optional configuration key filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>CSV content as string</returns>
    Task<string> ExportAuditLogAsync(Guid? tenantId, DateTime? from = null, DateTime? to = null, string? configKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old audit log entries based on retention policy (365 days).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of deleted records</returns>
    Task<int> CleanupExpiredAuditLogsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for configuration audit log entry.
/// </summary>
public class ConfigurationAuditLogDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string ConfigurationKey { get; set; }
    public string OldValue { get; set; }
    public string NewValue { get; set; }
    public Guid? ChangedBy { get; set; }
    public string ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; }
}
