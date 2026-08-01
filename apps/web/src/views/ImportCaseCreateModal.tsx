import React, { useState, useEffect } from 'react';
import { SupplierLookup } from '../types/importCase';
import { importCaseService } from '../services/importCaseService';
import { useAuth } from '../context/AuthContext';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (caseId: string) => void;
}

export const ImportCaseCreateModal: React.FC<Props> = ({ isOpen, onClose, onSuccess }) => {
  const { authenticatedFetch } = useAuth();
  const [title, setTitle] = useState('');
  const [supplierName, setSupplierName] = useState('');
  const [defaultTransportMode, setDefaultTransportMode] = useState('Sea');
  const [originCountry, setOriginCountry] = useState('Çin');
  const [incoterm, setIncoterm] = useState('FOB');
  const [estimatedProductionCompletionDate, setEstimatedProductionCompletionDate] = useState('');
  const [notes, setNotes] = useState('');

  const [suppliers, setSuppliers] = useState<SupplierLookup[]>([]);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
      importCaseService.getAvailableSuppliers(undefined, authenticatedFetch).then(setSuppliers).catch(() => {});
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, authenticatedFetch]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);

    if (!title.trim() || !supplierName) {
      setErrorMsg('Başlık ve Tedarikçi seçimi zorunludur.');
      return;
    }

    setLoading(true);
    const idempotencyKey = `case-create-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;

    try {
      const created = await importCaseService.createCase({
        title: title.trim(),
        supplierName,
        defaultTransportMode,
        originCountry: originCountry || null,
        incoterm: incoterm || null,
        estimatedProductionCompletionDate: estimatedProductionCompletionDate || null,
        notes: notes || null
      }, idempotencyKey, authenticatedFetch);

      onSuccess(created.id);
    } catch (err: any) {
      setErrorMsg(err.message || 'İthalat dosyası oluşturulamadı.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      className="modal-overlay"
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div className="modal-container" style={{ maxWidth: '640px' }}>
        {/* Modal Header */}
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
            <span style={{ fontSize: '1.2rem' }}>📁</span>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--text-main)' }}>
              Yeni İthalat Dosyası Oluştur
            </h3>
          </div>
          <button
            onClick={onClose}
            style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: '1.4rem', cursor: 'pointer', padding: '0.2rem 0.5rem', borderRadius: '6px' }}
          >
            &times;
          </button>
        </div>

        {/* Modal Body */}
        <div className="modal-body">
          {errorMsg && (
            <div style={{ padding: '0.85rem 1rem', background: 'rgba(244, 63, 94, 0.1)', border: '1px solid rgba(244, 63, 94, 0.3)', borderRadius: '8px', color: 'var(--accent-rose)', fontSize: '0.85rem', marginBottom: '1.25rem' }}>
              ⚠️ {errorMsg}
            </div>
          )}

          <form id="create-case-form" onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.1rem' }}>
            <div className="form-group" style={{ marginBottom: 0 }}>
              <label className="form-label">Dosya Başlığı *</label>
              <input
                type="text"
                required
                placeholder="Örn: 2026 Q3 Hammadde ve Aksam İthalatı"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="form-input"
                style={{ width: '100%' }}
              />
            </div>

            <div className="form-group" style={{ marginBottom: 0 }}>
              <label className="form-label">Tedarikçi Seçimi * (Single Supplier)</label>
              <select
                required
                value={supplierName}
                onChange={(e) => setSupplierName(e.target.value)}
                className="form-input"
                style={{ width: '100%' }}
              >
                <option value="">-- Tedarikçi Seçiniz --</option>
                {suppliers.map((s) => (
                  <option key={s.normalizedSupplierName} value={s.supplierName}>
                    {s.supplierName} ({s.activeOrderCount} Açık Sipariş)
                  </option>
                ))}
              </select>
              <span style={{ fontSize: '0.72rem', color: 'var(--text-dim)', marginTop: '0.25rem' }}>
                İthalat dosyası kural gereği tek bir tedarikçiye ait siparişleri barındırır.
              </span>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Varsayılan Taşıma Modu</label>
                <select
                  value={defaultTransportMode}
                  onChange={(e) => setDefaultTransportMode(e.target.value)}
                  className="form-input"
                  style={{ width: '100%' }}
                >
                  <option value="Sea">Deniz (Sea)</option>
                  <option value="Air">Hava (Air)</option>
                  <option value="Road">Kara (Road)</option>
                  <option value="Rail">Demiryolu (Rail)</option>
                  <option value="Courier">Kurye (Courier)</option>
                  <option value="Multimodal">Multimodal</option>
                </select>
              </div>

              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Incoterm</label>
                <select
                  value={incoterm}
                  onChange={(e) => setIncoterm(e.target.value)}
                  className="form-input"
                  style={{ width: '100%' }}
                >
                  <option value="FOB">FOB (Free on Board)</option>
                  <option value="EXW">EXW (Ex Works)</option>
                  <option value="CIF">CIF (Cost, Insurance & Freight)</option>
                  <option value="CFR">CFR (Cost and Freight)</option>
                  <option value="DDP">DDP (Delivered Duty Paid)</option>
                  <option value="FCA">FCA (Free Carrier)</option>
                  <option value="DAP">DAP (Delivered at Place)</option>
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Menşei Ülke</label>
                <input
                  type="text"
                  placeholder="Örn: Çin, Almanya, İtalya"
                  value={originCountry}
                  onChange={(e) => setOriginCountry(e.target.value)}
                  className="form-input"
                  style={{ width: '100%' }}
                />
              </div>

              <div className="form-group" style={{ marginBottom: 0 }}>
                <label className="form-label">Tahmini Üretim Bitiş Tarihi</label>
                <input
                  type="date"
                  value={estimatedProductionCompletionDate}
                  onChange={(e) => setEstimatedProductionCompletionDate(e.target.value)}
                  className="form-input"
                  style={{ width: '100%' }}
                />
              </div>
            </div>

            <div className="form-group" style={{ marginBottom: 0 }}>
              <label className="form-label">Notlar & Operasyonel Açıklamalar</label>
              <textarea
                rows={3}
                placeholder="Dosya ile ilgili özel gereksinimler, yükleme notları..."
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                className="form-input"
                style={{ width: '100%', resize: 'vertical' }}
              />
            </div>
          </form>
        </div>

        {/* Modal Footer */}
        <div className="modal-footer">
          <button
            type="button"
            onClick={onClose}
            className="btn-secondary"
            disabled={loading}
          >
            Vazgeç
          </button>
          <button
            type="submit"
            form="create-case-form"
            className="btn-primary"
            disabled={loading}
          >
            {loading ? 'Oluşturuluyor...' : 'Dosya Oluştur'}
          </button>
        </div>
      </div>
    </div>
  );
};
