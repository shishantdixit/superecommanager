import { get, post, put, del } from '@/lib/api-client';
import type { PaginatedResponse } from '@/types/api';

// User types
export interface UserListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  isActive: boolean;
  emailVerified: boolean;
  lastLoginAt?: string;
  roles: string[];
  createdAt: string;
}

export interface UserDetail {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone?: string;
  isActive: boolean;
  emailVerified: boolean;
  lastLoginAt?: string;
  failedLoginAttempts: number;
  lockoutEndsAt?: string;
  isLockedOut: boolean;
  roles: UserRole[];
  permissions: string[];
  createdAt: string;
  updatedAt?: string;
}

export interface UserRole {
  roleId: string;
  roleName: string;
  description?: string;
  isSystem: boolean;
  assignedAt: string;
  assignedBy?: string;
  assignedByName?: string;
}

// Role types
export interface Role {
  id: string;
  name: string;
  description?: string;
  isSystem: boolean;
  userCount: number;
  permissions: Permission[];
  createdAt: string;
  updatedAt?: string;
}

export interface RoleListItem {
  id: string;
  name: string;
  description?: string;
  isSystem: boolean;
  userCount: number;
  permissionCount: number;
  createdAt: string;
}

export interface Permission {
  id: string;
  code: string;
  name: string;
  module: string;
  description?: string;
}

// User invitation
export interface UserInvitation {
  id: string;
  email: string;
  status: string;
  invitedRoles: string[];
  invitedAt: string;
  invitedBy: string;
  invitedByName: string;
  expiresAt?: string;
  acceptedAt?: string;
}

// User stats
export interface UserStats {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  verifiedUsers: number;
  usersLoggedInToday: number;
  usersLoggedInThisWeek: number;
  usersLoggedInThisMonth: number;
  usersByRole: Record<string, number>;
  recentUsers: RecentUser[];
}

export interface RecentUser {
  id: string;
  fullName: string;
  email: string;
  lastLoginAt?: string;
}

// User activity
export interface UserActivity {
  id: string;
  action: string;
  description?: string;
  entityType?: string;
  entityId?: string;
  ipAddress?: string;
  userAgent?: string;
  timestamp: string;
}

// Filters
export interface UserFilters {
  searchTerm?: string;
  isActive?: boolean;
  roleId?: string;
  emailVerified?: boolean;
  lastLoginFrom?: string;
  lastLoginTo?: string;
  page?: number;
  pageSize?: number;
  sortBy?: 'Name' | 'Email' | 'CreatedAt' | 'LastLoginAt' | 'Status';
  sortDescending?: boolean;
}

// Request types
export interface CreateInvitationRequest {
  email: string;
  firstName: string;
  lastName: string;
  roleIds: string[];
}

export interface UpdateUserProfileRequest {
  firstName: string;
  lastName: string;
  phone?: string;
}

export interface AssignRoleRequest {
  roleId: string;
}

export const teamService = {
  // Users
  getUsers: (filters: UserFilters = {}) =>
    get<PaginatedResponse<UserListItem>>('/users', { params: filters }),

  getUserById: (id: string) =>
    get<UserDetail>(`/users/${id}`),

  updateUser: (id: string, data: UpdateUserProfileRequest) =>
    put<UserDetail, UpdateUserProfileRequest>(`/users/${id}`, data),

  activateUser: (id: string) =>
    post<void, {}>(`/users/${id}/activate`, {}),

  deactivateUser: (id: string) =>
    post<void, {}>(`/users/${id}/deactivate`, {}),

  getUserStats: () =>
    get<UserStats>('/users/stats'),

  // User Invitations
  inviteUser: (data: CreateInvitationRequest) =>
    post<UserInvitation, CreateInvitationRequest>('/users/invite', data),

  getPendingInvitations: () =>
    get<UserInvitation[]>('/users/invitations'),

  cancelInvitation: (id: string) =>
    del<void>(`/users/invitations/${id}`),

  resendInvitation: (id: string) =>
    post<void, {}>(`/users/invitations/${id}/resend`, {}),

  // Roles
  getRoles: () =>
    get<RoleListItem[]>('/users/roles'),

  getRoleById: (id: string) =>
    get<Role>(`/users/roles/${id}`),

  assignRole: (userId: string, data: AssignRoleRequest) =>
    post<void, AssignRoleRequest>(`/users/${userId}/roles`, data),

  removeRole: (userId: string, roleId: string) =>
    del<void>(`/users/${userId}/roles/${roleId}`),

  // Permissions
  getPermissions: () =>
    get<Permission[]>('/users/permissions'),

  // User Activity
  getUserActivity: (userId: string, page = 1, pageSize = 20) =>
    get<PaginatedResponse<UserActivity>>(`/users/${userId}/activity`, {
      params: { page, pageSize },
    }),
};
