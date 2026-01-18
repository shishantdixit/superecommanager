'use client';

import { use, useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
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
  SectionLoader,
  Badge,
} from '@/components/ui';
import { useOrder, useUpdateOrder } from '@/hooks';
import type { UpdateOrderRequest, CreateOrderItemInput, CreateAddressInput } from '@/services/orders.service';
import { ArrowLeft, Plus, Trash2, Loader2, AlertCircle, AlertTriangle } from 'lucide-react';

interface PageProps {
  params: Promise<{ id: string }>;
}

const paymentMethodOptions = [
  { value: 'COD', label: 'Cash on Delivery' },
  { value: 'UPI', label: 'UPI' },
  { value: 'Card', label: 'Credit/Debit Card' },
  { value: 'NetBanking', label: 'Net Banking' },
  { value: 'Wallet', label: 'Wallet' },
  { value: 'Other', label: 'Other' },
];

const paymentStatusOptions = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Paid', label: 'Paid' },
  { value: 'PartiallyPaid', label: 'Partially Paid' },
  { value: 'Failed', label: 'Failed' },
  { value: 'Refunded', label: 'Refunded' },
];

const stateOptions = [
  { value: '', label: 'Select State' },
  { value: 'Andhra Pradesh', label: 'Andhra Pradesh' },
  { value: 'Arunachal Pradesh', label: 'Arunachal Pradesh' },
  { value: 'Assam', label: 'Assam' },
  { value: 'Bihar', label: 'Bihar' },
  { value: 'Chhattisgarh', label: 'Chhattisgarh' },
  { value: 'Delhi', label: 'Delhi' },
  { value: 'Goa', label: 'Goa' },
  { value: 'Gujarat', label: 'Gujarat' },
  { value: 'Haryana', label: 'Haryana' },
  { value: 'Himachal Pradesh', label: 'Himachal Pradesh' },
  { value: 'Jharkhand', label: 'Jharkhand' },
  { value: 'Karnataka', label: 'Karnataka' },
  { value: 'Kerala', label: 'Kerala' },
  { value: 'Madhya Pradesh', label: 'Madhya Pradesh' },
  { value: 'Maharashtra', label: 'Maharashtra' },
  { value: 'Manipur', label: 'Manipur' },
  { value: 'Meghalaya', label: 'Meghalaya' },
  { value: 'Mizoram', label: 'Mizoram' },
  { value: 'Nagaland', label: 'Nagaland' },
  { value: 'Odisha', label: 'Odisha' },
  { value: 'Punjab', label: 'Punjab' },
  { value: 'Rajasthan', label: 'Rajasthan' },
  { value: 'Sikkim', label: 'Sikkim' },
  { value: 'Tamil Nadu', label: 'Tamil Nadu' },
  { value: 'Telangana', label: 'Telangana' },
  { value: 'Tripura', label: 'Tripura' },
  { value: 'Uttar Pradesh', label: 'Uttar Pradesh' },
  { value: 'Uttarakhand', label: 'Uttarakhand' },
  { value: 'West Bengal', label: 'West Bengal' },
];

const defaultItem: CreateOrderItemInput = {
  sku: '',
  name: '',
  variantName: '',
  quantity: 1,
  unitPrice: 0,
  discountAmount: 0,
  taxAmount: 0,
};

const defaultAddress: CreateAddressInput = {
  name: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'India',
  phone: '',
};

// Statuses that cannot be edited
const NON_EDITABLE_STATUSES = ['Shipped', 'Delivered', 'Cancelled', 'Returned', 'RTO'];

export default function EditOrderPage({ params }: PageProps) {
  const { id: orderId } = use(params);
  const router = useRouter();
  const { data: order, isLoading: orderLoading, error: orderError } = useOrder(orderId);
  const updateOrder = useUpdateOrder();

  const [customerName, setCustomerName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');
  const [shippingAddress, setShippingAddress] = useState<CreateAddressInput>({ ...defaultAddress });
  const [items, setItems] = useState<CreateOrderItemInput[]>([{ ...defaultItem }]);
  const [paymentMethod, setPaymentMethod] = useState<UpdateOrderRequest['paymentMethod']>('COD');
  const [paymentStatus, setPaymentStatus] = useState<UpdateOrderRequest['paymentStatus']>('Pending');
  const [shippingAmount, setShippingAmount] = useState(0);
  const [discountAmount, setDiscountAmount] = useState(0);
  const [customerNotes, setCustomerNotes] = useState('');
  const [internalNotes, setInternalNotes] = useState('');
  const [syncToChannel, setSyncToChannel] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isInitialized, setIsInitialized] = useState(false);

  // Check if order originated from an external channel
  const hasExternalChannel = order && order.externalOrderId && order.channelType && order.channelType !== 'Custom';

  // Initialize form with order data
  useEffect(() => {
    if (order && !isInitialized) {
      setCustomerName(order.customerName || '');
      setCustomerEmail(order.customerEmail || '');
      setCustomerPhone(order.customerPhone || '');

      if (order.shippingAddress) {
        setShippingAddress({
          name: order.shippingAddress.name || '',
          line1: order.shippingAddress.line1 || '',
          line2: order.shippingAddress.line2 || '',
          city: order.shippingAddress.city || '',
          state: order.shippingAddress.state || '',
          postalCode: order.shippingAddress.postalCode || '',
          country: order.shippingAddress.country || 'India',
          phone: order.shippingAddress.phone || '',
        });
      }

      if (order.items && order.items.length > 0) {
        // Helper to extract amount from number or MoneyDto
        const getAmount = (value: number | { amount: number } | undefined): number => {
          if (value === undefined || value === null) return 0;
          if (typeof value === 'number') return value;
          return value.amount || 0;
        };

        setItems(
          order.items.map((item) => ({
            sku: item.sku || '',
            name: item.name || '',
            variantName: item.variantName || '',
            quantity: item.quantity || 1,
            unitPrice: getAmount(item.unitPrice),
            discountAmount: getAmount(item.discountAmount),
            taxAmount: getAmount(item.taxAmount),
          }))
        );
      }

      setPaymentMethod((order.paymentMethod || 'COD') as UpdateOrderRequest['paymentMethod']);
      setPaymentStatus((order.paymentStatus || 'Pending') as UpdateOrderRequest['paymentStatus']);
      setShippingAmount(order.shippingAmount || 0);
      setDiscountAmount(order.discountAmount || 0);
      setCustomerNotes(order.customerNotes || '');
      setInternalNotes(order.internalNotes || '');

      setIsInitialized(true);
    }
  }, [order, isInitialized]);

  // Check if order can be edited
  const canEdit = order && !NON_EDITABLE_STATUSES.includes(order.status);

  // Calculate totals
  const subtotal = items.reduce((sum, item) => sum + (item.unitPrice * item.quantity - item.discountAmount), 0);
  const totalTax = items.reduce((sum, item) => sum + item.taxAmount, 0);
  const total = subtotal - discountAmount + totalTax + shippingAmount;

  const handleAddItem = () => {
    setItems([...items, { ...defaultItem }]);
  };

  const handleRemoveItem = (index: number) => {
    if (items.length > 1) {
      setItems(items.filter((_, i) => i !== index));
    }
  };

  const handleItemChange = (index: number, field: keyof CreateOrderItemInput, value: string | number) => {
    const newItems = [...items];
    newItems[index] = { ...newItems[index], [field]: value };
    setItems(newItems);
  };

  const handleAddressChange = (field: keyof CreateAddressInput, value: string) => {
    setShippingAddress({ ...shippingAddress, [field]: value });
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!customerName.trim()) {
      newErrors.customerName = 'Customer name is required';
    }

    if (!shippingAddress.name.trim()) {
      newErrors.addressName = 'Recipient name is required';
    }
    if (!shippingAddress.line1.trim()) {
      newErrors.addressLine1 = 'Address line 1 is required';
    }
    if (!shippingAddress.city.trim()) {
      newErrors.addressCity = 'City is required';
    }
    if (!shippingAddress.state) {
      newErrors.addressState = 'State is required';
    }
    if (!shippingAddress.postalCode.trim()) {
      newErrors.addressPostalCode = 'PIN code is required';
    }

    // Validate items
    items.forEach((item, index) => {
      if (!item.sku.trim()) {
        newErrors[`item${index}Sku`] = 'SKU is required';
      }
      if (!item.name.trim()) {
        newErrors[`item${index}Name`] = 'Product name is required';
      }
      if (item.quantity <= 0) {
        newErrors[`item${index}Quantity`] = 'Quantity must be at least 1';
      }
      if (item.unitPrice <= 0) {
        newErrors[`item${index}UnitPrice`] = 'Unit price must be greater than 0';
      }
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    const orderData: UpdateOrderRequest = {
      customerName,
      customerEmail: customerEmail || undefined,
      customerPhone: customerPhone || undefined,
      shippingAddress: {
        ...shippingAddress,
        phone: shippingAddress.phone || customerPhone || undefined,
      },
      items,
      paymentMethod,
      paymentStatus,
      shippingAmount,
      discountAmount,
      taxAmount: totalTax,
      currency: 'INR',
      customerNotes: customerNotes || undefined,
      internalNotes: internalNotes || undefined,
      syncToChannel: hasExternalChannel ? syncToChannel : undefined,
    };

    try {
      await updateOrder.mutateAsync({ id: orderId, data: orderData });
      router.push(`/orders/${orderId}`);
    } catch (error) {
      console.error('Failed to update order:', error);
    }
  };

  if (orderLoading) {
    return (
      <DashboardLayout title="Edit Order">
        <SectionLoader />
      </DashboardLayout>
    );
  }

  if (orderError || !order) {
    return (
      <DashboardLayout title="Edit Order">
        <Card>
          <CardContent className="py-12 text-center">
            <AlertCircle className="mx-auto h-12 w-12 text-error" />
            <h2 className="mt-4 text-lg font-semibold">Order Not Found</h2>
            <p className="mt-2 text-muted-foreground">
              The order you&apos;re trying to edit doesn&apos;t exist.
            </p>
            <Button className="mt-4" onClick={() => router.push('/orders')}>
              Back to Orders
            </Button>
          </CardContent>
        </Card>
      </DashboardLayout>
    );
  }

  if (!canEdit) {
    return (
      <DashboardLayout title="Edit Order">
        <Card>
          <CardContent className="py-12 text-center">
            <AlertTriangle className="mx-auto h-12 w-12 text-warning" />
            <h2 className="mt-4 text-lg font-semibold">Cannot Edit Order</h2>
            <p className="mt-2 text-muted-foreground">
              Orders with status <Badge variant="default">{order.status}</Badge> cannot be edited.
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Only orders in Pending, Confirmed, or Processing status can be modified.
            </p>
            <Button className="mt-4" onClick={() => router.push(`/orders/${orderId}`)}>
              Back to Order Details
            </Button>
          </CardContent>
        </Card>
      </DashboardLayout>
    );
  }

  return (
    <DashboardLayout title={`Edit Order ${order.orderNumber}`}>
      <div className="mb-4 flex items-center justify-between">
        <Link
          href={`/orders/${orderId}`}
          className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Order Details
        </Link>
        <Badge variant="info">{order.status}</Badge>
      </div>

      <form onSubmit={handleSubmit}>
        <div className="grid gap-6 lg:grid-cols-3">
          {/* Main Content */}
          <div className="space-y-6 lg:col-span-2">
            {/* Customer Information */}
            <Card>
              <CardHeader>
                <CardTitle>Customer Information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1.5 block text-sm font-medium">
                      Customer Name <span className="text-error">*</span>
                    </label>
                    <Input
                      value={customerName}
                      onChange={(e) => setCustomerName(e.target.value)}
                      placeholder="Enter customer name"
                      error={errors.customerName}
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium">Phone Number</label>
                    <Input
                      value={customerPhone}
                      onChange={(e) => setCustomerPhone(e.target.value)}
                      placeholder="Enter phone number"
                    />
                  </div>
                </div>
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Email</label>
                  <Input
                    type="email"
                    value={customerEmail}
                    onChange={(e) => setCustomerEmail(e.target.value)}
                    placeholder="Enter email address"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Shipping Address */}
            <Card>
              <CardHeader>
                <CardTitle>Shipping Address</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1.5 block text-sm font-medium">
                      Recipient Name <span className="text-error">*</span>
                    </label>
                    <Input
                      value={shippingAddress.name}
                      onChange={(e) => handleAddressChange('name', e.target.value)}
                      placeholder="Full name"
                      error={errors.addressName}
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium">Phone</label>
                    <Input
                      value={shippingAddress.phone || ''}
                      onChange={(e) => handleAddressChange('phone', e.target.value)}
                      placeholder="Phone number"
                    />
                  </div>
                </div>
                <div>
                  <label className="mb-1.5 block text-sm font-medium">
                    Address Line 1 <span className="text-error">*</span>
                  </label>
                  <Input
                    value={shippingAddress.line1}
                    onChange={(e) => handleAddressChange('line1', e.target.value)}
                    placeholder="House/Flat No., Building, Street"
                    error={errors.addressLine1}
                  />
                </div>
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Address Line 2</label>
                  <Input
                    value={shippingAddress.line2 || ''}
                    onChange={(e) => handleAddressChange('line2', e.target.value)}
                    placeholder="Landmark, Area (Optional)"
                  />
                </div>
                <div className="grid gap-4 sm:grid-cols-3">
                  <div>
                    <label className="mb-1.5 block text-sm font-medium">
                      City <span className="text-error">*</span>
                    </label>
                    <Input
                      value={shippingAddress.city}
                      onChange={(e) => handleAddressChange('city', e.target.value)}
                      placeholder="City"
                      error={errors.addressCity}
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium">
                      State <span className="text-error">*</span>
                    </label>
                    <Select
                      options={stateOptions}
                      value={shippingAddress.state}
                      onChange={(e) => handleAddressChange('state', e.target.value)}
                      error={errors.addressState}
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-sm font-medium">
                      PIN Code <span className="text-error">*</span>
                    </label>
                    <Input
                      value={shippingAddress.postalCode}
                      onChange={(e) => handleAddressChange('postalCode', e.target.value)}
                      placeholder="6-digit PIN"
                      maxLength={6}
                      error={errors.addressPostalCode}
                    />
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Order Items */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle>Order Items</CardTitle>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handleAddItem}
                  leftIcon={<Plus className="h-4 w-4" />}
                >
                  Add Item
                </Button>
              </CardHeader>
              <CardContent className="space-y-4">
                {items.map((item, index) => (
                  <div key={index} className="rounded-lg border p-4">
                    <div className="mb-3 flex items-center justify-between">
                      <span className="text-sm font-medium">Item {index + 1}</span>
                      {items.length > 1 && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => handleRemoveItem(index)}
                          className="text-error hover:text-error"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                    <div className="grid gap-4 sm:grid-cols-2">
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">
                          SKU <span className="text-error">*</span>
                        </label>
                        <Input
                          value={item.sku}
                          onChange={(e) => handleItemChange(index, 'sku', e.target.value)}
                          placeholder="Product SKU"
                          error={errors[`item${index}Sku`]}
                        />
                      </div>
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">
                          Product Name <span className="text-error">*</span>
                        </label>
                        <Input
                          value={item.name}
                          onChange={(e) => handleItemChange(index, 'name', e.target.value)}
                          placeholder="Product name"
                          error={errors[`item${index}Name`]}
                        />
                      </div>
                    </div>
                    <div className="mt-4 grid gap-4 sm:grid-cols-4">
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">
                          Quantity <span className="text-error">*</span>
                        </label>
                        <Input
                          type="number"
                          min="1"
                          value={item.quantity}
                          onChange={(e) => handleItemChange(index, 'quantity', parseInt(e.target.value) || 1)}
                          error={errors[`item${index}Quantity`]}
                        />
                      </div>
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">
                          Unit Price <span className="text-error">*</span>
                        </label>
                        <Input
                          type="number"
                          min="0"
                          step="0.01"
                          value={item.unitPrice}
                          onChange={(e) => handleItemChange(index, 'unitPrice', parseFloat(e.target.value) || 0)}
                          error={errors[`item${index}UnitPrice`]}
                        />
                      </div>
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">Discount</label>
                        <Input
                          type="number"
                          min="0"
                          step="0.01"
                          value={item.discountAmount}
                          onChange={(e) => handleItemChange(index, 'discountAmount', parseFloat(e.target.value) || 0)}
                        />
                      </div>
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">Tax</label>
                        <Input
                          type="number"
                          min="0"
                          step="0.01"
                          value={item.taxAmount}
                          onChange={(e) => handleItemChange(index, 'taxAmount', parseFloat(e.target.value) || 0)}
                        />
                      </div>
                    </div>
                    <div className="mt-2 text-right text-sm text-muted-foreground">
                      Line Total: ₹{((item.unitPrice * item.quantity) - item.discountAmount + item.taxAmount).toFixed(2)}
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>

            {/* Notes */}
            <Card>
              <CardHeader>
                <CardTitle>Notes</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Customer Notes</label>
                  <textarea
                    value={customerNotes}
                    onChange={(e) => setCustomerNotes(e.target.value)}
                    placeholder="Notes from the customer..."
                    rows={2}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Internal Notes</label>
                  <textarea
                    value={internalNotes}
                    onChange={(e) => setInternalNotes(e.target.value)}
                    placeholder="Internal notes (not visible to customer)..."
                    rows={2}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  />
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Payment */}
            <Card>
              <CardHeader>
                <CardTitle>Payment</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Payment Method</label>
                  <Select
                    options={paymentMethodOptions}
                    value={paymentMethod}
                    onChange={(e) => setPaymentMethod(e.target.value as UpdateOrderRequest['paymentMethod'])}
                  />
                </div>
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Payment Status</label>
                  <Select
                    options={paymentStatusOptions}
                    value={paymentStatus}
                    onChange={(e) => setPaymentStatus(e.target.value as UpdateOrderRequest['paymentStatus'])}
                  />
                </div>
              </CardContent>
            </Card>

            {/* Order Summary */}
            <Card>
              <CardHeader>
                <CardTitle>Order Summary</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Subtotal</span>
                    <span>₹{subtotal.toFixed(2)}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">Shipping</span>
                    <Input
                      type="number"
                      min="0"
                      step="0.01"
                      value={shippingAmount}
                      onChange={(e) => setShippingAmount(parseFloat(e.target.value) || 0)}
                      className="w-24 text-right"
                    />
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-muted-foreground">Discount</span>
                    <Input
                      type="number"
                      min="0"
                      step="0.01"
                      value={discountAmount}
                      onChange={(e) => setDiscountAmount(parseFloat(e.target.value) || 0)}
                      className="w-24 text-right"
                    />
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Tax</span>
                    <span>₹{totalTax.toFixed(2)}</span>
                  </div>
                  <div className="border-t pt-2">
                    <div className="flex justify-between text-base font-semibold">
                      <span>Total</span>
                      <span>₹{total.toFixed(2)}</span>
                    </div>
                  </div>
                </div>

                {/* Sync to Channel Option */}
                {hasExternalChannel && (
                  <div className="rounded-md border border-info/30 bg-info/5 p-3">
                    <label className="flex cursor-pointer items-start gap-3">
                      <input
                        type="checkbox"
                        checked={syncToChannel}
                        onChange={(e) => setSyncToChannel(e.target.checked)}
                        className="mt-1 h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
                      />
                      <div className="text-sm">
                        <span className="font-medium">Sync to {order?.channelType}</span>
                        <p className="mt-0.5 text-muted-foreground">
                          Also update this order on {order?.channelType}
                        </p>
                      </div>
                    </label>
                  </div>
                )}

                {updateOrder.error && (
                  <div className="rounded-md bg-error/10 p-3 text-sm text-error">
                    Failed to update order. Please try again.
                  </div>
                )}

                <Button
                  type="submit"
                  className="w-full"
                  disabled={updateOrder.isPending}
                  leftIcon={updateOrder.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : undefined}
                >
                  {updateOrder.isPending ? 'Saving Changes...' : 'Save Changes'}
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>
      </form>
    </DashboardLayout>
  );
}
