# Middleware Configuration Options

## Overview

All middleware in KromicStore API is now fully configurable through options patterns. This document explains each middleware's configuration options and how to customize them.

## Architecture

The middleware is organized in a specific order in `Program.cs`:

1. **CorrelationIdMiddleware** - Generates/propagates correlation IDs for tracing
2. **TenantResolutionMiddleware** - Resolves and validates tenant context
3. **ErrorHandlingMiddleware** - Catches exceptions and returns standardized errors
4. **RateLimitingMiddleware** - Enforces API rate limits by subscription plan

## Configuration Files

All middleware options are configured in `appsettings.json` under the `Middleware` section:

```json
{
  "Middleware": {
    "CorrelationId": { ... },
    "TenantResolution": { ... },
    "ErrorHandling": { ... },
    "RateLimiting": { ... }
  }
}
```

## Middleware Options

### CorrelationIdMiddleware

**Purpose**: Generates and propagates correlation IDs for distributed tracing across services and logs.

**Configuration Class**: `KromicStore.API.Configuration.CorrelationIdOptions`

**Options**:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| CorrelationIdHeader | string | "X-Correlation-ID" | HTTP header name for correlation ID |
| GenerateIfMissing | bool | true | Whether to generate new ID if not provided in request |
| IncludeInResponse | bool | true | Whether to include correlation ID in response headers |

**Example Configuration**:

```json
{
  "Middleware": {
    "CorrelationId": {
      "CorrelationIdHeader": "X-Correlation-ID",
      "GenerateIfMissing": true,
      "IncludeInResponse": true
    }
  }
}
```

**Usage**:

```csharp
// In appsettings.json, use custom header name
{
  "Middleware": {
    "CorrelationId": {
      "CorrelationIdHeader": "X-Trace-ID"
    }
  }
}

// Clients send correlation ID in request
// GET /api/v1/products
// Headers: X-Trace-ID: my-trace-123

// Response includes same correlation ID
// Headers: X-Trace-ID: my-trace-123
```

---

### TenantResolutionMiddleware

**Purpose**: Extracts and validates tenant information from JWT tokens or headers, enforcing multi-tenancy.

**Configuration Class**: `KromicStore.API.Configuration.TenantResolutionOptions`

**Options**:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| BypassPaths | List<string> | Auth/Health/Swagger paths | Paths that don't require tenant resolution |
| UseWildcardMatching | bool | true | Enable wildcard pattern matching (e.g., /swagger*) |
| TenantIdClaimName | string | "tenant_id" | JWT claim name for tenant ID |
| AllowTenantIdFromHeaders | bool | false | Allow tenant ID from X-Tenant-ID header (fallback) |
| TenantIdHeaderName | string | "X-Tenant-ID" | Header name for tenant ID (if allowed) |

**Bypass Paths Examples**:

```json
{
  "Middleware": {
    "TenantResolution": {
      "BypassPaths": [
        "/api/v1/auth/register",      // Exact match
        "/api/v1/auth/login",
        "/swagger/*",                  // Wildcard pattern
        "/health/",                    // Trailing slash
        "/api-docs"
      ],
      "UseWildcardMatching": true
    }
  }
}
```

**Pattern Matching**:

- **Exact Path**: `/api/v1/auth/login` - matches only `/api/v1/auth/login`
- **Trailing Slash**: `/swagger/` - matches `/swagger` or `/swagger/*`
- **Wildcard**: `/swagger*` - matches `/swagger` and anything starting with `/swagger`

**Usage**:

```csharp
// Requests to bypass paths don't require tenant:
// GET /api/v1/auth/login - bypassed, no tenant needed
// GET /swagger/ui - bypassed, no tenant needed

// All other requests require tenant in JWT token:
// GET /api/v1/products
// Headers: Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
// JWT must contain: { "tenant_id": "550e8400-e29b-41d4-a716-446655440000" }

// If AllowTenantIdFromHeaders is true, fallback to header:
// GET /api/v1/products
// Headers: X-Tenant-ID: 550e8400-e29b-41d4-a716-446655440000
```

---

### ErrorHandlingMiddleware

**Purpose**: Catches unhandled exceptions and returns standardized error responses with appropriate HTTP status codes.

**Configuration Class**: `KromicStore.API.Configuration.ErrorHandlingOptions`

**Options**:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| IncludeExceptionDetails | bool | false | Include exception message in response (dev only) |
| MaskSensitiveData | bool | true | Mask sensitive data (passwords, tokens) in errors |
| LogStackTraces | bool | true | Log full stack traces of exceptions |
| IncludeCorrelationId | bool | true | Include correlation ID in error responses |
| BypassPaths | List<string> | Health endpoints | Paths where errors aren't caught |
| UseGenericInternalErrorMessage | bool | true | Use generic message for 500 errors (security) |
| GenericInternalErrorMessage | string | Standard message | Generic message for 500 errors |

**Example Configuration**:

```json
{
  "Middleware": {
    "ErrorHandling": {
      "IncludeExceptionDetails": false,
      "MaskSensitiveData": true,
      "LogStackTraces": true,
      "IncludeCorrelationId": true,
      "BypassPaths": [
        "/health",
        "/health/live",
        "/health/ready"
      ],
      "UseGenericInternalErrorMessage": true,
      "GenericInternalErrorMessage": "An unexpected error occurred. Please contact support."
    }
  }
}
```

**Development Configuration** (include details):

```json
{
  "Middleware": {
    "ErrorHandling": {
      "IncludeExceptionDetails": true,
      "UseGenericInternalErrorMessage": false
    }
  }
}
```

**Error Response Format**:

```json
{
  "errorCode": "VALIDATION_ERROR",
  "message": "One or more validation failures occurred.",
  "details": {
    "sku": ["SKU is required"],
    "price": ["Price must be greater than 0"]
  },
  "traceId": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

### RateLimitingMiddleware

**Purpose**: Enforces API rate limits based on subscription plan to protect system resources and fair usage.

**Configuration Class**: `KromicStore.API.Configuration.RateLimitingOptions`

**Options**:

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| BypassPaths | List<string> | Auth/Health/Swagger paths | Paths that bypass rate limiting |
| RateLimitsByPlan | Dictionary<string, int> | Plan-based limits | Requests per minute per plan |
| DefaultRateLimit | int | 100 | Default limit for unknown plans |
| TimeWindowMinutes | int | 1 | Time window for rate limit (minutes) |
| Enabled | bool | true | Enable rate limiting |
| UseDistributedCache | bool | true | Use Redis cache for distributed counting |
| FailOpen | bool | true | Allow requests if rate limit check fails |
| RateLimitExceededStatusCode | int | 429 | HTTP status code when limit exceeded |

**Example Configuration**:

```json
{
  "Middleware": {
    "RateLimiting": {
      "BypassPaths": [
        "/api/v1/auth/register",
        "/api/v1/auth/login",
        "/health/*",
        "/swagger*"
      ],
      "RateLimitsByPlan": {
        "basic": 100,
        "starter": 100,
        "professional": 500,
        "pro": 500,
        "enterprise": 5000
      },
      "DefaultRateLimit": 100,
      "TimeWindowMinutes": 1,
      "Enabled": true,
      "UseDistributedCache": true,
      "FailOpen": true,
      "RateLimitExceededStatusCode": 429
    }
  }
}
```

**Development Configuration** (disable rate limiting):

```json
{
  "Middleware": {
    "RateLimiting": {
      "Enabled": false
    }
  }
}
```

**Rate Limit Response Headers**:

```
X-RateLimit-Limit: 500              # Requests allowed in window
X-RateLimit-Remaining: 423          # Requests remaining
X-RateLimit-Reset: 1699564800      # Unix timestamp when limit resets
Retry-After: 45                     # Seconds until next window (if exceeded)
```

**Rate Limit Exceeded Response** (HTTP 429):

```json
{
  "code": "RATE_LIMIT_EXCEEDED",
  "message": "Rate limit exceeded. Maximum 500 requests per 1 minute(s).",
  "retryAfter": 45
}
```

---

## Configuration Per Environment

### Development (appsettings.Development.json)

- Relaxed rate limits
- Exception details included
- Stack traces logged
- Tenant ID allowed from headers

```json
{
  "Middleware": {
    "CorrelationId": {
      "GenerateIfMissing": true
    },
    "TenantResolution": {
      "AllowTenantIdFromHeaders": true
    },
    "ErrorHandling": {
      "IncludeExceptionDetails": true,
      "UseGenericInternalErrorMessage": false
    },
    "RateLimiting": {
      "Enabled": false
    }
  }
}
```

### Production (appsettings.json)

- Strict rate limits per plan
- Generic error messages
- Stack traces NOT exposed
- Sensitive data masked

```json
{
  "Middleware": {
    "ErrorHandling": {
      "IncludeExceptionDetails": false,
      "MaskSensitiveData": true,
      "UseGenericInternalErrorMessage": true
    },
    "TenantResolution": {
      "AllowTenantIdFromHeaders": false
    },
    "RateLimiting": {
      "Enabled": true,
      "FailOpen": false
    }
  }
}
```

---

## Programmatic Configuration

You can also configure middleware options programmatically in `Program.cs`:

```csharp
// Override specific options
builder.Services.Configure<RateLimitingOptions>(options =>
{
    options.Enabled = false;  // Disable rate limiting
    options.DefaultRateLimit = 1000;
    options.TimeWindowMinutes = 5;
    options.RateLimitsByPlan["starter"] = 5000;
});

builder.Services.Configure<TenantResolutionOptions>(options =>
{
    options.BypassPaths.Add("/api/v1/custom-public");
});
```

---

## Common Configuration Scenarios

### Scenario 1: Disable Rate Limiting for Testing

```json
{
  "Middleware": {
    "RateLimiting": {
      "Enabled": false
    }
  }
}
```

### Scenario 2: Allow Tenant ID from Custom Header

```json
{
  "Middleware": {
    "TenantResolution": {
      "AllowTenantIdFromHeaders": true,
      "TenantIdHeaderName": "X-Tenant-ID"
    }
  }
}
```

### Scenario 3: Strict Error Handling (Production)

```json
{
  "Middleware": {
    "ErrorHandling": {
      "IncludeExceptionDetails": false,
      "MaskSensitiveData": true,
      "LogStackTraces": true,
      "UseGenericInternalErrorMessage": true
    }
  }
}
```

### Scenario 4: Custom Bypass Paths

```json
{
  "Middleware": {
    "TenantResolution": {
      "BypassPaths": [
        "/api/v1/auth/*",
        "/health/*",
        "/api/v1/public/*",
        "/webhooks/*"
      ]
    },
    "RateLimiting": {
      "BypassPaths": [
        "/api/v1/auth/*",
        "/health/*",
        "/webhooks/*"
      ]
    }
  }
}
```

### Scenario 5: Per-Plan Rate Limits

```json
{
  "Middleware": {
    "RateLimiting": {
      "RateLimitsByPlan": {
        "free": 50,
        "basic": 100,
        "starter": 500,
        "professional": 2000,
        "enterprise": 10000
      },
      "DefaultRateLimit": 50,
      "TimeWindowMinutes": 1
    }
  }
}
```

---

## Testing Middleware Configuration

See `MiddlewareConfigurationTests.cs` for comprehensive examples:

```csharp
// Test bypass paths
[Fact]
public async Task ShouldBypassRateLimitingForConfiguredPaths()
{
    var options = new RateLimitingOptions
    {
        BypassPaths = new List<string> { "/api/v1/auth/login" }
    };
    // ...
}

// Test wildcard matching
[Fact]
public async Task ShouldSupportWildcardPatterns()
{
    var options = new TenantResolutionOptions
    {
        BypassPaths = new List<string> { "/swagger*", "/health/*" }
    };
    // ...
}
```

---

## Best Practices

1. **Use separate configuration files per environment** - Development, Staging, Production
2. **Never hardcode sensitive values** - Use configuration files or environment variables
3. **Enable exception details only in development** - Disable in production for security
4. **Use bypass paths appropriately** - Only public endpoints should bypass tenant resolution
5. **Monitor rate limit violations** - Track which tenants are hitting limits
6. **Test configuration changes** - Verify bypass paths work as expected
7. **Document custom paths** - Keep bypass paths list updated in code comments

---

## Troubleshooting

### Issue: Requests to public endpoint failing with 401

**Solution**: Add path to `TenantResolution.BypassPaths`

```json
{
  "Middleware": {
    "TenantResolution": {
      "BypassPaths": ["/api/v1/public/docs"]
    }
  }
}
```

### Issue: Rate limit not working

**Solution**: Verify enabled and check Redis connection

```json
{
  "Middleware": {
    "RateLimiting": {
      "Enabled": true,
      "UseDistributedCache": true
    }
  }
}
```

### Issue: Correlation IDs not showing in response

**Solution**: Enable in configuration

```json
{
  "Middleware": {
    "CorrelationId": {
      "IncludeInResponse": true
    }
  }
}
```

### Issue: Tenant from header not working

**Solution**: Enable header fallback

```json
{
  "Middleware": {
    "TenantResolution": {
      "AllowTenantIdFromHeaders": true
    }
  }
}
```

---

## Related Files

- Configuration Classes: `/src/KromicStore.API/Configuration/MiddlewareOptions.cs`
- Middleware Implementations:
  - `/src/KromicStore.API/Middleware/CorrelationIdMiddleware.cs`
  - `/src/KromicStore.API/Middleware/TenantResolutionMiddleware.cs`
  - `/src/KromicStore.API/Middleware/ErrorHandlingMiddleware.cs`
  - `/src/KromicStore.API/Middleware/RateLimitingMiddleware.cs`
- Configuration in Program.cs: `/src/KromicStore.API/Program.cs`
- Tests: `/tests/KromicStore.Tests/MiddlewareTests/MiddlewareConfigurationTests.cs`
