# KromicStore Webhook Consumer Guide

This guide explains how to consume and validate webhooks from KromicStore, including payload structure, signature verification, and implementation examples.

## Table of Contents

1. [Webhook Registration](#webhook-registration)
2. [Webhook Payload Structure](#webhook-payload-structure)
3. [Signature Verification](#signature-verification)
4. [Event Types](#event-types)
5. [Retry Behavior](#retry-behavior)
6. [Implementation Examples](#implementation-examples)
7. [Best Practices](#best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Webhook Registration

### Register a Webhook

To receive webhooks from KromicStore, register your endpoint via the API:

```http
POST /api/v1/webhooks
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "endpointUrl": "https://your-api.example.com/webhooks",
  "eventTypes": ["OrderCreated", "PaymentProcessed", "OrderStatusChanged"],
  "authenticationHeader": "Bearer your-secret-token",
  "description": "Main order webhook receiver",
  "isActive": true
}
```

**Response:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "tenantId": "550e8400-e29b-41d4-a716-446655440001",
  "endpointUrl": "https://your-api.example.com/webhooks",
  "eventTypes": ["OrderCreated", "PaymentProcessed", "OrderStatusChanged"],
  "secret": "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/",
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**Important:** Store the `secret` value securely. You'll use it to verify webhook signatures. This value is only returned on creation and cannot be retrieved later.

---

## Webhook Payload Structure

Every webhook delivery includes:

### HTTP Headers

```
X-KromicStore-Signature: sha256=<hex-encoded-hmac>
X-KromicStore-Timestamp: 2024-01-15T10:30:45.1234567Z
X-KromicStore-Event: Webhook
User-Agent: KromicStore-Webhook/1.0
```

### Request Body (JSON)

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440002",
  "eventType": "OrderCreated",
  "timestamp": "2024-01-15T10:30:45.1234567Z",
  "tenantId": "550e8400-e29b-41d4-a716-446655440001",
  "idempotencyKey": "order-12345-v1",
  "apiVersion": 1,
  "payload": {
    "orderId": "550e8400-e29b-41d4-a716-446655440003",
    "orderNumber": "ORD-20240115-12345",
    "customerId": "550e8400-e29b-41d4-a716-446655440004",
    "totalAmount": 99.99,
    "currency": "USD",
    "createdAt": "2024-01-15T10:30:45Z"
  }
}
```

### Payload Fields

- **eventId**: Unique identifier for this event
- **eventType**: The type of event triggered (see [Event Types](#event-types))
- **timestamp**: ISO 8601 timestamp when the event occurred
- **tenantId**: Your KromicStore tenant ID
- **idempotencyKey**: Unique key for deduplication; use this to detect duplicate deliveries
- **apiVersion**: Webhook API version (currently 1)
- **payload**: Event-specific data structure

---

## Signature Verification

### Algorithm

KromicStore uses **HMAC-SHA256** for webhook signatures:

1. Compute HMAC-SHA256 of the raw request body using your webhook secret (Base64-decoded)
2. Convert the HMAC hash to hexadecimal
3. Compare with the value in the `X-KromicStore-Signature` header (without "sha256=" prefix)
4. Verify the timestamp is within a 5-minute tolerance window

### Verification Steps

1. **Extract headers:**
   ```
   signature = X-KromicStore-Signature
   timestamp = X-KromicStore-Timestamp
   ```

2. **Validate timestamp:**
   - Parse ISO 8601 timestamp
   - Check if current time ± 5 minutes contains the timestamp
   - Prevents replay attacks from old payloads

3. **Compute expected signature:**
   - Take the raw request body (don't re-serialize/parse)
   - Decode the Base64 secret to bytes
   - Compute HMAC-SHA256(body_bytes, secret_bytes)
   - Convert to hex string (lowercase)

4. **Compare signatures:**
   - Use constant-time string comparison
   - Prevents timing attacks

---

## Event Types

### Supported Events

| Event Type | Triggered When | Payload Structure |
|------------|---|---|
| `OrderCreated` | New order is created | Order details, customer info, items |
| `OrderStatusChanged` | Order status transitions | Order ID, new status, previous status |
| `OrderCancelled` | Order is cancelled | Order ID, reason, cancelled timestamp |
| `PaymentProcessed` | Payment succeeds | Order ID, payment ID, amount, timestamp |
| `PaymentFailed` | Payment fails | Order ID, error reason |
| `TenantCreated` | New tenant is created | Tenant ID, company name |
| `SubscriptionChanged` | Subscription plan changes | Old plan, new plan, effective date |
| `SubscriptionCancelled` | Subscription is cancelled | Plan, cancellation date |
| `ProductPublished` | Product becomes available | Product ID, SKU, name |
| `ProductUnpublished` | Product is hidden | Product ID, reason |
| `CustomerCreated` | New customer is created | Customer ID, email, name |

---

## Retry Behavior

### Automatic Retries

KromicStore automatically retries failed deliveries with exponential backoff:

- **1st attempt**: Immediate
- **2nd attempt**: After 1 second
- **3rd attempt**: After 10 seconds
- **4th attempt**: After 100 seconds
- **5th attempt**: After 1000 seconds
- **6th attempt**: After 10000 seconds
- **Final failure**: Event marked as failed after 6 attempts

### Idempotency

Use the `idempotencyKey` header to implement deduplication:

```csharp
// Example: Track processed keys in your database
var processed = await db.ProcessedWebhookKeys.FindAsync(webhook.IdempotencyKey);
if (processed != null)
{
    return Ok(); // Already processed, return success
}

// Process the webhook...

await db.ProcessedWebhookKeys.AddAsync(new { webhook.IdempotencyKey });
await db.SaveChangesAsync();
```

### Response Codes

- **2xx**: Delivery successful, no retry
- **4xx**: Client error (e.g., 400, 403), may retry
- **5xx**: Server error (e.g., 500, 503), will retry
- **Timeout**: No response after 30 seconds, will retry

---

## Implementation Examples

### C# / .NET

```csharp
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private const int TIMESTAMP_TOLERANCE_MINUTES = 5;

    [HttpPost("kromic-store")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        // Read raw body
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        
        // Extract headers
        var signature = Request.Headers["X-KromicStore-Signature"].ToString();
        var timestamp = Request.Headers["X-KromicStore-Timestamp"].ToString();
        var secret = "your-webhook-secret-here";

        // Verify signature
        if (!VerifySignature(body, signature, timestamp, secret))
        {
            return Unauthorized("Invalid signature or timestamp");
        }

        // Parse payload
        var webhook = JsonSerializer.Deserialize<KromicWebhook>(body);

        // Process webhook...
        await ProcessWebhook(webhook);

        return Ok();
    }

    private bool VerifySignature(string body, string signature, string timestamp, string secret)
    {
        // Validate timestamp
        if (!DateTime.TryParse(timestamp, null, 
            System.Globalization.DateTimeStyles.RoundtripKind, out var ts))
        {
            return false;
        }

        var age = DateTime.UtcNow - ts;
        if (age.TotalMinutes > TIMESTAMP_TOLERANCE_MINUTES || age.TotalSeconds < -1)
        {
            return false;
        }

        // Compute signature
        var secretBytes = Convert.FromBase64String(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using (var hmac = new HMACSHA256(secretBytes))
        {
            var hash = hmac.ComputeHash(bodyBytes);
            var expectedSignature = "sha256=" + BitConverter.ToString(hash)
                .Replace("-", "").ToLowerInvariant();

            // Constant-time comparison
            return ConstantTimeEquals(signature, expectedSignature);
        }
    }

    private bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }
        return diff == 0;
    }

    private async Task ProcessWebhook(KromicWebhook webhook)
    {
        // Check for duplicates using idempotencyKey
        if (await IsProcessed(webhook.IdempotencyKey))
        {
            return;
        }

        // Handle different event types
        switch (webhook.EventType)
        {
            case "OrderCreated":
                await HandleOrderCreated((dynamic)webhook.Payload);
                break;
            case "PaymentProcessed":
                await HandlePaymentProcessed((dynamic)webhook.Payload);
                break;
            // ... handle other events
        }

        // Mark as processed
        await MarkProcessed(webhook.IdempotencyKey);
    }
}

public class KromicWebhook
{
    public Guid EventId { get; set; }
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid TenantId { get; set; }
    public string IdempotencyKey { get; set; }
    public object Payload { get; set; }
    public int ApiVersion { get; set; }
}
```

### Node.js / Express

```javascript
const express = require('express');
const crypto = require('crypto');
const app = express();

const TIMESTAMP_TOLERANCE_MINUTES = 5;

app.post('/webhooks/kromic-store', express.raw({ type: 'application/json' }), (req, res) => {
    const signature = req.headers['x-kromic-store-signature'];
    const timestamp = req.headers['x-kromic-store-timestamp'];
    const secret = 'your-webhook-secret-here';
    const body = req.body.toString('utf8');

    if (!verifySignature(body, signature, timestamp, secret)) {
        return res.status(401).json({ error: 'Invalid signature or timestamp' });
    }

    const webhook = JSON.parse(body);
    processWebhook(webhook);

    res.json({ received: true });
});

function verifySignature(body, signature, timestamp, secret) {
    // Validate timestamp
    const ts = new Date(timestamp);
    const now = new Date();
    const ageMs = Math.abs(now - ts);
    if (ageMs > TIMESTAMP_TOLERANCE_MINUTES * 60 * 1000) {
        return false;
    }

    // Compute signature
    const secretBuffer = Buffer.from(secret, 'base64');
    const hash = crypto
        .createHmac('sha256', secretBuffer)
        .update(body)
        .digest('hex');

    const expectedSignature = 'sha256=' + hash;

    // Constant-time comparison
    return crypto.timingSafeEqual(
        Buffer.from(signature),
        Buffer.from(expectedSignature)
    );
}

async function processWebhook(webhook) {
    // Check for duplicates
    if (await isProcessed(webhook.idempotencyKey)) {
        return;
    }

    // Handle event
    switch (webhook.eventType) {
        case 'OrderCreated':
            await handleOrderCreated(webhook.payload);
            break;
        case 'PaymentProcessed':
            await handlePaymentProcessed(webhook.payload);
            break;
    }

    await markProcessed(webhook.idempotencyKey);
}
```

### Python / Flask

```python
from flask import Flask, request
import hmac
import hashlib
import json
from datetime import datetime, timedelta

app = Flask(__name__)
TIMESTAMP_TOLERANCE_MINUTES = 5

@app.route('/webhooks/kromic-store', methods=['POST'])
def receive_webhook():
    signature = request.headers.get('X-KromicStore-Signature')
    timestamp = request.headers.get('X-KromicStore-Timestamp')
    secret = 'your-webhook-secret-here'
    body = request.get_data(as_text=True)

    if not verify_signature(body, signature, timestamp, secret):
        return {'error': 'Invalid signature or timestamp'}, 401

    webhook = json.loads(body)
    process_webhook(webhook)

    return {'received': True}, 200

def verify_signature(body, signature, timestamp, secret):
    # Validate timestamp
    try:
        ts = datetime.fromisoformat(timestamp.replace('Z', '+00:00'))
        now = datetime.now(ts.tzinfo)
        age = abs((now - ts).total_seconds())
        if age > TIMESTAMP_TOLERANCE_MINUTES * 60:
            return False
    except:
        return False

    # Compute signature
    secret_bytes = secret.encode('utf-8')  # If Base64, decode first
    body_bytes = body.encode('utf-8')
    
    expected_hash = hmac.new(
        secret_bytes,
        body_bytes,
        hashlib.sha256
    ).hexdigest()

    expected_signature = 'sha256=' + expected_hash

    # Constant-time comparison
    return hmac.compare_digest(signature, expected_signature)

def process_webhook(webhook):
    # Check for duplicates
    if is_processed(webhook['idempotencyKey']):
        return

    # Handle event
    event_type = webhook['eventType']
    if event_type == 'OrderCreated':
        handle_order_created(webhook['payload'])
    elif event_type == 'PaymentProcessed':
        handle_payment_processed(webhook['payload'])

    mark_processed(webhook['idempotencyKey'])
```

### cURL Testing

```bash
#!/bin/bash

# Variables
ENDPOINT="https://your-api.example.com/webhooks"
SECRET="your-webhook-secret-here"
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%S.000Z")

# Payload
PAYLOAD='{"eventId":"550e8400-e29b-41d4-a716-446655440002","eventType":"OrderCreated","timestamp":"'"$TIMESTAMP"'","tenantId":"550e8400-e29b-41d4-a716-446655440001","idempotencyKey":"test-1","apiVersion":1,"payload":{"orderId":"123","amount":99.99}}'

# Generate signature
SECRET_BYTES=$(echo -n "$SECRET" | base64 -d)
SIGNATURE=$(echo -n "$PAYLOAD" | openssl dgst -sha256 -mac HMAC -macopt "key=$SECRET_BYTES" -hex | cut -d' ' -f2)

# Send webhook
curl -X POST \
  -H "Content-Type: application/json" \
  -H "X-KromicStore-Signature: sha256=$SIGNATURE" \
  -H "X-KromicStore-Timestamp: $TIMESTAMP" \
  -d "$PAYLOAD" \
  "$ENDPOINT"
```

---

## Best Practices

1. **Verify signatures always** - Never skip signature verification in production
2. **Implement idempotency** - Track `idempotencyKey` to handle duplicate deliveries
3. **Use HTTPS only** - Webhook endpoints must use HTTPS with valid certificates
4. **Fail fast, retry later** - Return 2xx immediately, process asynchronously
5. **Log all webhooks** - Store webhook content for debugging and audit
6. **Monitor delivery** - Track delivery failures and investigate delays
7. **Use custom headers** - Consider requiring a custom authorization header
8. **Validate timestamps** - Prevent replay attacks by checking timestamp window
9. **Handle errors gracefully** - Return appropriate HTTP status codes
10. **Document payload** - Keep documentation of event-specific payload structures

---

## Troubleshooting

### "Invalid signature" errors

- Verify the secret is stored as Base64 and decoded before HMAC computation
- Ensure you're using the raw request body, not re-serialized JSON
- Check that `X-KromicStore-Signature` includes the "sha256=" prefix
- Use constant-time comparison to avoid timing attack vulnerabilities

### "Timestamp too old" errors

- Sync server clocks with NTP
- Check that timestamp is within ±5 minute tolerance
- Verify `X-KromicStore-Timestamp` is in ISO 8601 format with 'Z' timezone indicator

### Missing webhook deliveries

- Check webhook is marked as `isActive: true`
- Verify endpoint returns 2xx status code
- Check HTTP logs for connection errors or timeouts
- Review delivery logs in the KromicStore dashboard

### Duplicate webhook processing

- Implement idempotency by tracking `idempotencyKey`
- Store idempotency key in database before processing
- Always return 200 OK even if already processed

---

For additional help, contact support@example.com or visit https://docs.kromic-store.example.com
