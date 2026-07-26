#nullable disable

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Request model for verifying payment status with Razorpay
/// </summary>
public class VerifyPaymentRequest
{
    /// <summary>
    /// Payment ID to verify status for
    /// </summary>
    public string PaymentId { get; set; }

    /// <summary>
    /// Order ID associated with this payment (optional, for validation)
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// Razorpay payment signature for client-side verification (optional)
    /// </summary>
    public string Signature { get; set; }
}
