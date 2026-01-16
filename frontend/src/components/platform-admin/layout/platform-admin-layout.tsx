'use client';

import { type ReactNode } from 'react';
import { PlatformAdminSidebar } from './platform-admin-sidebar';
import { PlatformAdminHeader } from './platform-admin-header';

interface PlatformAdminLayoutProps {
  children: ReactNode;
  title?: string;
}

export function PlatformAdminLayout({ children, title }: PlatformAdminLayoutProps) {
  return (
    <div className="flex h-screen bg-slate-100">
      {/* Sidebar */}
      <PlatformAdminSidebar />

      {/* Main content area */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Header */}
        <PlatformAdminHeader title={title} />

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-6">{children}</main>
      </div>
    </div>
  );
}
