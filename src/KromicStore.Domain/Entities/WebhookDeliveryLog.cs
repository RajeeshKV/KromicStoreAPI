namespace KromicStore.Domain.Entities;

using System;

/// <summary>
/// Represents a webhook delivery attempt log.
/// Tracks HTTP status codes, response bodies, retry counts, and next retry times.
/// </summary>
public class WebhookDeliveryLog : BaseEntity
{
    /// <summary>
    /// Retry delays (in seconds) for exponential backoff: 1s, 10s, 100s, 1000s, 10000s
    /// </summary>
    public static readonly int[] RetryDelaysSeconds = { 1, 10, 100, 1000, 10000 };

    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    public static readonly int MaxRetryAttempts = 5;

    /// <summary>
    /// Gets the webhook configuration ID this delivery is for.
    /// </summary>
    public Guid WebhookConfigurationId { get; private set; }

    /// <summary>
    /// Gets the webhook event log ID.
    /// </summary>
    public Guid WebhookEventLogId { get; private set; }

    /// <summary>
    /// Gets the HTTP status code received from the endpoint.
    /// Null if delivery hasn't been attempted yet.
    /// </summary>
    public int? HttpStatusCode { get; private set; }

    /// <summary>
    /// Gets the response body (truncated to 1000 characters).
    /// </summary>
    public string? Response { get; private set; }

    /// <summary>
    /// Gets the number of delivery attempts made.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the timestamp of the next planned retry.
    /// Null if delivery succeeded or max retries exceeded.
    /// </summary>
    public DateTime? NextRetryAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this delivery succeeded (2xx status code).
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// Gets the timestamp when delivery was completed or failed.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Gets optional error message for diagnostic purposes.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Initializes a new instance of the WebhookDeliveryLog class.
    /// </summary>
    private WebhookDeliveryLog()
    {
    }

    /// <summary>
    /// Factory method to create a new webhook delivery log.
    /// </summary>
    /// <param name="webhookConfigurationId">The webhook configuration ID.</param>
    /// <param name="webhookEventLogId">The webhook event log ID.</param>
    /// <returns>A new WebhookDeliveryLog instance.</returns>
    public static WebhookDeliveryLog Create(
        Guid webhookConfigurationId,
        Guid webhookEventLogId)
    {
        var deliveryLog = new WebhookDeliveryLog
        {
            Id = Guid.NewGuid(),
            WebhookConfigurationId = webhookConfigurationId,
            WebhookEventLogId = webhookEventLogId,
            RetryCount = 0,
            IsSuccessful = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return deliveryLog;
    }

    /// <summary>
    /// Records a successful delivery attempt.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="response">Optional response body.</param>
    public void RecordSuccess(int statusCode, string? response = null)
    {
        HttpStatusCode = statusCode;
        Response = TruncateResponse(response);
        IsSuccessful = true;
        CompletedAt = DateTime.UtcNow;
        NextRetryAt = null;
        UpdateTimestamp();
    }

    /// <summary>
    /// Records a failed delivery attempt and calculates next retry time.
    /// </summary>
    /// <param name="statusCode">The HTTP status code, if available.</param>
    /// <param name="response">Optional response body or error message.</param>
    /// <param name="errorMessage">Optional diagnostic error message.</param>
    public void RecordFailure(int? statusCode = null, string? response = null, string? errorMessage = null)
    {
        HttpStatusCode = statusCode;
        Response = TruncateResponse(response);
        ErrorMessage = errorMessage;
        RetryCount++;
        
        if (RetryCount >= MaxRetryAttempts)
        {
            // Max retries exceeded
            NextRetryAt = null;
            CompletedAt = DateTime.UtcNow;
        }
        else
        {
            // Schedule next retry
            int delaySeconds = RetryDelaysSeconds[RetryCount - 1];
            NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
        }

        UpdateTimestamp();
    }

    /// <summary>
    /// Calculates the next retry time based on current retry count.
    /// </summary>
    /// <returns>The DateTime of the next retry, or null if max retries exceeded.</returns>
    public DateTime? CalculateNextRetry()
    {
        if (RetryCount >= MaxRetryAttempts)
        {
            return null;
        }

        int delaySeconds = RetryDelaysSeconds[RetryCount];
        return DateTime.UtcNow.AddSeconds(delaySeconds);
    }

    /// <summary>
    /// Determines if this delivery should be retried.
    /// </summary>
    /// <returns>True if retry should be attempted; false if max retries exceeded or already successful.</returns>
    public bool ShouldRetry()
    {
        if (IsSuccessful)
        {
            return false;
        }

        if (RetryCount >= MaxRetryAttempts)
        {
            return false;
        }

        if (NextRetryAt == null)
        {
            return true; // First attempt
        }

        return DateTime.UtcNow >= NextRetryAt;
    }

    /// <summary>
    /// Truncates response body to prevent excessive storage.
    /// </summary>
    /// <param name="response">The response text.</param>
    /// <returns>Truncated response (max 1000 characters).</returns>
    private static string? TruncateResponse(string? response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return null;
        }

        return response.Length > 1000 ? response.Substring(0, 1000) + "..." : response;
    }
}
