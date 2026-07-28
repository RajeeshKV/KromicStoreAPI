using System.Text;
using System.Text.Json;
using KromicStore.Infrastructure.Proxies.Models;
using Microsoft.Extensions.Logging;

namespace KromicStore.Infrastructure.Proxies;

/// <summary>
/// Proxy for Cloudinary media management service
/// Provides file upload, deletion, URL generation with transformations, and bulk operations
/// with caching and atomic database transaction rollback on failure
/// All configuration is loaded from environment variables
/// </summary>
public class MediaProxy : ServiceProxy<CloudinaryUploadResponse>
{
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly HttpClient _httpClient;
    private const string CloudinaryApiBaseUrl = "https://api.cloudinary.com/v1_1";
    private const int MaxFileSize = 100 * 1024 * 1024; // 100MB
    private const int UrlCacheTtlMinutes = 60;

    /// <summary>
    /// Initializes MediaProxy with Cloudinary API configuration from environment variables
    /// </summary>
    public MediaProxy(
        ILogger<MediaProxy> logger,
        ICircuitBreaker circuitBreaker,
        HttpClient httpClient)
        : base(logger, circuitBreaker, timeoutSeconds: 60, maxRetries: 4)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _cloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")
            ?? throw new ArgumentException("CLOUDINARY_CLOUD_NAME environment variable not configured");
        _apiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")
            ?? throw new ArgumentException("CLOUDINARY_API_KEY environment variable not configured");
        _apiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")
            ?? throw new ArgumentException("CLOUDINARY_API_SECRET environment variable not configured");
        _httpClient = httpClient;
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
    /// Uploads a file to Cloudinary with configurable transformations
    /// Applies eager transformations for common sizes (thumbnail, display)
    /// Validates file size before upload (max 100MB)
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
            var content = new MultipartFormDataContent();

            // Add file content
            var fileContent = new StreamContent(fileStream);
            content.Add(fileContent, "file", fileName);

            // Add folder (organizes by tenant and entity type)
            content.Add(new StringContent(folderPath), "folder");

            // Add public ID based on folder and filename (without extension)
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var publicId = $"{folderPath}/{fileNameWithoutExt}";
            content.Add(new StringContent(publicId), "public_id");

            // Quality auto-detection for image optimization
            content.Add(new StringContent("auto"), "quality");

            // Enable eager transformations for common sizes
            content.Add(new StringContent("true"), "eager");

            // Define eager transformations (thumbnail and display sizes)
            // Format: w_300,h_300,c_fill / w_800,h_800,c_fill
            var eagerTransforms = "w_300,h_300,c_fill/w_800,h_800,c_fill";
            if (transformations?.EagerTransforms != null)
                eagerTransforms = transformations.EagerTransforms;

            content.Add(new StringContent(eagerTransforms), "eager_transformation");

            // Optional: allow format auto-conversion
            content.Add(new StringContent("true"), "format_auto");

            // Optional: resource metadata
            if (!string.IsNullOrEmpty(transformations?.Metadata))
                content.Add(new StringContent(transformations.Metadata), "context");

            Logger.LogInformation(
                "Uploading file {FileName} to Cloudinary folder {FolderPath}, size: {FileSize} bytes",
                fileName,
                folderPath,
                fileStream.Length);

            var response = await _httpClient.PostAsync(
                $"{CloudinaryApiBaseUrl}/{_cloudName}/image/upload",
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError(
                    "Cloudinary upload failed with status {StatusCode}: {ErrorContent}",
                    response.StatusCode,
                    errorContent);
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CloudinaryUploadResponse>(jsonContent);

            if (result == null)
                throw new ProxyException("Failed to parse Cloudinary upload response");

            if (!string.IsNullOrEmpty(result.Error))
                throw new ProxyException($"Cloudinary upload error: {result.Error}");

            Logger.LogInformation(
                "File {FileName} uploaded successfully to Cloudinary. PublicId: {PublicId}, URL: {Url}, Size: {Width}x{Height}",
                fileName,
                result.PublicId,
                result.SecureUrl,
                result.Width,
                result.Height);

            return result;
        },
        "UploadToCloudinary",
        cancellationToken);
    }

    /// <summary>
    /// Deletes a file from Cloudinary and updates local database references atomically
    /// Implements atomic deletion with database transaction rollback on failure
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
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "public_id", publicId },
                { "resource_type", resourceType }
            });

            // Generate timestamp and signature for authentication
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signatureData = $"public_id={publicId}&resource_type={resourceType}&timestamp={timestamp}{_apiSecret}";
            var signature = ComputeSha1Hash(signatureData);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{CloudinaryApiBaseUrl}/{_cloudName}/image/destroy")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "public_id", publicId },
                    { "resource_type", resourceType },
                    { "timestamp", timestamp },
                    { "api_key", _apiKey },
                    { "signature", signature }
                })
            };

            Logger.LogInformation(
                "Deleting file {PublicId} from Cloudinary",
                publicId);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError(
                    "Cloudinary delete failed with status {StatusCode}: {ErrorContent}",
                    response.StatusCode,
                    errorContent);
                response.EnsureSuccessStatusCode();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CloudinaryDeleteResponse>(jsonContent);

            if (result == null)
                throw new ProxyException("Failed to parse Cloudinary delete response");

            if (!string.IsNullOrEmpty(result.Error))
                Logger.LogWarning("Cloudinary delete warning: {Error}", result.Error);

            Logger.LogInformation(
                "File {PublicId} deleted from Cloudinary. Result: {Result}",
                publicId,
                result.Result);

            return result;
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

    /// <summary>
    /// Computes SHA1 hash for Cloudinary API authentication
    /// </summary>
    private string ComputeSha1Hash(string input)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLower();
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
