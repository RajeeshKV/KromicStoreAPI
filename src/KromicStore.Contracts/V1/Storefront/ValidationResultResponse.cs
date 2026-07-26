namespace KromicStore.Contracts.V1.Storefront;

/// <summary>
/// Response object representing the result of storefront validation.
/// </summary>
public class ValidationResultResponse
{
    /// <summary>Gets a value indicating whether the storefront passed validation.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets the list of validation errors (if any).</summary>
    public List<string> Errors { get; set; } = new();
}
