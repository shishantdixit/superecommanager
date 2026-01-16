'use client';

import { useEffect, type ReactNode } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { usePlatformAdminStore } from '@/stores/platform-admin-store';

interface PlatformAdminAuthProviderProps {
  children: ReactNode;
}

// Public routes within platform-admin that don't require authentication
const publicRoutes = ['/platform-admin/login', '/platform-admin/forgot-password'];

export function PlatformAdminAuthProvider({ children }: PlatformAdminAuthProviderProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { isAuthenticated, isLoading, hasHydrated, checkAuth } = usePlatformAdminStore();

  // Check auth once hydration is complete
  useEffect(() => {
    if (hasHydrated) {
      checkAuth();
    }
  }, [hasHydrated, checkAuth]);

  // Handle redirects after hydration and auth check are complete
  useEffect(() => {
    if (!hasHydrated || isLoading) return;

    const isPublicRoute = publicRoutes.some((route) => pathname.startsWith(route));

    if (!isAuthenticated && !isPublicRoute) {
      router.push('/platform-admin/login');
    }

    if (isAuthenticated && pathname === '/platform-admin/login') {
      router.push('/platform-admin/dashboard');
    }
  }, [isAuthenticated, isLoading, hasHydrated, pathname, router]);

  // Show loading state while hydrating or checking auth on protected routes
  const isPublicRoute = publicRoutes.some((route) => pathname.startsWith(route));
  if ((!hasHydrated || isLoading) && !isPublicRoute) {
    return (
      <div className="flex h-screen items-center justify-center bg-slate-900">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
      </div>
    );
  }

  return <>{children}</>;
}

/**
 * HOC for protecting routes that require super admin access.
 */
export function withSuperAdminAccess<P extends object>(
  Component: React.ComponentType<P>
): React.FC<P> {
  return function SuperAdminProtectedComponent(props: P) {
    const router = useRouter();
    const { admin, isAuthenticated, isLoading, hasHydrated } = usePlatformAdminStore();

    useEffect(() => {
      if (!hasHydrated || isLoading) return;

      if (!isAuthenticated) {
        router.push('/platform-admin/login');
        return;
      }

      if (!admin?.isSuperAdmin) {
        router.push('/platform-admin/dashboard');
      }
    }, [admin, isAuthenticated, isLoading, hasHydrated, router]);

    if (!hasHydrated || isLoading) {
      return (
        <div className="flex h-screen items-center justify-center bg-slate-900">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
        </div>
      );
    }

    if (!admin?.isSuperAdmin) {
      return null;
    }

    return <Component {...props} />;
  };
}
