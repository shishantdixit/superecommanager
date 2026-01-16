'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowLeft,
  Building2,
  Users,
  Package,
  CreditCard,
  Calendar,
  Mail,
  Globe,
  Phone,
  Play,
  Pause,
  Clock,
  Trash2,
} from 'lucide-react';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import {
  getTenantById,
  suspendTenant,
  reactivateTenant,
  extendTrial,
  getTenantActivityLogs,
} from '@/services/platform-admin.service';
import type { TenantDetail, TenantActivityLog } from '@/types/api';
import { cn } from '@/lib/utils';

export default function TenantDetailPage() {
  const params = useParams();
  const tenantId = params.id as string;

  const [tenant, setTenant] = useState<TenantDetail | null>(null);
  const [activityLogs, setActivityLogs] = useState<TenantActivityLog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [extendTrialModal, setExtendTrialModal] = useState(false);
  const [trialDays, setTrialDays] = useState(7);

  useEffect(() => {
    fetchTenantData();
  }, [tenantId]);

  const fetchTenantData = async () => {
    try {
      setIsLoading(true);
      const [tenantData, logsData] = await Promise.all([
        getTenantById(tenantId),
        getTenantActivityLogs(tenantId, { pageSize: 10 }).catch(() => ({ items: [] })),
      ]);
      setTenant(tenantData);
      setActivityLogs(logsData.items || []);
    } catch (err) {
      setError('Failed to load tenant details');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSuspend = async () => {
    if (!tenant) return;
    try {
      await suspendTenant(tenant.id, 'Suspended by admin');
      fetchTenantData();
    } catch (err) {
      console.error('Failed to suspend tenant:', err);
    }
  };

  const handleReactivate = async () => {
    if (!tenant) return;
    try {
      await reactivateTenant(tenant.id);
      fetchTenantData();
    } catch (err) {
      console.error('Failed to reactivate tenant:', err);
    }
  };

  const handleExtendTrial = async () => {
    if (!tenant) return;
    try {
      await extendTrial(tenant.id, trialDays);
      setExtendTrialModal(false);
      setTrialDays(7);
      fetchTenantData();
    } catch (err) {
      console.error('Failed to extend trial:', err);
    }
  };

  if (isLoading) {
    return (
      <PlatformAdminLayout title="Tenant Details">
        <div className="flex h-64 items-center justify-center">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
        </div>
      </PlatformAdminLayout>
    );
  }

  if (error || !tenant) {
    return (
      <PlatformAdminLayout title="Tenant Details">
        <div className="rounded-lg bg-red-50 p-4 text-red-600">{error || 'Tenant not found'}</div>
      </PlatformAdminLayout>
    );
  }

  return (
    <PlatformAdminLayout title="Tenant Details">
      <div className="space-y-6">
        {/* Back Link */}
        <Link
          href="/platform-admin/tenants"
          className="inline-flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Tenants
        </Link>

        {/* Header */}
        <div className="flex items-start justify-between rounded-lg border border-slate-200 bg-white p-6">
          <div className="flex items-center gap-4">
            <div className="flex h-16 w-16 items-center justify-center rounded-xl bg-indigo-100">
              <Building2 className="h-8 w-8 text-indigo-600" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-slate-900">{tenant.name}</h1>
              <p className="text-slate-500">{tenant.slug}</p>
              <div className="mt-2 flex items-center gap-2">
                <TenantStatusBadge status={tenant.status} />
                {tenant.isTrialActive && (
                  <span className="rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-700">
                    Trial until {new Date(tenant.trialEndsAt!).toLocaleDateString()}
                  </span>
                )}
              </div>
            </div>
          </div>
          <div className="flex gap-2">
            {tenant.status === 'Active' && (
              <button
                onClick={handleSuspend}
                className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-2 text-sm font-medium text-amber-700 hover:bg-amber-100"
              >
                <Pause className="h-4 w-4" />
                Suspend
              </button>
            )}
            {tenant.status === 'Suspended' && (
              <button
                onClick={handleReactivate}
                className="flex items-center gap-2 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-700 hover:bg-emerald-100"
              >
                <Play className="h-4 w-4" />
                Reactivate
              </button>
            )}
            {tenant.isTrialActive && (
              <button
                onClick={() => setExtendTrialModal(true)}
                className="flex items-center gap-2 rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                <Clock className="h-4 w-4" />
                Extend Trial
              </button>
            )}
          </div>
        </div>

        {/* Content Grid */}
        <div className="grid gap-6 lg:grid-cols-3">
          {/* Main Content */}
          <div className="space-y-6 lg:col-span-2">
            {/* Stats */}
            <div className="grid gap-4 sm:grid-cols-3">
              <div className="rounded-lg border border-slate-200 bg-white p-4">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-indigo-100">
                    <Users className="h-5 w-5 text-indigo-600" />
                  </div>
                  <div>
                    <p className="text-sm text-slate-500">Users</p>
                    <p className="text-xl font-semibold">{tenant.userCount}</p>
                  </div>
                </div>
              </div>
              <div className="rounded-lg border border-slate-200 bg-white p-4">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-emerald-100">
                    <Package className="h-5 w-5 text-emerald-600" />
                  </div>
                  <div>
                    <p className="text-sm text-slate-500">Orders</p>
                    <p className="text-xl font-semibold">{tenant.orderCount}</p>
                  </div>
                </div>
              </div>
              <div className="rounded-lg border border-slate-200 bg-white p-4">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-100">
                    <CreditCard className="h-5 w-5 text-amber-600" />
                  </div>
                  <div>
                    <p className="text-sm text-slate-500">Plan</p>
                    <p className="text-xl font-semibold">{tenant.planName || 'Free'}</p>
                  </div>
                </div>
              </div>
            </div>

            {/* Activity Logs */}
            <div className="rounded-lg border border-slate-200 bg-white">
              <div className="border-b border-slate-200 px-5 py-4">
                <h2 className="font-semibold text-slate-900">Recent Activity</h2>
              </div>
              <div className="divide-y divide-slate-100">
                {activityLogs.length > 0 ? (
                  activityLogs.map((log) => (
                    <div key={log.id} className="px-5 py-3">
                      <div className="flex items-start justify-between">
                        <div>
                          <p className="text-sm text-slate-900">{log.description}</p>
                          <p className="mt-0.5 text-xs text-slate-500">
                            {log.performedByName || 'System'} • {log.action}
                          </p>
                        </div>
                        <span className="text-xs text-slate-400">
                          {new Date(log.createdAt).toLocaleString()}
                        </span>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="px-5 py-8 text-center text-slate-500">No activity yet</div>
                )}
              </div>
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Owner Info */}
            <div className="rounded-lg border border-slate-200 bg-white p-5">
              <h3 className="mb-4 font-semibold text-slate-900">Owner</h3>
              <div className="space-y-3">
                <div className="flex items-center gap-3">
                  <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-sm font-medium">
                    {tenant.ownerName
                      .split(' ')
                      .map((n) => n[0])
                      .join('')}
                  </div>
                  <div>
                    <p className="font-medium text-slate-900">{tenant.ownerName}</p>
                    <p className="text-sm text-slate-500">{tenant.ownerEmail}</p>
                  </div>
                </div>
              </div>
            </div>

            {/* Contact Info */}
            <div className="rounded-lg border border-slate-200 bg-white p-5">
              <h3 className="mb-4 font-semibold text-slate-900">Contact Information</h3>
              <div className="space-y-3">
                <div className="flex items-center gap-3 text-sm">
                  <Mail className="h-4 w-4 text-slate-400" />
                  <span>{tenant.ownerEmail}</span>
                </div>
                {tenant.phone && (
                  <div className="flex items-center gap-3 text-sm">
                    <Phone className="h-4 w-4 text-slate-400" />
                    <span>{tenant.phone}</span>
                  </div>
                )}
                {tenant.website && (
                  <div className="flex items-center gap-3 text-sm">
                    <Globe className="h-4 w-4 text-slate-400" />
                    <a href={tenant.website} target="_blank" rel="noopener noreferrer" className="text-indigo-600 hover:underline">
                      {tenant.website}
                    </a>
                  </div>
                )}
                <div className="flex items-center gap-3 text-sm">
                  <Calendar className="h-4 w-4 text-slate-400" />
                  <span>Created {new Date(tenant.createdAt).toLocaleDateString()}</span>
                </div>
              </div>
            </div>

            {/* Subscription */}
            {tenant.subscription && (
              <div className="rounded-lg border border-slate-200 bg-white p-5">
                <h3 className="mb-4 font-semibold text-slate-900">Subscription</h3>
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-slate-500">Plan</span>
                    <span className="font-medium">{tenant.subscription.planName}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Status</span>
                    <span className="font-medium">{tenant.subscription.status}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Billing</span>
                    <span className="font-medium">{tenant.subscription.billingCycle}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-500">Amount</span>
                    <span className="font-medium">₹{tenant.subscription.amount}/mo</span>
                  </div>
                </div>
              </div>
            )}

            {/* Features */}
            {tenant.features && tenant.features.length > 0 && (
              <div className="rounded-lg border border-slate-200 bg-white p-5">
                <h3 className="mb-4 font-semibold text-slate-900">Enabled Features</h3>
                <div className="flex flex-wrap gap-2">
                  {tenant.features.map((feature) => (
                    <span
                      key={feature}
                      className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-700"
                    >
                      {feature}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Danger Zone */}
            <div className="rounded-lg border border-red-200 bg-red-50 p-5">
              <h3 className="mb-2 font-semibold text-red-700">Danger Zone</h3>
              <p className="mb-4 text-sm text-red-600">
                Deactivating a tenant will disable their access permanently.
              </p>
              <button className="flex items-center gap-2 rounded-lg border border-red-300 bg-white px-4 py-2 text-sm font-medium text-red-700 hover:bg-red-50">
                <Trash2 className="h-4 w-4" />
                Deactivate Tenant
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Extend Trial Modal */}
      {extendTrialModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-lg bg-white p-6">
            <h3 className="text-lg font-semibold">Extend Trial</h3>
            <p className="mt-2 text-sm text-slate-500">
              Extend trial period for <strong>{tenant.name}</strong>
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
                onClick={() => setExtendTrialModal(false)}
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
