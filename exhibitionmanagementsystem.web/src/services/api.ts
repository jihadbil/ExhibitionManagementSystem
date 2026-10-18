import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import type { RefreshTokenResponseDto } from '../features/auth/types/auth.types';
import {
  clearSession,
  getAccessToken,
  getRefreshToken,
  getSessionId,
  updateSessionTokens,
} from '../features/auth/services/auth.storage';

export const API_BASE_URL = import.meta.env.VITE_API_URL || '';

type SessionRequest = InternalAxiosRequestConfig & { _retry?: boolean; _sessionId?: string | null };

export const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

const IDEMPOTENT_POST_PATHS = new Set([
  '/api/reservations',
  '/api/financial/payments',
]);

const createIdempotencyKey = (): string => {
  if (typeof globalThis.crypto?.randomUUID === 'function') {
    return globalThis.crypto.randomUUID();
  }

  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
};

const requiresIdempotencyKey = (config: InternalAxiosRequestConfig): boolean => {
  if (config.method?.toLowerCase() !== 'post' || !config.url) return false;

  try {
    const base = config.baseURL || window.location.origin;
    const path = new URL(config.url, base).pathname.replace(/\/+$/, '') || '/';
    return IDEMPOTENT_POST_PATHS.has(path.toLowerCase());
  } catch {
    const path = config.url.split('?')[0].replace(/\/+$/, '').toLowerCase();
    return IDEMPOTENT_POST_PATHS.has(path);
  }
};

// هوية المستأجر تأتي حصراً من JWT في الخادم، ولا تقبل الواجهة تغييرها محلياً.
api.interceptors.request.use(
  (config: SessionRequest) => {
    const sessionId = getSessionId();
    if (config._retry && config._sessionId !== sessionId) {
      throw new Error('تغيرت الجلسة قبل إعادة الطلب.');
    }
    config._sessionId = sessionId;
    const token = getAccessToken();

    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    } else {
      config.headers.delete('Authorization');
    }

    // Preserve an existing key on token-refresh retries; create one only for a
    // new financial/reservation command.
    if (config.headers && requiresIdempotencyKey(config) && !config.headers.has('Idempotency-Key')) {
      config.headers.set('Idempotency-Key', createIdempotencyKey());
    }

    return config;
  },
  (error: AxiosError) => Promise.reject(error)
);

// A refresh belongs to the session that initiated it, including queued retries.
let refreshFlight: { sessionId: string; promise: Promise<string> } | null = null;

const refreshSession = (sessionId: string): Promise<string> => {
  if (refreshFlight?.sessionId === sessionId) return refreshFlight.promise;
  const refreshToken = getRefreshToken();
  const accessToken = getAccessToken();
  const flight = { sessionId, promise: Promise.resolve('') };
  flight.promise = (async () => {
    try {
      if (!refreshToken || !accessToken || getSessionId() !== sessionId) {
        throw new Error('انتهت الجلسة.');
      }
      const response = await axios.post<RefreshTokenResponseDto>(`${API_BASE_URL}/api/auth/refresh-token`, {
        accessToken, refreshToken,
      }, { timeout: 30000 });
      const tokens = response.data;
      if (!tokens.accessToken || !tokens.refreshToken) throw new Error('استجابة تجديد الجلسة غير صالحة.');
      if (!updateSessionTokens(tokens, sessionId)) throw new Error('تغيرت الجلسة أثناء تجديدها.');
      return tokens.accessToken;
    } catch (error) {
      if (getSessionId() === sessionId) {
        clearSession();
        window.dispatchEvent(new Event('auth:expired'));
      }
      throw error;
    } finally {
      if (refreshFlight === flight) refreshFlight = null;
    }
  })();
  refreshFlight = flight;
  return flight.promise;
};

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as SessionRequest | undefined;
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      const sessionId = originalRequest._sessionId;
      if (!sessionId || sessionId !== getSessionId()) return Promise.reject(error);
      originalRequest._retry = true;
      const token = await refreshSession(sessionId);
      if (getSessionId() !== sessionId) throw new Error('تغيرت الجلسة قبل إعادة الطلب.');
      originalRequest.headers.Authorization = `Bearer ${token}`;
      return api(originalRequest);
    }

    return Promise.reject(error);
  }
);
