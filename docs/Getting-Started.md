# Getting Started with KromicStore API

## 1. Register your store
```bash
curl -X POST https://api.kromicstore.com/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"companyName":"Acme Store","email":"admin@acme.com","password":"SecurePass123!","country":"US"}'
```

## 2. Login
```bash
curl -X POST https://api.kromicstore.com/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@acme.com","password":"SecurePass123!"}'
# Save accessToken from response
```

## 3. Create a product category
```bash
curl -X POST https://api.kromicstore.com/api/v1/categories \
  -H "Authorization: Bearer {accessToken}" \
  -H "Content-Type: application/json" \
  -d '{"name":"Electronics","description":"Electronic products"}'
```

## 4. Add a product
```bash
curl -X POST https://api.kromicstore.com/api/v1/products \
  -H "Authorization: Bearer {accessToken}" \
  -H "Content-Type: application/json" \
  -d '{"sku":"LAPTOP-001","name":"Dell XPS 13","price":1299.99,"stockQuantity":50,"categoryId":"{categoryId}"}'
```

## 5. Publish the product
```bash
curl -X POST https://api.kromicstore.com/api/v1/products/{productId}/publish \
  -H "Authorization: Bearer {accessToken}"
```

## 6. Create an order
```bash
curl -X POST https://api.kromicstore.com/api/v1/orders \
  -H "Authorization: Bearer {accessToken}" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"{customerId}","items":[{"productId":"{productId}","quantity":1}],"shippingAddress":{"street":"123 Main St","city":"Springfield","state":"IL","postalCode":"62701","country":"US"}}'
```

## 7. Process payment
```bash
curl -X POST https://api.kromicstore.com/api/v1/payments/create \
  -H "Authorization: Bearer {accessToken}" \
  -H "Content-Type: application/json" \
  -d '{"orderId":"{orderId}"}'
# Returns Razorpay order ID for frontend checkout
```

## 8. Setup webhook
```bash
curl -X POST https://api.kromicstore.com/api/v1/webhooks \
  -H "Authorization: Bearer {accessToken}" \
  -H "Content-Type: application/json" \
  -d '{"endpointUrl":"https://yoursite.com/webhook","eventTypes":["OrderCreated","PaymentProcessed"]}'
```
