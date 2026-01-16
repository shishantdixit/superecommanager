'use client';

import { useState, useMemo } from 'react';
import Link from 'next/link';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Badge,
  SectionLoader,
} from '@/components/ui';
import { formatCurrency, formatDate } from '@/lib/utils';
import { useProfitLossReport, useRevenueStats, useExpensesSummary } from '@/hooks';
import type { DateRangeFilter } from '@/services/finance.service';
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  CreditCard,
  Receipt,
  ArrowRight,
  PieChart,
  BarChart3,
  Calendar,
  Package,
  Truck,
  RefreshCw,
} from 'lucide-react';

export default function FinancePage() {
  const [dateRange, setDateRange] = useState<DateRangeFilter>(() => {
    const today = new Date();
    const firstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    return {
      fromDate: firstOfMonth.toISOString().split('T')[0],
      toDate: today.toISOString().split('T')[0],
    };
  });

  const { data: plReport, isLoading: plLoading } = useProfitLossReport(dateRange);
  const { data: revenueStats, isLoading: revenueLoading } = useRevenueStats(dateRange);
  const { data: expensesSummary, isLoading: expensesLoading } = useExpensesSummary(dateRange);

  const isLoading = plLoading || revenueLoading || expensesLoading;

  const profitColor = useMemo(() => {
    if (!plReport) return 'text-foreground';
    return plReport.operatingProfit >= 0 ? 'text-success' : 'text-error';
  }, [plReport]);

  return (
    <DashboardLayout title="Finance">
      {/* Date Range Filter */}
      <div className="mb-6 flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <Calendar className="h-4 w-4 text-muted-foreground" />
            <Input
              type="date"
              value={dateRange.fromDate || ''}
              onChange={(e) => setDateRange((prev) => ({ ...prev, fromDate: e.target.value }))}
              className="w-40"
            />
            <span className="text-muted-foreground">to</span>
            <Input
              type="date"
              value={dateRange.toDate || ''}
              onChange={(e) => setDateRange((prev) => ({ ...prev, toDate: e.target.value }))}
              className="w-40"
            />
          </div>
        </div>
        <div className="flex gap-2">
          <Link href="/finance/expenses">
            <Button variant="outline" leftIcon={<Receipt className="h-4 w-4" />}>
              Manage Expenses
            </Button>
          </Link>
        </div>
      </div>

      {isLoading ? (
        <SectionLoader />
      ) : (
        <>
          {/* Key Metrics */}
          <div className="mb-6 grid gap-4 md:grid-cols-4">
            <MetricCard
              label="Net Revenue"
              value={formatCurrency(plReport?.netRevenue ?? 0)}
              icon={<DollarSign className="h-5 w-5 text-primary" />}
              trend={revenueStats?.dailyRevenue?.length ? 'up' : undefined}
            />
            <MetricCard
              label="Total Expenses"
              value={formatCurrency(plReport?.totalOperatingExpenses ?? 0)}
              icon={<CreditCard className="h-5 w-5 text-warning" />}
            />
            <MetricCard
              label="Operating Profit"
              value={formatCurrency(plReport?.operatingProfit ?? 0)}
              icon={<TrendingUp className="h-5 w-5 text-success" />}
              valueClassName={profitColor}
            />
            <MetricCard
              label="Profit Margin"
              value={`${(plReport?.operatingProfitMargin ?? 0).toFixed(1)}%`}
              icon={<PieChart className="h-5 w-5 text-info" />}
              valueClassName={profitColor}
            />
          </div>

          <div className="grid gap-6 lg:grid-cols-2">
            {/* P&L Summary */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <CardTitle className="flex items-center gap-2">
                  <BarChart3 className="h-5 w-5" />
                  Profit & Loss Summary
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {/* Revenue Section */}
                  <div>
                    <h4 className="font-medium text-sm text-muted-foreground mb-2">Revenue</h4>
                    <div className="space-y-2">
                      <PLRow label="Gross Revenue" value={plReport?.grossRevenue ?? 0} />
                      <PLRow label="Discounts" value={-(plReport?.discounts ?? 0)} negative />
                      <PLRow label="Returns" value={-(plReport?.returns ?? 0)} negative />
                      <PLRow
                        label="Net Revenue"
                        value={plReport?.netRevenue ?? 0}
                        bold
                        className="border-t pt-2"
                      />
                    </div>
                  </div>

                  {/* Cost Section */}
                  <div>
                    <h4 className="font-medium text-sm text-muted-foreground mb-2">
                      Cost of Goods
                    </h4>
                    <div className="space-y-2">
                      <PLRow label="COGS" value={-(plReport?.costOfGoodsSold ?? 0)} negative />
                      <PLRow
                        label="Gross Profit"
                        value={plReport?.grossProfit ?? 0}
                        bold
                        className="border-t pt-2"
                        highlight={plReport?.grossProfit ?? 0 >= 0 ? 'success' : 'error'}
                      />
                    </div>
                  </div>

                  {/* Expenses Section */}
                  <div>
                    <h4 className="font-medium text-sm text-muted-foreground mb-2">
                      Operating Expenses
                    </h4>
                    <div className="space-y-2">
                      <PLRow
                        label="Shipping"
                        value={-(plReport?.shippingExpenses ?? 0)}
                        negative
                      />
                      <PLRow
                        label="Platform Fees"
                        value={-(plReport?.platformFees ?? 0)}
                        negative
                      />
                      <PLRow
                        label="Payment Processing"
                        value={-(plReport?.paymentProcessingFees ?? 0)}
                        negative
                      />
                      <PLRow
                        label="Marketing"
                        value={-(plReport?.marketingExpenses ?? 0)}
                        negative
                      />
                      <PLRow
                        label="RTO Costs"
                        value={-(plReport?.rtoExpenses ?? 0)}
                        negative
                      />
                      <PLRow
                        label="Other"
                        value={-(plReport?.otherExpenses ?? 0)}
                        negative
                      />
                      <PLRow
                        label="Total Expenses"
                        value={-(plReport?.totalOperatingExpenses ?? 0)}
                        bold
                        negative
                        className="border-t pt-2"
                      />
                    </div>
                  </div>

                  {/* Bottom Line */}
                  <div className="border-t-2 pt-3">
                    <PLRow
                      label="Operating Profit"
                      value={plReport?.operatingProfit ?? 0}
                      bold
                      highlight={(plReport?.operatingProfit ?? 0) >= 0 ? 'success' : 'error'}
                      large
                    />
                    <p className="text-xs text-muted-foreground mt-1">
                      Margin: {(plReport?.operatingProfitMargin ?? 0).toFixed(1)}%
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Revenue by Channel */}
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle>Revenue by Channel</CardTitle>
                </CardHeader>
                <CardContent>
                  {revenueStats?.revenueByChannel &&
                  Object.keys(revenueStats.revenueByChannel).length > 0 ? (
                    <div className="space-y-3">
                      {Object.entries(revenueStats.revenueByChannel).map(([channel, revenue]) => {
                        const percentage = revenueStats.totalRevenue
                          ? (revenue / revenueStats.totalRevenue) * 100
                          : 0;
                        return (
                          <div key={channel}>
                            <div className="flex items-center justify-between mb-1">
                              <span className="font-medium">{channel}</span>
                              <span>{formatCurrency(revenue)}</span>
                            </div>
                            <div className="h-2 rounded-full bg-muted overflow-hidden">
                              <div
                                className="h-full bg-primary rounded-full"
                                style={{ width: `${percentage}%` }}
                              />
                            </div>
                            <p className="text-xs text-muted-foreground mt-0.5">
                              {percentage.toFixed(1)}% of total
                            </p>
                          </div>
                        );
                      })}
                    </div>
                  ) : (
                    <p className="text-center text-muted-foreground py-8">No revenue data</p>
                  )}
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Expenses by Category</CardTitle>
                </CardHeader>
                <CardContent>
                  {expensesSummary?.expensesByCategory &&
                  Object.keys(expensesSummary.expensesByCategory).length > 0 ? (
                    <div className="space-y-3">
                      {Object.entries(expensesSummary.expensesByCategory)
                        .sort(([, a], [, b]) => b - a)
                        .slice(0, 5)
                        .map(([category, amount]) => {
                          const percentage = expensesSummary.totalExpenses
                            ? (amount / expensesSummary.totalExpenses) * 100
                            : 0;
                          return (
                            <div key={category}>
                              <div className="flex items-center justify-between mb-1">
                                <span className="font-medium">{category}</span>
                                <span>{formatCurrency(amount)}</span>
                              </div>
                              <div className="h-2 rounded-full bg-muted overflow-hidden">
                                <div
                                  className="h-full bg-warning rounded-full"
                                  style={{ width: `${percentage}%` }}
                                />
                              </div>
                            </div>
                          );
                        })}
                    </div>
                  ) : (
                    <p className="text-center text-muted-foreground py-8">No expense data</p>
                  )}
                  <Link
                    href="/finance/expenses"
                    className="mt-4 flex items-center justify-center gap-1 text-sm text-primary hover:underline"
                  >
                    View all expenses <ArrowRight className="h-4 w-4" />
                  </Link>
                </CardContent>
              </Card>
            </div>
          </div>

          {/* Order Metrics */}
          <div className="mt-6 grid gap-4 md:grid-cols-5">
            <OrderMetricCard
              label="Total Orders"
              value={plReport?.totalOrders ?? 0}
              icon={<Package className="h-5 w-5" />}
            />
            <OrderMetricCard
              label="Delivered"
              value={plReport?.deliveredOrders ?? 0}
              icon={<Truck className="h-5 w-5" />}
              variant="success"
            />
            <OrderMetricCard
              label="Cancelled"
              value={plReport?.cancelledOrders ?? 0}
              icon={<TrendingDown className="h-5 w-5" />}
              variant="error"
            />
            <OrderMetricCard
              label="RTO"
              value={plReport?.rtoOrders ?? 0}
              icon={<RefreshCw className="h-5 w-5" />}
              variant="warning"
            />
            <OrderMetricCard
              label="Fulfillment Rate"
              value={`${(plReport?.fulfillmentRate ?? 0).toFixed(1)}%`}
              icon={<PieChart className="h-5 w-5" />}
              isText
            />
          </div>

          {/* Monthly Trend */}
          {plReport?.monthlyTrend && plReport.monthlyTrend.length > 0 && (
            <Card className="mt-6">
              <CardHeader>
                <CardTitle>Monthly Trend</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b">
                        <th className="py-2 text-left font-medium">Month</th>
                        <th className="py-2 text-right font-medium">Revenue</th>
                        <th className="py-2 text-right font-medium">Expenses</th>
                        <th className="py-2 text-right font-medium">Profit</th>
                        <th className="py-2 text-right font-medium">Margin</th>
                        <th className="py-2 text-right font-medium">Orders</th>
                      </tr>
                    </thead>
                    <tbody>
                      {plReport.monthlyTrend.map((month) => (
                        <tr key={`${month.year}-${month.month}`} className="border-b">
                          <td className="py-2 font-medium">{month.monthName}</td>
                          <td className="py-2 text-right">{formatCurrency(month.revenue)}</td>
                          <td className="py-2 text-right text-warning">
                            {formatCurrency(month.expenses)}
                          </td>
                          <td
                            className={`py-2 text-right font-medium ${
                              month.profit >= 0 ? 'text-success' : 'text-error'
                            }`}
                          >
                            {formatCurrency(month.profit)}
                          </td>
                          <td
                            className={`py-2 text-right ${
                              month.profitMargin >= 0 ? 'text-success' : 'text-error'
                            }`}
                          >
                            {month.profitMargin.toFixed(1)}%
                          </td>
                          <td className="py-2 text-right">{month.orderCount}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </DashboardLayout>
  );
}

function MetricCard({
  label,
  value,
  icon,
  trend,
  valueClassName,
}: {
  label: string;
  value: string;
  icon: React.ReactNode;
  trend?: 'up' | 'down';
  valueClassName?: string;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-muted-foreground">{label}</p>
            <p className={`text-2xl font-bold ${valueClassName || ''}`}>{value}</p>
          </div>
          <div className="flex flex-col items-center">
            {icon}
            {trend && (
              <span className={`text-xs ${trend === 'up' ? 'text-success' : 'text-error'}`}>
                {trend === 'up' ? '↑' : '↓'}
              </span>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function PLRow({
  label,
  value,
  bold,
  negative,
  highlight,
  large,
  className,
}: {
  label: string;
  value: number;
  bold?: boolean;
  negative?: boolean;
  highlight?: 'success' | 'error';
  large?: boolean;
  className?: string;
}) {
  const colorClass = highlight
    ? highlight === 'success'
      ? 'text-success'
      : 'text-error'
    : negative
    ? 'text-warning'
    : '';

  return (
    <div className={`flex items-center justify-between ${className || ''}`}>
      <span className={bold ? 'font-medium' : ''}>{label}</span>
      <span
        className={`${bold ? 'font-bold' : ''} ${large ? 'text-lg' : ''} ${colorClass}`}
      >
        {formatCurrency(Math.abs(value))}
        {value < 0 && !negative && ' -'}
      </span>
    </div>
  );
}

function OrderMetricCard({
  label,
  value,
  icon,
  variant,
  isText,
}: {
  label: string;
  value: number | string;
  icon: React.ReactNode;
  variant?: 'success' | 'error' | 'warning';
  isText?: boolean;
}) {
  const variantClasses = {
    success: 'text-success',
    error: 'text-error',
    warning: 'text-warning',
  };

  return (
    <Card>
      <CardContent className="p-4 text-center">
        <div className={`mb-2 ${variant ? variantClasses[variant] : 'text-muted-foreground'}`}>
          {icon}
        </div>
        <p className={`text-xl font-bold ${variant ? variantClasses[variant] : ''}`}>
          {isText ? value : typeof value === 'number' ? value.toLocaleString() : value}
        </p>
        <p className="text-xs text-muted-foreground">{label}</p>
      </CardContent>
    </Card>
  );
}
