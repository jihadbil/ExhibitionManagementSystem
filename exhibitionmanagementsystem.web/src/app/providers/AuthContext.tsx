import React, { createContext, useContext, useEffect, useState } from 'react';
import type { LoginRequestDto, LoginResponseDto } from '../../features/auth/types/auth.types';
import { authService } from '../../features/auth/services/auth.service';
import { getStoredUser, saveSession, SESSION_CHANGED_EVENT } from '../../features/auth/services/auth.storage';

interface AuthContextType {
  user: LoginResponseDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequestDto) => Promise<void>;
  logout: () => Promise<void>;
  updateCurrentUser: (changes: Partial<Pick<LoginResponseDto, 'fullName'>>) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<LoginResponseDto | null>(() => authService.getCurrentUser());

  useEffect(() => {
    const handleExpiredSession = () => setUser(null);
    const handleSessionChanged = () => setUser(getStoredUser());
    window.addEventListener('auth:expired', handleExpiredSession);
    window.addEventListener(SESSION_CHANGED_EVENT, handleSessionChanged);
    return () => {
      window.removeEventListener('auth:expired', handleExpiredSession);
      window.removeEventListener(SESSION_CHANGED_EVENT, handleSessionChanged);
    };
  }, []);

  const login = async (credentials: LoginRequestDto) => {
    const data = await authService.login(credentials);
    setUser(data);
  };

  const logout = async () => {
    try {
      await authService.logout();
    } finally {
      setUser(null);
    }
  };

  const updateCurrentUser = (changes: Partial<Pick<LoginResponseDto, 'fullName'>>) => {
    const current = getStoredUser();
    if (!current) { setUser(null); return; }
    const updated = { ...current, ...changes };
    saveSession(updated);
    setUser(updated);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading: false,
        login,
        logout,
        updateCurrentUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

// Provider and hook intentionally share one public module for stable imports.
// eslint-disable-next-line react-refresh/only-export-components
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
