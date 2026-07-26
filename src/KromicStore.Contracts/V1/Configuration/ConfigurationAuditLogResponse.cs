namespace KromicStore.Contracts.V1.Configuration;

/// <summary>
/// Response DTO for configuration audit log entries.
/// </summary>
public class ConfigurationAuditLogResponse
{
    /// <summary>
    /// Gets or sets the audit log ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the configuration key that was changed.
    /// </summary>
    public string ConfigurationKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the old value (or summary for sensitive values).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value (or summary for sensitive values).
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who made the change.
    /// </summary>
    public Guid ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets the name of the user who made the change.
    /// </summary>
    public string? ChangedByName { get; set; }

    /// <summary>
    /// Gets or sets when the change was made.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for the change.
    /// </summary>
    public string? Reason { get; set; }
}
