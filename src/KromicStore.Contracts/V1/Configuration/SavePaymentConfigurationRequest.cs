namespace KromicStore.Contracts.V1.Configuration;

/// <summary>
/// Request DTO for saving payment configuration.
/// Contains encrypted Razorpay API credentials.
/// </summary>
public class SavePaymentConfigurationRequest
{
    /// <summary>
    /// Gets or sets the Razorpay API Key ID.
    /// Used for authenticating requests to Razorpay API.
    /// </summary>
    public string RazorpayKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Razorpay API Key Secret.
    /// Used for authenticating requests to Razorpay API. Must be kept secure.
    /// </summary>
    public string RazorpayKeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Razorpay Webhook Secret.
    /// Used for verifying webhook signatures from Razorpay to ensure authenticity.
    /// </summary>
    public string RazorpayWebhookSecret { get; set; } = string.Empty;
}
