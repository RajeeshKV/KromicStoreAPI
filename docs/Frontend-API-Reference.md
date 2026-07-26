# KromicStore Frontend API Reference

Complete API endpoint reference with request/response examples, error codes, and status codes.

## Authentication Endpoints

### Register New Tenant

**Endpoint**: `POST /api/v1/auth/register`  
**Authentication**: None (public)  
**Rate Limit**: 10 requests per hour per IP

**Request Body**:
```json
{
  "companyName": "string (required, 3-255 chars)",
  "email": "string (required, valid email format)",
  "password": "string (required, min 8 chars, 1 uppercase, 1 number, 1 special)",
  "country": "string (required, 2-letter ISO code)"
}
```

**Success Response (201)**:
```json
{
  "data": {
    "tenantId": "uuid",
    "userId": "uuid",
    "accessToken": "jwt-token",
    "refreshToken": "jwt-token",
    "expiresIn": 3600
  }
}
```

**Error Responses**:
- `400`: Invalid request format
- `409`: Email already registered
- `422`: Validation failed (password too weak, invalid email)

---

### Login

**Endpoint**: `POST /api/v1/auth/login`  
**Authentication**: None (public)  
**Rate Limit**: 20 requests per hour per IP

**Request Body**:
```json
{
  "email": "string (required, valid email)",
  "password": "string (required)"
}
```

**Success Response (200)**:
```json
{
  "data": {
    "accessToken": "jwt-token",
    "refreshToken": "jwt-token",
    "expiresIn": 3600,
    "user": {
      "id": "uuid",
      "email": "user@example.com",
      "roles": ["TenantAdmin"],
      "tenantId": "uuid"
    }
  }
}
```

**Error Responses**:
- `401`: Invalid credentials
- `429`: Too many failed login attempts (account temporarily locked)

---

### Refresh Token

**Endpoint**: `POST /api/v1/auth/refresh`  
**Authentication**: Bearer token (refresh token in body)  
**Rate Limit**: 100 requests per hour

**Request Body**:
```json
{
  "refreshToken": "string (required)"
}
```

**Success Response (200)**:
```json
{
  "data": {
    "accessToken": "jwt-token",
    "refreshToken": "jwt-token",
    "expiresIn": 3600
  }
}
```

**Error Responses**:
- `401`: Invalid refresh token
- `401`: Refresh token expired

---

### Google OAuth

**Endpoint**: `POST /api/v1/auth/oauth/google`  
**Authentication**: None (public)

**Request Body**:
```json
{
  "code": "string (OAuth authorization code)",
  "redirectUri": "string (must match registered URI)"
}
```

**Success Response (200 or 201)**:
```json
{
  "data": {
    "isNewAccount": false,
    "accessToken": "jwt-token",
    "refreshToken": "jwt-token",
    "expiresIn": 3600,
    "user": {
      "id": "uuid",
      "email": "user@gmail.com",
      "tenantId": "uuid"
    }
  }
}
```

---

## Product Endpoints

### List Products

**Endpoint**: `GET /api/v1/products`  
**Authentication**: Bearer token required  
**Role**: Customer+

**Query Parameters**:
- `page` (integer, default: 1)
- `pageSize` (integer, default: 20, max: 100)
- `status` (enum: Draft, Published, Archived)
- `categoryId` (uuid, optional)
- `minPrice` (decimal, optional)
- `maxPrice` (decimal, optional)
- `search` (string, searches name and description)
- `sort` (string, default: createdAt)
- `order` (string, default: desc)

**Success Response (200)**:
```json
{
  "data": [
    {
      "id": "uuid",
      "tenantId": "uuid",
      "name": "Product Name",
      "sku": "SKU-001",
      "description": "Product description",
      "price": 99.99,
      "categoryId": "uuid",
      "stock": 100,
      "status": "Published",
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

**Error Responses**:
- `401`: Unauthorized
- `403`: Insufficient permissions

---

### Get Product Details

**Endpoint**: `GET /api/v1/products/{id}`  
**Authentication**: Bearer token required  
**Role**: Customer+

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "tenantId": "uuid",
    "name": "Product Name",
    "sku": "SKU-001",
    "description": "Detailed product description",
    "price": 99.99,
    "categoryId": "uuid",
    "category": {
      "id": "uuid",
      "name": "Category Name"
    },
    "stock": 100,
    "status": "Published",
    "images": [
      {
        "id": "uuid",
        "url": "https://cdn.example.com/image.jpg",
        "alt": "Product image",
        "displayOrder": 1
      }
    ],
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  }
}
```

**Error Responses**:
- `401`: Unauthorized
- `404`: Product not found

---

### Create Product

**Endpoint**: `POST /api/v1/products`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Request Body**:
```json
{
  "name": "string (required, 1-255 chars)",
  "sku": "string (required, unique per tenant, 1-50 chars)",
  "description": "string (optional, max 5000 chars)",
  "price": "decimal (required, > 0)",
  "categoryId": "uuid (required)",
  "stock": "integer (required, >= 0)",
  "images": [
    {
      "url": "string (required, valid URL)",
      "alt": "string (optional)",
      "displayOrder": "integer (optional, default: 0)"
    }
  ]
}
```

**Success Response (201)**:
```json
{
  "data": {
    "id": "uuid",
    "name": "Product Name",
    "sku": "SKU-001",
    "description": "...",
    "price": 99.99,
    "categoryId": "uuid",
    "stock": 100,
    "status": "Draft",
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

**Error Responses**:
- `400`: Invalid request format
- `403`: Insufficient permissions
- `409`: SKU already exists
- `422`: Validation failed

---

### Update Product

**Endpoint**: `PUT /api/v1/products/{id}`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Request Body**: Same as Create Product (all fields optional except where specified)

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "name": "Updated Product Name",
    "updatedAt": "2024-01-15T11:00:00Z"
  }
}
```

**Error Responses**:
- `403`: Insufficient permissions
- `404`: Product not found
- `409`: Cannot update published product without unpublishing

---

### Publish Product

**Endpoint**: `POST /api/v1/products/{id}/publish`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Request Body**: None

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "status": "Published",
    "publishedAt": "2024-01-15T11:00:00Z"
  }
}
```

**Error Responses**:
- `409`: Stock quantity must be > 0 to publish
- `409`: Product already published

---

### Unpublish Product

**Endpoint**: `POST /api/v1/products/{id}/unpublish`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "status": "Draft",
    "unpublishedAt": "2024-01-15T11:00:00Z"
  }
}
```

---

### Delete Product

**Endpoint**: `DELETE /api/v1/products/{id}`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Success Response (204)**: No content

**Error Responses**:
- `403`: Insufficient permissions
- `404`: Product not found

---

## Order Endpoints

### List Orders

**Endpoint**: `GET /api/v1/orders`  
**Authentication**: Bearer token required  
**Role**: TenantUser+ (TenantUser sees only their orders)

**Query Parameters**:
- `page` (integer, default: 1)
- `pageSize` (integer, default: 20, max: 100)
- `status` (enum: Pending, Confirmed, Paid, Shipped, Delivered, Cancelled)
- `customerId` (uuid, optional, TenantAdmin only)
- `minDate` (ISO date, optional)
- `maxDate` (ISO date, optional)

**Success Response (200)**:
```json
{
  "data": [
    {
      "id": "uuid",
      "orderNumber": "ORD-20240115-ABC123",
      "customerId": "uuid",
      "status": "Delivered",
      "total": 199.99,
      "subtotal": 179.99,
      "tax": 14.40,
      "shipping": 5.60,
      "itemCount": 2,
      "createdAt": "2024-01-15T10:30:00Z",
      "shippedAt": "2024-01-17T14:00:00Z",
      "deliveredAt": "2024-01-19T10:00:00Z"
    }
  ],
  "pagination": { ... }
}
```

---

### Get Order Details

**Endpoint**: `GET /api/v1/orders/{id}`  
**Authentication**: Bearer token required

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "orderNumber": "ORD-20240115-ABC123",
    "customerId": "uuid",
    "status": "Delivered",
    "total": 199.99,
    "subtotal": 179.99,
    "tax": 14.40,
    "shipping": 5.60,
    "items": [
      {
        "id": "uuid",
        "productId": "uuid",
        "productName": "Product Name",
        "productSku": "SKU-001",
        "quantity": 2,
        "unitPrice": 89.99,
        "lineTotal": 179.99
      }
    ],
    "shippingAddress": { ... },
    "billingAddress": { ... },
    "payment": {
      "id": "uuid",
      "status": "Completed",
      "method": "razorpay",
      "paidAt": "2024-01-16T10:00:00Z"
    },
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

---

### Create Order

**Endpoint**: `POST /api/v1/orders`  
**Authentication**: Bearer token required  
**Role**: TenantUser+

**Request Body**:
```json
{
  "customerId": "uuid (required)",
  "items": [
    {
      "productId": "uuid (required)",
      "quantity": "integer (required, > 0)"
    }
  ],
  "shippingAddress": {
    "street": "string (required)",
    "city": "string (required)",
    "state": "string (required)",
    "postalCode": "string (required)",
    "country": "string (required, 2-letter ISO)"
  },
  "billingAddress": {
    "street": "string (required)",
    "city": "string (required)",
    "state": "string (required)",
    "postalCode": "string (required)",
    "country": "string (required)"
  }
}
```

**Success Response (201)**:
```json
{
  "data": {
    "id": "uuid",
    "orderNumber": "ORD-20240115-ABC123",
    "status": "Pending",
    "total": 199.99,
    "items": [ ... ]
  }
}
```

**Error Responses**:
- `400`: Invalid request format
- `409`: Insufficient inventory
- `422`: Validation failed

---

### Confirm Order

**Endpoint**: `POST /api/v1/orders/{id}/confirm`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "status": "Confirmed",
    "confirmedAt": "2024-01-15T11:00:00Z"
  }
}
```

---

### Ship Order

**Endpoint**: `POST /api/v1/orders/{id}/ship`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Request Body** (optional):
```json
{
  "trackingNumber": "string (optional)",
  "carrier": "string (optional)"
}
```

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "status": "Shipped",
    "shippedAt": "2024-01-17T14:00:00Z"
  }
}
```

---

### Deliver Order

**Endpoint**: `POST /api/v1/orders/{id}/deliver`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "status": "Delivered",
    "deliveredAt": "2024-01-19T10:00:00Z"
  }
}
```

---

### Cancel Order

**Endpoint**: `POST /api/v1/orders/{id}/cancel`  
**Authentication**: Bearer token required

**Request Body**:
```json
{
  "reason": "string (optional, max 500 chars)"
}
```

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "status": "Cancelled",
    "cancelledAt": "2024-01-16T10:00:00Z",
    "refund": {
      "id": "uuid",
      "amount": 199.99,
      "status": "Initiated"
    }
  }
}
```

---

## Payment Endpoints

### Create Payment

**Endpoint**: `POST /api/v1/payments/create`  
**Authentication**: Bearer token required

**Request Body**:
```json
{
  "orderId": "uuid (required)",
  "amount": "decimal (optional, defaults to order total)",
  "currency": "string (default: USD, 3-letter ISO code)"
}
```

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "orderId": "uuid",
    "amount": 199.99,
    "currency": "USD",
    "status": "Processing",
    "razorpayOrderId": "order_ABC123",
    "razorpayKey": "rzp_key_123",
    "shortUrl": "https://rzp.io/i/ABC123"
  }
}
```

---

### Verify Payment

**Endpoint**: `POST /api/v1/payments/verify`  
**Authentication**: Bearer token required

**Request Body**:
```json
{
  "orderId": "uuid (required)",
  "razorpayPaymentId": "string (required)",
  "razorpayOrderId": "string (required)",
  "razorpaySignature": "string (required)"
}
```

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "orderId": "uuid",
    "status": "Completed",
    "amount": 199.99,
    "paidAt": "2024-01-15T11:00:00Z",
    "orderStatus": "Confirmed"
  }
}
```

**Error Responses**:
- `400`: Signature verification failed
- `404`: Order or payment not found

---

## Customer Endpoints

### List Customers

**Endpoint**: `GET /api/v1/customers`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Query Parameters**:
- `page` (integer, default: 1)
- `pageSize` (integer, default: 20)
- `search` (string, searches email and name)

**Success Response (200)**:
```json
{
  "data": [
    {
      "id": "uuid",
      "email": "customer@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "phoneNumber": "+1-555-0123",
      "orderCount": 5,
      "lifetimeValue": 1299.99,
      "lastOrderAt": "2024-01-15T10:30:00Z",
      "createdAt": "2024-01-01T10:00:00Z"
    }
  ],
  "pagination": { ... }
}
```

---

### Get Customer Details

**Endpoint**: `GET /api/v1/customers/{id}`  
**Authentication**: Bearer token required

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "email": "customer@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1-555-0123",
    "orderCount": 5,
    "lifetimeValue": 1299.99,
    "newsletterSubscribed": true,
    "verifiedAt": "2024-01-01T10:00:00Z",
    "billingAddress": { ... },
    "shippingAddress": { ... },
    "createdAt": "2024-01-01T10:00:00Z"
  }
}
```

---

### Create Customer

**Endpoint**: `POST /api/v1/customers`  
**Authentication**: Bearer token required or None (self-registration)

**Request Body**:
```json
{
  "email": "string (required, valid email, unique)",
  "firstName": "string (required, 1-50 chars)",
  "lastName": "string (required, 1-50 chars)",
  "phoneNumber": "string (optional, valid format)",
  "password": "string (optional for admin creation)"
}
```

**Success Response (201)**:
```json
{
  "data": {
    "id": "uuid",
    "email": "customer@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

---

### Update Customer

**Endpoint**: `PUT /api/v1/customers/{id}`  
**Authentication**: Bearer token required

**Request Body**: Same fields as Create (all optional)

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "email": "customer@example.com",
    "updatedAt": "2024-01-15T11:00:00Z"
  }
}
```

---

## Webhook Endpoints

### Register Webhook

**Endpoint**: `POST /api/v1/webhooks`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Request Body**:
```json
{
  "url": "string (required, valid HTTPS URL)",
  "events": [
    "string (enum: order.created, order.updated, payment.received, ...)"
  ],
  "description": "string (optional)"
}
```

**Success Response (201)**:
```json
{
  "data": {
    "id": "uuid",
    "url": "https://example.com/webhook",
    "events": ["order.created", "payment.received"],
    "secret": "whsec_abc123def456...",
    "isActive": true,
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

---

### List Webhooks

**Endpoint**: `GET /api/v1/webhooks`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Success Response (200)**:
```json
{
  "data": [
    {
      "id": "uuid",
      "url": "https://example.com/webhook",
      "events": ["order.created"],
      "isActive": true,
      "lastDeliveredAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

### Test Webhook

**Endpoint**: `POST /api/v1/webhooks/{id}/test`  
**Authentication**: Bearer token required

**Success Response (200)**:
```json
{
  "data": {
    "id": "uuid",
    "testEvent": "Webhook test event sent successfully"
  }
}
```

---

### Delete Webhook

**Endpoint**: `DELETE /api/v1/webhooks/{id}`  
**Authentication**: Bearer token required  
**Role**: TenantAdmin+

**Success Response (204)**: No content

---

## Health & Status Endpoints

### Health Check

**Endpoint**: `GET /health`  
**Authentication**: None

**Success Response (200)**:
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### Readiness Check

**Endpoint**: `GET /health/ready`  
**Authentication**: None

**Success Response (200)**:
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "cache": "Healthy"
  }
}
```

---

End of API Reference. See [Frontend-Integration-Guide.md](Frontend-Integration-Guide.md) for detailed integration instructions.
