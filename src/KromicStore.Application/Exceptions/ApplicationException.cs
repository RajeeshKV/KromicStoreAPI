#nullable disable

namespace KromicStore.Application.Exceptions;

/// <summary>
/// Base exception class for application-specific exceptions
/// </summary>
public abstract class ApplicationException : Exception
{
    /// <summary>
    /// Machine-readable error code
    /// </summary>
    public virtual string ErrorCode { get; set; }

    /// <summary>
    /// HTTP status code to return
    /// </summary>
    public virtual int StatusCode { get; set; } = 500;

    /// <summary>
    /// Initializes a new instance of ApplicationException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errorCode">Machine-readable error code</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="innerException">Inner exception</param>
    protected ApplicationException(
        string message,
        string errorCode = null,
        int statusCode = 500,
        Exception innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Exception thrown when a resource is not found
/// </summary>
public class NotFoundException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of NotFoundException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errorCode">Machine-readable error code (default: NOT_FOUND)</param>
    public NotFoundException(string message, string errorCode = "NOT_FOUND")
        : base(message, errorCode, 404)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationFailureException : ApplicationException
{
    /// <summary>
    /// Validation errors
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of ValidationFailureException
    /// </summary>
    /// <param name="errors">Validation errors</param>
    public ValidationFailureException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.", "VALIDATION_ERROR", 400)
    {
        Errors = errors;
    }
}

/// <summary>
/// Exception thrown when an operation fails due to conflict
/// </summary>
public class ConflictException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of ConflictException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errorCode">Machine-readable error code (default: CONFLICT)</param>
    public ConflictException(string message, string errorCode = "CONFLICT")
        : base(message, errorCode, 409)
    {
    }
}

/// <summary>
/// Exception thrown when access is denied
/// </summary>
public class ForbiddenException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of ForbiddenException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errorCode">Machine-readable error code (default: FORBIDDEN)</param>
    public ForbiddenException(string message, string errorCode = "FORBIDDEN")
        : base(message, errorCode, 403)
    {
    }
}

/// <summary>
/// Exception thrown when an external service fails
/// </summary>
public class ExternalServiceException : ApplicationException
{
    /// <summary>
    /// Initializes a new instance of ExternalServiceException
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="serviceName">Name of the external service</param>
    /// <param name="errorCode">Machine-readable error code (default: EXTERNAL_SERVICE_ERROR)</param>
    /// <param name="innerException">Inner exception</param>
    public ExternalServiceException(
        string message,
        string serviceName = null,
        string errorCode = "EXTERNAL_SERVICE_ERROR",
        Exception innerException = null)
        : base($"{message} (Service: {serviceName})", errorCode, 503, innerException)
    {
    }
}
