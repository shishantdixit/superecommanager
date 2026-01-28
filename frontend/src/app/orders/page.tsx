'use client';

import { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
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
  ConfirmModal,
} from '@/components/ui';
import { formatCurrency, formatDateTime } from '@/lib/utils';
import { useOrders, useOrderStats, useChannels, useSyncChannel, useDeleteOrder, useBulkUpdateOrders } from '@/hooks';
import type { OrderFilters, OrderStatus, PaymentStatus, OrderSortBy } from '@/types/api';
import { Search, Filter, Download, Plus, Eye, Truck, MoreHorizontal, Package, RefreshCw, ChevronDown, Loader2, Edit, Trash2, ArrowUpDown, ArrowUp, ArrowDown, CheckSquare, Square } from 'lucide-react';

const statusOptions = [
  { value: '', label: 'All Statuses' },
  { value: 'Pending', label: 'Pending' },
  { value: 'Confirmed', label: 'Confirmed' },
  { value: 'Processing', label: 'Processing' },
  { value: 'Shipped', label: 'Shipped' },
  { value: 'Delivered', label: 'Delivered' },
  { value: 'Cancelled', label: 'Cancelled' },
];

// Statuses that can be deleted
const DELETABLE_STATUSES = ['Pending', 'Cancelled'];

const paymentOptions = [
  { value: '', label: 'All Payments' },
  { value: 'Paid', label: 'Paid' },
  { value: 'COD', label: 'COD' },
  { value: 'CODPending', label: 'COD Pending' },
  { value: 'CODCollected', label: 'COD Collected' },
  { value: 'Pending', label: 'Pending' },
  { value: 'PartiallyPaid', label: 'Partially Paid' },
  { value: 'Failed', label: 'Failed' },
  { value: 'Refunded', label: 'Refunded' },
  { value: 'PartiallyRefunded', label: 'Partially Refunded' },
];

const sortOptions = [
  { value: 'OrderDate', label: 'Order Date' },
  { value: 'CreatedAt', label: 'Created Date' },
  { value: 'TotalAmount', label: 'Amount' },
  { value: 'CustomerName', label: 'Customer' },
  { value: 'Status', label: 'Status' },
];

export default function OrdersPage() {
  const router = useRouter();
  const [filters, setFilters] = useState<OrderFilters>({
    pageNumber: 1,
    pageSize: 10,
    sortBy: 'OrderDate',
    sortDescending: true,
  });
  const [searchQuery, setSearchQuery] = useState('');
  const [showSyncMenu, setShowSyncMenu] = useState(false);
  const [syncingChannelId, setSyncingChannelId] = useState<string | null>(null);
  const [activeDropdown, setActiveDropdown] = useState<string | null>(null);
  const [deleteConfirm, setDeleteConfirm] = useState<{ isOpen: boolean; orderId: string | null; orderNumber: string | null }>({
    isOpen: false,
    orderId: null,
    orderNumber: null,
  });
  const [selectedOrders, setSelectedOrders] = useState<Set<string>>(new Set());
  const [showBulkMenu, setShowBulkMenu] = useState(false);
  const [bulkDeleteConfirm, setBulkDeleteConfirm] = useState(false);
  const syncMenuRef = useRef<HTMLDivElement>(null);
  const bulkMenuRef = useRef<HTMLDivElement>(null);

  // Build API filters
  const apiFilters: OrderFilters = {
    ...filters,
    search: searchQuery || undefined,
  };

  const { data, isLoading, error } = useOrders(apiFilters);
  const { data: stats } = useOrderStats();
  const { data: channels } = useChannels();
  const syncChannel = useSyncChannel();
  const deleteOrder = useDeleteOrder();
  const bulkUpdate = useBulkUpdateOrders();

  // Connected channels only
  const connectedChannels = channels?.filter(c => c.isConnected) || [];

  // Close menus when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (syncMenuRef.current && !syncMenuRef.current.contains(event.target as Node)) {
        setShowSyncMenu(false);
      }
      if (bulkMenuRef.current && !bulkMenuRef.current.contains(event.target as Node)) {
        setShowBulkMenu(false);
      }
      // Close dropdown menus when clicking outside
      const target = event.target as HTMLElement;
      if (!target.closest('.dropdown-trigger') && !target.closest('.dropdown-menu')) {
        setActiveDropdown(null);
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

  const handleDeleteClick = (orderId: string, orderNumber: string) => {
    setDeleteConfirm({
      isOpen: true,
      orderId,
      orderNumber,
    });
    setActiveDropdown(null);
  };

  const handleDeleteConfirm = async () => {
    if (!deleteConfirm.orderId) return;

    try {
      await deleteOrder.mutateAsync(deleteConfirm.orderId);
      setDeleteConfirm({ isOpen: false, orderId: null, orderNumber: null });
    } catch (err: unknown) {
      const errorMessage = (err as { message?: string })?.message || 'Unknown error';
      console.error('Failed to delete order:', errorMessage);
      alert(`Failed to delete order: ${errorMessage}`);
    }
  };

  const handleDropdownToggle = (orderId: string) => {
    setActiveDropdown(activeDropdown === orderId ? null : orderId);
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

  const handleFilterChange = (key: keyof OrderFilters, value: string | boolean) => {
    setFilters((prev) => {
      const updated = { ...prev, pageNumber: 1 };

      // Handle sortDescending as boolean
      if (key === 'sortDescending') {
        updated.sortDescending = value as boolean;
        return updated;
      }

      // Special handling for COD filter
      if (key === 'paymentStatus' && value === 'COD') {
        // When 'COD' is selected, set isCOD=true and clear paymentStatus
        updated.isCOD = true;
        updated.paymentStatus = undefined;
        return updated;
      }

      // When other payment status is selected, clear isCOD
      if (key === 'paymentStatus' && value && value !== 'COD') {
        updated.paymentStatus = value as PaymentStatus;
        updated.isCOD = undefined;
        return updated;
      }

      // When payment status is cleared
      if (key === 'paymentStatus' && !value) {
        updated.paymentStatus = undefined;
        updated.isCOD = undefined;
        return updated;
      }

      // Default handling for other fields
      (updated[key] as any) = value || undefined;
      return updated;
    });
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, pageNumber: page }));
  };

  const handlePageSizeChange = (size: number) => {
    setFilters((prev) => ({ ...prev, pageSize: size, pageNumber: 1 }));
  };

  const handleSort = (sortBy: OrderSortBy) => {
    setFilters((prev) => ({
      ...prev,
      sortBy,
      sortDescending: prev.sortBy === sortBy ? !prev.sortDescending : true,
      pageNumber: 1,
    }));
  };

  const handleSelectAll = () => {
    if (selectedOrders.size === orders.length) {
      setSelectedOrders(new Set());
    } else {
      setSelectedOrders(new Set(orders.map((o) => o.id)));
    }
  };

  const handleSelectOrder = (orderId: string) => {
    const newSelected = new Set(selectedOrders);
    if (newSelected.has(orderId)) {
      newSelected.delete(orderId);
    } else {
      newSelected.add(orderId);
    }
    setSelectedOrders(newSelected);
  };

  const handleBulkStatusUpdate = async (status: OrderStatus) => {
    if (selectedOrders.size === 0) return;

    try {
      await bulkUpdate.mutateAsync({
        orderIds: Array.from(selectedOrders),
        status,
      });
      setSelectedOrders(new Set());
      setShowBulkMenu(false);
    } catch (err: unknown) {
      const errorMessage = (err as { message?: string })?.message || 'Unknown error';
      console.error('Failed to bulk update orders:', errorMessage);
      alert(`Failed to bulk update orders: ${errorMessage}`);
    }
  };

  const handleBulkDelete = async () => {
    if (selectedOrders.size === 0) return;

    // Filter orders to only include deletable ones
    const selectedOrdersList = Array.from(selectedOrders);
    const ordersToDelete = orders.filter(
      (o) => selectedOrdersList.includes(o.id) && DELETABLE_STATUSES.includes(o.status)
    );
    const nonDeletableOrders = orders.filter(
      (o) => selectedOrdersList.includes(o.id) && !DELETABLE_STATUSES.includes(o.status)
    );

    if (ordersToDelete.length === 0) {
      alert('None of the selected orders can be deleted. Only Pending and Cancelled orders can be deleted.');
      setBulkDeleteConfirm(false);
      return;
    }

    try {
      // Delete eligible orders
      await Promise.all(
        ordersToDelete.map((order) => deleteOrder.mutateAsync(order.id))
      );

      setSelectedOrders(new Set());
      setBulkDeleteConfirm(false);
      setShowBulkMenu(false);

      // Show summary if some orders couldn't be deleted
      if (nonDeletableOrders.length > 0) {
        alert(
          `Successfully deleted ${ordersToDelete.length} order${ordersToDelete.length > 1 ? 's' : ''}.\n\n` +
          `${nonDeletableOrders.length} order${nonDeletableOrders.length > 1 ? 's' : ''} could not be deleted (only Pending and Cancelled orders can be deleted).`
        );
      }
    } catch (err: unknown) {
      const errorMessage = (err as { message?: string })?.message || 'Unknown error';
      console.error('Failed to delete orders:', errorMessage);
      alert(`Failed to delete orders: ${errorMessage}`);
    }
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
          <CardTitle>
            All Orders
            {selectedOrders.size > 0 && (
              <span className="ml-2 text-sm font-normal text-muted-foreground">
                ({selectedOrders.size} selected)
              </span>
            )}
          </CardTitle>
          <div className="flex items-center gap-2">
            {/* Bulk Actions */}
            {selectedOrders.size > 0 && (
              <div className="relative" ref={bulkMenuRef}>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowBulkMenu(!showBulkMenu)}
                  leftIcon={<CheckSquare className="h-4 w-4" />}
                  rightIcon={<ChevronDown className="h-3 w-3" />}
                >
                  Bulk Actions
                </Button>
                {showBulkMenu && (
                  <div className="absolute right-0 top-full z-50 mt-1 w-48 rounded-md border bg-background shadow-lg">
                    <div className="p-2">
                      <p className="px-2 py-1.5 text-xs font-medium text-muted-foreground">
                        Update Status
                      </p>
                      {statusOptions.slice(1).map((status) => (
                        <button
                          type="button"
                          key={status.value}
                          onClick={() => handleBulkStatusUpdate(status.value as OrderStatus)}
                          disabled={bulkUpdate.isPending}
                          className="flex w-full items-center rounded-sm px-2 py-1.5 text-sm hover:bg-muted disabled:opacity-50"
                        >
                          {bulkUpdate.isPending ? (
                            <Loader2 className="mr-2 h-3 w-3 animate-spin" />
                          ) : null}
                          <span>{status.label}</span>
                        </button>
                      ))}
                      <div className="my-1 border-t" />
                      <button
                        type="button"
                        onClick={() => {
                          setBulkDeleteConfirm(true);
                          setShowBulkMenu(false);
                        }}
                        className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-error hover:bg-error/10"
                      >
                        <Trash2 className="h-4 w-4" />
                        <span>Delete Selected</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}
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
                          type="button"
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
              <div className="flex items-center gap-2 flex-wrap">
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
                  value={filters.isCOD ? 'COD' : (filters.paymentStatus || '')}
                  onChange={(e) => handleFilterChange('paymentStatus', e.target.value)}
                  className="w-40"
                />
                <div className="flex items-center gap-1">
                  <Select
                    options={sortOptions}
                    value={filters.sortBy || 'OrderDate'}
                    onChange={(e) => handleFilterChange('sortBy', e.target.value as OrderSortBy)}
                    className="w-36"
                  />
                  <button
                    type="button"
                    onClick={() => handleFilterChange('sortDescending', !filters.sortDescending)}
                    className="rounded p-2 hover:bg-muted"
                    title={filters.sortDescending ? 'Sort Ascending' : 'Sort Descending'}
                  >
                    <ArrowUpDown className={`h-4 w-4 ${filters.sortDescending ? 'rotate-180' : ''} transition-transform`} />
                  </button>
                </div>
              </div>
            </div>
            {/* Active Filters */}
            {(filters.status || filters.channelId || filters.paymentStatus || filters.isCOD || searchQuery) && (
              <div className="flex items-center gap-2 text-sm">
                <span className="text-muted-foreground">Active filters:</span>
                {searchQuery && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    Search: {searchQuery}
                    <button type="button" onClick={() => setSearchQuery('')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                {filters.status && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    {filters.status}
                    <button type="button" onClick={() => handleFilterChange('status', '')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                {filters.channelId && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    {channels?.find((c) => c.id === filters.channelId)?.name || 'Channel'}
                    <button type="button" onClick={() => handleFilterChange('channelId', '')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                {filters.isCOD && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    COD
                    <button type="button" onClick={() => handleFilterChange('paymentStatus', '')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                {filters.paymentStatus && (
                  <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2.5 py-0.5 text-xs">
                    {filters.paymentStatus}
                    <button type="button" onClick={() => handleFilterChange('paymentStatus', '')} className="ml-1 hover:text-primary">&times;</button>
                  </span>
                )}
                <button
                  type="button"
                  onClick={() => {
                    setFilters({ pageNumber: 1, pageSize: 10, sortBy: 'OrderDate', sortDescending: true });
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
                    <TableHead className="w-12">
                      <button
                        type="button"
                        onClick={handleSelectAll}
                        className="flex items-center justify-center"
                        title={selectedOrders.size === orders.length ? 'Deselect All' : 'Select All'}
                      >
                        {selectedOrders.size === orders.length && orders.length > 0 ? (
                          <CheckSquare className="h-4 w-4" />
                        ) : (
                          <Square className="h-4 w-4" />
                        )}
                      </button>
                    </TableHead>
                    <TableHead>
                      <button
                        type="button"
                        onClick={() => handleSort('OrderDate')}
                        className="flex items-center gap-1 hover:text-primary"
                      >
                        Order
                        {filters.sortBy === 'OrderDate' && (
                          filters.sortDescending ? <ArrowDown className="h-3 w-3" /> : <ArrowUp className="h-3 w-3" />
                        )}
                      </button>
                    </TableHead>
                    <TableHead>
                      <button
                        type="button"
                        onClick={() => handleSort('CustomerName')}
                        className="flex items-center gap-1 hover:text-primary"
                      >
                        Customer
                        {filters.sortBy === 'CustomerName' && (
                          filters.sortDescending ? <ArrowDown className="h-3 w-3" /> : <ArrowUp className="h-3 w-3" />
                        )}
                      </button>
                    </TableHead>
                    <TableHead>Channel</TableHead>
                    <TableHead>
                      <button
                        type="button"
                        onClick={() => handleSort('Status')}
                        className="flex items-center gap-1 hover:text-primary"
                      >
                        Status
                        {filters.sortBy === 'Status' && (
                          filters.sortDescending ? <ArrowDown className="h-3 w-3" /> : <ArrowUp className="h-3 w-3" />
                        )}
                      </button>
                    </TableHead>
                    <TableHead>Payment</TableHead>
                    <TableHead className="text-right">
                      <button
                        type="button"
                        onClick={() => handleSort('TotalAmount')}
                        className="flex items-center gap-1 ml-auto hover:text-primary"
                      >
                        Amount
                        {filters.sortBy === 'TotalAmount' && (
                          filters.sortDescending ? <ArrowDown className="h-3 w-3" /> : <ArrowUp className="h-3 w-3" />
                        )}
                      </button>
                    </TableHead>
                    <TableHead>
                      <button
                        type="button"
                        onClick={() => handleSort('CreatedAt')}
                        className="flex items-center gap-1 hover:text-primary"
                      >
                        Date
                        {filters.sortBy === 'CreatedAt' && (
                          filters.sortDescending ? <ArrowDown className="h-3 w-3" /> : <ArrowUp className="h-3 w-3" />
                        )}
                      </button>
                    </TableHead>
                    <TableHead className="w-16">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {orders.length === 0 ? (
                    <TableEmpty
                      colSpan={9}
                      message="No orders found"
                      icon={<Package className="h-8 w-8" />}
                    />
                  ) : (
                    orders.map((order) => (
                      <TableRow key={order.id}>
                        <TableCell>
                          <button
                            type="button"
                            onClick={() => handleSelectOrder(order.id)}
                            className="flex items-center justify-center"
                          >
                            {selectedOrders.has(order.id) ? (
                              <CheckSquare className="h-4 w-4 text-primary" />
                            ) : (
                              <Square className="h-4 w-4" />
                            )}
                          </button>
                        </TableCell>
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
                            {/* Actions Dropdown */}
                            <div className="relative">
                              <button
                                type="button"
                                onClick={() => handleDropdownToggle(order.id)}
                                className="dropdown-trigger rounded p-1.5 hover:bg-muted"
                                title="More Actions"
                              >
                                <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
                              </button>
                              {activeDropdown === order.id && (
                                <div className="dropdown-menu absolute right-0 z-50 mt-1 w-40 rounded-md border bg-background shadow-lg">
                                  <div className="p-1">
                                    <button
                                      type="button"
                                      onClick={() => {
                                        router.push(`/orders/${order.id}/edit`);
                                        setActiveDropdown(null);
                                      }}
                                      className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-muted"
                                    >
                                      <Edit className="h-4 w-4" />
                                      <span>Edit Order</span>
                                    </button>
                                    <button
                                      type="button"
                                      onClick={() => handleDeleteClick(order.id, order.orderNumber)}
                                      className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-error hover:bg-error/10"
                                    >
                                      <Trash2 className="h-4 w-4" />
                                      <span>Delete Order</span>
                                    </button>
                                  </div>
                                </div>
                              )}
                            </div>
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

      {/* Delete Confirmation Modal */}
      <ConfirmModal
        isOpen={deleteConfirm.isOpen}
        onClose={() => setDeleteConfirm({ isOpen: false, orderId: null, orderNumber: null })}
        onConfirm={handleDeleteConfirm}
        title="Delete Order"
        message={`Are you sure you want to delete order ${deleteConfirm.orderNumber}? This action cannot be undone.`}
        confirmText="Delete"
        variant="danger"
        isLoading={deleteOrder.isPending}
      />

      {/* Bulk Delete Confirmation Modal */}
      <ConfirmModal
        isOpen={bulkDeleteConfirm}
        onClose={() => setBulkDeleteConfirm(false)}
        onConfirm={handleBulkDelete}
        title="Delete Selected Orders"
        message={(() => {
          const selectedOrdersList = Array.from(selectedOrders);
          const deletableCount = orders.filter(
            (o) => selectedOrdersList.includes(o.id) && DELETABLE_STATUSES.includes(o.status)
          ).length;
          const nonDeletableCount = selectedOrders.size - deletableCount;

          if (deletableCount === 0) {
            return 'None of the selected orders can be deleted. Only Pending and Cancelled orders can be deleted.';
          }

          if (nonDeletableCount > 0) {
            return `${deletableCount} of ${selectedOrders.size} selected order${selectedOrders.size > 1 ? 's' : ''} will be deleted. ${nonDeletableCount} order${nonDeletableCount > 1 ? 's' : ''} cannot be deleted (only Pending and Cancelled orders can be deleted). This action cannot be undone.`;
          }

          return `Are you sure you want to delete ${selectedOrders.size} selected order${selectedOrders.size > 1 ? 's' : ''}? This action cannot be undone.`;
        })()}
        confirmText={(() => {
          const selectedOrdersList = Array.from(selectedOrders);
          const deletableCount = orders.filter(
            (o) => selectedOrdersList.includes(o.id) && DELETABLE_STATUSES.includes(o.status)
          ).length;
          return deletableCount === 0 ? 'OK' : `Delete ${deletableCount} Order${deletableCount > 1 ? 's' : ''}`;
        })()}
        variant="danger"
        isLoading={deleteOrder.isPending}
      />
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
