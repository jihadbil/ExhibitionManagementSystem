import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { RefreshTokenResponseDto } from '../features/auth/types/auth.types';
import {
  clearSession,
  getAccessToken,
  getRefreshToken,
  updateSessionTokens,
} from '../features/auth/services/auth.storage';

export const API_BASE_URL = import.meta.env.VITE_API_URL || '';

export const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// هوية المستأجر تأتي حصراً من JWT في الخادم، ولا تقبل الواجهة تغييرها محلياً.
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAccessToken();

    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error: AxiosError) => Promise.reject(error)
);

// Response Interceptor: Automatic Refresh Token Handling
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: AxiosError | null, token: string | null = null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

const expireSession = (): void => {
  clearSession();
  window.dispatchEvent(new Event('auth:expired'));
};

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then(token => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch(err => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = getRefreshToken();
      const currentToken = getAccessToken();

      if (!refreshToken || !currentToken) {
        isRefreshing = false;
        processQueue(error, null);
        expireSession();
        return Promise.reject(error);
      }

      try {
        const response = await axios.post<RefreshTokenResponseDto>(`${API_BASE_URL}/api/auth/refresh-token`, {
          accessToken: currentToken,
          refreshToken
        });

        const tokens = response.data;
        if (!tokens.accessToken || !tokens.refreshToken) {
          throw new Error('استجابة تجديد الجلسة غير صالحة.');
        }

        updateSessionTokens(tokens);
        processQueue(null, tokens.accessToken);

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${tokens.accessToken}`;
        }
        return api(originalRequest);
      } catch (refreshErr) {
        processQueue(refreshErr as AxiosError, null);
        expireSession();
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
