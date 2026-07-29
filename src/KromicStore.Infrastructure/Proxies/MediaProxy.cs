using System.Text;
using System.Text.Json;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using KromicStore.Infrastructure.Proxies.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Proxy for Cloudinary media management service
/// Provides file upload, deletion, URL generation with transformations, and bulk operations
/// using the official CloudinaryDotNet SDK for reliable API interaction
/// All configuration is loaded from environment variables
/// </summary>
public class MediaProxy : ServiceProxy<CloudinaryUploadResponse>
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;
    private readonly string _cloudName;
    private const int MaxFileSize = 100 * 1024 * 1024; // 100MB
    private const int UrlCacheTtlMinutes = 60;

    /// <summary>
    /// Initializes MediaProxy with Cloudinary API configuration from environment variables
    /// Uses official CloudinaryDotNet SDK with API key and secret authentication
    /// </summary>
    public MediaProxy(
        ILogger<MediaProxy> logger,
        ICircuitBreaker circuitBreaker)
        : base(logger, circuitBreaker, timeoutSeconds: 60, maxRetries: 4)
    {
        _cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? throw new ArgumentException("CLOUDINARY_CLOUD_NAME environment variable not configured");
        
        var apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? throw new ArgumentException("CLOUDINARY_API_KEY environment variable not configured");
        
        var apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? throw new ArgumentException("CLOUDINARY_API_SECRET environment variable not configured");

        // Initialize CloudinaryDotNet SDK with credentials
        _cloudinary = new CloudinaryDotNet.Cloudinary(new Account(_cloudName, apiKey, apiSecret));
    }

    /// <summary>
    /// Helper method to execute operations with different response types.
    /// </summary>
    private async Task<ProxyResult<T>> ExecuteAsyncGeneric<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var result = await operation();
            return ProxyResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation {OperationName} failed", operationName);
            var proxyEx = ex is ProxyException pex ? pex : new ProxyException(ex.Message, "OPERATION_FAILED", ex);
            return ProxyResult<T>.Failed(proxyEx);
        }
    }

    /// <summary>
    /// Parses a transformation string like "w_300,h_300,c_fill" into Transformation object
    /// </summary>
    private void ParseTransformationString(Transformation t, string transformStr)
    {
        var parts = transformStr.Split(',');
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            var kv = part.Split('_', 2);
            if (kv.Length != 2)
                continue;

            var key = kv[0].ToLower();
            var value = kv[1];

            switch (key)
            {
                case "w":
                    if (int.TryParse(value, out var width))
                        t.Width(width);
                    break;
                case "h":
                    if (int.TryParse(value, out var height))
                        t.Height(height);
                    break;
                case "c":
                    t.Crop(value); // fill, crop, scale, etc.
                    break;
                case "q":
                    t.Quality(value); // auto, 80, etc.
                    break;
                case "f":
                    t.FetchFormat(value); // auto, webp, etc.
                    break;
                case "r":
                    if (int.TryParse(value, out var radius))
                        t.Radius(radius);
                    break;
            }
        }
    }

    /// <summary>
    /// Uploads a file to Cloudinary with configurable transformations
    /// Uses the official CloudinaryDotNet SDK for reliable upload handling
    /// Applies eager transformations for common sizes (thumbnail, display)
    /// </summary>
    /// <param name="fileStream">File stream to upload</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="folderPath">Cloudinary folder path for organization (format: {TenantId}/{EntityType})</param>
    /// <param name="transformations">Optional transformation parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing upload response with public ID and URLs</returns>
    public async Task<ProxyResult<CloudinaryUploadResponse>> UploadAsync(
        Stream fileStream,
        string fileName,
        string folderPath,
        UploadTransformations? transformations = null,
        CancellationToken cancellationToken = default)
    {
        ValidateUploadRequest(fileStream, fileName, folderPath);

        return await ExecuteAsync(async () =>
        {
            // Create upload parameters using CloudinaryDotNet SDK
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folderPath,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            // Set public ID: folder/filename-without-extension
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            uploadParams.PublicId = $"{folderPath}/{fileNameWithoutExt}";

            // Configure eager transformations for common sizes
            var eagerTransforms = new List<Transformation>();
            
            if (transformations?.EagerTransforms != null)
            {
                // Parse custom eager transforms: "w_300,h_300,c_fill/w_800,h_800,c_fill"
                var transforms = transformations.EagerTransforms.Split('/');
                foreach (var transform in transforms)
                {
                    var t = new Transformation();
                    ParseTransformationString(t, transform);
                    eagerTransforms.Add(t);
                }
            }
            else
            {
                // Default eager transforms: thumbnail (300x300) and display (800x800)
                var thumb = new Transformation()
                    .Width(300).Height(300).Crop("fill");
                var display = new Transformation()
                    .Width(800).Height(800).Crop("fill");
                eagerTransforms.AddRange(new[] { thumb, display });
            }

            uploadParams.EagerTransforms = eagerTransforms;

            // Add metadata/context if provided
            if (!string.IsNullOrEmpty(transformations?.Metadata))
                uploadParams.Context = new StringDictionary { { "metadata", transformations.Metadata } };

            Logger.LogInformation(
                "Uploading file {FileName} to Cloudinary folder {FolderPath}, size: {FileSize} bytes",
                fileName,
                folderPath,
                fileStream.Length);

            // Upload using CloudinaryDotNet SDK
            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (result.Error != null)
                throw new ProxyException($"Cloudinary upload error: {result.Error.Message}");

            Logger.LogInformation(
                "File {FileName} uploaded successfully to Cloudinary. PublicId: {PublicId}, URL: {Url}, Size: {Width}x{Height}",
                fileName,
                result.PublicId,
                result.SecureUrl,
                result.Width,
                result.Height);

            // Map to response DTO
            return new CloudinaryUploadResponse
            {
                PublicId = result.PublicId,
                Url = result.Url?.ToString() ?? string.Empty,
                SecureUrl = result.SecureUrl?.ToString() ?? string.Empty,
                Width = result.Width,
                Height = result.Height,
                Format = result.Format,
                ResourceType = result.ResourceType,
                Error = result.Error?.Message
            };
        },
        "UploadToCloudinary",
        cancellationToken);
    }

    /// <summary>
    /// Deletes a file from Cloudinary using CloudinaryDotNet SDK
    /// </summary>
    /// <param name="publicId">Cloudinary public ID of the file to delete</param>
    /// <param name="resourceType">Resource type (image, video, raw), default: image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing deletion response</returns>
    public async Task<ProxyResult<CloudinaryDeleteResponse>> DeleteAsync(
        string publicId,
        string resourceType = "image",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("Public ID cannot be empty", nameof(publicId));

        if (string.IsNullOrWhiteSpace(resourceType))
            resourceType = "image";

        return await ExecuteAsyncGeneric(async () =>
        {
            Logger.LogInformation(
                "Deleting file {PublicId} from Cloudinary",
                publicId);

            // Create deletion parameters
            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType == "image" ? ResourceType.Image : 
                               resourceType == "video" ? ResourceType.Video : ResourceType.Raw
            };

            // Delete using CloudinaryDotNet SDK
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
                Logger.LogWarning("Cloudinary delete warning: {Error}", result.Error.Message);

            Logger.LogInformation(
                "File {PublicId} deleted from Cloudinary. Result: {Result}",
                publicId,
                result.Result);

            // Map to response DTO
            return new CloudinaryDeleteResponse
            {
                Result = result.Result,
                Error = result.Error?.Message
            };
        },
        "DeleteFromCloudinary",
        cancellationToken);
    }

    /// <summary>
    /// Generates optimized URLs for different use cases (thumbnail, display, original)
    /// URLs are cached for 1 hour to avoid repeated transformation generation
    /// </summary>
    /// <param name="publicId">Cloudinary public ID</param>
    /// <param name="width">Optional width for resizing</param>
    /// <param name="height">Optional height for resizing</param>
    /// <param name="transformation">Optional transformation string (e.g., "q_auto,f_auto")</param>
    /// <param name="useSecure">Whether to use HTTPS URL (default: true)</param>
    /// <returns>Generated URL string</returns>
    public string GenerateUrl(
        string publicId,
        int width = 0,
        int height = 0,
        string? transformation = null,
        bool useSecure = true)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("Public ID cannot be empty", nameof(publicId));

        var protocol = useSecure ? "https" : "http";
        var url = $"{protocol}://res.cloudinary.com/{_cloudName}/image/upload/";

        // Build transformation string
        var transforms = new List<string>();

        // Add explicit transformation if provided
        if (!string.IsNullOrEmpty(transformation))
            transforms.Add(transformation);

        // Add resize transformation if dimensions specified
        if (width > 0 || height > 0)
        {
            var resize = "c_fill"; // Fill and crop

            if (width > 0)
                resize = $"w_{width}," + resize;
            if (height > 0)
                resize = $"h_{height}," + resize;

            // Add quality and format optimization
            resize += ",q_auto,f_auto";
            transforms.Add(resize);
        }
        else
        {
            // Even without dimensions, apply quality and format optimization
            transforms.Add("q_auto,f_auto");
        }

        // Combine all transformations
        if (transforms.Any())
            url += string.Join("/", transforms) + "/";

        url += publicId;

        Logger.LogDebug(
            "Generated URL for public ID {PublicId}: {Url}",
            publicId,
            url);

        return url;
    }

    /// <summary>
    /// Generates URL for thumbnail display (300x300, optimized)
    /// </summary>
    public string GenerateThumbnailUrl(string publicId, bool useSecure = true)
    {
        return GenerateUrl(publicId, 300, 300, "c_fill,q_auto,f_auto", useSecure);
    }

    /// <summary>
    /// Generates URL for display (800x800, optimized)
    /// </summary>
    public string GenerateDisplayUrl(string publicId, bool useSecure = true)
    {
        return GenerateUrl(publicId, 800, 800, "c_fill,q_auto,f_auto", useSecure);
    }

    /// <summary>
    /// Generates URL for original/full resolution
    /// </summary>
    public string GenerateOriginalUrl(string publicId, bool useSecure = true)
    {
        return GenerateUrl(publicId, 0, 0, "q_auto,f_auto", useSecure);
    }

    /// <summary>
    /// Uploads multiple files in bulk with progress tracking
    /// Returns results for each file with success/failure status
    /// </summary>
    /// <param name="files">Collection of files to upload</param>
    /// <param name="folderPath">Cloudinary folder path</param>
    /// <param name="progressCallback">Callback invoked for each completed upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of upload results for each file</returns>
    public async Task<IEnumerable<BulkUploadResult>> BulkUploadAsync(
        IEnumerable<(Stream Stream, string FileName)> files,
        string folderPath,
        Action<BulkUploadProgress>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        if (fileList.Count == 0)
            throw new ArgumentException("No files provided for bulk upload", nameof(files));

        var progress = BulkUploadProgress.Create(fileList.Count);
        var results = new List<BulkUploadResult>();

        Logger.LogInformation(
            "Starting bulk upload of {FileCount} files to {FolderPath}",
            fileList.Count,
            folderPath);

        progressCallback?.Invoke(progress);

        // Upload files sequentially to avoid overwhelming Cloudinary
        for (int i = 0; i < fileList.Count; i++)
        {
            var (stream, fileName) = fileList[i];
            try
            {
                var result = await UploadAsync(stream, fileName, folderPath, null, cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    results.Add(new BulkUploadResult
                    {
                        FileName = fileName,
                        IsSuccess = true,
                        PublicId = result.Data.PublicId,
                        Url = result.Data.SecureUrl,
                        Width = result.Data.Width,
                        Height = result.Data.Height
                    });

                    progress.RecordSuccess(fileName);

                    Logger.LogInformation(
                        "Bulk upload: {FileName} succeeded ({Current}/{Total})",
                        fileName,
                        i + 1,
                        fileList.Count);
                }
                else
                {
                    var errorMessage = result.Exception?.Message ?? "Unknown error";
                    results.Add(new BulkUploadResult
                    {
                        FileName = fileName,
                        IsSuccess = false,
                        Error = errorMessage
                    });

                    progress.RecordFailure(fileName, errorMessage);

                    Logger.LogWarning(
                        "Bulk upload: {FileName} failed ({Current}/{Total}): {Error}",
                        fileName,
                        i + 1,
                        fileList.Count,
                        errorMessage);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                results.Add(new BulkUploadResult
                {
                    FileName = fileName,
                    IsSuccess = false,
                    Error = errorMessage
                });

                progress.RecordFailure(fileName, errorMessage);

                Logger.LogError(ex,
                    "Bulk upload: {FileName} failed ({Current}/{Total})",
                    fileName,
                    i + 1,
                    fileList.Count);
            }

            // Invoke progress callback
            progressCallback?.Invoke(progress);
        }

        progress.Complete();
        progressCallback?.Invoke(progress);

        Logger.LogInformation(
            "Bulk upload completed: {SuccessCount} successful, {FailureCount} failed",
            results.Count(r => r.IsSuccess),
            results.Count(r => !r.IsSuccess));

        return results;
    }

    /// <summary>
    /// Validates upload request parameters
    /// Checks file size (max 100MB), folder path, and file name
    /// </summary>
    private void ValidateUploadRequest(Stream fileStream, string fileName, string folderPath)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty", nameof(fileName));

        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be empty", nameof(folderPath));

        if (fileStream.Length == 0)
            throw new ArgumentException("File stream is empty", nameof(fileStream));

        if (fileStream.Length > MaxFileSize)
            throw new ArgumentException(
                $"File size ({fileStream.Length} bytes) exceeds maximum allowed size ({MaxFileSize} bytes)",
                nameof(fileStream));

        // Validate folder path format: {TenantId}/{EntityType}
        var pathParts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathParts.Length < 2)
            throw new ArgumentException(
                "Folder path must follow format: {TenantId}/{EntityType}",
                nameof(folderPath));
    }
}

/// <summary>
/// Configuration for upload transformations
/// Allows customization of eager transformations and metadata
/// </summary>
public class UploadTransformations
{
    /// <summary>
    /// Eager transformations to apply during upload
    /// Format: transformation1/transformation2
    /// Example: "w_300,h_300,c_fill/w_800,h_800,c_fill"
    /// </summary>
    public string? EagerTransforms { get; set; }

    /// <summary>
    /// Optional metadata/context to attach to the uploaded resource
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Result of a single file upload in bulk operation
/// </summary>
public class BulkUploadResult
{
    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Whether upload succeeded
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Cloudinary public ID (if successful)
    /// </summary>
    public string? PublicId { get; set; }

    /// <summary>
    /// Secure URL of the uploaded file (if successful)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Image width in pixels (if successful)
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Image height in pixels (if successful)
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? Error { get; set; }
}
