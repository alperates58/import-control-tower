import React, { useEffect, useState } from 'react';
import { DocumentVersion } from '../../types/document';
import { documentService } from '../../services/documentService';
import { useAuth } from '../../context/AuthContext';

interface Props {
  documentId: string;
  documentTitle: string;
  isOpen: boolean;
  onClose: () => void;
}

export const DocumentVersionDrawer: React.FC<Props> = ({
  documentId,
  documentTitle,
  isOpen,
  onClose
}) => {
  const { authenticatedFetch } = useAuth();
  const [versions, setVersions] = useState<DocumentVersion[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (isOpen && documentId) {
      setLoading(true);
      documentService.getDocumentVersions(documentId, authenticatedFetch)
        .then(setVersions)
        .catch(() => setVersions([]))
        .finally(() => setLoading(false));
    }
  }, [isOpen, documentId]);

  if (!isOpen) return null;

  const handleDownload = async (vId: string) => {
    try {
      const res = await documentService.getDownloadUrl(documentId, vId, authenticatedFetch);
      window.open(res.downloadUrl, '_blank');
    } catch (err: any) {
      alert(err.message);
    }
  };

  return (
    <div className="modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-container" style={{ maxWidth: '600px' }}>
        <div className="modal-header">
          <div>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)' }}>
              📜 Belge Versiyon Geçmişi
            </h3>
            <span style={{ fontSize: '0.82rem', color: 'var(--text-muted)' }}>{documentTitle}</span>
          </div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: '1.4rem', cursor: 'pointer' }}>&times;</button>
        </div>

        <div className="modal-body">
          {loading ? (
            <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>Versiyonlar yükleniyor...</div>
          ) : versions.length === 0 ? (
            <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>Geçmiş versiyon kaydı bulunamadı.</div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
              {versions.map((v) => (
                <div
                  key={v.id}
                  style={{
                    padding: '0.85rem 1rem',
                    background: v.isCurrent ? 'rgba(56, 189, 248, 0.08)' : 'rgba(30, 41, 59, 0.4)',
                    border: `1px solid ${v.isCurrent ? 'var(--accent-blue)' : 'var(--border-color)'}`,
                    borderRadius: '8px',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center'
                  }}
                >
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                      <span style={{ fontWeight: 800, fontFamily: 'var(--font-mono)', color: 'var(--accent-blue)' }}>
                        v{v.versionNumber}
                      </span>
                      {v.isCurrent && <span className="badge badge-emerald badge-sm">Aktif Sürüm</span>}
                      {v.status === 'Replaced' && <span className="badge badge-sm" style={{ background: 'rgba(148, 163, 184, 0.2)', color: '#94a3b8' }}>Eski Sürüm</span>}
                    </div>
                    <div style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-main)', marginTop: '0.2rem' }}>
                      {v.originalFileName}
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.15rem' }}>
                      {(v.fileSizeBytes / 1024).toFixed(1)} KB | {new Date(v.uploadedAtUtc).toLocaleString('tr-TR')} {v.uploadedByUserName ? `| ${v.uploadedByUserName}` : ''}
                    </div>
                  </div>

                  {v.storageStatus === 'Active' && v.status !== 'Cancelled' && (
                    <button
                      onClick={() => handleDownload(v.id)}
                      className="btn-secondary btn-sm"
                    >
                      📥 İndir
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="modal-footer">
          <button onClick={onClose} className="btn-secondary">Kapat</button>
        </div>
      </div>
    </div>
  );
};
