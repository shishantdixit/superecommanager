'use client';

import { useState } from 'react';
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
  ProductSearchInput,
} from '@/components/ui';
import type { SelectedProduct } from '@/components/ui/ProductSearchInput';
import { useCreateOrder, useChannels } from '@/hooks';
import type { CreateOrderRequest, CreateOrderItemInput, CreateAddressInput } from '@/services/orders.service';
import { ArrowLeft, Plus, Trash2, Loader2, AlertCircle, AlertTriangle } from 'lucide-react';

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

// Validation helpers
const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

const isValidIndianPhone = (phone: string): boolean => {
  // Indian phone: 10 digits, optionally starting with +91 or 91
  const cleanPhone = phone.replace(/[\s\-()]/g, '');
  const phoneRegex = /^(\+91|91)?[6-9]\d{9}$/;
  return phoneRegex.test(cleanPhone);
};

const isValidPinCode = (pin: string): boolean => {
  // Indian PIN code: exactly 6 digits, first digit 1-9
  const pinRegex = /^[1-9]\d{5}$/;
  return pinRegex.test(pin.trim());
};

const MAX_QUANTITY = 9999;
const MAX_UNIT_PRICE = 10000000; // 1 crore

export default function CreateOrderPage() {
  const router = useRouter();
  const createOrder = useCreateOrder();
  const { data: channels, isLoading: channelsLoading } = useChannels();

  const [selectedChannelId, setSelectedChannelId] = useState<string>('');
  const [customerName, setCustomerName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');
  const [shippingAddress, setShippingAddress] = useState<CreateAddressInput>({ ...defaultAddress });
  const [items, setItems] = useState<CreateOrderItemInput[]>([{ ...defaultItem }]);
  const [paymentMethod, setPaymentMethod] = useState<CreateOrderRequest['paymentMethod']>('COD');
  const [paymentStatus, setPaymentStatus] = useState<CreateOrderRequest['paymentStatus']>('Pending');
  const [shippingAmount, setShippingAmount] = useState(0);
  const [discountAmount, setDiscountAmount] = useState(0);
  const [customerNotes, setCustomerNotes] = useState('');
  const [internalNotes, setInternalNotes] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [hasSubmitted, setHasSubmitted] = useState(false);

  // Build channel options - connected channels + Manual option
  const channelOptions = [
    { value: '', label: 'Manual Order (No Channel)' },
    ...(channels || [])
      .filter((ch) => ch.isConnected || ch.type === 'Custom')
      .map((ch) => ({
        value: ch.id,
        label: `${ch.name}${ch.type !== 'Custom' ? ` (${ch.type})` : ''}`,
      })),
  ];

  // Get selected channel info for showing warnings
  const selectedChannel = channels?.find((ch) => ch.id === selectedChannelId);
  const isPlatformChannel = selectedChannel && selectedChannel.type !== 'Custom';

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

  const handleProductSelect = (index: number, product: SelectedProduct) => {
    const newItems = [...items];
    newItems[index] = {
      ...newItems[index],
      sku: product.sku,
      name: product.name,
      variantName: product.variantName || '',
      unitPrice: product.unitPrice,
    };
    setItems(newItems);
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    // Customer name validation
    if (!customerName.trim()) {
      newErrors.customerName = 'Customer name is required';
    } else if (customerName.trim().length < 2) {
      newErrors.customerName = 'Customer name must be at least 2 characters';
    } else if (customerName.trim().length > 100) {
      newErrors.customerName = 'Customer name must not exceed 100 characters';
    }

    // Phone validation (required for COD, format check if provided)
    const hasPhone = customerPhone.trim() || shippingAddress.phone?.trim();
    if (paymentMethod === 'COD' && !hasPhone) {
      newErrors.customerPhone = 'Phone number is required for Cash on Delivery orders';
    } else if (customerPhone.trim() && !isValidIndianPhone(customerPhone)) {
      newErrors.customerPhone = 'Please enter a valid 10-digit Indian phone number';
    }

    // Email validation (optional but must be valid if provided)
    if (customerEmail.trim() && !isValidEmail(customerEmail)) {
      newErrors.customerEmail = 'Please enter a valid email address';
    }

    // Shipping address validations
    if (!shippingAddress.name.trim()) {
      newErrors.addressName = 'Recipient name is required';
    } else if (shippingAddress.name.trim().length < 2) {
      newErrors.addressName = 'Recipient name must be at least 2 characters';
    }

    if (!shippingAddress.line1.trim()) {
      newErrors.addressLine1 = 'Address line 1 is required';
    } else if (shippingAddress.line1.trim().length < 5) {
      newErrors.addressLine1 = 'Address must be at least 5 characters';
    } else if (shippingAddress.line1.trim().length > 200) {
      newErrors.addressLine1 = 'Address must not exceed 200 characters';
    }

    if (!shippingAddress.city.trim()) {
      newErrors.addressCity = 'City is required';
    } else if (shippingAddress.city.trim().length < 2) {
      newErrors.addressCity = 'City name must be at least 2 characters';
    }

    if (!shippingAddress.state) {
      newErrors.addressState = 'State is required';
    }

    if (!shippingAddress.postalCode.trim()) {
      newErrors.addressPostalCode = 'PIN code is required';
    } else if (!isValidPinCode(shippingAddress.postalCode)) {
      newErrors.addressPostalCode = 'Please enter a valid 6-digit PIN code';
    }

    // Shipping address phone validation
    if (shippingAddress.phone?.trim() && !isValidIndianPhone(shippingAddress.phone)) {
      newErrors.addressPhone = 'Please enter a valid 10-digit phone number';
    }

    // Check for duplicate SKUs
    const skus = items.map(item => item.sku.trim().toUpperCase()).filter(sku => sku);
    const duplicateSkus = skus.filter((sku, index) => skus.indexOf(sku) !== index);
    if (duplicateSkus.length > 0) {
      newErrors.duplicateSku = `Duplicate SKU found: ${[...new Set(duplicateSkus)].join(', ')}`;
    }

    // Validate items
    items.forEach((item, index) => {
      if (!item.sku.trim()) {
        newErrors[`item${index}Sku`] = 'SKU is required';
      } else if (item.sku.trim().length > 100) {
        newErrors[`item${index}Sku`] = 'SKU must not exceed 100 characters';
      }

      if (!item.name.trim()) {
        newErrors[`item${index}Name`] = 'Product name is required';
      } else if (item.name.trim().length > 200) {
        newErrors[`item${index}Name`] = 'Product name must not exceed 200 characters';
      }

      if (item.quantity <= 0) {
        newErrors[`item${index}Quantity`] = 'Quantity must be at least 1';
      } else if (item.quantity > MAX_QUANTITY) {
        newErrors[`item${index}Quantity`] = `Quantity cannot exceed ${MAX_QUANTITY}`;
      } else if (!Number.isInteger(item.quantity)) {
        newErrors[`item${index}Quantity`] = 'Quantity must be a whole number';
      }

      if (item.unitPrice <= 0) {
        newErrors[`item${index}UnitPrice`] = 'Unit price must be greater than 0';
      } else if (item.unitPrice > MAX_UNIT_PRICE) {
        newErrors[`item${index}UnitPrice`] = 'Unit price exceeds maximum allowed value';
      }

      // Discount validation
      const lineTotal = item.unitPrice * item.quantity;
      if (item.discountAmount < 0) {
        newErrors[`item${index}Discount`] = 'Discount cannot be negative';
      } else if (item.discountAmount > lineTotal) {
        newErrors[`item${index}Discount`] = 'Discount cannot exceed line total';
      }

      // Tax validation
      if (item.taxAmount < 0) {
        newErrors[`item${index}Tax`] = 'Tax cannot be negative';
      }
    });

    // Order level discount validation
    if (discountAmount < 0) {
      newErrors.orderDiscount = 'Order discount cannot be negative';
    } else if (discountAmount > subtotal) {
      newErrors.orderDiscount = 'Order discount cannot exceed subtotal';
    }

    // Shipping amount validation
    if (shippingAmount < 0) {
      newErrors.shippingAmount = 'Shipping amount cannot be negative';
    }

    // Total validation
    if (total <= 0) {
      newErrors.orderTotal = 'Order total must be greater than zero';
    }

    setErrors(newErrors);

    // Scroll to first error
    if (Object.keys(newErrors).length > 0) {
      const firstErrorKey = Object.keys(newErrors)[0];
      const errorElement = document.querySelector(`[data-error="${firstErrorKey}"]`);
      if (errorElement) {
        errorElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }
    }

    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setHasSubmitted(true);

    if (!validateForm()) {
      return;
    }

    const orderData: CreateOrderRequest = {
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
      channelId: selectedChannelId || undefined,
    };

    try {
      const result = await createOrder.mutateAsync(orderData);
      router.push(`/orders/${result.id}`);
    } catch (error) {
      console.error('Failed to create order:', error);
    }
  };

  return (
    <DashboardLayout title="Create Order">
      <Link
        href="/orders"
        className="mb-4 inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to Orders
      </Link>

      <form onSubmit={handleSubmit}>
        {/* Validation Summary */}
        {hasSubmitted && Object.keys(errors).length > 0 && (
          <div className="mb-6 rounded-lg border border-error/50 bg-error/10 p-4" data-error="summary">
            <div className="flex items-start gap-3">
              <AlertTriangle className="h-5 w-5 text-error flex-shrink-0 mt-0.5" />
              <div>
                <h3 className="font-medium text-error">Please fix the following errors:</h3>
                <ul className="mt-2 list-disc list-inside text-sm text-error/80 space-y-1">
                  {Object.values(errors).map((error, index) => (
                    <li key={index}>{error}</li>
                  ))}
                </ul>
              </div>
            </div>
          </div>
        )}

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
                  <div data-error="customerName">
                    <label className="mb-1.5 block text-sm font-medium">
                      Customer Name <span className="text-error">*</span>
                    </label>
                    <Input
                      value={customerName}
                      onChange={(e) => setCustomerName(e.target.value)}
                      placeholder="Enter customer name"
                      error={errors.customerName}
                      maxLength={100}
                    />
                  </div>
                  <div data-error="customerPhone">
                    <label className="mb-1.5 block text-sm font-medium">
                      Phone Number {paymentMethod === 'COD' && <span className="text-error">*</span>}
                    </label>
                    <Input
                      value={customerPhone}
                      onChange={(e) => setCustomerPhone(e.target.value)}
                      placeholder="10-digit mobile number"
                      error={errors.customerPhone}
                      maxLength={15}
                    />
                  </div>
                </div>
                <div data-error="customerEmail">
                  <label className="mb-1.5 block text-sm font-medium">Email</label>
                  <Input
                    type="email"
                    value={customerEmail}
                    onChange={(e) => setCustomerEmail(e.target.value)}
                    placeholder="Enter email address"
                    error={errors.customerEmail}
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
                  <div data-error="addressName">
                    <label className="mb-1.5 block text-sm font-medium">
                      Recipient Name <span className="text-error">*</span>
                    </label>
                    <Input
                      value={shippingAddress.name}
                      onChange={(e) => handleAddressChange('name', e.target.value)}
                      placeholder="Full name"
                      error={errors.addressName}
                      maxLength={100}
                    />
                  </div>
                  <div data-error="addressPhone">
                    <label className="mb-1.5 block text-sm font-medium">Phone</label>
                    <Input
                      value={shippingAddress.phone || ''}
                      onChange={(e) => handleAddressChange('phone', e.target.value)}
                      placeholder="10-digit mobile number"
                      error={errors.addressPhone}
                      maxLength={15}
                    />
                  </div>
                </div>
                <div data-error="addressLine1">
                  <label className="mb-1.5 block text-sm font-medium">
                    Address Line 1 <span className="text-error">*</span>
                  </label>
                  <Input
                    value={shippingAddress.line1}
                    onChange={(e) => handleAddressChange('line1', e.target.value)}
                    placeholder="House/Flat No., Building, Street"
                    error={errors.addressLine1}
                    maxLength={200}
                  />
                </div>
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Address Line 2</label>
                  <Input
                    value={shippingAddress.line2 || ''}
                    onChange={(e) => handleAddressChange('line2', e.target.value)}
                    placeholder="Landmark, Area (Optional)"
                    maxLength={200}
                  />
                </div>
                <div className="grid gap-4 sm:grid-cols-3">
                  <div data-error="addressCity">
                    <label className="mb-1.5 block text-sm font-medium">
                      City <span className="text-error">*</span>
                    </label>
                    <Input
                      value={shippingAddress.city}
                      onChange={(e) => handleAddressChange('city', e.target.value)}
                      placeholder="City"
                      error={errors.addressCity}
                      maxLength={100}
                    />
                  </div>
                  <div data-error="addressState">
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
                  <div data-error="addressPostalCode">
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
                {errors.duplicateSku && (
                  <div className="rounded-md bg-error/10 p-3 text-sm text-error flex items-center gap-2">
                    <AlertCircle className="h-4 w-4 flex-shrink-0" />
                    {errors.duplicateSku}
                  </div>
                )}
                {items.map((item, index) => (
                  <div key={index} className="rounded-lg border p-4" data-error={`item${index}Sku`}>
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

                    {/* Product Search */}
                    <div className="mb-4">
                      <label className="mb-1.5 block text-sm font-medium">
                        Search Product
                      </label>
                      <ProductSearchInput
                        onSelect={(product) => handleProductSelect(index, product)}
                        placeholder="Search by product name or SKU..."
                        channelId={selectedChannelId || undefined}
                      />
                      <p className="mt-1 text-xs text-muted-foreground">
                        {selectedChannelId
                          ? `Showing products from ${selectedChannel?.name || 'selected channel'}. Select "Manual Order" to see all products.`
                          : 'Search and select a product to auto-fill details, or enter manually below'}
                      </p>
                    </div>

                    <div className="grid gap-4 sm:grid-cols-2">
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">
                          SKU <span className="text-error">*</span>
                        </label>
                        <Input
                          value={item.sku}
                          onChange={(e) => handleItemChange(index, 'sku', e.target.value.toUpperCase())}
                          placeholder="Product SKU"
                          error={errors[`item${index}Sku`]}
                          maxLength={100}
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
                          maxLength={200}
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
                          max={MAX_QUANTITY}
                          value={item.quantity}
                          onChange={(e) => handleItemChange(index, 'quantity', Math.min(parseInt(e.target.value) || 1, MAX_QUANTITY))}
                          error={errors[`item${index}Quantity`]}
                        />
                      </div>
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">
                          Unit Price (₹) <span className="text-error">*</span>
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
                        <label className="mb-1.5 block text-sm font-medium">Discount (₹)</label>
                        <Input
                          type="number"
                          min="0"
                          step="0.01"
                          value={item.discountAmount}
                          onChange={(e) => handleItemChange(index, 'discountAmount', parseFloat(e.target.value) || 0)}
                          error={errors[`item${index}Discount`]}
                        />
                      </div>
                      <div>
                        <label className="mb-1.5 block text-sm font-medium">Tax (₹)</label>
                        <Input
                          type="number"
                          min="0"
                          step="0.01"
                          value={item.taxAmount}
                          onChange={(e) => handleItemChange(index, 'taxAmount', parseFloat(e.target.value) || 0)}
                          error={errors[`item${index}Tax`]}
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
            {/* Channel Selection */}
            <Card>
              <CardHeader>
                <CardTitle>Sales Channel</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Create Order For</label>
                  <Select
                    options={channelOptions}
                    value={selectedChannelId}
                    onChange={(e) => setSelectedChannelId(e.target.value)}
                    disabled={channelsLoading}
                  />
                </div>
                {isPlatformChannel && (
                  <div className="flex items-start gap-2 rounded-md bg-warning/10 p-3 text-sm">
                    <AlertCircle className="h-4 w-4 text-warning mt-0.5 flex-shrink-0" />
                    <div>
                      <p className="font-medium text-warning">Platform Order</p>
                      <p className="text-muted-foreground">
                        This order will also be created on {selectedChannel?.type}.
                        Make sure the products exist in your {selectedChannel?.type} store.
                      </p>
                    </div>
                  </div>
                )}
                {!selectedChannelId && (
                  <p className="text-xs text-muted-foreground">
                    Order will be created as a manual/offline order in your system only.
                  </p>
                )}
              </CardContent>
            </Card>

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
                    onChange={(e) => setPaymentMethod(e.target.value as CreateOrderRequest['paymentMethod'])}
                  />
                </div>
                <div>
                  <label className="mb-1.5 block text-sm font-medium">Payment Status</label>
                  <Select
                    options={paymentStatusOptions}
                    value={paymentStatus}
                    onChange={(e) => setPaymentStatus(e.target.value as CreateOrderRequest['paymentStatus'])}
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
                  <div data-error="shippingAmount">
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground">Shipping</span>
                      <Input
                        type="number"
                        min="0"
                        step="0.01"
                        value={shippingAmount}
                        onChange={(e) => setShippingAmount(Math.max(0, parseFloat(e.target.value) || 0))}
                        className="w-24 text-right"
                      />
                    </div>
                    {errors.shippingAmount && (
                      <p className="text-xs text-error mt-1 text-right">{errors.shippingAmount}</p>
                    )}
                  </div>
                  <div data-error="orderDiscount">
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground">Discount</span>
                      <Input
                        type="number"
                        min="0"
                        step="0.01"
                        value={discountAmount}
                        onChange={(e) => setDiscountAmount(Math.max(0, parseFloat(e.target.value) || 0))}
                        className="w-24 text-right"
                      />
                    </div>
                    {errors.orderDiscount && (
                      <p className="text-xs text-error mt-1 text-right">{errors.orderDiscount}</p>
                    )}
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Tax</span>
                    <span>₹{totalTax.toFixed(2)}</span>
                  </div>
                  <div className="border-t pt-2" data-error="orderTotal">
                    <div className="flex justify-between text-base font-semibold">
                      <span>Total</span>
                      <span className={total <= 0 ? 'text-error' : ''}>₹{total.toFixed(2)}</span>
                    </div>
                    {errors.orderTotal && (
                      <p className="text-xs text-error mt-1 text-right">{errors.orderTotal}</p>
                    )}
                  </div>
                </div>

                {createOrder.error && (
                  <div className="rounded-md bg-error/10 p-3 text-sm text-error">
                    Failed to create order. Please try again.
                  </div>
                )}

                {hasSubmitted && Object.keys(errors).length > 0 && (
                  <div className="rounded-md bg-warning/10 p-3 text-sm text-warning flex items-center gap-2">
                    <AlertTriangle className="h-4 w-4 flex-shrink-0" />
                    Please fix {Object.keys(errors).length} validation error{Object.keys(errors).length > 1 ? 's' : ''} above
                  </div>
                )}

                <Button
                  type="submit"
                  className="w-full"
                  disabled={createOrder.isPending}
                  leftIcon={createOrder.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : undefined}
                >
                  {createOrder.isPending ? 'Creating Order...' : 'Create Order'}
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>
      </form>
    </DashboardLayout>
  );
}
