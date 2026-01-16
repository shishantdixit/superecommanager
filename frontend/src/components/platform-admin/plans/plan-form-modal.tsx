'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { X, Loader2 } from 'lucide-react';
import { createPlan, updatePlan } from '@/services/platform-admin.service';
import type { Plan, Feature } from '@/types/api';

const planSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters'),
  code: z.string().min(2, 'Code must be at least 2 characters').regex(/^[a-z0-9-]+$/, 'Code can only contain lowercase letters, numbers, and hyphens'),
  description: z.string().optional(),
  monthlyPrice: z.number().min(0, 'Price must be positive'),
  yearlyPrice: z.number().min(0, 'Price must be positive'),
  trialDays: z.number().min(0).max(365),
  maxUsers: z.number().min(-1),
  maxOrdersPerMonth: z.number().min(-1),
  isActive: z.boolean(),
  isDefault: z.boolean(),
  sortOrder: z.number().min(0),
  featureCodes: z.array(z.string()),
});

type PlanFormData = z.infer<typeof planSchema>;

interface PlanFormModalProps {
  plan: Plan | null;
  features: Feature[];
  onClose: () => void;
  onSuccess: () => void;
}

export function PlanFormModal({ plan, features, onClose, onSuccess }: PlanFormModalProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<PlanFormData>({
    resolver: zodResolver(planSchema),
    defaultValues: plan
      ? {
          name: plan.name,
          code: plan.code,
          description: plan.description || '',
          monthlyPrice: plan.monthlyPrice,
          yearlyPrice: plan.yearlyPrice,
          trialDays: plan.trialDays,
          maxUsers: plan.maxUsers,
          maxOrdersPerMonth: plan.maxOrdersPerMonth,
          isActive: plan.isActive,
          isDefault: plan.isDefault,
          sortOrder: plan.sortOrder,
          featureCodes: plan.features.filter((f) => f.isEnabled).map((f) => f.code),
        }
      : {
          name: '',
          code: '',
          description: '',
          monthlyPrice: 0,
          yearlyPrice: 0,
          trialDays: 14,
          maxUsers: 5,
          maxOrdersPerMonth: 1000,
          isActive: true,
          isDefault: false,
          sortOrder: 0,
          featureCodes: [],
        },
  });

  const selectedFeatures = watch('featureCodes') || [];

  const toggleFeature = (code: string) => {
    const current = selectedFeatures;
    if (current.includes(code)) {
      setValue(
        'featureCodes',
        current.filter((c) => c !== code)
      );
    } else {
      setValue('featureCodes', [...current, code]);
    }
  };

  const onSubmit = async (data: PlanFormData) => {
    setIsSubmitting(true);
    setError(null);
    try {
      if (plan) {
        await updatePlan({ id: plan.id, ...data });
      } else {
        await createPlan(data);
      }
      onSuccess();
    } catch (err) {
      const error = err as { message?: string };
      setError(error.message || 'Failed to save plan');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Group features by module
  const featuresByModule = features.reduce(
    (acc, feature) => {
      const featureModule = feature.module || 'Other';
      if (!acc[featureModule]) acc[featureModule] = [];
      acc[featureModule].push(feature);
      return acc;
    },
    {} as Record<string, Feature[]>
  );

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-lg bg-white">
        {/* Header */}
        <div className="sticky top-0 flex items-center justify-between border-b border-slate-200 bg-white px-6 py-4">
          <h2 className="text-lg font-semibold">{plan ? 'Edit Plan' : 'Create Plan'}</h2>
          <button onClick={onClose} className="rounded p-1 hover:bg-slate-100">
            <X className="h-5 w-5 text-slate-500" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="p-6">
          {error && (
            <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</div>
          )}

          <div className="space-y-6">
            {/* Basic Info */}
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700">Name *</label>
                <input
                  type="text"
                  {...register('name')}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  placeholder="Pro Plan"
                />
                {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>}
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700">Code *</label>
                <input
                  type="text"
                  {...register('code')}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  placeholder="pro"
                  disabled={!!plan}
                />
                {errors.code && <p className="mt-1 text-xs text-red-500">{errors.code.message}</p>}
              </div>
            </div>

            <div>
              <label className="mb-1 block text-sm font-medium text-slate-700">Description</label>
              <textarea
                {...register('description')}
                rows={2}
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                placeholder="Best for growing businesses..."
              />
            </div>

            {/* Pricing */}
            <div className="border-t border-slate-100 pt-4">
              <h3 className="mb-3 text-sm font-medium text-slate-900">Pricing</h3>
              <div className="grid gap-4 sm:grid-cols-3">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Monthly (₹)</label>
                  <input
                    type="number"
                    {...register('monthlyPrice', { valueAsNumber: true })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                  {errors.monthlyPrice && (
                    <p className="mt-1 text-xs text-red-500">{errors.monthlyPrice.message}</p>
                  )}
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Yearly (₹)</label>
                  <input
                    type="number"
                    {...register('yearlyPrice', { valueAsNumber: true })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                  {errors.yearlyPrice && (
                    <p className="mt-1 text-xs text-red-500">{errors.yearlyPrice.message}</p>
                  )}
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Trial Days</label>
                  <input
                    type="number"
                    {...register('trialDays', { valueAsNumber: true })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                </div>
              </div>
            </div>

            {/* Limits */}
            <div className="border-t border-slate-100 pt-4">
              <h3 className="mb-3 text-sm font-medium text-slate-900">Limits</h3>
              <p className="mb-3 text-xs text-slate-500">Use -1 for unlimited</p>
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Max Users</label>
                  <input
                    type="number"
                    {...register('maxUsers', { valueAsNumber: true })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Max Orders/Month</label>
                  <input
                    type="number"
                    {...register('maxOrdersPerMonth', { valueAsNumber: true })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                </div>
              </div>
            </div>

            {/* Settings */}
            <div className="border-t border-slate-100 pt-4">
              <h3 className="mb-3 text-sm font-medium text-slate-900">Settings</h3>
              <div className="grid gap-4 sm:grid-cols-3">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Sort Order</label>
                  <input
                    type="number"
                    {...register('sortOrder', { valueAsNumber: true })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    {...register('isActive')}
                    id="isActive"
                    className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
                  />
                  <label htmlFor="isActive" className="text-sm text-slate-700">
                    Active
                  </label>
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    {...register('isDefault')}
                    id="isDefault"
                    className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
                  />
                  <label htmlFor="isDefault" className="text-sm text-slate-700">
                    Default Plan
                  </label>
                </div>
              </div>
            </div>

            {/* Features */}
            <div className="border-t border-slate-100 pt-4">
              <h3 className="mb-3 text-sm font-medium text-slate-900">Features</h3>
              <div className="max-h-64 space-y-4 overflow-y-auto rounded-lg border border-slate-200 p-4">
                {Object.entries(featuresByModule).map(([module, moduleFeatures]) => (
                  <div key={module}>
                    <p className="mb-2 text-xs font-medium uppercase tracking-wider text-slate-500">
                      {module}
                    </p>
                    <div className="grid gap-2 sm:grid-cols-2">
                      {moduleFeatures.map((feature) => (
                        <label
                          key={feature.id}
                          className="flex cursor-pointer items-center gap-2 rounded-lg border border-slate-100 p-2 hover:bg-slate-50"
                        >
                          <input
                            type="checkbox"
                            checked={selectedFeatures.includes(feature.code)}
                            onChange={() => toggleFeature(feature.code)}
                            className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
                          />
                          <span className="text-sm">{feature.name}</span>
                        </label>
                      ))}
                    </div>
                  </div>
                ))}
                {features.length === 0 && (
                  <p className="text-center text-sm text-slate-500">No features available</p>
                )}
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="mt-6 flex justify-end gap-3 border-t border-slate-100 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
              {plan ? 'Update Plan' : 'Create Plan'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
