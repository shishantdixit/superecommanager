import { useQuery } from '@tanstack/react-query';
import { analyticsService, AnalyticsFilter } from '@/services/analytics.service';

export const analyticsKeys = {
  all: ['analytics'] as const,
  revenue: (filters: AnalyticsFilter) => [...analyticsKeys.all, 'revenue', filters] as const,
  orders: (filters: AnalyticsFilter) => [...analyticsKeys.all, 'orders', filters] as const,
  delivery: (filters: AnalyticsFilter) => [...analyticsKeys.all, 'delivery', filters] as const,
  couriers: (filters: AnalyticsFilter) => [...analyticsKeys.all, 'couriers', filters] as const,
  ndr: (filters: AnalyticsFilter) => [...analyticsKeys.all, 'ndr', filters] as const,
};

/**
 * Hook to fetch revenue trends.
 */
export function useRevenueTrends(filters: AnalyticsFilter = {}) {
  return useQuery({
    queryKey: analyticsKeys.revenue(filters),
    queryFn: () => analyticsService.getRevenueTrends(filters),
  });
}

/**
 * Hook to fetch order trends.
 */
export function useOrderTrends(filters: AnalyticsFilter = {}) {
  return useQuery({
    queryKey: analyticsKeys.orders(filters),
    queryFn: () => analyticsService.getOrderTrends(filters),
  });
}

/**
 * Hook to fetch delivery performance.
 */
export function useDeliveryPerformance(filters: AnalyticsFilter = {}) {
  return useQuery({
    queryKey: analyticsKeys.delivery(filters),
    queryFn: () => analyticsService.getDeliveryPerformance(filters),
  });
}

/**
 * Hook to fetch courier comparison.
 */
export function useCourierComparison(filters: AnalyticsFilter = {}) {
  return useQuery({
    queryKey: analyticsKeys.couriers(filters),
    queryFn: () => analyticsService.getCourierComparison(filters),
  });
}

/**
 * Hook to fetch NDR analytics.
 */
export function useNdrAnalytics(filters: AnalyticsFilter = {}) {
  return useQuery({
    queryKey: analyticsKeys.ndr(filters),
    queryFn: () => analyticsService.getNdrAnalytics(filters),
  });
}
