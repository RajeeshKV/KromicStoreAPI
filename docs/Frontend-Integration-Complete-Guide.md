# KromicStore Frontend Integration - Complete Guide

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Request Flow](#request-flow)
3. [Tenant Resolution](#tenant-resolution)
4. [Authentication](#authentication)
5. [Storefront Bootstrap](#storefront-bootstrap)
6. [Public Endpoints](#public-endpoints)
7. [Subdomain Management](#subdomain-management)
8. [Token Management](#token-management)
9. [Error Handling](#error-handling)
10. [Integration Examples](#integration-examples)

---

## Architecture Overview

KromicStore is a multi-tenant SaaS platform where each tenant has their own storefront served from a single frontend deployment. The backend handles tenant resolution, authentication, and data isolation.

### Key Concepts

- **Multi-Tenancy**: Each tenant has isolated data and configuration
- **Domain-Based Routing**: Tenants are identified by their domain (subdomain or custom domain)
- **Single Frontend**: One frontend deployment serves all tenants
- **Tenant Context**: Backend automatically resolves tenant from request hostname
- **Bootstrap Pattern**: Single endpoint provides all storefront initialization data

### System Components

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (Single)                         │
│  - React/Next.js/Vue/Angular (any framework)                   │
│  - Serves all tenant storefronts                                  │
│  - No tenant logic in frontend                                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    KromicStore API Backend                        │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Domain Tenant Resolution Middleware                        │  │
│  │  - Extracts hostname                                        │  │
│  │  - Looks up TenantDomain                                    │  │
│  │  - Validates tenant status                                   │  │
│  │  - Populates ITenantContext                                  │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              │                                   │
│                              ▼                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  JWT Authentication Middleware                               │  │
│  │  - Validates access tokens                                  │  │
│  │  - Checks token version (logout support)                     │  │
│  │  - Bypasses for SuperUser                                    │  │
│  └───────────────────────────────────────────────────────────┘  │
│                              │                                   │
│                              ▼                                   │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Controllers & Services                                     │  │
│  │  - Use ITenantContext for tenant data                       │  │
│  │  - Automatic data isolation                                 │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      PostgreSQL Database                          │
│  - Tenants (tenant data)                                        │
│  - TenantDomains (domain mappings)                              │
│  - TenantThemes (theme configurations)                          │
│  - Products, Orders, Customers (tenant-scoped)                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Request Flow

### Storefront Page Load Flow

```
1. User visits: https://mystore.kromic.in
   │
   ▼
2. Frontend loads (single deployment)
   │
   ▼
3. Frontend calls: GET /api/v1/store/bootstrap
   │
   ▼
4. DomainTenantResolutionMiddleware:
   - Extracts hostname: mystore.kromic.in
   - Normalizes: mystore.kromic.in
   - Looks up TenantDomain table
   - Finds tenant by domain
   - Validates tenant is active
   - Populates ITenantContext
   │
   ▼
5. StoreBootstrapService:
   - Uses ITenantContext to get tenant ID
   - Fetches tenant data
   - Fetches theme configuration
   - Fetches navigation/categories
   - Fetches homepage sections
   - Fetches feature flags
   - Fetches SEO data
   │
   ▼
6. Returns complete bootstrap response
   │
   ▼
7. Frontend renders storefront with bootstrap data
```

### Authentication Flow

```
1. User clicks "Login"
   │
   ▼
2. Frontend calls: POST /api/v1/auth/login
   {
     "email": "user@mystore.com",
     "password": "password123"
   }
   │
   ▼
3. AuthService:
   - Validates credentials
   - Generates JWT with token_version claim
   - Returns access token + refresh token
   │
   ▼
4. Frontend stores tokens (httpOnly cookie recommended)
   │
   ▼
5. Subsequent requests include Authorization header
   Authorization: Bearer eyJhbGc...
   │
   ▼
6. TenantResolutionMiddleware:
   - Extracts tenant_id from JWT
   - Populates ITenantProvider
   │
   ▼
7. Controllers use tenant-scoped data automatically
```

### Logout Flow

```
1. User clicks "Logout"
   │
   ▼
2. Frontend calls: POST /api/v1/auth/logout
   Authorization: Bearer eyJhbGc...
   │
   ▼
3. AuthController:
   - Extracts user ID from JWT
   - Calls AuthService.LogoutAsync(userId)
   │
   ▼
4. AuthService:
   - Increments user's TokenVersion in database
   - All existing tokens become invalid
   │
   ▼
5. Frontend clears stored tokens
   │
   ▼
6. Subsequent requests with old tokens fail (token version mismatch)
```

---

## Tenant Resolution

### Supported Domains

The backend supports both:
- **Kromic Subdomains**: `tenant.kromic.in`
- **Custom Domains**: `store.customerdomain.com`

### Domain Resolution Process

**Middleware**: `DomainTenantResolutionMiddleware`

**Process**:
1. Extract hostname from request
2. Normalize (lowercase, trim, remove trailing periods)
3. Skip if path is `/api/*`, `/health/*`, `/swagger/*`
4. Query `TenantDomains` table for matching domain
5. Validate tenant is active
6. Populate `ITenantContext` with tenant data
7. Short-circuit with 404 if domain not found
8. Short-circuit with 403 if tenant is inactive

### TenantContext Interface

```csharp
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantName { get; }
    string Slug { get; }
    string Domain { get; }
    string Locale { get; }
    string Currency { get; }
    string Timezone { get; }
    bool IsResolved { get; }
}
```

### Frontend Usage

**No action required** - the frontend simply makes requests to the API. The backend automatically resolves the tenant from the hostname.

**Example**:
```javascript
// Frontend on mystore.kromic.in
const response = await fetch('/api/v1/store/bootstrap');
// Backend automatically resolves tenant for mystore.kromic.in
```

### Error Responses

**Unknown Domain (404)**:
```json
{
  "error": "Tenant not found"
}
```

**Inactive Tenant (403)**:
```json
{
  "error": "Tenant is inactive"
}
```

---

## Authentication

### User Roles

| Role | Description | Access Scope |
|------|-------------|--------------|
| SuperUser | Platform administrator | Platform-wide, no tenant association |
| TenantAdmin | Tenant administrator | Single tenant, full access |
| TenantUser | Tenant team member | Single tenant, limited access |
| Customer | End customer | Single tenant, read-only |

### Regular User Authentication

#### Register Tenant

**Endpoint**: `POST /api/v1/auth/register`

**Request**:
```json
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

**Response**:
```json
{
  "data": {
    "tenantId": "tenant-uuid",
    "userId": "user-uuid",
    "accessToken": "eyJhbGc...",
    "refreshToken": "eyJhbGc...",
    "expiresIn": 3600
  }
}
```

#### Login

**Endpoint**: `POST /api/v1/auth/login`

**Request**:
```json
{
  "email": "admin@mystore.com",
  "password": "SecurePassword123!"
}
```

**Response**:
```json
{
  "userId": "user-uuid",
  "email": "admin@mystore.com",
  "firstName": "John",
  "lastName": "Doe",
  "accessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "expiresAt": "2024-01-15T11:30:00Z"
}
```

#### Logout

**Endpoint**: `POST /api/v1/auth/logout`

**Headers**: `Authorization: Bearer {token}`

**Response**:
```json
{
  "message": "Logout successful"
}
```

### SuperUser Authentication

#### Register SuperUser

**Endpoint**: `POST /api/v1/superuser/auth/register`

**Request**:
```json
{
  "email": "admin@kromic.in",
  "password": "SecurePassword123!"
}
```

**Response**:
```json
{
  "data": {
    "id": "super-user-uuid",
    "email": "admin@kromic.in",
    "firstName": "SuperUser",
    "lastName": "Admin",
    "isActive": true,
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

#### Login SuperUser

**Endpoint**: `POST /api/v1/superuser/auth/login`

**Request**:
```json
{
  "email": "admin@kromic.in",
  "password": "SecurePassword123!"
}
```

**Response**:
```json
{
  "userId": "super-user-uuid",
  "email": "admin@kromic.in",
  "firstName": "SuperUser",
  "lastName": "Admin",
  "accessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "expiresAt": "2024-01-15T11:30:00Z"
}
```

#### Logout SuperUser

**Endpoint**: `POST /api/v1/superuser/auth/logout`

**Headers**: `Authorization: Bearer {token}`

**Response**:
```json
{
  "message": "Logout successful"
}
```

### JWT Token Structure

**Regular User Token**:
```json
{
  "sub": "user-uuid",
  "tenant_id": "tenant-uuid",
  "email": "user@example.com",
  "token_version": "1",
  "roles": ["TenantAdmin"],
  "exp": 1234567890
}
```

**SuperUser Token**:
```json
{
  "sub": "super-user-uuid",
  "email": "admin@kromic.in",
  "role": "SuperUser",
  "type": "superuser",
  "token_version": "1",
  "exp": 1234567890
}
```

### Token Storage

**Recommended**: Use httpOnly cookies for security

```javascript
// Set httpOnly cookie
document.cookie = `access_token=${accessToken}; path=/; httpOnly; secure; samesite=strict`;
```

---

## Storefront Bootstrap

### Bootstrap Endpoint

**Endpoint**: `GET /api/v1/store/bootstrap`

**Purpose**: Single endpoint provides all data required to initialize the storefront

**Response**:
```json
{
  "tenant": {
    "id": "tenant-uuid",
    "name": "My Store",
    "slug": "mystore",
    "logoUrl": "https://cdn.example.com/logo.png",
    "status": "active",
    "locale": "en-US",
    "currency": "INR",
    "timezone": "Asia/Kolkata"
  },
  "theme": {
    "primaryColor": "#000000",
    "secondaryColor": "#666666",
    "accentColor": "#007bff",
    "backgroundColor": "#ffffff",
    "textColor": "#333333",
    "fontFamily": "Inter, sans-serif",
    "borderRadius": 8,
    "spacingUnit": 16,
    "componentOverrides": "{}",
    "layoutOptions": "{}"
  },
  "navigation": {
    "headerMenu": [
      {
        "label": "Home",
        "url": "/",
        "opensInNewTab": false
      },
      {
        "label": "Products",
        "url": "/products",
        "opensInNewTab": false
      }
    ],
    "footerMenu": [
      {
        "label": "Privacy Policy",
        "url": "/privacy",
        "opensInNewTab": false
      }
    ],
    "categories": [
      {
        "id": "cat-uuid",
        "name": "Electronics",
        "slug": "electronics",
        "displayOrder": 1,
        "children": []
      }
    ]
  },
  "homepage": {
    "layoutType": "default",
    "sections": [
      {
        "type": "hero",
        "name": "Hero Section",
        "displayOrder": 1,
        "config": {
          "isVisible": true,
          "cssClass": "hero-section",
          "trackingId": "hero-1"
        }
      }
    ]
  },
  "features": {
    "wishlistEnabled": true,
    "reviewsEnabled": true,
    "blogEnabled": false,
    "couponsEnabled": true,
    "multiCurrencyEnabled": false,
    "multiLanguageEnabled": false
  },
  "seo": {
    "siteTitle": "My Store Store",
    "metaDescription": "Welcome to My Store online store",
    "faviconUrl": "https://cdn.example.com/favicon.ico",
    "openGraphImageUrl": "https://cdn.example.com/og-image.png"
  }
}
```

### Frontend Integration

```javascript
// On page load
async function initializeStorefront() {
  try {
    const response = await fetch('/api/v1/store/bootstrap');
    const data = await response.json();
    
    // Apply theme
    applyTheme(data.theme);
    
    // Render navigation
    renderNavigation(data.navigation);
    
    // Render homepage
    renderHomepage(data.homepage);
    
    // Configure features
    configureFeatures(data.features);
    
    // Set SEO metadata
    setSeoMetadata(data.seo);
    
  } catch (error) {
    // Handle error (tenant not found, etc.)
    console.error('Bootstrap failed:', error);
  }
}

function applyTheme(theme) {
  document.documentElement.style.setProperty('--primary-color', theme.primaryColor);
  document.documentElement.style.setProperty('--secondary-color', theme.secondaryColor);
  document.documentElement.style.setProperty('--accent-color', theme.accentColor);
  document.documentElement.style.setProperty('--background-color', theme.backgroundColor);
  document.documentElement.style.setProperty('--text-color', theme.textColor);
  document.documentElement.style.setProperty('--font-family', theme.fontFamily);
  document.documentElement.style.setProperty('--border-radius', `${theme.borderRadius}px`);
  document.documentElement.style.setProperty('--spacing-unit', `${theme.spacingUnit}px`);
}
```

---

## Public Endpoints

Public endpoints are accessible without authentication and provide essential information for the landing page.

### Get Subscription Plans

**Endpoint**: `GET /api/v1/public/plans`

**Response**:
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

### Get Platform Configuration

**Endpoint**: `GET /api/v1/public/config`

**Response**:
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

### Contact Us Form

**Endpoint**: `POST /api/v1/public/contactus`

**Request**:
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "phone": "+91-9876543210",
  "subject": "Partnership Inquiry",
  "message": "I would like to discuss a potential partnership..."
}
```

**Response**:
```json
{
  "message": "Contact form submitted successfully"
}
```

---

## Subdomain Management

### Subdomain Selection

Each tenant selects a unique subdomain during registration (e.g., `mystore.kromic.in`).

### Subdomain Validation Rules

- **Length**: 3-63 characters
- **Characters**: Alphanumeric (a-z, 0-9) and hyphens (-) only
- **Format**: Must start and end with alphanumeric character
- **Case**: Automatically converted to lowercase
- **Reserved**: Cannot use reserved subdomains

### Reserved Subdomains

The following subdomains are reserved:
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

**Endpoint**: `GET /api/v1/public/subdomain/check?subdomain=mystore`

**Response (Available)**:
```json
{
  "available": true
}
```

**Response (Not Available)**:
```json
{
  "available": false,
  "reason": "Subdomain is already taken"
}
```

**Response (Reserved)**:
```json
{
  "available": false,
  "reason": "Subdomain is reserved"
}
```

**Response (Invalid Format)**:
```json
{
  "available": false,
  "reason": "Invalid subdomain format. Only alphanumeric characters and hyphens are allowed."
}
```

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

## Token Management

### Token Versioning

Each user has a `TokenVersion` in the database. When a user logs out, this version is incremented, invalidating all existing tokens.

### JWT Token with Token Version

```json
{
  "sub": "user-uuid",
  "tenant_id": "tenant-uuid",
  "email": "user@example.com",
  "token_version": "1",
  "roles": ["TenantAdmin"],
  "exp": 1234567890
}
```

### Token Validation

The middleware validates:
1. Token signature
2. Token expiration
3. Token version matches database value

If token version doesn't match, the token is rejected (logout occurred).

### Token Refresh

**Endpoint**: `POST /api/v1/auth/refresh`

**Request**:
```json
{
  "refreshToken": "refresh-token-uuid"
}
```

**Response**:
```json
{
  "userId": "user-uuid",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "accessToken": "new-access-token",
  "refreshToken": "new-refresh-token",
  "expiresAt": "2024-01-15T12:30:00Z"
}
```

---

## Error Handling

### HTTP Status Codes

| Status | Meaning | Example |
|--------|---------|---------|
| 200 | OK - Request succeeded | GET /api/v1/store/bootstrap |
| 201 | Created - Resource created | POST /api/v1/auth/register |
| 204 | No Content - Successful, no response body | DELETE /api/v1/products/123 |
| 400 | Bad Request - Invalid request data | POST with missing required field |
| 401 | Unauthorized - Missing or invalid token | Missing Authorization header |
| 403 | Forbidden - Insufficient permissions or inactive tenant | TenantUser accessing admin endpoint |
| 404 | Not Found - Resource doesn't exist or tenant not found | GET /api/v1/store/bootstrap (unknown domain) |
| 409 | Conflict - Business logic violation | Creating product with duplicate SKU |
| 422 | Unprocessable Entity - Validation error | POST with invalid email format |
| 429 | Too Many Requests - Rate limit exceeded | Exceeded API rate limit |
| 500 | Internal Server Error - Server error | Unexpected exception |

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
      }
    ],
    "traceId": "correlation-id-uuid"
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

## Integration Examples

### React Example

```javascript
// api.js
const API_BASE = '/api/v1';

export const api = {
  // Bootstrap
  getBootstrap: async () => {
    const response = await fetch(`${API_BASE}/store/bootstrap`);
    if (!response.ok) throw new Error('Bootstrap failed');
    return response.json();
  },

  // Auth
  login: async (email, password) => {
    const response = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    if (!response.ok) throw new Error('Login failed');
    return response.json();
  },

  logout: async () => {
    const token = localStorage.getItem('access_token');
    const response = await fetch(`${API_BASE}/auth/logout`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Logout failed');
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  },

  // Products
  getProducts: async () => {
    const token = localStorage.getItem('access_token');
    const response = await fetch(`${API_BASE}/products`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) throw new Error('Failed to fetch products');
    return response.json();
  }
};

// App.js
import { api } from './api';
import { useEffect, useState } from 'react';

function App() {
  const [bootstrap, setBootstrap] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadBootstrap() {
      try {
        const data = await api.getBootstrap();
        setBootstrap(data);
        applyTheme(data.theme);
      } catch (error) {
        console.error('Failed to load bootstrap:', error);
      } finally {
        setLoading(false);
      }
    }
    loadBootstrap();
  }, []);

  if (loading) return <div>Loading...</div>;
  if (!bootstrap) return <div>Store not found</div>;

  return (
    <div>
      <Navigation menu={bootstrap.navigation.headerMenu} />
      <Homepage sections={bootstrap.homepage.sections} />
      <Footer menu={bootstrap.navigation.footerMenu} />
    </div>
  );
}
```

### Next.js Example

```javascript
// lib/api.js
const API_BASE = process.env.NEXT_PUBLIC_API_BASE || '/api/v1';

export async function fetchBootstrap() {
  const response = await fetch(`${API_BASE}/store/bootstrap`, {
    cache: 'no-store' // Always fetch fresh data
  });
  if (!response.ok) throw new Error('Bootstrap failed');
  return response.json();
}

// app/page.js
import { fetchBootstrap } from '@/lib/api';

export default async function HomePage() {
  const bootstrap = await fetchBootstrap();

  return (
    <div>
      <Navigation menu={bootstrap.navigation.headerMenu} />
      <Homepage sections={bootstrap.homepage.sections} />
      <Footer menu={bootstrap.navigation.footerMenu} />
    </div>
  );
}
```

### Vue.js Example

```javascript
// composables/useBootstrap.js
import { ref, onMounted } from 'vue';

export function useBootstrap() {
  const bootstrap = ref(null);
  const loading = ref(true);
  const error = ref(null);

  onMounted(async () => {
    try {
      const response = await fetch('/api/v1/store/bootstrap');
      if (!response.ok) throw new Error('Bootstrap failed');
      bootstrap.value = await response.json();
    } catch (err) {
      error.value = err.message;
    } finally {
      loading.value = false;
    }
  });

  return { bootstrap, loading, error };
}

// components/Storefront.vue
<script setup>
import { useBootstrap } from '@/composables/useBootstrap';

const { bootstrap, loading, error } = useBootstrap();
</script>

<template>
  <div v-if="loading">Loading...</div>
  <div v-else-if="error">{{ error }}</div>
  <div v-else-if="bootstrap">
    <Navigation :menu="bootstrap.navigation.headerMenu" />
    <Homepage :sections="bootstrap.homepage.sections" />
    <Footer :menu="bootstrap.navigation.footerMenu" />
  </div>
</template>
```

---

## Summary

### Key Points for Frontend Developers

1. **No Tenant Logic in Frontend**: The backend handles all tenant resolution via domain
2. **Single Bootstrap Call**: Use `/api/v1/store/bootstrap` for all initialization data
3. **Domain-Based Routing**: Deploy once, serve all tenants via different domains
4. **Token Versioning**: Logout invalidates all tokens by incrementing version
5. **Public Endpoints**: Use for landing page (plans, config, contact us)
6. **Subdomain Validation**: Check availability before registration
7. **Error Handling**: Implement proper error handling for 404/403 responses

### API Base URLs

```
Production:   https://storeapi.kromic.in/api/v1
```

### Required Headers

```
Accept: application/json
Content-Type: application/json
Authorization: Bearer {access_token} (for authenticated requests)
```

### Support

For issues or questions:
- Check this documentation first
- Review error messages and status codes
- Contact support@kromic.in for assistance
