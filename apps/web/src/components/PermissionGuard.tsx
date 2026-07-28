import React from 'react';
import { useAuth } from '../context/AuthContext';

interface PermissionGuardProps {
  permission: string;
  fallback?: React.ReactNode;
  children: React.ReactNode;
}

export const PermissionGuard: React.FC<PermissionGuardProps> = ({ permission, fallback = null, children }) => {
  const { hasPermission } = useAuth();
  if (!hasPermission(permission)) {
    return <>{fallback}</>;
  }
  return <>{children}</>;
};
