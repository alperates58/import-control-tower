import React, { useEffect, useState } from 'react';
import { DocumentChecklist } from '../../types/document';
import { documentService } from '../../services/documentService';
import { useAuth } from '../../context/AuthContext';

interface Props {
  scopeType: 'ImportCase' | 'Shipment';
  scopeId: string;
}

export const DocumentChecklistWidget: React.FC<Props> = ({ scopeType, scopeId }) => {
  const { authenticatedFetch } = useAuth();
  const [checklist, setChecklist] = useState<DocumentChecklist | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchChecklist = async () => {
    setLoading(true);
    try {
      const data = await documentService.getChecklist(scopeType, scopeId, authenticatedFetch);
      setChecklist(data);
    } catch {
      setChecklist(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchChecklist();
  }, [scopeType, scopeId]);

  if (loading) return <div style={{ fontSize: '0.85rem', color: 'var(--text-dim)' }}>Evrak checklist kontrol ediliyor...</div>;
  if (!checklist) return null;

  const isComplete = checklist.status === 'Complete';

  return (
    <div className="panel" style={{ marginBottom: 0, borderLeft: `4px solid ${isComplete ? 'var(--accent-emerald)' : 'var(--accent-amber)'}` }}>
      <div className="panel-header" style={{ marginBottom: '0.75rem', paddingBottom: '0.5rem' }}>
        <div className="panel-title" style={{ fontSize: '0.95rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span>{isComplete ? '✅' : '⚠️'}</span>
          <span>Zorunlu Evrak Kontrol Listesi (Checklist)</span>
        </div>
        <span className={`badge ${isComplete ? 'badge-emerald' : 'badge-amber'}`}>
          {checklist.completedCount} / {checklist.items.length} Tamamlandı
        </span>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '0.75rem' }}>
        {checklist.items.map((item, idx) => (
          <div
            key={idx}
            style={{
              padding: '0.65rem 0.85rem',
              borderRadius: '6px',
              background: 'var(--bg-elevated)',
              border: '1px solid var(--border-subtle)',
              fontSize: '0.82rem',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.2rem'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <strong style={{ color: 'var(--text-main)' }}>{item.documentType}</strong>
              {item.status === 'Complete' && <span className="badge badge-emerald badge-sm">Mevcut</span>}
              {item.status === 'Missing' && <span className="badge badge-rose badge-sm">Eksik</span>}
              {item.status === 'Expired' && <span className="badge badge-amber badge-sm">Süresi Doldu</span>}
            </div>
            <span style={{ color: 'var(--text-muted)', fontSize: '0.75rem' }}>{item.description}</span>
            {item.documentTitle && (
              <span style={{ color: 'var(--accent-blue)', fontSize: '0.75rem', fontFamily: 'var(--font-mono)' }}>
                📄 {item.documentTitle}
              </span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};
