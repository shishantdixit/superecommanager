'use client';

import { useEffect, useState } from 'react';
import {
  CreditCard,
  Plus,
  Edit2,
  Trash2,
  Check,
  X,
  Users,
  Package,
  Star,
} from 'lucide-react';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import { getPlans, deletePlan, getFeatures } from '@/services/platform-admin.service';
import type { Plan, Feature } from '@/types/api';
import { cn } from '@/lib/utils';
import { PlanFormModal } from '@/components/platform-admin/plans/plan-form-modal';

export default function PlansPage() {
  const [plans, setPlans] = useState<Plan[]>([]);
  const [features, setFeatures] = useState<Feature[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [editingPlan, setEditingPlan] = useState<Plan | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setIsLoading(true);
      const [plansData, featuresData] = await Promise.all([
        getPlans().catch(() => []),
        getFeatures().catch(() => []),
      ]);
      setPlans(plansData);
      setFeatures(featuresData);
    } catch (err) {
      console.error('Failed to fetch plans:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this plan?')) return;
    try {
      await deletePlan(id);
      fetchData();
    } catch (err) {
      console.error('Failed to delete plan:', err);
    }
  };

  const handleEdit = (plan: Plan) => {
    setEditingPlan(plan);
    setIsModalOpen(true);
  };

  const handleCreate = () => {
    setEditingPlan(null);
    setIsModalOpen(true);
  };

  const handleModalClose = () => {
    setIsModalOpen(false);
    setEditingPlan(null);
  };

  const handleSaveSuccess = () => {
    handleModalClose();
    fetchData();
  };

  return (
    <PlatformAdminLayout title="Plans">
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-500">{plans.length} plans configured</p>
          <button
            onClick={handleCreate}
            className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
          >
            <Plus className="h-4 w-4" />
            Create Plan
          </button>
        </div>

        {/* Plans Grid */}
        {isLoading ? (
          <div className="flex h-64 items-center justify-center">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
          </div>
        ) : plans.length === 0 ? (
          <div className="rounded-lg border border-slate-200 bg-white p-8 text-center">
            <CreditCard className="mx-auto h-12 w-12 text-slate-300" />
            <p className="mt-4 text-slate-500">No plans configured yet</p>
            <button
              onClick={handleCreate}
              className="mt-4 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
            >
              Create First Plan
            </button>
          </div>
        ) : (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {plans
              .sort((a, b) => a.sortOrder - b.sortOrder)
              .map((plan) => (
                <PlanCard
                  key={plan.id}
                  plan={plan}
                  onEdit={() => handleEdit(plan)}
                  onDelete={() => handleDelete(plan.id)}
                />
              ))}
          </div>
        )}

        {/* Feature Comparison Table */}
        {plans.length > 0 && (
          <div className="rounded-lg border border-slate-200 bg-white">
            <div className="border-b border-slate-200 px-5 py-4">
              <h2 className="font-semibold text-slate-900">Feature Comparison</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="min-w-full">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50">
                    <th className="px-4 py-3 text-left text-sm font-medium text-slate-500">
                      Feature
                    </th>
                    {plans.map((plan) => (
                      <th key={plan.id} className="px-4 py-3 text-center text-sm font-medium text-slate-900">
                        {plan.name}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  <tr>
                    <td className="px-4 py-3 text-sm text-slate-700">Max Users</td>
                    {plans.map((plan) => (
                      <td key={plan.id} className="px-4 py-3 text-center text-sm">
                        {plan.maxUsers === -1 ? 'Unlimited' : (plan.maxUsers ?? 0)}
                      </td>
                    ))}
                  </tr>
                  <tr>
                    <td className="px-4 py-3 text-sm text-slate-700">Orders/Month</td>
                    {plans.map((plan) => (
                      <td key={plan.id} className="px-4 py-3 text-center text-sm">
                        {plan.maxOrdersPerMonth === -1 ? 'Unlimited' : (plan.maxOrdersPerMonth ?? 0).toLocaleString()}
                      </td>
                    ))}
                  </tr>
                  {features.map((feature) => (
                    <tr key={feature.id}>
                      <td className="px-4 py-3 text-sm text-slate-700">{feature.name}</td>
                      {plans.map((plan) => {
                        const hasFeature = (plan.features ?? []).some((f) => f.code === feature.code && f.isEnabled);
                        return (
                          <td key={plan.id} className="px-4 py-3 text-center">
                            {hasFeature ? (
                              <Check className="mx-auto h-5 w-5 text-emerald-500" />
                            ) : (
                              <X className="mx-auto h-5 w-5 text-slate-300" />
                            )}
                          </td>
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>

      {/* Plan Form Modal */}
      {isModalOpen && (
        <PlanFormModal
          plan={editingPlan}
          features={features}
          onClose={handleModalClose}
          onSuccess={handleSaveSuccess}
        />
      )}
    </PlatformAdminLayout>
  );
}

interface PlanCardProps {
  plan: Plan;
  onEdit: () => void;
  onDelete: () => void;
}

function PlanCard({ plan, onEdit, onDelete }: PlanCardProps) {
  const enabledFeatures = (plan.features ?? []).filter((f) => f.isEnabled);

  return (
    <div
      className={cn(
        'relative rounded-lg border bg-white p-6',
        plan.isDefault ? 'border-indigo-300 ring-2 ring-indigo-100' : 'border-slate-200'
      )}
    >
      {plan.isDefault && (
        <div className="absolute -top-3 left-1/2 -translate-x-1/2">
          <span className="rounded-full bg-indigo-600 px-3 py-1 text-xs font-medium text-white">
            Popular
          </span>
        </div>
      )}

      {/* Header */}
      <div className="mb-4 flex items-start justify-between">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">{plan.name}</h3>
          <p className="text-sm text-slate-500">{plan.code}</p>
        </div>
        <div className="flex gap-1">
          <button
            onClick={onEdit}
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
          >
            <Edit2 className="h-4 w-4" />
          </button>
          <button
            onClick={onDelete}
            className="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-600"
          >
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      </div>

      {/* Pricing */}
      <div className="mb-4">
        <div className="flex items-baseline gap-1">
          <span className="text-3xl font-bold text-slate-900">₹{plan.monthlyPrice ?? 0}</span>
          <span className="text-slate-500">/month</span>
        </div>
        <p className="text-sm text-slate-500">
          or ₹{plan.yearlyPrice ?? 0}/year (save {Math.round((1 - (plan.yearlyPrice ?? 0) / ((plan.monthlyPrice ?? 1) * 12)) * 100)}%)
        </p>
      </div>

      {/* Limits */}
      <div className="mb-4 space-y-2">
        <div className="flex items-center gap-2 text-sm">
          <Users className="h-4 w-4 text-slate-400" />
          <span>
            {plan.maxUsers === -1 ? 'Unlimited' : (plan.maxUsers ?? 0)} users
          </span>
        </div>
        <div className="flex items-center gap-2 text-sm">
          <Package className="h-4 w-4 text-slate-400" />
          <span>
            {plan.maxOrdersPerMonth === -1 ? 'Unlimited' : (plan.maxOrdersPerMonth ?? 0).toLocaleString()} orders/month
          </span>
        </div>
        {(plan.trialDays ?? 0) > 0 && (
          <div className="flex items-center gap-2 text-sm">
            <Star className="h-4 w-4 text-amber-500" />
            <span>{plan.trialDays} day free trial</span>
          </div>
        )}
      </div>

      {/* Features */}
      <div className="border-t border-slate-100 pt-4">
        <p className="mb-2 text-xs font-medium uppercase tracking-wider text-slate-500">
          Features ({enabledFeatures.length})
        </p>
        <div className="space-y-1">
          {enabledFeatures.slice(0, 5).map((feature) => (
            <div key={feature.code} className="flex items-center gap-2 text-sm">
              <Check className="h-4 w-4 text-emerald-500" />
              <span>{feature.name}</span>
            </div>
          ))}
          {enabledFeatures.length > 5 && (
            <p className="text-xs text-slate-500">
              +{enabledFeatures.length - 5} more features
            </p>
          )}
        </div>
      </div>

      {/* Status */}
      <div className="mt-4 flex items-center justify-between border-t border-slate-100 pt-4">
        <span
          className={cn(
            'rounded-full px-2 py-0.5 text-xs font-medium',
            plan.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-700'
          )}
        >
          {plan.isActive ? 'Active' : 'Inactive'}
        </span>
        <span className="text-xs text-slate-400">Order: {plan.sortOrder}</span>
      </div>
    </div>
  );
}
