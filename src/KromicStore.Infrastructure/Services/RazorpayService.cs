// Copyright (c) KromicStore. All rights reserved.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using KromicStore.Contracts.V1.External;
using KromicStore.Application.Interfaces;

namespace KromicStore.Infrastructure.Services;

/// <summary>
/// Service for interacting with Razorpay API.
/// Handles subscription creation, payments, and verification.
/// </summary>
public class RazorpayService : IRazorpayService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RazorpayService> _logger;
    private readonly string _keyId;
    private readonly string _keySecret;
    private const int DefaultTimeoutSeconds = 30;
    private const int MaxRetries = 3;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of RazorpayService.
    /// </summary>
    public RazorpayService(HttpClient httpClient, ILogger<RazorpayService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get credentials from environment
        _keyId = Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID") 
            ?? throw new InvalidOperationException("RAZORPAY_KEY_ID not configured");
        _keySecret = Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET") 
            ?? throw new InvalidOperationException("RAZORPAY_KEY_SECRET not configured");

        // Configure default headers with basic auth
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
    }

    /// <summary>
    /// Creates a subscription in Razorpay for recurring billing.
    /// </summary>
    public async Task<RazorpaySubscriptionResponse> CreateSubscriptionAsync(
        string customerId,
        int amountInPaisa,
        string planId,
        string customerEmail,
        Dictionary<string, string> notes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer ID required", nameof(customerId));
        if (amountInPaisa <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amountInPaisa));
        if (string.IsNullOrWhiteSpace(planId))
            throw new ArgumentException("Plan ID required", nameof(planId));

        var request = new
        {
            customer_id = customerId,
            plan_id = planId,
            customer_notify = 1,
            quantity = 1,
            notes = notes ?? new Dictionary<string, string>()
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        _logger.LogInformation("Creating Razorpay subscription for customer {CustomerId}", customerId);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await SendWithRetryAsync(HttpMethod.Post, "subscriptions", content, cancellationToken);

        return JsonSerializer.Deserialize<RazorpaySubscriptionResponse>(response, JsonOptions) 
            ?? throw new InvalidOperationException("Failed to parse subscription response");
    }

    /// <summary>
    /// Retrieves a subscription from Razorpay.
    /// </summary>
    public async Task<RazorpaySubscriptionResponse> GetSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID required", nameof(subscriptionId));

        _logger.LogInformation("Fetching Razorpay subscription {SubscriptionId}", subscriptionId);

        var response = await SendWithRetryAsync(
            HttpMethod.Get, 
            $"subscriptions/{subscriptionId}", 
            null, 
            cancellationToken);

        return JsonSerializer.Deserialize<RazorpaySubscriptionResponse>(response, JsonOptions) 
            ?? throw new InvalidOperationException("Failed to parse subscription response");
    }

    /// <summary>
    /// Updates subscription amount (for upgrades/downgrades).
    /// </summary>
    public async Task<RazorpaySubscriptionResponse> UpdateSubscriptionAsync(
        string subscriptionId,
        int newAmountInPaisa,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID required", nameof(subscriptionId));
        if (newAmountInPaisa <= 0)
            throw new ArgumentException("Amount must be positive", nameof(newAmountInPaisa));

        var request = new { quantity = 1 }; // Razorpay uses quantity to adjust amount

        var json = JsonSerializer.Serialize(request, JsonOptions);
        _logger.LogInformation("Updating Razorpay subscription {SubscriptionId} to ₹{Amount}", subscriptionId, newAmountInPaisa / 100m);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await SendWithRetryAsync(
            HttpMethod.Put, 
            $"subscriptions/{subscriptionId}", 
            content, 
            cancellationToken);

        return JsonSerializer.Deserialize<RazorpaySubscriptionResponse>(response, JsonOptions) 
            ?? throw new InvalidOperationException("Failed to parse subscription response");
    }

    /// <summary>
    /// Cancels a subscription.
    /// </summary>
    public async Task<bool> CancelSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID required", nameof(subscriptionId));

        _logger.LogInformation("Cancelling Razorpay subscription {SubscriptionId}", subscriptionId);

        try
        {
            await SendWithRetryAsync(
                HttpMethod.Post, 
                $"subscriptions/{subscriptionId}/cancel", 
                null, 
                cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Pauses a subscription.
    /// </summary>
    public async Task<bool> PauseSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID required", nameof(subscriptionId));

        _logger.LogInformation("Pausing Razorpay subscription {SubscriptionId}", subscriptionId);

        try
        {
            await SendWithRetryAsync(
                HttpMethod.Post, 
                $"subscriptions/{subscriptionId}/pause", 
                null, 
                cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Resumes a paused subscription.
    /// </summary>
    public async Task<bool> ResumeSubscriptionAsync(
        string subscriptionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID required", nameof(subscriptionId));

        _logger.LogInformation("Resuming Razorpay subscription {SubscriptionId}", subscriptionId);

        try
        {
            await SendWithRetryAsync(
                HttpMethod.Post, 
                $"subscriptions/{subscriptionId}/resume", 
                null, 
                cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    /// <summary>
    /// Creates an order for one-time payment (used by tenants for product sales).
    /// Uses provided API key/secret instead of default credentials.
    /// </summary>
    public async Task<RazorpayOrderResponse> CreateOrderAsync(
        decimal amountInRupees,
        string currency,
        string receipt,
        Dictionary<string, string> notes,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        if (amountInRupees <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amountInRupees));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency required", nameof(currency));
        if (string.IsNullOrWhiteSpace(receipt))
            throw new ArgumentException("Receipt required", nameof(receipt));

        var amountInPaisa = (int)(amountInRupees * 100);

        var request = new
        {
            amount = amountInPaisa,
            currency = currency,
            receipt = receipt,
            notes = notes ?? new Dictionary<string, string>()
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        _logger.LogInformation("Creating Razorpay order for ₹{Amount}", amountInRupees);

        // Use custom credentials for tenant's Razorpay account
        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request2.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var response = await _httpClient.SendAsync(request2, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay order creation failed: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Razorpay API error: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<RazorpayOrderResponse>(responseBody, JsonOptions) 
            ?? throw new InvalidOperationException("Failed to parse order response");
    }

    /// <summary>
    /// Captures a payment (finalizes authorized payment).
    /// Uses provided API key/secret for tenant's Razorpay account.
    /// </summary>
    public async Task<RazorpayPaymentResponse> CapturePaymentAsync(
        string paymentId,
        int amountInPaisa,
        string apiKey,
        string apiSecret,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            throw new ArgumentException("Payment ID required", nameof(paymentId));
        if (amountInPaisa <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amountInPaisa));

        var request = new { amount = amountInPaisa };
        var json = JsonSerializer.Serialize(request, JsonOptions);

        _logger.LogInformation("Capturing Razorpay payment {PaymentId}", paymentId);

        var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
        using var request2 = new HttpRequestMessage(
            HttpMethod.Post, 
            $"https://api.razorpay.com/v1/payments/{paymentId}/capture")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request2.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var response = await _httpClient.SendAsync(request2, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Payment capture failed: {StatusCode} {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Razorpay API error: {response.StatusCode}");
        }

        return JsonSerializer.Deserialize<RazorpayPaymentResponse>(responseBody, JsonOptions) 
            ?? throw new InvalidOperationException("Failed to parse payment response");
    }

    /// <summary>
    /// Verifies a payment signature (for webhooks).
    /// </summary>
    public bool VerifySignature(
        string orderId,
        string paymentId,
        string signature,
        string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature))
            return false;

        var message = $"{orderId}|{paymentId}";
        var computedSignature = ComputeHmacSha256(message, webhookSecret);

        var isValid = computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
        
        if (!isValid)
            _logger.LogWarning("Signature verification failed for order {OrderId}", orderId);

        return isValid;
    }

    /// <summary>
    /// Verifies webhook signature from Razorpay.
    /// </summary>
    public bool VerifyWebhookSignature(
        string body,
        string signature,
        string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(signature))
            return false;

        var computedSignature = ComputeHmacSha256(body, webhookSecret);
        var isValid = computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
            _logger.LogWarning("Webhook signature verification failed");

        return isValid;
    }

    /// <summary>
    /// Sends HTTP request with retry logic.
    /// </summary>
    private async Task<string> SendWithRetryAsync(
        HttpMethod method,
        string endpoint,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        int retryCount = 0;
        Exception? lastException = null;

        while (retryCount < MaxRetries)
        {
            try
            {
                using var request = new HttpRequestMessage(method, endpoint)
                {
                    Content = content
                };

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }

                // Log error but continue retry for transient errors
                _logger.LogWarning(
                    "Razorpay API error (attempt {Attempt}/{MaxRetries}): {StatusCode} {Body}",
                    retryCount + 1, MaxRetries, response.StatusCode, responseBody);

                if (!IsTransientError(response.StatusCode))
                {
                    throw new InvalidOperationException($"Razorpay API error: {response.StatusCode} - {responseBody}");
                }

                lastException = new HttpRequestException($"HTTP {response.StatusCode}");
            }
            catch (HttpRequestException ex) when (retryCount < MaxRetries - 1)
            {
                lastException = ex;
                _logger.LogWarning(ex, "HTTP error (attempt {Attempt}/{MaxRetries})", retryCount + 1, MaxRetries);
            }
            catch (TaskCanceledException ex) when (retryCount < MaxRetries - 1)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Request timeout (attempt {Attempt}/{MaxRetries})", retryCount + 1, MaxRetries);
            }

            retryCount++;

            if (retryCount < MaxRetries)
            {
                var delay = (int)Math.Pow(2, retryCount - 1) * 1000; // Exponential backoff: 1s, 2s, 4s
                _logger.LogInformation("Retrying in {DelayMs}ms...", delay);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Razorpay API request failed after {MaxRetries} attempts",
            lastException);
    }

    /// <summary>
    /// Determines if HTTP error is transient (retriable).
    /// </summary>
    private static bool IsTransientError(System.Net.HttpStatusCode statusCode)
    {
        return statusCode == System.Net.HttpStatusCode.RequestTimeout ||
               statusCode == System.Net.HttpStatusCode.TooManyRequests ||
               statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
               statusCode == System.Net.HttpStatusCode.GatewayTimeout ||
               (int)statusCode >= 500;
    }

    /// <summary>
    /// Computes HMAC SHA256 signature.
    /// </summary>
    private static string ComputeHmacSha256(string message, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
