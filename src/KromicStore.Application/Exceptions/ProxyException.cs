#nullable disable

namespace KromicStore.Application.Exceptions;

/// <summary>
/// Exception thrown when an external service proxy operation fails
/// </summary>
public class ProxyException : ApplicationException
{
    /// <summary>
    /// The error code from the external service
    /// </summary>
    public string ExternalErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of ProxyException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errorCode">Machine-readable error code</param>
    /// <param name="externalErrorCode">Error code from external service</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="innerException">Inner exception</param>
    public ProxyException(
        string message,
        string errorCode = "PROXY_ERROR",
        string externalErrorCode = null,
        int statusCode = 502,
        Exception innerException = null)
        : base(message, errorCode, statusCode, innerException)
    {
        ExternalErrorCode = externalErrorCode;
    }
}
