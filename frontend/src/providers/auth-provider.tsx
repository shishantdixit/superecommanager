'use client';

import { useEffect, type ReactNode } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useAuthStore } from '@/stores/auth-store';

interface AuthProviderProps {
  children: ReactNode;
}

// Public routes that don't require authentication
const publicRoutes = ['/login', '/forgot-password', '/reset-password'];

// Routes handled by separate auth providers
const excludedRoutes = ['/platform-admin'];

export function AuthProvider({ children }: AuthProviderProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { isAuthenticated, isLoading, hasHydrated, checkAuth } = useAuthStore();

  // Check if this route is handled by a different auth provider
  const isExcludedRoute = excludedRoutes.some((route) => pathname.startsWith(route));

  // Check auth once hydration is complete (skip for excluded routes)
  useEffect(() => {
    if (hasHydrated && !isExcludedRoute) {
      checkAuth();
    }
  }, [hasHydrated, checkAuth, isExcludedRoute]);

  // Handle redirects after hydration and auth check are complete
  useEffect(() => {
    // Skip for excluded routes (they have their own auth provider)
    if (isExcludedRoute) return;

    // Wait for hydration and auth check to complete
    if (!hasHydrated || isLoading) return;

    const isPublicRoute = publicRoutes.some((route) => pathname.startsWith(route));

    if (!isAuthenticated && !isPublicRoute) {
      router.push('/login');
    }

    if (isAuthenticated && pathname === '/login') {
      router.push('/dashboard');
    }
  }, [isAuthenticated, isLoading, hasHydrated, pathname, router, isExcludedRoute]);

  // Skip auth handling for excluded routes
  if (isExcludedRoute) {
    return <>{children}</>;
  }

  // Show loading state while hydrating or checking auth on protected routes
  const isPublicRoute = publicRoutes.some((route) => pathname.startsWith(route));
  if ((!hasHydrated || isLoading) && !isPublicRoute) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  return <>{children}</>;
}
