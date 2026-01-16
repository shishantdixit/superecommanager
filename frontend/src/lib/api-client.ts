import axios, {
  AxiosError,
  AxiosInstance,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';
import type { ApiError, ApiResponse, PaginatedResponse } from '@/types/api';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

/**
 * Token storage keys.
 */
const TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const TENANT_SLUG_KEY = 'tenant_slug';

/**
 * Get stored access token.
 */
export function getAccessToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(TOKEN_KEY);
}

/**
 * Get stored refresh token.
 */
export function getRefreshToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

/**
 * Get stored tenant slug.
 */
export function getTenantSlug(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(TENANT_SLUG_KEY);
}

/**
 * Store tokens and tenant slug.
 */
export function setTokens(accessToken: string, refreshToken: string, tenantSlug?: string): void {
  if (typeof window === 'undefined') return;
  localStorage.setItem(TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  if (tenantSlug) {
    localStorage.setItem(TENANT_SLUG_KEY, tenantSlug);
  }
}

/**
 * Clear tokens and tenant slug.
 */
export function clearTokens(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
  localStorage.removeItem(TENANT_SLUG_KEY);
}

/**
 * Check if user is authenticated.
 */
export function isAuthenticated(): boolean {
  return !!getAccessToken();
}

/**
 * Create axios instance with interceptors.
 */
function createApiClient(): AxiosInstance {
  const client = axios.create({
    baseURL: API_BASE_URL,
    headers: {
      'Content-Type': 'application/json',
    },
    timeout: 30000,
  });

  // Request interceptor - add auth token and tenant slug
  client.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      const token = getAccessToken();
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      // Add tenant slug header for multi-tenant API calls
      const tenantSlug = getTenantSlug();
      if (tenantSlug && config.headers) {
        config.headers['X-Tenant-Slug'] = tenantSlug;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  // Response interceptor - handle errors and token refresh
  client.interceptors.response.use(
    (response) => response,
    async (error: AxiosError<ApiError>) => {
      const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };
      const requestUrl = originalRequest.url || '';

      // Skip tenant token refresh for platform-admin routes
      // Platform admin auth is handled separately
      const isPlatformAdminRequest = requestUrl.includes('platform-admin');

      // Handle 401 - try to refresh token (only for tenant routes)
      if (error.response?.status === 401 && !originalRequest._retry && !isPlatformAdminRequest) {
        originalRequest._retry = true;

        const refreshToken = getRefreshToken();
        if (refreshToken) {
          try {
            const response = await axios.post(`${API_BASE_URL}/auth/refresh`, {
              refreshToken,
            });

            const { accessToken, refreshToken: newRefreshToken } = response.data;
            setTokens(accessToken, newRefreshToken);

            // Retry original request with new token
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${accessToken}`;
            }
            return client(originalRequest);
          } catch {
            // Refresh failed - clear tokens and redirect to login
            clearTokens();
            if (typeof window !== 'undefined') {
              window.location.href = '/login';
            }
          }
        } else {
          // No refresh token - redirect to login
          clearTokens();
          if (typeof window !== 'undefined') {
            window.location.href = '/login';
          }
        }
      }

      // Transform error for consistent handling
      const apiError: ApiError = {
        message: error.response?.data?.message || error.message || 'An error occurred',
        errors: error.response?.data?.errors,
        statusCode: error.response?.status || 500,
      };

      return Promise.reject(apiError);
    }
  );

  return client;
}

// Export singleton instance
export const apiClient = createApiClient();

/**
 * API helper functions.
 */

export async function get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.get<T>(url, config);
  return response.data;
}

export async function post<T, D = unknown>(
  url: string,
  data?: D,
  config?: AxiosRequestConfig
): Promise<T> {
  const response = await apiClient.post<T>(url, data, config);
  return response.data;
}

export async function put<T, D = unknown>(
  url: string,
  data?: D,
  config?: AxiosRequestConfig
): Promise<T> {
  const response = await apiClient.put<T>(url, data, config);
  return response.data;
}

export async function patch<T, D = unknown>(
  url: string,
  data?: D,
  config?: AxiosRequestConfig
): Promise<T> {
  const response = await apiClient.patch<T>(url, data, config);
  return response.data;
}

export async function del<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.delete<T>(url, config);
  return response.data;
}

/**
 * Typed API request helpers.
 */

export async function getOne<T>(url: string): Promise<ApiResponse<T>> {
  return get<ApiResponse<T>>(url);
}

export async function getMany<T>(url: string, params?: object): Promise<PaginatedResponse<T>> {
  return get<PaginatedResponse<T>>(url, { params });
}

export async function create<T, D>(url: string, data: D): Promise<ApiResponse<T>> {
  return post<ApiResponse<T>, D>(url, data);
}

export async function update<T, D>(url: string, data: D): Promise<ApiResponse<T>> {
  return put<ApiResponse<T>, D>(url, data);
}

export async function remove<T>(url: string): Promise<ApiResponse<T>> {
  return del<ApiResponse<T>>(url);
}
