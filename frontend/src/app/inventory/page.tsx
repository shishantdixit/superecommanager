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
import { useProducts, useInventoryStats } from '@/hooks';
import type { ProductFilters } from '@/services/inventory.service';
import {
  Search,
  Filter,
  Download,
  Plus,
  Eye,
  Edit,
  Package,
  AlertTriangle,
  TrendingDown,
  Box,
  DollarSign,
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

  const apiFilters: ProductFilters = {
    ...filters,
    searchTerm: searchQuery || undefined,
  };

  const { data, isLoading, error } = useProducts(apiFilters);
  const { data: stats } = useInventoryStats();

  const products = data?.items || [];
  const totalItems = data?.totalCount || 0;
  const totalPages = data?.totalPages || 1;

  const handleFilterChange = (key: keyof ProductFilters, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value === '' ? undefined : value === 'true' ? true : value === 'false' ? false : value,
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
                    <TableHead className="w-20">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {products.length === 0 ? (
                    <TableEmpty
                      colSpan={8}
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
