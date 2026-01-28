import { get, post, put } from '@/lib/api-client';
import type { ApiResponse, PaginatedResponse, StockMovementType } from '@/types/api';

// Sync status enum
export enum SyncStatus {
  Synced = 0,
  LocalOnly = 1,
  Pending = 2,
  Conflict = 3,
}

// Sync mode for product updates
export enum ProductSyncMode {
  LocalOnly = 0,
  PendingSync = 1,
  SyncImmediately = 2,
}

// Product types
export interface ProductListItem {
  id: string;
  sku: string;
  name: string;
  category?: string;
  brand?: string;
  costPrice: number;
  sellingPrice: number;
  currency: string;
  isActive: boolean;
  imageUrl?: string;
  totalStock: number;
  variantCount: number;
  createdAt: string;
  // Sync tracking
  syncStatus: SyncStatus;
  channelSellingPrice?: number;
  lastSyncedAt?: string;
}

export interface ProductDetail {
  id: string;
  sku: string;
  name: string;
  description?: string;
  category?: string;
  brand?: string;
  costPrice: number;
  sellingPrice: number;
  currency: string;
  weight?: number;
  imageUrl?: string;
  isActive: boolean;
  hsnCode?: string;
  taxRate?: number;
  createdAt: string;
  updatedAt?: string;
  // Sync tracking
  syncStatus: SyncStatus;
  lastSyncedAt?: string;
  channelProductId?: string;
  channelSellingPrice?: number;
  channelSellingCurrency?: string;
  // Relations
  variants: ProductVariant[];
  inventorySummary?: InventorySummary;
}

export interface ProductVariant {
  id: string;
  sku: string;
  name: string;
  option1Name?: string;
  option1Value?: string;
  option2Name?: string;
  option2Value?: string;
  costPrice?: number;
  sellingPrice?: number;
  weight?: number;
  imageUrl?: string;
  isActive: boolean;
  quantityOnHand: number;
  quantityAvailable: number;
}

export interface InventorySummary {
  totalOnHand: number;
  totalReserved: number;
  totalAvailable: number;
  isLowStock: boolean;
  items: InventoryItem[];
}

export interface InventoryItem {
  id: string;
  productId: string;
  productVariantId?: string;
  sku: string;
  variantName?: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  reorderPoint: number;
  reorderQuantity: number;
  location?: string;
  isLowStock: boolean;
}

export interface StockMovement {
  id: string;
  inventoryId: string;
  sku: string;
  movementType: StockMovementType;
  quantity: number;
  quantityBefore: number;
  quantityAfter: number;
  referenceType?: string;
  referenceId?: string;
  notes?: string;
  performedByUserName?: string;
  createdAt: string;
}

export interface InventoryStats {
  totalProducts: number;
  totalActiveProducts: number;
  totalVariants: number;
  totalStockOnHand: number;
  totalStockReserved: number;
  lowStockProducts: number;
  outOfStockProducts: number;
  totalInventoryValue: number;
  currency: string;
  stockByCategory: Record<string, number>;
  lowStockItems: LowStockItem[];
}

export interface LowStockItem {
  productId: string;
  variantId?: string;
  sku: string;
  productName: string;
  variantName?: string;
  quantityOnHand: number;
  reorderPoint: number;
  reorderQuantity: number;
}

// Filter types
export interface ProductFilters {
  searchTerm?: string;
  category?: string;
  brand?: string;
  isActive?: boolean;
  isLowStock?: boolean;
  minPrice?: number;
  maxPrice?: number;
  syncStatus?: SyncStatus;
  /** Filter products by source channel. If undefined, returns all products. */
  channelId?: string;
  page?: number;
  pageSize?: number;
  sortBy?: 'Name' | 'Sku' | 'Price' | 'Stock' | 'CreatedAt';
  sortDescending?: boolean;
}

export interface StockMovementFilters {
  productId?: string;
  inventoryItemId?: string;
  sku?: string;
  movementType?: StockMovementType;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

// Request types
export interface CreateProductRequest {
  sku: string;
  name: string;
  description?: string;
  category?: string;
  brand?: string;
  costPrice: number;
  sellingPrice: number;
  currency?: string;
  weight?: number;
  imageUrl?: string;
  hsnCode?: string;
  taxRate?: number;
  initialStock?: number;
  variants?: CreateVariantRequest[];
}

export interface CreateVariantRequest {
  sku: string;
  name: string;
  option1Name?: string;
  option1Value?: string;
  option2Name?: string;
  option2Value?: string;
  costPrice?: number;
  sellingPrice?: number;
  weight?: number;
  imageUrl?: string;
  initialStock?: number;
}

export interface UpdateProductRequest {
  name: string;
  description?: string;
  category?: string;
  brand?: string;
  costPrice: number;
  sellingPrice: number;
  currency?: string;
  weight?: number;
  imageUrl?: string;
  hsnCode?: string;
  taxRate?: number;
  isActive: boolean;
  syncMode?: ProductSyncMode;
}

export interface StockAdjustmentRequest {
  inventoryItemId: string;
  adjustmentType: StockMovementType;
  quantity: number;
  notes?: string;
  referenceType?: string;
  referenceId?: string;
}

export const inventoryService = {
  /**
   * Get paginated products with filters.
   */
  getProducts: async (filters: ProductFilters = {}) => {
    const response = await get<ApiResponse<PaginatedResponse<ProductListItem>>>('/inventory/products', { params: filters });
    return response.data;
  },

  /**
   * Get product by ID.
   */
  getProductById: async (id: string) => {
    const response = await get<ApiResponse<ProductDetail>>(`/inventory/products/${id}`);
    return response.data;
  },

  /**
   * Create a new product.
   */
  createProduct: async (data: CreateProductRequest) => {
    const response = await post<ApiResponse<ProductDetail>, CreateProductRequest>('/inventory/products', data);
    return response.data;
  },

  /**
   * Update a product.
   */
  updateProduct: async (id: string, data: UpdateProductRequest) => {
    const response = await put<ApiResponse<ProductDetail>, UpdateProductRequest>(`/inventory/products/${id}`, data);
    return response.data;
  },

  /**
   * Get inventory statistics.
   */
  getStats: async () => {
    const response = await get<ApiResponse<InventoryStats>>('/inventory/stats');
    return response.data;
  },

  /**
   * Get low stock products.
   */
  getLowStock: async (page = 1, pageSize = 20) => {
    const response = await get<ApiResponse<PaginatedResponse<ProductListItem>>>('/inventory/low-stock', {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Adjust stock for an inventory item.
   */
  adjustStock: async (data: StockAdjustmentRequest) => {
    const response = await post<ApiResponse<InventoryItem>, StockAdjustmentRequest>('/inventory/stock/adjust', data);
    return response.data;
  },

  /**
   * Get stock movements with filters.
   */
  getStockMovements: async (filters: StockMovementFilters = {}) => {
    const response = await get<ApiResponse<PaginatedResponse<StockMovement>>>('/inventory/stock/movements', { params: filters });
    return response.data;
  },
};
