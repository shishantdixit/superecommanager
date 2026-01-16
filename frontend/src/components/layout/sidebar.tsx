'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/stores/auth-store';
import {
  LayoutDashboard,
  Package,
  Truck,
  AlertTriangle,
  Boxes,
  Users,
  Settings,
  BarChart3,
  Store,
  Bell,
  ChevronDown,
  ChevronRight,
  Menu,
  X,
} from 'lucide-react';

interface NavItem {
  label: string;
  href: string;
  icon: React.ElementType;
  permission?: string;
  children?: NavItem[];
}

const navItems: NavItem[] = [
  {
    label: 'Dashboard',
    href: '/dashboard',
    icon: LayoutDashboard,
  },
  {
    label: 'Orders',
    href: '/orders',
    icon: Package,
    permission: 'orders.view',
  },
  {
    label: 'Shipments',
    href: '/shipments',
    icon: Truck,
    permission: 'shipments.view',
  },
  {
    label: 'NDR',
    href: '/ndr',
    icon: AlertTriangle,
    permission: 'ndr.view',
  },
  {
    label: 'Inventory',
    href: '/inventory',
    icon: Boxes,
    permission: 'inventory.view',
  },
  {
    label: 'Analytics',
    href: '/analytics',
    icon: BarChart3,
    permission: 'analytics.view',
    children: [
      { label: 'Overview', href: '/analytics', icon: BarChart3 },
      { label: 'Revenue', href: '/analytics/revenue', icon: BarChart3 },
      { label: 'Orders', href: '/analytics/orders', icon: BarChart3 },
      { label: 'Delivery', href: '/analytics/delivery', icon: BarChart3 },
    ],
  },
  {
    label: 'Channels',
    href: '/channels',
    icon: Store,
    permission: 'channels.view',
  },
  {
    label: 'Employees',
    href: '/employees',
    icon: Users,
    permission: 'employees.view',
  },
  {
    label: 'Notifications',
    href: '/notifications',
    icon: Bell,
  },
  {
    label: 'Settings',
    href: '/settings',
    icon: Settings,
  },
];

export function Sidebar() {
  const pathname = usePathname();
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [expandedItems, setExpandedItems] = useState<string[]>([]);
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const user = useAuthStore((state) => state.user);

  const toggleExpand = (label: string) => {
    setExpandedItems((prev) =>
      prev.includes(label) ? prev.filter((item) => item !== label) : [...prev, label]
    );
  };

  const isActive = (href: string) => {
    if (href === '/dashboard') {
      return pathname === '/dashboard';
    }
    return pathname.startsWith(href);
  };

  const filteredNavItems = navItems.filter((item) => {
    if (!item.permission) return true;
    return hasPermission(item.permission);
  });

  return (
    <>
      {/* Mobile overlay */}
      <div
        className={cn(
          'fixed inset-0 z-40 bg-black/50 lg:hidden',
          isCollapsed ? 'hidden' : 'block lg:hidden'
        )}
        onClick={() => setIsCollapsed(true)}
      />

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed left-0 top-0 z-50 flex h-screen flex-col bg-sidebar text-sidebar-foreground transition-all duration-300',
          isCollapsed ? 'w-16' : 'w-64',
          'lg:relative lg:z-0'
        )}
      >
        {/* Logo */}
        <div className="flex h-16 items-center justify-between border-b border-sidebar-muted px-4">
          {!isCollapsed && (
            <Link href="/dashboard" className="text-lg font-bold">
              SuperEcom
            </Link>
          )}
          <button
            onClick={() => setIsCollapsed(!isCollapsed)}
            className="rounded p-1.5 hover:bg-sidebar-muted"
          >
            {isCollapsed ? <Menu className="h-5 w-5" /> : <X className="h-5 w-5" />}
          </button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto py-4">
          <ul className="space-y-1 px-2">
            {filteredNavItems.map((item) => (
              <li key={item.label}>
                {item.children ? (
                  // Expandable item
                  <div>
                    <button
                      onClick={() => toggleExpand(item.label)}
                      className={cn(
                        'flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors',
                        isActive(item.href)
                          ? 'bg-sidebar-accent text-white'
                          : 'hover:bg-sidebar-muted'
                      )}
                    >
                      <item.icon className="h-5 w-5 shrink-0" />
                      {!isCollapsed && (
                        <>
                          <span className="flex-1 text-left">{item.label}</span>
                          {expandedItems.includes(item.label) ? (
                            <ChevronDown className="h-4 w-4" />
                          ) : (
                            <ChevronRight className="h-4 w-4" />
                          )}
                        </>
                      )}
                    </button>
                    {!isCollapsed && expandedItems.includes(item.label) && (
                      <ul className="ml-6 mt-1 space-y-1">
                        {item.children.map((child) => (
                          <li key={child.href}>
                            <Link
                              href={child.href}
                              className={cn(
                                'block rounded-md px-3 py-2 text-sm transition-colors',
                                pathname === child.href
                                  ? 'bg-sidebar-accent text-white'
                                  : 'hover:bg-sidebar-muted'
                              )}
                            >
                              {child.label}
                            </Link>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                ) : (
                  // Simple link
                  <Link
                    href={item.href}
                    className={cn(
                      'flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors',
                      isActive(item.href)
                        ? 'bg-sidebar-accent text-white'
                        : 'hover:bg-sidebar-muted'
                    )}
                    title={isCollapsed ? item.label : undefined}
                  >
                    <item.icon className="h-5 w-5 shrink-0" />
                    {!isCollapsed && <span>{item.label}</span>}
                  </Link>
                )}
              </li>
            ))}
          </ul>
        </nav>

        {/* User info */}
        {user && !isCollapsed && (
          <div className="border-t border-sidebar-muted p-4">
            <div className="flex items-center gap-3">
              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-sidebar-accent text-sm font-medium">
                {user.firstName[0]}
                {user.lastName[0]}
              </div>
              <div className="flex-1 overflow-hidden">
                <p className="truncate text-sm font-medium">{user.fullName}</p>
                <p className="truncate text-xs text-sidebar-foreground/70">
                  {user.tenantName || 'Platform Admin'}
                </p>
              </div>
            </div>
          </div>
        )}
      </aside>
    </>
  );
}
