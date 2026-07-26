namespace KromicStore.Application.Interfaces;

/// <summary>
/// Interface for media/file management.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Uploads a file to cloud storage.
    /// </summary>
    /// <param name="fileStream">The file stream to upload.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="folderPath">The folder path in cloud storage.</param>
    Task<MediaUploadResponse> UploadAsync(Stream fileStream, string fileName, string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from cloud storage.
    /// </summary>
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a file URL.
    /// </summary>
    Task<string> GetFileUrlAsync(string fileKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a thumbnail for an image.
    /// </summary>
    Task<MediaUploadResponse> GenerateThumbnailAsync(string fileUrl, int width, int height, CancellationToken cancellationToken = default);
}

/// <summary>
/// Media upload response.
/// </summary>
public record MediaUploadResponse(
    string FileUrl,
    string FileKey,
    long FileSizeBytes,
    string ContentType);
