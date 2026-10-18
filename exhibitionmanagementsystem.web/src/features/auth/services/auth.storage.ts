import type { LoginResponseDto, RefreshTokenResponseDto } from '../types/auth.types';

const STORAGE_KEY = 'expo.auth.session';
const REMEMBERED_EMAIL_KEY = 'expo.auth.email';
export const SESSION_CHANGED_EVENT = 'auth:session-changed';

interface StoredSession {
  user: LoginResponseDto;
  sessionId: string;
}

const newSessionId = (): string => globalThis.crypto?.randomUUID?.()
  ?? `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;

const readSession = (): StoredSession | null => {
  const rawSession = sessionStorage.getItem(STORAGE_KEY);
  if (!rawSession) return null;

  try {
    const session = JSON.parse(rawSession) as StoredSession;
    if (!session.user?.accessToken || !session.user?.refreshToken) {
      clearSession();
      return null;
    }
    if (!session.sessionId) {
      session.sessionId = newSessionId();
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    }
    return session;
  } catch {
    clearSession();
    return null;
  }
};

export const saveSession = (user: LoginResponseDto): void => {
  const previous = readSession();
  const sessionId = previous?.user.userId === user.userId
    && previous.user.tenantID === user.tenantID
    && previous.user.refreshToken === user.refreshToken
    ? previous.sessionId : newSessionId();
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ user, sessionId } satisfies StoredSession));
  window.dispatchEvent(new Event(SESSION_CHANGED_EVENT));
};

export const getSessionId = (): string | null => readSession()?.sessionId ?? null;

export const updateSessionTokens = (tokens: RefreshTokenResponseDto, expectedSessionId?: string): boolean => {
  const session = readSession();
  if (!session || (expectedSessionId !== undefined && session.sessionId !== expectedSessionId)) return false;

  session.user = {
    ...session.user,
    accessToken: tokens.accessToken,
    refreshToken: tokens.refreshToken,
    expiresAt: tokens.expiresAt,
  };
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  window.dispatchEvent(new Event(SESSION_CHANGED_EVENT));
  return true;
};

export const clearSession = (): void => {
  sessionStorage.removeItem(STORAGE_KEY);

  // تنظيف المفاتيح القديمة عند ترقية مستخدم لديه نسخة سابقة من الواجهة.
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  localStorage.removeItem('tenantId');
  window.dispatchEvent(new Event(SESSION_CHANGED_EVENT));
};

export const getStoredUser = (): LoginResponseDto | null => readSession()?.user ?? null;

export const getAccessToken = (): string | null => readSession()?.user.accessToken ?? null;

export const getRefreshToken = (): string | null => readSession()?.user.refreshToken ?? null;

export const saveRememberedEmail = (email: string | null): void => {
  if (email) {
    localStorage.setItem(REMEMBERED_EMAIL_KEY, email);
  } else {
    localStorage.removeItem(REMEMBERED_EMAIL_KEY);
  }
};

export const getRememberedEmail = (): string => localStorage.getItem(REMEMBERED_EMAIL_KEY) ?? '';
