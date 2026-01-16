'use client';

import { type ReactNode } from 'react';
import { useAuthStore } from '@/stores/auth-store';

interface RoleGuardProps {
  children: ReactNode;
  role: string;
  fallback?: ReactNode;
}

/**
 * Guard component that only renders children if user has the required role.
 */
export function RoleGuard({ children, role, fallback = null }: RoleGuardProps) {
  const hasRole = useAuthStore((state) => state.hasRole(role));

  if (!hasRole) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

interface MultiRoleGuardProps {
  children: ReactNode;
  roles: string[];
  requireAll?: boolean;
  fallback?: ReactNode;
}

/**
 * Guard component that checks multiple roles.
 * @param requireAll - If true, requires all roles. If false, requires any one role.
 */
export function MultiRoleGuard({
  children,
  roles,
  requireAll = false,
  fallback = null,
}: MultiRoleGuardProps) {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <>{fallback}</>;
  }

  const hasRoles = requireAll
    ? roles.every((r) => user.roles.includes(r))
    : roles.some((r) => user.roles.includes(r));

  if (!hasRoles) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

/**
 * Guard for platform admin access.
 */
export function PlatformAdminGuard({
  children,
  fallback = null,
}: {
  children: ReactNode;
  fallback?: ReactNode;
}) {
  return (
    <MultiRoleGuard roles={['PlatformAdmin', 'SuperAdmin']} fallback={fallback}>
      {children}
    </MultiRoleGuard>
  );
}

/**
 * Guard for tenant admin access.
 */
export function TenantAdminGuard({
  children,
  fallback = null,
}: {
  children: ReactNode;
  fallback?: ReactNode;
}) {
  return (
    <MultiRoleGuard roles={['TenantAdmin', 'Owner']} fallback={fallback}>
      {children}
    </MultiRoleGuard>
  );
}
