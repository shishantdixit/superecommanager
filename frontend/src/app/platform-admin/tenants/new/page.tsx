'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ArrowLeft, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import { createTenant, getPlans } from '@/services/platform-admin.service';
import type { Plan } from '@/types/api';

const createTenantSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters'),
  slug: z
    .string()
    .min(3, 'Slug must be at least 3 characters')
    .regex(/^[a-z0-9-]+$/, 'Slug can only contain lowercase letters, numbers, and hyphens'),
  ownerEmail: z.string().email('Invalid email address'),
  ownerFirstName: z.string().min(1, 'First name is required'),
  ownerLastName: z.string().min(1, 'Last name is required'),
  ownerPassword: z.string().min(8, 'Password must be at least 8 characters'),
  planId: z.string().optional(),
  trialDays: z.number().min(0).max(90).optional(),
});

type CreateTenantFormData = z.infer<typeof createTenantSchema>;

export default function CreateTenantPage() {
  const router = useRouter();
  const [plans, setPlans] = useState<Plan[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<CreateTenantFormData>({
    resolver: zodResolver(createTenantSchema),
    defaultValues: {
      trialDays: 14,
    },
  });

  const watchName = watch('name');

  useEffect(() => {
    async function fetchPlans() {
      try {
        const plansData = await getPlans();
        setPlans(plansData);
      } catch (err) {
        console.error('Failed to fetch plans:', err);
      }
    }
    fetchPlans();
  }, []);

  // Auto-generate slug from name
  useEffect(() => {
    if (watchName) {
      const slug = watchName
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .trim();
      setValue('slug', slug);
    }
  }, [watchName, setValue]);

  const onSubmit = async (data: CreateTenantFormData) => {
    setIsSubmitting(true);
    setError(null);
    try {
      await createTenant(data);
      router.push('/platform-admin/tenants');
    } catch (err) {
      const error = err as { message?: string };
      setError(error.message || 'Failed to create tenant');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <PlatformAdminLayout title="Create Tenant">
      <div className="mx-auto max-w-2xl">
        {/* Back Link */}
        <Link
          href="/platform-admin/tenants"
          className="mb-6 inline-flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to Tenants
        </Link>

        {/* Form */}
        <div className="rounded-lg border border-slate-200 bg-white p-6">
          <h2 className="mb-6 text-lg font-semibold">New Tenant</h2>

          {error && (
            <div className="mb-4 rounded-lg bg-red-50 p-3 text-sm text-red-600">{error}</div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            {/* Tenant Info */}
            <div className="space-y-4">
              <h3 className="text-sm font-medium text-slate-900">Tenant Information</h3>

              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">
                    Tenant Name *
                  </label>
                  <input
                    type="text"
                    {...register('name')}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    placeholder="Acme Corp"
                  />
                  {errors.name && (
                    <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>
                  )}
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">
                    Tenant Slug *
                  </label>
                  <input
                    type="text"
                    {...register('slug')}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    placeholder="acme-corp"
                  />
                  {errors.slug && (
                    <p className="mt-1 text-xs text-red-500">{errors.slug.message}</p>
                  )}
                  <p className="mt-1 text-xs text-slate-500">
                    Used in URLs and for identification
                  </p>
                </div>
              </div>
            </div>

            {/* Owner Info */}
            <div className="space-y-4 border-t border-slate-100 pt-6">
              <h3 className="text-sm font-medium text-slate-900">Owner Account</h3>

              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">
                    First Name *
                  </label>
                  <input
                    type="text"
                    {...register('ownerFirstName')}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    placeholder="John"
                  />
                  {errors.ownerFirstName && (
                    <p className="mt-1 text-xs text-red-500">{errors.ownerFirstName.message}</p>
                  )}
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">
                    Last Name *
                  </label>
                  <input
                    type="text"
                    {...register('ownerLastName')}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    placeholder="Doe"
                  />
                  {errors.ownerLastName && (
                    <p className="mt-1 text-xs text-red-500">{errors.ownerLastName.message}</p>
                  )}
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">
                    Email Address *
                  </label>
                  <input
                    type="email"
                    {...register('ownerEmail')}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    placeholder="john@acme.com"
                  />
                  {errors.ownerEmail && (
                    <p className="mt-1 text-xs text-red-500">{errors.ownerEmail.message}</p>
                  )}
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">
                    Password *
                  </label>
                  <input
                    type="password"
                    {...register('ownerPassword')}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                    placeholder="••••••••"
                  />
                  {errors.ownerPassword && (
                    <p className="mt-1 text-xs text-red-500">{errors.ownerPassword.message}</p>
                  )}
                </div>
              </div>
            </div>

            {/* Plan & Trial */}
            <div className="space-y-4 border-t border-slate-100 pt-6">
              <h3 className="text-sm font-medium text-slate-900">Subscription</h3>

              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">Plan</label>
                  <select
                    {...register('planId')}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  >
                    <option value="">Select a plan (optional)</option>
                    {plans.map((plan) => (
                      <option key={plan.id} value={plan.id}>
                        {plan.name} - ₹{plan.monthlyPrice}/mo
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="mb-1 block text-sm font-medium text-slate-700">
                    Trial Days
                  </label>
                  <input
                    type="number"
                    min={0}
                    max={90}
                    {...register('trialDays', { valueAsNumber: true })}
                    className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  />
                  {errors.trialDays && (
                    <p className="mt-1 text-xs text-red-500">{errors.trialDays.message}</p>
                  )}
                </div>
              </div>
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-3 border-t border-slate-100 pt-6">
              <Link
                href="/platform-admin/tenants"
                className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                Cancel
              </Link>
              <button
                type="submit"
                disabled={isSubmitting}
                className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
                Create Tenant
              </button>
            </div>
          </form>
        </div>
      </div>
    </PlatformAdminLayout>
  );
}
