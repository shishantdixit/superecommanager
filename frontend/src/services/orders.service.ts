import { get, post, put, del } from '@/lib/api-client';
import type { Order, OrderFilters, PaginatedResponse, OrderStatus, ApiResponse } from '@/types/api';

export interface OrderStats {
  totalOrders: number;
  totalRevenue: number;
  averageOrderValue: number;
  pendingOrders: number;
  confirmedOrders: number;
  shippedOrders: number;
  deliveredOrders: number;
  cancelledOrders: number;
  returnedOrders: number;
}

export interface UpdateOrderStatusRequest {
  status: OrderStatus;
  remarks?: string;
}

export interface UpdateOrderNotesRequest {
  notes: string;
}

export interface BulkUpdateRequest {
  orderIds: string[];
  status?: OrderStatus;
  notes?: string;
}

export interface CreateAddressInput {
  name: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone?: string;
}

export interface CreateOrderItemInput {
  sku: string;
  name: string;
  variantName?: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  taxAmount: number;
}

export interface CreateOrderRequest {
  customerName: string;
  customerEmail?: string;
  customerPhone?: string;
  shippingAddress: CreateAddressInput;
  billingAddress?: CreateAddressInput;
  items: CreateOrderItemInput[];
  paymentMethod: 'COD' | 'UPI' | 'Card' | 'NetBanking' | 'Wallet' | 'EMI' | 'Other';
  paymentStatus: 'Pending' | 'Paid' | 'PartiallyPaid' | 'Failed' | 'Refunded' | 'PartiallyRefunded';
  shippingAmount: number;
  discountAmount: number;
  taxAmount: number;
  currency: string;
  customerNotes?: string;
  internalNotes?: string;
  channelId?: string;
}

export interface UpdateOrderRequest {
  customerName: string;
  customerEmail?: string;
  customerPhone?: string;
  shippingAddress: CreateAddressInput;
  billingAddress?: CreateAddressInput;
  items: CreateOrderItemInput[];
  paymentMethod: 'COD' | 'UPI' | 'Card' | 'NetBanking' | 'Wallet' | 'EMI' | 'Other';
  paymentStatus: 'Pending' | 'Paid' | 'PartiallyPaid' | 'Failed' | 'Refunded' | 'PartiallyRefunded';
  shippingAmount: number;
  discountAmount: number;
  taxAmount: number;
  currency: string;
  customerNotes?: string;
  internalNotes?: string;
  /** Whether to sync changes to the external channel (e.g., Shopify) */
  syncToChannel?: boolean;
}

/**
 * Transform frontend pagination params to backend format.
 * Frontend uses pageNumber, backend expects page.
 */
function transformParams(params: OrderFilters) {
  const { pageNumber, ...rest } = params;
  return {
    ...rest,
    page: pageNumber,
  };
}

export const ordersService = {
  /**
   * Get paginated orders with filters.
   */
  getOrders: async (params: OrderFilters = {}) => {
    const response = await get<ApiResponse<PaginatedResponse<Order>>>('/orders', { params: transformParams(params) });
    return response.data;
  },

  /**
   * Get order by ID.
   */
  getOrderById: async (id: string) => {
    const response = await get<ApiResponse<Order>>(`/orders/${id}`);
    return response.data;
  },

  /**
   * Get order statistics.
   */
  getStats: async (fromDate?: string, toDate?: string) => {
    const response = await get<ApiResponse<OrderStats>>('/orders/stats', {
      params: { fromDate, toDate },
    });
    return response.data;
  },

  /**
   * Update order status.
   */
  updateStatus: async (id: string, data: UpdateOrderStatusRequest) => {
    const response = await put<ApiResponse<Order>, UpdateOrderStatusRequest>(`/orders/${id}/status`, data);
    return response.data;
  },

  /**
   * Update order notes.
   */
  updateNotes: async (id: string, data: UpdateOrderNotesRequest) => {
    const response = await put<ApiResponse<Order>, UpdateOrderNotesRequest>(`/orders/${id}/notes`, data);
    return response.data;
  },

  /**
   * Cancel an order.
   */
  cancelOrder: async (id: string, reason?: string) => {
    const response = await post<ApiResponse<Order>, { reason?: string }>(`/orders/${id}/cancel`, { reason });
    return response.data;
  },

  /**
   * Bulk update multiple orders.
   */
  bulkUpdate: async (data: BulkUpdateRequest) => {
    const response = await post<ApiResponse<{ updatedCount: number }>, BulkUpdateRequest>('/orders/bulk-update', data);
    return response.data;
  },

  /**
   * Advanced filtering with POST body.
   */
  filterOrders: async (filters: OrderFilters & {
    minAmount?: number;
    maxAmount?: number;
    locationIds?: string[];
  }) => {
    const response = await post<ApiResponse<PaginatedResponse<Order>>, typeof filters>('/orders/filter', filters);
    return response.data;
  },

  /**
   * Create a new manual order.
   */
  createOrder: async (data: CreateOrderRequest) => {
    const response = await post<ApiResponse<Order>, CreateOrderRequest>('/orders', data);
    return response.data;
  },

  /**
   * Update an existing order.
   */
  updateOrder: async (id: string, data: UpdateOrderRequest) => {
    const response = await put<ApiResponse<Order>, UpdateOrderRequest>(`/orders/${id}`, data);
    return response.data;
  },

  /**
   * Delete an order (soft delete).
   */
  deleteOrder: async (id: string) => {
    const response = await del<ApiResponse<boolean>>(`/orders/${id}`);
    return response.data;
  },
};
