# TenantUser Order Tracking Flow

Complete order tracking workflow for customers to monitor order status, view details, and request support.

## Overview

This flow covers how customers track their orders after purchase including status updates, tracking information, and order history.

## Complete Flow with Examples

### Step 1: View Order History

**Endpoint**: `GET /api/v1/orders`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/orders?page=1&pageSize=20" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "order-abc123",
      "orderNumber": "ORD-20240115-XYZ789",
      "status": "Shipped",
      "total": 599.99,
      "itemCount": 2,
      "createdAt": "2024-01-15T10:30:00Z",
      "shippedAt": "2024-01-17T14:00:00Z"
    }
  ],
  "pagination": {"page": 1, "pageSize": 20, "totalCount": 5}
}
```

### Step 2: View Order Details

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
    "status": "Shipped",
    "total": 599.99,
    "subtotal": 549.99,
    "tax": 44.00,
    "shipping": 6.00,
    "items": [
      {
        "id": "item-1",
        "productId": "product-123",
        "productName": "Wireless Headphones",
        "productSku": "WH-001",
        "quantity": 1,
        "unitPrice": 199.99,
        "lineTotal": 199.99
      },
      {
        "id": "item-2",
        "productId": "product-456",
        "productName": "USB Cable",
        "productSku": "USB-CABLE-001",
        "quantity": 2,
        "unitPrice": 175.00,
        "lineTotal": 350.00
      }
    ],
    "shippingAddress": {
      "street": "123 Main St",
      "city": "New York",
      "state": "NY",
      "postalCode": "10001",
      "country": "US"
    },
    "payment": {
      "id": "payment-xyz789",
      "method": "razorpay",
      "status": "Completed",
      "paidAt": "2024-01-15T10:35:00Z"
    },
    "timeline": [
      {
        "status": "Pending",
        "timestamp": "2024-01-15T10:30:00Z"
      },
      {
        "status": "Confirmed",
        "timestamp": "2024-01-15T10:35:00Z"
      },
      {
        "status": "Shipped",
        "timestamp": "2024-01-17T14:00:00Z",
        "tracking": {
          "carrier": "FedEx",
          "trackingNumber": "794610437591",
          "url": "https://tracking.fedex.com/794610437591"
        }
      }
    ],
    "createdAt": "2024-01-15T10:30:00Z",
    "shippedAt": "2024-01-17T14:00:00Z"
  }
}
```

### Step 3: Track Shipment

**From Response**: Get tracking information from timeline:

```json
{
  "status": "Shipped",
  "tracking": {
    "carrier": "FedEx",
    "trackingNumber": "794610437591",
    "url": "https://tracking.fedex.com/794610437591",
    "estimatedDelivery": "2024-01-19T18:00:00Z"
  }
}
```

**Next Steps**: 
- Click carrier URL to view real-time tracking
- Expected delivery by: January 19, 2024

### Step 4: Monitor Order Status Changes

Real-time status updates are sent via webhooks and email notifications:

```json
{
  "event": "order.updated",
  "data": {
    "orderId": "order-abc123",
    "orderNumber": "ORD-20240115-XYZ789",
    "status": "Delivered",
    "deliveredAt": "2024-01-19T10:00:00Z",
    "message": "Your order has been delivered"
  }
}
```

### Step 5: Check Estimated Delivery

The order details include estimated delivery dates at each stage:

```json
{
  "estimatedTimeline": {
    "confirmed": "2024-01-15T10:35:00Z",
    "shipped": "2024-01-17T14:00:00Z",
    "estimatedDelivery": "2024-01-19T18:00:00Z"
  }
}
```

### Step 6: Request Support

**Endpoint**: `POST /api/v1/orders/{id}/support`

```bash
curl -X POST https://api.kromic-store.com/api/v1/orders/order-abc123/support \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "subject": "Missing item",
    "description": "The USB cable is missing from my order",
    "category": "missing_item"
  }'
```

**Response** (201):
```json
{
  "data": {
    "ticketId": "support-ticket-123",
    "orderId": "order-abc123",
    "subject": "Missing item",
    "status": "Open",
    "createdAt": "2024-01-19T10:00:00Z"
  }
}
```

### Step 7: View Returns & Refunds

**Endpoint**: `GET /api/v1/orders/{id}/returns`

```bash
curl -X GET https://api.kromic-store.com/api/v1/orders/order-abc123/returns \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "return-123",
      "orderId": "order-abc123",
      "itemId": "item-1",
      "productName": "Wireless Headphones",
      "reason": "Defective",
      "status": "Approved",
      "refundAmount": 199.99,
      "initiatedAt": "2024-01-20T10:00:00Z",
      "shippingLabel": "https://returns.example.com/label-123"
    }
  ]
}
```

## Order Status Flow

```
Pending (Order Created)
   ↓
Confirmed (Payment Received)
   ↓
Processing (Preparing for Shipment)
   ↓
Shipped (In Transit) - Tracking Available
   ↓
Delivered (Order Completed)

Alternative Paths:
Cancelled (Before Shipment) → Refund Processing
Failed (Payment Failed) → Payment Retry
```

## Tracking Status Descriptions

| Status | Description | Action |
|--------|-------------|--------|
| Pending | Order created, awaiting confirmation | Payment processing |
| Confirmed | Payment received successfully | Preparing shipment |
| Processing | Preparing order for shipment | Check back soon |
| Shipped | Order dispatched with carrier | Track shipment |
| In Transit | On the way to delivery address | Check carrier tracking |
| Delivered | Order delivered successfully | Confirm receipt |
| Cancelled | Order cancelled by customer | N/A |
| Returned | Item returned for refund | Check refund status |

## Complete Order Tracking Implementation (JavaScript)

```javascript
class OrderTrackingClient {
  constructor(apiBaseUrl) {
    this.apiBaseUrl = apiBaseUrl;
  }

  async getOrders(page = 1, pageSize = 20) {
    const response = await fetch(
      `${this.apiBaseUrl}/orders?page=${page}&pageSize=${pageSize}`,
      {
        headers: { 'Authorization': `Bearer ${this.getToken()}` }
      }
    );
    return response.json();
  }

  async getOrderDetails(orderId) {
    const response = await fetch(`${this.apiBaseUrl}/orders/${orderId}`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    return response.json();
  }

  async requestSupport(orderId, subject, description, category) {
    const response = await fetch(`${this.apiBaseUrl}/orders/${orderId}/support`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ subject, description, category })
    });
    return response.json();
  }

  async requestReturn(orderId, itemId, reason) {
    const response = await fetch(`${this.apiBaseUrl}/orders/${orderId}/returns`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ itemId, reason })
    });
    return response.json();
  }

  getToken() {
    return localStorage.getItem('accessToken');
  }
}

// Usage: Track order status with polling
async function trackOrder(orderId, pollInterval = 60000) {
  const tracker = new OrderTrackingClient('https://api.kromic-store.com/api/v1');

  setInterval(async () => {
    try {
      const response = await tracker.getOrderDetails(orderId);
      const order = response.data;

      console.log(`Order ${order.orderNumber}: ${order.status}`);

      if (order.timeline?.length > 0) {
        const latestUpdate = order.timeline[order.timeline.length - 1];
        updateUI(latestUpdate);
      }

      // Check for tracking info
      if (order.status === 'Shipped' && order.tracking) {
        displayTrackingLink(order.tracking.url);
      }
    } catch (error) {
      console.error('Error tracking order:', error);
    }
  }, pollInterval);
}

// Display real-time order status
function displayOrderTimeline(order) {
  const timeline = order.timeline || [];
  
  return timeline.map((event, index) => ({
    status: event.status,
    timestamp: new Date(event.timestamp),
    completed: index < timeline.length - 1,
    current: index === timeline.length - 1,
    tracking: event.tracking
  }));
}
```

## Email Notifications

Customers receive automated emails at each stage:

1. **Order Confirmation** - Order received
2. **Payment Confirmation** - Payment successful
3. **Shipment Notification** - Order shipped with tracking
4. **Delivery Notification** - Order delivered
5. **Return Approval** - Return/refund approved

## Mobile Optimization

For mobile app integration:

```javascript
// Subscribe to order updates via webhooks
async function subscribeToOrderUpdates(orderId) {
  const notifications = [];

  // Webhook events received:
  // - order.shipped
  // - order.delivered
  // - order.cancelled

  return notifications;
}

// Display push notification
function showPushNotification(event) {
  if ('Notification' in window && Notification.permission === 'granted') {
    new Notification('Order Update', {
      body: `Your order ${event.orderNumber} is ${event.status}`,
      icon: '/assets/logo.png',
      tag: `order-${event.orderId}`
    });
  }
}
```

## Best Practices

1. **Real-time Updates**: Subscribe to webhooks for immediate notifications
2. **Tracking Links**: Provide direct links to carrier tracking
3. **Estimated Delivery**: Show estimated delivery dates
4. **Status Clarity**: Use clear language for each status
5. **Support Access**: Easy access to customer support
6. **Return Management**: Simple return/refund process
7. **Communication**: Keep customer informed of delays

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for general API patterns.
See [TenantUser-Checkout-Payment-Flow.md](TenantUser-Checkout-Payment-Flow.md) for checkout details.
