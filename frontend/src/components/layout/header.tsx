'use client';

import { useState, useRef, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuthStore } from '@/stores/auth-store';
import { cn } from '@/lib/utils';
import {
  Bell,
  Search,
  User,
  Settings,
  LogOut,
  HelpCircle,
  ChevronDown,
} from 'lucide-react';
import { Avatar } from '@/components/ui';

interface HeaderProps {
  title?: string;
}

export function Header({ title }: HeaderProps) {
  const router = useRouter();
  const { user, logout } = useAuthStore();
  const [showUserMenu, setShowUserMenu] = useState(false);
  const [showNotifications, setShowNotifications] = useState(false);
  const userMenuRef = useRef<HTMLDivElement>(null);
  const notificationRef = useRef<HTMLDivElement>(null);

  // Close menus when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setShowUserMenu(false);
      }
      if (notificationRef.current && !notificationRef.current.contains(event.target as Node)) {
        setShowNotifications(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-border bg-background px-6">
      {/* Left side - Title and breadcrumb */}
      <div className="flex items-center gap-4">
        {title && <h1 className="text-xl font-semibold text-foreground">{title}</h1>}
      </div>

      {/* Right side - Search, notifications, user menu */}
      <div className="flex items-center gap-4">
        {/* Search */}
        <div className="hidden md:block">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search..."
              className="h-9 w-64 rounded-md border border-input bg-background pl-9 pr-3 text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>
        </div>

        {/* Notifications */}
        <div className="relative" ref={notificationRef}>
          <button
            onClick={() => setShowNotifications(!showNotifications)}
            className="relative rounded-md p-2 hover:bg-muted"
          >
            <Bell className="h-5 w-5 text-muted-foreground" />
            {/* Notification badge */}
            <span className="absolute right-1 top-1 h-2 w-2 rounded-full bg-error" />
          </button>

          {/* Notifications dropdown */}
          {showNotifications && (
            <div className="absolute right-0 mt-2 w-80 rounded-lg border border-border bg-card shadow-lg">
              <div className="border-b border-border p-3">
                <h3 className="font-medium">Notifications</h3>
              </div>
              <div className="max-h-80 overflow-y-auto">
                <div className="p-3 text-center text-sm text-muted-foreground">
                  No new notifications
                </div>
              </div>
              <div className="border-t border-border p-2">
                <Link
                  href="/notifications"
                  className="block rounded px-3 py-2 text-center text-sm text-primary hover:bg-muted"
                >
                  View all notifications
                </Link>
              </div>
            </div>
          )}
        </div>

        {/* User menu */}
        <div className="relative" ref={userMenuRef}>
          <button
            onClick={() => setShowUserMenu(!showUserMenu)}
            className="flex items-center gap-2 rounded-md p-1.5 hover:bg-muted"
          >
            <Avatar name={user?.fullName || ''} size="sm" />
            <span className="hidden text-sm font-medium md:block">
              {user?.firstName}
            </span>
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
          </button>

          {/* User dropdown */}
          {showUserMenu && (
            <div className="absolute right-0 mt-2 w-56 rounded-lg border border-border bg-card shadow-lg">
              <div className="border-b border-border p-3">
                <p className="font-medium">{user?.fullName}</p>
                <p className="text-sm text-muted-foreground">{user?.email}</p>
                {user?.tenantName && (
                  <p className="mt-1 text-xs text-muted-foreground">
                    {user.tenantName}
                  </p>
                )}
              </div>
              <div className="py-1">
                <Link
                  href="/profile"
                  className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-muted"
                  onClick={() => setShowUserMenu(false)}
                >
                  <User className="h-4 w-4" />
                  Profile
                </Link>
                <Link
                  href="/settings"
                  className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-muted"
                  onClick={() => setShowUserMenu(false)}
                >
                  <Settings className="h-4 w-4" />
                  Settings
                </Link>
                <Link
                  href="/help"
                  className="flex items-center gap-2 px-3 py-2 text-sm hover:bg-muted"
                  onClick={() => setShowUserMenu(false)}
                >
                  <HelpCircle className="h-4 w-4" />
                  Help & Support
                </Link>
              </div>
              <div className="border-t border-border py-1">
                <button
                  onClick={handleLogout}
                  className="flex w-full items-center gap-2 px-3 py-2 text-sm text-error hover:bg-muted"
                >
                  <LogOut className="h-4 w-4" />
                  Logout
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
