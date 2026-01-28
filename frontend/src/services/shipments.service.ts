import { get, post, put } from '@/lib/api-client';
import type { Shipment, ShipmentFilters, PaginatedResponse, ShipmentStatus, TrackingEvent, ApiResponse } from '@/types/api';

export interface ShipmentStats {
  totalShipments: number;
  inTransit: number;
  outForDelivery: number;
  delivered: number;
  ndrCases: number;
  rtoInitiated: number;
  avgDeliveryDays: number;
  deliveryRate: number;
}

export interface CreateShipmentRequest {
  orderId: string;
  courierCode: string;
  weight: number;
  length?: number;
  width?: number;
  height?: number;
  pickupDate?: string;
}

export interface UpdateShipmentStatusRequest {
  status: ShipmentStatus;
  location?: string;
  remarks?: string;
}

export interface TrackingResponse {
  awbNumber: string;
  status: ShipmentStatus;
  currentLocation?: string;
  estimatedDeliveryDate?: string;
  events: TrackingEvent[];
}

export interface AvailableCourier {
  courierId: number;
  courierName: string;
  freightCharge: number;
  codCharges: number;
  totalCharge: number;
  estimatedDeliveryDays: string;
  etd?: string;
  rating: number;
  isSurface: boolean;
  isRecommended: boolean;
}

export interface AssignCourierRequest {
  courierId?: number;
}

/**
 * Transform frontend pagination params to backend format.
 */
function transformParams(params: ShipmentFilters) {
  const { pageNumber, ...rest } = params;
  return {
    ...rest,
    page: pageNumber,
  };
}

export const shipmentsService = {
  /**
   * Get paginated shipments with filters.
   */
  getShipments: async (params: ShipmentFilters = {}) => {
    const response = await get<ApiResponse<PaginatedResponse<Shipment>>>('/shipments', { params: transformParams(params) });
    return response.data;
  },

  /**
   * Get shipment by ID.
   */
  getShipmentById: async (id: string) => {
    const response = await get<ApiResponse<Shipment>>(`/shipments/${id}`);
    return response.data;
  },

  /**
   * Get shipment tracking information.
   */
  getTracking: async (id: string) => {
    const response = await get<ApiResponse<TrackingResponse>>(`/shipments/${id}/tracking`);
    return response.data;
  },

  /**
   * Get shipment statistics.
   */
  getStats: async (fromDate?: string, toDate?: string) => {
    const response = await get<ApiResponse<ShipmentStats>>('/shipments/stats', {
      params: { fromDate, toDate },
    });
    return response.data;
  },

  /**
   * Get all shipments for a specific order.
   */
  getShipmentsByOrder: async (orderId: string) => {
    const response = await get<ApiResponse<Shipment[]>>(`/shipments/order/${orderId}`);
    return response.data;
  },

  /**
   * Create a new shipment for an order.
   */
  createShipment: async (data: CreateShipmentRequest) => {
    const response = await post<ApiResponse<Shipment>, CreateShipmentRequest>('/shipments', data);
    return response.data;
  },

  /**
   * Update shipment status.
   */
  updateStatus: async (id: string, data: UpdateShipmentStatusRequest) => {
    const response = await put<ApiResponse<Shipment>, UpdateShipmentStatusRequest>(`/shipments/${id}/status`, data);
    return response.data;
  },

  /**
   * Cancel a shipment.
   */
  cancelShipment: async (id: string, reason?: string) => {
    const response = await post<ApiResponse<Shipment>, { reason?: string }>(`/shipments/${id}/cancel`, { reason });
    return response.data;
  },

  /**
   * Advanced filtering with POST body.
   */
  filterShipments: async (filters: ShipmentFilters & {
    isCod?: boolean;
    locationIds?: string[];
  }) => {
    const response = await post<ApiResponse<PaginatedResponse<Shipment>>, typeof filters>('/shipments/filter', filters);
    return response.data;
  },

  /**
   * Get available couriers for a shipment (serviceability check).
   */
  getAvailableCouriers: async (shipmentId: string) => {
    const response = await get<ApiResponse<AvailableCourier[]>>(`/shipments/${shipmentId}/available-couriers`);
    return response.data;
  },

  /**
   * Assign a courier to a shipment.
   */
  assignCourier: async (shipmentId: string, courierId?: number) => {
    const response = await post<ApiResponse<Shipment>, AssignCourierRequest>(`/shipments/${shipmentId}/assign-courier`, { courierId });
    return response.data;
  },
};
