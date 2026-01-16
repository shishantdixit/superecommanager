import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  financeService,
  ExpenseFilters,
  CreateExpenseRequest,
  UpdateExpenseRequest,
  DateRangeFilter,
} from '@/services/finance.service';

export const financeKeys = {
  all: ['finance'] as const,
  expenses: () => [...financeKeys.all, 'expenses'] as const,
  expenseList: (filters: ExpenseFilters) => [...financeKeys.expenses(), 'list', filters] as const,
  expenseDetail: (id: string) => [...financeKeys.expenses(), 'detail', id] as const,
  expensesSummary: (filters: DateRangeFilter) => [...financeKeys.expenses(), 'summary', filters] as const,
  revenue: () => [...financeKeys.all, 'revenue'] as const,
  revenueStats: (filters: DateRangeFilter) => [...financeKeys.revenue(), 'stats', filters] as const,
  profitLoss: (filters: DateRangeFilter) => [...financeKeys.all, 'profit-loss', filters] as const,
  orderFinancials: (orderId: string) => [...financeKeys.all, 'order-financials', orderId] as const,
  orderFinancialsSummary: (filters: DateRangeFilter) => [...financeKeys.all, 'order-financials-summary', filters] as const,
};

/**
 * Hook to fetch paginated expenses.
 */
export function useExpenses(filters: ExpenseFilters = {}) {
  return useQuery({
    queryKey: financeKeys.expenseList(filters),
    queryFn: () => financeService.getExpenses(filters),
  });
}

/**
 * Hook to fetch a single expense by ID.
 */
export function useExpense(id: string) {
  return useQuery({
    queryKey: financeKeys.expenseDetail(id),
    queryFn: () => financeService.getExpenseById(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch expenses summary.
 */
export function useExpensesSummary(filters: DateRangeFilter = {}) {
  return useQuery({
    queryKey: financeKeys.expensesSummary(filters),
    queryFn: () => financeService.getExpensesSummary(filters),
  });
}

/**
 * Hook to fetch revenue statistics.
 */
export function useRevenueStats(filters: DateRangeFilter = {}) {
  return useQuery({
    queryKey: financeKeys.revenueStats(filters),
    queryFn: () => financeService.getRevenueStats(filters),
  });
}

/**
 * Hook to fetch profit & loss report.
 */
export function useProfitLossReport(filters: DateRangeFilter = {}) {
  return useQuery({
    queryKey: financeKeys.profitLoss(filters),
    queryFn: () => financeService.getProfitLossReport(filters),
  });
}

/**
 * Hook to fetch order financials.
 */
export function useOrderFinancials(orderId: string) {
  return useQuery({
    queryKey: financeKeys.orderFinancials(orderId),
    queryFn: () => financeService.getOrderFinancials(orderId),
    enabled: !!orderId,
  });
}

/**
 * Hook to fetch order financials summary.
 */
export function useOrderFinancialsSummary(filters: DateRangeFilter = {}) {
  return useQuery({
    queryKey: financeKeys.orderFinancialsSummary(filters),
    queryFn: () => financeService.getOrderFinancialsSummary(filters),
  });
}

/**
 * Hook to create an expense.
 */
export function useCreateExpense() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateExpenseRequest) => financeService.createExpense(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financeKeys.expenses() });
      queryClient.invalidateQueries({ queryKey: financeKeys.profitLoss({}) });
    },
  });
}

/**
 * Hook to update an expense.
 */
export function useUpdateExpense() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateExpenseRequest }) =>
      financeService.updateExpense(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: financeKeys.expenseDetail(id) });
      queryClient.invalidateQueries({ queryKey: financeKeys.expenses() });
      queryClient.invalidateQueries({ queryKey: financeKeys.profitLoss({}) });
    },
  });
}

/**
 * Hook to delete an expense.
 */
export function useDeleteExpense() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => financeService.deleteExpense(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: financeKeys.expenses() });
      queryClient.invalidateQueries({ queryKey: financeKeys.profitLoss({}) });
    },
  });
}
