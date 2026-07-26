using System;
using System.Collections.Generic;

namespace KromicStore.Contracts.Abstractions
{
    /// <summary>
    /// Response class for API errors.
    /// Provides error details including code, message, and validation errors.
    /// </summary>
    public class ErrorResponse : ApiResponse
    {
        /// <summary>
        /// Gets or sets the error code.
        /// Typically a constant identifier for programmatic error handling (e.g., "VALIDATION_ERROR", "NOT_FOUND").
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// Human-readable description of the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets detailed validation errors.
        /// Dictionary mapping property names to arrays of error messages.
        /// Typically populated for validation errors (HTTP 400).
        /// </summary>
        public IDictionary<string, string[]> Details { get; set; }

        /// <summary>
        /// Initializes a new instance of the ErrorResponse class.
        /// </summary>
        public ErrorResponse()
        {
            Code = string.Empty;
            Message = string.Empty;
            Details = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the ErrorResponse class with error details.
        /// </summary>
        /// <param name="code">The error code identifying the type of error.</param>
        /// <param name="message">The human-readable error message.</param>
        /// <param name="details">Optional dictionary of validation details.</param>
        /// <exception cref="ArgumentNullException">Thrown when code or message is null or empty.</exception>
        public ErrorResponse(string code, string message, IDictionary<string, string[]>? details = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code), "Error code cannot be null or empty");
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message), "Error message cannot be null or empty");

            Code = code;
            Message = message;
            Details = details ?? new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Gets metadata about this error response.
        /// </summary>
        /// <returns>A dictionary containing error metadata.</returns>
        public override IDictionary<string, object> GetMetadata()
        {
            return new Dictionary<string, object>
            {
                { "Code", Code },
                { "Message", Message },
                { "DetailsCount", Details?.Count ?? 0 }
            };
        }
    }
}
