#nullable disable

using System.Text.Json.Serialization;

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Response model from Razorpay when creating or retrieving a payment/order
/// </summary>
public class PaymentResponse
{
    /// <summary>
    /// Unique order/payment ID from Razorpay
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Payment amount in paise
    /// </summary>
    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    /// <summary>
    /// Amount paid in paise
    /// </summary>
    [JsonPropertyName("amount_paid")]
    public int AmountPaid { get; set; }

    /// <summary>
    /// Amount due in paise
    /// </summary>
    [JsonPropertyName("amount_due")]
    public int AmountDue { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; }

    /// <summary>
    /// Payment/Order receipt reference
    /// </summary>
    [JsonPropertyName("receipt")]
    public string Receipt { get; set; }

    /// <summary>
    /// Status of the payment (created, attempted, captured, failed, etc.)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Attempts made for payment
    /// </summary>
    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }

    /// <summary>
    /// Notes attached to the order
    /// </summary>
    [JsonPropertyName("notes")]
    public Dictionary<string, string> Notes { get; set; }

    /// <summary>
    /// Unix timestamp when order was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public int CreatedAt { get; set; }

    /// <summary>
    /// Short URL for payment
    /// </summary>
    [JsonPropertyName("short_url")]
    public string ShortUrl { get; set; }

    /// <summary>
    /// User ID associated with order (if customer linked)
    /// </summary>
    [JsonPropertyName("customer_id")]
    public string CustomerId { get; set; }

    /// <summary>
    /// Description of the order
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// Expire by Unix timestamp
    /// </summary>
    [JsonPropertyName("expire_by")]
    public int? ExpireBy { get; set; }

    /// <summary>
    /// Expired indicator
    /// </summary>
    [JsonPropertyName("expired_at")]
    public int? ExpiredAt { get; set; }
}
