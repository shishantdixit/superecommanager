'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Select,
  Badge,
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableEmpty,
  Pagination,
  SectionLoader,
  Modal,
} from '@/components/ui';
import { formatDateTime } from '@/lib/utils';
import {
  useUsers,
  useUserStats,
  useRoles,
  usePendingInvitations,
  useInviteUser,
  useActivateUser,
  useDeactivateUser,
  useCancelInvitation,
  useResendInvitation,
} from '@/hooks';
import type { UserFilters, CreateInvitationRequest } from '@/services/team.service';
import {
  Search,
  Filter,
  UserPlus,
  Users,
  UserCheck,
  UserX,
  Eye,
  MoreHorizontal,
  Mail,
  Shield,
  Clock,
  X,
  RefreshCw,
} from 'lucide-react';

const statusOptions = [
  { value: '', label: 'All Status' },
  { value: 'true', label: 'Active' },
  { value: 'false', label: 'Inactive' },
];

const inviteSchema = z.object({
  email: z.string().email('Invalid email address'),
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  roleIds: z.array(z.string()).min(1, 'At least one role is required'),
});

type InviteFormData = z.infer<typeof inviteSchema>;

export default function TeamPage() {
  const [filters, setFilters] = useState<UserFilters>({
    page: 1,
    pageSize: 10,
  });
  const [searchQuery, setSearchQuery] = useState('');
  const [showInviteModal, setShowInviteModal] = useState(false);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
  const [actionType, setActionType] = useState<'activate' | 'deactivate' | null>(null);

  const apiFilters: UserFilters = {
    ...filters,
    searchTerm: searchQuery || undefined,
  };

  const { data, isLoading, error } = useUsers(apiFilters);
  const { data: stats } = useUserStats();
  const { data: roles } = useRoles();
  const { data: invitations } = usePendingInvitations();
  const inviteUserMutation = useInviteUser();
  const activateUserMutation = useActivateUser();
  const deactivateUserMutation = useDeactivateUser();
  const cancelInvitationMutation = useCancelInvitation();
  const resendInvitationMutation = useResendInvitation();

  const users = data?.items || [];
  const totalItems = data?.totalCount || 0;
  const totalPages = data?.totalPages || 1;

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<InviteFormData>({
    resolver: zodResolver(inviteSchema),
    defaultValues: {
      roleIds: [],
    },
  });

  const selectedRoles = watch('roleIds');

  const handleFilterChange = (key: keyof UserFilters, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value === '' ? undefined : value === 'true' ? true : value === 'false' ? false : value,
      page: 1,
    }));
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  };

  const handlePageSizeChange = (size: number) => {
    setFilters((prev) => ({ ...prev, pageSize: size, page: 1 }));
  };

  const onSubmitInvite = async (formData: InviteFormData) => {
    try {
      await inviteUserMutation.mutateAsync(formData as CreateInvitationRequest);
      setShowInviteModal(false);
      reset();
    } catch (err) {
      console.error('Failed to invite user:', err);
    }
  };

  const handleUserAction = async () => {
    if (!selectedUserId || !actionType) return;
    try {
      if (actionType === 'activate') {
        await activateUserMutation.mutateAsync(selectedUserId);
      } else {
        await deactivateUserMutation.mutateAsync(selectedUserId);
      }
      setSelectedUserId(null);
      setActionType(null);
    } catch (err) {
      console.error('Failed to update user:', err);
    }
  };

  const toggleRole = (roleId: string) => {
    const current = selectedRoles || [];
    if (current.includes(roleId)) {
      setValue(
        'roleIds',
        current.filter((id) => id !== roleId)
      );
    } else {
      setValue('roleIds', [...current, roleId]);
    }
  };

  return (
    <DashboardLayout title="Team">
      {/* Stats Cards */}
      {stats && (
        <div className="mb-6 grid gap-4 md:grid-cols-4">
          <StatCard
            label="Total Users"
            value={stats.totalUsers}
            icon={<Users className="h-5 w-5 text-primary" />}
          />
          <StatCard
            label="Active Users"
            value={stats.activeUsers}
            icon={<UserCheck className="h-5 w-5 text-success" />}
          />
          <StatCard
            label="Inactive Users"
            value={stats.inactiveUsers}
            icon={<UserX className="h-5 w-5 text-warning" />}
          />
          <StatCard
            label="Logged In Today"
            value={stats.usersLoggedInToday}
            icon={<Clock className="h-5 w-5 text-info" />}
          />
        </div>
      )}

      {/* Pending Invitations */}
      {invitations && invitations.length > 0 && (
        <Card className="mb-6">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Mail className="h-5 w-5" />
              Pending Invitations ({invitations.length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {invitations.map((invitation) => (
                <div
                  key={invitation.id}
                  className="flex items-center justify-between rounded-lg border p-3"
                >
                  <div>
                    <p className="font-medium">{invitation.email}</p>
                    <p className="text-xs text-muted-foreground">
                      Invited by {invitation.invitedByName} on{' '}
                      {formatDateTime(invitation.invitedAt)}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="flex gap-1">
                      {invitation.invitedRoles.map((role) => (
                        <Badge key={role} variant="default" size="sm">
                          {role}
                        </Badge>
                      ))}
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => resendInvitationMutation.mutate(invitation.id)}
                      disabled={resendInvitationMutation.isPending}
                    >
                      <RefreshCw className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => cancelInvitationMutation.mutate(invitation.id)}
                      disabled={cancelInvitationMutation.isPending}
                    >
                      <X className="h-4 w-4 text-error" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Team Members</CardTitle>
          <div className="flex items-center gap-2">
            <Link href="/team/roles">
              <Button variant="outline" size="sm" leftIcon={<Shield className="h-4 w-4" />}>
                Manage Roles
              </Button>
            </Link>
            <Button
              size="sm"
              leftIcon={<UserPlus className="h-4 w-4" />}
              onClick={() => setShowInviteModal(true)}
            >
              Invite User
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {/* Filters */}
          <div className="mb-6 flex flex-wrap items-center gap-4">
            <div className="flex-1 min-w-[200px]">
              <Input
                placeholder="Search users..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                leftIcon={<Search className="h-4 w-4" />}
              />
            </div>
            <Select
              options={statusOptions}
              value={filters.isActive === undefined ? '' : String(filters.isActive)}
              onChange={(e) => handleFilterChange('isActive', e.target.value)}
              className="w-32"
            />
            <Select
              options={[
                { value: '', label: 'All Roles' },
                ...(roles?.map((r) => ({ value: r.id, label: r.name })) || []),
              ]}
              value={filters.roleId || ''}
              onChange={(e) => handleFilterChange('roleId', e.target.value)}
              className="w-40"
            />
          </div>

          {/* Loading State */}
          {isLoading ? (
            <SectionLoader />
          ) : error ? (
            <div className="py-12 text-center text-error">
              Failed to load users. Please try again.
            </div>
          ) : (
            <>
              {/* Users Table */}
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>User</TableHead>
                    <TableHead>Roles</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Last Login</TableHead>
                    <TableHead>Joined</TableHead>
                    <TableHead className="w-20">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.length === 0 ? (
                    <TableEmpty
                      colSpan={6}
                      message="No users found"
                      icon={<Users className="h-8 w-8" />}
                    />
                  ) : (
                    users.map((user) => (
                      <TableRow key={user.id}>
                        <TableCell>
                          <div className="flex items-center gap-3">
                            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 text-primary font-medium">
                              {user.firstName[0]}
                              {user.lastName[0]}
                            </div>
                            <div>
                              <Link
                                href={`/team/${user.id}`}
                                className="font-medium text-primary hover:underline"
                              >
                                {user.fullName}
                              </Link>
                              <p className="text-xs text-muted-foreground">{user.email}</p>
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-1">
                            {user.roles.map((role) => (
                              <Badge key={role} variant="default" size="sm">
                                {role}
                              </Badge>
                            ))}
                          </div>
                        </TableCell>
                        <TableCell>
                          <Badge
                            variant={user.isActive ? 'success' : 'warning'}
                            size="sm"
                          >
                            {user.isActive ? 'Active' : 'Inactive'}
                          </Badge>
                          {!user.emailVerified && (
                            <Badge variant="default" size="sm" className="ml-1">
                              Unverified
                            </Badge>
                          )}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {user.lastLoginAt ? formatDateTime(user.lastLoginAt) : 'Never'}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {formatDateTime(user.createdAt)}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <Link
                              href={`/team/${user.id}`}
                              className="rounded p-1.5 hover:bg-muted"
                              title="View User"
                            >
                              <Eye className="h-4 w-4 text-muted-foreground" />
                            </Link>
                            <button
                              className="rounded p-1.5 hover:bg-muted"
                              title={user.isActive ? 'Deactivate' : 'Activate'}
                              onClick={() => {
                                setSelectedUserId(user.id);
                                setActionType(user.isActive ? 'deactivate' : 'activate');
                              }}
                            >
                              {user.isActive ? (
                                <UserX className="h-4 w-4 text-warning" />
                              ) : (
                                <UserCheck className="h-4 w-4 text-success" />
                              )}
                            </button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>

              {/* Pagination */}
              {users.length > 0 && (
                <div className="mt-4">
                  <Pagination
                    currentPage={filters.page || 1}
                    totalPages={totalPages}
                    totalItems={totalItems}
                    pageSize={filters.pageSize || 10}
                    onPageChange={handlePageChange}
                    onPageSizeChange={handlePageSizeChange}
                  />
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Invite User Modal */}
      <Modal
        isOpen={showInviteModal}
        onClose={() => {
          setShowInviteModal(false);
          reset();
        }}
        title="Invite Team Member"
      >
        <form onSubmit={handleSubmit(onSubmitInvite)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">
              Email <span className="text-error">*</span>
            </label>
            <Input {...register('email')} type="email" placeholder="user@example.com" />
            {errors.email && (
              <p className="text-sm text-error mt-1">{errors.email.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">
                First Name <span className="text-error">*</span>
              </label>
              <Input {...register('firstName')} placeholder="John" />
              {errors.firstName && (
                <p className="text-sm text-error mt-1">{errors.firstName.message}</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">
                Last Name <span className="text-error">*</span>
              </label>
              <Input {...register('lastName')} placeholder="Doe" />
              {errors.lastName && (
                <p className="text-sm text-error mt-1">{errors.lastName.message}</p>
              )}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">
              Roles <span className="text-error">*</span>
            </label>
            <div className="flex flex-wrap gap-2 p-3 border rounded-md">
              {roles?.map((role) => (
                <button
                  key={role.id}
                  type="button"
                  onClick={() => toggleRole(role.id)}
                  className={`px-3 py-1 rounded-full text-sm border transition-colors ${
                    selectedRoles?.includes(role.id)
                      ? 'bg-primary text-primary-foreground border-primary'
                      : 'border-input hover:bg-muted'
                  }`}
                >
                  {role.name}
                </button>
              ))}
            </div>
            {errors.roleIds && (
              <p className="text-sm text-error mt-1">{errors.roleIds.message}</p>
            )}
          </div>

          <div className="flex justify-end gap-2 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowInviteModal(false);
                reset();
              }}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              isLoading={isSubmitting || inviteUserMutation.isPending}
            >
              Send Invitation
            </Button>
          </div>
        </form>
      </Modal>

      {/* Activate/Deactivate Confirmation Modal */}
      <Modal
        isOpen={!!selectedUserId && !!actionType}
        onClose={() => {
          setSelectedUserId(null);
          setActionType(null);
        }}
        title={actionType === 'activate' ? 'Activate User' : 'Deactivate User'}
      >
        <p className="text-muted-foreground">
          {actionType === 'activate'
            ? 'Are you sure you want to activate this user? They will be able to log in and access the system.'
            : 'Are you sure you want to deactivate this user? They will no longer be able to log in.'}
        </p>
        <div className="flex justify-end gap-2 mt-6">
          <Button
            variant="outline"
            onClick={() => {
              setSelectedUserId(null);
              setActionType(null);
            }}
          >
            Cancel
          </Button>
          <Button
            variant={actionType === 'deactivate' ? 'danger' : 'default'}
            onClick={handleUserAction}
            isLoading={activateUserMutation.isPending || deactivateUserMutation.isPending}
          >
            {actionType === 'activate' ? 'Activate' : 'Deactivate'}
          </Button>
        </div>
      </Modal>
    </DashboardLayout>
  );
}

function StatCard({
  label,
  value,
  icon,
}: {
  label: string;
  value: number;
  icon: React.ReactNode;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-muted-foreground">{label}</p>
            <p className="text-2xl font-bold">{value.toLocaleString()}</p>
          </div>
          {icon}
        </div>
      </CardContent>
    </Card>
  );
}
