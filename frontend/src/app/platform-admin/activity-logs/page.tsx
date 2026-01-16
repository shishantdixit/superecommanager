'use client';

import { useEffect, useState } from 'react';
import {
  Activity,
  Search,
  Filter,
  Download,
  Calendar,
  User,
  Building2,
} from 'lucide-react';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import { getActivityLogs } from '@/services/platform-admin.service';
import type { TenantActivityLog, ActivityLogFilters } from '@/types/api';
import { cn } from '@/lib/utils';

export default function ActivityLogsPage() {
  const [logs, setLogs] = useState<TenantActivityLog[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<ActivityLogFilters>({
    pageNumber: 1,
    pageSize: 20,
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [actionFilter, setActionFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');

  useEffect(() => {
    fetchLogs();
  }, [filters]);

  const fetchLogs = async () => {
    try {
      setIsLoading(true);
      const response = await getActivityLogs({
        ...filters,
        action: actionFilter || undefined,
        fromDate: dateFrom || undefined,
        toDate: dateTo || undefined,
      });
      setLogs(response.items || []);
      setTotalCount(response.totalCount || 0);
    } catch (error) {
      console.error('Failed to fetch activity logs:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearch = () => {
    setFilters((prev) => ({ ...prev, pageNumber: 1 }));
    fetchLogs();
  };

  const handleExport = () => {
    // TODO: Implement export functionality
    alert('Export functionality coming soon');
  };

  const totalPages = Math.ceil(totalCount / (filters.pageSize || 20));

  const actionTypes = [
    'TenantCreated',
    'TenantSuspended',
    'TenantReactivated',
    'TenantDeactivated',
    'UserCreated',
    'UserUpdated',
    'UserDeleted',
    'PlanChanged',
    'SubscriptionCancelled',
    'TrialExtended',
  ];

  return (
    <PlatformAdminLayout title="Activity Logs">
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-500">{totalCount} total entries</p>
          <button
            onClick={handleExport}
            className="flex items-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
          >
            <Download className="h-4 w-4" />
            Export
          </button>
        </div>

        {/* Filters */}
        <div className="rounded-lg border border-slate-200 bg-white p-4">
          <div className="flex flex-wrap items-end gap-3">
            <div className="flex-1">
              <label className="mb-1 block text-xs font-medium text-slate-500">Search</label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <input
                  type="text"
                  placeholder="Search by tenant, user, or description..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                />
              </div>
            </div>
            <div className="w-40">
              <label className="mb-1 block text-xs font-medium text-slate-500">Action</label>
              <select
                value={actionFilter}
                onChange={(e) => setActionFilter(e.target.value)}
                className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 px-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
              >
                <option value="">All Actions</option>
                {actionTypes.map((action) => (
                  <option key={action} value={action}>
                    {action}
                  </option>
                ))}
              </select>
            </div>
            <div className="w-40">
              <label className="mb-1 block text-xs font-medium text-slate-500">From Date</label>
              <input
                type="date"
                value={dateFrom}
                onChange={(e) => setDateFrom(e.target.value)}
                className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 px-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
            </div>
            <div className="w-40">
              <label className="mb-1 block text-xs font-medium text-slate-500">To Date</label>
              <input
                type="date"
                value={dateTo}
                onChange={(e) => setDateTo(e.target.value)}
                className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 px-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
            </div>
            <button
              onClick={handleSearch}
              className="flex h-9 items-center gap-2 rounded-lg bg-indigo-600 px-4 text-sm font-medium text-white hover:bg-indigo-700"
            >
              <Filter className="h-4 w-4" />
              Apply
            </button>
          </div>
        </div>

        {/* Logs List */}
        <div className="rounded-lg border border-slate-200 bg-white">
          {isLoading ? (
            <div className="flex h-64 items-center justify-center">
              <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
            </div>
          ) : logs.length === 0 ? (
            <div className="flex h-64 flex-col items-center justify-center">
              <Activity className="h-12 w-12 text-slate-300" />
              <p className="mt-4 text-slate-500">No activity logs found</p>
            </div>
          ) : (
            <div className="divide-y divide-slate-100">
              {logs.map((log) => (
                <div key={log.id} className="p-4 hover:bg-slate-50">
                  <div className="flex items-start justify-between">
                    <div className="flex gap-4">
                      <div
                        className={cn(
                          'flex h-10 w-10 items-center justify-center rounded-lg',
                          getActionColor(log.action)
                        )}
                      >
                        {getActionIcon(log.action)}
                      </div>
                      <div>
                        <p className="font-medium text-slate-900">{log.description}</p>
                        <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-slate-500">
                          <span className="flex items-center gap-1">
                            <Building2 className="h-3.5 w-3.5" />
                            {log.tenantName}
                          </span>
                          <span className="flex items-center gap-1">
                            <User className="h-3.5 w-3.5" />
                            {log.performedByName || 'System'}
                          </span>
                          {log.ipAddress && (
                            <span className="text-xs text-slate-400">IP: {log.ipAddress}</span>
                          )}
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                      <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                        {log.action}
                      </span>
                      <p className="mt-1 flex items-center gap-1 text-xs text-slate-400">
                        <Calendar className="h-3 w-3" />
                        {new Date(log.createdAt).toLocaleString()}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3">
              <p className="text-sm text-slate-500">
                Page {filters.pageNumber} of {totalPages}
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() =>
                    setFilters((prev) => ({ ...prev, pageNumber: (prev.pageNumber || 1) - 1 }))
                  }
                  disabled={filters.pageNumber === 1}
                  className="rounded-lg border border-slate-200 px-3 py-1 text-sm disabled:opacity-50"
                >
                  Previous
                </button>
                <button
                  onClick={() =>
                    setFilters((prev) => ({ ...prev, pageNumber: (prev.pageNumber || 1) + 1 }))
                  }
                  disabled={filters.pageNumber === totalPages}
                  className="rounded-lg border border-slate-200 px-3 py-1 text-sm disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </PlatformAdminLayout>
  );
}

function getActionColor(action: string): string {
  if (action.includes('Created')) return 'bg-emerald-100 text-emerald-600';
  if (action.includes('Suspended') || action.includes('Deactivated') || action.includes('Deleted'))
    return 'bg-red-100 text-red-600';
  if (action.includes('Reactivated') || action.includes('Extended'))
    return 'bg-blue-100 text-blue-600';
  if (action.includes('Updated') || action.includes('Changed'))
    return 'bg-amber-100 text-amber-600';
  return 'bg-slate-100 text-slate-600';
}

function getActionIcon(action: string) {
  if (action.includes('Tenant')) return <Building2 className="h-5 w-5" />;
  if (action.includes('User')) return <User className="h-5 w-5" />;
  return <Activity className="h-5 w-5" />;
}
