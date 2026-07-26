#nullable disable

using System.Text.Json.Serialization;

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Response model from Razorpay refund operation
/// </summary>
public class RefundResponse
{
    /// <summary>
    /// Unique refund ID from Razorpay
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Associated payment ID
    /// </summary>
    [JsonPropertyName("payment_id")]
    public string PaymentId { get; set; }

    /// <summary>
    /// Refund amount in paise
    /// </summary>
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    /// <summary>
    /// Refund currency
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// Refund status (processed, pending, failed, etc.)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Reason for refund
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    /// <summary>
    /// Receipt reference for refund
    /// </summary>
    [JsonPropertyName("receipt")]
    public string Receipt { get; set; }

    /// <summary>
    /// Notes attached to refund
    /// </summary>
    [JsonPropertyName("notes")]
    public Dictionary<string, string> Notes { get; set; }

    /// <summary>
    /// Unix timestamp when refund was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public int CreatedAt { get; set; }

    /// <summary>
    /// Batch ID if part of batch processing
    /// </summary>
    [JsonPropertyName("batch_id")]
    public string BatchId { get; set; }
}
