import type { LoginResponseDto, RefreshTokenResponseDto } from '../types/auth.types';

const STORAGE_KEY = 'expo.auth.session';
const REMEMBERED_EMAIL_KEY = 'expo.auth.email';

interface StoredSession {
  user: LoginResponseDto;
}

const readSession = (): StoredSession | null => {
  const rawSession = sessionStorage.getItem(STORAGE_KEY);
  if (!rawSession) return null;

  try {
    const session = JSON.parse(rawSession) as StoredSession;
    if (!session.user?.accessToken || !session.user?.refreshToken) {
      clearSession();
      return null;
    }
    return session;
  } catch {
    clearSession();
    return null;
  }
};

export const saveSession = (user: LoginResponseDto): void => {
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify({ user } satisfies StoredSession));
};

export const updateSessionTokens = (tokens: RefreshTokenResponseDto): void => {
  const session = readSession();
  if (!session) return;

  saveSession({
    ...session.user,
    accessToken: tokens.accessToken,
    refreshToken: tokens.refreshToken,
    expiresAt: tokens.expiresAt,
  });
};

export const clearSession = (): void => {
  sessionStorage.removeItem(STORAGE_KEY);

  // تنظيف المفاتيح القديمة عند ترقية مستخدم لديه نسخة سابقة من الواجهة.
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  localStorage.removeItem('tenantId');
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
