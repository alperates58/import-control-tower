import React from 'react';

export interface CardProps {
  children: React.ReactNode;
  className?: string;
  style?: React.CSSProperties;
  onClick?: () => void;
}

export const Card: React.FC<CardProps> = ({ children, className = '', style, onClick }) => (
  <div
    className={`card ${className}`.trim()}
    style={{ ...style, cursor: onClick ? 'pointer' : undefined }}
    onClick={onClick}
  >
    {children}
  </div>
);

export const Section: React.FC<{ title: string; subtitle?: string; action?: React.ReactNode; children: React.ReactNode }> = ({
  title,
  subtitle,
  action,
  children
}) => (
  <div className="panel">
    <div className="panel-header">
      <div>
        <div className="panel-title">{title}</div>
        {subtitle && <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)', marginTop: '0.2rem' }}>{subtitle}</div>}
      </div>
      {action && <div>{action}</div>}
    </div>
    {children}
  </div>
);

export interface KPICardProps {
  title: string;
  value: React.ReactNode;
  subtext?: string;
  icon?: React.ReactNode;
  valueColor?: string;
  onClick?: () => void;
}

export const KPICard: React.FC<KPICardProps> = ({ title, value, subtext, icon, valueColor, onClick }) => (
  <div className="kpi-card" onClick={onClick} style={{ cursor: onClick ? 'pointer' : undefined }}>
    <div className="kpi-card-header">
      <span className="kpi-title">{title}</span>
      {icon && <div className="kpi-icon-box">{icon}</div>}
    </div>
    <div className="kpi-value" style={{ color: valueColor || 'var(--text-main)' }}>
      {value}
    </div>
    {subtext && <div className="kpi-subtext">{subtext}</div>}
  </div>
);

export const DetailField: React.FC<{ label: string; value: React.ReactNode; isMono?: boolean }> = ({
  label,
  value,
  isMono
}) => (
  <div style={{ marginBottom: 'var(--space-3)' }}>
    <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)', fontWeight: 'var(--weight-semibold)', marginBottom: '0.2rem' }}>
      {label}
    </div>
    <div style={{ fontSize: 'var(--font-sm)', color: 'var(--text-main)', fontFamily: isMono ? 'var(--font-mono)' : undefined }}>
      {value ?? '-'}
    </div>
  </div>
);
