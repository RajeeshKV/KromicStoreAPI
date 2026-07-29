using KromicStore.API.Authorization;
using KromicStore.Application.Interfaces;
using KromicStore.Infrastructure.Proxies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicStore.API.Controllers;

/// <summary>
/// Controller for media/file upload and management.
/// Handles image uploads to Cloudinary storage.
/// </summary>
[ApiController]
[Route("api/v1/media")]
[Authorize]
public class MediaController : BaseController
{
    private readonly MediaProxy _mediaProxy;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        ITenantProvider tenantProvider,
        MediaProxy mediaProxy,
        ILogger<MediaController> logger)
        : base(tenantProvider)
    {
        _mediaProxy = mediaProxy ?? throw new ArgumentNullException(nameof(mediaProxy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads a single image file to Cloudinary.
    /// </summary>
    /// <param name="file">The image file to upload.</param>
    /// <param name="folder">Optional folder path (defaults to tenant-specific folder).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Upload response with URL and metadata.</returns>
    /// <response code="200">File uploaded successfully.</response>
    /// <response code="400">Invalid request or file too large.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">Server error during upload.</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(MediaUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadImage(
        IFormFile file,
        [FromQuery] string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided or file is empty" });
            }

            // Validate file type (images only)
            if (!IsImageFile(file))
            {
                return BadRequest(new { error = "Only image files are allowed" });
            }

            // Validate file size (max 10MB for API uploads)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = $"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB" });
            }

            // Construct folder path with TenantId prefix
            var folderPath = string.IsNullOrEmpty(folder)
                ? $"{CurrentTenantId}/uploads"
                : $"{CurrentTenantId}/{folder}";

            using var stream = file.OpenReadStream();
            var result = await _mediaProxy.UploadAsync(
                stream,
                file.FileName,
                folderPath,
                null,
                cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                var errorMessage = result.Exception?.Message ?? "Upload failed";
                _logger.LogError("Image upload failed: {Error}", errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = errorMessage });
            }

            var response = new MediaUploadResponse(
                result.Data.SecureUrl,
                result.Data.PublicId,
                file.Length,
                file.ContentType);

            _logger.LogInformation(
                "Image uploaded successfully for tenant {TenantId}: {PublicId}",
                CurrentTenantId,
                result.Data.PublicId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while uploading the image" });
        }
    }

    /// <summary>
    /// Uploads multiple image files to Cloudinary in bulk.
    /// </summary>
    /// <param name="files">The image files to upload.</param>
    /// <param name="folder">Optional folder path (defaults to tenant-specific folder).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of upload results for each file.</returns>
    /// <response code="200">Files uploaded successfully (or partially successful).</response>
    /// <response code="400">Invalid request or no files provided.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">Server error during upload.</response>
    [HttpPost("upload/bulk")]
    [ProducesResponseType(typeof(BulkUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadImagesBulk(
        IFormFileCollection files,
        [FromQuery] string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { error = "No files provided" });
            }

            // Validate all files
            const long maxFileSize = 10 * 1024 * 1024;
            var fileStreams = new List<(Stream Stream, string FileName)>();

            foreach (var file in files)
            {
                if (!IsImageFile(file))
                {
                    return BadRequest(new { error = $"File '{file.FileName}' is not an image" });
                }

                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { error = $"File '{file.FileName}' exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB" });
                }

                fileStreams.Add((file.OpenReadStream(), file.FileName));
            }

            // Construct folder path with TenantId prefix
            var folderPath = string.IsNullOrEmpty(folder)
                ? $"{CurrentTenantId}/uploads"
                : $"{CurrentTenantId}/{folder}";

            var results = await _mediaProxy.BulkUploadAsync(
                fileStreams,
                folderPath,
                null,
                cancellationToken);

            var response = new BulkUploadResponse
            {
                TotalCount = results.Count(),
                SuccessCount = results.Count(r => r.IsSuccess),
                FailureCount = results.Count(r => !r.IsSuccess),
                Results = results.Select(r => new MediaUploadResult
                {
                    FileName = r.FileName,
                    IsSuccess = r.IsSuccess,
                    Url = r.Url,
                    PublicId = r.PublicId,
                    Error = r.Error
                }).ToList()
            };

            _logger.LogInformation(
                "Bulk image upload completed for tenant {TenantId}: {SuccessCount}/{TotalCount} successful",
                CurrentTenantId,
                response.SuccessCount,
                response.TotalCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk image upload for tenant {TenantId}", CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred during bulk upload" });
        }
    }

    /// <summary>
    /// Deletes an image from Cloudinary.
    /// </summary>
    /// <param name="publicId">Cloudinary public ID of the image to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deletion result.</returns>
    /// <response code="200">Image deleted successfully.</response>
    /// <response code="400">Invalid public ID.</response>
    /// <response code="401">User is not authenticated.</response>
    /// <response code="500">Server error during deletion.</response>
    [HttpDelete("{publicId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteImage(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return BadRequest(new { error = "Public ID is required" });
            }

            var result = await _mediaProxy.DeleteAsync(publicId, "image", cancellationToken);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Exception?.Message ?? "Deletion failed";
                _logger.LogError("Image deletion failed for {PublicId}: {Error}", publicId, errorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = errorMessage });
            }

            _logger.LogInformation(
                "Image deleted successfully for tenant {TenantId}: {PublicId}",
                CurrentTenantId,
                publicId);

            return Ok(new { message = "Image deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {PublicId} for tenant {TenantId}", publicId, CurrentTenantId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while deleting the image" });
        }
    }

    /// <summary>
    /// Checks if a file is an image based on content type.
    /// </summary>
    private static bool IsImageFile(IFormFile file)
    {
        var allowedContentTypes = new[]
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp",
            "image/bmp"
        };

        return allowedContentTypes.Contains(file.ContentType.ToLowerInvariant());
    }
}

/// <summary>
/// Media upload response DTO.
/// </summary>
public record MediaUploadResponse(
    string Url,
    string PublicId,
    long FileSize,
    string ContentType);

/// <summary>
/// Bulk upload response DTO.
/// </summary>
public record BulkUploadResponse
{
    public int TotalCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<MediaUploadResult> Results { get; init; } = new();
}

/// <summary>
/// Individual file upload result.
/// </summary>
public record MediaUploadResult
{
    public string FileName { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public string? Url { get; init; }
    public string? PublicId { get; init; }
    public string? Error { get; init; }
}
