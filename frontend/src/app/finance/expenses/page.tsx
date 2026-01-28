'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Select,
  Badge,
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableEmpty,
  Pagination,
  SectionLoader,
  Modal,
} from '@/components/ui';
import { formatCurrency, formatDate } from '@/lib/utils';
import {
  useExpenses,
  useExpensesSummary,
  useCreateExpense,
  useDeleteExpense,
} from '@/hooks';
import type { ExpenseFilters, ExpenseCategory, CreateExpenseRequest } from '@/services/finance.service';
import {
  Search,
  Filter,
  Download,
  Plus,
  Eye,
  Edit,
  Trash2,
  Receipt,
  ArrowLeft,
  Calendar,
} from 'lucide-react';

const categoryOptions = [
  { value: '', label: 'All Categories' },
  { value: 'Shipping', label: 'Shipping' },
  { value: 'PlatformFees', label: 'Platform Fees' },
  { value: 'PaymentProcessing', label: 'Payment Processing' },
  { value: 'Packaging', label: 'Packaging' },
  { value: 'Returns', label: 'Returns' },
  { value: 'RTO', label: 'RTO' },
  { value: 'Marketing', label: 'Marketing' },
  { value: 'Software', label: 'Software' },
  { value: 'Warehouse', label: 'Warehouse' },
  { value: 'Salaries', label: 'Salaries' },
  { value: 'OfficeExpenses', label: 'Office Expenses' },
  { value: 'Taxes', label: 'Taxes' },
  { value: 'Other', label: 'Other' },
];

const expenseSchema = z.object({
  category: z.string().min(1, 'Category is required'),
  amount: z.number().min(0.01, 'Amount must be greater than 0'),
  description: z.string().min(1, 'Description is required'),
  expenseDate: z.string().min(1, 'Date is required'),
  vendor: z.string().optional(),
  invoiceNumber: z.string().optional(),
  notes: z.string().optional(),
  isRecurring: z.boolean(),
});

type ExpenseFormData = z.infer<typeof expenseSchema>;

export default function ExpensesPage() {
  const router = useRouter();
  const [filters, setFilters] = useState<ExpenseFilters>({
    page: 1,
    pageSize: 10,
  });
  const [searchQuery, setSearchQuery] = useState('');
  const [showAddModal, setShowAddModal] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const apiFilters: ExpenseFilters = {
    ...filters,
    searchTerm: searchQuery || undefined,
  };

  const { data, isLoading, error } = useExpenses(apiFilters);
  const { data: summary } = useExpensesSummary({
    fromDate: filters.fromDate,
    toDate: filters.toDate,
  });
  const createExpenseMutation = useCreateExpense();
  const deleteExpenseMutation = useDeleteExpense();

  const expenses = data?.items || [];
  const totalItems = data?.totalCount || 0;
  const totalPages = data?.totalPages || 1;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ExpenseFormData>({
    resolver: zodResolver(expenseSchema),
    defaultValues: {
      expenseDate: new Date().toISOString().split('T')[0],
      isRecurring: false,
    },
  });

  const handleFilterChange = (key: keyof ExpenseFilters, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value || undefined,
      page: 1,
    }));
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  };

  const handlePageSizeChange = (size: number) => {
    setFilters((prev) => ({ ...prev, pageSize: size, page: 1 }));
  };

  const onSubmitExpense = async (formData: ExpenseFormData) => {
    try {
      await createExpenseMutation.mutateAsync({
        ...formData,
        category: formData.category as ExpenseCategory,
        currency: 'INR',
      } as CreateExpenseRequest);
      setShowAddModal(false);
      reset();
    } catch (err) {
      console.error('Failed to create expense:', err);
    }
  };

  const handleDelete = async () => {
    if (!deleteId) return;
    try {
      await deleteExpenseMutation.mutateAsync(deleteId);
      setDeleteId(null);
    } catch (err) {
      console.error('Failed to delete expense:', err);
    }
  };

  return (
    <DashboardLayout title="Expenses">
      {/* Header */}
      <div className="mb-6 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => router.push('/finance')}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold">Expenses</h1>
            <p className="text-sm text-muted-foreground">Manage your business expenses</p>
          </div>
        </div>
        <Button leftIcon={<Plus className="h-4 w-4" />} onClick={() => setShowAddModal(true)}>
          Add Expense
        </Button>
      </div>

      {/* Summary Cards */}
      {summary && (
        <div className="mb-6 grid gap-4 md:grid-cols-4">
          <Card>
            <CardContent className="p-4">
              <p className="text-sm text-muted-foreground">Total Expenses</p>
              <p className="text-2xl font-bold text-warning">
                {formatCurrency(summary.totalExpenses)}
              </p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <p className="text-sm text-muted-foreground">Expense Count</p>
              <p className="text-2xl font-bold">{summary.totalExpenseCount}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <p className="text-sm text-muted-foreground">Top Category</p>
              <p className="text-lg font-bold">
                {summary.expensesByCategory
                  ? Object.entries(summary.expensesByCategory).sort(([, a], [, b]) => b - a)[0]?.[0] || '-'
                  : '-'}
              </p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-4">
              <p className="text-sm text-muted-foreground">Average per Expense</p>
              <p className="text-2xl font-bold">
                {summary.totalExpenseCount > 0
                  ? formatCurrency(summary.totalExpenses / summary.totalExpenseCount)
                  : formatCurrency(0)}
              </p>
            </CardContent>
          </Card>
        </div>
      )}

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>All Expenses</CardTitle>
          <Button variant="outline" size="sm" leftIcon={<Download className="h-4 w-4" />}>
            Export
          </Button>
        </CardHeader>
        <CardContent>
          {/* Filters */}
          <div className="mb-6 flex flex-wrap items-center gap-4">
            <div className="flex-1 min-w-[200px]">
              <Input
                placeholder="Search expenses..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                leftIcon={<Search className="h-4 w-4" />}
              />
            </div>
            <Select
              options={categoryOptions}
              value={filters.category || ''}
              onChange={(e) => handleFilterChange('category', e.target.value)}
              className="w-44"
            />
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-muted-foreground" />
              <Input
                type="date"
                value={filters.fromDate || ''}
                onChange={(e) => handleFilterChange('fromDate', e.target.value)}
                className="w-36"
              />
              <span className="text-muted-foreground">to</span>
              <Input
                type="date"
                value={filters.toDate || ''}
                onChange={(e) => handleFilterChange('toDate', e.target.value)}
                className="w-36"
              />
            </div>
          </div>

          {/* Loading State */}
          {isLoading ? (
            <SectionLoader />
          ) : error ? (
            <div className="py-12 text-center text-error">
              Failed to load expenses. Please try again.
            </div>
          ) : (
            <>
              {/* Expenses Table */}
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Date</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead>Vendor</TableHead>
                    <TableHead className="text-right">Amount</TableHead>
                    <TableHead>Recurring</TableHead>
                    <TableHead className="w-20">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {expenses.length === 0 ? (
                    <TableEmpty
                      colSpan={7}
                      message="No expenses found"
                      icon={<Receipt className="h-8 w-8" />}
                    />
                  ) : (
                    expenses.map((expense) => (
                      <TableRow key={expense.id}>
                        <TableCell className="text-sm">
                          {formatDate(expense.expenseDate)}
                        </TableCell>
                        <TableCell>
                          <CategoryBadge category={expense.category} />
                        </TableCell>
                        <TableCell>
                          <p className="font-medium">{expense.description}</p>
                          {expense.invoiceNumber && (
                            <p className="text-xs text-muted-foreground">
                              Invoice: {expense.invoiceNumber}
                            </p>
                          )}
                        </TableCell>
                        <TableCell>{expense.vendor || '-'}</TableCell>
                        <TableCell className="text-right font-medium text-warning">
                          {formatCurrency(expense.amount)}
                        </TableCell>
                        <TableCell>
                          {expense.isRecurring ? (
                            <Badge variant="info" size="sm">
                              Recurring
                            </Badge>
                          ) : (
                            <span className="text-muted-foreground">-</span>
                          )}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <button
                              className="rounded p-1.5 hover:bg-muted"
                              title="Edit"
                            >
                              <Edit className="h-4 w-4 text-muted-foreground" />
                            </button>
                            <button
                              className="rounded p-1.5 hover:bg-muted"
                              title="Delete"
                              onClick={() => setDeleteId(expense.id)}
                            >
                              <Trash2 className="h-4 w-4 text-error" />
                            </button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>

              {/* Pagination */}
              {expenses.length > 0 && (
                <div className="mt-4">
                  <Pagination
                    currentPage={filters.page || 1}
                    totalPages={totalPages}
                    totalItems={totalItems}
                    pageSize={filters.pageSize || 10}
                    onPageChange={handlePageChange}
                    onPageSizeChange={handlePageSizeChange}
                  />
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Add Expense Modal */}
      <Modal
        isOpen={showAddModal}
        onClose={() => {
          setShowAddModal(false);
          reset();
        }}
        title="Add Expense"
      >
        <form onSubmit={handleSubmit(onSubmitExpense)} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">
                Category <span className="text-error">*</span>
              </label>
              <Select
                options={categoryOptions.filter((o) => o.value !== '')}
                {...register('category')}
              />
              {errors.category && (
                <p className="text-sm text-error mt-1">{errors.category.message}</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">
                Date <span className="text-error">*</span>
              </label>
              <Input type="date" {...register('expenseDate')} />
              {errors.expenseDate && (
                <p className="text-sm text-error mt-1">{errors.expenseDate.message}</p>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">
              Amount <span className="text-error">*</span>
            </label>
            <Input
              type="number"
              step="0.01"
              min="0"
              {...register('amount', { valueAsNumber: true })}
              placeholder="0.00"
            />
            {errors.amount && (
              <p className="text-sm text-error mt-1">{errors.amount.message}</p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">
              Description <span className="text-error">*</span>
            </label>
            <Input {...register('description')} placeholder="What was this expense for?" />
            {errors.description && (
              <p className="text-sm text-error mt-1">{errors.description.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">Vendor</label>
              <Input {...register('vendor')} placeholder="Vendor name" />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">Invoice Number</label>
              <Input {...register('invoiceNumber')} placeholder="INV-001" />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Notes</label>
            <textarea
              {...register('notes')}
              className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm min-h-[80px]"
              placeholder="Additional notes..."
            />
          </div>

          <div className="flex items-center gap-2">
            <input
              type="checkbox"
              id="isRecurring"
              {...register('isRecurring')}
              className="rounded border-input"
            />
            <label htmlFor="isRecurring" className="text-sm">
              This is a recurring expense
            </label>
          </div>

          <div className="flex justify-end gap-2 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowAddModal(false);
                reset();
              }}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              isLoading={isSubmitting || createExpenseMutation.isPending}
            >
              Add Expense
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deleteId}
        onClose={() => setDeleteId(null)}
        title="Delete Expense"
      >
        <p className="text-muted-foreground">
          Are you sure you want to delete this expense? This action cannot be undone.
        </p>
        <div className="flex justify-end gap-2 mt-6">
          <Button variant="outline" onClick={() => setDeleteId(null)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            onClick={handleDelete}
            isLoading={deleteExpenseMutation.isPending}
          >
            Delete
          </Button>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

function CategoryBadge({ category }: { category: ExpenseCategory }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default' | 'primary'> = {
    Shipping: 'info',
    PlatformFees: 'warning',
    PaymentProcessing: 'default',
    Packaging: 'default',
    Returns: 'error',
    RTO: 'error',
    Marketing: 'primary',
    Software: 'info',
    Warehouse: 'default',
    Salaries: 'success',
    OfficeExpenses: 'default',
    Taxes: 'warning',
    Other: 'default',
  };

  const labels: Record<string, string> = {
    Shipping: 'Shipping',
    PlatformFees: 'Platform Fees',
    PaymentProcessing: 'Payment',
    Packaging: 'Packaging',
    Returns: 'Returns',
    RTO: 'RTO',
    Marketing: 'Marketing',
    Software: 'Software',
    Warehouse: 'Warehouse',
    Salaries: 'Salaries',
    OfficeExpenses: 'Office',
    Taxes: 'Taxes',
    Other: 'Other',
  };

  return (
    <Badge variant={variants[category] || 'default'} size="sm">
      {labels[category] || category}
    </Badge>
  );
}
