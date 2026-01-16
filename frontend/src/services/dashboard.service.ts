import { get } from '@/lib/api-client';
import type { ApiResponse } from '@/types/api';

export interface DashboardOverview {
  // Order metrics
  totalOrders: number;
  todayOrders: number;
  pendingOrders: number;
  confirmedOrders: number;

  // Revenue metrics
  totalRevenue: number;
  todayRevenue: number;
  averageOrderValue: number;

  // Shipment metrics
  activeShipments: number;
  inTransit: number;
  outForDelivery: number;
  deliveredToday: number;

  // NDR metrics
  openNdrCases: number;
  criticalNdrCases: number;
  unassignedNdrCases: number;

  // Inventory metrics
  lowStockItems: number;
  outOfStockItems: number;

  // Trends
  ordersTrend: TrendData[];
  revenueTrend: TrendData[];
}

export interface TrendData {
  date: string;
  value: number;
}

export interface DashboardOrders {
  totalOrders: number;
  ordersByStatus: Record<string, number>;
  ordersByChannel: Record<string, number>;
  topProducts: TopProduct[];
  recentOrders: RecentOrder[];
}

export interface TopProduct {
  productId: string;
  name: string;
  sku: string;
  orderCount: number;
  revenue: number;
}

export interface RecentOrder {
  id: string;
  orderNumber: string;
  customerName: string;
  total: number;
  status: string;
  channelType: string;
  createdAt: string;
}

export interface DashboardShipments {
  totalShipments: number;
  shipmentsByStatus: Record<string, number>;
  shipmentsByCourier: Record<string, number>;
  avgDeliveryDays: number;
  deliveryRate: number;
  ndrRate: number;
  rtoRate: number;
}

export interface DashboardAlerts {
  alerts: Alert[];
  actionItems: ActionItem[];
}

export interface Alert {
  id: string;
  type: 'warning' | 'error' | 'info';
  title: string;
  message: string;
  link?: string;
  createdAt: string;
}

export interface ActionItem {
  id: string;
  type: string;
  title: string;
  description: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
  link?: string;
  dueDate?: string;
}

export const dashboardService = {
  /**
   * Get main dashboard with aggregated metrics.
   */
  getOverview: async (period: 'today' | '7days' | '30days' = '7days', trendDays = 7) => {
    const response = await get<ApiResponse<DashboardOverview>>('/dashboard/overview', {
      params: { period, trendDays },
    });
    return response.data;
  },

  /**
   * Get orders-focused metrics for dashboard.
   */
  getOrdersMetrics: async (period: 'today' | '7days' | '30days' = '7days') => {
    const response = await get<ApiResponse<DashboardOrders>>('/dashboard/orders', {
      params: { period },
    });
    return response.data;
  },

  /**
   * Get shipments-focused metrics for dashboard.
   */
  getShipmentsMetrics: async (period: 'today' | '7days' | '30days' = '7days') => {
    const response = await get<ApiResponse<DashboardShipments>>('/dashboard/shipments', {
      params: { period },
    });
    return response.data;
  },

  /**
   * Get alerts and action items.
   */
  getAlerts: async () => {
    const response = await get<ApiResponse<DashboardAlerts>>('/dashboard/alerts');
    return response.data;
  },
};
