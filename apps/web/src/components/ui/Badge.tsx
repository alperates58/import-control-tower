import React from 'react';

export interface BadgeProps {
  variant?: 'emerald' | 'rose' | 'amber' | 'cyan' | 'purple' | 'neutral' | 'success' | 'warning' | 'danger' | 'info';
  children: React.ReactNode;
  className?: string;
  style?: React.CSSProperties;
}

export const Badge: React.FC<BadgeProps> = ({
  variant = 'neutral',
  children,
  className = '',
  style
}) => {
  const variantClass = `badge-${variant}`;
  return (
    <span className={`badge ${variantClass} ${className}`.trim()} style={style}>
      {children}
    </span>
  );
};

export const StatusBadge: React.FC<{ status: string; label?: string }> = ({ status, label }) => {
  let variant: BadgeProps['variant'] = 'neutral';
  let displayLabel = label || status;

  const lower = status.toLowerCase();
  if (lower.includes('active') || lower.includes('completed') || lower.includes('ready') || lower.includes('open')) {
    variant = 'emerald';
  } else if (lower.includes('pending') || lower.includes('warning') || lower.includes('draft')) {
    variant = 'amber';
  } else if (lower.includes('failed') || lower.includes('error') || lower.includes('disabled') || lower.includes('cancelled')) {
    variant = 'rose';
  } else if (lower.includes('transit') || lower.includes('progress') || lower.includes('processing')) {
    variant = 'cyan';
  }

  return <Badge variant={variant}>{displayLabel}</Badge>;
};
