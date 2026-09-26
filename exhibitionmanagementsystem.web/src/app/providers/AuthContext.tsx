import React, { createContext, useContext, useState, useEffect } from 'react';
import { LoginRequestDto, LoginResponseDto } from '../../features/auth/types/auth.types';
import { authService } from '../../features/auth/services/auth.service';

interface AuthContextType {
  user: LoginResponseDto | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  tenantId: number;
  login: (credentials: LoginRequestDto) => Promise<void>;
  logout: () => Promise<void>;
  switchTenant: (newTenantId: number, newTenantName: string) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<LoginResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [tenantId, setTenantId] = useState<number>(1);

  useEffect(() => {
    const savedUser = authService.getCurrentUser();
    const savedToken = localStorage.getItem('token');
    const savedTenant = localStorage.getItem('tenantId');

    if (savedUser && savedToken) {
      setUser(savedUser);
      setTenantId(savedTenant ? parseInt(savedTenant, 10) : savedUser.tenantID || 1);
    }
    setIsLoading(false);
  }, []);

  const login = async (credentials: LoginRequestDto) => {
    const data = await authService.login(credentials);
    setUser(data);
    setTenantId(data.tenantID || 1);

    localStorage.setItem('token', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    localStorage.setItem('user', JSON.stringify(data));
    localStorage.setItem('tenantId', (data.tenantID || 1).toString());
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
  };

  const switchTenant = (newTenantId: number, newTenantName: string) => {
    setTenantId(newTenantId);
    localStorage.setItem('tenantId', newTenantId.toString());
    if (user) {
      const updatedUser = { ...user, tenantID: newTenantId, tenantName: newTenantName };
      setUser(updatedUser);
      localStorage.setItem('user', JSON.stringify(updatedUser));
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        tenantId,
        login,
        logout,
        switchTenant
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
