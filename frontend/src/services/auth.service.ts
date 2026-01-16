import { post, get } from '@/lib/api-client';
import type { LoginRequest, LoginResponse, User } from '@/types/api';

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  tenantSlug?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export const authService = {
  /**
   * Login with email, password, and optional tenant slug.
   */
  login: (data: LoginRequest & { tenantSlug?: string }) =>
    post<LoginResponse, LoginRequest & { tenantSlug?: string }>('/auth/login', data),

  /**
   * Register a new user.
   */
  register: (data: RegisterRequest) =>
    post<LoginResponse, RegisterRequest>('/auth/register', data),

  /**
   * Refresh access token using refresh token.
   */
  refreshToken: (data: RefreshTokenRequest) =>
    post<RefreshTokenResponse, RefreshTokenRequest>('/auth/refresh', data),

  /**
   * Logout and revoke refresh token.
   */
  logout: (refreshToken: string) =>
    post<void, { refreshToken: string }>('/auth/logout', { refreshToken }),

  /**
   * Get current authenticated user info.
   */
  getCurrentUser: () =>
    get<{ user: User }>('/auth/me'),
};
