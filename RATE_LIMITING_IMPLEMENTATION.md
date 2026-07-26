# Rate Limiting Middleware Implementation

## Overview

The `RateLimitingMiddleware` enforces API rate limiting based on tenant subscription plans. It uses a sliding window counter mechanism stored in Redis for distributed rate limit tracking across multiple application instances.

## Architecture

### Rate Limits by Subscription Plan

- **Basic**: 100 requests per minute
- **Starter**: 100 requests per minute
- **Professional**: 500 requests per minute
- **Pro**: 500 requests per minute
- **Enterprise**: 5000 requests per minute

### Request Flow

1. **Tenant Resolution**: TenantId extracted from HTTP context (set by TenantResolutionMiddleware)
2. **Subscription Plan Lookup**: Tenant's subscription plan retrieved from database
3. **Rate Limit Determination**: Appropriate rate limit applied based on plan
4. **Request Counting**: Current request count retrieved from Redis cache
5. **Cache Key**: `ratelimit:{TenantId}:{yyyyMMddHHmm}` - sliding window per minute
6. **Limit Check**: If count exceeds limit, return 429 (Too Many Requests)
7. **Response Headers**: Include rate limit information

## Implementation Details

### File: `src/KromicStore.API/Middleware/RateLimitingMiddleware.cs`

#### Key Features:

1. **Tenant Context Support**
   - Integrates with `ITenantProvider` to get current tenant ID
   - Looks up tenant subscription plan from database
   - Supports Guid-based tenant identification

2. **Distributed Rate Limiting**
   - Uses Redis cache for request counting via `ICacheService`
   - Sliding window approach with per-minute granularity
   - Cache key expires automatically after 1 minute

3. **Public Endpoint Bypass**
   - Skips rate limiting for authentication endpoints:
     - `/api/v1/auth/register`
     - `/api/v1/auth/login`
     - `/api/v1/auth/oauth`
     - `/api/v1/auth/refresh`
   - Skips health check endpoints: `/health`
   - Skips Swagger endpoints: `/swagger`, `/swagger-ui`, `/swagger-resources`, `/api-docs`

4. **Error Handling**
   - Tenant not found returns 401 Unauthorized
   - Rate limit exceeded returns 429 Too Many Requests
   - Cache service failures fail open (allow request, log warning)
   - Database errors fail open to prevent cascading failures

5. **Response Headers**
   - `X-RateLimit-Limit`: Maximum requests allowed
   - `X-RateLimit-Remaining`: Requests remaining in current window
   - `X-RateLimit-Reset`: Unix timestamp when limit resets
   - `Retry-After`: Seconds to wait before retrying (on 429)

6. **Error Response Format**

```json
{
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Rate limit exceeded. Maximum {limit} requests per minute.",
  "retryAfter": {seconds}
}
```

### Program Configuration

Updated `src/KromicStore.API/Program.cs` to:

1. Register `IConnectionMultiplexer` singleton for Redis
2. Register `ICacheService` implementation (`CacheService`)
3. Add middleware to pipeline in correct order:
   - CorrelationIdMiddleware (first)
   - TenantResolutionMiddleware
   - ErrorHandlingMiddleware
   - RateLimitingMiddleware
   - Authentication/Authorization
   - Controllers

### Middleware Registration

```csharp
// In Program.cs
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    var options = ConfigurationOptions.Parse(redisConnection);
    var connection = ConnectionMultiplexer.Connect(options);
    builder.Services.AddSingleton<IConnectionMultiplexer>(connection);
    builder.Services.AddSingleton<ICacheService, CacheService>();
}

// In middleware pipeline
app.UseMiddleware<RateLimitingMiddleware>();
```

## Testing

### Test Coverage: `tests/KromicStore.Tests/RateLimitingMiddlewareTests.cs`

Comprehensive unit tests covering:

1. **Public Endpoint Bypass**
   - Skips rate limiting for public endpoints
   - Skips rate limiting for health endpoints

2. **Tenant Resolution**
   - Allows requests when tenant is resolved
   - Returns 401 when tenant not found
   - Allows requests to proceed when tenant ID is empty (deferred error)

3. **Plan-Based Limits**
   - Applies correct limit for each subscription plan
   - Handles unknown plans with default (basic) limit

4. **Rate Limit Enforcement**
   - Returns 429 when limit exceeded
   - Includes Retry-After header
   - Includes correct X-RateLimit headers

5. **Request Counting**
   - Increments request count correctly
   - Uses sliding window cache keys
   - Cache key includes current minute window

6. **Error Resilience**
   - Fails open on cache service exceptions
   - Fails open on database exceptions
   - Always calls next middleware (except on 429 or 401)

### Running Tests

```bash
# Run all rate limiting tests
dotnet test tests/KromicStore.Tests/RateLimitingMiddlewareTests.cs

# Run specific test
dotnet test tests/KromicStore.Tests/RateLimitingMiddlewareTests.cs -k "RateLimitExceeded"
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=kromicstore;Username=postgres;Password=password;",
    "Redis": "localhost:6379"
  }
}
```

### Environment Variables

```bash
# For production
export ConnectionStrings__Redis="redis-prod-server:6379"
export ConnectionStrings__DefaultConnection="Host=prod-db;Database=kromicstore;..."
```

## Rate Limit Behavior Examples

### Example 1: Within Limit

```
Tenant: basic plan (100 requests/minute)
Current count: 50
Request #51 arrives

Response: 200 OK
Headers:
  X-RateLimit-Limit: 100
  X-RateLimit-Remaining: 49
  X-RateLimit-Reset: 1704067260
```

### Example 2: At Limit

```
Tenant: basic plan (100 requests/minute)
Current count: 100
Request #101 arrives

Response: 429 Too Many Requests
Headers:
  X-RateLimit-Limit: 100
  X-RateLimit-Remaining: 0
  X-RateLimit-Reset: 1704067260
  Retry-After: 45

Body:
{
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Rate limit exceeded. Maximum 100 requests per minute.",
  "retryAfter": 45
}
```

### Example 3: Minute Window Reset

```
Previous minute window: ratelimit:tenant-id:202401011459
Current minute window: ratelimit:tenant-id:202401011500
Cache expires after 1 minute
Request count resets to 1 for new minute
```

## Multi-Tenant Isolation

Rate limits are properly isolated per tenant:

- Each tenant has independent request counters
- Cache key includes `{TenantId}`
- Tenant A's rate limit doesn't affect Tenant B
- Different subscription plans get different limits

## Dependencies

### NuGet Packages

- `StackExchange.Redis` - Distributed cache
- `Microsoft.EntityFrameworkCore` - Database access
- `Serilog` - Logging

### Services

- `ITenantProvider` - Gets current tenant context
- `ICacheService` - Redis-based caching
- `AppDbContext` - Database context for tenant lookup

## Performance Considerations

### Cache Efficiency

- **Per-minute granularity**: Cache keys change every minute
- **Automatic expiration**: Redis keys expire after 1 minute
- **No cleanup needed**: Expired keys automatically removed by Redis

### Database Queries

- **Cached tenant lookups**: Recommended to add caching to tenant service
- **AsNoTracking**: Database queries use `AsNoTracking()` to reduce memory
- **Minimized impact**: Only one database query per request (tenant lookup)

### Fail-Safe Behavior

- **Fail open**: If cache or database fails, request is allowed through
- **Logging**: All failures logged for monitoring
- **No cascading failures**: Exceptions caught and logged, pipeline continues

## Logging

The middleware logs the following information:

### Info Level
- Successful rate limit enforcement (per tenant for violations)

### Warning Level
- Unknown subscription plans (defaults to basic limit)
- Tenant not found (401 response)
- Middleware exceptions

### Error Level
- Database errors during tenant lookup
- Cache service errors

## Future Enhancements

1. **Quota Reset Time Configuration**: Allow per-tenant quota reset times
2. **Burst Allowance**: Allow burst requests exceeding limit (with backoff)
3. **Endpoint-Specific Limits**: Different limits for different endpoint types
4. **Rate Limit Metrics**: Export metrics to monitoring system
5. **User-Level Rate Limiting**: Add per-user limits in addition to tenant limits
6. **Cost-Based Rate Limiting**: Assign costs to different operations

## Security Considerations

1. **Tenant Isolation**: All rate limits keyed by TenantId for isolation
2. **Header Injection**: Headers set by middleware (not client-provided)
3. **Retry-After Accuracy**: Calculated server-side to prevent tampering
4. **Public Endpoint Protection**: Auth endpoints have reasonable protection despite bypass

## Troubleshooting

### Issue: All Requests Return 429

**Cause**: Redis cache persisting old counts or clock skew

**Solution**:
1. Verify Redis is running and accessible
2. Check system time across servers (time synchronization)
3. Clear Redis cache: `redis-cli FLUSHDB`

### Issue: Rate Limiting Not Working

**Cause**: Redis connection not configured or failing

**Solution**:
1. Verify `ConnectionStrings:Redis` in appsettings.json
2. Test Redis connection: `redis-cli ping`
3. Check middleware order in Program.cs

### Issue: Tenant Returns 401

**Cause**: Tenant not found in database

**Solution**:
1. Verify TenantId is correct in JWT token
2. Verify tenant exists in database
3. Check TenantResolutionMiddleware properly set TenantId

## Compliance

- **Rate Limiting Response Format**: Complies with RFC 6585 (429 Too Many Requests)
- **Retry-After Header**: RFC 7231 standard format (seconds)
- **X-RateLimit Headers**: De facto standard for rate limiting APIs
