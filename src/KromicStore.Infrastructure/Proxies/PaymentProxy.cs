#nullable disable

using System.Text;
using System.Text.Json;
using KromicStore.Infrastructure.Proxies.Models;
using Microsoft.Extensions.Logging;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Proxy for Razorpay payment gateway integration
/// Handles payment creation, verification, refunds, and status queries with fault tolerance
/// All configuration is loaded from environment variables
/// </summary>
public class PaymentProxy : ServiceProxy<PaymentResponse>
{
    private readonly HttpClient _httpClient;
    private readonly string _keyId;
    private readonly string _keySecret;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the PaymentProxy class with environment variables
    /// </summary>
    public PaymentProxy(
        ILogger<PaymentProxy> logger,
        ICircuitBreaker circuitBreaker,
        HttpClient httpClient)
        : base(logger, circuitBreaker)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _keyId = Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID")
            ?? throw new ArgumentException("RAZORPAY_KEY_ID environment variable not configured");
        _keySecret = Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET")
            ?? throw new ArgumentException("RAZORPAY_KEY_SECRET environment variable not configured");
        _baseUrl = Environment.GetEnvironmentVariable("RAZORPAY_API_ENDPOINT")
            ?? throw new ArgumentException("RAZORPAY_API_ENDPOINT environment variable not configured");
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
    /// Creates a payment order with Razorpay
    /// </summary>
    /// <param name="request">Payment creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProxyResult containing payment response or error</returns>
    public async Task<ProxyResult<PaymentResponse>> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ValidatePaymentRequest(request);

        Logger.LogInformation(
            "Creating payment with amount {Amount} {Currency}, idempotency key: {IdempotencyKey}",
            request.Amount,
            request.Currency,
            request.IdempotencyKey);

        return await ExecuteAsync(async () =>
        {
            // Prepare request body - build dictionary first
            var formData = new Dictionary<string, string>
            {
                { "amount", request.Amount.ToString() },
                { "currency", request.Currency },
                { "receipt", request.IdempotencyKey },
                { "description", request.Description }
            };

            // Add customer details if provided
            if (!string.IsNullOrEmpty(request.CustomerEmail))
                formData["customer_email"] = request.CustomerEmail;
            if (!string.IsNullOrEmpty(request.CustomerName))
                formData["customer_name"] = request.CustomerName;
            if (!string.IsNullOrEmpty(request.CustomerPhone))
                formData["customer_phone"] = request.CustomerPhone;

            // Add notification preferences
            if (!request.NotifyEmail)
                formData["notify_email"] = "0";
            if (!request.NotifySms)
                formData["notify_sms"] = "0";

            var content = new FormUrlEncodedContent(formData);

            // Create HTTP request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/orders")
            {
                Content = content
            };

            // Add authentication header (Basic Auth with KeyId:KeySecret)
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
            httpRequest.Headers.Add("Authorization", $"Basic {authHeader}");

            // Add idempotency key for safe retries
            httpRequest.Headers.Add("Idempotency-Key", request.IdempotencyKey);

            // Send request
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            // Handle error responses
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogWarning(
                    "Razorpay error response ({StatusCode}): {ErrorContent}",
                    response.StatusCode,
                    errorContent);

                throw new ProxyException(
                    $"Razorpay API returned {response.StatusCode}: {errorContent}",
                    "RAZORPAY_API_ERROR");
            }

            // Parse successful response
            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var paymentData = JsonSerializer.Deserialize<PaymentResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paymentData == null)
                throw new ProxyException("Failed to deserialize Razorpay response", "DESERIALIZATION_ERROR");

            Logger.LogInformation(
                "Payment created successfully. Payment ID: {PaymentId}, Amount: {Amount}",
                paymentData.Id,
                paymentData.Amount);

            return paymentData;
        },
        "CreatePayment",
        cancellationToken);
    }

    /// <summary>
    /// Verifies payment status with Razorpay
    /// </summary>
    /// <param name="request">Payment verification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProxyResult containing payment status</returns>
    public async Task<ProxyResult<PaymentResponse>> VerifyPaymentAsync(
        VerifyPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrEmpty(request.PaymentId))
            throw new ArgumentException("PaymentId is required", nameof(request));

        Logger.LogInformation("Verifying payment status for payment ID: {PaymentId}", request.PaymentId);

        return await ExecuteAsync(async () =>
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/payments/{request.PaymentId}");

            // Add authentication
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
            httpRequest.Headers.Add("Authorization", $"Basic {authHeader}");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogWarning(
                    "Failed to verify payment {PaymentId} ({StatusCode}): {ErrorContent}",
                    request.PaymentId,
                    response.StatusCode,
                    errorContent);

                throw new ProxyException(
                    $"Failed to verify payment: {response.StatusCode}",
                    "PAYMENT_VERIFICATION_FAILED");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var paymentData = JsonSerializer.Deserialize<PaymentResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paymentData == null)
                throw new ProxyException("Failed to deserialize payment response", "DESERIALIZATION_ERROR");

            Logger.LogInformation(
                "Payment {PaymentId} status verified: {Status}",
                paymentData.Id,
                paymentData.Status);

            return paymentData;
        },
        "VerifyPayment",
        cancellationToken);
    }

    /// <summary>
    /// Requests a refund for a payment
    /// </summary>
    /// <param name="paymentId">Razorpay payment ID</param>
    /// <param name="amount">Refund amount in paise (null for full refund)</param>
    /// <param name="reason">Refund reason (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProxyResult containing refund response or error</returns>
    public async Task<ProxyResult<RefundResponse>> RefundAsync(
        string paymentId,
        int? amount = null,
        string reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(paymentId))
            throw new ArgumentException("PaymentId is required", nameof(paymentId));

        Logger.LogInformation(
            "Processing refund for payment {PaymentId}, amount: {Amount}, reason: {Reason}",
            paymentId,
            amount?.ToString() ?? "full",
            reason ?? "none");

        return await ExecuteAsyncGeneric(async () =>
        {
            var refundData = new Dictionary<string, string>();

            if (amount.HasValue && amount.Value > 0)
                refundData["amount"] = amount.Value.ToString();

            if (!string.IsNullOrEmpty(reason))
                refundData["notes"] = reason;

            var content = new FormUrlEncodedContent(refundData);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{_baseUrl}/payments/{paymentId}/refund")
            {
                Content = content
            };

            // Add authentication
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
            httpRequest.Headers.Add("Authorization", $"Basic {authHeader}");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogWarning(
                    "Refund failed for payment {PaymentId} ({StatusCode}): {ErrorContent}",
                    paymentId,
                    response.StatusCode,
                    errorContent);

                throw new ProxyException(
                    $"Refund failed: {response.StatusCode}",
                    "REFUND_FAILED");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var refundResponse = JsonSerializer.Deserialize<RefundResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (refundResponse == null)
                throw new ProxyException("Failed to deserialize refund response", "DESERIALIZATION_ERROR");

            Logger.LogInformation(
                "Refund processed successfully. Refund ID: {RefundId}, Amount: {Amount}",
                refundResponse.Id,
                refundResponse.Amount);

            return refundResponse;
        },
        "RefundPayment",
        cancellationToken);
    }

    /// <summary>
    /// Retrieves all refunds for a payment
    /// </summary>
    /// <param name="paymentId">Razorpay payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ProxyResult containing list of refunds</returns>
    public async Task<ProxyResult<RefundListResponse>> GetRefundsAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(paymentId))
            throw new ArgumentException("PaymentId is required", nameof(paymentId));

        Logger.LogInformation("Fetching refunds for payment {PaymentId}", paymentId);

        return await ExecuteAsyncGeneric(async () =>
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/payments/{paymentId}/refunds");

            // Add authentication
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_keyId}:{_keySecret}"));
            httpRequest.Headers.Add("Authorization", $"Basic {authHeader}");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogWarning(
                    "Failed to fetch refunds for payment {PaymentId} ({StatusCode}): {ErrorContent}",
                    paymentId,
                    response.StatusCode,
                    errorContent);

                throw new ProxyException(
                    $"Failed to fetch refunds: {response.StatusCode}",
                    "FETCH_REFUNDS_FAILED");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var refundListResponse = JsonSerializer.Deserialize<RefundListResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (refundListResponse == null)
                throw new ProxyException("Failed to deserialize refunds response", "DESERIALIZATION_ERROR");

            Logger.LogInformation(
                "Retrieved {RefundCount} refunds for payment {PaymentId}",
                refundListResponse.Items?.Count ?? 0,
                paymentId);

            return refundListResponse;
        },
        "GetRefunds",
        cancellationToken);
    }

    /// <summary>
    /// Validates the payment request
    /// </summary>
    private static void ValidatePaymentRequest(CreatePaymentRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(request.Amount));

        if (string.IsNullOrEmpty(request.Currency))
            throw new ArgumentException("Currency is required", nameof(request.Currency));

        if (string.IsNullOrEmpty(request.Description))
            throw new ArgumentException("Description is required", nameof(request.Description));

        if (string.IsNullOrEmpty(request.IdempotencyKey))
            throw new ArgumentException("IdempotencyKey is required", nameof(request.IdempotencyKey));
    }
}

/// <summary>
/// Response model for refund operations
/// </summary>
public class RefundResponse
{
    public string Id { get; set; }
    public string Entity { get; set; }
    public int Amount { get; set; }
    public string Currency { get; set; }
    public string PaymentId { get; set; }
    public string Status { get; set; }
    public int CreatedAt { get; set; }
}

/// <summary>
/// Response model for list of refunds
/// </summary>
public class RefundListResponse
{
    public string Entity { get; set; }
    public int Count { get; set; }
    public List<RefundResponse> Items { get; set; }
}

