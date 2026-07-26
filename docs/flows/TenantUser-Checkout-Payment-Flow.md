# TenantUser Checkout & Payment Flow

Complete checkout workflow from cart to payment confirmation including order creation and payment processing.

## Overview

This flow covers the complete purchase process: creating orders, processing payments via Razorpay, and order confirmation.

## Complete Flow with Examples

### Step 1: Retrieve or Create Customer Profile

**Endpoint**: `GET /api/v1/customers/profile` or `POST /api/v1/customers`

For new customers (self-register):
```bash
curl -X POST https://api.kromic-store.com/api/v1/customers \
  -H "Content-Type: application/json" \
  -d '{
    "email": "customer@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+1-555-0123"
  }'
```

**Response** (201):
```json
{
  "data": {
    "id": "customer-123",
    "email": "customer@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

### Step 2: Create Order

**Endpoint**: `POST /api/v1/orders`

```bash
curl -X POST https://api.kromic-store.com/api/v1/orders \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-123",
    "items": [
      {
        "productId": "product-123",
        "quantity": 1
      },
      {
        "productId": "product-456",
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
```

**Response** (201):
```json
{
  "data": {
    "id": "order-abc123",
    "orderNumber": "ORD-20240115-XYZ789",
    "customerId": "customer-123",
    "status": "Pending",
    "total": 599.99,
    "subtotal": 549.99,
    "tax": 44.00,
    "shipping": 6.00,
    "items": [
      {
        "productId": "product-123",
        "productName": "Wireless Headphones",
        "quantity": 1,
        "unitPrice": 199.99,
        "lineTotal": 199.99
      },
      {
        "productId": "product-456",
        "productName": "USB Cable",
        "quantity": 2,
        "unitPrice": 175.00,
        "lineTotal": 350.00
      }
    ],
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

**Error Scenarios**:
- `409`: Insufficient inventory for one or more items
- `422`: Invalid shipping address format
- `404`: Product not found

### Step 3: Initiate Payment

**Endpoint**: `POST /api/v1/payments/create`

```bash
curl -X POST https://api.kromic-store.com/api/v1/payments/create \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "order-abc123",
    "amount": 599.99,
    "currency": "USD"
  }'
```

**Response** (200):
```json
{
  "data": {
    "id": "payment-xyz789",
    "orderId": "order-abc123",
    "amount": 599.99,
    "currency": "USD",
    "status": "Processing",
    "razorpayOrderId": "order_KA9xvMbPXxhZiR",
    "razorpayKey": "rzp_live_DHfFBXvM87yyXZ",
    "shortUrl": "https://rzp.io/i/w8Ujz1g8"
  }
}
```

### Step 4: Display Payment Modal (Frontend)

Display Razorpay Checkout modal with payment details:

```javascript
const options = {
  key: response.data.razorpayKey,
  amount: response.data.amount * 100, // Amount in paise
  currency: response.data.currency,
  name: "My Awesome Store",
  description: "Order: " + orderNumber,
  order_id: response.data.razorpayOrderId,
  handler: function(paymentResponse) {
    // Payment successful
    verifyPayment(paymentResponse);
  },
  prefill: {
    name: "John Doe",
    email: "customer@example.com",
    contact: "9999999999"
  },
  theme: {
    color: "#3399cc"
  }
};

const rzp = new Razorpay(options);
rzp.open();
```

### Step 5: Verify Payment

**Endpoint**: `POST /api/v1/payments/verify`

```bash
curl -X POST https://api.kromic-store.com/api/v1/payments/verify \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "order-abc123",
    "razorpayPaymentId": "pay_KA9xvMbPXxhZiR",
    "razorpayOrderId": "order_KA9xvMbPXxhZiR",
    "razorpaySignature": "9ef4dffbfd84f1318f6739a3ce19f9d85851857ae648f114332d8401e0949a3d"
  }'
```

**Success Response** (200):
```json
{
  "data": {
    "id": "payment-xyz789",
    "orderId": "order-abc123",
    "status": "Completed",
    "amount": 599.99,
    "paidAt": "2024-01-15T10:35:00Z",
    "orderStatus": "Confirmed"
  }
}
```

**Error Responses**:
- `400`: Signature verification failed
- `404`: Order or payment not found

### Step 6: Display Order Confirmation

**Endpoint**: `GET /api/v1/orders/{id}`

```bash
curl -X GET https://api.kromic-store.com/api/v1/orders/order-abc123 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "id": "order-abc123",
    "orderNumber": "ORD-20240115-XYZ789",
    "status": "Confirmed",
    "total": 599.99,
    "items": [...],
    "payment": {
      "id": "payment-xyz789",
      "status": "Completed",
      "paidAt": "2024-01-15T10:35:00Z"
    },
    "shippingAddress": {...},
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

### Step 7: Send Confirmation Email

System automatically sends confirmation email to customer with:
- Order details and order number
- Payment confirmation
- Estimated delivery date
- Tracking link (once shipped)

## Complete Checkout Implementation (JavaScript)

```javascript
class CheckoutClient {
  constructor(apiBaseUrl) {
    this.apiBaseUrl = apiBaseUrl;
  }

  async createOrder(customerId, items, shippingAddress, billingAddress) {
    const response = await fetch(`${this.apiBaseUrl}/orders`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        customerId,
        items,
        shippingAddress,
        billingAddress
      })
    });
    return response.json();
  }

  async initiatePayment(orderId, amount, currency = 'USD') {
    const response = await fetch(`${this.apiBaseUrl}/payments/create`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ orderId, amount, currency })
    });
    return response.json();
  }

  async verifyPayment(orderId, razorpayPaymentId, razorpayOrderId, razorpaySignature) {
    const response = await fetch(`${this.apiBaseUrl}/payments/verify`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        orderId,
        razorpayPaymentId,
        razorpayOrderId,
        razorpaySignature
      })
    });
    return response.json();
  }

  async getOrder(orderId) {
    const response = await fetch(`${this.apiBaseUrl}/orders/${orderId}`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    return response.json();
  }

  getToken() {
    return localStorage.getItem('accessToken');
  }
}

// Usage
async function handleCheckout() {
  const checkout = new CheckoutClient('https://api.kromic-store.com/api/v1');

  try {
    // Step 1: Create order
    const orderResponse = await checkout.createOrder(
      customerId,
      items,
      shippingAddress,
      billingAddress
    );

    if (!orderResponse.data) throw new Error('Failed to create order');

    const orderId = orderResponse.data.id;
    const orderTotal = orderResponse.data.total;

    // Step 2: Initiate payment
    const paymentResponse = await checkout.initiatePayment(orderId, orderTotal);
    if (!paymentResponse.data) throw new Error('Failed to initiate payment');

    // Step 3: Show Razorpay modal
    const options = {
      key: paymentResponse.data.razorpayKey,
      amount: orderTotal * 100,
      order_id: paymentResponse.data.razorpayOrderId,
      handler: async function(res) {
        // Step 4: Verify payment
        const verifyResponse = await checkout.verifyPayment(
          orderId,
          res.razorpay_payment_id,
          res.razorpay_order_id,
          res.razorpay_signature
        );

        if (verifyResponse.data?.status === 'Completed') {
          // Step 5: Show confirmation
          showOrderConfirmation(orderId);
        }
      }
    };

    const rzp = new Razorpay(options);
    rzp.open();
  } catch (error) {
    console.error('Checkout error:', error);
    showError(error.message);
  }
}
```

## Payment States and Transitions

```
Order Creation: Pending
       ↓
Payment Initiation: Processing
       ↓
Payment Verification Success: Confirmed
       ↓
Shipment: Shipped
       ↓
Delivery: Delivered
```

## Error Handling

```javascript
async function handlePaymentError(error) {
  const errorMap = {
    'INSUFFICIENT_INVENTORY': 'Some items are no longer in stock',
    'PAYMENT_FAILED': 'Payment processing failed. Please try again',
    'INVALID_ADDRESS': 'Please check your shipping address',
    'NETWORK_ERROR': 'Network connection failed'
  };

  const message = errorMap[error.code] || 'An error occurred';
  showUserError(message);
  logError(error);
}
```

## Best Practices

1. **Save Cart**: Auto-save cart in localStorage
2. **Validate Addresses**: Validate before order creation
3. **Handle Failures**: Implement retry logic for failed payments
4. **Track Progress**: Show checkout progress indicator
5. **Security**: Never log sensitive payment info
6. **Confirmation**: Send order confirmation email immediately
7. **Tracking**: Provide order tracking link

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for general API patterns.
See [TenantUser-Order-Tracking-Flow.md](TenantUser-Order-Tracking-Flow.md) for order tracking.
