'use client';

import { useEffect, useState } from 'react';
import {
  Users,
  Plus,
  Shield,
  ShieldOff,
  UserCheck,
  UserX,
  Trash2,
  MoreHorizontal,
  Search,
} from 'lucide-react';
import { PlatformAdminLayout } from '@/components/platform-admin/layout';
import {
  getPlatformAdmins,
  createPlatformAdmin,
  deletePlatformAdmin,
  promoteToSuperAdmin,
  demoteFromSuperAdmin,
  activatePlatformAdmin,
  deactivatePlatformAdmin,
} from '@/services/platform-admin.service';
import { usePlatformAdminStore, useIsSuperAdmin } from '@/stores/platform-admin-store';
import type { PlatformAdmin, PlatformAdminFilters } from '@/types/api';
import { cn } from '@/lib/utils';
import { AdminFormModal } from '@/components/platform-admin/admins/admin-form-modal';

export default function AdminsPage() {
  const { admin: currentAdmin } = usePlatformAdminStore();
  const isSuperAdmin = useIsSuperAdmin();

  const [admins, setAdmins] = useState<PlatformAdmin[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState<PlatformAdminFilters>({
    pageNumber: 1,
    pageSize: 10,
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [actionMenuOpen, setActionMenuOpen] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

  useEffect(() => {
    if (isSuperAdmin) {
      fetchAdmins();
    }
  }, [filters, isSuperAdmin]);

  const fetchAdmins = async () => {
    try {
      setIsLoading(true);
      const response = await getPlatformAdmins({
        ...filters,
        search: searchTerm || undefined,
      });
      setAdmins(response.items || []);
      setTotalCount(response.totalCount || 0);
    } catch (error) {
      console.error('Failed to fetch admins:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearch = () => {
    setFilters((prev) => ({ ...prev, pageNumber: 1 }));
    fetchAdmins();
  };

  const handlePromote = async (id: string) => {
    try {
      await promoteToSuperAdmin(id);
      fetchAdmins();
    } catch (error) {
      console.error('Failed to promote admin:', error);
    }
    setActionMenuOpen(null);
  };

  const handleDemote = async (id: string) => {
    try {
      await demoteFromSuperAdmin(id);
      fetchAdmins();
    } catch (error) {
      console.error('Failed to demote admin:', error);
    }
    setActionMenuOpen(null);
  };

  const handleActivate = async (id: string) => {
    try {
      await activatePlatformAdmin(id);
      fetchAdmins();
    } catch (error) {
      console.error('Failed to activate admin:', error);
    }
    setActionMenuOpen(null);
  };

  const handleDeactivate = async (id: string) => {
    try {
      await deactivatePlatformAdmin(id);
      fetchAdmins();
    } catch (error) {
      console.error('Failed to deactivate admin:', error);
    }
    setActionMenuOpen(null);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this admin?')) return;
    try {
      await deletePlatformAdmin(id);
      fetchAdmins();
    } catch (error) {
      console.error('Failed to delete admin:', error);
    }
    setActionMenuOpen(null);
  };

  if (!isSuperAdmin) {
    return (
      <PlatformAdminLayout title="Admin Management">
        <div className="flex h-64 flex-col items-center justify-center rounded-lg border border-slate-200 bg-white">
          <Shield className="h-12 w-12 text-slate-300" />
          <p className="mt-4 text-slate-500">Only Super Admins can access this page</p>
        </div>
      </PlatformAdminLayout>
    );
  }

  const totalPages = Math.ceil(totalCount / (filters.pageSize || 10));

  return (
    <PlatformAdminLayout title="Admin Management">
      <div className="space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-500">{totalCount} platform admins</p>
          <button
            onClick={() => setIsModalOpen(true)}
            className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
          >
            <Plus className="h-4 w-4" />
            Add Admin
          </button>
        </div>

        {/* Search */}
        <div className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white p-4">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              placeholder="Search by name or email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
              className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-3 text-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500"
            />
          </div>
          <button
            onClick={handleSearch}
            className="h-9 rounded-lg bg-slate-100 px-4 text-sm font-medium text-slate-700 hover:bg-slate-200"
          >
            Search
          </button>
        </div>

        {/* Table */}
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
          <table className="min-w-full divide-y divide-slate-200">
            <thead className="bg-slate-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Admin
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Role
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Status
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
                  Last Login
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-slate-500">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {isLoading ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center">
                    <div className="flex justify-center">
                      <div className="h-6 w-6 animate-spin rounded-full border-2 border-indigo-500 border-t-transparent" />
                    </div>
                  </td>
                </tr>
              ) : admins.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-slate-500">
                    No admins found
                  </td>
                </tr>
              ) : (
                admins.map((admin) => (
                  <tr key={admin.id} className="hover:bg-slate-50">
                    <td className="whitespace-nowrap px-4 py-3">
                      <div className="flex items-center gap-3">
                        <div className="flex h-9 w-9 items-center justify-center rounded-full bg-indigo-100 text-sm font-medium text-indigo-600">
                          {admin.firstName[0]}
                          {admin.lastName[0]}
                        </div>
                        <div>
                          <p className="font-medium text-slate-900">
                            {admin.firstName} {admin.lastName}
                            {admin.id === currentAdmin?.id && (
                              <span className="ml-2 text-xs text-slate-400">(You)</span>
                            )}
                          </p>
                          <p className="text-sm text-slate-500">{admin.email}</p>
                        </div>
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      {admin.isSuperAdmin ? (
                        <span className="inline-flex items-center gap-1 rounded-full bg-amber-100 px-2.5 py-0.5 text-xs font-medium text-amber-700">
                          <Shield className="h-3 w-3" />
                          Super Admin
                        </span>
                      ) : (
                        <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700">
                          Admin
                        </span>
                      )}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3">
                      <span
                        className={cn(
                          'rounded-full px-2.5 py-0.5 text-xs font-medium',
                          admin.isActive
                            ? 'bg-emerald-100 text-emerald-700'
                            : 'bg-red-100 text-red-700'
                        )}
                      >
                        {admin.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-500">
                      {admin.lastLoginAt
                        ? new Date(admin.lastLoginAt).toLocaleString()
                        : 'Never'}
                    </td>
                    <td className="whitespace-nowrap px-4 py-3 text-right">
                      {admin.id !== currentAdmin?.id && (
                        <div className="relative">
                          <button
                            onClick={() =>
                              setActionMenuOpen(actionMenuOpen === admin.id ? null : admin.id)
                            }
                            className="rounded p-1 hover:bg-slate-100"
                          >
                            <MoreHorizontal className="h-5 w-5 text-slate-500" />
                          </button>
                          {actionMenuOpen === admin.id && (
                            <div className="absolute right-0 z-10 mt-1 w-48 rounded-lg border border-slate-200 bg-white py-1 shadow-lg">
                              {admin.isSuperAdmin ? (
                                <button
                                  onClick={() => handleDemote(admin.id)}
                                  className="flex w-full items-center gap-2 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                                >
                                  <ShieldOff className="h-4 w-4" />
                                  Remove Super Admin
                                </button>
                              ) : (
                                <button
                                  onClick={() => handlePromote(admin.id)}
                                  className="flex w-full items-center gap-2 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
                                >
                                  <Shield className="h-4 w-4" />
                                  Make Super Admin
                                </button>
                              )}
                              {admin.isActive ? (
                                <button
                                  onClick={() => handleDeactivate(admin.id)}
                                  className="flex w-full items-center gap-2 px-4 py-2 text-sm text-amber-600 hover:bg-slate-50"
                                >
                                  <UserX className="h-4 w-4" />
                                  Deactivate
                                </button>
                              ) : (
                                <button
                                  onClick={() => handleActivate(admin.id)}
                                  className="flex w-full items-center gap-2 px-4 py-2 text-sm text-emerald-600 hover:bg-slate-50"
                                >
                                  <UserCheck className="h-4 w-4" />
                                  Activate
                                </button>
                              )}
                              <hr className="my-1 border-slate-100" />
                              <button
                                onClick={() => handleDelete(admin.id)}
                                className="flex w-full items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-slate-50"
                              >
                                <Trash2 className="h-4 w-4" />
                                Delete
                              </button>
                            </div>
                          )}
                        </div>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3">
              <p className="text-sm text-slate-500">
                Page {filters.pageNumber} of {totalPages}
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() =>
                    setFilters((prev) => ({ ...prev, pageNumber: (prev.pageNumber || 1) - 1 }))
                  }
                  disabled={filters.pageNumber === 1}
                  className="rounded-lg border border-slate-200 px-3 py-1 text-sm disabled:opacity-50"
                >
                  Previous
                </button>
                <button
                  onClick={() =>
                    setFilters((prev) => ({ ...prev, pageNumber: (prev.pageNumber || 1) + 1 }))
                  }
                  disabled={filters.pageNumber === totalPages}
                  className="rounded-lg border border-slate-200 px-3 py-1 text-sm disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Admin Form Modal */}
      {isModalOpen && (
        <AdminFormModal
          onClose={() => setIsModalOpen(false)}
          onSuccess={() => {
            setIsModalOpen(false);
            fetchAdmins();
          }}
        />
      )}
    </PlatformAdminLayout>
  );
}
