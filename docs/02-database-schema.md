# Database Schema

## Table of Contents
1. [Overview](#overview)
2. [Schema Strategy](#schema-strategy)
3. [Shared Schema (public)](#shared-schema-public)
4. [Tenant Schema](#tenant-schema)
5. [Entity Relationship Diagrams](#entity-relationship-diagrams)
6. [Indexes and Performance](#indexes-and-performance)
7. [Data Migration Strategy](#data-migration-strategy)

---

## Overview

The database is designed with multi-tenancy in mind, using a **schema-per-tenant** approach within a shared PostgreSQL database.

### Database Engine
- **PostgreSQL 15+**
- Features: JSONB, Full-text search, Row-level security, Schema isolation

### Naming Conventions
- Tables: `snake_case`, plural (e.g., `orders`, `order_items`)
- Columns: `snake_case` (e.g., `created_at`, `order_number`)
- Primary Keys: `id` (UUID)
- Foreign Keys: `{table_singular}_id` (e.g., `order_id`)
- Timestamps: `created_at`, `updated_at`, `deleted_at`

---

## Schema Strategy

```
PostgreSQL Database
│
├── public (Shared Schema)
│   ├── tenants
│   ├── plans
│   ├── features
│   ├── plan_features
│   ├── subscriptions
│   ├── tenant_features
│   ├── super_admins
│   ├── audit_logs
│   ├── channel_types
│   ├── plan_channels
│   └── courier_types
│
├── tenant_acme (Tenant Schema)
│   ├── users
│   ├── roles
│   ├── permissions
│   ├── orders
│   ├── shipments
│   ├── ndr_records
│   └── ... (all tenant tables)
│
├── tenant_globex (Tenant Schema)
│   └── ... (same structure)
│
└── tenant_xxx (Tenant Schema)
    └── ... (same structure)
```

---

## Shared Schema (public)

### tenants
Stores all tenant (organization) information.

```sql
CREATE TABLE public.tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(100) UNIQUE NOT NULL,
    domain VARCHAR(255),
    schema_name VARCHAR(100) UNIQUE NOT NULL,
    logo_url VARCHAR(500),
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    settings JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

-- Settings JSONB structure:
-- {
--   "timezone": "Asia/Kolkata",
--   "currency": "INR",
--   "date_format": "DD/MM/YYYY",
--   "business_name": "Acme Corp",
--   "gstin": "XXXXXXXXX",
--   "support_email": "support@acme.com",
--   "branding": {
--     "primary_color": "#3B82F6",
--     "logo_dark": "url"
--   }
-- }

COMMENT ON TABLE public.tenants IS 'Stores tenant (organization) information';
COMMENT ON COLUMN public.tenants.slug IS 'URL-friendly unique identifier';
COMMENT ON COLUMN public.tenants.schema_name IS 'PostgreSQL schema name for tenant data';
COMMENT ON COLUMN public.tenants.status IS 'active, suspended, cancelled';
```

### plans
Subscription plans offered by the platform.

```sql
CREATE TABLE public.plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    price_monthly DECIMAL(10, 2) NOT NULL,
    price_yearly DECIMAL(10, 2),
    max_users INT,
    max_orders_per_month INT,
    max_channels INT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    sort_order INT NOT NULL DEFAULT 0,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Sample Plans:
-- Starter: 999/month, 2 users, 500 orders, 1 channel
-- Professional: 2499/month, 5 users, 2000 orders, 3 channels
-- Business: 4999/month, 15 users, 10000 orders, 5 channels
-- Enterprise: Custom pricing, unlimited

COMMENT ON TABLE public.plans IS 'Subscription plans available on the platform';
```

### features
Master list of all features in the system.

```sql
CREATE TABLE public.features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    category VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Feature Categories:
-- orders, shipments, ndr, inventory, channels, finance, analytics, integrations

-- Sample Features:
INSERT INTO public.features (name, code, category, description) VALUES
('Order Management', 'orders_management', 'orders', 'Basic order management capabilities'),
('Multi-Channel', 'multi_channel', 'channels', 'Connect multiple sales channels'),
('Inventory Sync', 'inventory_sync', 'inventory', 'Auto-sync inventory across channels'),
('Shipment Management', 'shipment_management', 'shipments', 'Create and track shipments'),
('NDR Management', 'ndr_management', 'ndr', 'NDR inbox and follow-ups'),
('NDR Automation', 'ndr_automation', 'ndr', 'Automated NDR workflows'),
('Finance Reports', 'finance_reports', 'finance', 'P&L and expense tracking'),
('Advanced Analytics', 'advanced_analytics', 'analytics', 'Detailed analytics dashboards'),
('API Access', 'api_access', 'integrations', 'API key generation'),
('Custom Couriers', 'custom_couriers', 'shipments', 'Add custom courier integrations'),
('Bulk Operations', 'bulk_operations', 'orders', 'Bulk order/shipment actions'),
('Export Data', 'export_data', 'analytics', 'Export data to CSV/Excel'),
('White Label', 'white_label', 'branding', 'Custom branding');

COMMENT ON TABLE public.features IS 'Master list of platform features';
```

### plan_features
Maps features to plans (which features each plan includes).

```sql
CREATE TABLE public.plan_features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id UUID NOT NULL REFERENCES public.plans(id) ON DELETE CASCADE,
    feature_id UUID NOT NULL REFERENCES public.features(id) ON DELETE CASCADE,
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    config JSONB DEFAULT '{}',
    UNIQUE(plan_id, feature_id)
);

-- Config JSONB can contain feature-specific limits:
-- { "max_ndr_per_day": 100, "max_export_rows": 1000 }

COMMENT ON TABLE public.plan_features IS 'Features included in each subscription plan';
```

### subscriptions
Tenant subscriptions to plans.

```sql
CREATE TABLE public.subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES public.tenants(id) ON DELETE CASCADE,
    plan_id UUID NOT NULL REFERENCES public.plans(id),
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    trial_ends_at TIMESTAMPTZ,
    current_period_start TIMESTAMPTZ NOT NULL,
    current_period_end TIMESTAMPTZ NOT NULL,
    cancelled_at TIMESTAMPTZ,
    payment_provider VARCHAR(50),
    external_subscription_id VARCHAR(255),
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Status values: trial, active, past_due, cancelled, expired
-- Payment providers: stripe, razorpay, manual

COMMENT ON TABLE public.subscriptions IS 'Tenant subscription records';
COMMENT ON COLUMN public.subscriptions.status IS 'trial, active, past_due, cancelled, expired';
```

### tenant_features
Tenant-specific feature overrides (super admin can enable/disable features per tenant).

```sql
CREATE TABLE public.tenant_features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES public.tenants(id) ON DELETE CASCADE,
    feature_id UUID NOT NULL REFERENCES public.features(id) ON DELETE CASCADE,
    is_enabled BOOLEAN NOT NULL,
    config JSONB DEFAULT '{}',
    override_reason VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(tenant_id, feature_id)
);

COMMENT ON TABLE public.tenant_features IS 'Tenant-level feature overrides';
```

### super_admins
Platform administrators (not tenant users).

```sql
CREATE TABLE public.super_admins (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE public.super_admins IS 'Platform owner/administrator accounts';
```

### audit_logs
Platform-wide audit trail.

```sql
CREATE TABLE public.audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES public.tenants(id),
    user_id UUID,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(255),
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Partition by month for performance
CREATE INDEX idx_audit_logs_tenant_created ON public.audit_logs(tenant_id, created_at);
CREATE INDEX idx_audit_logs_entity ON public.audit_logs(entity_type, entity_id);

COMMENT ON TABLE public.audit_logs IS 'Audit trail for all platform actions';
```

### channel_types
Master list of supported sales channels.

```sql
CREATE TABLE public.channel_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    logo_url VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    config_schema JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Seed data
INSERT INTO public.channel_types (name, code, logo_url, config_schema) VALUES
('Shopify', 'shopify', '/images/channels/shopify.svg', '{
  "type": "object",
  "required": ["shop_domain", "access_token"],
  "properties": {
    "shop_domain": { "type": "string", "description": "Your Shopify store domain" },
    "access_token": { "type": "string", "description": "API access token" },
    "api_version": { "type": "string", "default": "2024-01" }
  }
}'),
('Amazon', 'amazon', '/images/channels/amazon.svg', '{
  "type": "object",
  "required": ["seller_id", "mws_auth_token", "marketplace_id"],
  "properties": {
    "seller_id": { "type": "string" },
    "mws_auth_token": { "type": "string" },
    "marketplace_id": { "type": "string" },
    "region": { "type": "string", "enum": ["IN", "US", "EU"] }
  }
}'),
('Flipkart', 'flipkart', '/images/channels/flipkart.svg', NULL),
('Meesho', 'meesho', '/images/channels/meesho.svg', NULL),
('WooCommerce', 'woocommerce', '/images/channels/woocommerce.svg', NULL);

COMMENT ON TABLE public.channel_types IS 'Supported sales channel types';
```

### plan_channels
Which channels each plan can connect.

```sql
CREATE TABLE public.plan_channels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id UUID NOT NULL REFERENCES public.plans(id) ON DELETE CASCADE,
    channel_type_id UUID NOT NULL REFERENCES public.channel_types(id) ON DELETE CASCADE,
    max_connections INT DEFAULT 1,
    UNIQUE(plan_id, channel_type_id)
);

COMMENT ON TABLE public.plan_channels IS 'Channel access per subscription plan';
```

### courier_types
Master list of supported courier services.

```sql
CREATE TABLE public.courier_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) UNIQUE NOT NULL,
    logo_url VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_default BOOLEAN NOT NULL DEFAULT false,
    config_schema JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Seed data
INSERT INTO public.courier_types (name, code, is_default, config_schema) VALUES
('Shiprocket', 'shiprocket', true, '{
  "type": "object",
  "required": ["email", "password"],
  "properties": {
    "email": { "type": "string" },
    "password": { "type": "string" }
  }
}'),
('Delhivery', 'delhivery', false, NULL),
('BlueDart', 'bluedart', false, NULL),
('DTDC', 'dtdc', false, NULL),
('Custom', 'custom', false, NULL);

COMMENT ON TABLE public.courier_types IS 'Supported courier/shipping providers';
```

---

## Tenant Schema

Each tenant gets their own schema with the following tables. Schema name format: `tenant_{slug}` or `tenant_{uuid_short}`.

### users
Tenant users (employees, owners).

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255),
    name VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    avatar_url VARCHAR(500),
    is_owner BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    email_verified_at TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ,
    settings JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE(email)
);

-- Settings JSONB:
-- {
--   "notification_preferences": { "email": true, "sms": false },
--   "dashboard_layout": "compact",
--   "theme": "light"
-- }

COMMENT ON TABLE users IS 'Tenant user accounts';
COMMENT ON COLUMN users.is_owner IS 'Tenant owner has all permissions';
```

### roles
Custom roles for RBAC.

```sql
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(100) NOT NULL,
    description TEXT,
    is_system BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(code)
);

-- System roles (cannot be deleted)
INSERT INTO roles (name, code, is_system, description) VALUES
('Owner', 'owner', true, 'Full access to all features'),
('Admin', 'admin', true, 'Administrative access'),
('Manager', 'manager', true, 'Team and operations management'),
('Operator', 'operator', true, 'Day-to-day operations'),
('Viewer', 'viewer', true, 'Read-only access');

COMMENT ON TABLE roles IS 'User roles for access control';
```

### permissions
Available permissions in the system.

```sql
CREATE TABLE permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(100) NOT NULL,
    module VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(code)
);

-- Seed permissions
INSERT INTO permissions (name, code, module, description) VALUES
-- Orders
('View Orders', 'orders.view', 'orders', 'View order list and details'),
('Create Orders', 'orders.create', 'orders', 'Create manual orders'),
('Edit Orders', 'orders.edit', 'orders', 'Edit order details'),
('Cancel Orders', 'orders.cancel', 'orders', 'Cancel orders'),
('Export Orders', 'orders.export', 'orders', 'Export orders to CSV/Excel'),

-- Shipments
('View Shipments', 'shipments.view', 'shipments', 'View shipment list and details'),
('Create Shipments', 'shipments.create', 'shipments', 'Create new shipments'),
('Cancel Shipments', 'shipments.cancel', 'shipments', 'Cancel shipments'),
('Track Shipments', 'shipments.track', 'shipments', 'View tracking information'),

-- NDR
('View NDR', 'ndr.view', 'ndr', 'View NDR inbox'),
('Assign NDR', 'ndr.assign', 'ndr', 'Assign NDR to team members'),
('Action NDR', 'ndr.action', 'ndr', 'Perform NDR actions (call, message)'),
('Reattempt NDR', 'ndr.reattempt', 'ndr', 'Schedule reattempts'),
('Close NDR', 'ndr.close', 'ndr', 'Close/resolve NDR cases'),

-- Inventory
('View Inventory', 'inventory.view', 'inventory', 'View inventory levels'),
('Adjust Inventory', 'inventory.adjust', 'inventory', 'Adjust stock levels'),
('Sync Inventory', 'inventory.sync', 'inventory', 'Trigger inventory sync'),

-- Channels
('View Channels', 'channels.view', 'channels', 'View connected channels'),
('Connect Channels', 'channels.connect', 'channels', 'Connect new channels'),
('Disconnect Channels', 'channels.disconnect', 'channels', 'Disconnect channels'),
('Configure Channels', 'channels.configure', 'channels', 'Configure channel settings'),

-- Team
('View Team', 'team.view', 'team', 'View team members'),
('Invite Team', 'team.invite', 'team', 'Invite new team members'),
('Edit Team', 'team.edit', 'team', 'Edit team member details'),
('Delete Team', 'team.delete', 'team', 'Remove team members'),
('Manage Roles', 'team.roles', 'team', 'Create and edit roles'),

-- Finance
('View Finance', 'finance.view', 'finance', 'View financial reports'),
('Create Expenses', 'finance.create', 'finance', 'Record expenses'),
('Export Finance', 'finance.export', 'finance', 'Export financial data'),

-- Settings
('View Settings', 'settings.view', 'settings', 'View settings'),
('Edit Settings', 'settings.edit', 'settings', 'Modify settings'),

-- Analytics
('View Analytics', 'analytics.view', 'analytics', 'View analytics dashboards'),
('Export Analytics', 'analytics.export', 'analytics', 'Export analytics data');

COMMENT ON TABLE permissions IS 'Available system permissions';
```

### role_permissions
Maps permissions to roles.

```sql
CREATE TABLE role_permissions (
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

COMMENT ON TABLE role_permissions IS 'Role to permission mapping';
```

### user_roles
Maps users to roles.

```sql
CREATE TABLE user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    assigned_by UUID REFERENCES users(id),
    PRIMARY KEY (user_id, role_id)
);

COMMENT ON TABLE user_roles IS 'User to role assignments';
```

### user_features
User-level feature overrides (tenant owner can restrict features per user).

```sql
CREATE TABLE user_features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    feature_code VARCHAR(100) NOT NULL,
    is_enabled BOOLEAN NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, feature_code)
);

COMMENT ON TABLE user_features IS 'User-level feature access overrides';
```

### sales_channels
Tenant's connected sales channels.

```sql
CREATE TABLE sales_channels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel_type_code VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    credentials JSONB NOT NULL,
    settings JSONB DEFAULT '{}',
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    last_sync_at TIMESTAMPTZ,
    sync_error TEXT,
    webhook_url VARCHAR(500),
    webhook_secret VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Credentials are encrypted at application level
-- Status: active, disconnected, error, syncing

COMMENT ON TABLE sales_channels IS 'Connected sales channels for the tenant';
```

### courier_configs
Tenant's courier configurations.

```sql
CREATE TABLE courier_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    courier_type_code VARCHAR(50) NOT NULL,
    name VARCHAR(255) NOT NULL,
    credentials JSONB NOT NULL,
    settings JSONB DEFAULT '{}',
    is_default BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE courier_configs IS 'Configured courier/shipping providers';
```

### products
Product catalog.

```sql
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sku VARCHAR(100) NOT NULL,
    name VARCHAR(500) NOT NULL,
    description TEXT,
    category VARCHAR(255),
    brand VARCHAR(255),
    weight_grams INT,
    dimensions JSONB,
    hsn_code VARCHAR(50),
    cost_price DECIMAL(12, 2),
    selling_price DECIMAL(12, 2),
    mrp DECIMAL(12, 2),
    tax_rate DECIMAL(5, 2),
    images JSONB DEFAULT '[]',
    attributes JSONB DEFAULT '{}',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE(sku)
);

-- Dimensions JSONB: { "length": 10, "width": 5, "height": 3, "unit": "cm" }
-- Images JSONB: ["url1", "url2", ...]
-- Attributes JSONB: { "color": "Red", "material": "Cotton" }

COMMENT ON TABLE products IS 'Product catalog';
```

### product_variants
Product variations (size, color, etc.).

```sql
CREATE TABLE product_variants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    sku VARCHAR(100) NOT NULL,
    name VARCHAR(500) NOT NULL,
    attributes JSONB NOT NULL,
    cost_price DECIMAL(12, 2),
    selling_price DECIMAL(12, 2),
    mrp DECIMAL(12, 2),
    weight_grams INT,
    images JSONB DEFAULT '[]',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(sku)
);

-- Attributes JSONB: { "color": "Red", "size": "M" }

COMMENT ON TABLE product_variants IS 'Product variants (size, color, etc.)';
```

### channel_products
Maps internal products to external channel products.

```sql
CREATE TABLE channel_products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    variant_id UUID REFERENCES product_variants(id) ON DELETE CASCADE,
    channel_id UUID NOT NULL REFERENCES sales_channels(id) ON DELETE CASCADE,
    external_product_id VARCHAR(255) NOT NULL,
    external_variant_id VARCHAR(255),
    external_sku VARCHAR(255),
    sync_status VARCHAR(50) NOT NULL DEFAULT 'synced',
    last_synced_at TIMESTAMPTZ,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(channel_id, external_product_id, external_variant_id)
);

COMMENT ON TABLE channel_products IS 'Product mapping to external channels';
```

### inventory
Inventory levels.

```sql
CREATE TABLE inventory (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    variant_id UUID REFERENCES product_variants(id) ON DELETE CASCADE,
    warehouse_location VARCHAR(255),
    available_quantity INT NOT NULL DEFAULT 0,
    reserved_quantity INT NOT NULL DEFAULT 0,
    incoming_quantity INT NOT NULL DEFAULT 0,
    reorder_point INT,
    reorder_quantity INT,
    last_counted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(product_id, variant_id, warehouse_location)
);

COMMENT ON TABLE inventory IS 'Inventory levels per product/variant/location';
```

### stock_movements
Stock movement history.

```sql
CREATE TABLE stock_movements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_id UUID NOT NULL REFERENCES inventory(id) ON DELETE CASCADE,
    movement_type VARCHAR(50) NOT NULL,
    quantity INT NOT NULL,
    reference_type VARCHAR(50),
    reference_id UUID,
    notes TEXT,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Movement types: sale, return, adjustment, transfer, purchase, sync
-- Reference types: order, purchase_order, adjustment, return_order

COMMENT ON TABLE stock_movements IS 'Stock movement audit trail';
```

### orders
Unified order model (orders from all channels).

```sql
CREATE TABLE orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number VARCHAR(100) NOT NULL,
    channel_id UUID NOT NULL REFERENCES sales_channels(id),
    external_order_id VARCHAR(255) NOT NULL,
    external_order_number VARCHAR(255),

    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    payment_status VARCHAR(50) NOT NULL DEFAULT 'pending',
    fulfillment_status VARCHAR(50) NOT NULL DEFAULT 'unfulfilled',

    -- Customer
    customer_name VARCHAR(255) NOT NULL,
    customer_email VARCHAR(255),
    customer_phone VARCHAR(20),

    -- Addresses (JSONB for flexibility)
    shipping_address JSONB NOT NULL,
    billing_address JSONB,

    -- Financial
    subtotal DECIMAL(12, 2) NOT NULL,
    discount_amount DECIMAL(12, 2) DEFAULT 0,
    tax_amount DECIMAL(12, 2) DEFAULT 0,
    shipping_amount DECIMAL(12, 2) DEFAULT 0,
    total_amount DECIMAL(12, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'INR',

    -- Payment
    payment_method VARCHAR(50),
    payment_gateway VARCHAR(50),
    payment_reference VARCHAR(255),

    -- Timestamps
    order_date TIMESTAMPTZ NOT NULL,
    confirmed_at TIMESTAMPTZ,
    shipped_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,

    -- Additional
    notes TEXT,
    tags JSONB DEFAULT '[]',
    metadata JSONB DEFAULT '{}',

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(channel_id, external_order_id)
);

-- Status: pending, confirmed, processing, shipped, delivered, cancelled, returned
-- Payment status: pending, paid, cod, refunded, failed
-- Fulfillment status: unfulfilled, partial, fulfilled
-- Payment method: cod, prepaid, card, upi, wallet, netbanking

-- Address JSONB structure:
-- {
--   "name": "John Doe",
--   "phone": "9876543210",
--   "line1": "123 Main St",
--   "line2": "Apt 4B",
--   "city": "Mumbai",
--   "state": "Maharashtra",
--   "postal_code": "400001",
--   "country": "IN"
-- }

CREATE INDEX idx_orders_channel ON orders(channel_id);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_orders_order_date ON orders(order_date DESC);
CREATE INDEX idx_orders_customer_phone ON orders(customer_phone);
CREATE INDEX idx_orders_external ON orders(channel_id, external_order_id);

COMMENT ON TABLE orders IS 'Unified orders from all sales channels';
```

### order_items
Line items within orders.

```sql
CREATE TABLE order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id),
    variant_id UUID REFERENCES product_variants(id),
    external_product_id VARCHAR(255),
    external_variant_id VARCHAR(255),
    sku VARCHAR(100),
    name VARCHAR(500) NOT NULL,
    quantity INT NOT NULL,
    unit_price DECIMAL(12, 2) NOT NULL,
    discount_amount DECIMAL(12, 2) DEFAULT 0,
    tax_amount DECIMAL(12, 2) DEFAULT 0,
    total_amount DECIMAL(12, 2) NOT NULL,
    fulfilled_quantity INT NOT NULL DEFAULT 0,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_order_items_order ON order_items(order_id);

COMMENT ON TABLE order_items IS 'Order line items';
```

### order_status_history
Order status change history.

```sql
CREATE TABLE order_status_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    status VARCHAR(50) NOT NULL,
    notes TEXT,
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_order_history_order ON order_status_history(order_id);

COMMENT ON TABLE order_status_history IS 'Order status change audit trail';
```

### shipments
Shipment records.

```sql
CREATE TABLE shipments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_number VARCHAR(100) NOT NULL,
    order_id UUID NOT NULL REFERENCES orders(id),
    courier_config_id UUID NOT NULL REFERENCES courier_configs(id),

    -- AWB & Tracking
    awb_number VARCHAR(100),
    tracking_url VARCHAR(500),
    label_url VARCHAR(500),
    manifest_url VARCHAR(500),

    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'created',

    -- Package
    weight_grams INT,
    dimensions JSONB,
    package_count INT DEFAULT 1,

    -- Addresses
    pickup_address JSONB,
    delivery_address JSONB NOT NULL,

    -- Dates
    estimated_delivery_date DATE,
    pickup_scheduled_date DATE,
    picked_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    rto_initiated_at TIMESTAMPTZ,
    rto_delivered_at TIMESTAMPTZ,

    -- Financial
    shipping_charges DECIMAL(12, 2),
    cod_amount DECIMAL(12, 2),

    -- External
    external_shipment_id VARCHAR(255),
    external_order_id VARCHAR(255),

    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(awb_number)
);

-- Status: created, manifested, picked, in_transit, out_for_delivery,
--         delivered, rto_initiated, rto_in_transit, rto_delivered, cancelled

CREATE INDEX idx_shipments_order ON shipments(order_id);
CREATE INDEX idx_shipments_awb ON shipments(awb_number);
CREATE INDEX idx_shipments_status ON shipments(status);

COMMENT ON TABLE shipments IS 'Shipment records';
```

### shipment_items
Items included in a shipment.

```sql
CREATE TABLE shipment_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id UUID NOT NULL REFERENCES shipments(id) ON DELETE CASCADE,
    order_item_id UUID NOT NULL REFERENCES order_items(id),
    quantity INT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE shipment_items IS 'Items in a shipment';
```

### shipment_tracking
Shipment tracking events.

```sql
CREATE TABLE shipment_tracking (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    shipment_id UUID NOT NULL REFERENCES shipments(id) ON DELETE CASCADE,
    status VARCHAR(100) NOT NULL,
    status_code VARCHAR(50),
    location VARCHAR(255),
    description TEXT,
    event_time TIMESTAMPTZ NOT NULL,
    raw_data JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tracking_shipment ON shipment_tracking(shipment_id);
CREATE INDEX idx_tracking_event_time ON shipment_tracking(event_time);

COMMENT ON TABLE shipment_tracking IS 'Shipment tracking events from courier';
```

### ndr_records
Non-Delivery Report records.

```sql
CREATE TABLE ndr_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ndr_number VARCHAR(100) NOT NULL,
    shipment_id UUID NOT NULL REFERENCES shipments(id),
    order_id UUID NOT NULL REFERENCES orders(id),

    -- NDR Details
    reason_code VARCHAR(50) NOT NULL,
    reason_description TEXT,
    ndr_date TIMESTAMPTZ NOT NULL,
    attempt_count INT NOT NULL DEFAULT 1,

    -- Status
    status VARCHAR(50) NOT NULL DEFAULT 'open',
    resolution VARCHAR(50),

    -- Assignment
    assigned_to UUID REFERENCES users(id),
    assigned_at TIMESTAMPTZ,

    -- Customer (cached)
    customer_name VARCHAR(255),
    customer_phone VARCHAR(20),
    customer_address JSONB,

    -- Priority
    priority VARCHAR(20) NOT NULL DEFAULT 'medium',

    -- Dates
    due_date DATE,
    resolved_at TIMESTAMPTZ,

    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(ndr_number)
);

-- Status: open, in_progress, reattempt_scheduled, resolved, rto
-- Resolution: delivered, rto, cancelled
-- Priority: low, medium, high, urgent
-- Reason codes: customer_unavailable, wrong_address, refused,
--               incomplete_address, cod_not_ready, etc.

CREATE INDEX idx_ndr_shipment ON ndr_records(shipment_id);
CREATE INDEX idx_ndr_status ON ndr_records(status);
CREATE INDEX idx_ndr_assigned ON ndr_records(assigned_to);
CREATE INDEX idx_ndr_due_date ON ndr_records(due_date);
CREATE INDEX idx_ndr_priority ON ndr_records(priority, status);

COMMENT ON TABLE ndr_records IS 'Non-Delivery Report cases';
```

### ndr_actions
Actions taken on NDR cases.

```sql
CREATE TABLE ndr_actions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ndr_id UUID NOT NULL REFERENCES ndr_records(id) ON DELETE CASCADE,
    action_type VARCHAR(50) NOT NULL,

    -- Call details
    call_status VARCHAR(50),
    call_duration_seconds INT,

    -- Message details
    message_content TEXT,
    message_status VARCHAR(50),

    -- Outcome
    outcome VARCHAR(100),
    outcome_notes TEXT,

    -- Reattempt details
    reattempt_date DATE,
    new_address JSONB,
    new_phone VARCHAR(20),

    performed_by UUID NOT NULL REFERENCES users(id),
    performed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    metadata JSONB DEFAULT '{}'
);

-- Action types: call, whatsapp, sms, email, reattempt_request
-- Call status: connected, not_answered, busy, switched_off, invalid_number
-- Message status: sent, delivered, read, failed
-- Outcomes: will_accept, wrong_address, reschedule, refuse, not_reachable

CREATE INDEX idx_ndr_actions_ndr ON ndr_actions(ndr_id);
CREATE INDEX idx_ndr_actions_performer ON ndr_actions(performed_by);
CREATE INDEX idx_ndr_actions_type ON ndr_actions(action_type);

COMMENT ON TABLE ndr_actions IS 'Actions taken on NDR cases';
```

### ndr_remarks
Remarks/notes on NDR cases.

```sql
CREATE TABLE ndr_remarks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ndr_id UUID NOT NULL REFERENCES ndr_records(id) ON DELETE CASCADE,
    remark TEXT NOT NULL,
    is_internal BOOLEAN NOT NULL DEFAULT true,
    created_by UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_ndr_remarks_ndr ON ndr_remarks(ndr_id);

COMMENT ON TABLE ndr_remarks IS 'Remarks/notes on NDR cases';
```

### expenses
Expense tracking.

```sql
CREATE TABLE expenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category VARCHAR(100) NOT NULL,
    subcategory VARCHAR(100),
    description TEXT,
    amount DECIMAL(12, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'INR',
    expense_date DATE NOT NULL,
    channel_id UUID REFERENCES sales_channels(id),
    order_id UUID REFERENCES orders(id),
    shipment_id UUID REFERENCES shipments(id),
    receipt_url VARCHAR(500),
    tags JSONB DEFAULT '[]',
    metadata JSONB DEFAULT '{}',
    created_by UUID REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Categories: shipping, platform_fee, packaging, returns, marketing,
--             operations, software, other

CREATE INDEX idx_expenses_date ON expenses(expense_date);
CREATE INDEX idx_expenses_channel ON expenses(channel_id);
CREATE INDEX idx_expenses_category ON expenses(category);

COMMENT ON TABLE expenses IS 'Business expenses';
```

### revenue_records
Revenue records for P&L calculation.

```sql
CREATE TABLE revenue_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES orders(id),
    channel_id UUID NOT NULL REFERENCES sales_channels(id),
    gross_revenue DECIMAL(12, 2) NOT NULL,
    platform_fee DECIMAL(12, 2) DEFAULT 0,
    payment_gateway_fee DECIMAL(12, 2) DEFAULT 0,
    shipping_cost DECIMAL(12, 2) DEFAULT 0,
    product_cost DECIMAL(12, 2) DEFAULT 0,
    other_costs DECIMAL(12, 2) DEFAULT 0,
    net_revenue DECIMAL(12, 2) NOT NULL,
    revenue_date DATE NOT NULL,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_revenue_order ON revenue_records(order_id);
CREATE INDEX idx_revenue_date ON revenue_records(revenue_date);
CREATE INDEX idx_revenue_channel ON revenue_records(channel_id);

COMMENT ON TABLE revenue_records IS 'Revenue records for P&L';
```

### notification_templates
Notification message templates.

```sql
CREATE TABLE notification_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    code VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL,
    subject VARCHAR(500),
    body TEXT NOT NULL,
    variables JSONB DEFAULT '[]',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(code, type)
);

-- Type: email, sms, whatsapp
-- Variables: ["customer_name", "order_number", "tracking_url"]

COMMENT ON TABLE notification_templates IS 'Notification message templates';
```

### notification_logs
Notification delivery logs.

```sql
CREATE TABLE notification_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID REFERENCES notification_templates(id),
    type VARCHAR(50) NOT NULL,
    recipient VARCHAR(255) NOT NULL,
    subject VARCHAR(500),
    body TEXT NOT NULL,
    status VARCHAR(50) NOT NULL,
    error_message TEXT,
    reference_type VARCHAR(50),
    reference_id UUID,
    sent_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Status: pending, sent, delivered, failed
-- Reference type: order, shipment, ndr

CREATE INDEX idx_notification_logs_status ON notification_logs(status);
CREATE INDEX idx_notification_logs_reference ON notification_logs(reference_type, reference_id);

COMMENT ON TABLE notification_logs IS 'Notification delivery logs';
```

### refresh_tokens
JWT refresh tokens.

```sql
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(500) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMPTZ,
    UNIQUE(token)
);

CREATE INDEX idx_refresh_tokens_user ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_expires ON refresh_tokens(expires_at);

COMMENT ON TABLE refresh_tokens IS 'JWT refresh tokens';
```

---

## Entity Relationship Diagrams

### Core Relationships

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│   tenants    │──────<│subscriptions │>──────│    plans     │
└──────────────┘       └──────────────┘       └──────────────┘
                                                     │
                                                     │
                                              ┌──────┴──────┐
                                              │plan_features│
                                              └──────┬──────┘
                                                     │
                                              ┌──────┴──────┐
                                              │  features   │
                                              └─────────────┘
```

### Order to NDR Flow

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│    orders    │──────<│  shipments   │──────<│ ndr_records  │
└──────────────┘       └──────────────┘       └──────────────┘
       │                      │                      │
       │                      │                      │
┌──────┴──────┐        ┌──────┴──────┐        ┌──────┴──────┐
│ order_items │        │  shipment   │        │ ndr_actions │
└─────────────┘        │  _tracking  │        └─────────────┘
                       └─────────────┘               │
                                               ┌─────┴──────┐
                                               │ndr_remarks │
                                               └────────────┘
```

### User & Access Control

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│    users     │──────<│  user_roles  │>──────│    roles     │
└──────────────┘       └──────────────┘       └──────────────┘
                                                     │
                                                     │
                                              ┌──────┴──────┐
                                              │    role     │
                                              │ permissions │
                                              └──────┬──────┘
                                                     │
                                              ┌──────┴──────┐
                                              │ permissions │
                                              └─────────────┘
```

---

## Indexes and Performance

### Key Indexes Summary

| Table | Index | Columns | Purpose |
|-------|-------|---------|---------|
| orders | idx_orders_channel | channel_id | Filter by channel |
| orders | idx_orders_status | status | Filter by status |
| orders | idx_orders_order_date | order_date DESC | Sort by date |
| orders | idx_orders_customer_phone | customer_phone | Customer lookup |
| shipments | idx_shipments_awb | awb_number | AWB lookup |
| shipments | idx_shipments_status | status | Filter by status |
| ndr_records | idx_ndr_status | status | Filter by status |
| ndr_records | idx_ndr_assigned | assigned_to | User assignment |
| ndr_records | idx_ndr_due_date | due_date | Due date filtering |
| audit_logs | idx_audit_logs_tenant_created | tenant_id, created_at | Tenant audit trail |

### Query Optimization Tips

1. **Always filter by tenant context** - Schema isolation handles this
2. **Use covering indexes** for frequent queries
3. **Partition large tables** (audit_logs, notification_logs) by date
4. **Use JSONB indexes** for frequently queried JSON fields:
   ```sql
   CREATE INDEX idx_orders_shipping_city
   ON orders ((shipping_address->>'city'));
   ```

---

## Security Configuration Tables (Tenant Schema)

### security_settings
Tenant-level security configuration.

```sql
CREATE TABLE security_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Copy Protection Settings
    copy_protection_enabled BOOLEAN NOT NULL DEFAULT true,
    disable_text_selection BOOLEAN NOT NULL DEFAULT false,
    disable_right_click BOOLEAN NOT NULL DEFAULT false,
    block_keyboard_shortcuts BOOLEAN NOT NULL DEFAULT false,
    blur_on_print_screen BOOLEAN NOT NULL DEFAULT false,

    -- Export Settings
    export_enabled BOOLEAN NOT NULL DEFAULT true,
    require_watermark BOOLEAN NOT NULL DEFAULT true,
    max_export_rows INT NOT NULL DEFAULT 10000,
    max_exports_per_day INT NOT NULL DEFAULT 10,
    require_approval_threshold INT DEFAULT 1000,
    export_link_expiry_hours INT NOT NULL DEFAULT 24,

    -- Data Masking Settings
    data_masking_enabled BOOLEAN NOT NULL DEFAULT true,
    mask_phone_numbers BOOLEAN NOT NULL DEFAULT true,
    mask_email_addresses BOOLEAN NOT NULL DEFAULT true,
    mask_addresses BOOLEAN NOT NULL DEFAULT false,
    mask_financial_data BOOLEAN NOT NULL DEFAULT false,

    -- Session Settings
    session_timeout_minutes INT NOT NULL DEFAULT 30,
    max_concurrent_sessions INT NOT NULL DEFAULT 3,
    force_logout_on_role_change BOOLEAN NOT NULL DEFAULT true,
    ip_binding_enabled BOOLEAN NOT NULL DEFAULT false,
    mfa_required BOOLEAN NOT NULL DEFAULT false,

    -- Rate Limiting
    requests_per_minute INT NOT NULL DEFAULT 100,
    export_requests_per_hour INT NOT NULL DEFAULT 10,
    alert_on_limit_violations BOOLEAN NOT NULL DEFAULT true,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE security_settings IS 'Tenant-level security configuration';
```

### role_security_settings
Per-role security overrides.

```sql
CREATE TABLE role_security_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,

    -- Copy Protection (per role)
    disable_text_selection BOOLEAN,
    disable_right_click BOOLEAN,
    block_keyboard_shortcuts BOOLEAN,

    -- Data Access (per role)
    view_full_data BOOLEAN NOT NULL DEFAULT false,
    can_copy_data BOOLEAN NOT NULL DEFAULT false,
    can_print BOOLEAN NOT NULL DEFAULT true,

    -- Export Permissions (per role) - JSON array of allowed export types
    allowed_exports JSONB DEFAULT '[]',
    -- Example: ["orders_csv", "orders_excel", "ndr", "inventory"]

    -- Rate Limiting Overrides
    custom_requests_per_minute INT,
    custom_export_limit INT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(role_id)
);

-- Allowed exports values:
-- orders_csv, orders_excel, customers, financial, ndr, inventory, analytics, bulk_api

COMMENT ON TABLE role_security_settings IS 'Role-specific security configuration overrides';
```

### data_masking_rules
Custom data masking rules.

```sql
CREATE TABLE data_masking_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    field_name VARCHAR(100) NOT NULL,
    field_type VARCHAR(50) NOT NULL,
    mask_pattern VARCHAR(100) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    applies_to_roles JSONB DEFAULT '[]',
    exempt_roles JSONB DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(field_name)
);

-- Seed default masking rules
INSERT INTO data_masking_rules (field_name, field_type, mask_pattern, description, applies_to_roles) VALUES
('customer_phone', 'phone', '##****####', 'Mask middle 4 digits of phone', '["operator", "viewer", "ndr_agent"]'),
('customer_email', 'email', '##**@***.***', 'Mask email keeping first 2 chars', '["operator", "viewer"]'),
('full_address', 'address', '******', 'Mask address line 2 and beyond', '["viewer"]'),
('bank_account', 'sensitive', '************####', 'Show only last 4 digits', '["operator", "viewer", "manager", "ndr_agent"]'),
('aadhaar_pan', 'sensitive', '*****#####', 'Mask first 5 characters', '["operator", "viewer", "manager", "ndr_agent"]');

COMMENT ON TABLE data_masking_rules IS 'Custom data masking patterns per field';
```

### export_logs
Detailed export audit trail.

```sql
CREATE TABLE export_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    export_type VARCHAR(50) NOT NULL,
    file_format VARCHAR(20) NOT NULL,
    row_count INT NOT NULL,
    file_size_bytes BIGINT,
    filters_applied JSONB DEFAULT '{}',
    columns_exported JSONB DEFAULT '[]',
    download_url VARCHAR(500),
    download_expires_at TIMESTAMPTZ,
    download_count INT NOT NULL DEFAULT 0,
    watermark_text TEXT,
    ip_address INET,
    user_agent TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'completed',
    approval_required BOOLEAN NOT NULL DEFAULT false,
    approved_by UUID REFERENCES users(id),
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Status: pending_approval, approved, rejected, completed, expired, downloaded

CREATE INDEX idx_export_logs_user ON export_logs(user_id);
CREATE INDEX idx_export_logs_created ON export_logs(created_at);
CREATE INDEX idx_export_logs_status ON export_logs(status);

COMMENT ON TABLE export_logs IS 'Audit trail for all data exports';
```

### user_sessions
Active user sessions for session management.

```sql
CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    session_token VARCHAR(500) NOT NULL,
    ip_address INET NOT NULL,
    user_agent TEXT,
    device_type VARCHAR(50),
    device_name VARCHAR(255),
    location VARCHAR(255),
    is_trusted BOOLEAN NOT NULL DEFAULT false,
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    terminated_at TIMESTAMPTZ,
    terminated_by UUID REFERENCES users(id),
    termination_reason VARCHAR(100),

    UNIQUE(session_token)
);

-- Termination reasons: user_logout, admin_force_logout, session_expired,
--                      max_sessions_exceeded, role_changed, security_violation

CREATE INDEX idx_user_sessions_user ON user_sessions(user_id);
CREATE INDEX idx_user_sessions_expires ON user_sessions(expires_at);
CREATE INDEX idx_user_sessions_active ON user_sessions(user_id) WHERE terminated_at IS NULL;

COMMENT ON TABLE user_sessions IS 'Active and historical user sessions';
```

### security_violations
Log of security policy violations.

```sql
CREATE TABLE security_violations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id),
    violation_type VARCHAR(100) NOT NULL,
    description TEXT,
    severity VARCHAR(20) NOT NULL DEFAULT 'low',
    ip_address INET,
    user_agent TEXT,
    request_path VARCHAR(500),
    blocked BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Violation types:
--   copy_attempt, export_limit_exceeded, rate_limit_exceeded,
--   unauthorized_access, session_hijack_attempt, suspicious_activity
-- Severity: low, medium, high, critical

CREATE INDEX idx_security_violations_user ON security_violations(user_id);
CREATE INDEX idx_security_violations_type ON security_violations(violation_type);
CREATE INDEX idx_security_violations_created ON security_violations(created_at);

COMMENT ON TABLE security_violations IS 'Log of security policy violations';
```

---

## Data Migration Strategy

### Creating New Tenant Schema

```sql
-- Function to create tenant schema
CREATE OR REPLACE FUNCTION create_tenant_schema(tenant_slug VARCHAR)
RETURNS VOID AS $$
DECLARE
    schema_name VARCHAR;
BEGIN
    schema_name := 'tenant_' || tenant_slug;

    -- Create schema
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);

    -- Create all tenant tables in the new schema
    EXECUTE format('SET search_path TO %I', schema_name);

    -- Create tables (use migration scripts)
    -- ... table creation statements

    -- Reset search path
    SET search_path TO public;
END;
$$ LANGUAGE plpgsql;
```

### Tenant Data Deletion (GDPR)

```sql
-- Function to delete tenant data
CREATE OR REPLACE FUNCTION delete_tenant_data(tenant_id UUID)
RETURNS VOID AS $$
DECLARE
    schema_name VARCHAR;
BEGIN
    -- Get schema name
    SELECT t.schema_name INTO schema_name
    FROM tenants t WHERE t.id = tenant_id;

    -- Drop tenant schema (cascades all tables)
    EXECUTE format('DROP SCHEMA IF EXISTS %I CASCADE', schema_name);

    -- Delete from shared tables
    DELETE FROM audit_logs WHERE tenant_id = tenant_id;
    DELETE FROM tenant_features WHERE tenant_id = tenant_id;
    DELETE FROM subscriptions WHERE tenant_id = tenant_id;
    DELETE FROM tenants WHERE id = tenant_id;
END;
$$ LANGUAGE plpgsql;
```

---

## Next Steps

See the following documents for implementation details:
- [Backend Structure](03-backend-structure.md)
- [Frontend Structure](04-frontend-structure.md)
- [Feature Flags & RBAC](05-feature-flags-rbac.md)
