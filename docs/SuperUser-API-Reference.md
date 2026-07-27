# SuperUser API Reference

Complete API reference for SuperUser (platform admin) authentication and public endpoints.

## Overview

SuperUser is a separate entity from regular tenant users and has full platform-wide access. SuperUser authentication uses a dedicated endpoint and generates JWT tokens with a special `type: superuser` claim that bypasses tenant resolution middleware.

## Base URL

```
https://api.kromicstore.com/api/v1
```

---

## Authentication Endpoints

### SuperUser Register

Register a new SuperUser (platform admin) account.

**Endpoint:** `POST /superuser/auth/register`

**Request Body:**
```json
{
  "email": "admin@kromicstore.com",
  "firstName": "Admin",
  "lastName": "User",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "data": {
    "id": "super-user-uuid",
    "email": "admin@kromicstore.com",
    "firstName": "Admin",
    "lastName": "User",
    "isActive": true,
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

**Error Responses:**

- **400 Bad Request:**
```json
{
  "error": "Email address is already registered"
}
```

- **400 Bad Request (Validation):**
```json
{
  "error": "Email is required"
}
```

- **500 Internal Server Error:**
```json
{
  "error": "An error occurred during registration"
}
```

**Note:** This endpoint should be protected or restricted in production. Currently, it's publicly accessible for initial setup.

---

### SuperUser Login

Authenticates a SuperUser with email and password.

**Endpoint:** `POST /superuser/auth/login`

**Request Body:**
```json
{
  "email": "admin@kromicstore.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "user": {
      "id": "super-user-uuid",
      "email": "admin@kromicstore.com",
      "roles": ["SuperUser"],
      "tenantId": null
    }
  }
}
```

**Error Responses:**

- **401 Unauthorized:**
```json
{
  "error": "Invalid email or password"
}
```

- **500 Internal Server Error:**
```json
{
  "error": "An error occurred during login"
}
```

**Notes:**
- The `accessToken` expires in 1 hour (3600 seconds)
- The `tenantId` is always `null` for SuperUser
- Include the `accessToken` in the `Authorization: Bearer <token>` header for subsequent requests

---

### Refresh SuperUser Token

Refreshes an expired SuperUser authentication token.

**Endpoint:** `POST /superuser/auth/refresh`

**Request Body:**
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (200 OK):**
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "user": {
      "id": "super-user-uuid",
      "email": "admin@kromicstore.com",
      "roles": ["SuperUser"],
      "tenantId": null
    }
  }
}
```

**Error Responses:**

- **401 Unauthorized:**
```json
{
  "error": "Invalid refresh token"
}
```

- **500 Internal Server Error:**
```json
{
  "error": "An error occurred during token refresh"
}
```

**Note:** This endpoint is currently not fully implemented and will throw `NotImplementedException`.

---

## Public Endpoints

These endpoints are accessible without authentication.

### Get Subscription Plans

Retrieves available subscription plans for new tenant sign-ups.

**Endpoint:** `GET /public/plans`

**Response (200 OK):**
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

**Error Responses:**

- **500 Internal Server Error:**
```json
{
  "error": "Failed to retrieve subscription plans"
}
```

---

### Get SuperUser Configuration

Retrieves platform-wide configuration including contact details for footer/contact page.

**Endpoint:** `GET /public/config`

**Response (200 OK):**
```json
{
  "data": {
    "contactEmail": "support@kromicstore.com",
    "contactPhone": "+91-9876543210",
    "supportEmail": "support@kromicstore.com",
    "companyName": "KromicStore",
    "websiteUrl": "https://kromicstore.com",
    "instagramUrl": "https://instagram.com/kromicstore"
  }
}
```

**Field Descriptions:**
- `contactEmail` - Primary contact email displayed on contact page
- `contactPhone` - Contact phone number (optional)
- `supportEmail` - Support email for customer inquiries
- `companyName` - Company name for branding
- `websiteUrl` - Main website URL
- `instagramUrl` - Instagram profile URL (if configured, used to show footer icon)

**Error Responses:**

- **500 Internal Server Error:**
```json
{
  "error": "Failed to retrieve configuration"
}
```

---

### Contact Us Form

Submits a contact us form and sends an email to the SuperUser.

**Endpoint:** `POST /public/contactus`

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

**Response (200 OK):**
```json
{
  "message": "Contact form submitted successfully"
}
```

**Error Responses:**

- **400 Bad Request:**
```json
{
  "error": "Name, email, and message are required"
}
```

- **500 Internal Server Error:**
```json
{
  "error": "Failed to send email"
}
```

**Note:** The email is sent to `rajeeshkva2z@gmail.com` (hardcoded for now).

---

## Authentication Flow

### 1. Login
```bash
curl -X POST https://api.kromicstore.com/api/v1/superuser/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@kromicstore.com",
    "password": "SecurePassword123!"
  }'
```

### 2. Use Access Token
```bash
curl -X GET https://api.kromicstore.com/api/v1/admin/config \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 3. Token Expiry Handling
When the access token expires (after 1 hour), use the refresh token to get a new access token:
```bash
curl -X POST https://api.kromicstore.com/api/v1/superuser/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }'
```

---

## JWT Token Structure

The SuperUser JWT token contains the following claims:

```json
{
  "sub": "super-user-uuid",
  "email": "admin@kromicstore.com",
  "role": "SuperUser",
  "type": "superuser",
  "exp": 1234567890
}
```

**Key Claims:**
- `type: "superuser"` - This claim is used by the TenantResolutionMiddleware to bypass tenant requirement
- `role: "SuperUser"` - Role claim for authorization
- `sub` - SuperUser ID (UUID)
- `email` - SuperUser email

---

## Tenant Resolution Behavior

The `TenantResolutionMiddleware` handles SuperUser authentication differently:

1. **Check for `type: superuser` claim** - If present, bypasses tenant resolution entirely
2. **No tenant association** - SuperUser has `tenantId: null` in the database
3. **Platform-wide access** - SuperUser can access all admin endpoints without tenant context

This allows SuperUser to:
- Access platform configuration endpoints
- View all tenants
- Manage system-wide settings
- Perform administrative tasks across all tenants

---

## Error Handling

All endpoints follow a consistent error response format:

```json
{
  "error": "Error message description"
}
```

Common HTTP status codes:
- `200 OK` - Success
- `400 Bad Request` - Invalid request parameters
- `401 Unauthorized` - Authentication failed or missing
- `500 Internal Server Error` - Server-side error

---

## Rate Limiting

Public endpoints may be subject to rate limiting. Check the `Rate-Limit` response headers for current limits:

```
Rate-Limit-Limit: 100
Rate-Limit-Remaining: 95
Rate-Limit-Reset: 1690000000
```

---

## CORS

The API supports CORS for the following origins (configurable):
- `https://app.kromicstore.com`
- `https://admin.kromicstore.com`

---

## SuperUser Account Creation

SuperUser accounts are created directly in the database (no public registration endpoint). Use database migrations or direct database operations to create SuperUser accounts.

**Example SQL:**
```sql
INSERT INTO "SuperUsers" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "UpdatedAt")
VALUES (gen_random_uuid(), 'admin@kromicstore.com', 'Admin', 'User', '<hashed_password>', true, NOW(), NOW());
```

**Password Hashing:** Use BCrypt or similar secure hashing algorithm. The current implementation uses simple comparison (TODO: implement proper password hashing).

---

## SuperUser Configuration Management

SuperUser configuration is stored in the `SuperUserConfigs` table as key-value pairs:

**Example SQL:**
```sql
INSERT INTO "SuperUserConfigs" ("ConfigKey", "ConfigValue", "Description", "CreatedAt", "UpdatedAt")
VALUES 
  ('contact_email', 'support@kromicstore.com', 'Primary contact email', NOW(), NOW()),
  ('contact_phone', '+91-9876543210', 'Contact phone number', NOW(), NOW()),
  ('instagram_url', 'https://instagram.com/kromicstore', 'Instagram profile URL', NOW(), NOW());
```

---

## Security Considerations

1. **JWT Secret:** Ensure `JWT_SECRET` environment variable is set with a strong, randomly generated value (minimum 32 characters)
2. **HTTPS:** Always use HTTPS in production
3. **Token Storage:** Store tokens securely on the client side (e.g., httpOnly cookies or secure storage)
4. **Password Hashing:** Implement proper password hashing (BCrypt recommended)
5. **Rate Limiting:** Implement rate limiting on authentication endpoints to prevent brute force attacks

---

## Future Enhancements

- [ ] Implement proper password hashing (BCrypt)
- [ ] Implement refresh token storage and validation
- [ ] Add SuperUser account creation endpoint (protected)
- [ ] Add SuperUser account management endpoints (update password, deactivate)
- [ ] Add SuperUser configuration management endpoints (CRUD)
- [ ] Implement multi-factor authentication (MFA)
- [ ] Add audit logging for SuperUser actions
