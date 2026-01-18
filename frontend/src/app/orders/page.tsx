'use client';

import { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Select,
  Badge,
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableEmpty,
  Pagination,
  SectionLoader,
} from '@/components/ui';
import { formatCurrency, formatDateTime } from '@/lib/utils';
import { useOrders, useOrderStats, useChannels, useSyncChannel } from '@/hooks';
import type { OrderFilters, OrderStatus, PaymentStatus } from '@/types/api';
import { Search, Filter, Download, Plus, Eye, Truck, MoreHorizontal, Package, RefreshCw, ChevronDown, Loader2 } from 'lucide-react';

const statusOptions = [
  { value: '', label: 'All Statuses' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Confirmed', label: 'Confirmed' },
  { value: 'Processing', label: 'Processing' },
  { value: 'Shipped', label: 'Shipped' },
  { value: 'Delivered', label: 'Delivered' },
  { value: 'Cancelled', label: 'Cancelled' },
];

const paymentOptions = [
  { value: '', label: 'All Payments' },
  { value: 'Paid', label: 'Paid' },
  { value: 'COD', label: 'COD' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Failed', label: 'Failed' },
  { value: 'Refunded', label: 'Refunded' },
];

export default function OrdersPage() {
  const [filters, setFilters] = useState<OrderFilters>({
    pageNumber: 1,
    pageSize: 10,
  });
  const [searchQuery, setSearchQuery] = useState('');
  const [showSyncMenu, setShowSyncMenu] = useState(false);
  const [syncingChannelId, setSyncingChannelId] = useState<string | null>(null);
  const syncMenuRef = useRef<HTMLDivElement>(null);

  // Build API filters
  const apiFilters: OrderFilters = {
    ...filters,
    search: searchQuery || undefined,
  };

  const { data, isLoading, error } = useOrders(apiFilters);
  const { data: stats } = useOrderStats();
  const { data: channels } = useChannels();
  const syncChannel = useSyncChannel();

  // Connected channels only
  const connectedChannels = channels?.filter(c => c.isConnected) || [];

  // Close sync menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (syncMenuRef.current && !syncMenuRef.current.contains(event.target as Node)) {
        setShowSyncMenu(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSyncChannel = async (channelId: string) => {
    setSyncingChannelId(channelId);
    try {
      await syncChannel.mutateAsync(channelId);
    } catch (err: unknown) {
      const errorMessage = (err as { message?: string })?.message || 'Unknown error';
      console.error('Failed to sync orders:', errorMessage);
      alert(`Failed to sync orders: ${errorMessage}`);
    } finally {
      setSyncingChannelId(null);
      setShowSyncMenu(false);
    }
  };

  const orders = data?.items || [];

  // Build dynamic channel options from fetched channels
  const channelOptions = [
    { value: '', label: 'All Channels' },
    ...(channels || []).map((channel) => ({
      value: channel.id,
      label: channel.name,
    })),
  ];
  const totalItems = data?.totalCount || 0;
  const totalPages = data?.totalPages || 1;

  const handleFilterChange = (key: keyof OrderFilters, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value || undefined,
      pageNumber: 1, // Reset to first page on filter change
    }));
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, pageNumber: page }));
  };

  const handlePageSizeChange = (size: number) => {
    setFilters((prev) => ({ ...prev, pageSize: size, pageNumber: 1 }));
  };

  return (
    <DashboardLayout title="Orders">
      {/* Stats Cards */}
      {stats && (
        <div className="mb-6 grid gap-4 md:grid-cols-4">
          <StatCard label="Total Orders" value={stats.totalOrders} />
          <StatCard label="Pending" value={stats.pendingOrders} />
          <StatCard label="Shipped" value={stats.shippedOrders} />
          <StatCard label="Delivered" value={stats.deliveredOrders} />
        </div>
      )}

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>All Orders</CardTitle>
          <div className="flex items-center gap-2">
            {/* Sync Orders Dropdown */}
            {connectedChannels.length > 0 && (
              <div className="relative" ref={syncMenuRef}>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowSyncMenu(!showSyncMenu)}
                  leftIcon={<RefreshCw className="h-4 w-4" />}
                  rightIcon={<ChevronDown className="h-3 w-3" />}
                >
                  Sync Orders
                </Button>
                {showSyncMenu && (
                  <div className="absolute right-0 top-full z-50 mt-1 w-56 rounded-md border bg-background shadow-lg">
                    <div className="p-2">
                      <p className="px-2 py-1.5 text-xs font-medium text-muted-foreground">
                        Select channel to sync
                      </p>
                      {connectedChannels.map((channel) => (
                        <button
                          key={channel.id}
                          onClick={() => handleSyncChannel(channel.id)}
                          disabled={syncingChannelId !== null}
                          className="flex w-full items-center justify-between rounded-sm px-2 py-1.5 text-sm hover:bg-muted disabled:opacity-50"
                        >
                          <span>{channel.name}</span>
                          {syncingChannelId === channel.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <RefreshCw className="h-3 w-3 text-muted-foreground" />
                          )}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
            <Button variant="outline" size="sm" leftIcon={<Download className="h-4 w-4" />}>
              Export
            </Button>
            <Link href="/orders/new">
              <Button size="sm" leftIcon={<Plus className="h-4 w-4" />}>
                Create Order
              </Button>
            </Link>
          </div>
        </CardHeader>
        <CardContent>
          {/* Filters */}
          <div className="mb-6 space-y-3">
            {/* Search and Actions Row */}
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
              <div className="flex-1 max-w-md">
                <Input
                  placeholder="Search orders..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  leftIcon={<Search className="h-4 w-4" />}
                />
              </div>
              <div className="flex items-center gap-2">
                <Select
                  options={statusOptions}
                  value={filters.status || ''}
                  onChange={(e) => handleFilterChange('status', e.target.value as OrderStatus)}
                  className="w-32"
                />
                <Select
                  options={channelOptions}
                  value={filters.channelId || ''}
                  onChange={(e) => handleFilterChange('channelId', e.target.value)}
                  className="w-40"
                />
                <Select
                  options={paymentOptions}
                  value={filters.paymentStatus || ''}
                  onChange={(e) => handleFilterChange('paymentStatus', e.target.value as PaymentStatus)}
                  className="w-32"
                />
                <Button variant="outline" size="sm" leftIcon={<Filter className="h-4 w-4" />}>
                  More
                </Button>
              </div>
            </div>
            {/* Active Filters */}
            {(filters.status || filters.channelId || filters.paymentStatus || searchQuery) && (
              <div className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Active filters:</span>
                {searchQuery && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    Search: {searchQuery}
                    <button onClick={() => setSearchQuery('')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                {filters.status && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    {filters.status}
                    <button onClick={() => handleFilterChange('status', '')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                {filters.channelId && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    {channels?.find((c) => c.id === filters.channelId)?.name || 'Channel'}
                    <button onClick={() => handleFilterChange('channelId', '')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                {filters.paymentStatus && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    {filters.paymentStatus}
                    <button onClick={() => handleFilterChange('paymentStatus', '')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                <button
                  onClick={() => {
                    setFilters({ pageNumber: 1, pageSize: 10 });
                    setSearchQuery('');
                  }}
                  className="text-xs text-muted-foreground hover:text-primary"
                >
                  Clear all
                </button>
              </div>
            )}
          </div>

          {/* Loading State */}
          {isLoading ? (
            <SectionLoader />
          ) : error ? (
            <div className="py-12 text-center text-error">
              Failed to load orders. Please try again.
            </div>
          ) : (
            <>
              {/* Orders Table */}
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Order</TableHead>
                    <TableHead>Customer</TableHead>
                    <TableHead>Channel</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Payment</TableHead>
                    <TableHead className="text-right">Amount</TableHead>
                    <TableHead>Date</TableHead>
                    <TableHead className="w-16">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {orders.length === 0 ? (
                    <TableEmpty
                      colSpan={8}
                      message="No orders found"
                      icon={<Package className="h-8 w-8" />}
                    />
                  ) : (
                    orders.map((order) => (
                      <TableRow key={order.id}>
                        <TableCell>
                          <div>
                            <Link
                              href={`/orders/${order.id}`}
                              className="font-medium text-primary hover:underline"
                            >
                              {order.orderNumber}
                            </Link>
                            <p className="text-xs text-muted-foreground">{order.externalOrderId}</p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div>
                            <p className="font-medium">{order.customerName}</p>
                            <p className="text-xs text-muted-foreground">{order.customerPhone}</p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <ChannelBadge name={order.channelName} type={order.channelType} />
                        </TableCell>
                        <TableCell>
                          <OrderStatusBadge status={order.status} />
                        </TableCell>
                        <TableCell>
                          <PaymentBadge status={order.paymentStatus} />
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(order.totalAmount)}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {formatDateTime(order.createdAt)}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <Link
                              href={`/orders/${order.id}`}
                              className="rounded p-1.5 hover:bg-muted"
                              title="View Order"
                            >
                              <Eye className="h-4 w-4 text-muted-foreground" />
                            </Link>
                            {(order.status === 'Confirmed' || order.status === 'Processing') && (
                              <button
                                className="rounded p-1.5 hover:bg-muted"
                                title="Create Shipment"
                              >
                                <Truck className="h-4 w-4 text-muted-foreground" />
                              </button>
                            )}
                            <button className="rounded p-1.5 hover:bg-muted" title="More Actions">
                              <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
                            </button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>

              {/* Pagination */}
              {orders.length > 0 && (
                <div className="mt-4">
                  <Pagination
                    currentPage={filters.pageNumber || 1}
                    totalPages={totalPages}
                    totalItems={totalItems}
                    pageSize={filters.pageSize || 10}
                    onPageChange={handlePageChange}
                    onPageSizeChange={handlePageSizeChange}
                  />
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </DashboardLayout>
  );
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <Card>
      <CardContent className="p-4">
        <p className="text-sm text-muted-foreground">{label}</p>
        <p className="text-2xl font-bold">{value.toLocaleString()}</p>
      </CardContent>
    </Card>
  );
}

function OrderStatusBadge({ status }: { status: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default' | 'primary'> = {
    Pending: 'warning',
    Confirmed: 'info',
    Processing: 'info',
    Shipped: 'primary',
    Delivered: 'success',
    Cancelled: 'error',
    Returned: 'default',
  };

  return (
    <Badge variant={variants[status] || 'default'} size="sm">
      {status}
    </Badge>
  );
}

function PaymentBadge({ status }: { status: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default'> = {
    Paid: 'success',
    COD: 'info',
    Pending: 'warning',
    Failed: 'error',
    Refunded: 'warning',
  };

  return (
    <Badge variant={variants[status] || 'default'} size="sm">
      {status}
    </Badge>
  );
}

function ChannelBadge({ name, type }: { name: string; type: string }) {
  const colors: Record<string, string> = {
    Shopify: 'bg-green-100 text-green-700',
    Amazon: 'bg-orange-100 text-orange-700',
    Flipkart: 'bg-yellow-100 text-yellow-700',
    Meesho: 'bg-pink-100 text-pink-700',
    WooCommerce: 'bg-purple-100 text-purple-700',
    Custom: 'bg-blue-100 text-blue-700',
  };

  return (
    <span
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
        colors[type] || 'bg-gray-100 text-gray-700'
      }`}
      title={type !== 'Custom' ? `${type} - ${name}` : name}
    >
      {name}
    </span>
  );
}
