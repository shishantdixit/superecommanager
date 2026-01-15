# API Design

## Table of Contents
1. [Overview](#overview)
2. [API Conventions](#api-conventions)
3. [Authentication](#authentication)
4. [API Endpoints](#api-endpoints)
5. [Request/Response Formats](#requestresponse-formats)
6. [Error Handling](#error-handling)
7. [Pagination & Filtering](#pagination--filtering)
8. [Rate Limiting](#rate-limiting)

---

## Overview

The API follows RESTful principles and is designed to be:
- **Channel-agnostic** - Same API works for web, mobile, and third-party integrations
- **Consistent** - Uniform response formats and error handling
- **Versioned** - API versioning for backward compatibility
- **Secure** - JWT authentication with feature and permission checks

### Base URL
```
Production: https://api.superecommanager.com/api/v1
Staging:    https://api-staging.superecommanager.com/api/v1
Local:      http://localhost:5000/api/v1
```

---

## API Conventions

### HTTP Methods
| Method | Usage |
|--------|-------|
| GET | Retrieve resources |
| POST | Create resources, trigger actions |
| PUT | Full update of resources |
| PATCH | Partial update of resources |
| DELETE | Remove resources |

### URL Naming
- Use lowercase with hyphens
- Use plural nouns for collections
- Use resource IDs in path

```
GET    /api/v1/orders              # List orders
GET    /api/v1/orders/{id}         # Get single order
POST   /api/v1/orders              # Create order
PATCH  /api/v1/orders/{id}         # Update order
DELETE /api/v1/orders/{id}         # Delete order
POST   /api/v1/orders/{id}/cancel  # Action on order
```

### Headers
```http
# Required
Authorization: Bearer {jwt_token}
X-Tenant-Id: {tenant_id}
Content-Type: application/json

# Optional
X-Request-Id: {uuid}           # For request tracking
X-Api-Version: 2024-01-01      # API version override
Accept-Language: en-IN         # Localization
```

---

## Authentication

### Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "********",
  "tenantSlug": "acme-corp"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresAt": "2024-01-15T12:00:00Z",
    "user": {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "name": "John Doe",
      "tenantId": "123e4567-e89b-12d3-a456-426614174000",
      "tenantName": "Acme Corp",
      "isOwner": false,
      "roles": ["operator"],
      "permissions": ["orders.view", "orders.create", "shipments.view"]
    }
  }
}
```

### Refresh Token
```http
POST /api/v1/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

### JWT Token Structure
```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "email": "user@example.com",
  "name": "John Doe",
  "tenant_id": "123e4567-e89b-12d3-a456-426614174000",
  "tenant_schema": "tenant_acme",
  "is_owner": false,
  "roles": ["operator"],
  "iat": 1705315200,
  "exp": 1705318800,
  "iss": "SuperEcomManager",
  "aud": "SuperEcomManagerClients"
}
```

---

## API Endpoints

### Orders

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/orders` | GET | List orders | orders.view |
| `/orders/{id}` | GET | Get order details | orders.view |
| `/orders` | POST | Create order | orders.create |
| `/orders/{id}` | PATCH | Update order | orders.edit |
| `/orders/{id}/cancel` | POST | Cancel order | orders.cancel |
| `/orders/{id}/status` | PATCH | Update status | orders.edit |
| `/orders/export` | GET | Export orders | orders.export |
| `/orders/stats` | GET | Order statistics | orders.view |
| `/orders/sync` | POST | Trigger sync | orders.edit |

### Shipments

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/shipments` | GET | List shipments | shipments.view |
| `/shipments/{id}` | GET | Get shipment details | shipments.view |
| `/shipments` | POST | Create shipment | shipments.create |
| `/shipments/{id}/cancel` | POST | Cancel shipment | shipments.cancel |
| `/shipments/{id}/tracking` | GET | Get tracking | shipments.track |
| `/shipments/bulk` | POST | Bulk create | shipments.create |
| `/shipments/{id}/label` | GET | Get shipping label | shipments.view |

### NDR

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/ndr` | GET | NDR inbox | ndr.view |
| `/ndr/{id}` | GET | Get NDR details | ndr.view |
| `/ndr/assigned` | GET | My assigned NDRs | ndr.view |
| `/ndr/{id}/assign` | POST | Assign NDR | ndr.assign |
| `/ndr/{id}/action` | POST | Add action | ndr.action |
| `/ndr/{id}/remark` | POST | Add remark | ndr.action |
| `/ndr/{id}/reattempt` | POST | Schedule reattempt | ndr.reattempt |
| `/ndr/{id}/close` | POST | Close NDR | ndr.close |
| `/ndr/analytics` | GET | NDR analytics | analytics.view |
| `/ndr/bulk-assign` | POST | Bulk assign | ndr.assign |

### Inventory

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/products` | GET | List products | inventory.view |
| `/products/{id}` | GET | Get product | inventory.view |
| `/products` | POST | Create product | inventory.adjust |
| `/products/{id}` | PATCH | Update product | inventory.adjust |
| `/products/{id}` | DELETE | Delete product | inventory.adjust |
| `/inventory` | GET | Inventory levels | inventory.view |
| `/inventory/{id}/adjust` | POST | Adjust stock | inventory.adjust |
| `/inventory/sync` | POST | Sync inventory | inventory.sync |
| `/inventory/low-stock` | GET | Low stock alerts | inventory.view |

### Channels

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/channels` | GET | List channels | channels.view |
| `/channels/{id}` | GET | Get channel | channels.view |
| `/channels/types` | GET | Available types | channels.view |
| `/channels` | POST | Connect channel | channels.connect |
| `/channels/{id}` | DELETE | Disconnect | channels.disconnect |
| `/channels/{id}/settings` | PATCH | Update settings | channels.configure |
| `/channels/{id}/sync` | POST | Trigger sync | channels.configure |
| `/channels/{id}/test` | POST | Test connection | channels.view |

### Team & Roles

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/users` | GET | List users | team.view |
| `/users/{id}` | GET | Get user | team.view |
| `/users/invite` | POST | Invite user | team.invite |
| `/users/{id}` | PATCH | Update user | team.edit |
| `/users/{id}` | DELETE | Remove user | team.delete |
| `/users/{id}/roles` | PUT | Assign roles | team.roles |
| `/roles` | GET | List roles | team.view |
| `/roles/{id}` | GET | Get role | team.view |
| `/roles` | POST | Create role | team.roles |
| `/roles/{id}` | PATCH | Update role | team.roles |
| `/roles/{id}` | DELETE | Delete role | team.roles |
| `/permissions` | GET | List permissions | team.view |

### Finance

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/expenses` | GET | List expenses | finance.view |
| `/expenses/{id}` | GET | Get expense | finance.view |
| `/expenses` | POST | Create expense | finance.create |
| `/expenses/{id}` | PATCH | Update expense | finance.create |
| `/expenses/{id}` | DELETE | Delete expense | finance.create |
| `/finance/profit-loss` | GET | P&L report | finance.view |
| `/finance/revenue` | GET | Revenue report | finance.view |
| `/finance/export` | GET | Export data | finance.export |

### Analytics

| Endpoint | Method | Description | Permissions |
|----------|--------|-------------|-------------|
| `/analytics/dashboard` | GET | Dashboard stats | analytics.view |
| `/analytics/orders` | GET | Order analytics | analytics.view |
| `/analytics/channels` | GET | Channel analytics | analytics.view |
| `/analytics/performance` | GET | Performance | analytics.view |

---

## Request/Response Formats

### Standard Response Format

**Success Response:**
```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "requestId": "req_abc123",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

**Paginated Response:**
```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 150,
      "totalPages": 8,
      "hasNextPage": true,
      "hasPreviousPage": false
    }
  }
}
```

**Error Response:**
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred.",
    "details": [
      {
        "field": "email",
        "message": "Email is required"
      },
      {
        "field": "password",
        "message": "Password must be at least 8 characters"
      }
    ]
  },
  "meta": {
    "requestId": "req_abc123",
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Order Request/Response Examples

**Create Order Request:**
```json
POST /api/v1/orders
{
  "channelId": "123e4567-e89b-12d3-a456-426614174000",
  "externalOrderId": "SHP-12345",
  "customer": {
    "name": "John Doe",
    "email": "john@example.com",
    "phone": "+919876543210"
  },
  "shippingAddress": {
    "name": "John Doe",
    "phone": "+919876543210",
    "line1": "123 Main Street",
    "line2": "Apt 4B",
    "city": "Mumbai",
    "state": "Maharashtra",
    "postalCode": "400001",
    "country": "IN"
  },
  "items": [
    {
      "sku": "PROD-001",
      "name": "Product Name",
      "quantity": 2,
      "unitPrice": 999.00
    }
  ],
  "subtotal": 1998.00,
  "discountAmount": 100.00,
  "taxAmount": 359.64,
  "shippingAmount": 50.00,
  "totalAmount": 2307.64,
  "paymentMethod": "cod",
  "orderDate": "2024-01-15T10:00:00Z"
}
```

**Order Response:**
```json
{
  "success": true,
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "orderNumber": "ORD-20240115-ABC123",
    "channelId": "123e4567-e89b-12d3-a456-426614174000",
    "channelName": "Shopify Store",
    "externalOrderId": "SHP-12345",
    "status": "pending",
    "paymentStatus": "cod",
    "fulfillmentStatus": "unfulfilled",
    "customer": {
      "name": "John Doe",
      "email": "john@example.com",
      "phone": "+919876543210"
    },
    "shippingAddress": {
      "name": "John Doe",
      "phone": "+919876543210",
      "line1": "123 Main Street",
      "line2": "Apt 4B",
      "city": "Mumbai",
      "state": "Maharashtra",
      "postalCode": "400001",
      "country": "IN"
    },
    "items": [
      {
        "id": "item-uuid",
        "sku": "PROD-001",
        "name": "Product Name",
        "quantity": 2,
        "unitPrice": 999.00,
        "totalAmount": 1998.00,
        "fulfilledQuantity": 0
      }
    ],
    "subtotal": 1998.00,
    "discountAmount": 100.00,
    "taxAmount": 359.64,
    "shippingAmount": 50.00,
    "totalAmount": 2307.64,
    "paymentMethod": "cod",
    "orderDate": "2024-01-15T10:00:00Z",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z"
  }
}
```

### NDR Action Request Example

```json
POST /api/v1/ndr/{id}/action
{
  "actionType": "call",
  "callStatus": "connected",
  "callDurationSeconds": 120,
  "outcome": "will_accept",
  "notes": "Customer confirmed delivery for tomorrow morning"
}
```

```json
POST /api/v1/ndr/{id}/action
{
  "actionType": "whatsapp",
  "templateCode": "ndr_address_confirm",
  "templateVariables": {
    "customer_name": "John",
    "order_number": "ORD-12345"
  },
  "outcome": "reschedule",
  "notes": "Sent address confirmation request"
}
```

```json
POST /api/v1/ndr/{id}/reattempt
{
  "reattemptDate": "2024-01-17",
  "newPhone": "+919876543211",
  "newAddress": {
    "line1": "456 New Street",
    "city": "Mumbai",
    "state": "Maharashtra",
    "postalCode": "400002"
  },
  "notes": "Customer provided alternate address"
}
```

---

## Error Handling

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `INVALID_REQUEST` | 400 | Malformed request |
| `UNAUTHORIZED` | 401 | Authentication required |
| `INVALID_TOKEN` | 401 | JWT token invalid/expired |
| `ACCESS_DENIED` | 403 | Permission denied |
| `FEATURE_NOT_ENABLED` | 403 | Feature not in plan |
| `NOT_FOUND` | 404 | Resource not found |
| `CONFLICT` | 409 | Resource conflict |
| `RATE_LIMITED` | 429 | Too many requests |
| `INTERNAL_ERROR` | 500 | Server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

### Error Response Examples

**Validation Error:**
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [
      { "field": "email", "message": "Invalid email format" },
      { "field": "phone", "message": "Phone number is required" }
    ]
  }
}
```

**Permission Error:**
```json
{
  "success": false,
  "error": {
    "code": "ACCESS_DENIED",
    "message": "You don't have permission to perform this action.",
    "details": [
      { "required": "orders.cancel", "action": "Cancel order" }
    ]
  }
}
```

**Feature Error:**
```json
{
  "success": false,
  "error": {
    "code": "FEATURE_NOT_ENABLED",
    "message": "The feature 'ndr_management' is not available in your current plan.",
    "details": {
      "feature": "ndr_management",
      "currentPlan": "starter",
      "requiredPlan": "professional"
    }
  }
}
```

---

## Pagination & Filtering

### Query Parameters

```http
GET /api/v1/orders?page=1&pageSize=20&sortBy=orderDate&sortOrder=desc&status=pending&channelId=xxx
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 20 | Items per page (max 100) |
| `sortBy` | string | createdAt | Sort field |
| `sortOrder` | string | desc | asc or desc |
| `search` | string | - | Search term |

### Filter Parameters (Orders)

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | string | Order status |
| `paymentStatus` | string | Payment status |
| `fulfillmentStatus` | string | Fulfillment status |
| `channelId` | uuid | Filter by channel |
| `startDate` | datetime | Order date from |
| `endDate` | datetime | Order date to |
| `customerPhone` | string | Customer phone |

### Filter Parameters (NDR)

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | string | NDR status |
| `priority` | string | NDR priority |
| `reasonCode` | string | NDR reason |
| `assignedTo` | uuid | Assigned user |
| `startDate` | datetime | Created from |
| `endDate` | datetime | Created to |
| `dueBefore` | datetime | Due before date |

---

## Rate Limiting

### Limits

| Tier | Requests/Minute | Requests/Hour |
|------|-----------------|---------------|
| Starter | 60 | 1,000 |
| Professional | 120 | 5,000 |
| Business | 300 | 15,000 |
| Enterprise | Custom | Custom |

### Rate Limit Headers

```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1705315260
```

### Rate Limit Error

```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMITED",
    "message": "Too many requests. Please retry after 60 seconds.",
    "details": {
      "retryAfter": 60,
      "limit": 100,
      "remaining": 0,
      "resetAt": "2024-01-15T10:31:00Z"
    }
  }
}
```

---

## Next Steps

See the following documents for more details:
- [Mobile Readiness](09-mobile-readiness.md)
- [Development Roadmap](10-development-roadmap.md)
