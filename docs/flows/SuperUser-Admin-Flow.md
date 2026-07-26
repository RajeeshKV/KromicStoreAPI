# SuperUser Admin Flow

Complete workflow for system administrators managing the KromicStore platform.

## Overview

SuperUsers have full platform access and can manage system-wide configurations, view all tenants, and perform administrative tasks that affect all users.

## Prerequisites

- Valid SuperUser account with platform-wide credentials
- Access to admin dashboard
- Proper authorization headers

## Complete Flow with Examples

### Step 1: Login as SuperUser

**Endpoint**: `POST /api/v1/auth/login`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@kromic-store.com",
    "password": "SecurePassword123!"
  }'
```

**Response** (200):
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "user": {
      "id": "super-user-uuid",
      "email": "admin@kromic-store.com",
      "roles": ["SuperUser"],
      "tenantId": null
    }
  }
}
```

### Step 2: View Platform Configuration

**Endpoint**: `GET /api/v1/admin/config`

```bash
curl -X GET https://api.kromic-store.com/api/v1/admin/config \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "sections": {
      "externalServices": {
        "razorpay": {
          "isEnabled": true,
          "testMode": false,
          "liveKeyPrefix": "rzp_live_"
        },
        "cloudinary": {
          "isEnabled": true,
          "cloudName": "your-cloud"
        },
        "brevo": {
          "isEnabled": true,
          "apiKeyConfigured": true
        }
      },
      "featureFlags": {
        "webhooksEnabled": true,
        "multiCurrencyEnabled": false,
        "advancedAnalyticsEnabled": true
      },
      "performance": {
        "cacheTTL": 300,
        "maxConnections": 100,
        "rateLimit": 1000
      }
    }
  }
}
```

### Step 3: Update Platform Configuration

**Endpoint**: `PUT /api/v1/admin/config/{key}`

```bash
curl -X PUT https://api.kromic-store.com/api/v1/admin/config/featureFlags:multiCurrencyEnabled \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "value": true,
    "reason": "Enabling multi-currency support for Q1 2024"
  }'
```

**Response** (200):
```json
{
  "data": {
    "key": "featureFlags:multiCurrencyEnabled",
    "value": true,
    "updatedAt": "2024-01-15T10:30:00Z",
    "updatedBy": "super-user-uuid"
  }
}
```

### Step 4: View Configuration Audit Log

**Endpoint**: `GET /api/v1/admin/config/audit-logs`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/admin/config/audit-logs?page=1&pageSize=50" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "log-123",
      "configKey": "featureFlags:multiCurrencyEnabled",
      "oldValue": false,
      "newValue": true,
      "changedBy": "super-user-uuid",
      "changedByEmail": "admin@kromic-store.com",
      "changedAt": "2024-01-15T10:30:00Z",
      "reason": "Enabling multi-currency support for Q1 2024"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 245,
    "totalPages": 5
  }
}
```

### Step 5: Monitor System Health

**Endpoint**: `GET /health/ready`

```bash
curl -X GET https://api.kromic-store.com/health/ready \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "cache": "Healthy",
    "externalServices": {
      "razorpay": "Healthy",
      "cloudinary": "Healthy",
      "brevo": "Healthy"
    }
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Step 6: View All Tenants (Analytics)

**Endpoint**: `GET /api/v1/admin/tenants` (hypothetical)

```bash
curl -X GET "https://api.kromic-store.com/api/v1/admin/tenants?page=1&pageSize=50&status=Active" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "tenant-123",
      "companyName": "Example Store",
      "email": "admin@example-store.com",
      "status": "Active",
      "plan": "Professional",
      "usersCount": 5,
      "productsCount": 250,
      "ordersCount": 1523,
      "revenue": 45230.50,
      "createdAt": "2023-06-15T10:00:00Z",
      "trialEndsAt": null
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 342
  }
}
```

### Step 7: Suspend a Tenant

**Endpoint**: `POST /api/v1/admin/tenants/{tenantId}/suspend`

```bash
curl -X POST https://api.kromic-store.com/api/v1/admin/tenants/tenant-123/suspend \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Payment failed - no valid payment method on file",
    "notifyTenant": true
  }'
```

**Response** (200):
```json
{
  "data": {
    "tenantId": "tenant-123",
    "status": "Suspended",
    "suspendedAt": "2024-01-15T10:30:00Z",
    "suspendedBy": "super-user-uuid"
  }
}
```

### Step 8: Reactivate a Tenant

**Endpoint**: `POST /api/v1/admin/tenants/{tenantId}/reactivate`

```bash
curl -X POST https://api.kromic-store.com/api/v1/admin/tenants/tenant-123/reactivate \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "tenantId": "tenant-123",
    "status": "Active",
    "reactivatedAt": "2024-01-15T11:00:00Z"
  }
}
```

## Error Scenarios

### Insufficient Permissions

When a non-SuperUser attempts to access SuperUser endpoints:

```json
{
  "error": {
    "code": "INSUFFICIENT_PERMISSIONS",
    "message": "You do not have permission to access this resource",
    "requiredRoles": ["SuperUser"]
  }
}
```

### Configuration Key Not Found

```json
{
  "error": {
    "code": "CONFIGURATION_NOT_FOUND",
    "message": "Configuration key not found: invalid:config:key"
  }
}
```

## Best Practices

1. **Audit Regularly**: Review configuration changes in audit logs weekly
2. **Test Changes**: Test configuration changes in staging environment first
3. **Monitor Health**: Check system health before and after major configuration changes
4. **Document Changes**: Always include a reason when updating configuration
5. **Backup Configurations**: Keep records of all configuration changes
6. **Security**: Store SuperUser credentials securely (use password manager)
7. **Alerts**: Set up alerts for suspicious configuration changes

## Rollback Procedures

### To Revert a Configuration Change

1. Go to audit log and find the previous value
2. Call PUT endpoint with the old value:

```bash
curl -X PUT https://api.kromic-store.com/api/v1/admin/config/featureFlags:multiCurrencyEnabled \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "value": false,
    "reason": "ROLLBACK: Issue with multi-currency in Q1 launch"
  }'
```

3. Verify the change in health check endpoint

## Key Responsibilities

- Monitor platform health and performance
- Manage external service configurations (Razorpay, Cloudinary, Brevo)
- Enable/disable feature flags for all tenants
- Manage rate limiting and performance settings
- Handle tenant suspension/reactivation
- Monitor audit logs for suspicious activity
- Perform system maintenance and updates

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for general API usage patterns.
