'use client';

import { Bell, Search } from 'lucide-react';
import { usePlatformAdminStore } from '@/stores/platform-admin-store';

interface PlatformAdminHeaderProps {
  title?: string;
}

export function PlatformAdminHeader({ title }: PlatformAdminHeaderProps) {
  const { admin } = usePlatformAdminStore();

  return (
    <header className="flex h-16 items-center justify-between border-b border-slate-200 bg-white px-6">
      <div className="flex items-center gap-4">
        {title && <h1 className="text-xl font-semibold text-slate-900">{title}</h1>}
      </div>

      <div className="flex items-center gap-4">
        {/* Search */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            placeholder="Search tenants..."
            className="h-9 w-64 rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-3 text-sm placeholder:text-slate-400 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
          />
        </div>

        {/* Notifications */}
        <button className="relative rounded-lg p-2 text-slate-500 hover:bg-slate-100">
          <Bell className="h-5 w-5" />
          <span className="absolute right-1 top-1 h-2 w-2 rounded-full bg-red-500" />
        </button>

        {/* User avatar */}
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-indigo-600 text-sm font-medium text-white">
            {admin?.firstName?.[0]}
            {admin?.lastName?.[0]}
          </div>
        </div>
      </div>
    </header>
  );
}
