import React, { useState, useEffect } from 'react';
import { documentService } from '../services/documentService';
import { useAuth } from '../context/AuthContext';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  scopeType?: 'ImportCase' | 'Shipment' | 'Container';
  scopeId?: string;
  existingDocumentId?: string; // If provided, adds a new version to existing document
}

export const DocumentUploadModal: React.FC<Props> = ({
  isOpen,
  onClose,
  onSuccess,
  scopeType,
  scopeId,
  existingDocumentId
}) => {
  const { authenticatedFetch } = useAuth();
  const [title, setTitle] = useState('');
  const [documentType, setDocumentType] = useState('CommercialInvoice');
  const [documentNumber, setDocumentNumber] = useState('');
  const [documentDate, setDocumentDate] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [notes, setNotes] = useState('');
  const [file, setFile] = useState<File | null>(null);

  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      setTitle('');
      setDocumentNumber('');
      setDocumentDate('');
      setExpiryDate('');
      setNotes('');
      setFile(null);
      setErrorMsg(null);
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => { document.body.style.overflow = 'unset'; };
  }, [isOpen]);

  if (!isOpen) return null;

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const selected = e.target.files[0];
      if (selected.size > 25 * 1024 * 1024) {
        setErrorMsg('Dosya boyutu 25 MB sınırını aşamaz (HTTP 413).');
        setFile(null);
        return;
      }
      setErrorMsg(null);
      setFile(selected);
      if (!title) {
        setTitle(selected.name.replace(/\.[^/.]+$/, ''));
      }
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setErrorMsg('Lütfen yüklenecek bir dosya seçiniz.');
      return;
    }

    setLoading(true);
    setErrorMsg(null);

    const idempotencyKey = `doc-upload-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
    const formData = new FormData();
    formData.append('file', file);

    try {
      if (existingDocumentId) {
        await documentService.addVersion(existingDocumentId, formData, idempotencyKey, authenticatedFetch);
      } else {
        formData.append('title', title || file.name);
        formData.append('documentType', documentType);
        if (documentNumber) formData.append('documentNumber', documentNumber);
        if (documentDate) formData.append('documentDate', documentDate);
        if (expiryDate) formData.append('expiryDate', expiryDate);
        if (notes) formData.append('notes', notes);

        if (scopeType === 'ImportCase' && scopeId) {
          await documentService.uploadCaseDocument(scopeId, formData, idempotencyKey, authenticatedFetch);
        } else if (scopeType === 'Shipment' && scopeId) {
          await documentService.uploadShipmentDocument(scopeId, formData, idempotencyKey, authenticatedFetch);
        } else if (scopeType === 'Container' && scopeId) {
          await documentService.uploadContainerDocument(scopeId, formData, idempotencyKey, authenticatedFetch);
        } else {
          throw new Error('Geçerli bir evrak kapsama alanı seçilmedi.');
        }
      }

      onSuccess();
      onClose();
    } catch (err: any) {
      setErrorMsg(err.message || 'Evrak yüklenirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-container" style={{ maxWidth: '580px' }}>
        <div className="modal-header">
          <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)' }}>
            {existingDocumentId ? '➕ Yeni Evrak Versiyonu Yükle' : '📁 Yeni İthalat Evrakı Yükle'}
          </h3>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: '1.4rem', cursor: 'pointer' }}>&times;</button>
        </div>

        <div className="modal-body">
          {errorMsg && (
            <div style={{ padding: '0.85rem 1rem', background: 'rgba(244, 63, 94, 0.1)', border: '1px solid rgba(244, 63, 94, 0.3)', borderRadius: '8px', color: 'var(--accent-rose)', fontSize: '0.85rem', marginBottom: '1rem' }}>
              ⚠️ {errorMsg}
            </div>
          )}

          <form id="upload-doc-form" onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            <div className="form-group" style={{ marginBottom: 0 }}>
              <label className="form-label">Dosya Seçiniz (Max 25 MB) *</label>
              <input
                type="file"
                required
                accept=".pdf,.docx,.xlsx,.png,.jpg,.jpeg"
                onChange={handleFileChange}
                className="form-input"
                style={{ width: '100%', padding: '0.5rem' }}
              />
              <span style={{ fontSize: '0.72rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                Desteklenen formatlar: PDF, DOCX, XLSX, PNG, JPG/JPEG.
              </span>
            </div>

            {!existingDocumentId && (
              <>
                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label">Evrak Başlığı *</label>
                  <input
                    type="text"
                    required
                    placeholder="Örn: 2026 Ticari Fatura - INV9901"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    className="form-input"
                    style={{ width: '100%' }}
                  />
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Evrak Türü *</label>
                    <select
                      value={documentType}
                      onChange={(e) => setDocumentType(e.target.value)}
                      className="form-input"
                      style={{ width: '100%' }}
                    >
                      <option value="CommercialInvoice">Commercial Invoice (Ticari Fatura)</option>
                      <option value="ProformaInvoice">Proforma Invoice</option>
                      <option value="PackingList">Packing List (Çeki Listesi)</option>
                      <option value="BillOfLading">Bill of Lading (Konşimento)</option>
                      <option value="SeaWaybill">Sea Waybill</option>
                      <option value="AirWaybill">Air Waybill (AWB)</option>
                      <option value="CMR">CMR (Kara Taşıma Senedi)</option>
                      <option value="CertificateOfOrigin">Certificate of Origin (Menşe)</option>
                      <option value="ATR">A.TR Dolaşım Belgesi</option>
                      <option value="EUR1">EUR.1 Dolaşım Belgesi</option>
                      <option value="InsuranceCertificate">Sigorta Poliçesi</option>
                      <option value="CustomsDeclaration">Gümrük Beyannamesi</option>
                      <option value="MSDS">MSDS / SDS</option>
                      <option value="Other">Diğer Belge</option>
                    </select>
                  </div>

                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Evrak / Fatura No</label>
                    <input
                      type="text"
                      placeholder="Örn: INV-2026-9901"
                      value={documentNumber}
                      onChange={(e) => setDocumentNumber(e.target.value)}
                      className="form-input"
                      style={{ width: '100%' }}
                    />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Evrak Tarihi</label>
                    <input
                      type="date"
                      value={documentDate}
                      onChange={(e) => setDocumentDate(e.target.value)}
                      className="form-input"
                      style={{ width: '100%' }}
                    />
                  </div>

                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Geçerlilik Bitiş Tarihi</label>
                    <input
                      type="date"
                      value={expiryDate}
                      onChange={(e) => setExpiryDate(e.target.value)}
                      className="form-input"
                      style={{ width: '100%' }}
                    />
                  </div>
                </div>

                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label">Notlar</label>
                  <textarea
                    rows={2}
                    placeholder="Evrak hakkında ek notlar..."
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    className="form-input"
                    style={{ width: '100%', resize: 'vertical' }}
                  />
                </div>
              </>
            )}
          </form>
        </div>

        <div className="modal-footer">
          <button onClick={onClose} disabled={loading} className="btn-secondary">Vazgeç</button>
          <button type="submit" form="upload-doc-form" disabled={loading || !file} className="btn-primary">
            {loading ? 'Yükleniyor...' : 'Evrak Yükle'}
          </button>
        </div>
      </div>
    </div>
  );
};
