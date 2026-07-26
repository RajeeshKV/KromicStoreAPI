namespace KromicStore.Application.Interfaces;

/// <summary>
/// Service interface for storefront creation operations.
/// Provides methods for creating storefronts from themes or from scratch, and validating mandatory fields.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether validation passed (no errors).
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the list of validation error messages.
    /// </summary>
    public List<string> Errors { get; private set; }

    /// <summary>
    /// Initializes a new instance of the ValidationResult class.
    /// </summary>
    private ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors ?? new List<string>();
    }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A ValidationResult indicating success.</returns>
    public static ValidationResult CreateSuccess()
    {
        return new ValidationResult(true, new List<string>());
    }

    /// <summary>
    /// Creates a failed validation result with one or more errors.
    /// </summary>
    /// <param name="errors">The validation error messages.</param>
    /// <returns>A ValidationResult indicating failure with the provided errors.</returns>
    public static ValidationResult CreateFailure(params string[] errors)
    {
        if (errors == null || errors.Length == 0)
            throw new ArgumentException("At least one error must be provided for a failure result.", nameof(errors));

        return new ValidationResult(false, errors.ToList());
    }

    /// <summary>
    /// Creates a failed validation result with a list of errors.
    /// </summary>
    /// <param name="errors">The list of validation error messages.</param>
    /// <returns>A ValidationResult indicating failure with the provided errors.</returns>
    public static ValidationResult CreateFailure(List<string> errors)
    {
        if (errors == null || errors.Count == 0)
            throw new ArgumentException("At least one error must be provided for a failure result.", nameof(errors));

        return new ValidationResult(false, errors);
    }
}

public interface IStorefrontCreationService
{
    /// <summary>
    /// Creates a storefront from a theme template.
    /// Loads the theme, clones its pages/sections/components, populates default data, and saves transactionally.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="themeId">The theme ID to use as template.</param>
    /// <param name="storeName">The name for the new storefront.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the created storefront.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when theme is not found or theme JSON is invalid.</exception>
    Task<Guid> CreateFromThemeAsync(
        Guid tenantId,
        Guid themeId,
        string storeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an empty storefront from scratch (without a theme).
    /// Creates a storefront with a default home page and populates default data.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="storeName">The name for the new storefront.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the created storefront.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    Task<Guid> CreateFromScratchAsync(
        Guid tenantId,
        string storeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all mandatory storefront fields are provided (not placeholders).
    /// </summary>
    /// <param name="storefrontId">The storefront ID to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A ValidationResult with validation status and missing field names if validation fails.</returns>
    /// <exception cref="ArgumentException">Thrown when storefront ID is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when storefront is not found.</exception>
    Task<ValidationResult> ValidateMandatoryFieldsAsync(
        Guid storefrontId,
        CancellationToken cancellationToken = default);
}
