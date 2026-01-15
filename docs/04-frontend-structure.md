# Frontend Structure

## Table of Contents
1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Routing Architecture](#routing-architecture)
4. [Component Organization](#component-organization)
5. [State Management](#state-management)
6. [API Integration](#api-integration)
7. [Responsive Design Strategy](#responsive-design-strategy)
8. [Authentication & Authorization](#authentication--authorization)
9. [Security & Data Protection](#security--data-protection)

---

## Overview

The frontend is built with **Next.js 14+** using the **App Router**, providing a modern, performant, and SEO-friendly application.

### Core Requirement: 100% Responsive Design

> **CRITICAL**: The web application MUST be completely responsive and fully functional across ALL device sizes - from mobile phones to large desktop monitors. This is a non-negotiable requirement.

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                        RESPONSIVE DESIGN MANDATE                                     │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   ✓ Mobile First Approach    - Design for mobile, enhance for desktop               │
│   ✓ Touch-Friendly UI        - Minimum 44px touch targets                           │
│   ✓ Fluid Layouts            - No horizontal scrolling at any breakpoint            │
│   ✓ Adaptive Components      - Tables → Cards, Sidebar → Bottom Nav                 │
│   ✓ Optimized Media          - Responsive images, lazy loading                      │
│   ✓ Cross-Browser Support    - Chrome, Safari, Firefox, Edge                        │
│   ✓ PWA Ready                - Installable, offline-capable                         │
│                                                                                      │
│   Supported Devices:                                                                 │
│   ├── 📱 Mobile (320px - 639px)      - Full functionality, optimized UX             │
│   ├── 📱 Mobile Landscape (640px - 767px)                                           │
│   ├── 📲 Tablet (768px - 1023px)     - Hybrid mobile/desktop experience            │
│   ├── 💻 Desktop (1024px - 1279px)   - Full desktop experience                     │
│   ├── 🖥️ Large Desktop (1280px+)     - Enhanced layouts                             │
│   └── 📺 Ultra-wide (1536px+)        - Multi-column layouts                         │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### Technology Stack
- **Next.js 14+** - React framework with App Router
- **TypeScript** - Type safety
- **Tailwind CSS** - Utility-first styling (mobile-first responsive classes)
- **shadcn/ui** - Accessible, responsive component library
- **Zustand** - Lightweight state management
- **TanStack Query** - Server state management
- **React Hook Form** - Form handling
- **Zod** - Schema validation

---

## Project Structure

```
frontend/
│
├── public/
│   ├── images/
│   │   ├── logo.svg
│   │   ├── logo-dark.svg
│   │   └── channels/
│   │       ├── shopify.svg
│   │       ├── amazon.svg
│   │       ├── flipkart.svg
│   │       ├── meesho.svg
│   │       └── woocommerce.svg
│   ├── fonts/
│   └── manifest.json
│
├── src/
│   │
│   ├── app/                                    # Next.js App Router
│   │   │
│   │   ├── (auth)/                             # Auth Layout Group (public)
│   │   │   ├── layout.tsx                      # Auth pages layout
│   │   │   ├── login/
│   │   │   │   └── page.tsx
│   │   │   ├── register/
│   │   │   │   └── page.tsx
│   │   │   ├── forgot-password/
│   │   │   │   └── page.tsx
│   │   │   ├── reset-password/
│   │   │   │   └── page.tsx
│   │   │   └── verify-email/
│   │   │       └── page.tsx
│   │   │
│   │   ├── (dashboard)/                        # Dashboard Layout Group (protected)
│   │   │   ├── layout.tsx                      # Dashboard layout with sidebar
│   │   │   ├── page.tsx                        # Dashboard home /
│   │   │   │
│   │   │   ├── orders/
│   │   │   │   ├── page.tsx                    # /orders - Order list
│   │   │   │   ├── loading.tsx
│   │   │   │   ├── [orderId]/
│   │   │   │   │   └── page.tsx                # /orders/[id] - Order detail
│   │   │   │   └── create/
│   │   │   │       └── page.tsx                # /orders/create - Manual order
│   │   │   │
│   │   │   ├── shipments/
│   │   │   │   ├── page.tsx                    # /shipments - Shipment list
│   │   │   │   ├── [shipmentId]/
│   │   │   │   │   └── page.tsx                # /shipments/[id] - Detail
│   │   │   │   ├── create/
│   │   │   │   │   └── page.tsx                # /shipments/create
│   │   │   │   └── tracking/
│   │   │   │       └── page.tsx                # /shipments/tracking - Bulk track
│   │   │   │
│   │   │   ├── ndr/
│   │   │   │   ├── page.tsx                    # /ndr - NDR inbox
│   │   │   │   ├── [ndrId]/
│   │   │   │   │   └── page.tsx                # /ndr/[id] - NDR detail
│   │   │   │   ├── assigned/
│   │   │   │   │   └── page.tsx                # /ndr/assigned - My NDRs
│   │   │   │   ├── analytics/
│   │   │   │   │   └── page.tsx                # /ndr/analytics
│   │   │   │   └── settings/
│   │   │   │       └── page.tsx                # /ndr/settings - Workflow config
│   │   │   │
│   │   │   ├── inventory/
│   │   │   │   ├── page.tsx                    # /inventory - Overview
│   │   │   │   ├── products/
│   │   │   │   │   ├── page.tsx                # /inventory/products
│   │   │   │   │   ├── [productId]/
│   │   │   │   │   │   └── page.tsx
│   │   │   │   │   └── create/
│   │   │   │   │       └── page.tsx
│   │   │   │   ├── stock-movements/
│   │   │   │   │   └── page.tsx                # /inventory/stock-movements
│   │   │   │   └── sync/
│   │   │   │       └── page.tsx                # /inventory/sync
│   │   │   │
│   │   │   ├── channels/
│   │   │   │   ├── page.tsx                    # /channels - Connected channels
│   │   │   │   ├── connect/
│   │   │   │   │   └── page.tsx                # /channels/connect
│   │   │   │   └── [channelId]/
│   │   │   │       ├── page.tsx                # /channels/[id]
│   │   │   │       └── settings/
│   │   │   │           └── page.tsx
│   │   │   │
│   │   │   ├── couriers/
│   │   │   │   ├── page.tsx                    # /couriers
│   │   │   │   ├── configure/
│   │   │   │   │   └── page.tsx                # /couriers/configure
│   │   │   │   └── [courierId]/
│   │   │   │       └── page.tsx
│   │   │   │
│   │   │   ├── finance/
│   │   │   │   ├── page.tsx                    # /finance - P&L overview
│   │   │   │   ├── expenses/
│   │   │   │   │   └── page.tsx                # /finance/expenses
│   │   │   │   ├── revenue/
│   │   │   │   │   └── page.tsx                # /finance/revenue
│   │   │   │   └── reports/
│   │   │   │       └── page.tsx                # /finance/reports
│   │   │   │
│   │   │   ├── analytics/
│   │   │   │   ├── page.tsx                    # /analytics - Dashboard
│   │   │   │   ├── orders/
│   │   │   │   │   └── page.tsx
│   │   │   │   ├── channels/
│   │   │   │   │   └── page.tsx
│   │   │   │   └── performance/
│   │   │   │       └── page.tsx
│   │   │   │
│   │   │   ├── team/
│   │   │   │   ├── page.tsx                    # /team - Members list
│   │   │   │   ├── invite/
│   │   │   │   │   └── page.tsx                # /team/invite
│   │   │   │   ├── [userId]/
│   │   │   │   │   └── page.tsx                # /team/[id]
│   │   │   │   └── roles/
│   │   │   │       ├── page.tsx                # /team/roles
│   │   │   │       ├── create/
│   │   │   │       │   └── page.tsx
│   │   │   │       └── [roleId]/
│   │   │   │           └── page.tsx
│   │   │   │
│   │   │   ├── notifications/
│   │   │   │   ├── page.tsx                    # /notifications
│   │   │   │   └── settings/
│   │   │   │       └── page.tsx
│   │   │   │
│   │   │   └── settings/
│   │   │       ├── page.tsx                    # /settings - General
│   │   │       ├── profile/
│   │   │       │   └── page.tsx                # /settings/profile
│   │   │       ├── billing/
│   │   │       │   └── page.tsx                # /settings/billing
│   │   │       ├── subscription/
│   │   │       │   └── page.tsx                # /settings/subscription
│   │   │       ├── security/
│   │   │       │   └── page.tsx                # /settings/security
│   │   │       └── api-keys/
│   │   │           └── page.tsx                # /settings/api-keys
│   │   │
│   │   ├── (superadmin)/                       # Super Admin Routes
│   │   │   ├── layout.tsx
│   │   │   ├── admin/
│   │   │   │   ├── page.tsx                    # /admin - Dashboard
│   │   │   │   ├── tenants/
│   │   │   │   │   ├── page.tsx
│   │   │   │   │   ├── [tenantId]/
│   │   │   │   │   │   └── page.tsx
│   │   │   │   │   └── create/
│   │   │   │   │       └── page.tsx
│   │   │   │   ├── plans/
│   │   │   │   │   ├── page.tsx
│   │   │   │   │   └── [planId]/
│   │   │   │   │       └── page.tsx
│   │   │   │   ├── features/
│   │   │   │   │   └── page.tsx
│   │   │   │   ├── subscriptions/
│   │   │   │   │   └── page.tsx
│   │   │   │   └── system/
│   │   │   │       ├── health/
│   │   │   │       │   └── page.tsx
│   │   │   │       └── logs/
│   │   │   │           └── page.tsx
│   │   │
│   │   ├── api/                                # API Routes (BFF)
│   │   │   ├── auth/
│   │   │   │   └── [...nextauth]/
│   │   │   │       └── route.ts
│   │   │   └── proxy/
│   │   │       └── [...path]/
│   │   │           └── route.ts
│   │   │
│   │   ├── layout.tsx                          # Root layout
│   │   ├── page.tsx                            # Landing page (/)
│   │   ├── loading.tsx                         # Global loading
│   │   ├── error.tsx                           # Global error
│   │   ├── not-found.tsx                       # 404 page
│   │   └── globals.css                         # Global styles
│   │
│   │
│   ├── components/
│   │   │
│   │   ├── ui/                                 # Base UI (shadcn/ui)
│   │   │   ├── button.tsx
│   │   │   ├── input.tsx
│   │   │   ├── select.tsx
│   │   │   ├── textarea.tsx
│   │   │   ├── checkbox.tsx
│   │   │   ├── radio-group.tsx
│   │   │   ├── switch.tsx
│   │   │   ├── dialog.tsx
│   │   │   ├── sheet.tsx                       # Mobile drawer
│   │   │   ├── dropdown-menu.tsx
│   │   │   ├── popover.tsx
│   │   │   ├── tooltip.tsx
│   │   │   ├── toast.tsx
│   │   │   ├── badge.tsx
│   │   │   ├── avatar.tsx
│   │   │   ├── card.tsx
│   │   │   ├── table.tsx
│   │   │   ├── tabs.tsx
│   │   │   ├── accordion.tsx
│   │   │   ├── skeleton.tsx
│   │   │   ├── spinner.tsx
│   │   │   ├── progress.tsx
│   │   │   ├── slider.tsx
│   │   │   ├── calendar.tsx
│   │   │   ├── date-picker.tsx
│   │   │   ├── command.tsx
│   │   │   ├── combobox.tsx
│   │   │   └── separator.tsx
│   │   │
│   │   ├── layout/                             # Layout Components
│   │   │   ├── header/
│   │   │   │   ├── header.tsx
│   │   │   │   ├── mobile-header.tsx
│   │   │   │   ├── user-menu.tsx
│   │   │   │   ├── notification-bell.tsx
│   │   │   │   └── search-command.tsx
│   │   │   ├── sidebar/
│   │   │   │   ├── sidebar.tsx
│   │   │   │   ├── sidebar-item.tsx
│   │   │   │   ├── sidebar-mobile.tsx
│   │   │   │   ├── sidebar-collapse.tsx
│   │   │   │   └── sidebar-nav.tsx
│   │   │   ├── footer/
│   │   │   │   └── footer.tsx
│   │   │   ├── breadcrumbs.tsx
│   │   │   ├── page-header.tsx
│   │   │   └── container.tsx
│   │   │
│   │   ├── features/                           # Feature Components
│   │   │   │
│   │   │   ├── orders/
│   │   │   │   ├── order-table.tsx
│   │   │   │   ├── order-table-columns.tsx
│   │   │   │   ├── order-card.tsx              # Mobile card view
│   │   │   │   ├── order-filters.tsx
│   │   │   │   ├── order-timeline.tsx
│   │   │   │   ├── order-actions.tsx
│   │   │   │   ├── order-status-badge.tsx
│   │   │   │   ├── order-detail-card.tsx
│   │   │   │   └── order-item-list.tsx
│   │   │   │
│   │   │   ├── shipments/
│   │   │   │   ├── shipment-table.tsx
│   │   │   │   ├── shipment-card.tsx
│   │   │   │   ├── shipment-form.tsx
│   │   │   │   ├── tracking-timeline.tsx
│   │   │   │   ├── courier-selector.tsx
│   │   │   │   └── shipment-status-badge.tsx
│   │   │   │
│   │   │   ├── ndr/
│   │   │   │   ├── ndr-inbox.tsx
│   │   │   │   ├── ndr-table.tsx
│   │   │   │   ├── ndr-card.tsx
│   │   │   │   ├── ndr-detail-panel.tsx
│   │   │   │   ├── ndr-action-panel.tsx
│   │   │   │   ├── ndr-call-form.tsx
│   │   │   │   ├── ndr-whatsapp-form.tsx
│   │   │   │   ├── ndr-sms-form.tsx
│   │   │   │   ├── ndr-remark-form.tsx
│   │   │   │   ├── ndr-reattempt-dialog.tsx
│   │   │   │   ├── ndr-assignment-dialog.tsx
│   │   │   │   ├── ndr-status-badge.tsx
│   │   │   │   ├── ndr-priority-badge.tsx
│   │   │   │   ├── ndr-action-history.tsx
│   │   │   │   └── ndr-analytics-charts.tsx
│   │   │   │
│   │   │   ├── inventory/
│   │   │   │   ├── product-table.tsx
│   │   │   │   ├── product-card.tsx
│   │   │   │   ├── product-form.tsx
│   │   │   │   ├── stock-level-indicator.tsx
│   │   │   │   ├── stock-adjustment-form.tsx
│   │   │   │   ├── inventory-sync-status.tsx
│   │   │   │   └── low-stock-alert.tsx
│   │   │   │
│   │   │   ├── channels/
│   │   │   │   ├── channel-card.tsx
│   │   │   │   ├── channel-grid.tsx
│   │   │   │   ├── channel-connect-wizard.tsx
│   │   │   │   ├── channel-sync-status.tsx
│   │   │   │   ├── shopify-connect-form.tsx
│   │   │   │   ├── amazon-connect-form.tsx
│   │   │   │   └── woocommerce-connect-form.tsx
│   │   │   │
│   │   │   ├── couriers/
│   │   │   │   ├── courier-card.tsx
│   │   │   │   ├── courier-config-form.tsx
│   │   │   │   └── shiprocket-config-form.tsx
│   │   │   │
│   │   │   ├── finance/
│   │   │   │   ├── pl-summary-card.tsx
│   │   │   │   ├── expense-table.tsx
│   │   │   │   ├── expense-form.tsx
│   │   │   │   ├── revenue-chart.tsx
│   │   │   │   └── platform-breakdown.tsx
│   │   │   │
│   │   │   ├── team/
│   │   │   │   ├── member-table.tsx
│   │   │   │   ├── member-card.tsx
│   │   │   │   ├── invite-form.tsx
│   │   │   │   ├── role-form.tsx
│   │   │   │   └── permission-matrix.tsx
│   │   │   │
│   │   │   ├── analytics/
│   │   │   │   ├── stats-card.tsx
│   │   │   │   ├── stats-grid.tsx
│   │   │   │   ├── chart-container.tsx
│   │   │   │   ├── line-chart.tsx
│   │   │   │   ├── bar-chart.tsx
│   │   │   │   ├── pie-chart.tsx
│   │   │   │   └── metric-trend.tsx
│   │   │   │
│   │   │   └── dashboard/
│   │   │       ├── dashboard-stats.tsx
│   │   │       ├── recent-orders.tsx
│   │   │       ├── ndr-summary.tsx
│   │   │       ├── channel-performance.tsx
│   │   │       └── quick-actions.tsx
│   │   │
│   │   ├── shared/                             # Shared Components
│   │   │   ├── data-table/
│   │   │   │   ├── data-table.tsx
│   │   │   │   ├── data-table-pagination.tsx
│   │   │   │   ├── data-table-toolbar.tsx
│   │   │   │   ├── data-table-column-header.tsx
│   │   │   │   ├── data-table-row-actions.tsx
│   │   │   │   └── data-table-faceted-filter.tsx
│   │   │   ├── forms/
│   │   │   │   ├── form-field.tsx
│   │   │   │   ├── form-section.tsx
│   │   │   │   ├── form-actions.tsx
│   │   │   │   └── address-form.tsx
│   │   │   ├── empty-state.tsx
│   │   │   ├── error-boundary.tsx
│   │   │   ├── confirm-dialog.tsx
│   │   │   ├── search-input.tsx
│   │   │   ├── filter-panel.tsx
│   │   │   ├── export-button.tsx
│   │   │   ├── feature-gate.tsx
│   │   │   ├── permission-gate.tsx
│   │   │   ├── loading-overlay.tsx
│   │   │   ├── status-dot.tsx
│   │   │   └── copy-button.tsx
│   │   │
│   │   └── providers/
│   │       ├── auth-provider.tsx
│   │       ├── tenant-provider.tsx
│   │       ├── feature-provider.tsx
│   │       ├── query-provider.tsx
│   │       ├── theme-provider.tsx
│   │       └── toast-provider.tsx
│   │
│   │
│   ├── hooks/
│   │   ├── use-auth.ts
│   │   ├── use-tenant.ts
│   │   ├── use-permissions.ts
│   │   ├── use-features.ts
│   │   ├── use-media-query.ts
│   │   ├── use-mobile.ts
│   │   ├── use-debounce.ts
│   │   ├── use-local-storage.ts
│   │   ├── use-toast.ts
│   │   ├── use-infinite-scroll.ts
│   │   ├── use-clipboard.ts
│   │   └── queries/
│   │       ├── use-orders.ts
│   │       ├── use-shipments.ts
│   │       ├── use-ndr.ts
│   │       ├── use-inventory.ts
│   │       ├── use-channels.ts
│   │       ├── use-analytics.ts
│   │       └── use-team.ts
│   │
│   │
│   ├── lib/
│   │   ├── api/
│   │   │   ├── client.ts                       # Axios/fetch setup
│   │   │   ├── endpoints.ts                    # API endpoint constants
│   │   │   ├── interceptors.ts                 # Request/response interceptors
│   │   │   └── types.ts                        # API response types
│   │   ├── auth/
│   │   │   ├── auth-options.ts                 # NextAuth config
│   │   │   └── session.ts                      # Session helpers
│   │   ├── utils/
│   │   │   ├── cn.ts                           # classNames helper
│   │   │   ├── formatters.ts                   # Date, currency formatters
│   │   │   ├── validators.ts                   # Validation utilities
│   │   │   └── constants.ts                    # App constants
│   │   ├── validations/
│   │   │   ├── auth.ts                         # Auth form schemas
│   │   │   ├── order.ts                        # Order schemas
│   │   │   └── common.ts                       # Common schemas
│   │   └── config.ts                           # App configuration
│   │
│   │
│   ├── services/                               # API Service Layer
│   │   ├── auth.service.ts
│   │   ├── orders.service.ts
│   │   ├── shipments.service.ts
│   │   ├── ndr.service.ts
│   │   ├── inventory.service.ts
│   │   ├── channels.service.ts
│   │   ├── couriers.service.ts
│   │   ├── users.service.ts
│   │   ├── roles.service.ts
│   │   ├── finance.service.ts
│   │   ├── analytics.service.ts
│   │   ├── notifications.service.ts
│   │   └── settings.service.ts
│   │
│   │
│   ├── store/                                  # Zustand Stores
│   │   ├── auth-store.ts
│   │   ├── tenant-store.ts
│   │   ├── ui-store.ts
│   │   ├── notification-store.ts
│   │   └── filter-store.ts
│   │
│   │
│   ├── types/
│   │   ├── api.ts                              # API response types
│   │   ├── auth.ts
│   │   ├── order.ts
│   │   ├── shipment.ts
│   │   ├── ndr.ts
│   │   ├── inventory.ts
│   │   ├── channel.ts
│   │   ├── user.ts
│   │   ├── subscription.ts
│   │   ├── analytics.ts
│   │   └── common.ts
│   │
│   │
│   └── styles/
│       └── globals.css
│
│
├── .env.example
├── .env.local
├── .eslintrc.json
├── .prettierrc
├── components.json                             # shadcn/ui config
├── next.config.js
├── tailwind.config.js
├── tsconfig.json
├── package.json
└── README.md
```

---

## Routing Architecture

### Route Groups

```
/                           → Landing page
├── (auth)/                 → Public auth routes
│   ├── login
│   ├── register
│   ├── forgot-password
│   └── reset-password
│
├── (dashboard)/            → Protected tenant routes
│   ├── /                   → Dashboard home
│   ├── orders/
│   ├── shipments/
│   ├── ndr/
│   ├── inventory/
│   ├── channels/
│   ├── couriers/
│   ├── finance/
│   ├── analytics/
│   ├── team/
│   ├── notifications/
│   └── settings/
│
└── (superadmin)/           → Super admin routes
    └── admin/
        ├── tenants/
        ├── plans/
        ├── features/
        └── system/
```

### Layout Structure

```tsx
// app/(dashboard)/layout.tsx
import { redirect } from 'next/navigation';
import { getServerSession } from 'next-auth';
import { Sidebar } from '@/components/layout/sidebar';
import { Header } from '@/components/layout/header';
import { TenantProvider } from '@/components/providers/tenant-provider';
import { FeatureProvider } from '@/components/providers/feature-provider';

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getServerSession();

  if (!session) {
    redirect('/login');
  }

  return (
    <TenantProvider>
      <FeatureProvider>
        <div className="flex h-screen overflow-hidden">
          {/* Sidebar - hidden on mobile */}
          <Sidebar className="hidden lg:flex" />

          {/* Main content */}
          <div className="flex flex-1 flex-col overflow-hidden">
            <Header />
            <main className="flex-1 overflow-y-auto bg-gray-50 p-4 lg:p-6">
              {children}
            </main>
          </div>
        </div>
      </FeatureProvider>
    </TenantProvider>
  );
}
```

---

## Component Organization

### UI Components (shadcn/ui pattern)

```tsx
// components/ui/button.tsx
import * as React from 'react';
import { Slot } from '@radix-ui/react-slot';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils/cn';

const buttonVariants = cva(
  'inline-flex items-center justify-center whitespace-nowrap rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'bg-primary text-primary-foreground shadow hover:bg-primary/90',
        destructive: 'bg-destructive text-destructive-foreground shadow-sm hover:bg-destructive/90',
        outline: 'border border-input bg-background shadow-sm hover:bg-accent hover:text-accent-foreground',
        secondary: 'bg-secondary text-secondary-foreground shadow-sm hover:bg-secondary/80',
        ghost: 'hover:bg-accent hover:text-accent-foreground',
        link: 'text-primary underline-offset-4 hover:underline',
      },
      size: {
        default: 'h-9 px-4 py-2',
        sm: 'h-8 rounded-md px-3 text-xs',
        lg: 'h-10 rounded-md px-8',
        icon: 'h-9 w-9',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  }
);

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button';
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    );
  }
);
Button.displayName = 'Button';

export { Button, buttonVariants };
```

### Feature Components

```tsx
// components/features/ndr/ndr-inbox.tsx
'use client';

import { useState } from 'react';
import { useNdrList } from '@/hooks/queries/use-ndr';
import { NdrTable } from './ndr-table';
import { NdrCard } from './ndr-card';
import { NdrFilters } from './ndr-filters';
import { useMediaQuery } from '@/hooks/use-media-query';
import { FeatureGate } from '@/components/shared/feature-gate';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';

interface NdrInboxProps {
  initialFilters?: NdrFilters;
}

export function NdrInbox({ initialFilters }: NdrInboxProps) {
  const isMobile = useMediaQuery('(max-width: 768px)');
  const [filters, setFilters] = useState<NdrFilters>(initialFilters ?? {});
  const [activeTab, setActiveTab] = useState('all');

  const { data, isLoading, fetchNextPage, hasNextPage } = useNdrList({
    ...filters,
    status: activeTab === 'all' ? undefined : activeTab,
  });

  return (
    <FeatureGate feature="ndr_management">
      <div className="space-y-4">
        {/* Filters */}
        <NdrFilters
          filters={filters}
          onChange={setFilters}
        />

        {/* Status tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList>
            <TabsTrigger value="all">All</TabsTrigger>
            <TabsTrigger value="open">Open</TabsTrigger>
            <TabsTrigger value="in_progress">In Progress</TabsTrigger>
            <TabsTrigger value="resolved">Resolved</TabsTrigger>
          </TabsList>
        </Tabs>

        {/* Responsive view */}
        {isMobile ? (
          // Mobile: Card view
          <div className="space-y-3">
            {data?.pages.flatMap((page) =>
              page.items.map((ndr) => (
                <NdrCard key={ndr.id} ndr={ndr} />
              ))
            )}
          </div>
        ) : (
          // Desktop: Table view
          <NdrTable
            data={data?.pages.flatMap((page) => page.items) ?? []}
            isLoading={isLoading}
          />
        )}

        {/* Load more */}
        {hasNextPage && (
          <button
            onClick={() => fetchNextPage()}
            className="w-full py-2 text-sm text-muted-foreground"
          >
            Load more
          </button>
        )}
      </div>
    </FeatureGate>
  );
}
```

---

## State Management

### Zustand Store Example

```tsx
// store/auth-store.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface User {
  id: string;
  email: string;
  name: string;
  tenantId: string;
  isOwner: boolean;
}

interface AuthState {
  user: User | null;
  accessToken: string | null;
  isAuthenticated: boolean;

  // Actions
  setAuth: (user: User, accessToken: string) => void;
  clearAuth: () => void;
  updateUser: (user: Partial<User>) => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      isAuthenticated: false,

      setAuth: (user, accessToken) =>
        set({
          user,
          accessToken,
          isAuthenticated: true,
        }),

      clearAuth: () =>
        set({
          user: null,
          accessToken: null,
          isAuthenticated: false,
        }),

      updateUser: (updates) =>
        set((state) => ({
          user: state.user ? { ...state.user, ...updates } : null,
        })),
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
```

### TanStack Query Hooks

```tsx
// hooks/queries/use-orders.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ordersService } from '@/services/orders.service';
import type { Order, OrderFilters, CreateOrderInput } from '@/types/order';

// Query keys
export const orderKeys = {
  all: ['orders'] as const,
  lists: () => [...orderKeys.all, 'list'] as const,
  list: (filters: OrderFilters) => [...orderKeys.lists(), filters] as const,
  details: () => [...orderKeys.all, 'detail'] as const,
  detail: (id: string) => [...orderKeys.details(), id] as const,
};

// Get orders list
export function useOrders(filters: OrderFilters = {}) {
  return useQuery({
    queryKey: orderKeys.list(filters),
    queryFn: () => ordersService.getOrders(filters),
  });
}

// Get single order
export function useOrder(id: string) {
  return useQuery({
    queryKey: orderKeys.detail(id),
    queryFn: () => ordersService.getOrder(id),
    enabled: !!id,
  });
}

// Create order mutation
export function useCreateOrder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateOrderInput) => ordersService.createOrder(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() });
    },
  });
}

// Update order status
export function useUpdateOrderStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      ordersService.updateOrderStatus(id, status),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: orderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() });
    },
  });
}
```

---

## API Integration

### API Client Setup

```tsx
// lib/api/client.ts
import axios, { AxiosError, AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/auth-store';
import { useTenantStore } from '@/store/tenant-store';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1';

// Create axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const { accessToken } = useAuthStore.getState();
    const { tenantId } = useTenantStore.getState();

    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }

    if (tenantId) {
      config.headers['X-Tenant-Id'] = tenantId;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Handle 401 - Token expired
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        // Attempt token refresh
        const { data } = await axios.post(`${API_BASE_URL}/auth/refresh-token`);
        useAuthStore.getState().setAuth(data.user, data.accessToken);

        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        useAuthStore.getState().clearAuth();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);
```

### Service Layer

```tsx
// services/orders.service.ts
import { apiClient } from '@/lib/api/client';
import type {
  Order,
  OrderDto,
  OrderListDto,
  OrderFilters,
  CreateOrderInput,
  PaginatedResponse,
} from '@/types/order';

class OrdersService {
  private basePath = '/orders';

  async getOrders(filters: OrderFilters = {}): Promise<PaginatedResponse<OrderListDto>> {
    const params = new URLSearchParams();

    if (filters.status) params.append('status', filters.status);
    if (filters.channelId) params.append('channelId', filters.channelId);
    if (filters.search) params.append('search', filters.search);
    if (filters.startDate) params.append('startDate', filters.startDate);
    if (filters.endDate) params.append('endDate', filters.endDate);
    if (filters.page) params.append('page', filters.page.toString());
    if (filters.pageSize) params.append('pageSize', filters.pageSize.toString());

    const { data } = await apiClient.get(`${this.basePath}?${params}`);
    return data.data;
  }

  async getOrder(id: string): Promise<OrderDto> {
    const { data } = await apiClient.get(`${this.basePath}/${id}`);
    return data.data;
  }

  async createOrder(input: CreateOrderInput): Promise<OrderDto> {
    const { data } = await apiClient.post(this.basePath, input);
    return data.data;
  }

  async updateOrderStatus(id: string, status: string): Promise<OrderDto> {
    const { data } = await apiClient.patch(`${this.basePath}/${id}/status`, { status });
    return data.data;
  }

  async cancelOrder(id: string, reason?: string): Promise<void> {
    await apiClient.post(`${this.basePath}/${id}/cancel`, { reason });
  }

  async exportOrders(filters: OrderFilters): Promise<Blob> {
    const params = new URLSearchParams(filters as Record<string, string>);
    const { data } = await apiClient.get(`${this.basePath}/export?${params}`, {
      responseType: 'blob',
    });
    return data;
  }
}

export const ordersService = new OrdersService();
```

---

## Responsive Design Strategy

### Breakpoints

```tsx
// tailwind.config.js
module.exports = {
  theme: {
    screens: {
      'sm': '640px',   // Mobile landscape
      'md': '768px',   // Tablet
      'lg': '1024px',  // Desktop
      'xl': '1280px',  // Large desktop
      '2xl': '1536px', // Extra large
    },
  },
};
```

### Responsive Patterns

```tsx
// Pattern 1: Show/hide based on screen size
<div className="hidden lg:block">Desktop content</div>
<div className="lg:hidden">Mobile content</div>

// Pattern 2: Grid columns
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
  {items.map(item => <Card key={item.id} />)}
</div>

// Pattern 3: Responsive table → cards
{isMobile ? (
  <div className="space-y-3">
    {data.map(item => <ItemCard key={item.id} item={item} />)}
  </div>
) : (
  <DataTable columns={columns} data={data} />
)}

// Pattern 4: Sheet drawer for mobile
<Sheet open={open} onOpenChange={setOpen}>
  <SheetTrigger asChild>
    <Button variant="outline" className="lg:hidden">
      Filters
    </Button>
  </SheetTrigger>
  <SheetContent side="right" className="w-[300px]">
    <FilterPanel />
  </SheetContent>
</Sheet>
```

### Mobile Hook

```tsx
// hooks/use-mobile.ts
import { useMediaQuery } from './use-media-query';

export function useMobile() {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const isTablet = useMediaQuery('(min-width: 768px) and (max-width: 1023px)');
  const isDesktop = useMediaQuery('(min-width: 1024px)');

  return { isMobile, isTablet, isDesktop };
}
```

---

## Authentication & Authorization

### Auth Provider

```tsx
// components/providers/auth-provider.tsx
'use client';

import { createContext, useContext, useEffect, ReactNode } from 'react';
import { useAuthStore } from '@/store/auth-store';
import { authService } from '@/services/auth.service';

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const { user, isAuthenticated, setAuth, clearAuth } = useAuthStore();
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check session on mount
    const checkSession = async () => {
      try {
        const session = await authService.getSession();
        if (session) {
          setAuth(session.user, session.accessToken);
        }
      } catch (error) {
        clearAuth();
      } finally {
        setIsLoading(false);
      }
    };

    checkSession();
  }, []);

  const login = async (email: string, password: string) => {
    const response = await authService.login(email, password);
    setAuth(response.user, response.accessToken);
  };

  const logout = async () => {
    await authService.logout();
    clearAuth();
  };

  return (
    <AuthContext.Provider
      value={{ isAuthenticated, isLoading, user, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
```

### Feature Gate Component

```tsx
// components/shared/feature-gate.tsx
'use client';

import { ReactNode } from 'react';
import { useFeatures } from '@/hooks/use-features';
import { Card } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Lock } from 'lucide-react';

interface FeatureGateProps {
  feature: string;
  children: ReactNode;
  fallback?: ReactNode;
}

export function FeatureGate({ feature, children, fallback }: FeatureGateProps) {
  const { isEnabled, isLoading } = useFeatures();

  if (isLoading) {
    return <div className="animate-pulse h-32 bg-gray-100 rounded-lg" />;
  }

  if (!isEnabled(feature)) {
    if (fallback) return <>{fallback}</>;

    return (
      <Card className="p-8 text-center">
        <Lock className="mx-auto h-12 w-12 text-muted-foreground" />
        <h3 className="mt-4 text-lg font-semibold">Feature Not Available</h3>
        <p className="mt-2 text-sm text-muted-foreground">
          This feature is not included in your current plan.
        </p>
        <Button className="mt-4" variant="outline">
          Upgrade Plan
        </Button>
      </Card>
    );
  }

  return <>{children}</>;
}
```

### Permission Gate Component

```tsx
// components/shared/permission-gate.tsx
'use client';

import { ReactNode } from 'react';
import { usePermissions } from '@/hooks/use-permissions';

interface PermissionGateProps {
  permission: string | string[];
  requireAll?: boolean;
  children: ReactNode;
  fallback?: ReactNode;
}

export function PermissionGate({
  permission,
  requireAll = false,
  children,
  fallback = null,
}: PermissionGateProps) {
  const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermissions();

  const permissions = Array.isArray(permission) ? permission : [permission];

  const hasAccess = requireAll
    ? hasAllPermissions(...permissions)
    : hasAnyPermission(...permissions);

  if (!hasAccess) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

// Usage
<PermissionGate permission="orders.create">
  <Button>Create Order</Button>
</PermissionGate>

<PermissionGate permission={['ndr.view', 'ndr.action']} requireAll>
  <NdrActionPanel />
</PermissionGate>
```

---

## Security & Data Protection

> **CRITICAL**: All security features are role-based and controlled by the tenant admin through the Security Settings panel. These protections apply based on the user's assigned role.

### Security Context Provider

```tsx
// providers/security-provider.tsx
'use client';

import { createContext, useContext, useEffect, ReactNode } from 'react';
import { useQuery } from '@tanstack/react-query';
import { securityService } from '@/services/security.service';
import { useAuth } from '@/hooks/use-auth';

interface SecuritySettings {
  copyProtection: {
    enabled: boolean;
    disableTextSelection: boolean;
    disableRightClick: boolean;
    blockKeyboardShortcuts: boolean;
  };
  dataAccess: {
    viewFullData: boolean;
    canCopy: boolean;
    canPrint: boolean;
  };
  export: {
    enabled: boolean;
    allowedTypes: string[];
    maxRows: number;
  };
}

const SecurityContext = createContext<SecuritySettings | null>(null);

export function SecurityProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();

  const { data: settings } = useQuery({
    queryKey: ['security-settings', user?.roleId],
    queryFn: () => securityService.getMySecuritySettings(),
    enabled: !!user,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Apply global security restrictions
  useEffect(() => {
    if (!settings?.copyProtection.enabled) return;

    const handleCopy = (e: ClipboardEvent) => {
      if (!settings.dataAccess.canCopy) {
        e.preventDefault();
        // Log violation attempt
        securityService.logViolation('copy_attempt');
      }
    };

    const handleContextMenu = (e: MouseEvent) => {
      if (settings.copyProtection.disableRightClick) {
        const target = e.target as HTMLElement;
        if (target.closest('[data-protected]')) {
          e.preventDefault();
        }
      }
    };

    const handleKeyDown = (e: KeyboardEvent) => {
      if (settings.copyProtection.blockKeyboardShortcuts) {
        // Block Ctrl+C, Ctrl+A on protected areas
        if ((e.ctrlKey || e.metaKey) && ['c', 'a', 'p'].includes(e.key.toLowerCase())) {
          const target = e.target as HTMLElement;
          if (target.closest('[data-protected]')) {
            e.preventDefault();
            securityService.logViolation('keyboard_shortcut_blocked');
          }
        }
      }
    };

    document.addEventListener('copy', handleCopy);
    document.addEventListener('contextmenu', handleContextMenu);
    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('copy', handleCopy);
      document.removeEventListener('contextmenu', handleContextMenu);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [settings]);

  return (
    <SecurityContext.Provider value={settings ?? null}>
      {children}
    </SecurityContext.Provider>
  );
}

export const useSecurity = () => useContext(SecurityContext);
```

### Protected Data Wrapper Component

```tsx
// components/shared/protected-data.tsx
'use client';

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { useSecurity } from '@/providers/security-provider';

interface ProtectedDataProps {
  children: ReactNode;
  className?: string;
  sensitivity?: 'low' | 'medium' | 'high';
}

export function ProtectedData({
  children,
  className,
  sensitivity = 'medium',
}: ProtectedDataProps) {
  const security = useSecurity();

  const protectionClasses = cn(
    className,
    security?.copyProtection.disableTextSelection && 'select-none',
    sensitivity === 'high' && 'pointer-events-none'
  );

  return (
    <div
      data-protected
      data-sensitivity={sensitivity}
      className={protectionClasses}
      onDragStart={(e) => e.preventDefault()}
    >
      {children}
    </div>
  );
}
```

### Data Masking Component

```tsx
// components/shared/masked-data.tsx
'use client';

import { useMemo } from 'react';
import { useSecurity } from '@/providers/security-provider';
import { usePermissions } from '@/hooks/use-permissions';

interface MaskedDataProps {
  value: string;
  type: 'phone' | 'email' | 'address' | 'sensitive';
  showCopyButton?: boolean;
}

export function MaskedData({ value, type, showCopyButton = false }: MaskedDataProps) {
  const security = useSecurity();
  const { hasPermission } = usePermissions();

  const canViewFull = hasPermission('data.view_full');
  const canCopy = hasPermission('data.copy');

  const displayValue = useMemo(() => {
    if (canViewFull) return value;

    switch (type) {
      case 'phone':
        // 9876543210 → 98****3210
        return value.replace(/(\d{2})(\d{4})(\d{4})/, '$1****$3');

      case 'email':
        // john@example.com → jo**@***.com
        const [local, domain] = value.split('@');
        const maskedLocal = local.slice(0, 2) + '**';
        const maskedDomain = '***.com';
        return `${maskedLocal}@${maskedDomain}`;

      case 'address':
        // Show only first line
        return value.split('\n')[0] + ', ***';

      case 'sensitive':
        // Show only last 4 characters
        return '*'.repeat(value.length - 4) + value.slice(-4);

      default:
        return value;
    }
  }, [value, type, canViewFull]);

  return (
    <span className="inline-flex items-center gap-1">
      <span data-protected>{displayValue}</span>
      {showCopyButton && canCopy && canViewFull && (
        <button
          className="text-gray-400 hover:text-gray-600"
          onClick={() => navigator.clipboard.writeText(value)}
        >
          <CopyIcon className="h-3.5 w-3.5" />
        </button>
      )}
    </span>
  );
}

// Usage
<MaskedData value="9876543210" type="phone" />
<MaskedData value="john@example.com" type="email" />
<MaskedData value="1234567890123456" type="sensitive" />
```

### Export Permission Gate

```tsx
// components/shared/export-button.tsx
'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Download, Loader2, Lock } from 'lucide-react';
import { usePermissions } from '@/hooks/use-permissions';
import { useSecurity } from '@/providers/security-provider';
import { exportService } from '@/services/export.service';
import { toast } from 'sonner';

interface ExportButtonProps {
  exportType: 'orders_csv' | 'orders_excel' | 'customers' | 'financial' | 'ndr' | 'inventory' | 'analytics';
  filters?: Record<string, unknown>;
  disabled?: boolean;
}

export function ExportButton({ exportType, filters, disabled }: ExportButtonProps) {
  const [loading, setLoading] = useState(false);
  const { hasPermission } = usePermissions();
  const security = useSecurity();

  const permissionCode = `export.${exportType}`;
  const canExport = hasPermission(permissionCode) &&
    security?.export.enabled &&
    security?.export.allowedTypes.includes(exportType);

  const handleExport = async () => {
    if (!canExport) return;

    setLoading(true);
    try {
      const result = await exportService.requestExport({
        type: exportType,
        filters,
      });

      if (result.requiresApproval) {
        toast.info('Export request submitted for approval');
      } else {
        // Download file
        window.open(result.downloadUrl, '_blank');
        toast.success('Export started');
      }
    } catch (error) {
      toast.error('Export failed');
    } finally {
      setLoading(false);
    }
  };

  if (!canExport) {
    return (
      <Button variant="outline" disabled>
        <Lock className="mr-2 h-4 w-4" />
        Export
      </Button>
    );
  }

  return (
    <Button
      variant="outline"
      onClick={handleExport}
      disabled={disabled || loading}
    >
      {loading ? (
        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
      ) : (
        <Download className="mr-2 h-4 w-4" />
      )}
      Export
    </Button>
  );
}
```

### Print Protection

```tsx
// components/shared/print-protected.tsx
'use client';

import { useEffect } from 'react';
import { usePermissions } from '@/hooks/use-permissions';
import { securityService } from '@/services/security.service';

export function PrintProtection() {
  const { hasPermission } = usePermissions();
  const canPrint = hasPermission('data.print');

  useEffect(() => {
    if (canPrint) return;

    const handleBeforePrint = () => {
      // Log print attempt
      securityService.logViolation('print_attempt');

      // Hide sensitive content during print
      document.body.classList.add('print-protected');
    };

    const handleAfterPrint = () => {
      document.body.classList.remove('print-protected');
    };

    window.addEventListener('beforeprint', handleBeforePrint);
    window.addEventListener('afterprint', handleAfterPrint);

    return () => {
      window.removeEventListener('beforeprint', handleBeforePrint);
      window.removeEventListener('afterprint', handleAfterPrint);
    };
  }, [canPrint]);

  return null;
}

// Add to global CSS:
// @media print {
//   .print-protected [data-protected] {
//     visibility: hidden !important;
//   }
//   .print-protected [data-protected]::after {
//     content: '[PROTECTED]';
//     visibility: visible;
//   }
// }
```

### Security Admin Settings Page

```tsx
// app/(dashboard)/settings/security/page.tsx
'use client';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { securityService } from '@/services/security.service';
import { FeatureGate } from '@/components/shared/feature-gate';
import { PermissionGate } from '@/components/shared/permission-gate';

const securitySettingsSchema = z.object({
  copyProtection: z.object({
    enabled: z.boolean(),
    disableTextSelection: z.boolean(),
    disableRightClick: z.boolean(),
    blockKeyboardShortcuts: z.boolean(),
  }),
  export: z.object({
    enabled: z.boolean(),
    requireWatermark: z.boolean(),
    maxRows: z.number().min(100).max(100000),
    maxExportsPerDay: z.number().min(1).max(100),
    requireApprovalThreshold: z.number().min(100),
  }),
  dataMasking: z.object({
    enabled: z.boolean(),
    maskPhoneNumbers: z.boolean(),
    maskEmailAddresses: z.boolean(),
    maskAddresses: z.boolean(),
  }),
  session: z.object({
    timeoutMinutes: z.number().min(15).max(480),
    maxConcurrentSessions: z.number().min(1).max(10),
    forceLogoutOnRoleChange: z.boolean(),
  }),
});

export default function SecuritySettingsPage() {
  return (
    <PermissionGate permission="security.configure">
      <div className="space-y-6">
        <PageHeader title="Security Settings" />

        {/* Copy Protection Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Shield className="h-5 w-5" />
              Copy Protection
            </CardTitle>
            <CardDescription>
              Control text selection and copying per role
            </CardDescription>
          </CardHeader>
          <CardContent>
            {/* Form fields for copy protection */}
          </CardContent>
        </Card>

        {/* Export Controls Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Download className="h-5 w-5" />
              Export Controls
            </CardTitle>
          </CardHeader>
          <CardContent>
            {/* Form fields for export controls */}
          </CardContent>
        </Card>

        {/* Data Masking Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Eye className="h-5 w-5" />
              Data Masking
            </CardTitle>
          </CardHeader>
          <CardContent>
            {/* Form fields for data masking */}
          </CardContent>
        </Card>

        {/* Role-Specific Settings */}
        <Card>
          <CardHeader>
            <CardTitle>Role Security Settings</CardTitle>
            <CardDescription>
              Configure security settings per role
            </CardDescription>
          </CardHeader>
          <CardContent>
            <RoleSecuritySettingsTable />
          </CardContent>
        </Card>
      </div>
    </PermissionGate>
  );
}
```

### Watermark Utility for Exports

```tsx
// lib/watermark.ts
interface WatermarkOptions {
  tenantName: string;
  userEmail: string;
  exportId: string;
  timestamp: Date;
  ipAddress?: string;
}

export function generateWatermarkText(options: WatermarkOptions): string {
  const lines = [
    `Exported by: ${options.userEmail}`,
    `Tenant: ${options.tenantName}`,
    `Export ID: ${options.exportId}`,
    `Date: ${options.timestamp.toISOString()}`,
  ];

  if (options.ipAddress) {
    lines.push(`IP: ${options.ipAddress}`);
  }

  return lines.join(' | ');
}

// For PDF exports (using jsPDF)
export function addPdfWatermark(doc: jsPDF, watermarkText: string) {
  const pageCount = doc.getNumberOfPages();

  for (let i = 1; i <= pageCount; i++) {
    doc.setPage(i);
    doc.setTextColor(200, 200, 200);
    doc.setFontSize(10);
    doc.text(watermarkText, doc.internal.pageSize.width / 2, 10, {
      align: 'center',
    });

    // Diagonal watermark
    doc.setFontSize(40);
    doc.setTextColor(240, 240, 240);
    doc.text(watermarkText, doc.internal.pageSize.width / 2,
             doc.internal.pageSize.height / 2, {
      align: 'center',
      angle: 45,
    });
  }
}
```

### Security Styles (CSS)

```css
/* styles/security.css */

/* Text selection protection - applied via class */
.select-none {
  -webkit-user-select: none;
  -moz-user-select: none;
  -ms-user-select: none;
  user-select: none;
}

/* Protected data styling */
[data-protected] {
  -webkit-touch-callout: none;
}

[data-protected][data-sensitivity="high"] {
  filter: blur(0);
  transition: filter 0.2s;
}

/* Print protection */
@media print {
  .print-protected [data-protected] {
    visibility: hidden !important;
  }

  .print-protected [data-protected]::after {
    content: '[RESTRICTED]';
    visibility: visible;
    color: #999;
    font-style: italic;
  }

  .no-print {
    display: none !important;
  }
}

/* Watermark overlay for screenshots (optional) */
.watermark-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  pointer-events: none;
  z-index: 9999;
  opacity: 0.03;
  font-size: 24px;
  color: #000;
  transform: rotate(-45deg);
  display: flex;
  align-items: center;
  justify-content: center;
}
```

---

## Next Steps

See the following documents for more details:
- [Feature Flags & RBAC](05-feature-flags-rbac.md)
- [API Design](08-api-design.md)
- [Mobile Readiness](09-mobile-readiness.md)
- [System Architecture - Security Section](01-system-architecture.md#data-protection--security-controls)
