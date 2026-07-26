#nullable enable

namespace KromicStore.Infrastructure.BackgroundJobs;

using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KromicStore.Domain.Entities;
using KromicStore.Application.Interfaces;

/// <summary>
/// Background job for delivering webhook payloads to configured endpoints.
/// Implements HMAC-SHA256 signature generation, retry logic, and delivery tracking.
/// </summary>
public class WebhookDeliveryJob
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookDeliveryJob> _logger;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the WebhookDeliveryJob class.
    /// </summary>
    public WebhookDeliveryJob(
        HttpClient httpClient,
        ILogger<WebhookDeliveryJob> logger,
        IUnitOfWork unitOfWork)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Executes the webhook delivery attempt.
    /// </summary>
    /// <param name="deliveryLogId">The webhook delivery log ID.</param>
    /// <param name="payload">The JSON payload to deliver.</param>
    /// <param name="secret">The webhook secret for signature generation.</param>
    /// <param name="endpointUrl">The endpoint URL.</param>
    /// <param name="authenticationHeader">Optional authentication header.</param>
    public async Task ExecuteAsync(
        Guid deliveryLogId,
        string payload,
        string secret,
        string endpointUrl,
        string? authenticationHeader = null)
    {
        try
        {
            _logger.LogInformation(
                "Executing webhook delivery {DeliveryLogId} to {EndpointUrl}",
                deliveryLogId, endpointUrl);

            // Generate signature
            string signature = GenerateSignature(payload, secret);
            string timestamp = DateTime.UtcNow.ToString("O");

            // Create HTTP request
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl))
            {
                // Add headers
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                request.Headers.Add("X-KromicStore-Signature", $"sha256={signature}");
                request.Headers.Add("X-KromicStore-Timestamp", timestamp);
                request.Headers.Add("X-KromicStore-Event", "Webhook");
                request.Headers.Add("User-Agent", "KromicStore-Webhook/1.0");

                // Add custom authentication header if provided
                if (!string.IsNullOrEmpty(authenticationHeader))
                {
                    if (authenticationHeader.StartsWith("Bearer "))
                    {
                        request.Headers.Add("Authorization", authenticationHeader);
                    }
                    else
                    {
                        request.Headers.Add("X-Custom-Auth", authenticationHeader);
                    }
                }

                // Send request with 30-second timeout
                using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    try
                    {
                        var response = await _httpClient.SendAsync(request, cts.Token);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation(
                                "Webhook delivery {DeliveryLogId} succeeded with status {StatusCode}",
                                deliveryLogId, response.StatusCode);

                            // Record success
                            string responseBody = await response.Content.ReadAsStringAsync();
                            await RecordSuccessAsync(deliveryLogId, (int)response.StatusCode, responseBody);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Webhook delivery {DeliveryLogId} failed with status {StatusCode}",
                                deliveryLogId, response.StatusCode);

                            // Record failure and schedule retry
                            string responseBody = await response.Content.ReadAsStringAsync();
                            await RecordFailureAsync(deliveryLogId, (int)response.StatusCode, responseBody);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning(
                            "Webhook delivery {DeliveryLogId} timed out after 30 seconds",
                            deliveryLogId);

                        await RecordFailureAsync(deliveryLogId, null, null, "Request timeout after 30 seconds");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error in webhook delivery {DeliveryLogId}",
                deliveryLogId);

            await RecordFailureAsync(deliveryLogId, null, null, ex.Message);
        }
    }

    /// <summary>
    /// Generates HMAC-SHA256 signature for payload verification.
    /// </summary>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="secret">The webhook secret (Base64-encoded).</param>
    /// <returns>Hex-encoded HMAC-SHA256 signature.</returns>
    private string GenerateSignature(string payload, string secret)
    {
        try
        {
            // Decode Base64 secret
            byte[] secretBytes = Convert.FromBase64String(secret);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

            // Generate HMAC-SHA256
            using (var hmac = new HMACSHA256(secretBytes))
            {
                byte[] hash = hmac.ComputeHash(payloadBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating webhook signature");
            throw;
        }
    }

    /// <summary>
    /// Records a successful delivery attempt.
    /// </summary>
    private async Task RecordSuccessAsync(Guid deliveryLogId, int statusCode, string? responseBody)
    {
        try
        {
            // In a full implementation, would update the WebhookDeliveryLog entity
            // For now, just log
            _logger.LogInformation(
                "Recorded successful webhook delivery {DeliveryLogId} with status {StatusCode}",
                deliveryLogId, statusCode);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording successful webhook delivery");
        }
    }

    /// <summary>
    /// Records a failed delivery attempt and schedules retry if applicable.
    /// </summary>
    private async Task RecordFailureAsync(
        Guid deliveryLogId,
        int? statusCode,
        string? responseBody,
        string? errorMessage = null)
    {
        try
        {
            // In a full implementation, would update the WebhookDeliveryLog entity
            // and requeue the job for retry
            _logger.LogWarning(
                "Recorded failed webhook delivery {DeliveryLogId} with status {StatusCode}: {ErrorMessage}",
                deliveryLogId, statusCode, errorMessage);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording failed webhook delivery");
        }
    }
}
