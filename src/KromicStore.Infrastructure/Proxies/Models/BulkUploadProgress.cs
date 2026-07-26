#nullable disable

namespace KromicStore.Infrastructure.Proxies.Models;

/// <summary>
/// Progress information for bulk upload operations
/// </summary>
public class BulkUploadProgress
{
    /// <summary>
    /// Total number of files to upload
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Number of files successfully uploaded
    /// </summary>
    public int UploadedCount { get; set; }

    /// <summary>
    /// Number of files that failed to upload
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Current progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage => TotalFiles == 0 ? 0 : (UploadedCount * 100) / TotalFiles;

    /// <summary>
    /// Overall status of the bulk upload operation
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Details of the current file being processed
    /// </summary>
    public string CurrentFileName { get; set; }

    /// <summary>
    /// Timestamp of the last update
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Create progress object for bulk upload initialization
    /// </summary>
    public static BulkUploadProgress Create(int totalFiles) => new()
    {
        TotalFiles = totalFiles,
        UploadedCount = 0,
        FailedCount = 0,
        Status = "Starting",
        LastUpdated = DateTime.UtcNow
    };

    /// <summary>
    /// Update progress for successful upload
    /// </summary>
    public void RecordSuccess(string fileName)
    {
        UploadedCount++;
        CurrentFileName = fileName;
        Status = "In Progress";
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Update progress for failed upload
    /// </summary>
    public void RecordFailure(string fileName, string error)
    {
        FailedCount++;
        CurrentFileName = $"{fileName} (Failed: {error})";
        Status = FailedCount > 0 ? "Partial Failures" : "In Progress";
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark bulk upload as completed
    /// </summary>
    public void Complete()
    {
        Status = FailedCount == 0 ? "Completed" : "Completed with Failures";
        LastUpdated = DateTime.UtcNow;
    }
}
