'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuthStore } from '@/stores/auth-store';
import { Button, Input, Card, CardContent, CardHeader, CardTitle, CardDescription, Alert } from '@/components/ui';
import { Mail, Lock, Eye, EyeOff, Building } from 'lucide-react';

const loginSchema = z.object({
  tenantSlug: z.string().min(1, 'Tenant is required'),
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
});

type LoginFormData = z.infer<typeof loginSchema>;

// Demo credentials for development
const DEMO_CREDENTIALS = {
  tenantSlug: 'demo',
  email: 'admin@demo.com',
  password: 'Admin@123',
};

export default function LoginPage() {
  const router = useRouter();
  const { login, isLoading, error, clearError } = useAuthStore();
  const [showPassword, setShowPassword] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      tenantSlug: DEMO_CREDENTIALS.tenantSlug,
    },
  });

  const fillDemoCredentials = () => {
    setValue('tenantSlug', DEMO_CREDENTIALS.tenantSlug);
    setValue('email', DEMO_CREDENTIALS.email);
    setValue('password', DEMO_CREDENTIALS.password);
  };

  const onSubmit = async (data: LoginFormData) => {
    try {
      await login(data);
      router.push('/dashboard');
    } catch {
      // Error is handled by the store
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted px-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary text-2xl font-bold text-primary-foreground">
            S
          </div>
          <CardTitle className="text-2xl">Welcome back</CardTitle>
          <CardDescription>Sign in to your SuperEcomManager account</CardDescription>
        </CardHeader>
        <CardContent>
          {error && (
            <Alert variant="error" className="mb-4" onClose={clearError}>
              {error}
            </Alert>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <Input
              {...register('tenantSlug')}
              type="text"
              label="Tenant"
              placeholder="Enter tenant slug"
              error={errors.tenantSlug?.message}
              leftIcon={<Building className="h-4 w-4" />}
              autoComplete="organization"
            />

            <Input
              {...register('email')}
              type="email"
              label="Email"
              placeholder="Enter your email"
              error={errors.email?.message}
              leftIcon={<Mail className="h-4 w-4" />}
              autoComplete="email"
            />

            <Input
              {...register('password')}
              type={showPassword ? 'text' : 'password'}
              label="Password"
              placeholder="Enter your password"
              error={errors.password?.message}
              leftIcon={<Lock className="h-4 w-4" />}
              rightIcon={
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="focus:outline-none"
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                </button>
              }
              autoComplete="current-password"
            />

            <div className="flex items-center justify-between text-sm">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-input text-primary focus:ring-primary"
                />
                <span className="text-muted-foreground">Remember me</span>
              </label>
              <a href="/forgot-password" className="text-primary hover:underline">
                Forgot password?
              </a>
            </div>

            <Button type="submit" className="w-full" isLoading={isLoading}>
              Sign In
            </Button>
          </form>

          <div className="mt-6 text-center text-sm text-muted-foreground">
            Don&apos;t have an account?{' '}
            <a href="/signup" className="text-primary hover:underline">
              Contact sales
            </a>
          </div>

          {/* Demo credentials for development */}
          {process.env.NODE_ENV === 'development' && (
            <div className="mt-4 rounded-lg border border-dashed border-border bg-muted/50 p-3">
              <p className="mb-2 text-xs font-medium text-muted-foreground">Development Mode</p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="w-full"
                onClick={fillDemoCredentials}
              >
                Fill Demo Credentials
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
