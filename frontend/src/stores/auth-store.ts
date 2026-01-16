import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { User, LoginRequest, LoginResponse, ApiResponse } from '@/types/api';
import { post, get as apiGet, clearTokens, setTokens, getAccessToken } from '@/lib/api-client';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  hasHydrated: boolean;
  error: string | null;

  // Actions
  login: (credentials: LoginRequest) => Promise<void>;
  logout: () => void;
  setUser: (user: User | null) => void;
  clearError: () => void;
  checkAuth: () => Promise<void>;
  setHasHydrated: (hasHydrated: boolean) => void;
  hasPermission: (permission: string) => boolean;
  hasRole: (role: string) => boolean;
  hasAnyRole: (roles: string[]) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      isAuthenticated: false,
      isLoading: true, // Start with true until hydration and auth check complete
      hasHydrated: false,
      error: null,

      login: async (credentials: LoginRequest) => {
        set({ isLoading: true, error: null });
        try {
          // Backend returns ApiResponse<AuthResponse> with data property
          const response = await post<ApiResponse<LoginResponse>, LoginRequest>(
            '/auth/login',
            credentials
          );

          const authData = response.data;
          // Store tokens along with tenant slug for API header injection
          setTokens(authData.accessToken, authData.refreshToken, credentials.tenantSlug);

          // Map UserInfo from backend to frontend User type
          const user: User = {
            id: authData.user.id,
            email: authData.user.email,
            firstName: authData.user.firstName,
            lastName: authData.user.lastName,
            fullName: `${authData.user.firstName} ${authData.user.lastName}`.trim(),
            roles: authData.user.roleName ? [authData.user.roleName] : [],
            permissions: authData.user.permissions || [],
          };

          set({
            user,
            isAuthenticated: true,
            isLoading: false,
            error: null,
          });
        } catch (err) {
          const error = err as { message?: string };
          set({
            user: null,
            isAuthenticated: false,
            isLoading: false,
            error: error.message || 'Login failed',
          });
          throw err;
        }
      },

      logout: () => {
        clearTokens();
        set({
          user: null,
          isAuthenticated: false,
          error: null,
        });
      },

      setUser: (user: User | null) => {
        set({
          user,
          isAuthenticated: !!user,
        });
      },

      clearError: () => {
        set({ error: null });
      },

      setHasHydrated: (hasHydrated: boolean) => {
        set({ hasHydrated });
      },

      checkAuth: async () => {
        const token = getAccessToken();
        if (!token) {
          set({ user: null, isAuthenticated: false, isLoading: false });
          return;
        }

        set({ isLoading: true });
        try {
          // Backend returns ApiResponse<UserInfo> with data property
          const response = await apiGet<ApiResponse<{
            id: string;
            email: string;
            firstName: string;
            lastName: string;
            roleName?: string;
            permissions: string[];
          }>>('/auth/me');

          const userInfo = response.data;
          const user: User = {
            id: userInfo.id,
            email: userInfo.email,
            firstName: userInfo.firstName,
            lastName: userInfo.lastName,
            fullName: `${userInfo.firstName} ${userInfo.lastName}`.trim(),
            roles: userInfo.roleName ? [userInfo.roleName] : [],
            permissions: userInfo.permissions || [],
          };

          set({
            user,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch {
          clearTokens();
          set({
            user: null,
            isAuthenticated: false,
            isLoading: false,
          });
        }
      },

      hasPermission: (permission: string): boolean => {
        const { user } = get();
        if (!user) return false;
        return user.permissions.includes(permission);
      },

      hasRole: (role: string): boolean => {
        const { user } = get();
        if (!user) return false;
        return user.roles.includes(role);
      },

      hasAnyRole: (roles: string[]): boolean => {
        const { user } = get();
        if (!user) return false;
        return roles.some((role) => user.roles.includes(role));
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
      onRehydrateStorage: () => (state) => {
        // Called after hydration completes
        state?.setHasHydrated(true);
      },
    }
  )
);

/**
 * Hook to check if user has required permission.
 */
export function usePermission(permission: string): boolean {
  return useAuthStore((state) => state.hasPermission(permission));
}

/**
 * Hook to check if user has required role.
 */
export function useRole(role: string): boolean {
  return useAuthStore((state) => state.hasRole(role));
}

/**
 * Hook to check if user has any of the required roles.
 */
export function useAnyRole(roles: string[]): boolean {
  return useAuthStore((state) => state.hasAnyRole(roles));
}
