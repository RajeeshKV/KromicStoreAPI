# Frontend Error Handling

Comprehensive guide for handling API errors and providing user-friendly error messages.

## HTTP Status Codes

| Code | Meaning | User Message | Action |
|------|---------|--------------|--------|
| 200 | OK | Success | Proceed |
| 201 | Created | Resource created | Proceed |
| 204 | No Content | Operation successful | Proceed |
| 400 | Bad Request | Invalid request | Check input |
| 401 | Unauthorized | Authentication required | Login |
| 403 | Forbidden | Permission denied | Contact admin |
| 404 | Not Found | Resource not found | Search again |
| 409 | Conflict | Data conflict | Resolve conflict |
| 422 | Validation Error | Input validation failed | Fix errors |
| 429 | Rate Limited | Too many requests | Wait and retry |
| 500 | Server Error | Technical error | Contact support |
| 503 | Service Down | Service unavailable | Try later |

## Error Response Format

All error responses follow this structure:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable message",
    "details": [
      {
        "field": "fieldName",
        "code": "FIELD_ERROR_CODE",
        "message": "Field-specific error"
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

## Common Error Codes and Solutions

### INVALID_TOKEN (401)

**Cause**: Access token is expired, invalid, or malformed  
**HTTP Status**: 401  
**User Message**: "Your session has expired. Please log in again."  
**Solution**: Redirect to login page

```javascript
if (error.code === 'INVALID_TOKEN') {
  localStorage.removeItem('accessToken');
  window.location.href = '/login';
}
```

### MISSING_TOKEN (401)

**Cause**: Authorization header missing  
**HTTP Status**: 401  
**User Message**: "Please log in to continue."  
**Solution**: Redirect to login

### INSUFFICIENT_PERMISSIONS (403)

**Cause**: User role lacks required permissions  
**HTTP Status**: 403  
**User Message**: "You don't have permission to perform this action."  
**Solution**: Show error, suggest contacting admin

```javascript
if (error.code === 'INSUFFICIENT_PERMISSIONS') {
  showError('You do not have permission to access this resource. Contact your administrator.');
}
```

### RESOURCE_NOT_FOUND (404)

**Cause**: Requested resource doesn't exist  
**HTTP Status**: 404  
**User Message**: "The item you're looking for doesn't exist."  
**Solution**: Redirect to listing page or home

```javascript
if (error.code === 'RESOURCE_NOT_FOUND') {
  showError('This item is no longer available.');
  setTimeout(() => window.location.href = '/products', 3000);
}
```

### DUPLICATE_RESOURCE (409)

**Cause**: Resource already exists (e.g., duplicate SKU)  
**HTTP Status**: 409  
**User Message**: "This item already exists."  
**Solution**: Show error, allow user to update existing

```javascript
if (error.code === 'DUPLICATE_RESOURCE') {
  showError('This SKU already exists. Would you like to update the existing product?');
}
```

### VALIDATION_ERROR (422)

**Cause**: Input validation failed  
**HTTP Status**: 422  
**User Message**: "Please fix the errors below"  
**Solution**: Show field-specific errors

```javascript
if (error.code === 'VALIDATION_ERROR') {
  error.error.details.forEach(detail => {
    showFieldError(detail.field, detail.message);
  });
}
```

### INSUFFICIENT_INVENTORY (409)

**Cause**: Not enough product stock  
**HTTP Status**: 409  
**User Message**: "This product is out of stock."  
**Solution**: Suggest similar items or notify when available

```javascript
if (error.code === 'INSUFFICIENT_INVENTORY') {
  showError('Sorry, this item is out of stock. We\'ll notify you when it\'s available.');
}
```

### RATE_LIMIT_EXCEEDED (429)

**Cause**: Too many API requests  
**HTTP Status**: 429  
**User Message**: "You're making requests too quickly. Please wait a moment."  
**Solution**: Implement exponential backoff

```javascript
if (error.code === 'RATE_LIMIT_EXCEEDED') {
  const retryAfter = error.retryAfter || 60;
  showError(`Please wait ${retryAfter} seconds before trying again.`);
  
  await wait(retryAfter * 1000);
  retryRequest();
}
```

### EXTERNAL_SERVICE_ERROR (503)

**Cause**: External service (Razorpay, Cloudinary) unavailable  
**HTTP Status**: 503  
**User Message**: "Service temporarily unavailable. Please try again later."  
**Solution**: Show error and retry

```javascript
if (error.code === 'EXTERNAL_SERVICE_ERROR') {
  showError('Payment service temporarily unavailable. Please try again in a moment.');
  showRetryButton();
}
```

## Field Validation Errors

When validation fails, detailed field errors are provided:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": [
      {
        "field": "email",
        "code": "INVALID_EMAIL_FORMAT",
        "message": "Email address is not in valid format"
      },
      {
        "field": "password",
        "code": "PASSWORD_TOO_WEAK",
        "message": "Password must contain at least 1 uppercase letter and 1 special character"
      },
      {
        "field": "price",
        "code": "PRICE_MUST_BE_POSITIVE",
        "message": "Price must be greater than 0"
      }
    ]
  }
}
```

### Field Error Codes

| Field | Error Code | Message |
|-------|-----------|---------|
| email | INVALID_EMAIL_FORMAT | Email not valid |
| email | EMAIL_ALREADY_EXISTS | Email already registered |
| password | PASSWORD_TOO_SHORT | Min 8 characters required |
| password | PASSWORD_TOO_WEAK | Must contain uppercase, number, special char |
| name | REQUIRED_FIELD | This field is required |
| price | PRICE_MUST_BE_POSITIVE | Price must be > 0 |
| sku | DUPLICATE_SKU | SKU already exists |
| stock | NEGATIVE_STOCK | Stock cannot be negative |

## Error Handling Implementation

### Basic Error Handler

```javascript
class ErrorHandler {
  static handle(error, context = {}) {
    console.error('Error:', error);

    // Network error
    if (!error.response) {
      return this.handleNetworkError();
    }

    const { status, data } = error.response;
    const errorCode = data?.error?.code;

    // Route by status code
    switch (status) {
      case 400:
        return this.handleBadRequest(data);
      case 401:
        return this.handleUnauthorized();
      case 403:
        return this.handleForbidden(data);
      case 404:
        return this.handleNotFound(data);
      case 409:
        return this.handleConflict(data);
      case 422:
        return this.handleValidationError(data);
      case 429:
        return this.handleRateLimit(error.response.headers);
      case 500:
        return this.handleServerError(data);
      case 503:
        return this.handleServiceUnavailable();
      default:
        return this.handleUnknownError();
    }
  }

  static handleNetworkError() {
    showError('Network connection error. Please check your internet.');
    return {
      userMessage: 'Network error',
      recoverable: true,
      retry: true
    };
  }

  static handleUnauthorized() {
    localStorage.removeItem('accessToken');
    showError('Your session has expired. Please log in again.');
    window.location.href = '/login';
    return { userMessage: 'Unauthorized', recoverable: false };
  }

  static handleForbidden(data) {
    showError('You do not have permission to perform this action.');
    logWarning(`Permission denied: ${data?.error?.message}`);
    return { userMessage: 'Permission denied', recoverable: false };
  }

  static handleNotFound(data) {
    showError('The requested item was not found.');
    return { userMessage: 'Not found', recoverable: false };
  }

  static handleConflict(data) {
    showError(data?.error?.message || 'This action conflicts with existing data.');
    return { userMessage: 'Conflict', recoverable: false };
  }

  static handleValidationError(data) {
    if (data?.error?.details?.length) {
      data.error.details.forEach(detail => {
        showFieldError(detail.field, detail.message);
      });
    }
    return { userMessage: 'Validation error', recoverable: true };
  }

  static handleRateLimit(headers) {
    const retryAfter = parseInt(headers['retry-after'] || 60);
    showError(`Too many requests. Please wait ${retryAfter} seconds.`);
    return { userMessage: 'Rate limited', recoverable: true, retryAfter };
  }

  static handleServerError(data) {
    logError(`Server error: ${data?.error?.message}`);
    showError('An unexpected error occurred. Please try again later.');
    return { userMessage: 'Server error', recoverable: true };
  }

  static handleServiceUnavailable() {
    showError('Service temporarily unavailable. Please try again later.');
    return { userMessage: 'Service unavailable', recoverable: true };
  }

  static handleUnknownError() {
    showError('An unexpected error occurred.');
    return { userMessage: 'Unknown error', recoverable: true };
  }
}
```

### Advanced Error Handler with Retry

```javascript
class APIClient {
  async request(method, url, data = null, options = {}) {
    const maxRetries = options.maxRetries || 3;
    const backoffMultiplier = options.backoffMultiplier || 2;
    let lastError;

    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        const response = await fetch(url, {
          method,
          headers: this.getHeaders(),
          body: data ? JSON.stringify(data) : null
        });

        if (response.ok) {
          return response.json();
        }

        if (response.status === 429) {
          // Rate limited: wait and retry
          const retryAfter = parseInt(response.headers.get('retry-after') || 60);
          await this.wait(retryAfter * 1000);
          continue;
        }

        if (response.status >= 500 && attempt < maxRetries) {
          // Server error: exponential backoff
          const waitTime = Math.pow(backoffMultiplier, attempt) * 1000;
          await this.wait(waitTime);
          continue;
        }

        lastError = await response.json();
        throw lastError;
      } catch (error) {
        lastError = error;

        if (!this.isRetryable(error) || attempt === maxRetries) {
          throw error;
        }

        const waitTime = Math.pow(backoffMultiplier, attempt) * 1000;
        await this.wait(waitTime);
      }
    }

    throw lastError;
  }

  isRetryable(error) {
    // Retry on network errors and 5xx server errors
    return !error.response || error.response.status >= 500;
  }

  wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  getHeaders() {
    const token = localStorage.getItem('accessToken');
    return {
      'Content-Type': 'application/json',
      'Authorization': token ? `Bearer ${token}` : ''
    };
  }
}
```

## User-Friendly Error Messages

### Map Error Codes to User Messages

```javascript
const errorMessages = {
  INVALID_TOKEN: 'Your session has expired. Please log in again.',
  INSUFFICIENT_PERMISSIONS: 'You do not have permission to perform this action.',
  RESOURCE_NOT_FOUND: 'The requested item was not found.',
  DUPLICATE_RESOURCE: 'This item already exists.',
  VALIDATION_ERROR: 'Please check your input and try again.',
  INSUFFICIENT_INVENTORY: 'This item is currently out of stock.',
  RATE_LIMIT_EXCEEDED: 'You\'re making requests too quickly. Please wait a moment.',
  EXTERNAL_SERVICE_ERROR: 'Service temporarily unavailable. Please try again.',
  INVALID_EMAIL_FORMAT: 'Please enter a valid email address.',
  PASSWORD_TOO_WEAK: 'Password must be at least 8 characters with uppercase, number, and special character.',
  DUPLICATE_SKU: 'This product code (SKU) already exists.',
  NETWORK_ERROR: 'Network connection failed. Please check your internet.',
  SERVER_ERROR: 'An unexpected error occurred. Please try again.'
};

function getUserMessage(errorCode) {
  return errorMessages[errorCode] || 'An error occurred. Please try again.';
}
```

## Logging and Monitoring

### Error Logging Strategy

```javascript
class ErrorLogger {
  static log(error, context = {}) {
    const logEntry = {
      timestamp: new Date().toISOString(),
      code: error.code || 'UNKNOWN',
      message: error.message,
      status: error.status,
      context,
      url: window.location.href,
      userAgent: navigator.userAgent,
      traceId: error.traceId
    };

    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
      console.error('Error Log:', logEntry);
    }

    // Send to monitoring service
    this.sendToMonitoring(logEntry);
  }

  static sendToMonitoring(logEntry) {
    // Send to error tracking service (Sentry, LogRocket, etc.)
    fetch('/api/v1/logs', {
      method: 'POST',
      body: JSON.stringify(logEntry)
    }).catch(() => {
      // Silently fail if logging fails
    });
  }
}
```

## Common Error Scenarios

### Scenario 1: User Enters Invalid Email

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "details": [
      {
        "field": "email",
        "code": "INVALID_EMAIL_FORMAT",
        "message": "Email address is not valid"
      }
    ]
  }
}
```

**Frontend Response**:
```javascript
showFieldError('email', 'Please enter a valid email address.');
```

### Scenario 2: Product Out of Stock

```json
{
  "error": {
    "code": "INSUFFICIENT_INVENTORY",
    "message": "Product XYZ is out of stock"
  }
}
```

**Frontend Response**:
```javascript
showError('Sorry, this product is out of stock.');
notifyWhenAvailable(productId);
```

### Scenario 3: Razorpay Payment Failed

```json
{
  "error": {
    "code": "EXTERNAL_SERVICE_ERROR",
    "message": "Payment gateway error",
    "details": {
      "service": "Razorpay",
      "originalError": "Card declined"
    }
  }
}
```

**Frontend Response**:
```javascript
showError('Payment failed. Please check your card details and try again.');
showRetryButton();
```

---

See [Frontend-Integration-Guide.md](Frontend-Integration-Guide.md) for general API patterns.
