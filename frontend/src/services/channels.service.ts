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
  autoSyncOrders?: boolean;
  autoSyncInventory?: boolean;
  // Credential/connection status for OAuth channels
  isConnected?: boolean;
  hasCredentials?: boolean;
  lastError?: string;
  // Advanced sync settings
  initialSyncDays?: number | null;
  inventorySyncDays?: number | null;
  productSyncDays?: number | null;
  orderSyncLimit?: number | null;
  inventorySyncLimit?: number | null;
  productSyncLimit?: number | null;
  syncProductsEnabled?: boolean;
  autoSyncProducts?: boolean;
  lastProductSyncAt?: string;
  lastInventorySyncAt?: string;
}

export interface UpdateChannelSettingsRequest {
  autoSyncOrders?: boolean;
  autoSyncInventory?: boolean;
  // Advanced sync settings
  initialSyncDays?: number | null;
  inventorySyncDays?: number | null;
  productSyncDays?: number | null;
  orderSyncLimit?: number | null;
  inventorySyncLimit?: number | null;
  productSyncLimit?: number | null;
  syncProductsEnabled?: boolean;
  autoSyncProducts?: boolean;
}

export interface ConnectShopifyRequest {
  shopDomain: string;
}

export interface SaveShopifyCredentialsRequest {
  channelId?: string;
  apiKey: string;
  apiSecret: string;
  shopDomain: string;
  scopes?: string;
}

export interface ShopifyOAuthResult {
  authorizationUrl: string;
  state: string;
}

export interface ChannelSyncResult {
  channelId: string;
  status: string;
  syncedAt: string;
  // Order sync results
  ordersImported: number;
  ordersUpdated: number;
  ordersFailed: number;
  // Product sync results
  productsImported: number;
  productsUpdated: number;
  productsFailed: number;
  // Inventory sync results
  inventoryUpdated: number;
  inventorySkipped: number;
  inventoryFailed: number;
  errors: string[];
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
   * Save Shopify API credentials.
   * Each tenant must create their own Shopify app and provide credentials here.
   */
  saveShopifyCredentials: async (data: SaveShopifyCredentialsRequest) => {
    const response = await post<ApiResponse<Channel>, SaveShopifyCredentialsRequest>(
      '/channels/shopify/credentials',
      data
    );
    return response.data;
  },

  /**
   * Initiate Shopify OAuth connection for a channel that has credentials saved.
   */
  connectShopify: async (channelId: string) => {
    const response = await post<ApiResponse<ShopifyOAuthResult>>(
      `/channels/${channelId}/shopify/connect`,
      {}
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
   * Trigger manual order sync for a channel.
   */
  syncChannel: async (id: string) => {
    const response = await post<ApiResponse<ChannelSyncResult>>(`/channels/${id}/sync`, {});
    return response.data;
  },

  /**
   * Trigger manual inventory sync for a channel.
   * Pulls inventory levels from the external channel and updates local inventory.
   */
  syncInventory: async (id: string) => {
    const response = await post<ApiResponse<ChannelSyncResult>>(`/channels/${id}/sync/inventory`, {});
    return response.data;
  },

  /**
   * Trigger manual product sync for a channel.
   * Imports products and creates local inventory records.
   */
  syncProducts: async (id: string) => {
    const response = await post<ApiResponse<ChannelSyncResult>>(`/channels/${id}/sync/products`, {});
    return response.data;
  },

  /**
   * Update channel settings.
   */
  updateChannelSettings: async (id: string, data: UpdateChannelSettingsRequest) => {
    const response = await post<ApiResponse<Channel>, UpdateChannelSettingsRequest>(
      `/channels/${id}/settings`,
      data
    );
    return response.data;
  },
};
