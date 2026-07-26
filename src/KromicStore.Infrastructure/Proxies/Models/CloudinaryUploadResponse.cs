#nullable disable

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Response from Cloudinary upload operation
/// </summary>
public class CloudinaryUploadResponse
{
    /// <summary>
    /// Public identifier of the uploaded resource (includes folder path)
    /// </summary>
    public string PublicId { get; set; }

    /// <summary>
    /// HTTP URL of the uploaded resource
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// HTTPS URL of the uploaded resource
    /// </summary>
    public string SecureUrl { get; set; }

    /// <summary>
    /// Width of the uploaded image in pixels
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the uploaded image in pixels
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Bytes { get; set; }

    /// <summary>
    /// Media type (e.g., "image/jpeg")
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// Resource type (e.g., "image", "video", "raw")
    /// </summary>
    public string ResourceType { get; set; }

    /// <summary>
    /// Timestamp when the resource was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Version of the resource (for cache busting)
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Derived images created during upload (thumbnails, etc.)
    /// </summary>
    public Dictionary<string, string> EagerResults { get; set; } = new();

    /// <summary>
    /// Unique identifier assigned by Cloudinary
    /// </summary>
    public string AssetId { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string Error { get; set; }
}
