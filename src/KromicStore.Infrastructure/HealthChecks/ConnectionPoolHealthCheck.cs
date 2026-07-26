namespace KromicStore.Infrastructure.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Data;

/// <summary>
/// Health check for database connection pool status.
/// Verifies that the connection pool is healthy and can serve requests.
/// </summary>
public class ConnectionPoolHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of ConnectionPoolHealthCheck.
    /// </summary>
    public ConnectionPoolHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Checks the health of the database connection pool.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt a simple query to verify connection
            await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "Status", "Healthy" },
                { "Timestamp", DateTime.UtcNow },
                { "Database", _dbContext.Database.GetConnectionString() ?? "Unknown" }
            };

            return HealthCheckResult.Healthy("Database connection pool is healthy", data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded(
                "Database connection pool check was cancelled (possible timeout)");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Database connection pool is unhealthy: {ex.Message}",
                ex);
        }
    }
}
