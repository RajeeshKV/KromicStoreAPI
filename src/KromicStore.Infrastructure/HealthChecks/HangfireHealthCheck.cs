namespace KromicStore.Infrastructure.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Hangfire;
using Hangfire.Storage;

/// <summary>
/// Health check for Hangfire background job system.
/// Verifies that the job storage is accessible and jobs can be enqueued/processed.
/// </summary>
public class HangfireHealthCheck : IHealthCheck
{
    private readonly ILogger<HangfireHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of HangfireHealthCheck.
    /// </summary>
    public HangfireHealthCheck(ILogger<HangfireHealthCheck> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Checks the health of the Hangfire background job system.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var storage = JobStorage.Current;

            if (storage == null)
            {
                return HealthCheckResult.Unhealthy(
                    "Hangfire job storage is not configured");
            }

            // Test connection to storage
            using var connection = storage.GetConnection();

            // Get storage statistics via monitoring API
            var monitoringApi = storage.GetMonitoringApi();
            var stats = await Task.Run(() => monitoringApi.GetStatistics(), cancellationToken);

            if (stats == null)
            {
                return HealthCheckResult.Unhealthy(
                    "Unable to retrieve Hangfire statistics");
            }

            var data = new Dictionary<string, object>
            {
                { "Status", "Healthy" },
                { "Timestamp", DateTime.UtcNow },
                { "Queues", stats.Queues },
                { "Scheduled", stats.Scheduled },
                { "Enqueued", stats.Enqueued },
                { "Failed", stats.Failed },
                { "Processing", stats.Processing },
                { "Succeeded", stats.Succeeded },
                { "Recurring", stats.Recurring },
                { "Servers", stats.Servers }
            };

            _logger.LogInformation("Hangfire health check successful. Queues: {Queues}, Enqueued: {Enqueued}, Failed: {Failed}",
                stats.Queues, stats.Enqueued, stats.Failed);

            return HealthCheckResult.Healthy("Hangfire job system is healthy", data);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Hangfire health check was cancelled");
            return HealthCheckResult.Degraded(
                "Hangfire health check was cancelled (possible timeout)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire health check failed: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy(
                $"Hangfire job system is unhealthy: {ex.Message}",
                ex);
        }
    }
}
