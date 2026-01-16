'use client';

import { DashboardLayout } from '@/components/layout';
import { Card, CardContent, CardHeader, CardTitle, Badge, SectionLoader } from '@/components/ui';
import { formatCurrency } from '@/lib/utils';
import { useDashboardOverview, useDashboardOrders, useDashboardAlerts } from '@/hooks';
import {
  Package,
  Truck,
  AlertTriangle,
  IndianRupee,
  TrendingUp,
  TrendingDown,
  Clock,
  CheckCircle,
} from 'lucide-react';

export default function DashboardPage() {
  const { data: overview, isLoading: isLoadingOverview } = useDashboardOverview('7days');
  const { data: ordersData, isLoading: isLoadingOrders } = useDashboardOrders('7days');
  const { data: alertsData } = useDashboardAlerts();

  if (isLoadingOverview) {
    return (
      <DashboardLayout title="Dashboard">
        <SectionLoader />
      </DashboardLayout>
    );
  }

  // Use API data or fallback to defaults (handles both null and error cases)
  const stats = {
    totalOrders: overview?.totalOrders ?? 0,
    totalRevenue: overview?.totalRevenue ?? 0,
    pendingOrders: overview?.pendingOrders ?? 0,
    activeShipments: overview?.activeShipments ?? 0,
    inTransit: overview?.inTransit ?? 0,
    deliveredToday: overview?.deliveredToday ?? 0,
    openNdrCases: overview?.openNdrCases ?? 0,
    lowStockItems: overview?.lowStockItems ?? 0,
  };

  const recentOrders = ordersData?.recentOrders || [];
  const alerts = alertsData?.alerts || [];

  return (
    <DashboardLayout title="Dashboard">
      {/* Stats Grid */}
      <div className="mb-6 grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Total Revenue */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total Revenue</p>
                <p className="text-2xl font-bold">{formatCurrency(stats.totalRevenue)}</p>
                <p className="mt-1 flex items-center gap-1 text-xs text-success">
                  <TrendingUp className="h-3 w-3" />
                  +12.5% from last month
                </p>
              </div>
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
                <IndianRupee className="h-6 w-6 text-primary" />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Total Orders */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Total Orders</p>
                <p className="text-2xl font-bold">{stats.totalOrders.toLocaleString()}</p>
                <p className="mt-1 flex items-center gap-1 text-xs text-success">
                  <TrendingUp className="h-3 w-3" />
                  +8.2% from last month
                </p>
              </div>
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-info/10">
                <Package className="h-6 w-6 text-info" />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* In Transit */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">In Transit</p>
                <p className="text-2xl font-bold">{stats.inTransit}</p>
                <p className="mt-1 text-xs text-muted-foreground">
                  {stats.pendingOrders} pending pickup
                </p>
              </div>
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-warning/10">
                <Truck className="h-6 w-6 text-warning" />
              </div>
            </div>
          </CardContent>
        </Card>

        {/* NDR Cases */}
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-muted-foreground">Open NDR Cases</p>
                <p className="text-2xl font-bold">{stats.openNdrCases}</p>
                <p className="mt-1 flex items-center gap-1 text-xs text-error">
                  <TrendingDown className="h-3 w-3" />
                  Needs attention
                </p>
              </div>
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-error/10">
                <AlertTriangle className="h-6 w-6 text-error" />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Secondary Stats */}
      <div className="mb-6 grid gap-4 md:grid-cols-3">
        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-success/10">
              <CheckCircle className="h-5 w-5 text-success" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Delivered Today</p>
              <p className="text-xl font-semibold">{stats.deliveredToday}</p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-info/10">
              <Clock className="h-5 w-5 text-info" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Avg. Delivery Time</p>
              <p className="text-xl font-semibold">3.2 days</p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="flex items-center gap-4 p-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10">
              <Package className="h-5 w-5 text-primary" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Low Stock Items</p>
              <p className="text-xl font-semibold">{stats.lowStockItems}</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Recent Orders */}
      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Recent Orders</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoadingOrders ? (
              <SectionLoader className="py-8" />
            ) : recentOrders.length > 0 ? (
              <div className="space-y-4">
                {recentOrders.map((order) => (
                  <div
                    key={order.id}
                    className="flex items-center justify-between rounded-lg border border-border p-3"
                  >
                    <div className="flex-1">
                      <p className="font-medium">{order.orderNumber}</p>
                      <p className="text-sm text-muted-foreground">{order.customerName}</p>
                    </div>
                    <div className="text-right">
                      <p className="font-medium">{formatCurrency(order.total)}</p>
                      <StatusBadge status={order.status} />
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-center text-muted-foreground py-8">No recent orders</p>
            )}
          </CardContent>
        </Card>

        {/* Quick Actions */}
        <Card>
          <CardHeader>
            <CardTitle>Quick Actions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-3 sm:grid-cols-2">
              <ActionButton
                href="/orders/new"
                icon={<Package className="h-5 w-5" />}
                label="Create Order"
              />
              <ActionButton
                href="/shipments/create"
                icon={<Truck className="h-5 w-5" />}
                label="Create Shipment"
              />
              <ActionButton
                href="/ndr"
                icon={<AlertTriangle className="h-5 w-5" />}
                label="View NDR Cases"
              />
              <ActionButton
                href="/inventory"
                icon={<Package className="h-5 w-5" />}
                label="Manage Inventory"
              />
            </div>

            {/* Alerts */}
            <div className="mt-6 space-y-3">
              <h4 className="text-sm font-medium text-muted-foreground">Alerts</h4>
              {alerts.length > 0 ? (
                alerts.slice(0, 3).map((alert) => (
                  <AlertItem
                    key={alert.id}
                    type={alert.type}
                    message={alert.message}
                    link={alert.link || '#'}
                  />
                ))
              ) : (
                <>
                  <AlertItem
                    type="warning"
                    message={`${stats.lowStockItems} items are running low on stock`}
                    link="/inventory?filter=low-stock"
                  />
                  <AlertItem
                    type="error"
                    message={`${stats.openNdrCases} NDR cases need attention`}
                    link="/ndr?priority=high"
                  />
                </>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </DashboardLayout>
  );
}

function StatusBadge({ status }: { status: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default'> = {
    Delivered: 'success',
    Shipped: 'info',
    Processing: 'info',
    Pending: 'warning',
    NDR: 'error',
    Cancelled: 'error',
  };

  return (
    <Badge variant={variants[status] || 'default'} size="sm">
      {status}
    </Badge>
  );
}

function ActionButton({
  href,
  icon,
  label,
}: {
  href: string;
  icon: React.ReactNode;
  label: string;
}) {
  return (
    <a
      href={href}
      className="flex items-center gap-3 rounded-lg border border-border p-3 transition-colors hover:bg-muted"
    >
      <div className="text-muted-foreground">{icon}</div>
      <span className="text-sm font-medium">{label}</span>
    </a>
  );
}

function AlertItem({
  type,
  message,
  link,
}: {
  type: 'warning' | 'error' | 'info';
  message: string;
  link: string;
}) {
  const colors = {
    warning: 'bg-warning/10 text-warning border-warning/20',
    error: 'bg-error/10 text-error border-error/20',
    info: 'bg-info/10 text-info border-info/20',
  };

  return (
    <a
      href={link}
      className={`block rounded-lg border p-3 text-sm transition-opacity hover:opacity-80 ${colors[type]}`}
    >
      {message}
    </a>
  );
}
