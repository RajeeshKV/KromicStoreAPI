#nullable enable

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Request model for sending transactional emails via Brevo
/// </summary>
public class SendEmailRequest
{
    /// <summary>
    /// Recipient email address
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Recipient display name (optional)
    /// </summary>
    public string? ToName { get; set; }

    /// <summary>
    /// Email subject line
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Brevo template ID to use for rendering
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// Template variables to substitute in template
    /// </summary>
    public Dictionary<string, string>? TemplateParameters { get; set; }

    /// <summary>
    /// Custom headers to include in email (optional)
    /// </summary>
    public Dictionary<string, string>? CustomHeaders { get; set; }

    /// <summary>
    /// Tag for categorizing emails in Brevo (optional)
    /// </summary>
    public string? Tag { get; set; }
}

/// <summary>
/// Request model for sending SMS via Brevo
/// </summary>
public class SendSmsRequest
{
    /// <summary>
    /// Recipient phone number (E.164 format: +[country][number])
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Message content (max 160 characters for single SMS)
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata for tracking
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
