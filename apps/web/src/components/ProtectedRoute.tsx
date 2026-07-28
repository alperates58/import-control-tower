import React from 'react';
import { useAuth } from '../context/AuthContext';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermission?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredPermission }) => {
  const { isAuthenticated, isBootstrapping, hasPermission } = useAuth();

  if (isBootstrapping) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', background: '#0f172a', color: '#94a3b8' }}>
        <div>Kimlik doğrulanıyor...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: '#f87171' }}>
        <h2>Erişim Engellendi</h2>
        <p>Lütfen önce oturum açın.</p>
      </div>
    );
  }

  if (requiredPermission && !hasPermission(requiredPermission)) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: '#f87171' }}>
        <h2>Yetkisiz Erişim (403)</h2>
        <p>Bu sayfayı görüntülemek için gerekli izne ({requiredPermission}) sahip değilsiniz.</p>
      </div>
    );
  }

  return <>{children}</>;
};
