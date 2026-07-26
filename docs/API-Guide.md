# KromicStore API Guide

## Overview
Multi-tenant SaaS e-commerce API built on ASP.NET Core 8.

## Authentication
- POST /api/v1/auth/register → returns accessToken + refreshToken
- POST /api/v1/auth/login → same
- All subsequent requests: Authorization: Bearer {accessToken}
- Token expires: accessToken 1 hour, refreshToken 30 days
- Refresh: POST /api/v1/auth/refresh with refreshToken

## Multi-Tenancy
- TenantId is embedded in the JWT token (claim: tenantId)
- All API responses are automatically scoped to your tenant
- Cross-tenant access returns 404 (not 403) to prevent information leakage

## Rate Limiting
Response headers on every request:
- X-RateLimit-Limit: max requests per window
- X-RateLimit-Remaining: requests remaining
- X-RateLimit-Reset: Unix timestamp when window resets

Limits by plan: Starter 1000/hr, Professional 5000/hr, Enterprise 20000/hr

## Error Handling
All errors return ErrorResponse:
```json
{
  "code": "VALIDATION_ERROR",
  "message": "Human-readable message",
  "details": { "field": ["error"] }
}
```

HTTP Status codes:
- 400 Bad Request — validation failure
- 401 Unauthorized — missing/invalid token
- 403 Forbidden — insufficient role
- 404 Not Found — resource not found (also used for cross-tenant)
- 409 Conflict — duplicate resource or invalid state transition
- 422 Unprocessable — business rule violation
- 500 Internal Server Error — unexpected server error

## Webhook Integration
See /api/v1/webhooks endpoints.
Signature validation: X-KromicStore-Signature header (HMAC-SHA256).
Retry delays: 1s, 10s, 100s, 1000s, 10000s.

## API Versioning
Current version: v1 (prefix: /api/v1/)
Breaking changes will use v2 with 6-month overlap support.

## OpenAPI Export
Download spec: GET /swagger/v1/swagger.json
Use with openapi-generator or nswag for client SDK generation.
