'use client';

import { type ReactNode } from 'react';
import { useAuthStore } from '@/stores/auth-store';

interface PermissionGuardProps {
  children: ReactNode;
  permission: string;
  fallback?: ReactNode;
}

/**
 * Guard component that only renders children if user has the required permission.
 */
export function PermissionGuard({
  children,
  permission,
  fallback = null,
}: PermissionGuardProps) {
  const hasPermission = useAuthStore((state) => state.hasPermission(permission));

  if (!hasPermission) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

interface MultiPermissionGuardProps {
  children: ReactNode;
  permissions: string[];
  requireAll?: boolean;
  fallback?: ReactNode;
}

/**
 * Guard component that checks multiple permissions.
 * @param requireAll - If true, requires all permissions. If false, requires any one permission.
 */
export function MultiPermissionGuard({
  children,
  permissions,
  requireAll = false,
  fallback = null,
}: MultiPermissionGuardProps) {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <>{fallback}</>;
  }

  const hasPermissions = requireAll
    ? permissions.every((p) => user.permissions.includes(p))
    : permissions.some((p) => user.permissions.includes(p));

  if (!hasPermissions) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}
