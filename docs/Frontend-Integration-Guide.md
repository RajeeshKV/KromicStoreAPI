# KromicStore Frontend Integration Guide

## Complete Guide to Integrating with KromicStore API

This comprehensive guide provides frontend developers with everything needed to integrate with the KromicStore multi-tenant e-commerce API. The guide covers authentication, authorization, tenant identification, error handling, and all major feature workflows.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [API Fundamentals](#api-fundamentals)
3. [Authentication & Authorization](#authentication--authorization)
4. [Tenant Identification](#tenant-identification)
5. [Subdomain Management](#subdomain-management)
6. [Public Endpoints](#public-endpoints)
7. [SuperUser Authentication](#superuser-authentication)
8. [Request/Response Patterns](#requestresponse-patterns)
9. [Error Handling](#error-handling)
10. [Multi-Tenancy](#multi-tenancy)
11. [Caching & Performance](#caching--performance)
12. [Webhooks](#webhooks)
13. [Best Practices](#best-practices)

---

## Getting Started

### Base URL

```
Development:  http://localhost:8080/api/v1
Staging:      https://staging.kromic.in/api/v1
Production:   https://api.kromic.in/api/v1
```

### Authentication Methods

KromicStore API supports two authentication methods:

1. **OAuth 2.0 Bearer Token** (recommended for user-facing applications)
2. **API Key** (for server-to-server integrations and webhooks)

### Required Headers

All API requests must include:

```
Accept: application/json
Content-Type: application/json
Authorization: Bearer {access_token}
```

---

## API Fundamentals

### API Version

The current API version is **v1**. All endpoints use the `/api/v1/` prefix.

### Response Format

All responses follow a standard format:

```json
{
  "data": {
    "id": "uuid-string",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  },
  "meta": {
    "requestId": "correlation-id-uuid",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Pagination

List endpoints support pagination with the following query parameters:

```
GET /api/v1/products?page=1&pageSize=20&sort=createdAt&order=desc
```

**Parameters:**
- `page` (integer, default: 1) - Page number starting from 1
- `pageSize` (integer, default: 20, max: 100) - Number of items per page
- `sort` (string, default: "createdAt") - Field to sort by
- `order` (string, default: "desc") - Sort order: "asc" or "desc"

**Paginated Response Format:**

```json
{
  "data": [
    { "id": "product-1", "name": "Product Name" },
    { "id": "product-2", "name": "Another Product" }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  },
  "meta": {
    "requestId": "abc-123-def-456",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Filtering

List endpoints support filtering with query parameters. Filter syntax varies by endpoint:

```
GET /api/v1/products?status=published&categoryId=cat-123&minPrice=10&maxPrice=100
```

### Field Projection

Some endpoints support field projection to reduce payload size:

```
GET /api/v1/products?fields=id,name,price
```

---

## Authentication & Authorization

### User Roles

KromicStore defines the following user roles:

1. **SuperUser** - System administrator with full platform access
2. **TenantAdmin** - Tenant administrator with access to their tenant data
3. **TenantUser** - Regular user within a tenant (customer or team member)
4. **Customer** - End customer of the store (read-only access to products)

### Role-Based Access Control (RBAC)

Endpoints require specific roles:

```
GET /api/v1/products             - Customer, TenantUser, TenantAdmin
POST /api/v1/products            - TenantAdmin+
PUT /api/v1/products/{id}        - TenantAdmin+
DELETE /api/v1/products/{id}     - TenantAdmin+
GET /api/v1/admin/config         - SuperUser+
```

### Permission Levels

Permissions are hierarchical:

- **SuperUser** has all permissions
- **TenantAdmin** has all permissions within their tenant
- **TenantUser** has limited permissions (view products, place orders, manage profile)
- **Customer** has read-only permissions (view products, track orders)

---

## Tenant Identification

### How Tenant is Identified

The system identifies the tenant through multiple methods (in priority order):

1. **Request Context** - Automatically extracted from JWT token for authenticated requests
2. **Subdomain** - Extracted from URL: `{tenant-slug}.kromic-store.com`
3. **Custom Domain** - If tenant uses custom domain, tenant ID is stored in domain configuration
4. **X-Tenant-Id Header** (optional) - Can be provided for server-to-server requests

### JWT Token Structure

The access token contains the tenant information in its claims:

```json
{
  "sub": "user-123",
  "email": "admin@company.com",
  "tenant_id": "tenant-456",
  "roles": ["TenantAdmin"],
  "iat": 1642252800,
  "exp": 1642256400,
  "iss": "https://api.kromic-store.com",
  "aud": "https://api.kromic-store.com"
}
```

### Multi-Tenant Data Isolation

All API responses are automatically filtered to the authenticated user's tenant. Data from other tenants is never returned, even if the ID is known.

```javascript
// This request returns only products from the authenticated user's tenant
GET /api/v1/products/product-123

// If product-123 belongs to a different tenant, you receive 404
```

---

## Request/Response Patterns

### Standard HTTP Methods

```
GET     - Retrieve resource(s)
POST    - Create resource
PUT     - Update entire resource
PATCH   - Partially update resource
DELETE  - Delete resource
HEAD    - Check resource existence (no response body)
OPTIONS - Get available methods (CORS)
```

### Request Body Format

All POST/PUT/PATCH requests should include a JSON body with the following format:

```json
{
  "name": "Product Name",
  "description": "Product description",
  "price": 99.99,
  "categoryId": "category-123",
  "stock": 100
}
```

### Response Body Format

Successful responses include the following structure:

```json
{
  "data": {
    "id": "product-123",
    "name": "Product Name",
    "description": "Product description",
    "price": 99.99,
    "categoryId": "category-123",
    "stock": 100,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  },
  "meta": {
    "requestId": "req-123-456-789",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Response Headers

All responses include important headers:

```
Content-Type: application/json
X-Request-Id: correlation-id-uuid
X-Response-Time: 45ms
Cache-Control: private, max-age=300
ETag: "abc123def456"
```

---

## Error Handling

### HTTP Status Codes

| Status | Meaning | Example |
|--------|---------|---------|
| 200 | OK - Request succeeded | GET /api/v1/products/123 |
| 201 | Created - Resource created | POST /api/v1/products |
| 204 | No Content - Successful, no response body | DELETE /api/v1/products/123 |
| 400 | Bad Request - Invalid request data | POST with missing required field |
| 401 | Unauthorized - Missing or invalid token | Missing Authorization header |
| 403 | Forbidden - Insufficient permissions | TenantUser accessing admin endpoint |
| 404 | Not Found - Resource doesn't exist | GET /api/v1/products/nonexistent |
| 409 | Conflict - Business logic violation | Creating product with duplicate SKU |
| 422 | Unprocessable Entity - Validation error | POST with invalid email format |
| 429 | Too Many Requests - Rate limit exceeded | Exceeded API rate limit |
| 500 | Internal Server Error - Server error | Unexpected exception |
| 503 | Service Unavailable - Maintenance | Database unavailable |

### Error Response Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": [
      {
        "field": "email",
        "code": "INVALID_EMAIL_FORMAT",
        "message": "Email address is not valid"
      },
      {
        "field": "price",
        "code": "PRICE_MUST_BE_POSITIVE",
        "message": "Price must be greater than 0"
      }
    ],
    "traceId": "correlation-id-uuid"
  },
  "meta": {
    "requestId": "req-123-456-789",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Common Error Codes

| Error Code | HTTP Status | Description |
|------------|------------|-------------|
| INVALID_TOKEN | 401 | Access token is expired or malformed |
| MISSING_TOKEN | 401 | Authorization header missing or empty |
| INSUFFICIENT_PERMISSIONS | 403 | User role lacks required permissions |
| RESOURCE_NOT_FOUND | 404 | Requested resource doesn't exist |
| DUPLICATE_RESOURCE | 409 | Resource already exists (e.g., duplicate SKU) |
| INVALID_STATUS_TRANSITION | 409 | Cannot transition to requested status |
| INSUFFICIENT_INVENTORY | 409 | Product stock insufficient for operation |
| VALIDATION_ERROR | 422 | Request validation failed |
| EXTERNAL_SERVICE_ERROR | 503 | External service (Razorpay, etc.) unavailable |
| RATE_LIMIT_EXCEEDED | 429 | Too many requests in time window |

---

## Subdomain Management

### Subdomain Selection

Each tenant selects a unique subdomain during registration (e.g., `mystore.kromic.in`). This subdomain serves as:
- The tenant's unique identifier
- The URL for their storefront
- The routing key for multi-tenancy

### Subdomain Validation

Subdomains must follow these rules:
- **Length**: 3-63 characters
- **Characters**: Alphanumeric (a-z, 0-9) and hyphens (-) only
- **Format**: Must start and end with alphanumeric character
- **Case**: Automatically converted to lowercase
- **Reserved**: Cannot use reserved subdomains

### Reserved Subdomains

The following subdomains are reserved and cannot be used:
- `api` - API endpoint
- `admin` - Admin panel
- `www` - Web redirect
- `mail` - Email services
- `ftp` - File transfer
- `cdn` - Content delivery
- `static` - Static assets
- `assets` - Asset files
- `echoroom`, `mallu-masala`, `spinema`, `flowapi`, `flow`, `storeapi`, `store` - Platform services

### Check Subdomain Availability

Before registration, check if a subdomain is available:

```bash
GET /api/v1/public/subdomain/check?subdomain=mystore
```

**Response (Available):**
```json
{
  "available": true
}
```

**Response (Not Available):**
```json
{
  "available": false,
  "reason": "Subdomain is already taken"
}
```

**Response (Reserved):**
```json
{
  "available": false,
  "reason": "Subdomain is reserved"
}
```

**Response (Invalid Format):**
```json
{
  "available": false,
  "reason": "Invalid subdomain format. Only alphanumeric characters and hyphens are allowed."
}
```

### Subdomain Routing

When users visit a tenant's subdomain (`https://mystore.kromic.in`), the system:
1. Extracts the subdomain from the request host
2. Looks up the tenant by subdomain
3. Redirects to the tenant's frontend URL
4. Preserves the path and query string for login flows

### Tenant Registration with Subdomain

When registering a new tenant, include the subdomain:

```bash
POST /api/v1/auth/register
{
  "companyName": "My Store",
  "subdomain": "mystore",
  "email": "admin@mystore.com",
  "firstName": "John",
  "lastName": "Doe",
  "password": "SecurePassword123!",
  "country": "IN"
}
```

The subdomain is validated for:
- Format compliance
- Uniqueness across all tenants
- Reserved subdomain exclusion

### Subdomain-Based Login

Users can login directly via their tenant's subdomain:

```
https://mystore.kromic.in/login?redirect=/dashboard
```

The subdomain routing middleware:
- Identifies the tenant from the subdomain
- Redirects to the tenant's frontend
- Preserves the login redirect parameter

---

## Public Endpoints

Public endpoints are accessible without authentication and provide essential information for the landing page and registration flow.

### Get Subscription Plans

Retrieve available subscription plans for new tenant sign-ups.

**Endpoint:** `GET /api/v1/public/plans`

**Response:**
```json
{
  "data": [
    {
      "id": "starter",
      "name": "Starter",
      "price": 0,
      "currency": "INR",
      "features": [
        "5 Users",
        "100 Products",
        "10,000 API Calls/month"
      ]
    },
    {
      "id": "professional",
      "name": "Professional",
      "price": 799,
      "currency": "INR",
      "features": [
        "50 Users",
        "1,000 Products",
        "100,000 API Calls/month"
      ]
    },
    {
      "id": "enterprise",
      "name": "Enterprise",
      "price": 2499,
      "currency": "INR",
      "features": [
        "Unlimited Users",
        "Unlimited Products",
        "Unlimited API Calls",
        "Priority Support"
      ]
    }
  ]
}
```

**Usage:** Display plans on the pricing page of the landing page.

### Get Platform Configuration

Retrieve platform-wide configuration including contact details for footer/contact page.

**Endpoint:** `GET /api/v1/public/config`

**Response:**
```json
{
  "data": {
    "contactEmail": "support@kromic.in",
    "contactPhone": "+91-9876543210",
    "supportEmail": "support@kromic.in",
    "companyName": "KromicStore",
    "websiteUrl": "https://kromic.in",
    "instagramUrl": "https://instagram.com/kromicstore"
  }
}
```

**Usage:** 
- Display contact information in footer
- Show social media links (Instagram if configured)
- Display company branding

### Contact Us Form

Submit a contact us form inquiry.

**Endpoint:** `POST /api/v1/public/contactus`

**Request Body:**
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "phone": "+91-9876543210",
  "subject": "Partnership Inquiry",
  "message": "I would like to discuss a potential partnership..."
}
```

**Required Fields:**
- `name` - Contact person's name
- `email` - Contact person's email
- `message` - The message content

**Optional Fields:**
- `phone` - Contact phone number
- `subject` - Subject line for the email

**Response (Success):**
```json
{
  "message": "Contact form submitted successfully"
}
```

**Response (Error):**
```json
{
  "error": "Name, email, and message are required"
}
```

**Usage:** Implement the contact form on the contact page.

---

## SuperUser Authentication

SuperUser is a separate authentication flow for platform administrators. SuperUsers have full platform-wide access and bypass tenant resolution.

### SuperUser Login

**Endpoint:** `POST /api/v1/superuser/auth/login`

**Request Body:**
```json
{
  "email": "admin@kromic.in",
  "password": "SecurePassword123!"
}
```

**Response:**
```json
{
  "userId": "super-user-uuid",
  "email": "admin@kromic.in",
  "firstName": "Admin",
  "lastName": "User",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-15T11:30:00Z"
}
```

**Error Responses:**
- `401 Unauthorized` - Invalid email or password
- `500 Internal Server Error` - Server error during login

### SuperUser JWT Token Structure

The SuperUser JWT token contains special claims:

```json
{
  "sub": "super-user-uuid",
  "email": "admin@kromic.in",
  "role": "SuperUser",
  "type": "superuser",
  "exp": 1234567890
}
```

**Key Claims:**
- `type: "superuser"` - Bypasses tenant resolution middleware
- `role: "SuperUser"` - Platform-wide authorization
- `sub` - SuperUser ID (UUID)
- `email` - SuperUser email

### SuperUser vs Regular User Authentication

| Feature | SuperUser | Regular User |
|---------|-----------|--------------|
| Endpoint | `/superuser/auth/login` | `/auth/login` |
| Tenant Association | None (null) | Required |
| Token Type Claim | `type: superuser` | Not present |
| Tenant Resolution | Bypassed | Required |
| Access Scope | Platform-wide | Tenant-specific |
| Registration | No public endpoint | Available |

### Using SuperUser Token

Include the SuperUser access token in requests:

```bash
GET /api/v1/admin/config
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

The `type: superuser` claim allows the request to bypass tenant resolution.

### SuperUser Refresh Token

**Endpoint:** `POST /api/v1/superuser/auth/refresh`

**Request Body:**
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Note:** This endpoint is currently not fully implemented.

### SuperUser Account Creation

SuperUser accounts are created directly in the database (no public registration). Use database migrations or direct database operations.

**Example SQL:**
```sql
INSERT INTO "SuperUsers" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (gen_random_uuid(), 'admin@kromic.in', 'Admin', 'User', '<hashed_password>', true, NOW(), NOW());
```

**Password Hashing:** Use BCrypt or similar secure hashing algorithm.

---

## Multi-Tenancy

### Tenant Scope

All endpoints operate within the authenticated user's tenant scope. When you make a request, the system automatically:

1. Extracts the tenant ID from the JWT token
2. Filters all queries to include only that tenant's data
3. Enforces data isolation at the database level

### Tenant Identification in Responses

Responses include tenant information:

```json
{
  "data": {
    "id": "product-123",
    "tenantId": "tenant-456",
    "name": "Product Name"
  },
  "meta": {
    "tenantId": "tenant-456",
    "requestId": "req-123"
  }
}
```

### Cross-Tenant Operations

The API prohibits cross-tenant operations. If you attempt to access or modify data from a different tenant, you receive:

```json
{
  "error": {
    "code": "RESOURCE_NOT_FOUND",
    "message": "The requested resource was not found"
  }
}
```

### Tenant Switching

SuperUsers can operate within multiple tenants by providing credentials for each tenant. Regular TenantAdmins and Users are restricted to their single tenant.

---

## Caching & Performance

### Cache Headers

The API implements HTTP caching via standard headers:

```
Cache-Control: private, max-age=300
ETag: "abc123def456"
Last-Modified: Mon, 15 Jan 2024 10:30:00 GMT
```

### Cache Behavior

| Endpoint | TTL | Cache Strategy |
|----------|-----|-----------------|
| GET /products | 5 minutes | Public (CDN) |
| GET /products/{id} | 5 minutes | Public (CDN) |
| GET /categories | 1 hour | Public (CDN) |
| GET /orders | None | Private (no cache) |
| GET /account | None | Private (no cache) |

### Cache Invalidation

Caches are automatically invalidated when:

- Resource is created (POST endpoint)
- Resource is updated (PUT/PATCH endpoint)
- Resource is deleted (DELETE endpoint)

Related caches are also invalidated:
- Updating a Product invalidates product list cache
- Updating a Category invalidates product list cache (if category changed)

### Conditional Requests

Use ETags and Last-Modified headers to reduce bandwidth:

```javascript
// First request
GET /api/v1/products/123
Response: 200 OK
ETag: "abc123"
Last-Modified: Mon, 15 Jan 2024 10:30:00 GMT

// Subsequent request (if resource might have changed)
GET /api/v1/products/123
If-None-Match: "abc123"
If-Modified-Since: Mon, 15 Jan 2024 10:30:00 GMT

Response: 304 Not Modified (saves bandwidth)
```

---

## Webhooks

### Webhook Events

The API sends webhooks for important events:

| Event | Payload | When |
|-------|---------|------|
| order.created | Order data | New order placed |
| order.updated | Order data | Order status changed |
| payment.received | Payment data | Payment confirmed |
| product.created | Product data | New product added |
| customer.registered | Customer data | New customer signup |

### Webhook Format

Webhooks are sent as POST requests with signature verification:

```json
{
  "id": "webhook-event-123",
  "type": "order.created",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "id": "order-456",
    "customerId": "customer-789",
    "total": 99.99,
    "items": [...]
  }
}
```

**Headers:**
```
X-KromicStore-Signature: sha256=abc123def456
X-KromicStore-Timestamp: 2024-01-15T10:30:00Z
X-KromicStore-Event: order.created
```

### Webhook Security

All webhooks include HMAC-SHA256 signature for verification:

```javascript
const crypto = require('crypto');
const signature = req.headers['x-kromic-store-signature'];
const timestamp = req.headers['x-kromic-store-timestamp'];
const body = JSON.stringify(req.body);

// Verify signature
const hash = crypto
  .createHmac('sha256', WEBHOOK_SECRET)
  .update(body)
  .digest('hex');

const expectedSignature = `sha256=${hash}`;
if (signature !== expectedSignature) {
  throw new Error('Invalid signature');
}

// Verify timestamp (prevent replay attacks)
const eventTime = new Date(timestamp).getTime();
const now = Date.now();
if (Math.abs(now - eventTime) > 5 * 60 * 1000) { // 5 minutes
  throw new Error('Webhook timestamp too old');
}
```

### Webhook Delivery

Webhooks are delivered with the following guarantees:

- **At-least-once delivery**: Webhook may be delivered multiple times
- **In-order delivery**: Events are delivered in chronological order
- **Retry logic**: Failed deliveries retry up to 5 times with exponential backoff
- **Idempotency**: Use `id` and `timestamp` fields for deduplication

---

## Best Practices

### API Usage

1. **Use Pagination**: Always paginate list endpoints to avoid large payloads
   ```
   GET /api/v1/products?page=1&pageSize=20
   ```

2. **Filter Results**: Use query parameters to filter on the server side
   ```
   GET /api/v1/products?status=published&categoryId=cat-123
   ```

3. **Handle Rate Limits**: Implement exponential backoff when rate limited
   ```javascript
   const wait = (ms) => new Promise(resolve => setTimeout(resolve, ms));
   let backoff = 1000;
   while (true) {
     try {
       const response = await fetch(url);
       if (response.status === 429) {
         await wait(backoff);
         backoff *= 2;
       } else {
         return response;
       }
     } catch (error) {
       await wait(backoff);
       backoff *= 2;
     }
   }
   ```

4. **Cache Responses**: Implement client-side caching for frequently accessed data
   ```javascript
   const cache = new Map();
   async function getProduct(id) {
     if (cache.has(id)) {
       return cache.get(id);
     }
     const response = await fetch(`/api/v1/products/${id}`);
     const data = await response.json();
     cache.set(id, data);
     return data;
   }
   ```

5. **Use ETags**: Implement conditional requests to save bandwidth
   ```javascript
   const etag = localStorage.getItem(`product-${id}-etag`);
   const headers = etag ? { 'If-None-Match': etag } : {};
   const response = await fetch(`/api/v1/products/${id}`, { headers });
   ```

### Error Handling

1. **Handle All Error Codes**: Implement handlers for all documented error codes
2. **Provide User Feedback**: Display meaningful error messages to users
3. **Log Errors**: Log all errors with correlation IDs for debugging
4. **Retry Transient Errors**: Automatically retry on 5xx status codes

### Security

1. **Store Tokens Securely**: Use httpOnly cookies for token storage
2. **HTTPS Only**: Always use HTTPS in production
3. **Validate Webhooks**: Always verify webhook signatures
4. **Rotate Credentials**: Regularly rotate API keys and refresh tokens
5. **Principle of Least Privilege**: Only request necessary permissions

### Performance

1. **Minimize Requests**: Batch related operations where possible
2. **Use Compression**: Enable gzip compression for request/response bodies
3. **Implement Caching**: Cache frequently accessed data
4. **Monitor Response Times**: Track API response times and investigate slow queries
5. **Use CDN**: Deliver static assets via CDN

### Monitoring

1. **Track Request Metrics**: Monitor API response times, error rates, and throughput
2. **Set Up Alerts**: Alert when error rate exceeds threshold
3. **Log All Requests**: Maintain audit trail of all API operations
4. **Use Correlation IDs**: Include correlation IDs in all logs for tracing

---

## Rate Limiting

### Rate Limit Headers

All responses include rate limit information:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642256400
```

### Rate Limit Tiers

| Tier | Requests/Hour | Requests/Day |
|------|---------------|--------------|
| Starter | 1,000 | 10,000 |
| Professional | 5,000 | 50,000 |
| Enterprise | 50,000 | 500,000 |

### Exceeding Limits

When rate limit is exceeded:

```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "You have exceeded the rate limit",
    "retryAfter": 3600
  }
}
```

---

## Versioning

### API Versioning Strategy

The API uses URL versioning (v1, v2, etc.). When new versions are released:

1. Old versions remain supported for 12 months
2. Deprecation warnings are sent 6 months before removal
3. Migration guides are provided for new versions

### Version Upgrade Path

To upgrade to a new API version:

1. Review breaking changes in migration guide
2. Update client code to use new endpoints
3. Test thoroughly in staging environment
4. Deploy to production with ability to rollback

---

## Support & Documentation

### Documentation Links

- **API Reference**: https://api.kromic-store.com/docs
- **Webhook Guide**: [See Webhook-Consumer-Guide.md]
- **Error Reference**: [See Frontend-Error-Handling.md]
- **Code Examples**: [See examples/api-client.ts]

### Getting Help

1. **Check Documentation**: Review error handling and troubleshooting guides
2. **Search Issues**: Check GitHub issues for known problems
3. **Contact Support**: Reach out to support@kromic-store.com for assistance
4. **Community Forum**: Discuss with other developers on community forum

---

## Quick Start Examples

### Register a New Tenant

```bash
curl -X POST https://api.kromic.in/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "My Store",
    "subdomain": "mystore",
    "email": "admin@mystore.com",
    "firstName": "John",
    "lastName": "Doe",
    "password": "SecurePassword123!",
    "country": "IN"
  }'

# Response
{
  "data": {
    "tenantId": "tenant-123",
    "userId": "user-456",
    "accessToken": "eyJhbGc...",
    "refreshToken": "eyJhbGc...",
    "expiresIn": 3600
  }
}
```

### Create a Product

```bash
curl -X POST https://api.kromic.in/api/v1/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGc..." \
  -d '{
    "name": "Awesome Product",
    "description": "This is a great product",
    "price": 29.99,
    "categoryId": "cat-123",
    "stock": 100,
    "sku": "PROD-001"
  }'

# Response
{
  "data": {
    "id": "product-123",
    "name": "Awesome Product",
    "price": 29.99,
    "categoryId": "cat-123",
    "stock": 100,
    "status": "Draft",
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

### Publish a Product

```bash
curl -X POST https://api.kromic.in/api/v1/products/product-123/publish \
  -H "Authorization: Bearer eyJhbGc..."

# Response
{
  "data": {
    "id": "product-123",
    "name": "Awesome Product",
    "status": "Published"
  }
}
```

### Place an Order

```bash
curl -X POST https://api.kromic.in/api/v1/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGc..." \
  -d '{
    "customerId": "customer-123",
    "items": [
      {
        "productId": "product-123",
        "quantity": 2
      }
    ],
    "shippingAddress": {
      "street": "123 Main St",
      "city": "New York",
      "state": "NY",
      "postalCode": "10001",
      "country": "US"
    },
    "billingAddress": {
      "street": "123 Main St",
      "city": "New York",
      "state": "NY",
      "postalCode": "10001",
      "country": "US"
    }
  }'

# Response
{
  "data": {
    "id": "order-123",
    "orderNumber": "ORD-20240115-ABC123",
    "customerId": "customer-123",
    "status": "Pending",
    "total": 59.98,
    "items": [
      {
        "productId": "product-123",
        "quantity": 2,
        "unitPrice": 29.99
      }
    ]
  }
}
```

### Confirm Payment

```bash
curl -X POST https://api.kromic.in/api/v1/payments/verify \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGc..." \
  -d '{
    "orderId": "order-123",
    "razorpayPaymentId": "pay_ABC123",
    "razorpayOrderId": "order_DEF456",
    "razorpaySignature": "signature123"
  }'

# Response
{
  "data": {
    "id": "payment-123",
    "orderId": "order-123",
    "amount": 59.98,
    "status": "Completed",
    "paidAt": "2024-01-15T10:30:00Z"
  }
}
```

---

## Changelog

### Version 1.0 (Current)

- Initial API release
- Product management
- Order management
- Customer management
- Payment integration
- Webhook support
- Configuration management

### Upcoming Features

- Advanced analytics
- Bulk operations
- Scheduled reports
- Custom fields
- API rate limit upgrades

---

## Conclusion

This guide covers the essential aspects of integrating with the KromicStore API. For detailed endpoint documentation, see the [Frontend-API-Reference.md](Frontend-API-Reference.md) file.

For implementation examples, refer to the [examples/api-client.ts](examples/api-client.ts) TypeScript client library.

For specific workflow examples, see the individual flow documentation files in the `docs/flows/` directory.
