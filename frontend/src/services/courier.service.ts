import { apiClient } from '@/lib/api-client';
import type { ApiResponse } from '@/types/api';

export interface CourierAccountDto {
  id: string;
  name: string;
  courierType: string;
  courierTypeName: string;
  isActive: boolean;
  isDefault: boolean;
  isConnected: boolean;
  lastConnectedAt?: string;
  lastError?: string;
  apiUserEmail?: string;
  priority: number;
  supportsCOD: boolean;
  supportsReverse: boolean;
  supportsExpress: boolean;
  createdAt: string;
}

export interface CourierAccountDetailDto extends CourierAccountDto {
  hasApiKey: boolean;
  hasApiSecret: boolean;
  hasAccessToken: boolean;
  accountId?: string;
  channelId?: string;
  webhookUrl?: string;
  settings?: Record<string, any>;
}

export interface CreateCourierAccountRequest {
  name: string;
  courierType: string;
  apiKey?: string;
  apiSecret?: string;
  accessToken?: string;
  accountId?: string;
  channelId?: string;
  isDefault: boolean;
  priority?: number;
}

export interface UpdateCourierAccountRequest {
  name?: string;
  apiKey?: string;
  apiSecret?: string;
  accountId?: string;
  channelId?: string;
  pickupLocation?: string;
  isActive?: boolean;
}

export interface CourierConnectionTestResult {
  isConnected: boolean;
  message?: string;
  accountName?: string;
  accountInfo?: Record<string, string>;
  testedAt: string;
}

export interface CourierWalletBalance {
  balance: number;
  currency: string;
  lastUpdated?: string;
  accountEmail?: string;
}

export interface ShiprocketChannel {
  id: number;
  name: string;
  type?: string;
}

export interface ShiprocketPickupLocation {
  id: number;
  name: string;
  address?: string;
  city?: string;
  state?: string;
  pinCode?: string;
  phone?: string;
  isActive: boolean;
}

class CourierService {
  private readonly baseUrl = '/courieraccounts';

  async getCourierAccounts(params?: { isActive?: boolean; isConnected?: boolean }): Promise<CourierAccountDto[]> {
    const response = await apiClient.get<ApiResponse<CourierAccountDto[]>>(this.baseUrl, { params });
    return response.data.data;
  }

  async getActiveCourierAccounts(): Promise<CourierAccountDto[]> {
    return this.getCourierAccounts({ isActive: true, isConnected: true });
  }

  async getCourierAccount(id: string): Promise<CourierAccountDetailDto> {
    const response = await apiClient.get<ApiResponse<CourierAccountDetailDto>>(`${this.baseUrl}/${id}`);
    return response.data.data;
  }

  async createCourierAccount(request: CreateCourierAccountRequest): Promise<CourierAccountDto> {
    const response = await apiClient.post<ApiResponse<CourierAccountDto>>(this.baseUrl, request);
    return response.data.data;
  }

  async updateCourierAccount(
    id: string,
    request: UpdateCourierAccountRequest
  ): Promise<CourierAccountDto> {
    const response = await apiClient.put<ApiResponse<CourierAccountDto>>(`${this.baseUrl}/${id}`, request);
    return response.data.data;
  }

  async deleteCourierAccount(id: string): Promise<void> {
    await apiClient.delete(`${this.baseUrl}/${id}`);
  }

  async testConnection(id: string): Promise<CourierConnectionTestResult> {
    const response = await apiClient.post<ApiResponse<CourierConnectionTestResult>>(`${this.baseUrl}/${id}/test`);
    return response.data.data;
  }

  async getWalletBalance(id: string): Promise<CourierWalletBalance> {
    const response = await apiClient.get<ApiResponse<CourierWalletBalance>>(`${this.baseUrl}/${id}/wallet-balance`);
    return response.data.data;
  }

  async getShiprocketChannels(id: string): Promise<ShiprocketChannel[]> {
    const response = await apiClient.get<ApiResponse<ShiprocketChannel[]>>(`${this.baseUrl}/${id}/shiprocket-channels`);
    return response.data.data;
  }

  async getShiprocketPickupLocations(id: string): Promise<ShiprocketPickupLocation[]> {
    const response = await apiClient.get<ApiResponse<ShiprocketPickupLocation[]>>(`${this.baseUrl}/${id}/shiprocket-pickup-locations`);
    return response.data.data;
  }
}

export const courierService = new CourierService();
