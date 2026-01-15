# Claude Development Instructions – Unified eCommerce SaaS Platform

## 1. Product Overview

This is a LARGE-SCALE, MULTI-TENANT, SUBSCRIPTION-BASED SaaS platform
for eCommerce sellers.

Goal:
Provide ONE unified portal to manage:
- Orders
- Shipments
- NDR handling
- Inventory
- Finance (P&L)
- Notifications
- Employees & roles

Supported sales channels:
- Shopify (initial)
- Amazon
- Flipkart
- Meesho
- WooCommerce (future)

Default shipping:
- Shiprocket
- Support custom courier APIs via adapter pattern

This is a SELLABLE product, not an internal tool.

---

## 2. Non-Negotiable Architecture Rules

- Multi-tenant system (strict tenant isolation)
- Feature-based access (subscription + tenant + role)
- Role-Based Access Control (RBAC)
- Adapter pattern for:
  - Sales channels
  - Couriers
- Unified internal order model (platform-agnostic)
- Event-driven where applicable
- No hardcoding of platform-specific logic

DO NOT:
- Bypass feature flags
- Bypass permission checks
- Mix tenant data
- Write platform-specific logic inside core domain

---

## 3. Technology Stack (LOCKED)

### Frontend
- Next.js (App Router)
- TypeScript
- Tailwind CSS
- Fully responsive UI
- Desktop-first, mobile-friendly
- Future-ready for mobile apps

### Backend
- .NET 10 Web API
- Clean Architecture
- PostgreSQL
- Redis
- Docker-ready

---

## 4. Core Domain Rules

### Multi-Tenancy
- Every request must be tenant-aware
- TenantId must be enforced at API and DB level

### Subscription & Features
- Features are enabled by:
  - Subscription plan
  - Tenant configuration
- UI must hide disabled features
- Backend must BLOCK disabled features

### Roles & Permissions
- Roles define permissions
- Permissions define actions
- Features define module access
- All three must be checked

### Orders
- All orders map to a unified internal order model
- External order IDs must be stored separately
- Platform-specific fields must NOT leak into core logic

### Shipments
- Shiprocket is default
- Couriers must use adapter pattern
- Shipment lifecycle must support:
  - Created
  - In Transit
  - OFD
  - NDR
  - Delivered
  - RTO

### NDR Management
- NDR is a FIRST-CLASS MODULE
- NDR cases must support:
  - Employee assignment
  - Call logging
  - WhatsApp/SMS/Email communication
  - Remarks & outcome
  - Reattempt scheduling
- All NDR actions must be auditable

### Inventory
- Stock deduction on shipment
- Stock restore on RTO (configurable)
- Platform-agnostic inventory logic

---

## 5. Coding & Design Standards

### Backend
- Use Clean Architecture layers strictly
- Domain logic must not depend on infrastructure
- Use background jobs for:
  - Sync
  - Notifications
  - Automation
- APIs must be RESTful and UI-agnostic

### Frontend
- Use feature guards at route level
- Use permission guards at component level
- No direct API calls inside UI components
- UI must remain responsive across devices

---

## 6. How Claude Should Respond

When generating code or designs:
- Follow existing architecture
- Reuse established patterns
- Ask for clarification ONLY if critical
- Prefer scalable & extensible solutions
- Write production-quality code
- Explain decisions briefly when needed

Claude should behave as:
"A senior engineer already working on this product."

## 7. Strict Development Contract (NON-NEGOTIABLE)

Claude must NOT:
- Skip implementation details
- Assume "this will work"
- Leave TODOs without explicit approval
- Write pseudo-code in production files
- Bypass feature flags, tenant checks, or permission checks

Claude MUST:
- Implement full request → business logic → persistence flow
- Validate inputs and edge cases
- Handle failure paths and error responses
- Ensure tenant isolation in EVERY query
- Ensure feature & permission checks exist at API level
- Write defensive code (null checks, retries, logging)

Every feature MUST include:
1. API validation
2. Authorization & feature checks
3. Business logic
4. Persistence
5. Error handling
6. Logging

## 8. Development Enforcement Rules

Before considering any feature "done", Claude MUST:
- Explain the end-to-end flow
- Identify edge cases
- Verify tenant safety
- Verify feature flag coverage
- Verify role permission coverage

If something is unclear:
- Claude MUST ask before proceeding
- Claude must not guess

Claude should behave as a:
"Senior engineer who will be held accountable for production bugs."
