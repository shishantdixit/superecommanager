import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  inventoryService,
  ProductFilters,
  StockMovementFilters,
  CreateProductRequest,
  UpdateProductRequest,
  StockAdjustmentRequest,
} from '@/services/inventory.service';

export const inventoryKeys = {
  all: ['inventory'] as const,
  products: () => [...inventoryKeys.all, 'products'] as const,
  productList: (filters: ProductFilters) => [...inventoryKeys.products(), 'list', filters] as const,
  productDetail: (id: string) => [...inventoryKeys.products(), 'detail', id] as const,
  stats: () => [...inventoryKeys.all, 'stats'] as const,
  lowStock: (page: number, pageSize: number) => [...inventoryKeys.all, 'low-stock', { page, pageSize }] as const,
  movements: (filters: StockMovementFilters) => [...inventoryKeys.all, 'movements', filters] as const,
};

/**
 * Hook to fetch paginated products with filters.
 */
export function useProducts(filters: ProductFilters = {}) {
  return useQuery({
    queryKey: inventoryKeys.productList(filters),
    queryFn: () => inventoryService.getProducts(filters),
  });
}

/**
 * Hook to fetch a single product by ID.
 */
export function useProduct(id: string) {
  return useQuery({
    queryKey: inventoryKeys.productDetail(id),
    queryFn: () => inventoryService.getProductById(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch inventory statistics.
 */
export function useInventoryStats() {
  return useQuery({
    queryKey: inventoryKeys.stats(),
    queryFn: () => inventoryService.getStats(),
  });
}

/**
 * Hook to fetch low stock products.
 */
export function useLowStockProducts(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: inventoryKeys.lowStock(page, pageSize),
    queryFn: () => inventoryService.getLowStock(page, pageSize),
  });
}

/**
 * Hook to fetch stock movements.
 */
export function useStockMovements(filters: StockMovementFilters = {}) {
  return useQuery({
    queryKey: inventoryKeys.movements(filters),
    queryFn: () => inventoryService.getStockMovements(filters),
  });
}

/**
 * Hook to create a new product.
 */
export function useCreateProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateProductRequest) => inventoryService.createProduct(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.products() });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.stats() });
    },
  });
}

/**
 * Hook to update a product.
 */
export function useUpdateProduct() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateProductRequest }) =>
      inventoryService.updateProduct(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.productDetail(id) });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.products() });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.stats() });
    },
  });
}

/**
 * Hook to adjust stock.
 */
export function useAdjustStock() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: StockAdjustmentRequest) => inventoryService.adjustStock(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: inventoryKeys.products() });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.stats() });
      queryClient.invalidateQueries({ queryKey: inventoryKeys.all });
    },
  });
}
