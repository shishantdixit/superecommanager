'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  LayoutDashboard,
  Building2,
  CreditCard,
  Users,
  Settings,
  Activity,
  Shield,
  LogOut,
} from 'lucide-react';
import { usePlatformAdminStore, useIsSuperAdmin } from '@/stores/platform-admin-store';
import { cn } from '@/lib/utils';

interface NavItem {
  label: string;
  href: string;
  icon: React.ReactNode;
  requiresSuperAdmin?: boolean;
}

const navItems: NavItem[] = [
  {
    label: 'Dashboard',
    href: '/platform-admin/dashboard',
    icon: <LayoutDashboard className="h-5 w-5" />,
  },
  {
    label: 'Tenants',
    href: '/platform-admin/tenants',
    icon: <Building2 className="h-5 w-5" />,
  },
  {
    label: 'Plans',
    href: '/platform-admin/plans',
    icon: <CreditCard className="h-5 w-5" />,
  },
  {
    label: 'Admins',
    href: '/platform-admin/admins',
    icon: <Users className="h-5 w-5" />,
    requiresSuperAdmin: true,
  },
  {
    label: 'Activity Logs',
    href: '/platform-admin/activity-logs',
    icon: <Activity className="h-5 w-5" />,
  },
  {
    label: 'Settings',
    href: '/platform-admin/settings',
    icon: <Settings className="h-5 w-5" />,
  },
];

export function PlatformAdminSidebar() {
  const pathname = usePathname();
  const { admin, logout } = usePlatformAdminStore();
  const isSuperAdmin = useIsSuperAdmin();

  const filteredNavItems = navItems.filter(
    (item) => !item.requiresSuperAdmin || isSuperAdmin
  );

  return (
    <aside className="flex h-screen w-64 flex-col bg-slate-900 text-white">
      {/* Logo */}
      <div className="flex h-16 items-center gap-2 border-b border-slate-700 px-6">
        <Shield className="h-8 w-8 text-indigo-500" />
        <div>
          <span className="text-lg font-bold">SuperEcom</span>
          <span className="ml-1 rounded bg-indigo-600 px-1.5 py-0.5 text-xs font-medium">
            Admin
          </span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4">
        <ul className="space-y-1 px-3">
          {filteredNavItems.map((item) => {
            const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`);
            return (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={cn(
                    'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-indigo-600 text-white'
                      : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                  )}
                >
                  {item.icon}
                  {item.label}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* User section */}
      <div className="border-t border-slate-700 p-4">
        <div className="mb-3 flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-indigo-600 text-sm font-medium">
            {admin?.firstName?.[0]}
            {admin?.lastName?.[0]}
          </div>
          <div className="flex-1 overflow-hidden">
            <p className="truncate text-sm font-medium">
              {admin?.firstName} {admin?.lastName}
            </p>
            <p className="truncate text-xs text-slate-400">{admin?.email}</p>
            {admin?.isSuperAdmin && (
              <span className="mt-1 inline-block rounded bg-amber-600/20 px-1.5 py-0.5 text-xs font-medium text-amber-400">
                Super Admin
              </span>
            )}
          </div>
        </div>
        <button
          onClick={() => logout()}
          className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium text-slate-300 transition-colors hover:bg-slate-800 hover:text-white"
        >
          <LogOut className="h-4 w-4" />
          Logout
        </button>
      </div>
    </aside>
  );
}
