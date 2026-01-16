import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  teamService,
  UserFilters,
  CreateInvitationRequest,
  UpdateUserProfileRequest,
  AssignRoleRequest,
} from '@/services/team.service';

export const teamKeys = {
  all: ['team'] as const,
  users: () => [...teamKeys.all, 'users'] as const,
  userList: (filters: UserFilters) => [...teamKeys.users(), 'list', filters] as const,
  userDetail: (id: string) => [...teamKeys.users(), 'detail', id] as const,
  userStats: () => [...teamKeys.users(), 'stats'] as const,
  invitations: () => [...teamKeys.all, 'invitations'] as const,
  roles: () => [...teamKeys.all, 'roles'] as const,
  roleDetail: (id: string) => [...teamKeys.roles(), 'detail', id] as const,
  permissions: () => [...teamKeys.all, 'permissions'] as const,
  userActivity: (userId: string, page: number, pageSize: number) =>
    [...teamKeys.users(), userId, 'activity', { page, pageSize }] as const,
};

/**
 * Hook to fetch paginated users.
 */
export function useUsers(filters: UserFilters = {}) {
  return useQuery({
    queryKey: teamKeys.userList(filters),
    queryFn: () => teamService.getUsers(filters),
  });
}

/**
 * Hook to fetch a single user by ID.
 */
export function useUser(id: string) {
  return useQuery({
    queryKey: teamKeys.userDetail(id),
    queryFn: () => teamService.getUserById(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch user statistics.
 */
export function useUserStats() {
  return useQuery({
    queryKey: teamKeys.userStats(),
    queryFn: () => teamService.getUserStats(),
  });
}

/**
 * Hook to fetch pending invitations.
 */
export function usePendingInvitations() {
  return useQuery({
    queryKey: teamKeys.invitations(),
    queryFn: () => teamService.getPendingInvitations(),
  });
}

/**
 * Hook to fetch all roles.
 */
export function useRoles() {
  return useQuery({
    queryKey: teamKeys.roles(),
    queryFn: () => teamService.getRoles(),
  });
}

/**
 * Hook to fetch a single role by ID.
 */
export function useRole(id: string) {
  return useQuery({
    queryKey: teamKeys.roleDetail(id),
    queryFn: () => teamService.getRoleById(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch all permissions.
 */
export function usePermissions() {
  return useQuery({
    queryKey: teamKeys.permissions(),
    queryFn: () => teamService.getPermissions(),
  });
}

/**
 * Hook to fetch user activity.
 */
export function useUserActivity(userId: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: teamKeys.userActivity(userId, page, pageSize),
    queryFn: () => teamService.getUserActivity(userId, page, pageSize),
    enabled: !!userId,
  });
}

/**
 * Hook to invite a user.
 */
export function useInviteUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateInvitationRequest) => teamService.inviteUser(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: teamKeys.invitations() });
      queryClient.invalidateQueries({ queryKey: teamKeys.userStats() });
    },
  });
}

/**
 * Hook to update a user.
 */
export function useUpdateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserProfileRequest }) =>
      teamService.updateUser(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: teamKeys.userDetail(id) });
      queryClient.invalidateQueries({ queryKey: teamKeys.users() });
    },
  });
}

/**
 * Hook to activate a user.
 */
export function useActivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => teamService.activateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: teamKeys.users() });
      queryClient.invalidateQueries({ queryKey: teamKeys.userStats() });
    },
  });
}

/**
 * Hook to deactivate a user.
 */
export function useDeactivateUser() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => teamService.deactivateUser(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: teamKeys.users() });
      queryClient.invalidateQueries({ queryKey: teamKeys.userStats() });
    },
  });
}

/**
 * Hook to assign a role to a user.
 */
export function useAssignRole() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, data }: { userId: string; data: AssignRoleRequest }) =>
      teamService.assignRole(userId, data),
    onSuccess: (_, { userId }) => {
      queryClient.invalidateQueries({ queryKey: teamKeys.userDetail(userId) });
      queryClient.invalidateQueries({ queryKey: teamKeys.users() });
      queryClient.invalidateQueries({ queryKey: teamKeys.roles() });
    },
  });
}

/**
 * Hook to remove a role from a user.
 */
export function useRemoveRole() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, roleId }: { userId: string; roleId: string }) =>
      teamService.removeRole(userId, roleId),
    onSuccess: (_, { userId }) => {
      queryClient.invalidateQueries({ queryKey: teamKeys.userDetail(userId) });
      queryClient.invalidateQueries({ queryKey: teamKeys.users() });
      queryClient.invalidateQueries({ queryKey: teamKeys.roles() });
    },
  });
}

/**
 * Hook to cancel an invitation.
 */
export function useCancelInvitation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => teamService.cancelInvitation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: teamKeys.invitations() });
    },
  });
}

/**
 * Hook to resend an invitation.
 */
export function useResendInvitation() {
  return useMutation({
    mutationFn: (id: string) => teamService.resendInvitation(id),
  });
}
