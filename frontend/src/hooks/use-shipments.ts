import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  shipmentsService,
  CreateShipmentRequest,
  UpdateShipmentStatusRequest,
} from '@/services/shipments.service';
import type { ShipmentFilters } from '@/types/api';

export const shipmentKeys = {
  all: ['shipments'] as const,
  lists: () => [...shipmentKeys.all, 'list'] as const,
  list: (filters: ShipmentFilters) => [...shipmentKeys.lists(), filters] as const,
  details: () => [...shipmentKeys.all, 'detail'] as const,
  detail: (id: string) => [...shipmentKeys.details(), id] as const,
  tracking: (id: string) => [...shipmentKeys.all, 'tracking', id] as const,
  byOrder: (orderId: string) => [...shipmentKeys.all, 'order', orderId] as const,
  stats: (fromDate?: string, toDate?: string) => [...shipmentKeys.all, 'stats', { fromDate, toDate }] as const,
};

/**
 * Hook to fetch paginated shipments with filters.
 */
export function useShipments(filters: ShipmentFilters = {}) {
  return useQuery({
    queryKey: shipmentKeys.list(filters),
    queryFn: () => shipmentsService.getShipments(filters),
  });
}

/**
 * Hook to fetch a single shipment by ID.
 */
export function useShipment(id: string) {
  return useQuery({
    queryKey: shipmentKeys.detail(id),
    queryFn: () => shipmentsService.getShipmentById(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch shipment tracking information.
 */
export function useShipmentTracking(id: string) {
  return useQuery({
    queryKey: shipmentKeys.tracking(id),
    queryFn: () => shipmentsService.getTracking(id),
    enabled: !!id,
    refetchInterval: 5 * 60 * 1000, // Refetch every 5 minutes
  });
}

/**
 * Hook to fetch shipment statistics.
 */
export function useShipmentStats(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: shipmentKeys.stats(fromDate, toDate),
    queryFn: () => shipmentsService.getStats(fromDate, toDate),
  });
}

/**
 * Hook to fetch shipments for a specific order.
 */
export function useShipmentsByOrder(orderId: string) {
  return useQuery({
    queryKey: shipmentKeys.byOrder(orderId),
    queryFn: () => shipmentsService.getShipmentsByOrder(orderId),
    enabled: !!orderId,
  });
}

/**
 * Hook to create a new shipment.
 */
export function useCreateShipment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateShipmentRequest) => shipmentsService.createShipment(data),
    onSuccess: (_, { orderId }) => {
      queryClient.invalidateQueries({ queryKey: shipmentKeys.lists() });
      queryClient.invalidateQueries({ queryKey: shipmentKeys.byOrder(orderId) });
      queryClient.invalidateQueries({ queryKey: shipmentKeys.stats() });
    },
  });
}

/**
 * Hook to update shipment status.
 */
export function useUpdateShipmentStatus() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateShipmentStatusRequest }) =>
      shipmentsService.updateStatus(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: shipmentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: shipmentKeys.tracking(id) });
      queryClient.invalidateQueries({ queryKey: shipmentKeys.lists() });
      queryClient.invalidateQueries({ queryKey: shipmentKeys.stats() });
    },
  });
}

/**
 * Hook to cancel a shipment.
 */
export function useCancelShipment() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      shipmentsService.cancelShipment(id, reason),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: shipmentKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: shipmentKeys.lists() });
      queryClient.invalidateQueries({ queryKey: shipmentKeys.stats() });
    },
  });
}
