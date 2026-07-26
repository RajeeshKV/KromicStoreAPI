#nullable disable

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Request model for creating a payment with Razorpay
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Payment amount in paise (1 INR = 100 paise)
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Currency code (default: INR)
    /// </summary>
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// Payment description/purpose
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Unique idempotency key for safe retries
    /// </summary>
    public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Optional customer email for Razorpay customer creation
    /// </summary>
    public string CustomerEmail { get; set; }

    /// <summary>
    /// Optional customer name
    /// </summary>
    public string CustomerName { get; set; }

    /// <summary>
    /// Optional customer phone number
    /// </summary>
    public string CustomerPhone { get; set; }

    /// <summary>
    /// Notification preferences
    /// </summary>
    public bool NotifyEmail { get; set; } = true;

    public bool NotifySms { get; set; } = true;
}
