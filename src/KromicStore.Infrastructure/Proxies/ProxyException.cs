using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Exception thrown by proxy operations for standardized error handling across external service integrations
/// </summary>
/// <remarks>
/// This exception is used by all ServiceProxy implementations to represent failures in external service calls.
/// It includes error categorization and optional retry information to help consumers handle specific failure scenarios.
/// </remarks>
[Serializable]
public class ProxyException : Exception
{
    /// <summary>
    /// Error code categorizing the type of proxy failure
    /// </summary>
    /// <remarks>
    /// Common error codes:
    /// - TIMEOUT: Operation exceeded configured timeout
    /// - SERVICE_UNAVAILABLE: External service is unreachable
    /// - RATE_LIMITED: Rate limit exceeded by external service
    /// - INVALID_REQUEST: Request validation failed
    /// - UNAUTHORIZED: Authentication with external service failed
    /// - CIRCUIT_BREAKER_OPEN: Circuit breaker prevented the call
    /// - UNKNOWN: Unexpected error
    /// </remarks>
    public string ErrorCode { get; }

    /// <summary>
    /// Number of retry attempts made before the error occurred
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Elapsed time in milliseconds for the failed operation
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// External service that failed
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// HTTP status code if applicable
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Raw response from external service if available
    /// </summary>
    public string? ServiceResponse { get; set; }

    /// <summary>
    /// Initializes a new instance of the ProxyException class
    /// </summary>
    /// <param name="message">Error message describing what went wrong</param>
    /// <param name="errorCode">Error code categorizing the failure (default: UNKNOWN)</param>
    /// <param name="innerException">Inner exception that caused this error (optional)</param>
    public ProxyException(string message, string errorCode = "UNKNOWN", Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? "UNKNOWN";
    }

    /// <summary>
    /// Initializes a new instance of the ProxyException class (serialization constructor)
    /// </summary>
    /// <param name="info">Serialization info</param>
    /// <param name="context">Streaming context</param>
#pragma warning disable SYSLIB0051
    protected ProxyException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ErrorCode = info.GetString(nameof(ErrorCode)) ?? "UNKNOWN";
        RetryAttempts = info.GetInt32(nameof(RetryAttempts));
        ElapsedMilliseconds = info.GetInt64(nameof(ElapsedMilliseconds));
        ServiceName = info.GetString(nameof(ServiceName));
        HttpStatusCode = info.GetValue(nameof(HttpStatusCode), typeof(int?)) as int?;
        ServiceResponse = info.GetString(nameof(ServiceResponse));
#pragma warning restore SYSLIB0051
    }

    /// <summary>
    /// Gets object data for serialization
    /// </summary>
    /// <param name="info">Serialization info</param>
    /// <param name="context">Streaming context</param>
    [System.Obsolete("This API supports obsolete formatter-based serialization and should not be used.", false)]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
            throw new ArgumentNullException(nameof(info));

        base.GetObjectData(info, context);
        info.AddValue(nameof(ErrorCode), ErrorCode);
        info.AddValue(nameof(RetryAttempts), RetryAttempts);
        info.AddValue(nameof(ElapsedMilliseconds), ElapsedMilliseconds);
        info.AddValue(nameof(ServiceName), ServiceName);
        info.AddValue(nameof(HttpStatusCode), HttpStatusCode);
        info.AddValue(nameof(ServiceResponse), ServiceResponse);
    }

    /// <summary>
    /// Creates a formatted summary of the exception with all relevant details
    /// </summary>
    /// <returns>A detailed string representation of the proxy exception</returns>
    public override string ToString()
    {
        var parts = new List<string>
        {
            base.ToString(),
            $"ErrorCode: {ErrorCode}",
            $"RetryAttempts: {RetryAttempts}",
            $"ElapsedMilliseconds: {ElapsedMilliseconds}"
        };

        if (!string.IsNullOrEmpty(ServiceName))
            parts.Add($"Service: {ServiceName}");

        if (HttpStatusCode.HasValue)
            parts.Add($"HttpStatus: {HttpStatusCode}");

        if (!string.IsNullOrEmpty(ServiceResponse))
            parts.Add($"ServiceResponse: {ServiceResponse[..Math.Min(200, ServiceResponse.Length)]}");

        return string.Join(Environment.NewLine, parts);
    }
}
