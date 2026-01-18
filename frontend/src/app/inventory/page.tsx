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
import { formatCurrency } from '@/lib/utils';
import { useProducts, useInventoryStats, useChannels, useSyncInventory } from '@/hooks';
import { SyncStatus, type ProductFilters } from '@/services/inventory.service';
import {
  Search,
  Filter,
  Download,
  Plus,
  Eye,
  Edit,
  Package,
  AlertTriangle,
  Box,
  DollarSign,
  RefreshCw,
  ChevronDown,
  Loader2,
  CheckCircle,
  Lock,
  Clock,
  AlertCircle,
} from 'lucide-react';

const statusOptions = [
  { value: '', label: 'All Status' },
  { value: 'true', label: 'Active' },
  { value: 'false', label: 'Inactive' },
];

const stockOptions = [
  { value: '', label: 'All Stock' },
  { value: 'true', label: 'Low Stock' },
];

const syncStatusOptions = [
  { value: '', label: 'All Sync' },
  { value: String(SyncStatus.Synced), label: '✓ Synced' },
  { value: String(SyncStatus.LocalOnly), label: '🔒 Local Only' },
  { value: String(SyncStatus.Pending), label: '⏳ Pending' },
  { value: String(SyncStatus.Conflict), label: '⚠️ Conflict' },
];

const sortOptions = [
  { value: 'Name', label: 'Name' },
  { value: 'Sku', label: 'SKU' },
  { value: 'Price', label: 'Price' },
  { value: 'Stock', label: 'Stock' },
  { value: 'CreatedAt', label: 'Date Added' },
];

export default function InventoryPage() {
  const [filters, setFilters] = useState<ProductFilters>({
    page: 1,
    pageSize: 10,
    sortBy: 'CreatedAt',
    sortDescending: true,
  });
  const [searchQuery, setSearchQuery] = useState('');
  const [showSyncMenu, setShowSyncMenu] = useState(false);
  const [syncingChannelId, setSyncingChannelId] = useState<string | null>(null);
  const syncMenuRef = useRef<HTMLDivElement>(null);

  const apiFilters: ProductFilters = {
    ...filters,
    searchTerm: searchQuery || undefined,
  };

  const { data, isLoading, error } = useProducts(apiFilters);
  const { data: stats } = useInventoryStats();
  const { data: channels } = useChannels();
  const syncInventory = useSyncInventory();

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

  const handleSyncInventory = async (channelId: string) => {
    setSyncingChannelId(channelId);
    try {
      await syncInventory.mutateAsync(channelId);
    } catch (err: unknown) {
      const errorMessage = (err as { message?: string })?.message || 'Unknown error';
      console.error('Failed to sync inventory:', errorMessage);
      alert(`Failed to sync inventory: ${errorMessage}`);
    } finally {
      setSyncingChannelId(null);
      setShowSyncMenu(false);
    }
  };

  const products = data?.items || [];
  const totalItems = data?.totalCount || 0;
  const totalPages = data?.totalPages || 1;

  const handleFilterChange = (key: keyof ProductFilters, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value === ''
        ? undefined
        : value === 'true'
        ? true
        : value === 'false'
        ? false
        : key === 'syncStatus'
        ? Number(value)
        : value,
      page: 1,
    }));
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  };

  const handlePageSizeChange = (size: number) => {
    setFilters((prev) => ({ ...prev, pageSize: size, page: 1 }));
  };

  return (
    <DashboardLayout title="Inventory">
      {/* Stats Cards */}
      {stats && (
        <div className="mb-6 grid gap-4 md:grid-cols-4">
          <StatCard
            label="Total Products"
            value={stats.totalProducts ?? 0}
            icon={<Package className="h-5 w-5 text-primary" />}
          />
          <StatCard
            label="Total Stock"
            value={stats.totalStockOnHand ?? 0}
            icon={<Box className="h-5 w-5 text-info" />}
          />
          <StatCard
            label="Low Stock"
            value={stats.lowStockProducts ?? 0}
            icon={<AlertTriangle className="h-5 w-5 text-warning" />}
            variant="warning"
          />
          <StatCard
            label="Inventory Value"
            value={formatCurrency(stats.totalInventoryValue ?? 0)}
            icon={<DollarSign className="h-5 w-5 text-success" />}
            isFormatted
          />
        </div>
      )}

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Products</CardTitle>
          <div className="flex items-center gap-2">
            {/* Sync Inventory Dropdown */}
            {connectedChannels.length > 0 && (
              <div className="relative" ref={syncMenuRef}>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowSyncMenu(!showSyncMenu)}
                  leftIcon={<RefreshCw className="h-4 w-4" />}
                  rightIcon={<ChevronDown className="h-3 w-3" />}
                >
                  Sync Inventory
                </Button>
                {showSyncMenu && (
                  <div className="absolute right-0 top-full z-50 mt-1 w-56 rounded-md border bg-background shadow-lg">
                    <div className="p-2">
                      <p className="px-2 py-1.5 text-xs font-medium text-muted-foreground">
                        Pull inventory from channel
                      </p>
                      {connectedChannels.map((channel) => (
                        <button
                          key={channel.id}
                          onClick={() => handleSyncInventory(channel.id)}
                          disabled={syncingChannelId !== null}
                          className="flex w-full items-center justify-between rounded-sm px-2 py-1.5 text-sm hover:bg-muted disabled:opacity-50"
                        >
                          <span>{channel.name}</span>
                          {syncingChannelId === channel.id ? (
                            <Loader2 className="h-4 w-4 animate-spin" />
                          ) : (
                            <Package className="h-3 w-3 text-muted-foreground" />
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
            <Link href="/inventory/new">
              <Button size="sm" leftIcon={<Plus className="h-4 w-4" />}>
                Add Product
              </Button>
            </Link>
          </div>
        </CardHeader>
        <CardContent>
          {/* Filters */}
          <div className="mb-6 flex flex-wrap items-center gap-4">
            <div className="flex-1 min-w-[200px]">
              <Input
                placeholder="Search products..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                leftIcon={<Search className="h-4 w-4" />}
              />
            </div>
            <Select
              options={statusOptions}
              value={filters.isActive === undefined ? '' : String(filters.isActive)}
              onChange={(e) => handleFilterChange('isActive', e.target.value)}
              className="w-32"
            />
            <Select
              options={stockOptions}
              value={filters.isLowStock === undefined ? '' : String(filters.isLowStock)}
              onChange={(e) => handleFilterChange('isLowStock', e.target.value)}
              className="w-32"
            />
            <Select
              options={syncStatusOptions}
              value={filters.syncStatus === undefined ? '' : String(filters.syncStatus)}
              onChange={(e) => handleFilterChange('syncStatus', e.target.value)}
              className="w-36"
            />
            <Select
              options={sortOptions}
              value={filters.sortBy || 'CreatedAt'}
              onChange={(e) => handleFilterChange('sortBy', e.target.value)}
              className="w-36"
            />
            <Button variant="outline" size="sm" leftIcon={<Filter className="h-4 w-4" />}>
              More Filters
            </Button>
          </div>

          {/* Loading State */}
          {isLoading ? (
            <SectionLoader />
          ) : error ? (
            <div className="py-12 text-center text-error">
              Failed to load products. Please try again.
            </div>
          ) : (
            <>
              {/* Products Table */}
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Product</TableHead>
                    <TableHead>SKU</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead className="text-right">Cost</TableHead>
                    <TableHead className="text-right">Price</TableHead>
                    <TableHead className="text-center">Stock</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Sync</TableHead>
                    <TableHead className="w-20">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {products.length === 0 ? (
                    <TableEmpty
                      colSpan={9}
                      message="No products found"
                      icon={<Package className="h-8 w-8" />}
                    />
                  ) : (
                    products.map((product) => (
                      <TableRow key={product.id}>
                        <TableCell>
                          <div className="flex items-center gap-3">
                            {product.imageUrl ? (
                              <img
                                src={product.imageUrl}
                                alt={product.name}
                                className="h-10 w-10 rounded object-cover"
                              />
                            ) : (
                              <div className="flex h-10 w-10 items-center justify-center rounded bg-muted">
                                <Package className="h-5 w-5 text-muted-foreground" />
                              </div>
                            )}
                            <div>
                              <Link
                                href={`/inventory/${product.id}`}
                                className="font-medium text-primary hover:underline"
                              >
                                {product.name}
                              </Link>
                              {product.variantCount > 0 && (
                                <p className="text-xs text-muted-foreground">
                                  {product.variantCount} variants
                                </p>
                              )}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell className="font-mono text-sm">{product.sku}</TableCell>
                        <TableCell>
                          {product.category ? (
                            <Badge variant="default" size="sm">
                              {product.category}
                            </Badge>
                          ) : (
                            <span className="text-muted-foreground">-</span>
                          )}
                        </TableCell>
                        <TableCell className="text-right">
                          {formatCurrency(product.costPrice)}
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(product.sellingPrice)}
                        </TableCell>
                        <TableCell className="text-center">
                          <StockBadge stock={product.totalStock} />
                        </TableCell>
                        <TableCell>
                          <Badge
                            variant={product.isActive ? 'success' : 'default'}
                            size="sm"
                          >
                            {product.isActive ? 'Active' : 'Inactive'}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <SyncStatusBadge
                            syncStatus={product.syncStatus}
                            channelPrice={product.channelSellingPrice}
                            localPrice={product.sellingPrice}
                          />
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <Link
                              href={`/inventory/${product.id}`}
                              className="rounded p-1.5 hover:bg-muted"
                              title="View Product"
                            >
                              <Eye className="h-4 w-4 text-muted-foreground" />
                            </Link>
                            <Link
                              href={`/inventory/${product.id}/edit`}
                              className="rounded p-1.5 hover:bg-muted"
                              title="Edit Product"
                            >
                              <Edit className="h-4 w-4 text-muted-foreground" />
                            </Link>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>

              {/* Pagination */}
              {products.length > 0 && (
                <div className="mt-4">
                  <Pagination
                    currentPage={filters.page || 1}
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

      {/* Low Stock Alert */}
      {stats && (stats.lowStockItems?.length ?? 0) > 0 && (
        <Card className="mt-6">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-warning">
              <AlertTriangle className="h-5 w-5" />
              Low Stock Alerts
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {stats.lowStockItems?.slice(0, 5).map((item) => (
                <div
                  key={`${item.productId}-${item.variantId || 'main'}`}
                  className="flex items-center justify-between rounded-lg border p-3"
                >
                  <div>
                    <p className="font-medium">{item.productName}</p>
                    {item.variantName && (
                      <p className="text-sm text-muted-foreground">{item.variantName}</p>
                    )}
                    <p className="text-xs text-muted-foreground">SKU: {item.sku}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-lg font-bold text-warning">{item.quantityOnHand}</p>
                    <p className="text-xs text-muted-foreground">
                      Reorder at {item.reorderPoint}
                    </p>
                  </div>
                </div>
              ))}
            </div>
            {(stats.lowStockItems?.length ?? 0) > 5 && (
              <Link
                href="/inventory?lowStock=true"
                className="mt-4 block text-center text-sm text-primary hover:underline"
              >
                View all {stats.lowStockItems?.length} low stock items
              </Link>
            )}
          </CardContent>
        </Card>
      )}
    </DashboardLayout>
  );
}

function StatCard({
  label,
  value,
  icon,
  variant,
  isFormatted,
}: {
  label: string;
  value: number | string;
  icon: React.ReactNode;
  variant?: 'warning' | 'error';
  isFormatted?: boolean;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-muted-foreground">{label}</p>
            <p
              className={`text-2xl font-bold ${
                variant === 'warning'
                  ? 'text-warning'
                  : variant === 'error'
                  ? 'text-error'
                  : ''
              }`}
            >
              {isFormatted ? value : typeof value === 'number' ? value.toLocaleString() : value}
            </p>
          </div>
          {icon}
        </div>
      </CardContent>
    </Card>
  );
}

function StockBadge({ stock }: { stock: number }) {
  if (stock === 0) {
    return (
      <Badge variant="error" size="sm">
        Out of Stock
      </Badge>
    );
  }
  if (stock < 10) {
    return (
      <Badge variant="warning" size="sm">
        {stock}
      </Badge>
    );
  }
  return (
    <span className="font-medium text-success">{stock.toLocaleString()}</span>
  );
}

function SyncStatusBadge({
  syncStatus,
  channelPrice,
  localPrice,
}: {
  syncStatus: SyncStatus;
  channelPrice?: number;
  localPrice: number;
}) {
  const statusConfig = {
    [SyncStatus.Synced]: {
      icon: <CheckCircle className="h-3 w-3" />,
      label: 'Synced',
      variant: 'success' as const,
      tooltip: 'Product is synced with channel',
    },
    [SyncStatus.LocalOnly]: {
      icon: <Lock className="h-3 w-3" />,
      label: 'Local',
      variant: 'default' as const,
      tooltip: 'Local only - will not sync to channel',
    },
    [SyncStatus.Pending]: {
      icon: <Clock className="h-3 w-3" />,
      label: 'Pending',
      variant: 'warning' as const,
      tooltip: 'Changes pending - click to push to channel',
    },
    [SyncStatus.Conflict]: {
      icon: <AlertCircle className="h-3 w-3" />,
      label: 'Conflict',
      variant: 'error' as const,
      tooltip: channelPrice
        ? `Channel: ${formatCurrency(channelPrice)} vs Local: ${formatCurrency(localPrice)}`
        : 'Price conflict detected',
    },
  };

  const config = statusConfig[syncStatus] || statusConfig[SyncStatus.Synced];

  return (
    <div title={config.tooltip}>
      <Badge variant={config.variant} size="sm" className="gap-1">
        {config.icon}
        {config.label}
      </Badge>
    </div>
  );
}
