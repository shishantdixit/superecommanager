import { get, post, put, del } from '@/lib/api-client';
import type { PaginatedResponse } from '@/types/api';

// Expense category enum
export type ExpenseCategory =
  | 'Shipping'
  | 'PlatformFees'
  | 'PaymentProcessing'
  | 'Packaging'
  | 'Returns'
  | 'RTO'
  | 'Marketing'
  | 'Software'
  | 'Warehouse'
  | 'Salaries'
  | 'OfficeExpenses'
  | 'Taxes'
  | 'Other';

// Expense types
export interface ExpenseListItem {
  id: string;
  category: ExpenseCategory;
  categoryName: string;
  amount: number;
  currency: string;
  description: string;
  expenseDate: string;
  vendor?: string;
  invoiceNumber?: string;
  isRecurring: boolean;
  createdAt: string;
}

export interface ExpenseDetail extends ExpenseListItem {
  referenceType?: string;
  referenceId?: string;
  notes?: string;
  recordedByUserId?: string;
  recordedByUserName?: string;
  updatedAt?: string;
}

export interface CreateExpenseRequest {
  category: ExpenseCategory;
  amount: number;
  currency?: string;
  description: string;
  expenseDate: string;
  referenceType?: string;
  referenceId?: string;
  vendor?: string;
  invoiceNumber?: string;
  notes?: string;
  isRecurring?: boolean;
}

export interface UpdateExpenseRequest {
  category: ExpenseCategory;
  amount: number;
  currency?: string;
  description: string;
  expenseDate: string;
  notes?: string;
}

export interface ExpenseFilters {
  category?: ExpenseCategory;
  fromDate?: string;
  toDate?: string;
  minAmount?: number;
  maxAmount?: number;
  vendor?: string;
  isRecurring?: boolean;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

// Revenue types
export interface RevenueStats {
  totalRevenue: number;
  totalOrders: number;
  averageOrderValue: number;
  currency: string;
  deliveredRevenue: number;
  pendingRevenue: number;
  cancelledRevenue: number;
  rtoRevenue: number;
  prepaidRevenue: number;
  codRevenue: number;
  totalOrderCount: number;
  deliveredOrderCount: number;
  pendingOrderCount: number;
  cancelledOrderCount: number;
  rtoOrderCount: number;
  revenueByChannel: Record<string, number>;
  dailyRevenue: FinanceDailyRevenue[];
}

export interface FinanceDailyRevenue {
  date: string;
  revenue: number;
  orderCount: number;
}

// Expenses summary
export interface ExpensesSummary {
  totalExpenses: number;
  currency: string;
  totalExpenseCount: number;
  expensesByCategory: Record<string, number>;
  countByCategory: Record<string, number>;
  topExpenses: ExpenseListItem[];
  dailyExpenses: DailyExpense[];
}

export interface DailyExpense {
  date: string;
  amount: number;
  count: number;
}

// P&L Report
export interface ProfitLossReport {
  fromDate: string;
  toDate: string;
  currency: string;
  grossRevenue: number;
  discounts: number;
  returns: number;
  netRevenue: number;
  costOfGoodsSold: number;
  grossProfit: number;
  grossProfitMargin: number;
  shippingExpenses: number;
  platformFees: number;
  paymentProcessingFees: number;
  packagingExpenses: number;
  returnExpenses: number;
  rtoExpenses: number;
  marketingExpenses: number;
  otherExpenses: number;
  totalOperatingExpenses: number;
  operatingProfit: number;
  operatingProfitMargin: number;
  totalOrders: number;
  deliveredOrders: number;
  cancelledOrders: number;
  returnedOrders: number;
  rtoOrders: number;
  fulfillmentRate: number;
  expenseBreakdown: Record<string, number>;
  monthlyTrend: MonthlyProfitLoss[];
}

export interface MonthlyProfitLoss {
  year: number;
  month: number;
  monthName: string;
  revenue: number;
  expenses: number;
  profit: number;
  profitMargin: number;
  orderCount: number;
}

// Order Financials
export interface OrderFinancials {
  orderId: string;
  orderNumber: string;
  orderDate: string;
  channelName: string;
  status: string;
  subtotal: number;
  discountAmount: number;
  taxAmount: number;
  shippingCharged: number;
  totalAmount: number;
  currency: string;
  costOfGoods: number;
  shippingCost: number;
  platformFee: number;
  paymentProcessingFee: number;
  packagingCost: number;
  totalCosts: number;
  grossProfit: number;
  netProfit: number;
  profitMargin: number;
  items: OrderItemFinancials[];
  associatedExpenses: ExpenseListItem[];
}

export interface OrderItemFinancials {
  itemId: string;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
  unitCost: number;
  totalPrice: number;
  totalCost: number;
  itemProfit: number;
  profitMargin: number;
}

export interface OrderFinancialsSummary {
  fromDate: string;
  toDate: string;
  currency: string;
  totalOrders: number;
  totalRevenue: number;
  totalCosts: number;
  totalProfit: number;
  averageOrderValue: number;
  averageProfit: number;
  averageProfitMargin: number;
  profitableOrders: number;
  unprofitableOrders: number;
  topProfitableOrders: OrderFinancials[];
  leastProfitableOrders: OrderFinancials[];
}

export interface DateRangeFilter {
  fromDate?: string;
  toDate?: string;
}

export const financeService = {
  // Expenses
  getExpenses: (filters: ExpenseFilters = {}) =>
    get<PaginatedResponse<ExpenseListItem>>('/finance/expenses', { params: filters }),

  getExpenseById: (id: string) =>
    get<ExpenseDetail>(`/finance/expenses/${id}`),

  createExpense: (data: CreateExpenseRequest) =>
    post<ExpenseDetail, CreateExpenseRequest>('/finance/expenses', data),

  updateExpense: (id: string, data: UpdateExpenseRequest) =>
    put<ExpenseDetail, UpdateExpenseRequest>(`/finance/expenses/${id}`, data),

  deleteExpense: (id: string) =>
    del<void>(`/finance/expenses/${id}`),

  getExpensesSummary: (filters: DateRangeFilter = {}) =>
    get<ExpensesSummary>('/finance/expenses/summary', { params: filters }),

  // Revenue
  getRevenueStats: (filters: DateRangeFilter = {}) =>
    get<RevenueStats>('/finance/revenue/stats', { params: filters }),

  // P&L Report
  getProfitLossReport: (filters: DateRangeFilter = {}) =>
    get<ProfitLossReport>('/finance/profit-loss', { params: filters }),

  // Order Financials
  getOrderFinancials: (orderId: string) =>
    get<OrderFinancials>(`/finance/orders/${orderId}`),

  getOrderFinancialsSummary: (filters: DateRangeFilter = {}) =>
    get<OrderFinancialsSummary>('/finance/orders/summary', { params: filters }),
};
