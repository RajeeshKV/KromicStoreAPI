namespace KromicStore.Contracts.V1.Storefront;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request to clone an existing theme for customization.
/// </summary>
public class CloneThemeRequest
{
    /// <summary>Gets or sets the optional new name for the cloned theme. If not provided, uses "{OriginalName} (Clone)".</summary>
    [StringLength(255, ErrorMessage = "Theme name cannot exceed 255 characters")]
    public string? NewName { get; set; }
}
