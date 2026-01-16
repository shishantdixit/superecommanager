'use client';

import { useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Badge,
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableEmpty,
  SectionLoader,
  Input,
  Select,
  Modal,
} from '@/components/ui';
import { formatCurrency, formatDateTime } from '@/lib/utils';
import { useProduct, useStockMovements, useAdjustStock } from '@/hooks';
import type { StockMovementFilters, StockAdjustmentRequest } from '@/services/inventory.service';
import type { StockMovementType } from '@/types/api';
import {
  ArrowLeft,
  Edit,
  Package,
  Box,
  TrendingUp,
  TrendingDown,
  AlertTriangle,
  Plus,
  Minus,
  RotateCcw,
  History,
} from 'lucide-react';

const movementTypeOptions = [
  { value: '', label: 'All Types' },
  { value: 'Purchase', label: 'Purchase' },
  { value: 'Sale', label: 'Sale' },
  { value: 'Return', label: 'Return' },
  { value: 'Adjustment', label: 'Adjustment' },
  { value: 'Transfer', label: 'Transfer' },
  { value: 'Damage', label: 'Damage' },
  { value: 'RTO', label: 'RTO' },
];

const adjustmentTypeOptions = [
  { value: 'Purchase', label: 'Add Stock (Purchase)' },
  { value: 'Adjustment', label: 'Adjust Stock' },
  { value: 'Damage', label: 'Remove (Damage)' },
  { value: 'Return', label: 'Return to Stock' },
];

export default function ProductDetailPage() {
  const params = useParams();
  const router = useRouter();
  const productId = params.id as string;

  const [movementFilters, setMovementFilters] = useState<StockMovementFilters>({
    productId,
    page: 1,
    pageSize: 10,
  });
  const [showAdjustModal, setShowAdjustModal] = useState(false);
  const [selectedInventoryItem, setSelectedInventoryItem] = useState<string | null>(null);
  const [adjustmentType, setAdjustmentType] = useState<StockMovementType>('Adjustment');
  const [adjustmentQuantity, setAdjustmentQuantity] = useState('');
  const [adjustmentNotes, setAdjustmentNotes] = useState('');

  const { data: product, isLoading, error } = useProduct(productId);
  const { data: movements, isLoading: movementsLoading } = useStockMovements(movementFilters);
  const adjustStockMutation = useAdjustStock();

  const handleAdjustStock = async () => {
    if (!selectedInventoryItem || !adjustmentQuantity) return;

    const data: StockAdjustmentRequest = {
      inventoryItemId: selectedInventoryItem,
      adjustmentType: adjustmentType,
      quantity: parseInt(adjustmentQuantity, 10),
      notes: adjustmentNotes || undefined,
    };

    try {
      await adjustStockMutation.mutateAsync(data);
      setShowAdjustModal(false);
      setSelectedInventoryItem(null);
      setAdjustmentQuantity('');
      setAdjustmentNotes('');
    } catch (err) {
      console.error('Failed to adjust stock:', err);
    }
  };

  const openAdjustModal = (inventoryItemId: string) => {
    setSelectedInventoryItem(inventoryItemId);
    setShowAdjustModal(true);
  };

  if (isLoading) {
    return (
      <DashboardLayout title="Product Details">
        <SectionLoader />
      </DashboardLayout>
    );
  }

  if (error || !product) {
    return (
      <DashboardLayout title="Product Details">
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-error">Failed to load product details.</p>
            <Button variant="outline" className="mt-4" onClick={() => router.back()}>
              Go Back
            </Button>
          </CardContent>
        </Card>
      </DashboardLayout>
    );
  }

  const inventory = product.inventorySummary;

  return (
    <DashboardLayout title={product.name}>
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => router.back()}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold">{product.name}</h1>
            <p className="text-sm text-muted-foreground">SKU: {product.sku}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Link href={`/inventory/${productId}/edit`}>
            <Button variant="outline" leftIcon={<Edit className="h-4 w-4" />}>
              Edit Product
            </Button>
          </Link>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Product Info */}
        <div className="lg:col-span-2 space-y-6">
          {/* Basic Info Card */}
          <Card>
            <CardHeader>
              <CardTitle>Product Information</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex gap-6">
                {product.imageUrl ? (
                  <img
                    src={product.imageUrl}
                    alt={product.name}
                    className="h-32 w-32 rounded-lg object-cover"
                  />
                ) : (
                  <div className="flex h-32 w-32 items-center justify-center rounded-lg bg-muted">
                    <Package className="h-12 w-12 text-muted-foreground" />
                  </div>
                )}
                <div className="flex-1 space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <p className="text-sm text-muted-foreground">Category</p>
                      <p className="font-medium">{product.category || '-'}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Brand</p>
                      <p className="font-medium">{product.brand || '-'}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Cost Price</p>
                      <p className="font-medium">{formatCurrency(product.costPrice)}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Selling Price</p>
                      <p className="font-medium text-primary">
                        {formatCurrency(product.sellingPrice)}
                      </p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">HSN Code</p>
                      <p className="font-medium">{product.hsnCode || '-'}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Tax Rate</p>
                      <p className="font-medium">
                        {product.taxRate ? `${product.taxRate}%` : '-'}
                      </p>
                    </div>
                  </div>
                  {product.description && (
                    <div>
                      <p className="text-sm text-muted-foreground">Description</p>
                      <p className="text-sm">{product.description}</p>
                    </div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Variants */}
          {product.variants.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Variants ({product.variants.length})</CardTitle>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Variant</TableHead>
                      <TableHead>SKU</TableHead>
                      <TableHead className="text-right">Price</TableHead>
                      <TableHead className="text-center">Stock</TableHead>
                      <TableHead>Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {product.variants.map((variant) => (
                      <TableRow key={variant.id}>
                        <TableCell>
                          <div>
                            <p className="font-medium">{variant.name}</p>
                            {variant.option1Name && (
                              <p className="text-xs text-muted-foreground">
                                {variant.option1Name}: {variant.option1Value}
                                {variant.option2Name &&
                                  ` | ${variant.option2Name}: ${variant.option2Value}`}
                              </p>
                            )}
                          </div>
                        </TableCell>
                        <TableCell className="font-mono text-sm">{variant.sku}</TableCell>
                        <TableCell className="text-right">
                          {variant.sellingPrice
                            ? formatCurrency(variant.sellingPrice)
                            : formatCurrency(product.sellingPrice)}
                        </TableCell>
                        <TableCell className="text-center">
                          <StockBadge stock={variant.quantityOnHand} />
                        </TableCell>
                        <TableCell>
                          <Badge variant={variant.isActive ? 'success' : 'default'} size="sm">
                            {variant.isActive ? 'Active' : 'Inactive'}
                          </Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          )}

          {/* Stock Movements */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <History className="h-5 w-5" />
                Stock Movement History
              </CardTitle>
              <Select
                options={movementTypeOptions}
                value={movementFilters.movementType || ''}
                onChange={(e) =>
                  setMovementFilters((prev) => ({
                    ...prev,
                    movementType: (e.target.value as StockMovementType) || undefined,
                    page: 1,
                  }))
                }
                className="w-40"
              />
            </CardHeader>
            <CardContent>
              {movementsLoading ? (
                <SectionLoader />
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Date</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead>SKU</TableHead>
                      <TableHead className="text-right">Qty</TableHead>
                      <TableHead className="text-right">Before</TableHead>
                      <TableHead className="text-right">After</TableHead>
                      <TableHead>Notes</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {(movements?.items?.length ?? 0) === 0 ? (
                      <TableEmpty
                        colSpan={7}
                        message="No stock movements found"
                        icon={<History className="h-8 w-8" />}
                      />
                    ) : (
                      movements?.items?.map((movement) => (
                        <TableRow key={movement.id}>
                          <TableCell className="text-sm">
                            {formatDateTime(movement.createdAt)}
                          </TableCell>
                          <TableCell>
                            <MovementTypeBadge type={movement.movementType} />
                          </TableCell>
                          <TableCell className="font-mono text-sm">{movement.sku}</TableCell>
                          <TableCell className="text-right">
                            <span
                              className={
                                movement.quantity > 0 ? 'text-success' : 'text-error'
                              }
                            >
                              {movement.quantity > 0 ? '+' : ''}
                              {movement.quantity}
                            </span>
                          </TableCell>
                          <TableCell className="text-right text-muted-foreground">
                            {movement.quantityBefore}
                          </TableCell>
                          <TableCell className="text-right font-medium">
                            {movement.quantityAfter}
                          </TableCell>
                          <TableCell className="max-w-[150px] truncate text-sm text-muted-foreground">
                            {movement.notes || '-'}
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Sidebar - Inventory Summary */}
        <div className="space-y-6">
          {/* Inventory Summary */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Box className="h-5 w-5" />
                Inventory Summary
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="rounded-lg bg-muted p-3 text-center">
                  <p className="text-2xl font-bold">{inventory?.totalOnHand ?? 0}</p>
                  <p className="text-xs text-muted-foreground">On Hand</p>
                </div>
                <div className="rounded-lg bg-muted p-3 text-center">
                  <p className="text-2xl font-bold text-warning">
                    {inventory?.totalReserved ?? 0}
                  </p>
                  <p className="text-xs text-muted-foreground">Reserved</p>
                </div>
              </div>
              <div className="rounded-lg border p-3 text-center">
                <p className="text-3xl font-bold text-primary">
                  {inventory?.totalAvailable ?? 0}
                </p>
                <p className="text-sm text-muted-foreground">Available for Sale</p>
              </div>
              {inventory?.isLowStock && (
                <div className="flex items-center gap-2 rounded-lg bg-warning/10 p-3 text-warning">
                  <AlertTriangle className="h-5 w-5" />
                  <span className="text-sm font-medium">Low Stock Alert</span>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Inventory Items */}
          {inventory?.items && inventory.items.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Stock by Location</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {inventory.items.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <div>
                      <p className="font-medium text-sm">
                        {item.variantName || 'Main Product'}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {item.location || 'Default Location'}
                      </p>
                    </div>
                    <div className="flex items-center gap-3">
                      <div className="text-right">
                        <p className="font-bold">{item.quantityAvailable}</p>
                        <p className="text-xs text-muted-foreground">available</p>
                      </div>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => openAdjustModal(item.id)}
                      >
                        Adjust
                      </Button>
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>
          )}

          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button
                variant="outline"
                className="w-full justify-start"
                leftIcon={<Plus className="h-4 w-4" />}
                onClick={() => {
                  if (inventory?.items?.[0]) {
                    setAdjustmentType('Purchase');
                    openAdjustModal(inventory.items[0].id);
                  }
                }}
              >
                Add Stock
              </Button>
              <Button
                variant="outline"
                className="w-full justify-start"
                leftIcon={<Minus className="h-4 w-4" />}
                onClick={() => {
                  if (inventory?.items?.[0]) {
                    setAdjustmentType('Damage');
                    openAdjustModal(inventory.items[0].id);
                  }
                }}
              >
                Remove Stock
              </Button>
              <Button
                variant="outline"
                className="w-full justify-start"
                leftIcon={<RotateCcw className="h-4 w-4" />}
                onClick={() => {
                  if (inventory?.items?.[0]) {
                    setAdjustmentType('Adjustment');
                    openAdjustModal(inventory.items[0].id);
                  }
                }}
              >
                Adjust Stock
              </Button>
            </CardContent>
          </Card>

          {/* Product Status */}
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Status</span>
                <Badge variant={product.isActive ? 'success' : 'default'}>
                  {product.isActive ? 'Active' : 'Inactive'}
                </Badge>
              </div>
              <div className="mt-3 flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Created</span>
                <span className="text-sm">{formatDateTime(product.createdAt)}</span>
              </div>
              {product.updatedAt && (
                <div className="mt-2 flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Updated</span>
                  <span className="text-sm">{formatDateTime(product.updatedAt)}</span>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Stock Adjustment Modal */}
      <Modal
        isOpen={showAdjustModal}
        onClose={() => setShowAdjustModal(false)}
        title="Adjust Stock"
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Adjustment Type</label>
            <Select
              options={adjustmentTypeOptions}
              value={adjustmentType}
              onChange={(e) => setAdjustmentType(e.target.value as StockMovementType)}
              className="w-full"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Quantity</label>
            <Input
              type="number"
              min="1"
              value={adjustmentQuantity}
              onChange={(e) => setAdjustmentQuantity(e.target.value)}
              placeholder="Enter quantity"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Notes (Optional)</label>
            <Input
              value={adjustmentNotes}
              onChange={(e) => setAdjustmentNotes(e.target.value)}
              placeholder="Reason for adjustment"
            />
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <Button variant="outline" onClick={() => setShowAdjustModal(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleAdjustStock}
              isLoading={adjustStockMutation.isPending}
              disabled={!adjustmentQuantity || parseInt(adjustmentQuantity, 10) <= 0}
            >
              Adjust Stock
            </Button>
          </div>
        </div>
      </Modal>
    </DashboardLayout>
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
  return <span className="font-medium text-success">{stock.toLocaleString()}</span>;
}

function MovementTypeBadge({ type }: { type: StockMovementType }) {
  const config: Record<
    StockMovementType,
    { variant: 'success' | 'error' | 'warning' | 'info' | 'default'; icon: React.ReactNode }
  > = {
    Purchase: { variant: 'success', icon: <TrendingUp className="h-3 w-3" /> },
    Sale: { variant: 'info', icon: <TrendingDown className="h-3 w-3" /> },
    Return: { variant: 'warning', icon: <RotateCcw className="h-3 w-3" /> },
    Adjustment: { variant: 'default', icon: null },
    Transfer: { variant: 'info', icon: null },
    Damage: { variant: 'error', icon: <AlertTriangle className="h-3 w-3" /> },
    RTO: { variant: 'warning', icon: <RotateCcw className="h-3 w-3" /> },
  };

  const { variant, icon } = config[type] || { variant: 'default', icon: null };

  return (
    <Badge variant={variant} size="sm" className="gap-1">
      {icon}
      {type}
    </Badge>
  );
}
