import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type {
  PlatformAdmin,
  PlatformAdminLoginRequest,
  PlatformAdminLoginResponse,
} from '@/types/api';
import { post, get as apiGet } from '@/lib/api-client';

const PLATFORM_ADMIN_TOKEN_KEY = 'platform_admin_token';
const PLATFORM_ADMIN_REFRESH_TOKEN_KEY = 'platform_admin_refresh_token';

/**
 * Get stored platform admin access token.
 */
export function getPlatformAdminToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(PLATFORM_ADMIN_TOKEN_KEY);
}

/**
 * Get stored platform admin refresh token.
 */
export function getPlatformAdminRefreshToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(PLATFORM_ADMIN_REFRESH_TOKEN_KEY);
}

/**
 * Store platform admin tokens.
 */
export function setPlatformAdminTokens(accessToken: string, refreshToken: string): void {
  if (typeof window === 'undefined') return;
  localStorage.setItem(PLATFORM_ADMIN_TOKEN_KEY, accessToken);
  localStorage.setItem(PLATFORM_ADMIN_REFRESH_TOKEN_KEY, refreshToken);
}

/**
 * Clear platform admin tokens.
 */
export function clearPlatformAdminTokens(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(PLATFORM_ADMIN_TOKEN_KEY);
  localStorage.removeItem(PLATFORM_ADMIN_REFRESH_TOKEN_KEY);
}

interface PlatformAdminState {
  admin: PlatformAdmin | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  hasHydrated: boolean;
  error: string | null;

  // Actions
  login: (credentials: PlatformAdminLoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  checkAuth: () => Promise<void>;
  setHasHydrated: (hasHydrated: boolean) => void;
  clearError: () => void;
  isSuperAdmin: () => boolean;
}

export const usePlatformAdminStore = create<PlatformAdminState>()(
  persist(
    (set, get) => ({
      admin: null,
      isAuthenticated: false,
      isLoading: true,
      hasHydrated: false,
      error: null,

      login: async (credentials: PlatformAdminLoginRequest) => {
        set({ isLoading: true, error: null });
        try {
          // Backend returns response directly (not wrapped in ApiResponse)
          const response = await post<PlatformAdminLoginResponse, PlatformAdminLoginRequest>(
            '/platform-admin/auth/login',
            credentials
          );

          setPlatformAdminTokens(response.accessToken, response.refreshToken);

          set({
            admin: response.admin,
            isAuthenticated: true,
            isLoading: false,
            error: null,
          });
        } catch (err) {
          const error = err as { message?: string };
          set({
            admin: null,
            isAuthenticated: false,
            isLoading: false,
            error: error.message || 'Login failed',
          });
          throw err;
        }
      },

      logout: async () => {
        try {
          const refreshToken = getPlatformAdminRefreshToken();
          if (refreshToken) {
            await post('/platform-admin/auth/logout', { refreshToken });
          }
        } catch {
          // Ignore logout errors
        } finally {
          clearPlatformAdminTokens();
          set({
            admin: null,
            isAuthenticated: false,
            error: null,
          });
        }
      },

      checkAuth: async () => {
        const token = getPlatformAdminToken();
        if (!token) {
          set({ admin: null, isAuthenticated: false, isLoading: false });
          return;
        }

        set({ isLoading: true });
        try {
          // Use platform admin token for this request
          // Backend returns PlatformAdmin directly (not wrapped in ApiResponse)
          const admin = await apiGet<PlatformAdmin>('/platform-admin/auth/me', {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          });

          set({
            admin,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch {
          clearPlatformAdminTokens();
          set({
            admin: null,
            isAuthenticated: false,
            isLoading: false,
          });
        }
      },

      setHasHydrated: (hasHydrated: boolean) => {
        set({ hasHydrated });
      },

      clearError: () => {
        set({ error: null });
      },

      isSuperAdmin: (): boolean => {
        const { admin } = get();
        return admin?.isSuperAdmin ?? false;
      },
    }),
    {
      name: 'platform-admin-storage',
      partialize: (state) => ({
        admin: state.admin,
        isAuthenticated: state.isAuthenticated,
      }),
      onRehydrateStorage: () => (state) => {
        state?.setHasHydrated(true);
      },
    }
  )
);

/**
 * Hook to check if current admin is super admin.
 */
export function useIsSuperAdmin(): boolean {
  return usePlatformAdminStore((state) => state.admin?.isSuperAdmin ?? false);
}
