import { useQuery } from '@tanstack/react-query';
import { dashboardService } from '@/services/dashboard.service';

export const dashboardKeys = {
  all: ['dashboard'] as const,
  overview: (period: string, trendDays: number) => [...dashboardKeys.all, 'overview', { period, trendDays }] as const,
  orders: (period: string) => [...dashboardKeys.all, 'orders', period] as const,
  shipments: (period: string) => [...dashboardKeys.all, 'shipments', period] as const,
  alerts: () => [...dashboardKeys.all, 'alerts'] as const,
};

type DashboardPeriod = 'today' | '7days' | '30days';

/**
 * Hook to fetch main dashboard overview.
 */
export function useDashboardOverview(period: DashboardPeriod = '7days', trendDays = 7) {
  return useQuery({
    queryKey: dashboardKeys.overview(period, trendDays),
    queryFn: () => dashboardService.getOverview(period, trendDays),
    refetchInterval: 5 * 60 * 1000, // Refetch every 5 minutes
  });
}

/**
 * Hook to fetch orders metrics for dashboard.
 */
export function useDashboardOrders(period: DashboardPeriod = '7days') {
  return useQuery({
    queryKey: dashboardKeys.orders(period),
    queryFn: () => dashboardService.getOrdersMetrics(period),
  });
}

/**
 * Hook to fetch shipments metrics for dashboard.
 */
export function useDashboardShipments(period: DashboardPeriod = '7days') {
  return useQuery({
    queryKey: dashboardKeys.shipments(period),
    queryFn: () => dashboardService.getShipmentsMetrics(period),
  });
}

/**
 * Hook to fetch alerts and action items.
 */
export function useDashboardAlerts() {
  return useQuery({
    queryKey: dashboardKeys.alerts(),
    queryFn: () => dashboardService.getAlerts(),
    refetchInterval: 2 * 60 * 1000, // Refetch every 2 minutes
  });
}
