import React from 'react';

interface ImportStatusBadgeProps {
  status: string;
}

export const ImportStatusBadge: React.FC<ImportStatusBadgeProps> = ({ status }) => {
  let badgeStyle = {
    background: 'rgba(255, 255, 255, 0.08)',
    color: 'var(--text-muted, #94a3b8)',
    border: '1px solid rgba(255, 255, 255, 0.1)'
  };

  let label = status;

  switch (status) {
    case 'Uploaded':
      label = 'Yüklendi';
      badgeStyle = { background: 'rgba(59, 130, 246, 0.15)', color: '#60a5fa', border: '1px solid rgba(59, 130, 246, 0.3)' };
      break;
    case 'Parsing':
      label = 'Okunuyor...';
      badgeStyle = { background: 'rgba(59, 130, 246, 0.2)', color: '#93c5fd', border: '1px solid rgba(59, 130, 246, 0.4)' };
      break;
    case 'MappingRequired':
      label = 'Eşleştirme Bekliyor';
      badgeStyle = { background: 'rgba(245, 158, 11, 0.15)', color: '#fbbf24', border: '1px solid rgba(245, 158, 11, 0.3)' };
      break;
    case 'Validating':
      label = 'Doğrulanıyor';
      badgeStyle = { background: 'rgba(59, 130, 246, 0.15)', color: '#60a5fa', border: '1px solid rgba(59, 130, 246, 0.3)' };
      break;
    case 'ValidationFailed':
      label = 'Hatalı Satırlar Var';
      badgeStyle = { background: 'rgba(239, 68, 68, 0.15)', color: '#f87171', border: '1px solid rgba(239, 68, 68, 0.3)' };
      break;
    case 'ReadyForConfirmation':
      label = 'Onaya Hazır';
      badgeStyle = { background: 'rgba(16, 185, 129, 0.15)', color: '#34d399', border: '1px solid rgba(16, 185, 129, 0.3)' };
      break;
    case 'Importing':
      label = 'Aktarılıyor...';
      badgeStyle = { background: 'rgba(139, 92, 246, 0.2)', color: '#c084fc', border: '1px solid rgba(139, 92, 246, 0.4)' };
      break;
    case 'Completed':
      label = 'Tamamlandı';
      badgeStyle = { background: 'rgba(16, 185, 129, 0.2)', color: '#10b981', border: '1px solid rgba(16, 185, 129, 0.4)' };
      break;
    case 'Failed':
      label = 'Başarısız';
      badgeStyle = { background: 'rgba(239, 68, 68, 0.2)', color: '#ef4444', border: '1px solid rgba(239, 68, 68, 0.4)' };
      break;
    case 'Cancelled':
      label = 'İptal Edildi';
      badgeStyle = { background: 'rgba(148, 163, 184, 0.15)', color: '#94a3b8', border: '1px solid rgba(148, 163, 184, 0.3)' };
      break;
  }

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        padding: '0.25rem 0.65rem',
        borderRadius: '9999px',
        fontSize: '0.75rem',
        fontWeight: 600,
        letterSpacing: '0.025em',
        whiteSpace: 'nowrap',
        ...badgeStyle
      }}
    >
      {label}
    </span>
  );
};
