import { get, post, del } from '@/lib/api-client';
import type { ApiResponse } from '@/types/api';

export type ChannelType = 'Shopify' | 'Amazon' | 'Flipkart' | 'Meesho' | 'WooCommerce' | 'Custom';

export type ChannelSyncStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Failed';

export interface Channel {
  id: string;
  name: string;
  type: ChannelType;
  isActive: boolean;
  storeUrl?: string;
  storeName?: string;
  lastSyncAt?: string;
  totalOrders: number;
  syncStatus: ChannelSyncStatus;
  createdAt: string;
}

export interface ConnectShopifyRequest {
  shopDomain: string;
}

export interface ShopifyOAuthResult {
  authorizationUrl: string;
  state: string;
}

export interface ChannelSyncResult {
  channelId: string;
  ordersImported: number;
  ordersUpdated: number;
  errors: string[];
  syncedAt: string;
}

export const channelsService = {
  /**
   * Get all sales channels for the current tenant.
   */
  getChannels: async () => {
    const response = await get<ApiResponse<Channel[]>>('/channels');
    return response.data;
  },

  /**
   * Get channel by ID.
   */
  getChannelById: async (id: string) => {
    const response = await get<ApiResponse<Channel>>(`/channels/${id}`);
    return response.data;
  },

  /**
   * Initiate Shopify OAuth connection.
   */
  connectShopify: async (data: ConnectShopifyRequest) => {
    const response = await post<ApiResponse<ShopifyOAuthResult>, ConnectShopifyRequest>(
      '/channels/shopify/connect',
      data
    );
    return response.data;
  },

  /**
   * Disconnect a sales channel.
   */
  disconnectChannel: async (id: string) => {
    await del(`/channels/${id}`);
  },

  /**
   * Trigger manual sync for a channel.
   */
  syncChannel: async (id: string) => {
    const response = await post<ApiResponse<ChannelSyncResult>>(`/channels/${id}/sync`, {});
    return response.data;
  },
};
