#nullable enable

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Response model from Brevo send operations
/// </summary>
public class BrevoSendResponse
{
    /// <summary>
    /// Unique message ID from Brevo for tracking
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Code from Brevo (typically "success" or error code)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Optional message from Brevo
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Response model for delivery status tracking
/// </summary>
public class DeliveryStatusResponse
{
    /// <summary>
    /// Current delivery status: sent, delivered, bounced, opened, clicked, complaint, etc.
    /// </summary>
    public string Status { get; set; } = "unknown";

    /// <summary>
    /// Brevo message ID for tracking
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Recipient email address
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of last event
    /// </summary>
    public DateTime? LastEventAt { get; set; }

    /// <summary>
    /// Bounce type if bounced: hard, soft, complaint
    /// </summary>
    public string? BounceType { get; set; }

    /// <summary>
    /// Bounce reason if available
    /// </summary>
    public string? BounceReason { get; set; }

    /// <summary>
    /// Number of times opened (if tracking enabled)
    /// </summary>
    public int OpenCount { get; set; }

    /// <summary>
    /// Number of links clicked (if tracking enabled)
    /// </summary>
    public int ClickCount { get; set; }

    /// <summary>
    /// Whether recipient is on unsubscribe list
    /// </summary>
    public bool IsUnsubscribed { get; set; }

    /// <summary>
    /// Whether recipient is on complaint list
    /// </summary>
    public bool IsComplaint { get; set; }

    /// <summary>
    /// Whether recipient is blocked/bounced
    /// </summary>
    public bool IsBlocked { get; set; }
}
