#pragma warning disable CS8601, CS8625, CS8603, CS8604
#nullable enable

namespace KromicStore.Infrastructure.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KromicStore.Application.Interfaces;
using KromicStore.Domain.Entities;
using KromicStore.Domain.Enums;

/// <summary>
/// Implementation of configuration service for runtime configuration management with caching and audit trail.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IEncryptionService _encryptionService;
    private const string CacheKeyPrefix = "config";
    private const int CacheTTLMinutes = 30; // Default cache TTL for 30 minutes

    public ConfigurationService(
        ILogger<ConfigurationService> logger,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IEncryptionService encryptionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <summary>
    /// Gets a configuration value by key with type conversion and caching.
    /// </summary>
    public async Task<T> GetAsync<T>(Guid? tenantId, string key, T defaultValue = default, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be empty", nameof(key));

        var cacheKey = BuildCacheKey(tenantId, key);

        _logger.LogDebug("Getting configuration {Key} for tenant {TenantId}", key, tenantId ?? Guid.Empty);

        // Try to get from cache first
        try
        {
            var cachedValue = await _cacheService.GetAsync<string>(cacheKey, cancellationToken);
            if (cachedValue != null)
            {
                _logger.LogDebug("Configuration {Key} found in cache", key);
                return DeserializeValue<T>(cachedValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving configuration {Key} from cache", key);
        }

        // Fetch from database
        try
        {
            var config = await _unitOfWork.TenantConfigurations.FindAsync(
                c => c.ConfigKey == key && c.TenantId == tenantId,
                cancellationToken);

            var configEntity = config.FirstOrDefault();

            // If not found for tenant, try platform-wide configuration (if tenantId is not null)
            if (configEntity == null && tenantId.HasValue)
            {
                var platformConfig = await _unitOfWork.TenantConfigurations.FindAsync(
                    c => c.ConfigKey == key && c.TenantId == null && c.Scope == ConfigScope.Platform,
                    cancellationToken);

                configEntity = platformConfig.FirstOrDefault();
            }

            // Check if configuration exists and hasn't expired
            if (configEntity != null && !configEntity.IsExpired())
            {
                var value = configEntity.ConfigValue;

                // Decrypt if needed
                if (configEntity.IsEncrypted)
                {
                    try
                    {
                        value = await _encryptionService.DecryptAsync(value, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decrypting configuration {Key}", key);
                        return defaultValue;
                    }
                }

                // Cache the value
                try
                {
                    await _cacheService.SetAsync(cacheKey, value, TimeSpan.FromMinutes(CacheTTLMinutes), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error caching configuration {Key}", key);
                }

                _logger.LogDebug("Configuration {Key} retrieved from database", key);
                return DeserializeValue<T>(value);
            }

            // If expired, delete and return default
            if (configEntity?.IsExpired() == true)
            {
                _unitOfWork.TenantConfigurations.Delete(configEntity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Expired configuration {Key} deleted", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {Key} from database", key);
        }

        _logger.LogDebug("Configuration {Key} not found, returning default value", key);
        return defaultValue;
    }

    /// <summary>
    /// Gets all configuration values for a tenant.
    /// </summary>
    public async Task<IDictionary<string, string>> GetAllAsync(Guid? tenantId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all configurations for tenant {TenantId}", tenantId ?? Guid.Empty);

        try
        {
            // Query for all configs for the tenant
            var configs = await _unitOfWork.TenantConfigurations.FindAsync(
                c => c.TenantId == tenantId,
                cancellationToken);

            var result = new Dictionary<string, string>();

            foreach (var config in configs.Where(c => !c.IsExpired()))
            {
                var value = config.ConfigValue;

                // Decrypt if needed
                if (config.IsEncrypted)
                {
                    try
                    {
                        value = await _encryptionService.DecryptAsync(value, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decrypting configuration {Key}", config.ConfigKey);
                        continue;
                    }
                }

                result[config.ConfigKey] = value;
            }

            _logger.LogDebug("Retrieved {Count} configurations for tenant {TenantId}", result.Count, tenantId ?? Guid.Empty);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all configurations for tenant {TenantId}", tenantId ?? Guid.Empty);
            throw;
        }
    }

    /// <summary>
    /// Sets a configuration value with automatic audit logging.
    /// </summary>
    public async Task SetAsync<T>(Guid? tenantId, string key, T value, Guid userId, string reason = null, bool isEncrypted = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be empty", nameof(key));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        _logger.LogInformation("Setting configuration {Key} for tenant {TenantId}", key, tenantId ?? Guid.Empty);

        try
        {
            // Get old value for audit trail
            var oldValue = await GetAsync<T>(tenantId, key, cancellationToken: cancellationToken);
            var oldValueSerialized = SerializeValue(oldValue);

            // Serialize new value
            var serializedValue = SerializeValue(value);

            // Encrypt if needed
            if (isEncrypted)
            {
                serializedValue = await _encryptionService.EncryptAsync(serializedValue, cancellationToken);
            }

            // Find existing configuration
            var configs = await _unitOfWork.TenantConfigurations.FindAsync(
                c => c.ConfigKey == key && c.TenantId == tenantId,
                cancellationToken);

            var configEntity = configs.FirstOrDefault();

            if (configEntity != null)
            {
                // Update existing
                configEntity.Update(serializedValue, isEncrypted);
                _unitOfWork.TenantConfigurations.Update(configEntity);
            }
            else
            {
                // Create new
                configEntity = TenantConfiguration.Create(
                    tenantId,
                    key,
                    serializedValue,
                    tenantId.HasValue ? ConfigScope.Tenant : ConfigScope.Platform,
                    isEncrypted);

                await _unitOfWork.TenantConfigurations.AddAsync(configEntity, cancellationToken);
            }

            // Create audit log only for tenant-specific configs
            if (tenantId.HasValue)
            {
                var auditLog = ConfigurationAuditLog.Create(
                    tenantId.Value,
                    key,
                    oldValueSerialized,
                    serializedValue,
                    userId,
                    reason ?? "Configuration updated");

                await _unitOfWork.ConfigurationAuditLogs.AddAsync(auditLog, cancellationToken);
            }

            // Save changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            var cacheKey = BuildCacheKey(tenantId, key);
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            _logger.LogInformation("Configuration {Key} set successfully for tenant {TenantId}", key, tenantId ?? Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting configuration {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Gets all configuration values for a section (wildcard pattern matching).
    /// </summary>
    public async Task<IDictionary<string, string>> GetSectionAsync(Guid? tenantId, string sectionPrefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sectionPrefix))
            throw new ArgumentException("Section prefix cannot be empty", nameof(sectionPrefix));

        _logger.LogDebug("Getting configuration section {Prefix} for tenant {TenantId}", sectionPrefix, tenantId ?? Guid.Empty);

        try
        {
            // Query for configs matching the prefix pattern
            var configs = await _unitOfWork.TenantConfigurations.FindAsync(
                c => c.ConfigKey.StartsWith(sectionPrefix) && c.TenantId == tenantId,
                cancellationToken);

            var result = new Dictionary<string, string>();

            foreach (var config in configs.Where(c => !c.IsExpired()))
            {
                var value = config.ConfigValue;

                // Decrypt if needed
                if (config.IsEncrypted)
                {
                    try
                    {
                        value = await _encryptionService.DecryptAsync(value, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decrypting configuration {Key}", config.ConfigKey);
                        continue;
                    }
                }

                result[config.ConfigKey] = value;
            }

            _logger.LogDebug("Retrieved {Count} configuration entries for section {Prefix}", result.Count, sectionPrefix);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration section {Prefix}", sectionPrefix);
            throw;
        }
    }

    /// <summary>
    /// Invalidates cached configuration entries.
    /// </summary>
    public async Task InvalidateCacheAsync(Guid? tenantId, string keyPattern = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Invalidating configuration cache for tenant {TenantId}, pattern {KeyPattern}",
            tenantId ?? Guid.Empty, keyPattern ?? "*");

        try
        {
            if (string.IsNullOrWhiteSpace(keyPattern))
            {
                // Invalidate all configs for tenant
                var pattern = tenantId.HasValue ? $"{CacheKeyPrefix}:{tenantId}:*" : $"{CacheKeyPrefix}:platform:*";
                await _cacheService.ClearByPatternAsync(pattern, cancellationToken);
            }
            else
            {
                // Invalidate specific pattern
                var pattern = tenantId.HasValue
                    ? $"{CacheKeyPrefix}:{tenantId}:{keyPattern}*"
                    : $"{CacheKeyPrefix}:platform:{keyPattern}*";
                await _cacheService.ClearByPatternAsync(pattern, cancellationToken);
            }

            _logger.LogInformation("Configuration cache invalidated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating configuration cache");
            // Don't throw - cache invalidation failure shouldn't break functionality
        }
    }

    /// <summary>
    /// Gets configuration audit log with filtering and pagination.
    /// </summary>
    public async Task<(List<ConfigurationAuditLogDto> logs, int total)> GetAuditLogAsync(
        Guid? tenantId,
        DateTime? from = null,
        DateTime? to = null,
        string key = null,
        Guid? userId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Querying configuration audit log for tenant {TenantId}, key {Key}, user {UserId}",
            tenantId ?? Guid.Empty, key, userId ?? Guid.Empty);

        try
        {
            // Build query
            var query = await _unitOfWork.ConfigurationAuditLogs.FindAsync(
                log =>
                    (tenantId == null || log.TenantId == tenantId) &&
                    (string.IsNullOrEmpty(key) || log.ConfigurationKey == key) &&
                    (userId == null || log.ChangedBy == userId) &&
                    (from == null || log.ChangedAt >= from) &&
                    (to == null || log.ChangedAt <= to),
                cancellationToken);

            var total = query.Count;

            // Apply pagination and sorting
            var results = query
                .OrderByDescending(l => l.ChangedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            var dtos = results.Select(log => MapToDto(log)).ToList();

            _logger.LogDebug("Retrieved {Count} audit log entries, total {Total}", dtos.Count, total);

            return (dtos, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying configuration audit log");
            throw;
        }
    }

    /// <summary>
    /// Resets a configuration to its default or platform value.
    /// </summary>
    public async Task ResetAsync(Guid tenantId, string key, Guid userId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key cannot be empty", nameof(key));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        _logger.LogInformation("Resetting configuration {Key} for tenant {TenantId}", key, tenantId);

        try
        {
            // Find tenant-specific configuration
            var configs = await _unitOfWork.TenantConfigurations.FindAsync(
                c => c.ConfigKey == key && c.TenantId == tenantId,
                cancellationToken);

            var configEntity = configs.FirstOrDefault();

            if (configEntity != null)
            {
                // Get old value for audit
                var oldValue = configEntity.ConfigValue;

                // Create audit log for reset
                var auditLog = ConfigurationAuditLog.Create(
                    tenantId,
                    key,
                    oldValue,
                    null, // No new value for reset
                    userId,
                    "Configuration reset to default");

                await _unitOfWork.ConfigurationAuditLogs.AddAsync(auditLog, cancellationToken);

                // Delete tenant config
                _unitOfWork.TenantConfigurations.Delete(configEntity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Invalidate cache
                var cacheKey = BuildCacheKey(tenantId, key);
                await _cacheService.RemoveAsync(cacheKey, cancellationToken);

                _logger.LogInformation("Configuration {Key} reset successfully for tenant {TenantId}", key, tenantId);
            }
            else
            {
                _logger.LogWarning("Configuration {Key} not found for tenant {TenantId}", key, tenantId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting configuration {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Builds a cache key for a configuration value.
    /// </summary>
    private string BuildCacheKey(Guid? tenantId, string key)
    {
        if (tenantId.HasValue && tenantId != Guid.Empty)
        {
            return $"{CacheKeyPrefix}:{tenantId}:{key}";
        }

        return $"{CacheKeyPrefix}:platform:{key}";
    }

    /// <summary>
    /// Serializes a value to JSON string.
    /// </summary>
    private string SerializeValue<T>(T value)
    {
        if (value == null)
            return null;

        if (typeof(T) == typeof(string))
            return value.ToString();

        return JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    private T DeserializeValue<T>(string value)
    {
        if (value == null)
            return default;

        if (typeof(T) == typeof(string))
            return (T)(object)value;

        try
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing configuration value");
            return default;
        }
    }

    /// <summary>
    /// Maps ConfigurationAuditLog entity to DTO with masked sensitive values.
    /// </summary>
    private ConfigurationAuditLogDto MapToDto(ConfigurationAuditLog log)
    {
        return new ConfigurationAuditLogDto
        {
            Id = log.Id,
            TenantId = log.TenantId,
            ConfigurationKey = log.ConfigurationKey,
            OldValue = MaskSensitiveValue(log.ConfigurationKey, log.OldValue),
            NewValue = MaskSensitiveValue(log.ConfigurationKey, log.NewValue),
            ChangedBy = log.ChangedBy,
            ChangedByName = log.ChangedByName,
            ChangedAt = log.ChangedAt,
            Reason = log.Reason
        };
    }

    /// <summary>
    /// Masks sensitive values in audit logs.
    /// </summary>
    private string MaskSensitiveValue(string key, string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Check if key contains sensitive indicators
        var lowerKey = key.ToLowerInvariant();
        if (lowerKey.Contains("password") ||
            lowerKey.Contains("token") ||
            lowerKey.Contains("secret") ||
            lowerKey.Contains("key") ||
            lowerKey.Contains("credential") ||
            lowerKey.Contains("api_key"))
        {
            return "***MASKED***";
        }

        return value;
    }

    /// <summary>
    /// Exports configuration audit log as CSV.
    /// </summary>
    public async Task<string> ExportAuditLogAsync(
        Guid? tenantId,
        DateTime? from = null,
        DateTime? to = null,
        string? configKey = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting audit log for tenant {TenantId}", tenantId ?? Guid.Empty);

        try
        {
            var (auditLogs, _) = await GetAuditLogAsync(
                tenantId,
                from,
                to,
                configKey,
                null,
                0,
                10000, // Get all available records
                cancellationToken);

            // Build CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("\"ID\",\"Tenant ID\",\"Configuration Key\",\"Old Value\",\"New Value\",\"Changed By\",\"Changed By Name\",\"Changed At\",\"Reason\"");

            foreach (var log in auditLogs)
            {
                var line = $"\"{log.Id}\",\"{log.TenantId}\",\"{EscapeCsv(log.ConfigurationKey)}\",\"{EscapeCsv(log.OldValue)}\",\"{EscapeCsv(log.NewValue)}\",\"{log.ChangedBy}\",\"{EscapeCsv(log.ChangedByName)}\",\"{log.ChangedAt:O}\",\"{EscapeCsv(log.Reason)}\"";
                csv.AppendLine(line);
            }

            _logger.LogInformation("Audit log exported with {Count} entries", auditLogs.Count);
            return csv.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit log");
            throw;
        }
    }

    /// <summary>
    /// Cleans up old audit log entries based on retention policy (365 days).
    /// </summary>
    public async Task<int> CleanupExpiredAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cleanup of expired audit logs (> 365 days)");

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-365);

            // Find and delete old audit logs
            var oldLogs = await _unitOfWork.ConfigurationAuditLogs.FindAsync(
                log => log.ChangedAt < cutoffDate,
                cancellationToken);

            var count = oldLogs.Count;

            foreach (var log in oldLogs)
            {
                _unitOfWork.ConfigurationAuditLogs.Delete(log);
            }

            if (count > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deleted {Count} expired audit log entries", count);
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired audit logs");
            throw;
        }
    }

    /// <summary>
    /// Escapes values for CSV export.
    /// </summary>
    private string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Replace quotes with double quotes and handle newlines
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
    }
}

