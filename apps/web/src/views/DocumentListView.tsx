import React, { useEffect, useState } from 'react';
import { DocumentSummary } from '../types/document';
import { documentService } from '../services/documentService';
import { DocumentUploadModal } from './DocumentUploadModal';
import { DocumentVersionDrawer } from '../components/documents/DocumentVersionDrawer';
import { useAuth } from '../context/AuthContext';

export const DocumentListView: React.FC = () => {
  const { authenticatedFetch, hasPermission } = useAuth();
  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [search, setSearch] = useState('');
  const [documentType, setDocumentType] = useState('');
  const [status, setStatus] = useState('Active');

  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Modals
  const [uploadModalOpen, setUploadModalOpen] = useState(false);
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);
  const [selectedDocTitle, setSelectedDocTitle] = useState('');
  const [versionDrawerOpen, setVersionDrawerOpen] = useState(false);

  const fetchDocuments = async () => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const data = await documentService.getDocuments({
        search: search || undefined,
        documentType: documentType || undefined,
        status: status || undefined
      }, authenticatedFetch);
      setDocuments(data);
    } catch (err: any) {
      setErrorMsg(err.message || 'Belgeler yüklenemedi.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, [search, documentType, status]);

  const handleDownload = async (docId: string) => {
    try {
      const res = await documentService.getDownloadUrl(docId, undefined, authenticatedFetch);
      window.open(res.downloadUrl, '_blank');
    } catch (err: any) {
      alert(err.message);
    }
  };

  const handleCancelDocument = async (doc: DocumentSummary) => {
    if (!confirm(`"${doc.title}" evrakını iptal etmek istediğinize emin misiniz?`)) return;
    try {
      await documentService.cancelDocument(doc.id, doc.rowVersion, authenticatedFetch);
      await fetchDocuments();
    } catch (err: any) {
      alert(err.message);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Header Bar */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text-main)', letterSpacing: '-0.02em' }}>
            İthalat Evrakları ve Belge Yönetimi
          </h1>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>
            Faz 04 — İthalat dosyaları, sevkiyatlar ve konteynerlere ait belgeler ve versiyon takibi
          </p>
        </div>
      </div>

      {/* Filter Toolbar Panel */}
      <div className="panel" style={{ marginBottom: 0 }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', alignItems: 'flex-end' }}>
          <div style={{ gridColumn: 'span 2' }}>
            <label className="form-label">Arama</label>
            <input
              type="text"
              placeholder="Evrak Başlığı veya Fatura No Ara..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <label className="form-label">Evrak Türü</label>
            <select
              value={documentType}
              onChange={(e) => setDocumentType(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="">Tüm Evrak Türleri</option>
              <option value="CommercialInvoice">Commercial Invoice</option>
              <option value="ProformaInvoice">Proforma Invoice</option>
              <option value="PackingList">Packing List</option>
              <option value="BillOfLading">Bill of Lading</option>
              <option value="AirWaybill">Air Waybill</option>
              <option value="CMR">CMR</option>
              <option value="CertificateOfOrigin">Certificate of Origin</option>
              <option value="ATR">A.TR</option>
              <option value="EUR1">EUR.1</option>
              <option value="InsuranceCertificate">Sigorta Poliçesi</option>
              <option value="CustomsDeclaration">Gümrük Beyannamesi</option>
            </select>
          </div>

          <div>
            <label className="form-label">Durum</label>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="">Tüm Durumlar</option>
              <option value="Active">Aktif</option>
              <option value="Cancelled">İptal Edildi</option>
            </select>
          </div>
        </div>
      </div>

      {/* Main Table / State Container */}
      <div className="panel">
        {loading ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--accent-blue)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '1rem' }}>
            <div style={{ width: '32px', height: '32px', border: '3px solid var(--border-color)', borderTopColor: 'var(--accent-blue)', borderRadius: '50%', animation: 'spin 1s linear infinite' }}></div>
            <div style={{ fontSize: '0.9rem', fontWeight: 600 }}>Evraklar yükleniyor...</div>
          </div>
        ) : errorMsg ? (
          <div style={{ padding: '1.5rem', background: 'rgba(244, 63, 94, 0.1)', border: '1px solid rgba(244, 63, 94, 0.3)', borderRadius: '10px', color: 'var(--accent-rose)' }}>
            ⚠️ {errorMsg}
          </div>
        ) : documents.length === 0 ? (
          <div style={{ padding: '4rem 2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
            <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>📄</div>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.4rem' }}>
              Evrak Bulunmadı
            </h3>
            <p style={{ fontSize: '0.85rem', maxWidth: '400px', margin: '0 auto' }}>
              Arama kriterlerinize uygun evrak kaydı bulunamadı.
            </p>
          </div>
        ) : (
          <div className="data-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Evrak Başlığı</th>
                  <th>Evrak Türü</th>
                  <th>Evrak / Fatura No</th>
                  <th>Aktif Sürüm</th>
                  <th>Boyut</th>
                  <th>Oluşturulma</th>
                  <th>Durum</th>
                  <th>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {documents.map((d) => (
                  <tr key={d.id}>
                    <td style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                      📄 {d.title}
                    </td>
                    <td><span className="badge badge-cyan">{d.documentType}</span></td>
                    <td style={{ fontFamily: 'var(--font-mono)' }}>{d.documentNumber || '-'}</td>
                    <td>
                      {d.currentVersion ? (
                        <span className="badge badge-emerald" style={{ fontFamily: 'var(--font-mono)' }}>
                          v{d.currentVersion.versionNumber} ({d.currentVersion.fileExtension})
                        </span>
                      ) : (
                        <span style={{ color: 'var(--text-dim)' }}>-</span>
                      )}
                    </td>
                    <td style={{ fontSize: '0.8rem' }}>
                      {d.currentVersion ? `${(d.currentVersion.fileSizeBytes / 1024).toFixed(1)} KB` : '-'}
                    </td>
                    <td style={{ fontSize: '0.8rem' }}>
                      {new Date(d.createdAtUtc).toLocaleDateString('tr-TR')}
                    </td>
                    <td>
                      {d.status === 'Active' ? (
                        <span className="badge badge-emerald">Aktif</span>
                      ) : (
                        <span className="badge badge-rose">İptal Edildi</span>
                      )}
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: '0.4rem' }}>
                        {d.status === 'Active' && (
                          <button
                            onClick={() => handleDownload(d.id)}
                            className="btn-secondary btn-sm"
                            title="İndir"
                          >
                            📥 İndir
                          </button>
                        )}

                        <button
                          onClick={() => {
                            setSelectedDocId(d.id);
                            setSelectedDocTitle(d.title);
                            setVersionDrawerOpen(true);
                          }}
                          className="btn-secondary btn-sm"
                          title="Versiyon Geçmişi"
                        >
                          📜 Geçmiş
                        </button>

                        {d.status === 'Active' && hasPermission('documents.version') && (
                          <button
                            onClick={() => {
                              setSelectedDocId(d.id);
                              setSelectedDocTitle(d.title);
                              setUploadModalOpen(true);
                            }}
                            className="btn-secondary btn-sm"
                            title="Yeni Sürüm Yükle"
                          >
                            ➕ Sürüm
                          </button>
                        )}

                        {d.status === 'Active' && hasPermission('documents.cancel') && (
                          <button
                            onClick={() => handleCancelDocument(d)}
                            className="btn-secondary btn-sm"
                            style={{ color: 'var(--accent-rose)', borderColor: 'rgba(244, 63, 94, 0.3)' }}
                            title="İptal Et"
                          >
                            🚫
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Version History Drawer */}
      {selectedDocId && (
        <DocumentVersionDrawer
          documentId={selectedDocId}
          documentTitle={selectedDocTitle}
          isOpen={versionDrawerOpen}
          onClose={() => setVersionDrawerOpen(false)}
        />
      )}

      {/* New Version Upload Modal */}
      {selectedDocId && (
        <DocumentUploadModal
          isOpen={uploadModalOpen}
          onClose={() => setUploadModalOpen(false)}
          onSuccess={fetchDocuments}
          existingDocumentId={selectedDocId}
        />
      )}
    </div>
  );
};
