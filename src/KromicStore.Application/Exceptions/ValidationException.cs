namespace KromicStore.Application.Exceptions;

using FluentValidation.Results;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public Dictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Creates a new instance of ValidationException.
    /// </summary>
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures have occurred.")
    {
        Errors = failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());
    }

    /// <summary>
    /// Creates a new instance of ValidationException with a single error message.
    /// </summary>
    public ValidationException(string propertyName, string message)
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { message } }
        };
    }
}
