# TenantAdmin Product Management Flow

Complete workflow for managing product catalog including CRUD operations, inventory management, and publishing.

## Overview

This flow covers all product management tasks: creating products, updating details, managing inventory, publishing/unpublishing, and organizing into categories.

## Prerequisites

- TenantAdmin or higher role
- Valid authentication token
- Existing categories (optional but recommended)

## Complete Flow with Examples

### Step 1: List Existing Products

**Endpoint**: `GET /api/v1/products`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/products?page=1&pageSize=20&status=Draft" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "product-123",
      "name": "Product Name",
      "sku": "SKU-001",
      "price": 99.99,
      "stock": 50,
      "status": "Draft",
      "categoryId": "category-123",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 5,
    "totalPages": 1
  }
}
```

### Step 2: Get Product Details

**Endpoint**: `GET /api/v1/products/{id}`

```bash
curl -X GET https://api.kromic-store.com/api/v1/products/product-123 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": {
    "id": "product-123",
    "tenantId": "tenant-abc123",
    "name": "Wireless Headphones",
    "sku": "WH-001",
    "description": "Premium wireless headphones with noise cancellation",
    "price": 199.99,
    "categoryId": "category-electronics",
    "stock": 50,
    "reorderLevel": 10,
    "status": "Draft",
    "images": [
      {
        "id": "image-1",
        "url": "https://cdn.example.com/headphones.jpg",
        "alt": "Wireless headphones product image",
        "displayOrder": 1
      }
    ],
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  }
}
```

### Step 3: Create New Product

**Endpoint**: `POST /api/v1/products`

```bash
curl -X POST https://api.kromic-store.com/api/v1/products \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Wireless Headphones Pro",
    "sku": "WH-PRO-001",
    "description": "Advanced wireless headphones with active noise cancellation and 40-hour battery life",
    "price": 349.99,
    "categoryId": "category-electronics",
    "stock": 100,
    "reorderLevel": 15,
    "images": [
      {
        "url": "https://cdn.example.com/headphones-pro-1.jpg",
        "alt": "Front view",
        "displayOrder": 1
      },
      {
        "url": "https://cdn.example.com/headphones-pro-2.jpg",
        "alt": "Side view",
        "displayOrder": 2
      }
    ]
  }'
```

**Response** (201):
```json
{
  "data": {
    "id": "product-new-001",
    "tenantId": "tenant-abc123",
    "name": "Wireless Headphones Pro",
    "sku": "WH-PRO-001",
    "price": 349.99,
    "categoryId": "category-electronics",
    "stock": 100,
    "status": "Draft",
    "createdAt": "2024-01-15T10:35:00Z"
  }
}
```

**Error Scenarios**:
- `409`: SKU already exists (must be unique per tenant)
- `422`: Missing required fields or invalid values

### Step 4: Update Product Details

**Endpoint**: `PUT /api/v1/products/{id}`

```bash
curl -X PUT https://api.kromic-store.com/api/v1/products/product-new-001 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Premium Wireless Headphones Pro",
    "description": "Advanced wireless headphones with active noise cancellation, 40-hour battery, and premium sound",
    "price": 379.99
  }'
```

**Response** (200):
```json
{
  "data": {
    "id": "product-new-001",
    "name": "Premium Wireless Headphones Pro",
    "price": 379.99,
    "updatedAt": "2024-01-15T10:40:00Z"
  }
}
```

### Step 5: Update Inventory/Stock

**Endpoint**: `PUT /api/v1/products/{id}`

```bash
curl -X PUT https://api.kromic-store.com/api/v1/products/product-new-001 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "stock": 75,
    "reorderLevel": 20
  }'
```

**Response** (200):
```json
{
  "data": {
    "id": "product-new-001",
    "stock": 75,
    "reorderLevel": 20,
    "updatedAt": "2024-01-15T10:42:00Z"
  }
}
```

### Step 6: Move to Different Category

**Endpoint**: `PUT /api/v1/products/{id}`

```bash
curl -X PUT https://api.kromic-store.com/api/v1/products/product-new-001 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "categoryId": "category-audio"
  }'
```

**Response** (200):
```json
{
  "data": {
    "id": "product-new-001",
    "categoryId": "category-audio",
    "updatedAt": "2024-01-15T10:43:00Z"
  }
}
```

### Step 7: Publish Product

**Endpoint**: `POST /api/v1/products/{id}/publish`

```bash
curl -X POST https://api.kromic-store.com/api/v1/products/product-new-001/publish \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Success Response** (200):
```json
{
  "data": {
    "id": "product-new-001",
    "status": "Published",
    "publishedAt": "2024-01-15T10:45:00Z"
  }
}
```

**Error Responses**:
- `409`: Stock must be > 0 to publish
- `409`: Product already published

### Step 8: Search for Products

**Endpoint**: `GET /api/v1/products`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/products?search=headphones&status=Published" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "product-new-001",
      "name": "Premium Wireless Headphones Pro",
      "price": 379.99,
      "status": "Published"
    }
  ]
}
```

### Step 9: Filter by Price Range

**Endpoint**: `GET /api/v1/products`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/products?minPrice=100&maxPrice=500" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "product-new-001",
      "name": "Premium Wireless Headphones Pro",
      "price": 379.99,
      "status": "Published"
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

### Step 10: Unpublish Product

**Endpoint**: `POST /api/v1/products/{id}/unpublish`

```bash
curl -X POST https://api.kromic-store.com/api/v1/products/product-new-001/unpublish \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Success Response** (200):
```json
{
  "data": {
    "id": "product-new-001",
    "status": "Draft",
    "unpublishedAt": "2024-01-15T10:50:00Z"
  }
}
```

### Step 11: Delete Product

**Endpoint**: `DELETE /api/v1/products/{id}`

```bash
curl -X DELETE https://api.kromic-store.com/api/v1/products/product-new-001 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Success Response** (204): No content

**Error Responses**:
- `403`: Cannot delete published product (unpublish first)
- `404`: Product not found

## Bulk Operations

### Update Multiple Products at Once

```bash
curl -X POST https://api.kromic-store.com/api/v1/products/bulk-update \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "productIds": ["product-1", "product-2", "product-3"],
    "updates": {
      "categoryId": "category-new"
    }
  }'
```

### Publish Multiple Products

```bash
curl -X POST https://api.kromic-store.com/api/v1/products/bulk-publish \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "productIds": ["product-1", "product-2", "product-3"]
  }'
```

## Common Workflows

### Complete Product Lifecycle

1. Create product (status: Draft)
2. Add images and details
3. Set inventory level
4. Review and test
5. Publish (status: Published, visible to customers)
6. Monitor sales and reviews
7. Update pricing/stock as needed
8. Unpublish when discontinued
9. Archive or delete

### Seasonal Product Promotion

```bash
# Step 1: Create seasonal products
POST /api/v1/products

# Step 2: Set special promotion price
PUT /api/v1/products/{id}
# price: 99.99 (discounted from 149.99)

# Step 3: Publish for season
POST /api/v1/products/{id}/publish

# Step 4: Monitor inventory
GET /api/v1/products/{id}

# Step 5: After season, unpublish
POST /api/v1/products/{id}/unpublish
```

## Best Practices

1. **Use Descriptive Names**: Include key features in product name
2. **Quality Images**: Upload high-quality product images (JPG/PNG)
3. **Accurate Pricing**: Double-check prices before publishing
4. **Stock Management**: Set reorder levels to avoid stockouts
5. **Inventory Audits**: Regularly verify stock counts match actual inventory
6. **Category Organization**: Keep categories organized for easy browsing
7. **Product Descriptions**: Write compelling, accurate descriptions
8. **Test Before Publishing**: Verify product displays correctly before going live
9. **Monitor Performance**: Track which products sell well
10. **Regular Updates**: Keep product information current

## Performance Optimization

- Limit product image file sizes (< 2MB each recommended)
- Cache frequently viewed products
- Use pagination when listing products
- Filter by category to reduce result sets
- Sort by relevant fields (price, popularity, newest)

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for general API usage patterns.
See [TenantAdmin-Store-Setup-Flow.md](TenantAdmin-Store-Setup-Flow.md) for initial store setup.
