import React, { useEffect, useState } from 'react';
import { DocumentVersion } from '../../types/document';
import { documentService } from '../../services/documentService';
import { useAuth } from '../../context/AuthContext';
import { Drawer } from '../ui/Drawer';
import { Badge } from '../ui/Badge';
import { Button } from '../ui/Button';

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
    <Drawer
      isOpen={isOpen}
      onClose={onClose}
      title={
        <div>
          <div>📜 Belge Versiyon Geçmişi</div>
          <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)', fontWeight: 'var(--weight-normal)' }}>
            {documentTitle}
          </div>
        </div>
      }
      footer={
        <Button variant="secondary" onClick={onClose}>
          Kapat
        </Button>
      }
    >
      {loading ? (
        <div style={{ padding: 'var(--space-6)', textAlign: 'center', color: 'var(--text-muted)' }}>
          Versiyonlar yükleniyor...
        </div>
      ) : versions.length === 0 ? (
        <div style={{ padding: 'var(--space-6)', textAlign: 'center', color: 'var(--text-muted)' }}>
          Geçmiş versiyon kaydı bulunamadı.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-3)' }}>
          {versions.map((v) => (
            <div
              key={v.id}
              style={{
                padding: 'var(--space-3) var(--space-4)',
                background: v.isCurrent ? 'var(--primary-light)' : 'var(--bg-input)',
                border: `1px solid ${v.isCurrent ? 'var(--border-highlight)' : 'var(--border-subtle)'}`,
                borderRadius: 'var(--radius-md)',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center'
              }}
            >
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-2)' }}>
                  <span className="font-mono" style={{ fontWeight: 'var(--weight-bold)', color: 'var(--accent-blue)' }}>
                    v{v.versionNumber}
                  </span>
                  {v.isCurrent && <Badge variant="emerald">Aktif Sürüm</Badge>}
                  {v.status === 'Replaced' && <Badge variant="neutral">Eski Sürüm</Badge>}
                </div>
                <div style={{ fontSize: 'var(--font-sm)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)', marginTop: 'var(--space-1)' }}>
                  {v.originalFileName}
                </div>
                <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-dim)', marginTop: '0.1rem' }}>
                  {v.fileSizeBytes <= 0 ? '0.0 KB' : v.fileSizeBytes >= 1024 * 1024 ? `${(v.fileSizeBytes / (1024 * 1024)).toFixed(1)} MB` : `${(v.fileSizeBytes / 1024).toFixed(1)} KB`} | {new Date(v.uploadedAtUtc).toLocaleString('tr-TR')} {v.uploadedByUserName ? `| ${v.uploadedByUserName}` : ''}
                </div>
              </div>

              {v.storageStatus === 'Active' && v.status !== 'Cancelled' && (
                <Button variant="secondary" size="sm" onClick={() => handleDownload(v.id)}>
                  📥 İndir
                </Button>
              )}
            </div>
          ))}
        </div>
      )}
    </Drawer>
  );
};
