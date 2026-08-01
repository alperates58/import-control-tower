import React from 'react';

interface ImportStatusBadgeProps {
  status: string;
}

export const ImportStatusBadge: React.FC<ImportStatusBadgeProps> = ({ status }) => {
  let badgeStyle: React.CSSProperties = {
    background: 'var(--status-neutral-bg)',
    color: 'var(--status-neutral)',
    border: '1px solid var(--status-neutral-border)'
  };

  let label = status;

  switch (status) {
    case 'Uploaded':
      label = 'Yüklendi';
      badgeStyle = { background: 'var(--status-info-bg)', color: 'var(--status-info)', border: '1px solid var(--status-info-border)' };
      break;
    case 'Parsing':
      label = 'Okunuyor...';
      badgeStyle = { background: 'var(--status-info-bg)', color: 'var(--status-info)', border: '1px solid var(--status-info-border)' };
      break;
    case 'MappingRequired':
      label = 'Eşleştirme Bekliyor';
      badgeStyle = { background: 'var(--status-warning-bg)', color: 'var(--status-warning)', border: '1px solid var(--status-warning-border)' };
      break;
    case 'Validating':
      label = 'Doğrulanıyor';
      badgeStyle = { background: 'var(--status-info-bg)', color: 'var(--status-info)', border: '1px solid var(--status-info-border)' };
      break;
    case 'ValidationFailed':
      label = 'Hatalı Satırlar Var';
      badgeStyle = { background: 'var(--status-danger-bg)', color: 'var(--status-danger)', border: '1px solid var(--status-danger-border)' };
      break;
    case 'ReadyForConfirmation':
      label = 'Onaya Hazır';
      badgeStyle = { background: 'var(--status-success-bg)', color: 'var(--status-success)', border: '1px solid var(--status-success-border)' };
      break;
    case 'Importing':
      label = 'Aktarılıyor...';
      badgeStyle = { background: 'var(--status-info-bg)', color: 'var(--status-info)', border: '1px solid var(--status-info-border)' };
      break;
    case 'Completed':
      label = 'Tamamlandı';
      badgeStyle = { background: 'var(--status-success-bg)', color: 'var(--status-success)', border: '1px solid var(--status-success-border)' };
      break;
    case 'Failed':
      label = 'Başarısız';
      badgeStyle = { background: 'var(--status-danger-bg)', color: 'var(--status-danger)', border: '1px solid var(--status-danger-border)' };
      break;
    case 'Cancelled':
      label = 'İptal Edildi';
      badgeStyle = { background: 'var(--status-neutral-bg)', color: 'var(--status-neutral)', border: '1px solid var(--status-neutral-border)' };
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
