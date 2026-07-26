#nullable disable

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Response from Cloudinary delete operation
/// </summary>
public class CloudinaryDeleteResponse
{
    /// <summary>
    /// Result of the deletion: "ok" or "not_found"
    /// </summary>
    public string Result { get; set; }

    /// <summary>
    /// Error message if deletion failed
    /// </summary>
    public string Error { get; set; }
}
