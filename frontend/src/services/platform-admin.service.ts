import { apiClient } from '@/lib/api-client';
import { getPlatformAdminToken } from '@/stores/platform-admin-store';
import type {
  PaginatedResponse,
  TenantSummary,
  TenantDetail,
  TenantFilters,
  CreateTenantRequest,
  Plan,
  CreatePlanRequest,
  UpdatePlanRequest,
  Feature,
  PlatformStats,
  PlatformAdmin,
  PlatformAdminFilters,
  CreatePlatformAdminRequest,
  UpdatePlatformAdminRequest,
  TenantActivityLog,
  ActivityLogFilters,
  PlatformSetting,
  PlatformSettingCategory,
} from '@/types/api';

/**
 * Create axios config with platform admin token.
 */
function getAuthConfig() {
  const token = getPlatformAdminToken();
  return {
    headers: {
      Authorization: `Bearer ${token}`,
    },
  };
}

// ============================================================================
// Tenant Management
// ============================================================================

export async function getTenants(filters?: TenantFilters): Promise<PaginatedResponse<TenantSummary>> {
  // Backend returns response directly (not wrapped in ApiResponse)
  const response = await apiClient.get<PaginatedResponse<TenantSummary>>(
    '/platform-admin/tenants',
    { ...getAuthConfig(), params: filters }
  );
  return response.data;
}

export async function getTenantById(id: string): Promise<TenantDetail> {
  const response = await apiClient.get<TenantDetail>(
    `/platform-admin/tenants/${id}`,
    getAuthConfig()
  );
  return response.data;
}

export async function createTenant(data: CreateTenantRequest): Promise<TenantDetail> {
  const response = await apiClient.post<TenantDetail>(
    '/platform-admin/tenants',
    data,
    getAuthConfig()
  );
  return response.data;
}

export async function suspendTenant(id: string, reason?: string): Promise<void> {
  await apiClient.post(
    `/platform-admin/tenants/${id}/suspend`,
    { reason },
    getAuthConfig()
  );
}

export async function reactivateTenant(id: string): Promise<void> {
  await apiClient.post(
    `/platform-admin/tenants/${id}/reactivate`,
    {},
    getAuthConfig()
  );
}

export async function deactivateTenant(id: string, deleteData: boolean = false): Promise<void> {
  await apiClient.post(
    `/platform-admin/tenants/${id}/deactivate`,
    { deleteData },
    getAuthConfig()
  );
}

export async function extendTrial(id: string, days: number): Promise<void> {
  await apiClient.post(
    `/platform-admin/tenants/${id}/extend-trial`,
    { days },
    getAuthConfig()
  );
}

// ============================================================================
// Plan Management
// ============================================================================

export async function getPlans(): Promise<Plan[]> {
  const response = await apiClient.get<Plan[]>(
    '/platform-admin/plans',
    getAuthConfig()
  );
  return response.data;
}

export async function getPlanById(id: string): Promise<Plan> {
  const response = await apiClient.get<Plan>(
    `/platform-admin/plans/${id}`,
    getAuthConfig()
  );
  return response.data;
}

export async function createPlan(data: CreatePlanRequest): Promise<Plan> {
  const response = await apiClient.post<Plan>(
    '/platform-admin/plans',
    data,
    getAuthConfig()
  );
  return response.data;
}

export async function updatePlan(data: UpdatePlanRequest): Promise<Plan> {
  const response = await apiClient.put<Plan>(
    `/platform-admin/plans/${data.id}`,
    data,
    getAuthConfig()
  );
  return response.data;
}

export async function deletePlan(id: string): Promise<void> {
  await apiClient.delete(`/platform-admin/plans/${id}`, getAuthConfig());
}

// ============================================================================
// Feature Management
// ============================================================================

export async function getFeatures(): Promise<Feature[]> {
  const response = await apiClient.get<Feature[]>(
    '/platform-admin/features',
    getAuthConfig()
  );
  return response.data;
}

// ============================================================================
// Platform Stats
// ============================================================================

export async function getPlatformStats(): Promise<PlatformStats> {
  const response = await apiClient.get<PlatformStats>(
    '/platform-admin/stats',
    getAuthConfig()
  );
  return response.data;
}

// ============================================================================
// Platform Admin Management
// ============================================================================

export async function getPlatformAdmins(filters?: PlatformAdminFilters): Promise<PaginatedResponse<PlatformAdmin>> {
  const response = await apiClient.get<PaginatedResponse<PlatformAdmin>>(
    '/platform-admin/admins',
    { ...getAuthConfig(), params: filters }
  );
  return response.data;
}

export async function getPlatformAdminById(id: string): Promise<PlatformAdmin> {
  const response = await apiClient.get<PlatformAdmin>(
    `/platform-admin/admins/${id}`,
    getAuthConfig()
  );
  return response.data;
}

export async function createPlatformAdmin(data: CreatePlatformAdminRequest): Promise<PlatformAdmin> {
  const response = await apiClient.post<PlatformAdmin>(
    '/platform-admin/admins',
    data,
    getAuthConfig()
  );
  return response.data;
}

export async function updatePlatformAdmin(data: UpdatePlatformAdminRequest): Promise<PlatformAdmin> {
  const response = await apiClient.put<PlatformAdmin>(
    `/platform-admin/admins/${data.id}`,
    data,
    getAuthConfig()
  );
  return response.data;
}

export async function deletePlatformAdmin(id: string): Promise<void> {
  await apiClient.delete(`/platform-admin/admins/${id}`, getAuthConfig());
}

export async function promoteToSuperAdmin(id: string): Promise<void> {
  await apiClient.post(
    `/platform-admin/admins/${id}/promote`,
    {},
    getAuthConfig()
  );
}

export async function demoteFromSuperAdmin(id: string): Promise<void> {
  await apiClient.post(
    `/platform-admin/admins/${id}/demote`,
    {},
    getAuthConfig()
  );
}

export async function activatePlatformAdmin(id: string): Promise<void> {
  await apiClient.post(
    `/platform-admin/admins/${id}/activate`,
    {},
    getAuthConfig()
  );
}

export async function deactivatePlatformAdmin(id: string): Promise<void> {
  await apiClient.post(
    `/platform-admin/admins/${id}/deactivate`,
    {},
    getAuthConfig()
  );
}

// ============================================================================
// Activity Logs
// ============================================================================

export async function getActivityLogs(filters?: ActivityLogFilters): Promise<PaginatedResponse<TenantActivityLog>> {
  const response = await apiClient.get<PaginatedResponse<TenantActivityLog>>(
    '/platform-admin/activity-logs',
    { ...getAuthConfig(), params: filters }
  );
  return response.data;
}

export async function getTenantActivityLogs(
  tenantId: string,
  filters?: ActivityLogFilters
): Promise<PaginatedResponse<TenantActivityLog>> {
  const response = await apiClient.get<PaginatedResponse<TenantActivityLog>>(
    `/platform-admin/tenants/${tenantId}/activity-logs`,
    { ...getAuthConfig(), params: filters }
  );
  return response.data;
}

// ============================================================================
// Platform Settings
// ============================================================================

export async function getPlatformSettings(category?: PlatformSettingCategory): Promise<PlatformSetting[]> {
  const response = await apiClient.get<PlatformSetting[]>(
    '/platform-admin/settings',
    { ...getAuthConfig(), params: { category } }
  );
  return response.data;
}

export async function updatePlatformSetting(key: string, value: string): Promise<PlatformSetting> {
  const response = await apiClient.put<PlatformSetting>(
    '/platform-admin/settings',
    { key, value },
    getAuthConfig()
  );
  return response.data;
}

// ============================================================================
// Subscription Management
// ============================================================================

export async function changeTenantPlan(tenantId: string, planId: string): Promise<void> {
  await apiClient.post(
    `/platform-admin/tenants/${tenantId}/change-plan`,
    { planId },
    getAuthConfig()
  );
}

export async function cancelSubscription(tenantId: string, immediate: boolean = false): Promise<void> {
  await apiClient.post(
    `/platform-admin/tenants/${tenantId}/cancel-subscription`,
    { immediate },
    getAuthConfig()
  );
}
