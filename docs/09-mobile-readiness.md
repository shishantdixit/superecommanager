# Mobile Readiness Strategy

## Table of Contents
1. [Overview](#overview)
2. [Responsive Web Design](#responsive-web-design)
3. [API Design for Mobile](#api-design-for-mobile)
4. [Future Native App Strategy](#future-native-app-strategy)
5. [Offline Support](#offline-support)
6. [Push Notifications](#push-notifications)
7. [Performance Optimization](#performance-optimization)

---

## Overview

SuperEcomManager is designed with a **mobile-first** approach:

1. **Phase 1 (Current)**: Fully responsive web application
2. **Phase 2 (Future)**: Progressive Web App (PWA) capabilities
3. **Phase 3 (Future)**: Native mobile apps (React Native)

### Design Principles
- **Touch-friendly UI** - Minimum 44px touch targets
- **Thumb-zone navigation** - Important actions within easy reach
- **Offline-capable** - Critical features work without connection
- **Performance-focused** - Fast load times on mobile networks
- **API-first** - Same backend APIs power web and mobile

---

## Responsive Web Design

### Breakpoint Strategy

```tsx
// tailwind.config.js
module.exports = {
  theme: {
    screens: {
      'xs': '375px',   // Small phones
      'sm': '640px',   // Large phones / landscape
      'md': '768px',   // Tablets
      'lg': '1024px',  // Desktop
      'xl': '1280px',  // Large desktop
      '2xl': '1536px', // Extra large
    },
  },
};
```

### Layout Patterns

#### Pattern 1: Stack to Grid

```tsx
// Mobile: Stacked cards
// Desktop: Grid layout
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
  <StatsCard title="Orders" value={125} />
  <StatsCard title="Shipments" value={98} />
  <StatsCard title="NDR" value={12} />
  <StatsCard title="Revenue" value="₹1.2L" />
</div>
```

#### Pattern 2: Table to Cards

```tsx
// components/features/orders/order-list.tsx
export function OrderList({ orders }: { orders: Order[] }) {
  const { isMobile } = useMobile();

  if (isMobile) {
    return (
      <div className="space-y-3">
        {orders.map((order) => (
          <OrderCard key={order.id} order={order} />
        ))}
      </div>
    );
  }

  return <OrderTable orders={orders} />;
}
```

#### Pattern 3: Sidebar to Bottom Sheet

```tsx
// components/layout/sidebar/sidebar.tsx
export function Sidebar() {
  const { isMobile } = useMobile();
  const [isOpen, setIsOpen] = useState(false);

  if (isMobile) {
    return (
      <>
        {/* Mobile: Bottom navigation bar */}
        <nav className="fixed bottom-0 left-0 right-0 bg-white border-t z-50">
          <div className="flex justify-around py-2">
            <NavItem href="/" icon={Home} label="Home" />
            <NavItem href="/orders" icon={ShoppingCart} label="Orders" />
            <NavItem href="/ndr" icon={AlertCircle} label="NDR" />
            <NavItem href="/more" icon={Menu} label="More" onClick={() => setIsOpen(true)} />
          </div>
        </nav>

        {/* Full menu in sheet */}
        <Sheet open={isOpen} onOpenChange={setIsOpen}>
          <SheetContent side="bottom" className="h-[80vh]">
            <FullNavMenu onClose={() => setIsOpen(false)} />
          </SheetContent>
        </Sheet>
      </>
    );
  }

  // Desktop: Traditional sidebar
  return (
    <aside className="w-64 border-r bg-white h-screen">
      <SidebarNav />
    </aside>
  );
}
```

### Mobile-Specific Components

#### Mobile Order Card

```tsx
// components/features/orders/order-card.tsx
export function OrderCard({ order }: { order: Order }) {
  return (
    <Card className="p-4">
      <div className="flex justify-between items-start">
        <div>
          <p className="font-medium">{order.orderNumber}</p>
          <p className="text-sm text-muted-foreground">{order.customerName}</p>
        </div>
        <OrderStatusBadge status={order.status} />
      </div>

      <div className="mt-3 flex justify-between items-center">
        <div>
          <p className="text-lg font-semibold">₹{order.totalAmount}</p>
          <p className="text-xs text-muted-foreground">
            {formatDate(order.orderDate)}
          </p>
        </div>

        <div className="flex gap-2">
          <Button size="sm" variant="outline" asChild>
            <Link href={`/orders/${order.id}`}>View</Link>
          </Button>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button size="sm" variant="ghost">
                <MoreVertical className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem>Create Shipment</DropdownMenuItem>
              <DropdownMenuItem>Cancel Order</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </Card>
  );
}
```

#### Mobile NDR Action Panel

```tsx
// components/features/ndr/ndr-action-panel-mobile.tsx
export function NdrActionPanelMobile({ ndr, onAction }: Props) {
  const [activeAction, setActiveAction] = useState<string | null>(null);

  return (
    <div className="fixed bottom-0 left-0 right-0 bg-white border-t p-4 z-40">
      {/* Quick action buttons */}
      <div className="flex justify-around gap-2">
        <ActionButton
          icon={Phone}
          label="Call"
          onClick={() => setActiveAction('call')}
        />
        <ActionButton
          icon={MessageCircle}
          label="WhatsApp"
          onClick={() => setActiveAction('whatsapp')}
        />
        <ActionButton
          icon={MessageSquare}
          label="SMS"
          onClick={() => setActiveAction('sms')}
        />
        <ActionButton
          icon={RotateCcw}
          label="Reattempt"
          onClick={() => setActiveAction('reattempt')}
        />
      </div>

      {/* Action sheets */}
      <Sheet open={activeAction === 'call'} onOpenChange={() => setActiveAction(null)}>
        <SheetContent side="bottom">
          <NdrCallForm ndr={ndr} onSubmit={onAction} />
        </SheetContent>
      </Sheet>

      <Sheet open={activeAction === 'whatsapp'} onOpenChange={() => setActiveAction(null)}>
        <SheetContent side="bottom">
          <NdrWhatsAppForm ndr={ndr} onSubmit={onAction} />
        </SheetContent>
      </Sheet>

      {/* ... other sheets */}
    </div>
  );
}
```

### Touch Interactions

```tsx
// hooks/use-swipe.ts
export function useSwipe(onSwipeLeft?: () => void, onSwipeRight?: () => void) {
  const [touchStart, setTouchStart] = useState<number | null>(null);
  const [touchEnd, setTouchEnd] = useState<number | null>(null);

  const minSwipeDistance = 50;

  const onTouchStart = (e: TouchEvent) => {
    setTouchEnd(null);
    setTouchStart(e.targetTouches[0].clientX);
  };

  const onTouchMove = (e: TouchEvent) => {
    setTouchEnd(e.targetTouches[0].clientX);
  };

  const onTouchEnd = () => {
    if (!touchStart || !touchEnd) return;

    const distance = touchStart - touchEnd;
    const isLeftSwipe = distance > minSwipeDistance;
    const isRightSwipe = distance < -minSwipeDistance;

    if (isLeftSwipe && onSwipeLeft) onSwipeLeft();
    if (isRightSwipe && onSwipeRight) onSwipeRight();
  };

  return { onTouchStart, onTouchMove, onTouchEnd };
}

// Usage: Swipeable order card
export function SwipeableOrderCard({ order, onArchive, onShip }: Props) {
  const swipeHandlers = useSwipe(
    () => onArchive(order.id),  // Swipe left to archive
    () => onShip(order.id)      // Swipe right to ship
  );

  return (
    <div {...swipeHandlers}>
      <OrderCard order={order} />
    </div>
  );
}
```

---

## API Design for Mobile

### Mobile-Optimized Endpoints

```csharp
// API/Controllers/V1/MobileController.cs
[ApiController]
[Route("api/v1/mobile")]
[Authorize]
public class MobileController : ControllerBase
{
    /// <summary>
    /// Combined dashboard data for mobile home screen
    /// Reduces multiple API calls to single request
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetMobileDashboard()
    {
        var result = await _mediator.Send(new GetMobileDashboardQuery());
        return Ok(result);
    }

    /// <summary>
    /// Quick actions data (counts, pending items)
    /// </summary>
    [HttpGet("quick-stats")]
    public async Task<IActionResult> GetQuickStats()
    {
        var result = await _mediator.Send(new GetQuickStatsQuery());
        return Ok(result);
    }
}
```

### Efficient Data Loading

```csharp
// Mobile dashboard returns combined data
public class MobileDashboardDto
{
    public DashboardStats Stats { get; set; }
    public List<OrderSummaryDto> RecentOrders { get; set; }  // Last 5 orders
    public List<NdrSummaryDto> PendingNdrs { get; set; }     // Urgent NDRs
    public List<NotificationDto> Notifications { get; set; } // Unread notifications
    public QuickActions QuickActions { get; set; }
}

public class QuickActions
{
    public int PendingOrdersCount { get; set; }
    public int PendingShipmentsCount { get; set; }
    public int OpenNdrsCount { get; set; }
    public int LowStockCount { get; set; }
}
```

### Pagination for Mobile

```csharp
// Smaller page sizes for mobile
public class MobileListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;  // Smaller than web default of 20
    public bool InfiniteScroll { get; set; } = true;  // Return cursor for infinite scroll
}

// Response with cursor for infinite scroll
public class MobileListResponse<T>
{
    public List<T> Items { get; set; }
    public string? NextCursor { get; set; }  // For infinite scroll
    public bool HasMore { get; set; }
}
```

### Image Optimization

```csharp
// API returns responsive image URLs
public class ProductImageDto
{
    public string Thumbnail { get; set; }  // 100x100
    public string Small { get; set; }      // 300x300
    public string Medium { get; set; }     // 600x600
    public string Large { get; set; }      // 1200x1200
}

// Client selects appropriate size
const imageUrl = isMobile ? product.images.small : product.images.large;
```

---

## Future Native App Strategy

### React Native Architecture

```
mobile-app/
├── src/
│   ├── api/                    # API client (shared with web)
│   ├── components/
│   │   ├── ui/                 # React Native components
│   │   └── features/           # Feature components
│   ├── navigation/
│   │   ├── RootNavigator.tsx
│   │   ├── TabNavigator.tsx
│   │   └── AuthNavigator.tsx
│   ├── screens/
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── orders/
│   │   ├── ndr/
│   │   └── settings/
│   ├── hooks/                  # Custom hooks
│   ├── store/                  # Zustand stores (shared)
│   ├── services/               # Native services
│   └── utils/                  # Utilities
├── android/
├── ios/
└── package.json
```

### Shared Code Strategy

```
// Shared between web and mobile
packages/
├── api-client/          # API service layer
├── types/               # TypeScript types
├── store/               # Zustand stores
└── utils/               # Common utilities

// Platform-specific
apps/
├── web/                 # Next.js web app
└── mobile/              # React Native app
```

### Native Features

| Feature | Implementation |
|---------|----------------|
| Push Notifications | Firebase Cloud Messaging |
| Biometric Auth | Expo Local Authentication |
| Camera (Barcode) | Expo Camera |
| Offline Storage | MMKV / WatermelonDB |
| Background Sync | React Native Background Fetch |
| Deep Linking | React Navigation Deep Links |

---

## Offline Support

### Service Worker (PWA)

```typescript
// service-worker.ts
const CACHE_NAME = 'superecom-v1';
const OFFLINE_URLS = [
  '/',
  '/offline',
  '/manifest.json',
  '/icons/icon-192.png',
];

// Cache critical assets
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(OFFLINE_URLS))
  );
});

// Network-first with cache fallback
self.addEventListener('fetch', (event) => {
  if (event.request.method !== 'GET') return;

  event.respondWith(
    fetch(event.request)
      .then((response) => {
        // Cache successful responses
        if (response.ok) {
          const clone = response.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, clone);
          });
        }
        return response;
      })
      .catch(() => {
        // Return cached version or offline page
        return caches.match(event.request).then((cached) => {
          return cached || caches.match('/offline');
        });
      })
  );
});
```

### Offline Data Queue

```typescript
// lib/offline-queue.ts
interface QueuedAction {
  id: string;
  type: string;
  payload: unknown;
  timestamp: number;
  retries: number;
}

class OfflineQueue {
  private queue: QueuedAction[] = [];
  private readonly STORAGE_KEY = 'offline_queue';

  constructor() {
    this.loadFromStorage();
    this.setupNetworkListener();
  }

  async add(type: string, payload: unknown) {
    const action: QueuedAction = {
      id: crypto.randomUUID(),
      type,
      payload,
      timestamp: Date.now(),
      retries: 0,
    };

    this.queue.push(action);
    this.saveToStorage();

    // Try to process immediately if online
    if (navigator.onLine) {
      await this.processQueue();
    }
  }

  private async processQueue() {
    const pending = [...this.queue];

    for (const action of pending) {
      try {
        await this.executeAction(action);
        this.removeFromQueue(action.id);
      } catch (error) {
        action.retries++;
        if (action.retries >= 3) {
          // Move to failed queue
          this.removeFromQueue(action.id);
        }
      }
    }

    this.saveToStorage();
  }

  private setupNetworkListener() {
    window.addEventListener('online', () => this.processQueue());
  }
}

export const offlineQueue = new OfflineQueue();
```

### Optimistic Updates

```typescript
// hooks/queries/use-ndr.ts
export function useAddNdrAction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: AddNdrActionInput) => ndrService.addAction(data),

    // Optimistic update
    onMutate: async (newAction) => {
      await queryClient.cancelQueries({ queryKey: ['ndr', newAction.ndrId] });

      const previousNdr = queryClient.getQueryData(['ndr', newAction.ndrId]);

      // Optimistically add action
      queryClient.setQueryData(['ndr', newAction.ndrId], (old: Ndr) => ({
        ...old,
        actions: [
          ...old.actions,
          {
            id: 'temp-' + Date.now(),
            ...newAction,
            performedAt: new Date().toISOString(),
            isPending: true,
          },
        ],
      }));

      return { previousNdr };
    },

    // Rollback on error
    onError: (err, newAction, context) => {
      queryClient.setQueryData(
        ['ndr', newAction.ndrId],
        context?.previousNdr
      );
    },

    // Refetch on success
    onSettled: (data, error, variables) => {
      queryClient.invalidateQueries({ queryKey: ['ndr', variables.ndrId] });
    },
  });
}
```

---

## Push Notifications

### Web Push Setup

```typescript
// lib/push-notifications.ts
export async function subscribeToPush() {
  if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
    console.log('Push notifications not supported');
    return null;
  }

  const registration = await navigator.serviceWorker.ready;

  const subscription = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(process.env.NEXT_PUBLIC_VAPID_KEY!),
  });

  // Send subscription to backend
  await fetch('/api/push/subscribe', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(subscription),
  });

  return subscription;
}
```

### Notification Types

```typescript
interface PushNotification {
  type: 'order' | 'shipment' | 'ndr' | 'system';
  title: string;
  body: string;
  data: {
    entityId: string;
    entityType: string;
    action?: string;
  };
}

// Examples:
// - New order received
// - Shipment delivered
// - NDR requires attention
// - Low stock alert
```

---

## Performance Optimization

### Image Loading

```tsx
// components/ui/optimized-image.tsx
import Image from 'next/image';

export function OptimizedImage({
  src,
  alt,
  sizes = '100vw',
  priority = false,
}: Props) {
  return (
    <Image
      src={src}
      alt={alt}
      fill
      sizes={sizes}
      priority={priority}
      placeholder="blur"
      blurDataURL="data:image/jpeg;base64,/9j/4AAQSkZJRg..."
      className="object-cover"
    />
  );
}

// Usage with responsive sizes
<OptimizedImage
  src={product.image}
  alt={product.name}
  sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
/>
```

### Code Splitting

```tsx
// Lazy load heavy components
const NdrAnalytics = dynamic(
  () => import('@/components/features/ndr/ndr-analytics'),
  {
    loading: () => <Skeleton className="h-96" />,
    ssr: false, // Client-only
  }
);

const FinanceReports = dynamic(
  () => import('@/components/features/finance/reports'),
  { loading: () => <Skeleton className="h-96" /> }
);
```

### Bundle Analysis

```json
// package.json
{
  "scripts": {
    "analyze": "ANALYZE=true npm run build"
  }
}
```

```javascript
// next.config.js
const withBundleAnalyzer = require('@next/bundle-analyzer')({
  enabled: process.env.ANALYZE === 'true',
});

module.exports = withBundleAnalyzer({
  // config
});
```

### Performance Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| First Contentful Paint | < 1.5s | Lighthouse |
| Time to Interactive | < 3s | Lighthouse |
| Largest Contentful Paint | < 2.5s | Lighthouse |
| Cumulative Layout Shift | < 0.1 | Lighthouse |
| First Input Delay | < 100ms | Core Web Vitals |

---

## Next Steps

See the following documents for more details:
- [Development Roadmap](10-development-roadmap.md)
- [Future Enhancements](11-future-enhancements.md)
