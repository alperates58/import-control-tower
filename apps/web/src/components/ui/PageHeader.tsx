import React from 'react';

export interface PageHeaderProps {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  actions?: React.ReactNode;
}

export const PageHeader: React.FC<PageHeaderProps> = ({ title, subtitle, actions }) => (
  <div className="page-header">
    <div className="page-header-title-group">
      <h2>{title}</h2>
      {subtitle && <div className="page-header-subtitle">{subtitle}</div>}
    </div>
    {actions && <div className="page-actions">{actions}</div>}
  </div>
);

export interface TabItem {
  id: string;
  label: string;
  count?: number;
}

export const Tabs: React.FC<{ tabs: TabItem[]; activeTab: string; onChange: (id: string) => void }> = ({
  tabs,
  activeTab,
  onChange
}) => (
  <div style={{ display: 'flex', gap: '0.25rem', borderBottom: '1px solid var(--border-color)', marginBottom: 'var(--space-5)' }}>
    {tabs.map((tab) => {
      const isActive = tab.id === activeTab;
      return (
        <button
          key={tab.id}
          onClick={() => onChange(tab.id)}
          style={{
            padding: '0.6rem 1rem',
            background: 'transparent',
            border: 'none',
            borderBottom: isActive ? '2px solid var(--accent-blue)' : '2px solid transparent',
            color: isActive ? 'var(--accent-blue)' : 'var(--text-muted)',
            fontWeight: isActive ? 'var(--weight-semibold)' : 'var(--weight-medium)',
            fontSize: 'var(--font-sm)',
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            gap: '0.4rem'
          }}
        >
          <span>{tab.label}</span>
          {tab.count !== undefined && (
            <span
              style={{
                fontSize: '0.68rem',
                padding: '0.1rem 0.4rem',
                borderRadius: 'var(--radius-full)',
                background: isActive ? 'var(--primary-light)' : 'var(--bg-elevated)',
                color: isActive ? 'var(--accent-blue)' : 'var(--text-dim)'
              }}
            >
              {tab.count}
            </span>
          )}
        </button>
      );
    })}
  </div>
);

export interface TimelineItem {
  id: string;
  title: string;
  subtitle?: string;
  timestamp: string;
  status?: string;
}

export const ActivityTimeline: React.FC<{ items: TimelineItem[] }> = ({ items }) => (
  <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem', paddingLeft: '0.5rem' }}>
    {items.map((item, idx) => (
      <div key={item.id || idx} style={{ display: 'flex', gap: '0.75rem', position: 'relative' }}>
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <div
            style={{
              width: '10px',
              height: '10px',
              borderRadius: '50%',
              background: item.status === 'Completed' ? 'var(--status-success)' : 'var(--primary)'
            }}
          />
          {idx < items.length - 1 && (
            <div style={{ width: '2px', flex: 1, background: 'var(--border-subtle)', marginTop: '4px' }} />
          )}
        </div>
        <div style={{ flex: 1, paddingBottom: '0.5rem' }}>
          <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-dim)' }}>{item.timestamp}</div>
          <div style={{ fontSize: 'var(--font-sm)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>
            {item.title}
          </div>
          {item.subtitle && (
            <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)', marginTop: '0.1rem' }}>
              {item.subtitle}
            </div>
          )}
        </div>
      </div>
    ))}
  </div>
);
