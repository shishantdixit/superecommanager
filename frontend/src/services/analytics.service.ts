import { get } from '@/lib/api-client';

export type AnalyticsPeriod =
  | 'Today'
  | 'Yesterday'
  | 'Last7Days'
  | 'Last30Days'
  | 'ThisMonth'
  | 'LastMonth'
  | 'ThisQuarter'
  | 'ThisYear'
  | 'Custom';

export interface AnalyticsFilter {
  period?: AnalyticsPeriod;
  startDate?: string;
  endDate?: string;
}

// Revenue Trends
export interface RevenueTrends {
  totalRevenue: number;
  previousPeriodRevenue: number;
  percentageChange: number;
  totalOrders: number;
  previousPeriodOrders: number;
  averageOrderValue: number;
  previousAverageOrderValue: number;
  dailyRevenue: DailyRevenue[];
  revenueByChannel: ChannelRevenue[];
  revenueByPaymentMethod: PaymentMethodRevenue[];
}

export interface DailyRevenue {
  date: string;
  revenue: number;
  orderCount: number;
  averageOrderValue: number;
}

export interface ChannelRevenue {
  channelId?: string;
  channelName: string;
  channelType: string;
  revenue: number;
  orderCount: number;
  percentage: number;
}

export interface PaymentMethodRevenue {
  paymentMethod: string;
  revenue: number;
  orderCount: number;
  percentage: number;
}

// Order Trends
export interface OrderTrends {
  totalOrders: number;
  previousPeriodOrders: number;
  percentageChange: number;
  dailyOrders: DailyOrderCount[];
  ordersByStatus: OrderStatusCount[];
  ordersByHour: HourlyOrderCount[];
  averageOrdersPerDay: number;
  peakHour: number;
  peakDay: string;
}

export interface DailyOrderCount {
  date: string;
  orderCount: number;
  confirmedCount: number;
  cancelledCount: number;
}

export interface OrderStatusCount {
  status: string;
  count: number;
  percentage: number;
}

export interface HourlyOrderCount {
  hour: number;
  orderCount: number;
}

// Delivery Performance
export interface DeliveryPerformance {
  totalShipments: number;
  deliveredCount: number;
  rtoCount: number;
  inTransitCount: number;
  deliveryRate: number;
  rtoRate: number;
  averageDeliveryDays: number;
  previousAverageDeliveryDays: number;
  deliveryTimeDistribution: DeliveryTimeDistribution[];
  dailyDeliveries: DailyDelivery[];
  deliveryByState: StateDelivery[];
}

export interface DeliveryTimeDistribution {
  range: string;
  count: number;
  percentage: number;
}

export interface DailyDelivery {
  date: string;
  deliveredCount: number;
  rtoCount: number;
  ndrCount: number;
}

export interface StateDelivery {
  state: string;
  totalShipments: number;
  deliveredCount: number;
  rtoCount: number;
  deliveryRate: number;
  averageDeliveryDays: number;
}

// Courier Comparison
export interface CourierComparison {
  couriers: CourierPerformance[];
  bestDeliveryRateCourierId?: string;
  bestDeliveryRateCourierName?: string;
  fastestDeliveryCourierId?: string;
  fastestDeliveryCourierName?: string;
  lowestRtoCourierId?: string;
  lowestRtoCourierName?: string;
}

export interface CourierPerformance {
  courierId: string;
  courierName: string;
  courierType: string;
  totalShipments: number;
  deliveredCount: number;
  rtoCount: number;
  ndrCount: number;
  deliveryRate: number;
  rtoRate: number;
  ndrRate: number;
  averageDeliveryDays: number;
  averageCost: number;
  totalCost: number;
}

// NDR Analytics
export interface NdrAnalytics {
  totalNdrCases: number;
  resolvedCount: number;
  pendingCount: number;
  escalatedCount: number;
  resolutionRate: number;
  averageResolutionHours: number;
  byReason: NdrReasonBreakdown[];
  byStatus: NdrStatusBreakdown[];
  dailyNdr: DailyNdr[];
  agentPerformance: AgentPerformance[];
}

export interface NdrReasonBreakdown {
  reasonCode: string;
  reasonDescription: string;
  count: number;
  percentage: number;
  resolutionRate: number;
}

export interface NdrStatusBreakdown {
  status: string;
  count: number;
  percentage: number;
}

export interface DailyNdr {
  date: string;
  newCases: number;
  resolvedCases: number;
  escalatedCases: number;
}

export interface AgentPerformance {
  agentId: string;
  agentName: string;
  assignedCases: number;
  resolvedCases: number;
  pendingCases: number;
  resolutionRate: number;
  averageResolutionHours: number;
  totalCalls: number;
  successfulContacts: number;
}

export const analyticsService = {
  getRevenueTrends: (filters: AnalyticsFilter = {}) =>
    get<RevenueTrends>('/analytics/revenue', { params: filters }),

  getOrderTrends: (filters: AnalyticsFilter = {}) =>
    get<OrderTrends>('/analytics/orders', { params: filters }),

  getDeliveryPerformance: (filters: AnalyticsFilter = {}) =>
    get<DeliveryPerformance>('/analytics/delivery', { params: filters }),

  getCourierComparison: (filters: AnalyticsFilter = {}) =>
    get<CourierComparison>('/analytics/couriers', { params: filters }),

  getNdrAnalytics: (filters: AnalyticsFilter = {}) =>
    get<NdrAnalytics>('/analytics/ndr', { params: filters }),
};
