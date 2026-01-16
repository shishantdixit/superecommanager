'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import {
  Building2,
  Users,
  CreditCard,
  TrendingUp,
  TrendingDown,
  ArrowRight,
  Activity,
  AlertTriangle,
} from 'lucide-react';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import { getPlatformStats, getTenants, getActivityLogs } from '@/services/platform-admin.service';
import type { PlatformStats, TenantSummary, TenantActivityLog } from '@/types/api';
import { cn } from '@/lib/utils';

export default function PlatformAdminDashboardPage() {
  const [stats, setStats] = useState<PlatformStats | null>(null);
  const [recentTenants, setRecentTenants] = useState<TenantSummary[]>([]);
  const [recentActivity, setRecentActivity] = useState<TenantActivityLog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        setIsLoading(true);
        const [statsData, tenantsData, activityData] = await Promise.all([
          getPlatformStats().catch(() => null),
          getTenants({ pageSize: 5 }).catch(() => ({ items: [] })),
          getActivityLogs({ pageSize: 5 }).catch(() => ({ items: [] })),
        ]);

        setStats(statsData);
        setRecentTenants(tenantsData.items || []);
        setRecentActivity(activityData.items || []);
      } catch (err) {
        setError('Failed to load dashboard data');
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  // Fallback stats for demo
  const displayStats = stats || {
    totalTenants: 0,
    activeTenants: 0,
    trialingTenants: 0,
    suspendedTenants: 0,
    totalUsers: 0,
    totalOrders: 0,
    monthlyRecurringRevenue: 0,
    averageRevenuePerTenant: 0,
    tenantGrowth: [],
    revenueGrowth: [],
  };

  return (
    <PlatformAdminLayout title="Dashboard">
      {isLoading ? (
        <div className="flex h-64 items-center justify-center">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
        </div>
      ) : error ? (
        <div className="rounded-lg bg-red-50 p-4 text-red-600">{error}</div>
      ) : (
        <div className="space-y-6">
          {/* Stats Grid */}
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              title="Total Tenants"
              value={displayStats.totalTenants}
              icon={<Building2 className="h-6 w-6" />}
              color="indigo"
              change={displayStats.tenantGrowth?.[0]?.newTenants}
              changeLabel="new this month"
            />
            <StatCard
              title="Active Tenants"
              value={displayStats.activeTenants}
              icon={<Users className="h-6 w-6" />}
              color="emerald"
              subtitle={`${displayStats.trialingTenants ?? 0} trialing`}
            />
            <StatCard
              title="Monthly Revenue"
              value={`₹${(displayStats.monthlyRecurringRevenue ?? 0).toLocaleString()}`}
              icon={<CreditCard className="h-6 w-6" />}
              color="amber"
              change={displayStats.revenueGrowth?.[0]?.growth}
              changeLabel="from last month"
              isPercentage
            />
            <StatCard
              title="Avg. Revenue/Tenant"
              value={`₹${(displayStats.averageRevenuePerTenant ?? 0).toLocaleString()}`}
              icon={<TrendingUp className="h-6 w-6" />}
              color="blue"
            />
          </div>

          {/* Secondary Stats */}
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100">
                  <Activity className="h-5 w-5 text-emerald-600" />
                </div>
                <div>
                  <p className="text-sm text-slate-500">Total Orders</p>
                  <p className="text-xl font-semibold">{(displayStats.totalOrders ?? 0).toLocaleString()}</p>
                </div>
              </div>
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-blue-100">
                  <Users className="h-5 w-5 text-blue-600" />
                </div>
                <div>
                  <p className="text-sm text-slate-500">Total Users</p>
                  <p className="text-xl font-semibold">{(displayStats.totalUsers ?? 0).toLocaleString()}</p>
                </div>
              </div>
            </div>
            <div className="rounded-lg border border-slate-200 bg-white p-4">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-full bg-red-100">
                  <AlertTriangle className="h-5 w-5 text-red-600" />
                </div>
                <div>
                  <p className="text-sm text-slate-500">Suspended</p>
                  <p className="text-xl font-semibold">{displayStats.suspendedTenants ?? 0}</p>
                </div>
              </div>
            </div>
          </div>

          {/* Two Column Layout */}
          <div className="grid gap-6 lg:grid-cols-2">
            {/* Recent Tenants */}
            <div className="rounded-lg border border-slate-200 bg-white">
              <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
                <h2 className="font-semibold text-slate-900">Recent Tenants</h2>
                <Link
                  href="/platform-admin/tenants"
                  className="flex items-center gap-1 text-sm text-indigo-600 hover:text-indigo-700"
                >
                  View all
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </div>
              <div className="divide-y divide-slate-100">
                {recentTenants.length > 0 ? (
                  recentTenants.map((tenant) => (
                    <div key={tenant.id} className="flex items-center justify-between px-5 py-3">
                      <div>
                        <p className="font-medium text-slate-900">{tenant.name}</p>
                        <p className="text-sm text-slate-500">{tenant.ownerEmail}</p>
                      </div>
                      <div className="text-right">
                        <TenantStatusBadge status={tenant.status} />
                        <p className="mt-1 text-xs text-slate-400">
                          {new Date(tenant.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="px-5 py-8 text-center text-slate-500">No tenants yet</div>
                )}
              </div>
            </div>

            {/* Recent Activity */}
            <div className="rounded-lg border border-slate-200 bg-white">
              <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
                <h2 className="font-semibold text-slate-900">Recent Activity</h2>
                <Link
                  href="/platform-admin/activity-logs"
                  className="flex items-center gap-1 text-sm text-indigo-600 hover:text-indigo-700"
                >
                  View all
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </div>
              <div className="divide-y divide-slate-100">
                {recentActivity.length > 0 ? (
                  recentActivity.map((activity) => (
                    <div key={activity.id} className="px-5 py-3">
                      <div className="flex items-start justify-between">
                        <div>
                          <p className="text-sm text-slate-900">{activity.description}</p>
                          <p className="mt-0.5 text-xs text-slate-500">
                            {activity.tenantName} • {activity.performedByName || 'System'}
                          </p>
                        </div>
                        <span className="text-xs text-slate-400">
                          {new Date(activity.createdAt).toLocaleTimeString()}
                        </span>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="px-5 py-8 text-center text-slate-500">No recent activity</div>
                )}
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="rounded-lg border border-slate-200 bg-white p-5">
            <h2 className="mb-4 font-semibold text-slate-900">Quick Actions</h2>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <Link
                href="/platform-admin/tenants/new"
                className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 transition-colors hover:border-indigo-300 hover:bg-indigo-50"
              >
                <Building2 className="h-5 w-5 text-indigo-600" />
                <span className="text-sm font-medium">Create Tenant</span>
              </Link>
              <Link
                href="/platform-admin/plans"
                className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 transition-colors hover:border-indigo-300 hover:bg-indigo-50"
              >
                <CreditCard className="h-5 w-5 text-indigo-600" />
                <span className="text-sm font-medium">Manage Plans</span>
              </Link>
              <Link
                href="/platform-admin/admins"
                className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 transition-colors hover:border-indigo-300 hover:bg-indigo-50"
              >
                <Users className="h-5 w-5 text-indigo-600" />
                <span className="text-sm font-medium">Manage Admins</span>
              </Link>
              <Link
                href="/platform-admin/settings"
                className="flex items-center gap-3 rounded-lg border border-slate-200 p-3 transition-colors hover:border-indigo-300 hover:bg-indigo-50"
              >
                <Activity className="h-5 w-5 text-indigo-600" />
                <span className="text-sm font-medium">Platform Settings</span>
              </Link>
            </div>
          </div>
        </div>
      )}
    </PlatformAdminLayout>
  );
}

interface StatCardProps {
  title: string;
  value: string | number;
  icon: React.ReactNode;
  color: 'indigo' | 'emerald' | 'amber' | 'blue' | 'red';
  change?: number;
  changeLabel?: string;
  isPercentage?: boolean;
  subtitle?: string;
}

function StatCard({
  title,
  value,
  icon,
  color,
  change,
  changeLabel,
  isPercentage,
  subtitle,
}: StatCardProps) {
  const colorClasses = {
    indigo: 'bg-indigo-100 text-indigo-600',
    emerald: 'bg-emerald-100 text-emerald-600',
    amber: 'bg-amber-100 text-amber-600',
    blue: 'bg-blue-100 text-blue-600',
    red: 'bg-red-100 text-red-600',
  };

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-5">
      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-slate-500">{title}</p>
          <p className="mt-1 text-2xl font-bold text-slate-900">{value}</p>
          {subtitle && <p className="mt-1 text-xs text-slate-500">{subtitle}</p>}
          {change !== undefined && changeLabel && (
            <p
              className={cn(
                'mt-2 flex items-center gap-1 text-xs',
                change >= 0 ? 'text-emerald-600' : 'text-red-600'
              )}
            >
              {change >= 0 ? (
                <TrendingUp className="h-3 w-3" />
              ) : (
                <TrendingDown className="h-3 w-3" />
              )}
              {isPercentage ? `${Math.abs(change)}%` : Math.abs(change)} {changeLabel}
            </p>
          )}
        </div>
        <div className={cn('rounded-lg p-2', colorClasses[color])}>{icon}</div>
      </div>
    </div>
  );
}

function TenantStatusBadge({ status }: { status: string }) {
  const statusClasses: Record<string, string> = {
    Active: 'bg-emerald-100 text-emerald-700',
    Suspended: 'bg-red-100 text-red-700',
    Deactivated: 'bg-slate-100 text-slate-700',
    PendingSetup: 'bg-amber-100 text-amber-700',
  };

  return (
    <span
      className={cn(
        'inline-block rounded-full px-2 py-0.5 text-xs font-medium',
        statusClasses[status] || 'bg-slate-100 text-slate-700'
      )}
    >
      {status}
    </span>
  );
}
