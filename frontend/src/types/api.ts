/**
 * API response wrapper types.
 */

export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode: number;
}

/**
 * Authentication types.
 */
export interface LoginRequest {
  email: string;
  password: string;
  tenantSlug: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    roleName?: string;
    permissions: string[];
  };
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  tenantId?: string;
  tenantName?: string;
  roles: string[];
  permissions: string[];
}

/**
 * Order types.
 */
export interface Order {
  id: string;
  orderNumber: string;
  externalOrderId: string;
  channelType: string;
  status: OrderStatus;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  shippingAddress: Address;
  billingAddress?: Address;
  items: OrderItem[];
  subtotal: number;
  shippingCharges: number;
  discount: number;
  tax: number;
  total: number;
  paymentMethod: string;
  paymentStatus: PaymentStatus;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface OrderItem {
  id: string;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  tax: number;
  total: number;
}

export interface Address {
  name: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone?: string;
}

export type OrderStatus =
  | 'Pending'
  | 'Confirmed'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'
  | 'Returned'
  | 'RTO';

export type PaymentStatus = 'Pending' | 'Paid' | 'Failed' | 'Refunded' | 'COD';

/**
 * Shipment types.
 */
export interface Shipment {
  id: string;
  orderId: string;
  orderNumber: string;
  awbNumber: string;
  courierCode: string;
  courierName: string;
  status: ShipmentStatus;
  estimatedDeliveryDate?: string;
  actualDeliveryDate?: string;
  weight: number;
  dimensions?: Dimensions;
  shippingCost: number;
  trackingUrl?: string;
  trackingHistory: TrackingEvent[];
  createdAt: string;
  updatedAt: string;
}

export interface Dimensions {
  length: number;
  width: number;
  height: number;
}

export interface TrackingEvent {
  status: string;
  location: string;
  timestamp: string;
  description: string;
}

export type ShipmentStatus =
  | 'Created'
  | 'Manifested'
  | 'PickedUp'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'NDR'
  | 'RTOInitiated'
  | 'RTODelivered'
  | 'Cancelled';

/**
 * NDR types.
 */
export interface NdrCase {
  id: string;
  shipmentId: string;
  orderId: string;
  orderNumber: string;
  awbNumber: string;
  status: NdrStatus;
  reason: string;
  reasonCode: string;
  customerName: string;
  customerPhone: string;
  address: Address;
  assignedToId?: string;
  assignedToName?: string;
  priority: NdrPriority;
  attemptCount: number;
  nextActionDate?: string;
  remarks?: string;
  actions: NdrAction[];
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
}

export interface NdrAction {
  id: string;
  actionType: string;
  outcome?: string;
  remarks?: string;
  performedById: string;
  performedByName: string;
  performedAt: string;
}

export type NdrStatus =
  | 'Open'
  | 'InProgress'
  | 'ReattemptScheduled'
  | 'Resolved'
  | 'RTOInitiated'
  | 'Closed';

export type NdrPriority = 'Low' | 'Medium' | 'High' | 'Critical';

/**
 * Inventory types.
 */
export interface InventoryItem {
  id: string;
  sku: string;
  name: string;
  description?: string;
  category?: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  reorderPoint: number;
  reorderQuantity: number;
  costPrice: number;
  sellingPrice: number;
  location?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface StockMovement {
  id: string;
  inventoryItemId: string;
  sku: string;
  movementType: StockMovementType;
  quantity: number;
  referenceType?: string;
  referenceId?: string;
  notes?: string;
  createdAt: string;
  createdById: string;
  createdByName: string;
}

export type StockMovementType =
  | 'Purchase'
  | 'Sale'
  | 'Return'
  | 'Adjustment'
  | 'Transfer'
  | 'Damage'
  | 'RTO';

/**
 * Employee types.
 */
export interface Employee {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  department?: string;
  roleId: string;
  roleName: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
}

export interface Role {
  id: string;
  name: string;
  description?: string;
  permissions: string[];
  isSystem: boolean;
}

/**
 * Dashboard types.
 */
export interface DashboardStats {
  totalOrders: number;
  totalRevenue: number;
  pendingOrders: number;
  shippedOrders: number;
  deliveredOrders: number;
  openNdrCases: number;
  lowStockItems: number;
  todayOrders: number;
  todayRevenue: number;
}

export interface ChartData {
  labels: string[];
  datasets: {
    label: string;
    data: number[];
    backgroundColor?: string;
    borderColor?: string;
  }[];
}

/**
 * Filter and pagination types.
 */
export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
}

export interface OrderFilters extends PaginationParams {
  status?: OrderStatus;
  channelType?: string;
  paymentStatus?: PaymentStatus;
  fromDate?: string;
  toDate?: string;
  search?: string;
}

export interface ShipmentFilters extends PaginationParams {
  status?: ShipmentStatus;
  courierCode?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
}

export interface NdrFilters extends PaginationParams {
  status?: NdrStatus;
  priority?: NdrPriority;
  assignedToId?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
}

export interface InventoryFilters extends PaginationParams {
  category?: string;
  lowStock?: boolean;
  outOfStock?: boolean;
  search?: string;
}

/**
 * Channel configuration types.
 */
export interface ChannelConfig {
  id: string;
  channelType: string;
  name: string;
  isActive: boolean;
  credentials: Record<string, string>;
  settings: Record<string, unknown>;
  lastSyncAt?: string;
  createdAt: string;
}

/**
 * Notification types.
 */
export interface Notification {
  id: string;
  type: string;
  title: string;
  message: string;
  data?: Record<string, unknown>;
  isRead: boolean;
  createdAt: string;
}

/**
 * Platform Admin types.
 */
export interface PlatformAdminLoginRequest {
  email: string;
  password: string;
}

export interface PlatformAdminLoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  admin: PlatformAdmin;
}

export interface PlatformAdmin {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isSuperAdmin: boolean;
  isActive: boolean;
  lastLoginAt?: string;
}

export interface TenantSummary {
  id: string;
  name: string;
  slug: string;
  status: TenantStatus;
  ownerEmail: string;
  ownerName: string;
  planName?: string;
  subscriptionStatus?: SubscriptionStatus;
  trialEndsAt?: string;
  isTrialActive: boolean;
  userCount: number;
  orderCount: number;
  createdAt: string;
}

export interface TenantDetail extends TenantSummary {
  address?: string;
  phone?: string;
  website?: string;
  logoUrl?: string;
  subscription?: Subscription;
  features: string[];
  settings: Record<string, unknown>;
}

export type TenantStatus = 'Active' | 'Suspended' | 'Deactivated' | 'PendingSetup';

export interface Subscription {
  id: string;
  planId: string;
  planName: string;
  status: SubscriptionStatus;
  startDate: string;
  endDate?: string;
  trialEndsAt?: string;
  isTrialActive: boolean;
  billingCycle: 'Monthly' | 'Yearly';
  amount: number;
}

export type SubscriptionStatus = 'Active' | 'Trialing' | 'PastDue' | 'Cancelled' | 'Expired';

export interface Plan {
  id: string;
  name: string;
  code: string;
  description?: string;
  monthlyPrice: number;
  yearlyPrice: number;
  trialDays: number;
  maxUsers: number;
  maxOrdersPerMonth: number;
  features: PlanFeature[];
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
  createdAt: string;
}

export interface PlanFeature {
  code: string;
  name: string;
  description?: string;
  isEnabled: boolean;
}

export interface Feature {
  id: string;
  code: string;
  name: string;
  description?: string;
  module: string;
  isActive: boolean;
}

export interface PlatformStats {
  totalTenants: number;
  activeTenants: number;
  trialingTenants: number;
  suspendedTenants: number;
  totalUsers: number;
  totalOrders: number;
  monthlyRecurringRevenue: number;
  averageRevenuePerTenant: number;
  tenantGrowth: TenantGrowth[];
  revenueGrowth: RevenueGrowth[];
}

export interface TenantGrowth {
  month: string;
  newTenants: number;
  churnedTenants: number;
  totalTenants: number;
}

export interface RevenueGrowth {
  month: string;
  mrr: number;
  growth: number;
}

export interface TenantActivityLog {
  id: string;
  tenantId: string;
  tenantName: string;
  action: string;
  description: string;
  performedById?: string;
  performedByName?: string;
  performedByType: 'PlatformAdmin' | 'TenantUser' | 'System';
  ipAddress?: string;
  metadata?: Record<string, unknown>;
  createdAt: string;
}

export interface PlatformSetting {
  id: string;
  key: string;
  value: string;
  category: PlatformSettingCategory;
  description?: string;
  isEncrypted: boolean;
  updatedAt: string;
  updatedBy?: string;
}

export type PlatformSettingCategory =
  | 'General'
  | 'Email'
  | 'Security'
  | 'Payment'
  | 'Notification'
  | 'Integration'
  | 'Feature';

export interface CreateTenantRequest {
  name: string;
  slug: string;
  ownerEmail: string;
  ownerFirstName: string;
  ownerLastName: string;
  ownerPassword: string;
  planId?: string;
  trialDays?: number;
}

export interface CreatePlanRequest {
  name: string;
  code: string;
  description?: string;
  monthlyPrice: number;
  yearlyPrice: number;
  trialDays: number;
  maxUsers: number;
  maxOrdersPerMonth: number;
  featureCodes: string[];
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
}

export interface UpdatePlanRequest extends Partial<CreatePlanRequest> {
  id: string;
}

export interface CreatePlatformAdminRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  isSuperAdmin: boolean;
}

export interface UpdatePlatformAdminRequest {
  id: string;
  firstName?: string;
  lastName?: string;
  isActive?: boolean;
}

export interface TenantFilters extends PaginationParams {
  status?: TenantStatus;
  planId?: string;
  search?: string;
  isTrialActive?: boolean;
  fromDate?: string;
  toDate?: string;
}

export interface ActivityLogFilters extends PaginationParams {
  tenantId?: string;
  action?: string;
  performedByType?: string;
  fromDate?: string;
  toDate?: string;
}

export interface PlatformAdminFilters extends PaginationParams {
  isActive?: boolean;
  isSuperAdmin?: boolean;
  search?: string;
}
