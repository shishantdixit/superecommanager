'use client';

import { useState } from 'react';
import Link from 'next/link';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Select,
  Badge,
  SectionLoader,
} from '@/components/ui';
import { formatCurrency, formatDate } from '@/lib/utils';
import {
  useRevenueTrends,
  useOrderTrends,
  useDeliveryPerformance,
  useCourierComparison,
  useNdrAnalytics,
} from '@/hooks';
import type { AnalyticsFilter, AnalyticsPeriod } from '@/services/analytics.service';
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  ShoppingCart,
  Truck,
  AlertTriangle,
  ArrowUpRight,
  ArrowDownRight,
  Package,
  Clock,
  CheckCircle,
  RotateCcw,
  BarChart3,
  PieChart,
  LineChart,
  Award,
} from 'lucide-react';

const periodOptions = [
  { value: 'Today', label: 'Today' },
  { value: 'Yesterday', label: 'Yesterday' },
  { value: 'Last7Days', label: 'Last 7 Days' },
  { value: 'Last30Days', label: 'Last 30 Days' },
  { value: 'ThisMonth', label: 'This Month' },
  { value: 'LastMonth', label: 'Last Month' },
  { value: 'ThisQuarter', label: 'This Quarter' },
  { value: 'ThisYear', label: 'This Year' },
];

export default function AnalyticsPage() {
  const [filters, setFilters] = useState<AnalyticsFilter>({
    period: 'Last30Days',
  });

  const { data: revenue, isLoading: revenueLoading } = useRevenueTrends(filters);
  const { data: orders, isLoading: ordersLoading } = useOrderTrends(filters);
  const { data: delivery, isLoading: deliveryLoading } = useDeliveryPerformance(filters);
  const { data: couriers, isLoading: couriersLoading } = useCourierComparison(filters);
  const { data: ndr, isLoading: ndrLoading } = useNdrAnalytics(filters);

  const isLoading = revenueLoading || ordersLoading || deliveryLoading || couriersLoading || ndrLoading;

  return (
    <DashboardLayout title="Analytics">
      {/* Period Selector */}
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-2xl font-bold">Analytics & Reports</h1>
        <div className="flex items-center gap-2">
          <Select
            options={periodOptions}
            value={filters.period || 'Last30Days'}
            onChange={(e) => setFilters({ period: e.target.value as AnalyticsPeriod })}
            className="w-40"
          />
          <Button variant="outline" leftIcon={<BarChart3 className="h-4 w-4" />}>
            Export Report
          </Button>
        </div>
      </div>

      {isLoading ? (
        <SectionLoader />
      ) : (
        <>
          {/* Key Metrics */}
          <div className="mb-6 grid gap-4 md:grid-cols-4">
            <MetricCard
              label="Total Revenue"
              value={formatCurrency(revenue?.totalRevenue ?? 0)}
              change={revenue?.percentageChange ?? 0}
              icon={<DollarSign className="h-5 w-5 text-success" />}
            />
            <MetricCard
              label="Total Orders"
              value={(orders?.totalOrders ?? 0).toLocaleString()}
              change={orders?.percentageChange ?? 0}
              icon={<ShoppingCart className="h-5 w-5 text-primary" />}
            />
            <MetricCard
              label="Delivery Rate"
              value={`${(delivery?.deliveryRate ?? 0).toFixed(1)}%`}
              subtext={`${delivery?.deliveredCount ?? 0} delivered`}
              icon={<Truck className="h-5 w-5 text-info" />}
            />
            <MetricCard
              label="NDR Resolution"
              value={`${(ndr?.resolutionRate ?? 0).toFixed(1)}%`}
              subtext={`${ndr?.resolvedCount ?? 0} resolved`}
              icon={<CheckCircle className="h-5 w-5 text-warning" />}
            />
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            {/* Revenue Chart */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <LineChart className="h-5 w-5" />
                  Revenue Trend
                </CardTitle>
              </CardHeader>
              <CardContent>
                {revenue?.dailyRevenue && revenue.dailyRevenue.length > 0 ? (
                  <div className="space-y-4">
                    <div className="flex items-center justify-between text-sm">
                      <span>Avg Order Value: {formatCurrency(revenue.averageOrderValue)}</span>
                      <span className="text-muted-foreground">
                        Previous: {formatCurrency(revenue.previousAverageOrderValue)}
                      </span>
                    </div>
                    <div className="h-48 flex items-end gap-1">
                      {revenue.dailyRevenue.slice(-14).map((day, i) => {
                        const maxRevenue = Math.max(
                          ...revenue.dailyRevenue.slice(-14).map((d) => d.revenue)
                        );
                        const height = maxRevenue > 0 ? (day.revenue / maxRevenue) * 100 : 0;
                        return (
                          <div
                            key={i}
                            className="flex-1 bg-primary/20 hover:bg-primary/40 rounded-t transition-colors group relative"
                            style={{ height: `${Math.max(height, 5)}%` }}
                          >
                            <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-1 hidden group-hover:block bg-popover border rounded px-2 py-1 text-xs whitespace-nowrap z-10">
                              {formatCurrency(day.revenue)}
                              <br />
                              {formatDate(day.date)}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                ) : (
                  <p className="text-center text-muted-foreground py-12">No revenue data</p>
                )}
              </CardContent>
            </Card>

            {/* Orders by Status */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <PieChart className="h-5 w-5" />
                  Orders by Status
                </CardTitle>
              </CardHeader>
              <CardContent>
                {orders?.ordersByStatus && orders.ordersByStatus.length > 0 ? (
                  <div className="space-y-3">
                    {orders.ordersByStatus.map((status) => (
                      <div key={status.status}>
                        <div className="flex items-center justify-between mb-1">
                          <span className="font-medium">{status.status}</span>
                          <span>
                            {status.count.toLocaleString()} ({status.percentage.toFixed(1)}%)
                          </span>
                        </div>
                        <div className="h-2 rounded-full bg-muted overflow-hidden">
                          <div
                            className={`h-full rounded-full ${getStatusColor(status.status)}`}
                            style={{ width: `${status.percentage}%` }}
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-center text-muted-foreground py-12">No order data</p>
                )}
              </CardContent>
            </Card>

            {/* Revenue by Channel */}
            <Card>
              <CardHeader>
                <CardTitle>Revenue by Channel</CardTitle>
              </CardHeader>
              <CardContent>
                {revenue?.revenueByChannel && revenue.revenueByChannel.length > 0 ? (
                  <div className="space-y-4">
                    {revenue.revenueByChannel.map((channel) => (
                      <div
                        key={channel.channelName}
                        className="flex items-center justify-between"
                      >
                        <div className="flex items-center gap-3">
                          <ChannelBadge channel={channel.channelType} />
                          <div>
                            <p className="font-medium">{channel.channelName}</p>
                            <p className="text-xs text-muted-foreground">
                              {channel.orderCount} orders
                            </p>
                          </div>
                        </div>
                        <div className="text-right">
                          <p className="font-bold">{formatCurrency(channel.revenue)}</p>
                          <p className="text-xs text-muted-foreground">
                            {channel.percentage.toFixed(1)}%
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-center text-muted-foreground py-12">No channel data</p>
                )}
              </CardContent>
            </Card>

            {/* Delivery Performance */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Truck className="h-5 w-5" />
                  Delivery Performance
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-3 gap-4 mb-6">
                  <div className="text-center p-3 rounded-lg bg-success/10">
                    <p className="text-2xl font-bold text-success">
                      {delivery?.deliveredCount ?? 0}
                    </p>
                    <p className="text-xs text-muted-foreground">Delivered</p>
                  </div>
                  <div className="text-center p-3 rounded-lg bg-warning/10">
                    <p className="text-2xl font-bold text-warning">
                      {delivery?.inTransitCount ?? 0}
                    </p>
                    <p className="text-xs text-muted-foreground">In Transit</p>
                  </div>
                  <div className="text-center p-3 rounded-lg bg-error/10">
                    <p className="text-2xl font-bold text-error">{delivery?.rtoCount ?? 0}</p>
                    <p className="text-xs text-muted-foreground">RTO</p>
                  </div>
                </div>

                {delivery?.deliveryTimeDistribution &&
                  delivery.deliveryTimeDistribution.length > 0 && (
                    <div>
                      <h4 className="font-medium mb-3">Delivery Time Distribution</h4>
                      <div className="space-y-2">
                        {delivery.deliveryTimeDistribution.map((dist) => (
                          <div key={dist.range} className="flex items-center gap-3">
                            <span className="w-24 text-sm">{dist.range}</span>
                            <div className="flex-1 h-3 rounded-full bg-muted overflow-hidden">
                              <div
                                className="h-full bg-info rounded-full"
                                style={{ width: `${dist.percentage}%` }}
                              />
                            </div>
                            <span className="text-sm text-muted-foreground w-12 text-right">
                              {dist.count}
                            </span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
              </CardContent>
            </Card>

            {/* Courier Comparison */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Award className="h-5 w-5" />
                  Courier Performance
                </CardTitle>
              </CardHeader>
              <CardContent>
                {couriers?.couriers && couriers.couriers.length > 0 ? (
                  <div className="space-y-4">
                    {couriers.couriers.slice(0, 5).map((courier) => (
                      <div key={courier.courierId} className="rounded-lg border p-3">
                        <div className="flex items-center justify-between mb-2">
                          <span className="font-medium">{courier.courierName}</span>
                          <span className="text-sm text-muted-foreground">
                            {courier.totalShipments} shipments
                          </span>
                        </div>
                        <div className="grid grid-cols-4 gap-2 text-center text-sm">
                          <div>
                            <p className="text-success font-medium">
                              {courier.deliveryRate.toFixed(1)}%
                            </p>
                            <p className="text-xs text-muted-foreground">Delivery</p>
                          </div>
                          <div>
                            <p className="text-error font-medium">
                              {courier.rtoRate.toFixed(1)}%
                            </p>
                            <p className="text-xs text-muted-foreground">RTO</p>
                          </div>
                          <div>
                            <p className="font-medium">
                              {courier.averageDeliveryDays.toFixed(1)}d
                            </p>
                            <p className="text-xs text-muted-foreground">Avg Days</p>
                          </div>
                          <div>
                            <p className="font-medium">
                              {formatCurrency(courier.averageCost)}
                            </p>
                            <p className="text-xs text-muted-foreground">Avg Cost</p>
                          </div>
                        </div>
                        {courier.courierName === couriers.bestDeliveryRateCourierName && (
                          <Badge variant="success" size="sm" className="mt-2">
                            Best Delivery Rate
                          </Badge>
                        )}
                        {courier.courierName === couriers.fastestDeliveryCourierName && (
                          <Badge variant="info" size="sm" className="mt-2 ml-1">
                            Fastest
                          </Badge>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-center text-muted-foreground py-12">No courier data</p>
                )}
              </CardContent>
            </Card>

            {/* NDR Analytics */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <AlertTriangle className="h-5 w-5" />
                  NDR Analytics
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-3 gap-4 mb-6">
                  <div className="text-center p-3 rounded-lg bg-muted">
                    <p className="text-2xl font-bold">{ndr?.totalNdrCases ?? 0}</p>
                    <p className="text-xs text-muted-foreground">Total Cases</p>
                  </div>
                  <div className="text-center p-3 rounded-lg bg-success/10">
                    <p className="text-2xl font-bold text-success">
                      {ndr?.resolvedCount ?? 0}
                    </p>
                    <p className="text-xs text-muted-foreground">Resolved</p>
                  </div>
                  <div className="text-center p-3 rounded-lg bg-warning/10">
                    <p className="text-2xl font-bold text-warning">
                      {ndr?.pendingCount ?? 0}
                    </p>
                    <p className="text-xs text-muted-foreground">Pending</p>
                  </div>
                </div>

                {ndr?.byReason && ndr.byReason.length > 0 && (
                  <div>
                    <h4 className="font-medium mb-3">Top NDR Reasons</h4>
                    <div className="space-y-2">
                      {ndr.byReason.slice(0, 5).map((reason) => (
                        <div
                          key={reason.reasonCode}
                          className="flex items-center justify-between"
                        >
                          <span className="text-sm">{reason.reasonDescription}</span>
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-medium">{reason.count}</span>
                            <span className="text-xs text-muted-foreground">
                              ({reason.resolutionRate.toFixed(0)}% resolved)
                            </span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Peak Hours */}
          {orders?.ordersByHour && orders.ordersByHour.length > 0 && (
            <Card className="mt-6">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Clock className="h-5 w-5" />
                  Orders by Hour of Day
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex items-end gap-1 h-32">
                  {orders.ordersByHour.map((hour) => {
                    const maxOrders = Math.max(...orders.ordersByHour.map((h) => h.orderCount));
                    const height = maxOrders > 0 ? (hour.orderCount / maxOrders) * 100 : 0;
                    const isPeak = hour.hour === orders.peakHour;
                    return (
                      <div key={hour.hour} className="flex-1 flex flex-col items-center">
                        <div
                          className={`w-full rounded-t transition-colors ${
                            isPeak ? 'bg-primary' : 'bg-primary/30'
                          }`}
                          style={{ height: `${Math.max(height, 5)}%` }}
                        />
                        <span className="text-xs text-muted-foreground mt-1">
                          {hour.hour.toString().padStart(2, '0')}
                        </span>
                      </div>
                    );
                  })}
                </div>
                <div className="flex justify-between mt-4 text-sm">
                  <div>
                    <span className="text-muted-foreground">Peak Hour: </span>
                    <span className="font-medium">{orders.peakHour}:00</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Peak Day: </span>
                    <span className="font-medium">{orders.peakDay}</span>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Avg/Day: </span>
                    <span className="font-medium">
                      {orders.averageOrdersPerDay.toFixed(1)} orders
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </DashboardLayout>
  );
}

function MetricCard({
  label,
  value,
  change,
  subtext,
  icon,
}: {
  label: string;
  value: string;
  change?: number;
  subtext?: string;
  icon: React.ReactNode;
}) {
  const isPositive = (change ?? 0) >= 0;

  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-muted-foreground">{label}</p>
            <p className="text-2xl font-bold">{value}</p>
            {change !== undefined && (
              <div
                className={`flex items-center gap-1 text-sm ${
                  isPositive ? 'text-success' : 'text-error'
                }`}
              >
                {isPositive ? (
                  <ArrowUpRight className="h-4 w-4" />
                ) : (
                  <ArrowDownRight className="h-4 w-4" />
                )}
                {Math.abs(change).toFixed(1)}% vs prev
              </div>
            )}
            {subtext && <p className="text-xs text-muted-foreground">{subtext}</p>}
          </div>
          {icon}
        </div>
      </CardContent>
    </Card>
  );
}

function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    Pending: 'bg-warning',
    Confirmed: 'bg-info',
    Processing: 'bg-info',
    Shipped: 'bg-primary',
    Delivered: 'bg-success',
    Cancelled: 'bg-error',
    Returned: 'bg-muted-foreground',
    RTO: 'bg-error',
  };
  return colors[status] || 'bg-muted-foreground';
}

function ChannelBadge({ channel }: { channel: string }) {
  const colors: Record<string, string> = {
    Shopify: 'bg-green-100 text-green-700',
    Amazon: 'bg-orange-100 text-orange-700',
    Flipkart: 'bg-yellow-100 text-yellow-700',
    Meesho: 'bg-pink-100 text-pink-700',
  };

  return (
    <span
      className={`inline-flex h-8 w-8 items-center justify-center rounded-full text-xs font-medium ${
        colors[channel] || 'bg-gray-100 text-gray-700'
      }`}
    >
      {channel[0]}
    </span>
  );
}
