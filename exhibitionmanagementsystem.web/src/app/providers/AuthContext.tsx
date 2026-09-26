import React, { createContext, useContext, useEffect, useState } from 'react';
import type { LoginRequestDto, LoginResponseDto } from '../../features/auth/types/auth.types';
import { authService } from '../../features/auth/services/auth.service';

interface AuthContextType {
  user: LoginResponseDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequestDto) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<LoginResponseDto | null>(() => authService.getCurrentUser());

  useEffect(() => {
    const handleExpiredSession = () => setUser(null);
    window.addEventListener('auth:expired', handleExpiredSession);
    return () => window.removeEventListener('auth:expired', handleExpiredSession);
  }, []);

  const login = async (credentials: LoginRequestDto) => {
    const data = await authService.login(credentials);
    setUser(data);
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading: false,
        login,
        logout
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
