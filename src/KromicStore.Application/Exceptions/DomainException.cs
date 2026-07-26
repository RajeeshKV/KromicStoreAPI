namespace KromicStore.Application.Exceptions;

/// <summary>
/// Base exception for domain-level errors.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Creates a new instance of DomainException.
    /// </summary>
    public DomainException(string message, string errorCode = "DOMAIN_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a new instance of DomainException with an inner exception.
    /// </summary>
    public DomainException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
