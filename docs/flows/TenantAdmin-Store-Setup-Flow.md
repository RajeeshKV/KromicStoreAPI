# TenantAdmin Store Setup Flow

Complete workflow for setting up a new store from registration through initial configuration.

## Overview

This flow covers the entire setup process for a TenantAdmin after registering their store, including company information, store branding, payment configuration, and initial product setup.

## Prerequisites

- Valid registration credentials
- Access to email for verification
- Basic store information (name, country, etc.)

## Complete Flow with Examples

### Step 1: Register New Tenant/Store

**Endpoint**: `POST /api/v1/auth/register`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "companyName": "My Awesome Store",
    "email": "admin@mystore.com",
    "password": "SecurePassword123!@#",
    "country": "US"
  }'
```

**Response** (201):
```json
{
  "data": {
    "tenantId": "tenant-a1b2c3d4",
    "userId": "user-e5f6g7h8",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600
  }
}
```

**Next Step**: Email verification (check inbox for verification link)

### Step 2: Verify Email Address

Navigate to verification link sent in email. The system creates default trial subscription automatically.

### Step 3: View Subscription Details

**Endpoint**: `GET /api/v1/subscriptions/current`

```bash
curl -X GET https://api.kromic-store.com/api/v1/subscriptions/current \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "id": "subscription-trial-123",
    "tenantId": "tenant-a1b2c3d4",
    "plan": "Trial",
    "status": "Active",
    "features": {
      "maxUsers": 3,
      "maxProducts": 100,
      "maxApiCallsPerMonth": 10000,
      "webhooksEnabled": true,
      "analyticsEnabled": false
    },
    "trialEndsAt": "2024-02-14T00:00:00Z",
    "startedAt": "2024-01-15T10:30:00Z"
  }
}
```

### Step 4: Configure Tenant Settings

**Endpoint**: `PUT /api/v1/config/{key}`

Configure notification preferences:

```bash
curl -X PUT https://api.kromic-store.com/api/v1/config/notifications:enabled \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "value": true
  }'
```

Configure webhook settings:

```bash
curl -X PUT https://api.kromic-store.com/api/v1/config/webhooks:maxRetries \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "value": 5
  }'
```

**Response** (200):
```json
{
  "data": {
    "key": "notifications:enabled",
    "value": true,
    "updatedAt": "2024-01-15T10:30:00Z"
  }
}
```

### Step 5: Create Product Categories

**Endpoint**: `POST /api/v1/categories`

```bash
curl -X POST https://api.kromic-store.com/api/v1/categories \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Electronics",
    "description": "Electronic devices and gadgets",
    "displayOrder": 1
  }'
```

**Response** (201):
```json
{
  "data": {
    "id": "category-elec-001",
    "tenantId": "tenant-a1b2c3d4",
    "name": "Electronics",
    "description": "Electronic devices and gadgets",
    "displayOrder": 1,
    "productCount": 0,
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

**Create Sub-Category**:

```bash
curl -X POST https://api.kromic-store.com/api/v1/categories \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptops",
    "description": "Portable computers",
    "parentCategoryId": "category-elec-001",
    "displayOrder": 1
  }'
```

**Response** (201):
```json
{
  "data": {
    "id": "category-laptops-001",
    "parentCategoryId": "category-elec-001",
    "name": "Laptops",
    "description": "Portable computers",
    "displayOrder": 1,
    "createdAt": "2024-01-15T10:31:00Z"
  }
}
```

### Step 6: Register Webhook for Order Notifications

**Endpoint**: `POST /api/v1/webhooks`

```bash
curl -X POST https://api.kromic-store.com/api/v1/webhooks \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://mystore.com/webhooks/kromic-store",
    "events": ["order.created", "order.updated", "payment.received"],
    "description": "Main webhook for order and payment notifications"
  }'
```

**Response** (201):
```json
{
  "data": {
    "id": "webhook-123",
    "url": "https://mystore.com/webhooks/kromic-store",
    "events": ["order.created", "order.updated", "payment.received"],
    "secret": "whsec_abc123def456ghi789...",
    "isActive": true,
    "createdAt": "2024-01-15T10:32:00Z"
  }
}
```

**Important**: Store the webhook secret securely - it will only be shown once!

### Step 7: Test Webhook Connection

**Endpoint**: `POST /api/v1/webhooks/{id}/test`

```bash
curl -X POST https://api.kromic-store.com/api/v1/webhooks/webhook-123/test \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "webhookId": "webhook-123",
    "testEventSent": true,
    "message": "Test webhook sent successfully"
  }
}
```

**Verify**: Check your webhook endpoint to confirm receipt

### Step 8: Add Initial Products

**Endpoint**: `POST /api/v1/products`

```bash
curl -X POST https://api.kromic-store.com/api/v1/products \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Dell XPS 13",
    "sku": "DELL-XPS-13-001",
    "description": "Ultra-portable laptop with 13.3\" FHD display",
    "price": 1299.99,
    "categoryId": "category-laptops-001",
    "stock": 50,
    "images": [
      {
        "url": "https://example.com/images/dell-xps-13.jpg",
        "alt": "Dell XPS 13 laptop",
        "displayOrder": 1
      }
    ]
  }'
```

**Response** (201):
```json
{
  "data": {
    "id": "product-dell-xps-13",
    "tenantId": "tenant-a1b2c3d4",
    "name": "Dell XPS 13",
    "sku": "DELL-XPS-13-001",
    "description": "Ultra-portable laptop with 13.3\" FHD display",
    "price": 1299.99,
    "categoryId": "category-laptops-001",
    "stock": 50,
    "status": "Draft",
    "createdAt": "2024-01-15T10:33:00Z"
  }
}
```

### Step 9: Publish Products

**Endpoint**: `POST /api/v1/products/{id}/publish`

```bash
curl -X POST https://api.kromic-store.com/api/v1/products/product-dell-xps-13/publish \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "id": "product-dell-xps-13",
    "status": "Published",
    "publishedAt": "2024-01-15T10:34:00Z"
  }
}
```

### Step 10: Verify Store is Live

**Endpoint**: `GET /api/v1/products` (without admin token)

```bash
curl -X GET https://api.kromic-store.com/api/v1/products?status=published
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "product-dell-xps-13",
      "name": "Dell XPS 13",
      "price": 1299.99,
      "status": "Published",
      "images": [...]
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

## Common Tasks During Setup

### Add Team Member (TenantUser)

```bash
# Note: This would typically be done through an invite flow
# User registration endpoint for team members
POST /api/v1/users/invite
```

### Configure Store Name/Branding

Store branding is typically managed through configuration:

```bash
curl -X PUT https://api.kromic-store.com/api/v1/config/store:name \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{"value": "My Awesome Store"}'
```

### Check Quota Usage

```bash
curl -X GET https://api.kromic-store.com/api/v1/subscriptions/current/usage \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "users": {
      "used": 1,
      "limit": 3
    },
    "products": {
      "used": 1,
      "limit": 100
    },
    "apiCallsThisMonth": {
      "used": 1543,
      "limit": 10000
    }
  }
}
```

## Typical Timeline

1. **Registration**: 5 minutes
2. **Email Verification**: < 2 minutes
3. **Configuration**: 10-15 minutes
4. **Category Setup**: 5-10 minutes
5. **Initial Products**: 15-30 minutes (depending on number)
6. **Webhook Setup**: 5-10 minutes
7. **Testing**: 5-10 minutes

**Total**: 45 minutes to 1.5 hours for initial setup

## Next Steps After Setup

- Customize store branding and theme
- Add more products and categories
- Set up payment methods and shipping
- Configure email notifications
- Invite team members
- Enable advanced features (analytics, reports, etc.)

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for general API usage patterns.
See [TenantAdmin-Product-Management-Flow.md](TenantAdmin-Product-Management-Flow.md) for product management details.
