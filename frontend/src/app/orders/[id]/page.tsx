'use client';

import { use, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Badge,
  SectionLoader,
} from '@/components/ui';
import { formatCurrency, formatDateTime } from '@/lib/utils';
import { useOrder, useUpdateOrderStatus, useCancelOrder, useDeleteOrder } from '@/hooks';
import {
  ArrowLeft,
  Package,
  User,
  MapPin,
  CreditCard,
  Truck,
  Clock,
  Phone,
  Mail,
  FileText,
  AlertCircle,
  Edit,
  Trash2,
  X,
  Loader2,
  AlertTriangle,
} from 'lucide-react';

// Statuses that can be edited
const EDITABLE_STATUSES = ['Pending', 'Confirmed', 'Processing'];
// Statuses that can be deleted
const DELETABLE_STATUSES = ['Pending', 'Cancelled'];

// Helper to extract amount from number or MoneyDto
const getAmount = (value: number | { amount: number } | undefined): number => {
  if (value === undefined || value === null) return 0;
  if (typeof value === 'number') return value;
  return value.amount || 0;
};

interface PageProps {
  params: Promise<{ id: string }>;
}

export default function OrderDetailPage({ params }: PageProps) {
  const { id: orderId } = use(params);
  const router = useRouter();
  const { data: order, isLoading, error } = useOrder(orderId);
  const updateStatus = useUpdateOrderStatus();
  const cancelOrder = useCancelOrder();
  const deleteOrder = useDeleteOrder();
  const [showDeleteModal, setShowDeleteModal] = useState(false);

  // Determine if order can be edited/deleted
  const canEdit = order && EDITABLE_STATUSES.includes(order.status);
  const canDelete = order && DELETABLE_STATUSES.includes(order.status);

  if (isLoading) {
    return (
      <DashboardLayout title="Order Details">
        <SectionLoader />
      </DashboardLayout>
    );
  }

  if (error || !order) {
    return (
      <DashboardLayout title="Order Details">
        <Card>
          <CardContent className="py-12 text-center">
            <AlertCircle className="mx-auto h-12 w-12 text-error" />
            <h2 className="mt-4 text-lg font-semibold">Order Not Found</h2>
            <p className="mt-2 text-muted-foreground">
              The order you&apos;re looking for doesn&apos;t exist or you don&apos;t have permission to view it.
            </p>
            <Button className="mt-4" onClick={() => router.push('/orders')}>
              Back to Orders
            </Button>
          </CardContent>
        </Card>
      </DashboardLayout>
    );
  }

  const handleCancelOrder = async () => {
    if (confirm('Are you sure you want to cancel this order?')) {
      try {
        await cancelOrder.mutateAsync({ id: orderId });
      } catch (err) {
        console.error('Failed to cancel order:', err);
      }
    }
  };

  const handleDeleteOrder = async () => {
    try {
      await deleteOrder.mutateAsync(orderId);
      router.push('/orders');
    } catch (err) {
      console.error('Failed to delete order:', err);
    }
  };

  return (
    <DashboardLayout title={`Order ${order.orderNumber}`}>
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/orders" className="rounded p-2 hover:bg-muted">
            <ArrowLeft className="h-5 w-5" />
          </Link>
          <div>
            <h1 className="text-2xl font-bold">{order.orderNumber}</h1>
            <p className="text-sm text-muted-foreground">
              {order.externalOrderNumber && `External: ${order.externalOrderNumber} | `}
              Placed on {formatDateTime(order.orderDate)}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {canEdit && (
            <Link href={`/orders/${orderId}/edit`}>
              <Button variant="outline">
                <Edit className="mr-2 h-4 w-4" />
                Edit Order
              </Button>
            </Link>
          )}
          {order.status === 'Confirmed' && (
            <Button variant="primary">
              <Truck className="mr-2 h-4 w-4" />
              Create Shipment
            </Button>
          )}
          {['Pending', 'Confirmed'].includes(order.status) && (
            <Button
              variant="outline"
              onClick={handleCancelOrder}
              disabled={cancelOrder.isPending}
            >
              Cancel Order
            </Button>
          )}
          {canDelete && (
            <Button
              variant="outline"
              onClick={() => setShowDeleteModal(true)}
              className="text-error hover:text-error hover:bg-error/10"
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Delete
            </Button>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main Content */}
        <div className="space-y-6 lg:col-span-2">
          {/* Order Status */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Clock className="h-5 w-5" />
                Order Status
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex items-center gap-4">
                <StatusBadge status={order.status} />
                <PaymentBadge status={order.paymentStatus} />
                {order.isCOD && (
                  <Badge variant="warning">COD</Badge>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Order Items */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Package className="h-5 w-5" />
                Order Items ({order.itemCount || order.items?.length || 0})
              </CardTitle>
            </CardHeader>
            <CardContent>
              {order.items && order.items.length > 0 ? (
                <div className="space-y-4">
                  {order.items.map((item) => (
                    <div
                      key={item.id}
                      className="flex items-center justify-between border-b pb-4 last:border-0 last:pb-0"
                    >
                      <div className="flex items-center gap-4">
                        <div className="flex h-16 w-16 items-center justify-center rounded bg-muted">
                          <Package className="h-8 w-8 text-muted-foreground" />
                        </div>
                        <div>
                          <p className="font-medium">{item.name}</p>
                          <p className="text-sm text-muted-foreground">
                            SKU: {item.sku}
                            {item.variantName && ` | ${item.variantName}`}
                          </p>
                          <p className="text-sm text-muted-foreground">
                            {formatCurrency(getAmount(item.unitPrice))} x {item.quantity}
                          </p>
                        </div>
                      </div>
                      <p className="font-semibold">
                        {formatCurrency(getAmount(item.unitPrice) * item.quantity)}
                      </p>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-muted-foreground">No items found</p>
              )}
            </CardContent>
          </Card>

          {/* Notes */}
          {order.notes && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5" />
                  Notes
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-muted-foreground">{order.notes}</p>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Order Summary */}
          <Card>
            <CardHeader>
              <CardTitle>Order Summary</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Subtotal</span>
                <span>{formatCurrency(order.subtotal)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Shipping</span>
                <span>{formatCurrency(order.shippingAmount)}</span>
              </div>
              {order.discountAmount > 0 && (
                <div className="flex justify-between text-sm text-success">
                  <span>Discount</span>
                  <span>-{formatCurrency(order.discountAmount)}</span>
                </div>
              )}
              {order.taxAmount > 0 && (
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Tax</span>
                  <span>{formatCurrency(order.taxAmount)}</span>
                </div>
              )}
              <div className="border-t pt-3">
                <div className="flex justify-between font-semibold">
                  <span>Total</span>
                  <span>{formatCurrency(order.totalAmount)}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Customer Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <User className="h-5 w-5" />
                Customer
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <p className="font-medium">{order.customerName}</p>
              {order.customerEmail && (
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Mail className="h-4 w-4" />
                  <a href={`mailto:${order.customerEmail}`} className="hover:text-primary">
                    {order.customerEmail}
                  </a>
                </div>
              )}
              {order.customerPhone && (
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Phone className="h-4 w-4" />
                  <a href={`tel:${order.customerPhone}`} className="hover:text-primary">
                    {order.customerPhone}
                  </a>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Shipping Address */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                Shipping Address
              </CardTitle>
            </CardHeader>
            <CardContent>
              {order.shippingAddress ? (
                <div className="space-y-1 text-sm">
                  <p className="font-medium">{order.shippingAddress.name}</p>
                  <p>{order.shippingAddress.line1}</p>
                  {order.shippingAddress.line2 && <p>{order.shippingAddress.line2}</p>}
                  <p>
                    {order.shippingAddress.city}, {order.shippingAddress.state} {order.shippingAddress.postalCode}
                  </p>
                  <p>{order.shippingAddress.country}</p>
                  {order.shippingAddress.phone && (
                    <p className="mt-2 flex items-center gap-2 text-muted-foreground">
                      <Phone className="h-4 w-4" />
                      {order.shippingAddress.phone}
                    </p>
                  )}
                </div>
              ) : (
                <p className="text-sm text-muted-foreground">
                  {order.shippingCity}, {order.shippingState}
                </p>
              )}
            </CardContent>
          </Card>

          {/* Payment Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                Payment
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Method</span>
                <span className="text-sm font-medium">
                  {order.paymentMethod || (order.isCOD ? 'COD' : 'Prepaid')}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Status</span>
                <PaymentBadge status={order.paymentStatus} />
              </div>
            </CardContent>
          </Card>

          {/* Channel Info */}
          <Card>
            <CardHeader>
              <CardTitle>Channel</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex items-center gap-2">
                <ChannelBadge channel={order.channelType} />
                <span className="text-sm text-muted-foreground">{order.channelName}</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          {/* Backdrop */}
          <div
            className="fixed inset-0 bg-black/50"
            onClick={() => setShowDeleteModal(false)}
          />
          {/* Modal */}
          <div className="relative z-50 w-full max-w-md rounded-lg bg-background p-6 shadow-lg">
            <button
              onClick={() => setShowDeleteModal(false)}
              className="absolute right-4 top-4 rounded p-1 hover:bg-muted"
            >
              <X className="h-4 w-4" />
            </button>
            <div className="flex flex-col items-center text-center">
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-error/10">
                <AlertTriangle className="h-6 w-6 text-error" />
              </div>
              <h3 className="mt-4 text-lg font-semibold">Delete Order</h3>
              <p className="mt-2 text-muted-foreground">
                Are you sure you want to delete order <strong>{order.orderNumber}</strong>?
                This action cannot be undone.
              </p>
              <div className="mt-6 flex gap-3">
                <Button
                  variant="outline"
                  onClick={() => setShowDeleteModal(false)}
                  disabled={deleteOrder.isPending}
                >
                  Cancel
                </Button>
                <Button
                  variant="primary"
                  onClick={handleDeleteOrder}
                  disabled={deleteOrder.isPending}
                  className="bg-error hover:bg-error/90"
                  leftIcon={deleteOrder.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
                >
                  {deleteOrder.isPending ? 'Deleting...' : 'Delete Order'}
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </DashboardLayout>
  );
}

function StatusBadge({ status }: { status: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default' | 'primary'> = {
    Pending: 'warning',
    Confirmed: 'info',
    Processing: 'info',
    Shipped: 'primary',
    Delivered: 'success',
    Cancelled: 'error',
    RTO: 'error',
    Returned: 'error',
  };

  return <Badge variant={variants[status] || 'default'}>{status}</Badge>;
}

function PaymentBadge({ status }: { status: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default'> = {
    Paid: 'success',
    Pending: 'warning',
    Failed: 'error',
    Refunded: 'info',
    PartiallyPaid: 'warning',
    PartiallyRefunded: 'info',
  };

  return <Badge variant={variants[status] || 'default'}>{status}</Badge>;
}

function ChannelBadge({ channel }: { channel: string }) {
  const colors: Record<string, string> = {
    Shopify: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
    Amazon: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200',
    Flipkart: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200',
    Meesho: 'bg-pink-100 text-pink-800 dark:bg-pink-900 dark:text-pink-200',
    WooCommerce: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
  };

  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${colors[channel] || 'bg-gray-100 text-gray-800'}`}>
      {channel}
    </span>
  );
}
