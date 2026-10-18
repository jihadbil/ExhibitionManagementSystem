import { api } from '../../../services/api';
import type {
  CompanyRegistrationRequestDto,
  CompanyRegistrationResponseDto,
  LoginRequestDto,
  LoginResponseDto,
} from '../types/auth.types';
import { clearSession, getStoredUser, saveRememberedEmail, saveSession } from './auth.storage';

export const authService = {
  async requestPasswordReset(email: string): Promise<void> {
    await api.post('/api/auth/forgot-password', { email });
  },

  async resetPassword(email: string, token: string, newPassword: string, confirmPassword: string): Promise<void> {
    await api.post('/api/auth/reset-password/confirm', { email, token, newPassword, confirmPassword });
  },
  async registerCompany(data: CompanyRegistrationRequestDto): Promise<CompanyRegistrationResponseDto> {
    const response = await api.post<CompanyRegistrationResponseDto>('/api/auth/register', data);
    return response.data;
  },

  async login(credentials: LoginRequestDto): Promise<LoginResponseDto> {
    const response = await api.post<LoginResponseDto>('/api/auth/login', credentials);
    saveSession(response.data);
    saveRememberedEmail(credentials.rememberMe ? credentials.email : null);
    return response.data;
  },

  async logout(): Promise<void> {
    try {
      await api.post('/api/auth/logout');
    } finally {
      clearSession();
    }
  },

  getCurrentUser(): LoginResponseDto | null {
    return getStoredUser();
  }
};
