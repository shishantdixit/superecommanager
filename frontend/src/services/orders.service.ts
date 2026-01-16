import { get, post, put } from '@/lib/api-client';
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
};
