'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Shield, Eye, EyeOff, Loader2 } from 'lucide-react';
import { usePlatformAdminStore } from '@/stores/platform-admin-store';

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
});

type LoginFormData = z.infer<typeof loginSchema>;

// Demo credentials for development
const DEMO_CREDENTIALS = {
  email: 'superadmin@superecom.com',
  password: 'SuperAdmin@123',
};

export default function PlatformAdminLoginPage() {
  const router = useRouter();
  const { login, isLoading, error, clearError } = usePlatformAdminStore();
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = async (data: LoginFormData) => {
    clearError();
    try {
      await login(data);
      router.push('/platform-admin/dashboard');
    } catch {
      // Error is handled by the store
    }
  };

  const fillDemoCredentials = () => {
    setValue('email', DEMO_CREDENTIALS.email);
    setValue('password', DEMO_CREDENTIALS.password);
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-indigo-900 p-4">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="mb-8 text-center">
          <div className="mb-4 inline-flex items-center justify-center rounded-full bg-indigo-600/20 p-4">
            <Shield className="h-12 w-12 text-indigo-400" />
          </div>
          <h1 className="text-3xl font-bold text-white">Platform Admin</h1>
          <p className="mt-2 text-slate-400">Sign in to manage your platform</p>
        </div>

        {/* Login Form */}
        <div className="rounded-xl bg-white/10 p-8 shadow-xl backdrop-blur-sm">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* Error Message */}
            {error && (
              <div className="rounded-lg bg-red-500/10 p-3 text-sm text-red-400">
                {error}
              </div>
            )}

            {/* Email Field */}
            <div>
              <label htmlFor="email" className="mb-1.5 block text-sm font-medium text-slate-300">
                Email Address
              </label>
              <input
                id="email"
                type="email"
                autoComplete="email"
                {...register('email')}
                className="w-full rounded-lg border border-slate-600 bg-slate-700/50 px-4 py-2.5 text-white placeholder:text-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                placeholder="admin@example.com"
              />
              {errors.email && (
                <p className="mt-1 text-sm text-red-400">{errors.email.message}</p>
              )}
            </div>

            {/* Password Field */}
            <div>
              <label htmlFor="password" className="mb-1.5 block text-sm font-medium text-slate-300">
                Password
              </label>
              <div className="relative">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  {...register('password')}
                  className="w-full rounded-lg border border-slate-600 bg-slate-700/50 px-4 py-2.5 pr-10 text-white placeholder:text-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                  placeholder="Enter your password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-300"
                >
                  {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-400">{errors.password.message}</p>
              )}
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading}
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-indigo-600 px-4 py-2.5 font-medium text-white transition-colors hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {isLoading ? (
                <>
                  <Loader2 className="h-5 w-5 animate-spin" />
                  Signing in...
                </>
              ) : (
                'Sign In'
              )}
            </button>

            {/* Demo Credentials Button */}
            {process.env.NODE_ENV === 'development' && (
              <button
                type="button"
                onClick={fillDemoCredentials}
                className="w-full rounded-lg border border-slate-600 px-4 py-2 text-sm text-slate-300 transition-colors hover:bg-slate-700/50"
              >
                Fill Demo Credentials
              </button>
            )}
          </form>
        </div>

        {/* Footer */}
        <p className="mt-6 text-center text-sm text-slate-500">
          Protected area. Authorized personnel only.
        </p>
      </div>
    </div>
  );
}
