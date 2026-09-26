import { api } from '../../../services/api';
import type { LoginRequestDto, LoginResponseDto } from '../types/auth.types';
import { clearSession, getStoredUser, saveRememberedEmail, saveSession } from './auth.storage';

export const authService = {
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
