# Tenant Identification Flow

Technical guide for understanding how tenants are identified and data is isolated in the KromicStore multi-tenant system.

## Overview

The KromicStore API is a multi-tenant system where each tenant's data is completely isolated. This document explains the mechanisms used to identify tenants and enforce data isolation.

## Tenant Identification Methods

### Method 1: JWT Token (Primary)

The most reliable method for authenticated requests is via JWT token.

**Token Structure**:
```json
{
  "sub": "user-123",
  "email": "admin@mystore.com",
  "tenant_id": "tenant-abc123",
  "roles": ["TenantAdmin"],
  "iat": 1642252800,
  "exp": 1642256400,
  "iss": "https://api.kromic-store.com",
  "aud": "https://api.kromic-store.com"
}
```

**Usage**:
```bash
curl -X GET https://api.kromic-store.com/api/v1/products \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

The system extracts `tenant_id` from the token and automatically filters all queries to that tenant.

### Method 2: Subdomain (Secondary)

For custom storefronts, tenant is identified via subdomain:

```
https://mystore.kromic-store.com/api/v1/products
        ^------^
        Tenant slug/subdomain
```

### Method 3: Custom Domain

Tenants can map custom domains:

```
https://www.mystore.com/api/v1/products
```

The system resolves custom domain to tenant ID via domain configuration.

### Method 4: X-Tenant-Id Header (Server-to-Server)

For server-to-server API calls:

```bash
curl -X GET https://api.kromic-store.com/api/v1/products \
  -H "X-Tenant-Id: tenant-abc123" \
  -H "Authorization: Bearer api-key"
```

## Data Isolation Enforcement

### Query Filtering

All database queries are automatically filtered by tenant:

```sql
-- User Request
SELECT * FROM Products WHERE id = 'product-123'

-- Actual Query Executed
SELECT * FROM Products 
WHERE id = 'product-123' 
AND tenant_id = 'tenant-abc123'
```

### Middleware Processing

The `TenantResolutionMiddleware` processes every request:

```
Request → Extract Tenant ID → Set in Context → Route Middleware → Database Query
           ↓
         From JWT Token
         From Subdomain
         From Custom Domain
         From X-Tenant-Id Header
```

### Access Control

Each endpoint enforces role-based and tenant-based access control:

```
1. Authenticate user (JWT token valid?)
2. Extract tenant from token
3. Verify user role (TenantAdmin, TenantUser, etc.)
4. Filter data by tenant
5. Execute request in tenant context
```

## Complete Tenant Identification Workflow

### Step 1: User Logs In

**Endpoint**: `POST /api/v1/auth/login`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@mystore.com",
    "password": "SecurePassword123!"
  }'
```

**Response** (200):
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEyMyIsImVtYWlsIjoiYWRtaW5AbXlzdG9yZS5jb20iLCJ0ZW5hbnRfaWQiOiJ0ZW5hbnQtYWJjMTIzIn0...",
    "user": {
      "id": "user-123",
      "email": "admin@mystore.com",
      "tenantId": "tenant-abc123",
      "roles": ["TenantAdmin"]
    }
  }
}
```

### Step 2: Include Token in Requests

```bash
curl -X GET https://api.kromic-store.com/api/v1/products \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Step 3: System Extracts Tenant ID

The middleware automatically extracts `tenant_abc123` from JWT token.

### Step 4: Data Query is Filtered

```javascript
// Frontend code
GET /api/v1/products

// Backend execution
SELECT * FROM Products 
WHERE tenant_id = 'tenant-abc123' AND status = 'Published'
```

### Step 5: Response Contains Only Tenant Data

```json
{
  "data": [
    {
      "id": "product-123",
      "tenantId": "tenant-abc123",
      "name": "Product Name"
    }
  ]
}
```

## Cross-Tenant Access Prevention

### Attempting to Access Another Tenant's Data

Even if you know the product ID from another tenant:

```bash
curl -X GET https://api.kromic-store.com/api/v1/products/other-tenant-product \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response** (404):
```json
{
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "The requested resource was not found"
  }
}
```

The system returns `404` instead of revealing that the product exists in another tenant.

### Multi-Tenant Authorization

SuperUsers can access all tenant data:

```bash
# SuperUser token
curl -X GET https://api.kromic-store.com/api/v1/admin/products/other-tenant-product \
  -H "Authorization: Bearer SUPERUSER_TOKEN"
```

SuperUser token contains `roles: ["SuperUser"]` and can access `/admin/` endpoints across all tenants.

## Technical Implementation

### Middleware

```csharp
public class TenantResolutionMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Extract tenant from JWT token, subdomain, or header
        var tenantId = ExtractTenantId(context);
        
        // Set in context for downstream use
        context.Items["TenantId"] = tenantId;
        
        await _next(context);
    }

    private string ExtractTenantId(HttpContext context)
    {
        // Priority 1: JWT Token
        var token = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
        {
            var claims = ValidateToken(token);
            return claims["tenant_id"];
        }

        // Priority 2: X-Tenant-Id Header
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
            return tenantId.ToString();
        }

        // Priority 3: Subdomain
        var subdomain = ExtractSubdomain(context.Request.Host.Host);
        if (!string.IsNullOrEmpty(subdomain))
        {
            return ResolveSubdomainToTenantId(subdomain);
        }

        throw new UnauthorizedException("Tenant not identified");
    }
}
```

### Query Filtering

```csharp
public class Repository<T> where T : TenantEntity
{
    private readonly string _tenantId;

    public Repository(IContextAccessor contextAccessor)
    {
        _tenantId = contextAccessor.GetTenantId();
    }

    public IQueryable<T> GetAll()
    {
        // Automatically filter by tenant
        return _dbSet.Where(x => x.TenantId == _tenantId);
    }
}
```

### API Authorization

```csharp
[Authorize]
[ApiController]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductDto>>> GetProducts()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        
        // Query is automatically filtered by TenantId in repository
        var products = await _productService.GetProductsAsync(tenantId);
        
        return Ok(products);
    }
}
```

## Verification Steps

### Verify Tenant Isolation

Test that data from one tenant is not visible to another:

```bash
# User 1 (Tenant A)
curl -X GET https://api.kromic-store.com/api/v1/products/product-abc \
  -H "Authorization: Bearer TOKEN_TENANT_A"
# Returns product from Tenant A

# User 2 (Tenant B) - tries to access same product
curl -X GET https://api.kromic-store.com/api/v1/products/product-abc \
  -H "Authorization: Bearer TOKEN_TENANT_B"
# Returns 404 (data isolation enforced)
```

### Verify Tenant ID in Responses

All responses include tenant ID:

```json
{
  "data": {
    "id": "product-123",
    "tenantId": "tenant-abc123",
    "name": "Product Name"
  },
  "meta": {
    "tenantId": "tenant-abc123"
  }
}
```

## Best Practices

1. **Always Include Token**: Include Authorization header in all requests
2. **Verify Responses**: Check `tenantId` in responses to confirm correct tenant
3. **Test Isolation**: Test that you cannot access other tenant data
4. **Token Rotation**: Rotate access tokens regularly
5. **Monitor Access**: Log all API access for audit trail
6. **Error Handling**: Handle 404 errors properly (could be missing or in different tenant)

## Troubleshooting

### 401 Unauthorized

- Token not included in request
- Token expired
- Token invalid/malformed

**Solution**: Refresh token using `/api/v1/auth/refresh`

### 403 Forbidden

- User role insufficient for operation
- Attempting to modify another tenant's data

**Solution**: Verify user role and tenant ID

### 404 Not Found

- Resource doesn't exist in your tenant
- Resource exists in different tenant (returns same error for security)

**Solution**: Verify resource ID and tenant context

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for authentication details.
