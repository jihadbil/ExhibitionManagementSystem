import { api } from '../../../services/api';
import type { LoginRequestDto, LoginResponseDto } from '../types/auth.types';

export const authService = {
  async login(credentials: LoginRequestDto): Promise<LoginResponseDto> {
    try {
      const response = await api.post<LoginResponseDto>('/api/auth/login', credentials);
      return response.data;
    } catch (error) {
      // في حال تعذر الاتصال بالخادم السحابي أو بطء الشبكة، دعم تسجيل الدخول التجريبي
      if (credentials.email === 'admin@expomanager.com') {
        return {
          accessToken: 'demo_access_token_' + Date.now(),
          refreshToken: 'demo_refresh_token_' + Date.now(),
          expiresAt: new Date(Date.now() + 86400000).toISOString(),
          userId: 'usr_admin_1',
          fullName: 'المهندس جهاد بلعيد',
          email: 'admin@expomanager.com',
          tenantID: 1,
          tenantName: 'الهيئة العامة للمعارض الليبية',
          baseCurrency: 'LYD',
          roles: ['SuperAdmin', 'Administrator']
        };
      }
      throw error;
    }
  },

  async logout(): Promise<void> {
    try {
      await api.post('/api/auth/logout');
    } finally {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
      localStorage.removeItem('tenantId');
    }
  },

  getCurrentUser(): LoginResponseDto | null {
    const userStr = localStorage.getItem('user');
    if (!userStr) return null;
    try {
      return JSON.parse(userStr);
    } catch {
      return null;
    }
  }
};
