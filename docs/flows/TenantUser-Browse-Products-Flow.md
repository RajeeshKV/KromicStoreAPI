# TenantUser Browse Products Flow

Complete customer browsing workflow for discovering and viewing products.

## Overview

This flow covers how customers browse the product catalog, search, filter, and view product details.

## Complete Flow with Examples

### Step 1: List Published Products

**Endpoint**: `GET /api/v1/products`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/products?status=Published&page=1&pageSize=20" \
  -H "Accept: application/json"
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "product-123",
      "name": "Wireless Headphones",
      "price": 199.99,
      "categoryId": "category-audio",
      "stock": 50,
      "images": [{"url": "https://cdn.example.com/headphones.jpg"}],
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "pagination": {"page": 1, "pageSize": 20, "totalCount": 150}
}
```

### Step 2: Get Categories

**Endpoint**: `GET /api/v1/categories`

```bash
curl -X GET https://api.kromic-store.com/api/v1/categories
```

**Response** (200):
```json
{
  "data": [
    {
      "id": "category-audio",
      "name": "Audio",
      "productCount": 45,
      "subcategories": [
        {"id": "category-headphones", "name": "Headphones", "productCount": 20}
      ]
    }
  ]
}
```

### Step 3: Filter by Category

**Endpoint**: `GET /api/v1/products`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/products?categoryId=category-audio&page=1" \
  -H "Accept: application/json"
```

### Step 4: Search Products

**Endpoint**: `GET /api/v1/products`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/products?search=wireless&status=Published" \
  -H "Accept: application/json"
```

### Step 5: Filter by Price

**Endpoint**: `GET /api/v1/products`

```bash
curl -X GET "https://api.kromic-store.com/api/v1/products?minPrice=100&maxPrice=500&status=Published" \
  -H "Accept: application/json"
```

### Step 6: View Product Details

**Endpoint**: `GET /api/v1/products/{id}`

```bash
curl -X GET https://api.kromic-store.com/api/v1/products/product-123 \
  -H "Accept: application/json"
```

**Response** (200):
```json
{
  "data": {
    "id": "product-123",
    "name": "Premium Wireless Headphones",
    "price": 199.99,
    "description": "High-quality wireless headphones with noise cancellation",
    "categoryId": "category-audio",
    "stock": 50,
    "images": [
      {"url": "https://cdn.example.com/image1.jpg", "alt": "Front view"},
      {"url": "https://cdn.example.com/image2.jpg", "alt": "Side view"}
    ],
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

## Best Practices for Frontend

1. **Pagination**: Always paginate results
2. **Caching**: Cache category and product data
3. **Error Handling**: Gracefully handle connection errors
4. **Mobile Optimization**: Responsive image sizing
5. **Performance**: Load images lazily

---

See [Frontend-Integration-Guide.md](../Frontend-Integration-Guide.md) for general API patterns.
