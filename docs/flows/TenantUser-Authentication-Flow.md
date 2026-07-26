# TenantUser Authentication Flow

Complete authentication workflow for end users including registration, login, password reset, and OAuth.

## Overview

This flow covers user authentication including email/password registration, login with credentials, token refresh, Google OAuth integration, and password recovery.

## Prerequisites

- Access to email for registration verification
- Valid credentials for login
- Optional: Google account for OAuth

## Complete Flow with Examples

### Step 1: User Registration

**Endpoint**: `POST /api/v1/auth/register`

A customer can self-register or be created by TenantAdmin:

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "customer@example.com",
    "password": "SecurePassword123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

**Response** (201):
```json
{
  "data": {
    "userId": "user-123",
    "email": "customer@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "message": "Verification email sent. Please check your inbox."
  }
}
```

### Step 2: Verify Email Address

Click the verification link in the email. System sends verification request:

**Endpoint**: `POST /api/v1/auth/verify-email`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "token": "verification-token-from-email"
  }'
```

**Response** (200):
```json
{
  "data": {
    "message": "Email verified successfully",
    "verifiedAt": "2024-01-15T10:30:00Z"
  }
}
```

### Step 3: User Login

**Endpoint**: `POST /api/v1/auth/login`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "customer@example.com",
    "password": "SecurePassword123!"
  }'
```

**Success Response** (200):
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600,
    "user": {
      "id": "user-123",
      "email": "customer@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "roles": ["Customer"]
    }
  }
}
```

**Error Responses**:
- `401`: Invalid email or password
- `403`: Email not verified
- `429`: Too many failed login attempts

### Step 4: Store Tokens Securely

**Recommended Approach** (for web applications):

```javascript
// Store in httpOnly cookie (set by server)
// DO NOT store in localStorage for security reasons

// If must store in localStorage:
localStorage.setItem('accessToken', data.accessToken);
localStorage.setItem('refreshToken', data.refreshToken);
localStorage.setItem('expiresAt', Date.now() + data.expiresIn * 1000);
```

### Step 5: Use Access Token for API Calls

All subsequent API requests include the token:

```bash
curl -X GET https://api.kromic-store.com/api/v1/customers/profile \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Step 6: Refresh Token When Expired

**Endpoint**: `POST /api/v1/auth/refresh`

When access token expires (after 1 hour), use refresh token to get new access token:

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }'
```

**Success Response** (200):
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600
  }
}
```

**Error Responses**:
- `401`: Invalid refresh token
- `401`: Refresh token expired (need to login again)

### Step 7: Forgot Password

**Endpoint**: `POST /api/v1/auth/forgot-password`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "customer@example.com"
  }'
```

**Response** (200):
```json
{
  "data": {
    "message": "Password reset email sent successfully"
  }
}
```

### Step 8: Reset Password with Token

Click the reset link in the email, then:

**Endpoint**: `POST /api/v1/auth/reset-password`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "token": "password-reset-token-from-email",
    "newPassword": "NewSecurePassword456!"
  }'
```

**Success Response** (200):
```json
{
  "data": {
    "message": "Password reset successfully",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

### Step 9: Google OAuth Login

**Step 9a: Get Authorization Code**

Redirect user to Google OAuth consent screen:

```
https://accounts.google.com/o/oauth2/v2/auth?
client_id=YOUR_CLIENT_ID&
redirect_uri=https://your-store.com/callback&
scope=openid%20email%20profile&
response_type=code
```

**Step 9b: Exchange Code for Token**

**Endpoint**: `POST /api/v1/auth/oauth/google`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/oauth/google \
  -H "Content-Type: application/json" \
  -d '{
    "code": "authorization-code-from-google",
    "redirectUri": "https://your-store.com/callback"
  }'
```

**Response for New Account** (201):
```json
{
  "data": {
    "isNewAccount": true,
    "userId": "user-oauth-123",
    "email": "user@gmail.com",
    "firstName": "John",
    "lastName": "Doe",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600
  }
}
```

**Response for Existing Account** (200):
```json
{
  "data": {
    "isNewAccount": false,
    "userId": "user-oauth-123",
    "email": "user@gmail.com",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 3600
  }
}
```

### Step 10: Logout

**Endpoint**: `POST /api/v1/auth/logout`

```bash
curl -X POST https://api.kromic-store.com/api/v1/auth/logout \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "message": "Logged out successfully"
  }
}
```

**Client-side cleanup**:
```javascript
// Clear stored tokens
localStorage.removeItem('accessToken');
localStorage.removeItem('refreshToken');
localStorage.removeItem('expiresAt');

// Redirect to login page
window.location.href = '/login';
```

## Complete Authentication Implementation (JavaScript)

```javascript
class AuthClient {
  constructor(apiBaseUrl) {
    this.apiBaseUrl = apiBaseUrl;
  }

  async register(email, password, firstName, lastName) {
    const response = await fetch(`${this.apiBaseUrl}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password, firstName, lastName })
    });
    return response.json();
  }

  async login(email, password) {
    const response = await fetch(`${this.apiBaseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    const data = await response.json();
    
    if (response.ok) {
      localStorage.setItem('accessToken', data.data.accessToken);
      localStorage.setItem('refreshToken', data.data.refreshToken);
      localStorage.setItem('expiresAt', Date.now() + data.data.expiresIn * 1000);
    }
    
    return data;
  }

  async refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    const response = await fetch(`${this.apiBaseUrl}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    });
    
    const data = await response.json();
    if (response.ok) {
      localStorage.setItem('accessToken', data.data.accessToken);
      localStorage.setItem('expiresAt', Date.now() + data.data.expiresIn * 1000);
    }
    
    return data;
  }

  getAccessToken() {
    return localStorage.getItem('accessToken');
  }

  isTokenExpired() {
    const expiresAt = localStorage.getItem('expiresAt');
    return !expiresAt || Date.now() > expiresAt;
  }

  async logout() {
    const response = await fetch(`${this.apiBaseUrl}/auth/logout`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getAccessToken()}`
      }
    });
    
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    
    return response.json();
  }
}
```

## Security Best Practices

1. **HTTPS Only**: Always use HTTPS in production
2. **Secure Storage**: Use httpOnly cookies for tokens when possible
3. **Token Rotation**: Automatically refresh tokens before expiry
4. **Logout Cleanup**: Clear all stored credentials on logout
5. **CORS Configuration**: Only allow requests from trusted origins
6. **Password Requirements**: Enforce strong passwords (min 8 chars)
7. **Rate Limiting**: Limit failed login attempts
8. **Two-Factor Authentication**: Consider implementing 2FA for sensitive accounts

## Error Handling

```javascript
async function makeAuthenticatedRequest(url, options = {}) {
  let token = authClient.getAccessToken();
  
  // Check if token expired
  if (authClient.isTokenExpired()) {
    const refreshResult = await authClient.refreshToken();
    if (!refreshResult.data?.accessToken) {
      // Redirect to login
      window.location.href = '/login';
      return;
    }
    token = refreshResult.data.accessToken;
  }
  
  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Authorization': `Bearer ${token}`
    }
  });
  
  if (response.status === 401) {
    // Token invalid, redirect to login
    window.location.href = '/login';
  }
  
  return response;
}
```

## Common Workflows

### Automatic Token Refresh

```javascript
// Refresh token 5 minutes before expiry
setInterval(async () => {
  if (authClient.isTokenExpired()) {
    await authClient.refreshToken();
  }
}, 5 * 60 * 1000); // Every 5 minutes
```

### OAuth Integration

```javascript
// Redirect to Google OAuth
function loginWithGoogle() {
  const clientId = 'YOUR_GOOGLE_CLIENT_ID';
  const redirectUri = encodeURIComponent('https://your-store.com/auth/callback');
  window.location.href = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${clientId}&redirect_uri=${redirectUri}&scope=openid%20email%20profile&response_type=code`;
}

// Handle callback
async function handleOAuthCallback(code) {
  const result = await authClient.oauthGoogle(code, 'https://your-store.com/auth/callback');
  if (result.data?.accessToken) {
    localStorage.setItem('accessToken', result.data.accessToken);
    window.location.href = '/dashboard';
  }
}
```

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for general API usage patterns.
