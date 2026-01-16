import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ndrService,
  AssignNdrRequest,
  LogActionRequest,
  AddRemarkRequest,
  ScheduleReattemptRequest,
  UpdateOutcomeRequest,
} from '@/services/ndr.service';
import type { NdrFilters } from '@/types/api';

export const ndrKeys = {
  all: ['ndr'] as const,
  lists: () => [...ndrKeys.all, 'list'] as const,
  list: (filters: NdrFilters) => [...ndrKeys.lists(), filters] as const,
  details: () => [...ndrKeys.all, 'detail'] as const,
  detail: (id: string) => [...ndrKeys.details(), id] as const,
  byShipment: (shipmentId: string) => [...ndrKeys.all, 'shipment', shipmentId] as const,
  myCases: (filters: NdrFilters) => [...ndrKeys.all, 'my-cases', filters] as const,
  stats: (fromDate?: string, toDate?: string) => [...ndrKeys.all, 'stats', { fromDate, toDate }] as const,
};

/**
 * Hook to fetch paginated NDR cases with filters.
 */
export function useNdrCases(filters: NdrFilters = {}) {
  return useQuery({
    queryKey: ndrKeys.list(filters),
    queryFn: () => ndrService.getNdrCases(filters),
  });
}

/**
 * Hook to fetch a single NDR case by ID.
 */
export function useNdrCase(id: string) {
  return useQuery({
    queryKey: ndrKeys.detail(id),
    queryFn: () => ndrService.getNdrCaseById(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch NDR statistics.
 */
export function useNdrStats(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: ndrKeys.stats(fromDate, toDate),
    queryFn: () => ndrService.getStats(fromDate, toDate),
  });
}

/**
 * Hook to fetch NDR cases for a specific shipment.
 */
export function useNdrByShipment(shipmentId: string) {
  return useQuery({
    queryKey: ndrKeys.byShipment(shipmentId),
    queryFn: () => ndrService.getNdrByShipment(shipmentId),
    enabled: !!shipmentId,
  });
}

/**
 * Hook to fetch NDR cases assigned to current user.
 */
export function useMyNdrCases(filters: NdrFilters = {}) {
  return useQuery({
    queryKey: ndrKeys.myCases(filters),
    queryFn: () => ndrService.getMyCases(filters),
  });
}

/**
 * Hook to assign NDR case to a user.
 */
export function useAssignNdrCase() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AssignNdrRequest }) =>
      ndrService.assignCase(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ndrKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: ndrKeys.lists() });
      queryClient.invalidateQueries({ queryKey: ndrKeys.stats() });
    },
  });
}

/**
 * Hook to log action on NDR case.
 */
export function useLogNdrAction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: LogActionRequest }) =>
      ndrService.logAction(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ndrKeys.detail(id) });
    },
  });
}

/**
 * Hook to add remark to NDR case.
 */
export function useAddNdrRemark() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AddRemarkRequest }) =>
      ndrService.addRemark(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ndrKeys.detail(id) });
    },
  });
}

/**
 * Hook to schedule reattempt for NDR case.
 */
export function useScheduleReattempt() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ScheduleReattemptRequest }) =>
      ndrService.scheduleReattempt(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ndrKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: ndrKeys.lists() });
      queryClient.invalidateQueries({ queryKey: ndrKeys.stats() });
    },
  });
}

/**
 * Hook to update NDR outcome.
 */
export function useUpdateNdrOutcome() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateOutcomeRequest }) =>
      ndrService.updateOutcome(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ndrKeys.detail(id) });
      queryClient.invalidateQueries({ queryKey: ndrKeys.lists() });
      queryClient.invalidateQueries({ queryKey: ndrKeys.stats() });
    },
  });
}
