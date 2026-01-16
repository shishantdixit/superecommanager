'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import {
  Building2,
  Plus,
  Search,
  Filter,
  MoreHorizontal,
  Play,
  Pause,
  Trash2,
  Clock,
  ExternalLink,
} from 'lucide-react';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import {
  getTenants,
  suspendTenant,
  reactivateTenant,
  deactivateTenant,
  extendTrial,
} from '@/services/platform-admin.service';
import type { TenantSummary, TenantStatus, TenantFilters } from '@/types/api';
import { cn } from '@/lib/utils';

export default function TenantsPage() {
  const [tenants, setTenants] = useState<TenantSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<TenantFilters>({
    pageNumber: 1,
    pageSize: 10,
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<TenantStatus | ''>('');
  const [actionMenuOpen, setActionMenuOpen] = useState<string | null>(null);
  const [extendTrialModal, setExtendTrialModal] = useState<{
    open: boolean;
    tenantId: string;
    tenantName: string;
  }>({ open: false, tenantId: '', tenantName: '' });
  const [trialDays, setTrialDays] = useState(7);

  useEffect(() => {
    fetchTenants();
  }, [filters]);

  const fetchTenants = async () => {
    try {
      setIsLoading(true);
      const response = await getTenants({
        ...filters,
        search: searchTerm || undefined,
        status: statusFilter || undefined,
      });
      setTenants(response.items || []);
      setTotalCount(response.totalCount || 0);
    } catch (error) {
      console.error('Failed to fetch tenants:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearch = () => {
    setFilters((prev) => ({ ...prev, pageNumber: 1 }));
    fetchTenants();
  };

  const handleSuspend = async (id: string) => {
    try {
      await suspendTenant(id, 'Suspended by admin');
      fetchTenants();
    } catch (error) {
      console.error('Failed to suspend tenant:', error);
    }
    setActionMenuOpen(null);
  };

  const handleReactivate = async (id: string) => {
    try {
      await reactivateTenant(id);
      fetchTenants();
    } catch (error) {
      console.error('Failed to reactivate tenant:', error);
    }
    setActionMenuOpen(null);
  };

  const handleDeactivate = async (id: string) => {
    if (!confirm('Are you sure you want to deactivate this tenant? This action is irreversible.')) {
      return;
    }
    try {
      await deactivateTenant(id, false);
      fetchTenants();
    } catch (error) {
      console.error('Failed to deactivate tenant:', error);
    }
    setActionMenuOpen(null);
  };

  const handleExtendTrial = async () => {
    try {
      await extendTrial(extendTrialModal.tenantId, trialDays);
      fetchTenants();
      setExtendTrialModal({ open: false, tenantId: '', tenantName: '' });
      setTrialDays(7);
    } catch (error) {
      console.error('Failed to extend trial:', error);
    }
  };

  const totalPages = Math.ceil(totalCount / (filters.pageSize || 10));

  return (
    <PlatformAdminLayout title="Tenants">
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-slate-500">
              {totalCount} total tenants
            </p>
          </div>
          <Link
            href="/platform-admin/tenants/new"
            className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
          >
            <Plus className="h-4 w-4" />
            Create Tenant
          </Link>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap items-center gap-3 rounded-lg border border-slate-200 bg-white p-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              placeholder="Search by name, email, or slug..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
          </div>
          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value as TenantStatus | '');
              setFilters((prev) => ({ ...prev, pageNumber: 1 }));
            }}
            className="h-9 rounded-lg border border-slate-200 bg-slate-50 px-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          >
            <option value="">All Statuses</option>
            <option value="Active">Active</option>
            <option value="Suspended">Suspended</option>
            <option value="Deactivated">Deactivated</option>
            <option value="PendingSetup">Pending Setup</option>
          </select>
          <button
            onClick={handleSearch}
            className="flex h-9 items-center gap-2 rounded-lg bg-slate-100 px-3 text-sm font-medium text-slate-700 hover:bg-slate-200"
          >
            <Filter className="h-4 w-4" />
            Apply
          </button>
        </div>

        {/* Table */}
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <table className="min-w-full divide-y divide-slate-200">
            <thead className="bg-slate-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Tenant
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Owner
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Plan
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Status
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Users
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Created
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-slate-500">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center">
                    <div className="flex justify-center">
                      <div className="h-6 w-6 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent" />
                    </div>
                  </td>
                </tr>
              ) : tenants.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-slate-500">
                    No tenants found
                  </td>
                </tr>
              ) : (
                tenants.map((tenant) => (
                  <tr key={tenant.id} className="hover:bg-slate-50">
                    <td className="whitespace-nowrap px-4 py-3">
                      <div className="flex items-center gap-3">
                        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-100">
                          <Building2 className="h-5 w-5 text-indigo-600" />
                        </div>
                        <div>
                          <Link
                            href={`/platform-admin/tenants/${tenant.id}`}
                            className="font-medium text-slate-900 hover:text-indigo-600"
                          >
                            {tenant.name}
                          </Link>
                          <p className="text-xs text-slate-500">{tenant.slug}</p>
                        </div>
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <p className="text-sm text-slate-900">{tenant.ownerName}</p>
                      <p className="text-xs text-slate-500">{tenant.ownerEmail}</p>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <div>
                        <p className="text-sm text-slate-900">{tenant.planName || 'No Plan'}</p>
                        {tenant.isTrialActive && (
                          <p className="text-xs text-amber-600">
                            Trial ends {new Date(tenant.trialEndsAt!).toLocaleDateString()}
                          </p>
                        )}
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <TenantStatusBadge status={tenant.status} />
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-900">
                      {tenant.userCount}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-500">
                      {new Date(tenant.createdAt).toLocaleDateString()}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-right">
                      <div className="relative">
                        <button
                          onClick={() =>
                            setActionMenuOpen(actionMenuOpen === tenant.id ? null : tenant.id)
                          }
                          className="rounded p-1 hover:bg-slate-100"
                        >
                          <MoreHorizontal className="h-5 w-5 text-slate-500" />
                        </button>
                        {actionMenuOpen === tenant.id && (
                          <div className="absolute right-0 z-10 mt-1 w-48 rounded-lg border border-slate-200 bg-white py-1 shadow-lg">
                            <Link
                              href={`/platform-admin/tenants/${tenant.id}`}
                              className="flex items-center gap-2 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                            >
                              <ExternalLink className="h-4 w-4" />
                              View Details
                            </Link>
                            {tenant.status === 'Active' && (
                              <button
                                onClick={() => handleSuspend(tenant.id)}
                                className="flex w-full items-center gap-2 px-4 py-2 text-sm text-amber-600 hover:bg-slate-50"
                              >
                                <Pause className="h-4 w-4" />
                                Suspend
                              </button>
                            )}
                            {tenant.status === 'Suspended' && (
                              <button
                                onClick={() => handleReactivate(tenant.id)}
                                className="flex w-full items-center gap-2 px-4 py-2 text-sm text-emerald-600 hover:bg-slate-50"
                              >
                                <Play className="h-4 w-4" />
                                Reactivate
                              </button>
                            )}
                            {tenant.isTrialActive && (
                              <button
                                onClick={() =>
                                  setExtendTrialModal({
                                    open: true,
                                    tenantId: tenant.id,
                                    tenantName: tenant.name,
                                  })
                                }
                                className="flex w-full items-center gap-2 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                              >
                                <Clock className="h-4 w-4" />
                                Extend Trial
                              </button>
                            )}
                            <hr className="my-1 border-slate-100" />
                            <button
                              onClick={() => handleDeactivate(tenant.id)}
                              className="flex w-full items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-slate-50"
                            >
                              <Trash2 className="h-4 w-4" />
                              Deactivate
                            </button>
                          </div>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

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

      {/* Extend Trial Modal */}
      {extendTrialModal.open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-lg bg-white p-6">
            <h3 className="text-lg font-semibold">Extend Trial</h3>
            <p className="mt-2 text-sm text-slate-500">
              Extend trial period for <strong>{extendTrialModal.tenantName}</strong>
            </p>
            <div className="mt-4">
              <label className="block text-sm font-medium text-slate-700">Days to extend</label>
              <input
                type="number"
                min={1}
                max={90}
                value={trialDays}
                onChange={(e) => setTrialDays(parseInt(e.target.value) || 7)}
                className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
              />
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setExtendTrialModal({ open: false, tenantId: '', tenantName: '' })}
                className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={handleExtendTrial}
                className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
              >
                Extend Trial
              </button>
            </div>
          </div>
        </div>
      )}
    </PlatformAdminLayout>
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
        'inline-block rounded-full px-2.5 py-0.5 text-xs font-medium',
        statusClasses[status] || 'bg-slate-100 text-slate-700'
      )}
    >
      {status}
    </span>
  );
}
