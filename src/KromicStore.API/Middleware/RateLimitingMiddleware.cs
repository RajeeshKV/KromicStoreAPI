using KromicStore.API.Configuration;
using KromicStore.Application.Interfaces;
using KromicStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KromicStore.API.Middleware;

/// <summary>
/// Middleware for enforcing API rate limiting based on subscription plan.
/// Uses sliding window counter stored in Redis cache with per-minute granularity.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;

    /// <summary>
    /// Initializes a new instance of RateLimitingMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Configuration options for RateLimitingMiddleware</param>
    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        IOptions<RateLimitingOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new RateLimitingOptions();
    }

    /// <summary>
    /// Processes the HTTP request and checks rate limit based on subscription plan.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ITenantProvider tenantProvider,
        ICacheService cacheService,
        AppDbContext dbContext)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "UNKNOWN";

        // Skip rate limiting if disabled
        if (!_options.Enabled)
        {
            _logger.LogDebug(
                "Rate limiting is disabled - Path: {Path}, CorrelationId: {CorrelationId}",
                context.Request.Path,
                correlationId);
            
            await _next(context);
            return;
        }

        // Check if endpoint is in bypass paths
        if (IsPathInBypassList(context.Request.Path))
        {
            _logger.LogDebug(
                "Skipping rate limiting for bypass endpoint - Path: {Path}, CorrelationId: {CorrelationId}",
                context.Request.Path,
                correlationId);
            
            await _next(context);
            return;
        }

        var tenantId = tenantProvider.TenantId;

        // If tenant is not resolved, allow the request to proceed
        // (error will be handled by TenantResolutionMiddleware)
        if (tenantId == Guid.Empty)
        {
            _logger.LogDebug(
                "Tenant not resolved during rate limiting check, skipping - CorrelationId: {CorrelationId}",
                correlationId);
            
            await _next(context);
            return;
        }

        try
        {
            // Fetch tenant's subscription plan from database
            var tenant = await dbContext.Set<Domain.Entities.Tenant>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenantId == tenantId.ToString());

            if (tenant == null)
            {
                _logger.LogWarning(
                    "Tenant not found during rate limiting check - TenantId: {TenantId}, Path: {Path}, CorrelationId: {CorrelationId}",
                    tenantId,
                    context.Request.Path,
                    correlationId);
                
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    errorCode = "TENANT_NOT_FOUND",
                    message = "Tenant not found"
                });
                return;
            }

            // Get rate limit for subscription plan
            var planKey = tenant.SubscriptionPlan?.ToLowerInvariant() ?? "basic";
            if (!_options.RateLimitsByPlan.TryGetValue(planKey, out var rateLimit))
            {
                rateLimit = _options.DefaultRateLimit;
                
                _logger.LogWarning(
                    "Unknown subscription plan, using default limit - TenantId: {TenantId}, Plan: {Plan}, DefaultLimit: {DefaultLimit}, " +
                    "Path: {Path}, CorrelationId: {CorrelationId}",
                    tenantId,
                    planKey,
                    rateLimit,
                    context.Request.Path,
                    correlationId);
            }

            // Generate rate limit cache key using sliding window (current minute)
            var minuteWindow = DateTime.UtcNow.ToString("yyyyMMddHHmm");
            var rateLimitKey = $"ratelimit:{tenantId}:{minuteWindow}";

            // Get current request count and increment
            string? requestCountStr = null;
            int requestCount = 0;

            if (_options.UseDistributedCache)
            {
                requestCountStr = await cacheService.GetAsync<string>(rateLimitKey);
            }

            requestCount = int.TryParse(requestCountStr, out var count) ? count : 0;
            requestCount++;

            // Set cache key with configured time window expiration
            if (_options.UseDistributedCache)
            {
                await cacheService.SetAsync(
                    rateLimitKey,
                    requestCount.ToString(),
                    TimeSpan.FromMinutes(_options.TimeWindowMinutes));
            }

            // Check if rate limit exceeded
            if (requestCount > rateLimit)
            {
                var retryAfterSeconds = (60 - DateTime.UtcNow.Second);
                if (retryAfterSeconds <= 0)
                    retryAfterSeconds = 60;

                _logger.LogWarning(
                    "Rate limit exceeded - TenantId: {TenantId}, Plan: {Plan}, Limit: {Limit}, CurrentCount: {CurrentCount}, " +
                    "Path: {Path}, Method: {Method}, CorrelationId: {CorrelationId}, RetryAfter: {RetryAfter}s",
                    tenantId,
                    planKey,
                    rateLimit,
                    requestCount,
                    context.Request.Path,
                    context.Request.Method,
                    correlationId,
                    retryAfterSeconds);

                context.Response.StatusCode = _options.RateLimitExceededStatusCode;
                context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                context.Response.Headers["X-RateLimit-Limit"] = rateLimit.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = "0";
                context.Response.Headers["X-RateLimit-Reset"] = GetResetTime().ToString();

                var errorResponse = new
                {
                    code = "RATE_LIMIT_EXCEEDED",
                    message = $"Rate limit exceeded. Maximum {rateLimit} requests per {_options.TimeWindowMinutes} minute(s).",
                    retryAfter = retryAfterSeconds
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }

            // Add rate limit headers to response for successful requests
            context.Response.Headers["X-RateLimit-Limit"] = rateLimit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, rateLimit - requestCount).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = GetResetTime().ToString();

            _logger.LogDebug(
                "Rate limit check passed - TenantId: {TenantId}, Plan: {Plan}, Limit: {Limit}, CurrentCount: {CurrentCount}, " +
                "Path: {Path}, CorrelationId: {CorrelationId}",
                tenantId,
                planKey,
                rateLimit,
                requestCount,
                context.Request.Path,
                correlationId);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in rate limiting middleware - TenantId: {TenantId}, Path: {Path}, CorrelationId: {CorrelationId}, " +
                "ExceptionType: {ExceptionType}",
                tenantId,
                context.Request.Path,
                correlationId,
                ex.GetType().Name);
            
            // On error, allow request through based on fail-open policy
            if (_options.FailOpen)
            {
                await _next(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    errorCode = "RATE_LIMIT_SERVICE_ERROR",
                    message = "Rate limiting service is temporarily unavailable"
                });
            }
        }
    }

    /// <summary>
    /// Checks if the path is in the bypass list
    /// </summary>
    private bool IsPathInBypassList(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        return _options.BypassPaths.Any(p =>
        {
            var bypassPath = p.ToLowerInvariant();
            
            if (bypassPath.EndsWith("*"))
            {
                // Pattern like "/swagger/*"
                var prefix = bypassPath.TrimEnd('*');
                return pathValue.StartsWith(prefix);
            }
            
            if (bypassPath.EndsWith("/"))
            {
                // Pattern like "/swagger/"
                return pathValue == bypassPath.TrimEnd('/') || pathValue.StartsWith(bypassPath);
            }

            // Exact match or prefix
            return pathValue == bypassPath || pathValue.StartsWith(bypassPath);
        });
    }

    /// <summary>
    /// Gets the Unix timestamp when the rate limit resets (start of next minute).
    /// </summary>
    private static long GetResetTime()
    {
        var now = DateTime.UtcNow;
        var resetTime = now.AddMinutes(1);
        // Round to next minute boundary
        var nextMinute = new DateTime(resetTime.Year, resetTime.Month, resetTime.Day,
            resetTime.Hour, resetTime.Minute, 0, DateTimeKind.Utc);
        return new DateTimeOffset(nextMinute).ToUnixTimeSeconds();
    }
}
