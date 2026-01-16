import { get, post, put } from '@/lib/api-client';
import type { NdrCase, NdrFilters, NdrStatus, PaginatedResponse, Address, ApiResponse } from '@/types/api';

export interface NdrStats {
  totalCases: number;
  openCases: number;
  inProgressCases: number;
  resolvedCases: number;
  rtoInitiatedCases: number;
  avgResolutionDays: number;
  resolutionRate: number;
  reattemptSuccessRate: number;
}

export interface AssignNdrRequest {
  userId: string;
}

export interface LogActionRequest {
  actionType: 'Call' | 'SMS' | 'WhatsApp' | 'Email';
  outcome?: string;
  remarks?: string;
  nextFollowUpAt?: string;
}

export interface AddRemarkRequest {
  content: string;
  isInternal: boolean;
}

export interface ScheduleReattemptRequest {
  reattemptDate: string;
  updatedAddress?: Address;
  remarks?: string;
}

export interface UpdateOutcomeRequest {
  status: NdrStatus;
  resolution?: string;
  remarks?: string;
}

/**
 * Transform frontend pagination params to backend format.
 */
function transformParams(params: NdrFilters) {
  const { pageNumber, ...rest } = params;
  return {
    ...rest,
    page: pageNumber,
  };
}

export const ndrService = {
  /**
   * Get paginated NDR cases with filters.
   */
  getNdrCases: async (params: NdrFilters = {}) => {
    const response = await get<ApiResponse<PaginatedResponse<NdrCase>>>('/ndr', { params: transformParams(params) });
    return response.data;
  },

  /**
   * Get NDR case by ID.
   */
  getNdrCaseById: async (id: string) => {
    const response = await get<ApiResponse<NdrCase>>(`/ndr/${id}`);
    return response.data;
  },

  /**
   * Get NDR statistics.
   */
  getStats: async (fromDate?: string, toDate?: string) => {
    const response = await get<ApiResponse<NdrStats>>('/ndr/stats', {
      params: { fromDate, toDate },
    });
    return response.data;
  },

  /**
   * Get NDR cases for a specific shipment.
   */
  getNdrByShipment: async (shipmentId: string) => {
    const response = await get<ApiResponse<NdrCase[]>>(`/ndr/by-shipment/${shipmentId}`);
    return response.data;
  },

  /**
   * Get NDR cases assigned to current user.
   */
  getMyCases: async (params: NdrFilters = {}) => {
    const response = await get<ApiResponse<PaginatedResponse<NdrCase>>>('/ndr/my-cases', { params: transformParams(params) });
    return response.data;
  },

  /**
   * Assign NDR case to a user.
   */
  assignCase: async (id: string, data: AssignNdrRequest) => {
    const response = await post<ApiResponse<NdrCase>, AssignNdrRequest>(`/ndr/${id}/assign`, data);
    return response.data;
  },

  /**
   * Log action (call, SMS, WhatsApp, Email) with outcome.
   */
  logAction: async (id: string, data: LogActionRequest) => {
    const response = await post<ApiResponse<NdrCase>, LogActionRequest>(`/ndr/${id}/actions`, data);
    return response.data;
  },

  /**
   * Add remark/note to NDR case.
   */
  addRemark: async (id: string, data: AddRemarkRequest) => {
    const response = await post<ApiResponse<NdrCase>, AddRemarkRequest>(`/ndr/${id}/remarks`, data);
    return response.data;
  },

  /**
   * Schedule reattempt with updated address.
   */
  scheduleReattempt: async (id: string, data: ScheduleReattemptRequest) => {
    const response = await post<ApiResponse<NdrCase>, ScheduleReattemptRequest>(`/ndr/${id}/reattempt`, data);
    return response.data;
  },

  /**
   * Update NDR resolution/outcome.
   */
  updateOutcome: async (id: string, data: UpdateOutcomeRequest) => {
    const response = await put<ApiResponse<NdrCase>, UpdateOutcomeRequest>(`/ndr/${id}/outcome`, data);
    return response.data;
  },
};
