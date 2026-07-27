namespace KromicStore.API.Configuration;

/// <summary>
/// Configuration options for CorrelationIdMiddleware
/// </summary>
public class CorrelationIdOptions
{
    /// <summary>
    /// HTTP header name for correlation ID
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-ID";

    /// <summary>
    /// Whether to generate a new correlation ID if not provided in request
    /// </summary>
    public bool GenerateIfMissing { get; set; } = true;

    /// <summary>
    /// Whether to include correlation ID in response headers
    /// </summary>
    public bool IncludeInResponse { get; set; } = true;
}

/// <summary>
/// Configuration options for TenantResolutionMiddleware
/// </summary>
public class TenantResolutionOptions
{
    /// <summary>
    /// Paths that bypass tenant resolution (e.g., auth endpoints, health checks)
    /// </summary>
    public List<string> BypassPaths { get; set; } = new()
    {
        "/api/v1/auth/register",
        "/api/v1/auth/login",
        "/api/v1/auth/oauth",
        "/api/v1/auth/refresh",
        "/api/v1/superuser/auth/register",
        "/api/v1/superuser/auth/login",
        "/api/v1/superuser/auth/refresh",
        "/api/v1/public/*",
        "/health",
        "/health/live",
        "/health/ready",
        "/swagger",
        "/swagger/",
        "/swagger-ui.html",
        "/swagger-resources",
        "/webjobs-list"
    };

    /// <summary>
    /// Whether to allow wildcard pattern matching for bypass paths
    /// </summary>
    public bool UseWildcardMatching { get; set; } = true;

    /// <summary>
    /// Claim name for tenant ID in JWT token
    /// </summary>
    public string TenantIdClaimName { get; set; } = "tenant_id";

    /// <summary>
    /// Whether to accept tenant ID from request headers as fallback
    /// </summary>
    public bool AllowTenantIdFromHeaders { get; set; } = false;

    /// <summary>
    /// Header name for tenant ID (if AllowTenantIdFromHeaders is true)
    /// </summary>
    public string TenantIdHeaderName { get; set; } = "X-Tenant-ID";
}

/// <summary>
/// Configuration options for ErrorHandlingMiddleware
/// </summary>
public class ErrorHandlingOptions
{
    /// <summary>
    /// Whether to include exception details in error response (should be false in production)
    /// </summary>
    public bool IncludeExceptionDetails { get; set; } = false;

    /// <summary>
    /// Whether to mask sensitive data in error messages
    /// </summary>
    public bool MaskSensitiveData { get; set; } = true;

    /// <summary>
    /// Whether to log full stack traces
    /// </summary>
    public bool LogStackTraces { get; set; } = true;

    /// <summary>
    /// Whether to include correlation ID in error response
    /// </summary>
    public bool IncludeCorrelationId { get; set; } = true;

    /// <summary>
    /// Paths where errors should not be caught (e.g., health checks returning custom errors)
    /// </summary>
    public List<string> BypassPaths { get; set; } = new()
    {
        "/health",
        "/health/live",
        "/health/ready"
    };

    /// <summary>
    /// Whether to return generic error message for 500 errors (security best practice)
    /// </summary>
    public bool UseGenericInternalErrorMessage { get; set; } = true;

    /// <summary>
    /// Generic error message to use for 500 errors when UseGenericInternalErrorMessage is true
    /// </summary>
    public string GenericInternalErrorMessage { get; set; } = "An unexpected error occurred. Please contact support.";
}

/// <summary>
/// Configuration options for RateLimitingMiddleware
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Paths that bypass rate limiting (e.g., auth endpoints, health checks)
    /// </summary>
    public List<string> BypassPaths { get; set; } = new()
    {
        "/api/v1/auth/register",
        "/api/v1/auth/login",
        "/api/v1/auth/oauth",
        "/api/v1/auth/refresh",
        "/health",
        "/health/live",
        "/health/ready",
        "/swagger",
        "/swagger-ui.html"
    };

    /// <summary>
    /// Rate limits per subscription plan (requests per minute)
    /// </summary>
    public Dictionary<string, int> RateLimitsByPlan { get; set; } = new()
    {
        { "basic", 100 },
        { "starter", 100 },
        { "professional", 500 },
        { "pro", 500 },
        { "enterprise", 5000 }
    };

    /// <summary>
    /// Default rate limit for unknown plans (requests per minute)
    /// </summary>
    public int DefaultRateLimit { get; set; } = 100;

    /// <summary>
    /// Time window for rate limiting in minutes
    /// </summary>
    public int TimeWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Whether to enable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether to store rate limit count in distributed cache (Redis)
    /// </summary>
    public bool UseDistributedCache { get; set; } = true;

    /// <summary>
    /// Whether to allow request when rate limit check fails (fail-open behavior)
    /// </summary>
    public bool FailOpen { get; set; } = true;

    /// <summary>
    /// HTTP status code to return when rate limit is exceeded
    /// </summary>
    public int RateLimitExceededStatusCode { get; set; } = StatusCodes.Status429TooManyRequests;
}

/// <summary>
/// Configuration options for CorrelationIdMiddleware, TenantResolutionMiddleware, ErrorHandlingMiddleware, and RateLimitingMiddleware
/// </summary>
public class MiddlewareOptions
{
    /// <summary>
    /// Configuration for CorrelationIdMiddleware
    /// </summary>
    public CorrelationIdOptions CorrelationId { get; set; } = new();

    /// <summary>
    /// Configuration for TenantResolutionMiddleware
    /// </summary>
    public TenantResolutionOptions TenantResolution { get; set; } = new();

    /// <summary>
    /// Configuration for ErrorHandlingMiddleware
    /// </summary>
    public ErrorHandlingOptions ErrorHandling { get; set; } = new();

    /// <summary>
    /// Configuration for RateLimitingMiddleware
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();
}
