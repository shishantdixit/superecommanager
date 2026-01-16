'use client';

import { type ReactNode } from 'react';
import { PlatformAdminAuthProvider } from '@/providers/platform-admin-auth-provider';

interface PlatformAdminRootLayoutProps {
  children: ReactNode;
}

export default function PlatformAdminRootLayout({ children }: PlatformAdminRootLayoutProps) {
  return <PlatformAdminAuthProvider>{children}</PlatformAdminAuthProvider>;
}
