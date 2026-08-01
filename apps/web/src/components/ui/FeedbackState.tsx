import React from 'react';
import { Button } from './Button';

export interface EmptyStateProps {
  title?: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ReactNode;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title = 'Kayıt Bulunamadı',
  description = 'Arama kriterlerinize uyan veri bulunamadı.',
  action,
  icon = '📦'
}) => (
  <div className="empty-state">
    <div className="empty-icon">{icon}</div>
    <div className="empty-title">{title}</div>
    <div className="empty-desc">{description}</div>
    {action}
  </div>
);

export interface ErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
}

export const ErrorState: React.FC<ErrorStateProps> = ({
  title = 'Bir Hata Oluştu',
  description = 'Veriler yüklenirken sistemsel bir hata oluştu.',
  onRetry
}) => (
  <div className="error-state">
    <div className="error-icon" style={{ color: 'var(--accent-rose)' }}>⚠️</div>
    <div className="error-title">{title}</div>
    <div className="error-desc">{description}</div>
    {onRetry && (
      <Button variant="secondary" size="sm" onClick={onRetry}>
        Tekrar Dene
      </Button>
    )}
  </div>
);

export const LoadingSkeleton: React.FC<{ rows?: number; height?: string }> = ({
  rows = 4,
  height = '36px'
}) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', width: '100%' }}>
    {Array.from({ length: rows }).map((_, i) => (
      <div key={i} className="skeleton" style={{ height, width: '100%' }} />
    ))}
  </div>
);
