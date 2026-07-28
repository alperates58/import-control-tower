import React, { createContext, useContext, useEffect, useState } from 'react';

export interface UserProfile {
  id: string;
  userName: string;
  email: string;
  fullName: string;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginUtc: string | null;
  createdAtUtc: string;
  roles: string[];
  permissions: string[];
}

interface AuthContextType {
  user: UserProfile | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isBootstrapping: boolean;
  login: (usernameOrEmail: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  hasPermission: (permissionCode: string) => boolean;
  hasRole: (roleName: string) => boolean;
  authenticatedFetch: (url: string, init?: RequestInit) => Promise<Response>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

let refreshPromise: Promise<string | null> | null = null;

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isBootstrapping, setIsBootstrapping] = useState<boolean>(true);

  const performRefreshToken = async (): Promise<string | null> => {
    if (refreshPromise) {
      return refreshPromise;
    }

    refreshPromise = (async () => {
      try {
        const response = await fetch('/api/v1/auth/refresh', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-ICT-CSRF-Protection': '1',
          },
        });

        if (!response.ok) {
          setUser(null);
          setAccessToken(null);
          return null;
        }

        const data = await response.json();
        setAccessToken(data.accessToken);
        setUser(data.user);
        return data.accessToken as string;
      } catch (err) {
        setUser(null);
        setAccessToken(null);
        return null;
      } finally {
        refreshPromise = null;
      }
    })();

    return refreshPromise;
  };

  useEffect(() => {
    const bootstrap = async () => {
      await performRefreshToken();
      setIsBootstrapping(false);
    };
    bootstrap();
  }, []);

  const login = async (usernameOrEmail: string, password: string) => {
    const response = await fetch('/api/v1/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ usernameOrEmail, password }),
    });

    if (!response.ok) {
      const err = await response.json();
      throw new Error(err.detail || 'Giriş başarısız.');
    }

    const data = await response.json();
    setAccessToken(data.accessToken);
    setUser(data.user);
  };

  const logout = async () => {
    try {
      await fetch('/api/v1/auth/logout', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-ICT-CSRF-Protection': '1',
        },
      });
    } finally {
      setUser(null);
      setAccessToken(null);
    }
  };

  const changePassword = async (currentPassword: string, newPassword: string) => {
    const response = await authenticatedFetch('/api/v1/auth/change-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ currentPassword, newPassword }),
    });

    if (!response.ok) {
      const err = await response.json();
      throw new Error(err.detail || 'Parola değiştirilemedi.');
    }

    if (user) {
      setUser({ ...user, mustChangePassword: false });
    }
  };

  const authenticatedFetch = async (url: string, init?: RequestInit): Promise<Response> => {
    let token = accessToken;

    if (!token) {
      token = await performRefreshToken();
    }

    const headers = new Headers(init?.headers || {});
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    let response = await fetch(url, { ...init, headers });

    if (response.status === 401) {
      // Retry once after refresh
      const newToken = await performRefreshToken();
      if (newToken) {
        headers.set('Authorization', `Bearer ${newToken}`);
        response = await fetch(url, { ...init, headers });
      }
    }

    return response;
  };

  const hasPermission = (permissionCode: string): boolean => {
    if (!user) return false;
    if (user.roles.includes('SystemAdmin')) return true;
    return user.permissions.includes(permissionCode);
  };

  const hasRole = (roleName: string): boolean => {
    if (!user) return false;
    return user.roles.includes(roleName);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        accessToken,
        isAuthenticated: !!user,
        isBootstrapping,
        login,
        logout,
        changePassword,
        hasPermission,
        hasRole,
        authenticatedFetch,
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
