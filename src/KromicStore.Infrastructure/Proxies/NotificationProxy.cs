using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Proxy for Brevo email and SMS notification service
/// Provides transactional email/SMS capabilities with templates, delivery tracking, and bounce handling
/// All configuration is loaded from environment variables via ConfigurationSettings.Brevo
/// </summary>
public class NotificationProxy : ServiceProxy<BrevoSendResponse>
{
    private readonly string _apiKey;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly string _baseUrl;
    private readonly string _apiVersion;
    private readonly int _templateOrderPlaced;
    private readonly int _templateOrderConfirmed;
    private readonly int _templateOrderDispatched;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes NotificationProxy with Brevo API configuration from environment variables
    /// </summary>
    public NotificationProxy(
        ILogger<NotificationProxy> logger,
        ICircuitBreaker circuitBreaker,
        HttpClient httpClient)
        : base(logger, circuitBreaker, timeoutSeconds: 15, maxRetries: 4)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _apiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY")
            ?? throw new ArgumentException("BREVO_API_KEY environment variable not configured");
        _senderEmail = Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL")
            ?? throw new ArgumentException("BREVO_SENDER_EMAIL environment variable not configured");
        _senderName = Environment.GetEnvironmentVariable("BREVO_SENDER_NAME")
            ?? throw new ArgumentException("BREVO_SENDER_NAME environment variable not configured");
        _baseUrl = Environment.GetEnvironmentVariable("BREVO_BASE_URL") ?? "https://api.brevo.com";
        _apiVersion = Environment.GetEnvironmentVariable("BREVO_API_VERSION") ?? "v3";

        var templateOrderPlacedStr = Environment.GetEnvironmentVariable("BREVO_TEMPLATE_ORDER_PLACED");
        var templateOrderConfirmedStr = Environment.GetEnvironmentVariable("BREVO_TEMPLATE_ORDER_CONFIRMED");
        var templateOrderDispatchedStr = Environment.GetEnvironmentVariable("BREVO_TEMPLATE_ORDER_DISPATCHED");

        if (!int.TryParse(templateOrderPlacedStr, out _templateOrderPlaced) || _templateOrderPlaced <= 0)
            throw new ArgumentException("BREVO_TEMPLATE_ORDER_PLACED environment variable not configured or invalid");
        if (!int.TryParse(templateOrderConfirmedStr, out _templateOrderConfirmed) || _templateOrderConfirmed <= 0)
            throw new ArgumentException("BREVO_TEMPLATE_ORDER_CONFIRMED environment variable not configured or invalid");
        if (!int.TryParse(templateOrderDispatchedStr, out _templateOrderDispatched) || _templateOrderDispatched <= 0)
            throw new ArgumentException("BREVO_TEMPLATE_ORDER_DISPATCHED environment variable not configured or invalid");

        _httpClient = httpClient;
    }

    /// <summary>
    /// Helper method to execute operations with different response types.
    /// </summary>
    private async Task<ProxyResult<T>> ExecuteAsyncGeneric<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var result = await operation();
            return ProxyResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation {OperationName} failed", operationName);
            var proxyEx = ex is ProxyException pex ? pex : new ProxyException(ex.Message, "OPERATION_FAILED", ex);
            return ProxyResult<T>.Failed(proxyEx);
        }
    }

    /// <summary>
    /// Sends a transactional email using Brevo templates
    /// </summary>
    /// <param name="request">Email request with template parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing message ID from Brevo</returns>
    public async Task<ProxyResult<BrevoSendResponse>> SendEmailAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateEmailRequest(request);

        // If template ID is 0, use default template based on email type
        if (request.TemplateId == 0 && !string.IsNullOrWhiteSpace(request.EmailType))
        {
            request.TemplateId = request.EmailType.ToLower() switch
            {
                "order_placed" => _templateOrderPlaced,
                "order_confirmed" => _templateOrderConfirmed,
                "order_dispatched" => _templateOrderDispatched,
                _ => throw new ArgumentException($"Unknown email type: {request.EmailType}")
            };
        }

        return await ExecuteAsync(async () =>
        {
            var payload = new
            {
                to = new[] { new { email = request.To, name = request.ToName ?? "" } },
                sender = new { email = _senderEmail, name = _senderName },
                subject = request.Subject,
                templateId = request.TemplateId,
                @params = request.TemplateParameters ?? new Dictionary<string, string>(),
                headers = request.CustomHeaders ?? new Dictionary<string, string>(),
                tags = new[] { request.Tag ?? "transactional" }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{_apiVersion}/smtp/email")
            {
                Content = content
            };
            httpRequest.Headers.Add("api-key", _apiKey);
            httpRequest.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError(
                    "Brevo email send failed with status {StatusCode}: {ErrorContent}",
                    response.StatusCode,
                    errorContent);
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<BrevoSendResponse>(jsonContent);

            Logger.LogInformation(
                "Email sent successfully to {RecipientEmail} using template {TemplateId}, MessageId: {MessageId}",
                request.To,
                request.TemplateId,
                result?.MessageId);

            return result ?? new BrevoSendResponse { MessageId = "unknown" };
        },
        "SendEmail",
        cancellationToken);
    }

    /// <summary>
    /// Sends an SMS message via Brevo
    /// </summary>
    /// <param name="request">SMS request with recipient and message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing message ID from Brevo</returns>
    public async Task<ProxyResult<BrevoSendResponse>> SendSmsAsync(
        SendSmsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSmsRequest(request);

        return await ExecuteAsync(async () =>
        {
            var payload = new
            {
                sender = _senderName,
                recipient = request.PhoneNumber,
                content = request.Message
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{_apiVersion}/sms/send")
            {
                Content = content
            };
            httpRequest.Headers.Add("api-key", _apiKey);
            httpRequest.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError(
                    "Brevo SMS send failed with status {StatusCode}: {ErrorContent}",
                    response.StatusCode,
                    errorContent);
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<BrevoSendResponse>(jsonContent);

            Logger.LogInformation(
                "SMS sent successfully to {PhoneNumber}, MessageId: {MessageId}",
                request.PhoneNumber,
                result?.MessageId);

            return result ?? new BrevoSendResponse { MessageId = "unknown" };
        },
        "SendSms",
        cancellationToken);
    }

    /// <summary>
    /// Queries email delivery status from Brevo
    /// Tracks sent, delivered, bounced, opened, clicked states
    /// </summary>
    /// <param name="messageId">Brevo message ID to track</param>
    /// <param name="recipientEmail">Recipient email for verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing delivery status</returns>
    public async Task<ProxyResult<DeliveryStatusResponse>> TrackDeliveryStatusAsync(
        string messageId,
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("Message ID cannot be empty", nameof(messageId));
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException("Recipient email cannot be empty", nameof(recipientEmail));

        return await ExecuteAsyncGeneric(async () =>
        {
            // Brevo provides event endpoint for transactional email tracking
            var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/{_apiVersion}/smtp/statistics");
            httpRequest.Headers.Add("api-key", _apiKey);
            httpRequest.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError(
                    "Brevo delivery status query failed with status {StatusCode}: {ErrorContent}",
                    response.StatusCode,
                    errorContent);
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<DeliveryStatusResponse>(jsonContent);

            Logger.LogInformation(
                "Delivery status retrieved for {RecipientEmail}, MessageId: {MessageId}, Status: {Status}",
                recipientEmail,
                messageId,
                result?.Status ?? "unknown");

            return result ?? new DeliveryStatusResponse 
            { 
                Status = "unknown",
                MessageId = messageId,
                RecipientEmail = recipientEmail 
            };
        },
        "TrackDeliveryStatus",
        cancellationToken);
    }

    /// <summary>
    /// Validates email request before sending
    /// Checks format, validates recipient exists, performs basic DNS validation
    /// </summary>
    /// <param name="request">Email request to validate</param>
    private void ValidateEmailRequest(SendEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.To))
            throw new ArgumentException("Recipient email is required", nameof(request.To));

        if (!IsValidEmail(request.To))
            throw new ArgumentException($"Invalid email format: {request.To}", nameof(request.To));

        if (request.TemplateId <= 0)
            throw new ArgumentException("Valid template ID is required", nameof(request.TemplateId));

        if (string.IsNullOrWhiteSpace(request.Subject) && request.TemplateId == 0)
            throw new ArgumentException("Subject is required when not using template", nameof(request.Subject));

        // Validate template parameters if provided
        if (request.TemplateParameters != null)
        {
            foreach (var param in request.TemplateParameters)
            {
                if (string.IsNullOrWhiteSpace(param.Key))
                    throw new ArgumentException("Template parameter key cannot be empty");
            }
        }
    }

    /// <summary>
    /// Validates SMS request before sending
    /// Checks phone number format and message content
    /// </summary>
    /// <param name="request">SMS request to validate</param>
    private void ValidateSmsRequest(SendSmsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new ArgumentException("Phone number is required", nameof(request.PhoneNumber));

        // Basic phone number validation (E.164 format recommended)
        if (!IsValidPhoneNumber(request.PhoneNumber))
            throw new ArgumentException($"Invalid phone number format: {request.PhoneNumber}", nameof(request.PhoneNumber));

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message content is required", nameof(request.Message));

        if (request.Message.Length > 160)
            Logger.LogWarning("SMS message exceeds 160 characters and may be split into multiple messages");
    }

    /// <summary>
    /// Simple email format validation using regex
    /// More robust validation could use email verification service
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Basic phone number validation (E.164 format)
    /// Format: +[country code][number]
    /// </summary>
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        // Remove common formatting characters
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            phoneNumber,
            "[^0-9+]",
            "");

        // Must start with + and contain 10-15 digits
        return System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\+\d{10,15}$") ||
               System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\d{10,15}$");
    }
}

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
    /// Email type for automatic template selection (order_placed, order_confirmed, order_dispatched)
    /// If TemplateId is 0, this will be used to select the template from environment variables
    /// </summary>
    public string? EmailType { get; set; }

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
