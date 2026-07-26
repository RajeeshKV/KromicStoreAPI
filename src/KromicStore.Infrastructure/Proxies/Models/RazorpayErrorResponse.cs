#nullable disable

using System.Text.Json.Serialization;

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Error response model from Razorpay API
/// </summary>
public class RazorpayErrorResponse
{
    /// <summary>
    /// Error code (e.g., BAD_REQUEST_ERROR, GATEWAY_ERROR, etc.)
    /// </summary>
    [JsonPropertyName("error")]
    public RazorpayError Error { get; set; }
}

/// <summary>
/// Details of a Razorpay error
/// </summary>
public class RazorpayError
{
    /// <summary>
    /// Error code identifier
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// Source of error (e.g., BAD_REQUEST_ERROR, GATEWAY_ERROR, NETWORK_ERROR)
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; }

    /// <summary>
    /// Reason code for the error
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    /// <summary>
    /// Metadata field (varies by error type)
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; }
}
