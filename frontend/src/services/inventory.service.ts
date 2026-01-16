import { get, post, put } from '@/lib/api-client';
import type { PaginatedResponse, StockMovementType } from '@/types/api';

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
  getProducts: (filters: ProductFilters = {}) =>
    get<PaginatedResponse<ProductListItem>>('/inventory/products', { params: filters }),

  /**
   * Get product by ID.
   */
  getProductById: (id: string) =>
    get<ProductDetail>(`/inventory/products/${id}`),

  /**
   * Create a new product.
   */
  createProduct: (data: CreateProductRequest) =>
    post<ProductDetail, CreateProductRequest>('/inventory/products', data),

  /**
   * Update a product.
   */
  updateProduct: (id: string, data: UpdateProductRequest) =>
    put<ProductDetail, UpdateProductRequest>(`/inventory/products/${id}`, data),

  /**
   * Get inventory statistics.
   */
  getStats: () =>
    get<InventoryStats>('/inventory/stats'),

  /**
   * Get low stock products.
   */
  getLowStock: (page = 1, pageSize = 20) =>
    get<PaginatedResponse<ProductListItem>>('/inventory/low-stock', {
      params: { page, pageSize },
    }),

  /**
   * Adjust stock for an inventory item.
   */
  adjustStock: (data: StockAdjustmentRequest) =>
    post<InventoryItem, StockAdjustmentRequest>('/inventory/stock/adjust', data),

  /**
   * Get stock movements with filters.
   */
  getStockMovements: (filters: StockMovementFilters = {}) =>
    get<PaginatedResponse<StockMovement>>('/inventory/stock/movements', { params: filters }),
};
