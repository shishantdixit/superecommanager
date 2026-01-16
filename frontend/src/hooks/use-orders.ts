import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ordersService, UpdateOrderStatusRequest, UpdateOrderNotesRequest, BulkUpdateRequest } from '@/services/orders.service';
import type { OrderFilters } from '@/types/api';

export const orderKeys = {
  all: ['orders'] as const,
  lists: () => [...orderKeys.all, 'list'] as const,
  list: (filters: OrderFilters) => [...orderKeys.lists(), filters] as const,
  details: () => [...orderKeys.all, 'detail'] as const,
  detail: (id: string) => [...orderKeys.details(), id] as const,
  stats: (fromDate?: string, toDate?: string) => [...orderKeys.all, 'stats', { fromDate, toDate }] as const,
};

/**
 * Hook to fetch paginated orders with filters.
 */
export function useOrders(filters: OrderFilters = {}) {
  return useQuery({
    queryKey: orderKeys.list(filters),
    queryFn: () => ordersService.getOrders(filters),
  });
}

/**
 * Hook to fetch a single order by ID.
 */
export function useOrder(id: string) {
  return useQuery({
    queryKey: orderKeys.detail(id),
    queryFn: () => ordersService.getOrderById(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch order statistics.
 */
export function useOrderStats(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: orderKeys.stats(fromDate, toDate),
    queryFn: () => ordersService.getStats(fromDate, toDate),
  });
}

/**
 * Hook to update order status.
 */
export function useUpdateOrderStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateOrderStatusRequest }) =>
      ordersService.updateStatus(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: orderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() });
      queryClient.invalidateQueries({ queryKey: orderKeys.stats() });
    },
  });
}

/**
 * Hook to update order notes.
 */
export function useUpdateOrderNotes() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateOrderNotesRequest }) =>
      ordersService.updateNotes(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: orderKeys.detail(id) });
    },
  });
}

/**
 * Hook to cancel an order.
 */
export function useCancelOrder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      ordersService.cancelOrder(id, reason),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: orderKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() });
      queryClient.invalidateQueries({ queryKey: orderKeys.stats() });
    },
  });
}

/**
 * Hook to bulk update orders.
 */
export function useBulkUpdateOrders() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BulkUpdateRequest) => ordersService.bulkUpdate(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() });
      queryClient.invalidateQueries({ queryKey: orderKeys.stats() });
    },
  });
}
