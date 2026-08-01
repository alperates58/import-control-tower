import React, { useState } from 'react';
import { ShipmentContainer } from '../../types/importCase';
import { importCaseService } from '../../services/importCaseService';
import { useAuth } from '../../context/AuthContext';

interface Props {
  shipmentId: string;
  transportMode: string;
  containers: ShipmentContainer[];
  onRefresh: () => void;
}

export const ContainerManagementPanel: React.FC<Props> = ({
  shipmentId,
  transportMode,
  containers,
  onRefresh
}) => {
  const { authenticatedFetch } = useAuth();
  const [containerNumber, setContainerNumber] = useState('');
  const [containerType, setContainerType] = useState('40HC');
  const [sealNumber, setSealNumber] = useState('');
  const [grossWeightKg, setGrossWeightKg] = useState('');
  const [netWeightKg, setNetWeightKg] = useState('');
  const [packageCount, setPackageCount] = useState('');
  const [notes, setNotes] = useState('');

  const [overrideModalOpen, setOverrideModalOpen] = useState(false);
  const [overrideReason, setOverrideReason] = useState('');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const isSeaOrMultimodal = transportMode === 'Sea' || transportMode === 'Multimodal';

  const handleAddContainer = async (override: boolean = false) => {
    setErrorMsg(null);
    setLoading(true);

    try {
      await importCaseService.addContainer(shipmentId, {
        containerNumber,
        containerType,
        sealNumber: sealNumber || null,
        grossWeightKg: grossWeightKg ? parseFloat(grossWeightKg) : null,
        netWeightKg: netWeightKg ? parseFloat(netWeightKg) : null,
        packageCount: packageCount ? parseInt(packageCount) : null,
        overrideCheckDigit: override,
        overrideReason: override ? overrideReason : null,
        notes: notes || null
      }, authenticatedFetch);

      setContainerNumber('');
      setSealNumber('');
      setGrossWeightKg('');
      setNetWeightKg('');
      setPackageCount('');
      setNotes('');
      setOverrideModalOpen(false);
      setOverrideReason('');
      onRefresh();
    } catch (err: any) {
      if (err.message.includes('CONTAINER_CHECK_DIGIT_INVALID')) {
        setOverrideModalOpen(true);
      } else {
        setErrorMsg(err.message || 'Konteyner eklenemedi.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleCancelContainer = async (containerId: string) => {
    if (!confirm('Bu konteyneri kaldırmak istediğinize emin misiniz?')) return;
    try {
      await importCaseService.cancelContainer(shipmentId, containerId, authenticatedFetch);
      onRefresh();
    } catch (err: any) {
      alert(err.message);
    }
  };

  if (!isSeaOrMultimodal) {
    return (
      <div style={{ padding: '1rem 1.25rem', background: 'rgba(245, 158, 11, 0.1)', border: '1px solid rgba(245, 158, 11, 0.3)', borderRadius: '10px', color: 'var(--accent-amber)', fontSize: '0.88rem' }}>
        ⚠️ Konteyner ekleme yalnızca Deniz (Sea) veya Multimodal taşımalarda geçerlidir. ({transportMode} taşımasında konteyner kullanılamaz).
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
      {errorMsg && (
        <div style={{ padding: '0.85rem 1rem', background: 'rgba(244, 63, 94, 0.1)', border: '1px solid rgba(244, 63, 94, 0.3)', borderRadius: '8px', color: 'var(--accent-rose)', fontSize: '0.85rem' }}>
          ⚠️ {errorMsg}
        </div>
      )}

      {/* Add Container Form Card */}
      <div className="panel" style={{ background: 'rgba(15, 23, 42, 0.5)', padding: '1.25rem', marginBottom: 0 }}>
        <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span>📦</span> Yeni Konteyner Ekle (ISO 6346 Doğrulamalı)
        </h4>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1rem', alignItems: 'flex-end' }}>
          <div>
            <label className="form-label">Konteyner No *</label>
            <input
              type="text"
              placeholder="Örn: CSQU3054383"
              value={containerNumber}
              onChange={(e) => setContainerNumber(e.target.value.toUpperCase())}
              className="form-input"
              style={{ width: '100%', fontFamily: 'var(--font-mono)' }}
            />
          </div>

          <div>
            <label className="form-label">Konteyner Tipi *</label>
            <select
              value={containerType}
              onChange={(e) => setContainerType(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="20DV">20DV (Standard)</option>
              <option value="40DV">40DV (Standard)</option>
              <option value="40HC">40HC (High Cube)</option>
              <option value="45HC">45HC (High Cube)</option>
              <option value="20RF">20RF (Reefer)</option>
              <option value="40RF">40RF (Reefer)</option>
              <option value="20OT">20OT (Open Top)</option>
              <option value="40OT">40OT (Open Top)</option>
              <option value="20FR">20FR (Flat Rack)</option>
              <option value="40FR">40FR (Flat Rack)</option>
            </select>
          </div>

          <div>
            <label className="form-label">Mühür No (Seal No)</label>
            <input
              type="text"
              placeholder="Örn: SEAL-998877"
              value={sealNumber}
              onChange={(e) => setSealNumber(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <label className="form-label">Brüt Ağırlık (KG)</label>
            <input
              type="number"
              placeholder="Örn: 22000"
              value={grossWeightKg}
              onChange={(e) => setGrossWeightKg(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <button
              disabled={loading || !containerNumber}
              onClick={() => handleAddContainer(false)}
              className="btn-primary"
              style={{ width: '100%', justifyContent: 'center' }}
            >
              {loading ? 'Ekleniyor...' : '+ Konteyner Ekle'}
            </button>
          </div>
        </div>
      </div>

      {/* Containers List Table */}
      {containers.length === 0 ? (
        <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)', fontSize: '0.88rem' }}>
          Henüz bu sevkiyata bağlı konteyner bulunmuyor.
        </div>
      ) : (
        <div className="data-table-wrapper">
          <table className="data-table">
            <thead>
              <tr>
                <th>Konteyner No</th>
                <th>Tip</th>
                <th>Mühür No</th>
                <th>Brüt Ağırlık</th>
                <th>Durum</th>
                <th>İşlem</th>
              </tr>
            </thead>
            <tbody>
              {containers.map((c) => (
                <tr key={c.id}>
                  <td style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, color: 'var(--accent-cyan)' }}>
                    {c.normalizedContainerNumber}
                  </td>
                  <td><span className="badge badge-cyan">{c.containerType}</span></td>
                  <td>{c.sealNumber || '-'}</td>
                  <td>{c.grossWeightKg ? `${c.grossWeightKg.toLocaleString()} kg` : '-'}</td>
                  <td>
                    {c.status === 'Active' ? (
                      <span className="badge badge-emerald">Aktif</span>
                    ) : (
                      <span className="badge badge-rose">İptal Edildi</span>
                    )}
                  </td>
                  <td>
                    {c.status === 'Active' && (
                      <button
                        onClick={() => handleCancelContainer(c.id)}
                        className="btn-secondary btn-sm"
                        style={{ color: 'var(--accent-rose)', borderColor: 'rgba(244, 63, 94, 0.3)' }}
                      >
                        Kaldır
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Check Digit Override Modal */}
      {overrideModalOpen && (
        <div className="modal-overlay">
          <div className="modal-container" style={{ maxWidth: '500px' }}>
            <div className="modal-header">
              <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--accent-amber)' }}>
                ⚠️ ISO 6346 Check Digit Uyarısı
              </h3>
              <button onClick={() => setOverrideModalOpen(false)} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: '1.4rem' }}>&times;</button>
            </div>
            <div className="modal-body">
              <p style={{ fontSize: '0.88rem', color: 'var(--text-main)', marginBottom: '1rem', lineHeight: 1.5 }}>
                Girdiğiniz <strong>{containerNumber}</strong> konteyner numarası ISO 6346 standart kontrol basamağı algoritmasını geçemedi. Kontrol basamağını atlayıp (Override) yine de eklemek istiyorsanız bir audit gerekçesi belirtiniz.
              </p>
              <div className="form-group">
                <label className="form-label">Audit Geçersiz Kılma Gerekçesi *</label>
                <textarea
                  rows={3}
                  required
                  placeholder="Örn: Taşıyıcı konşimentosunda geçen hatalı numara doğrulandı..."
                  value={overrideReason}
                  onChange={(e) => setOverrideReason(e.target.value)}
                  className="form-input"
                  style={{ width: '100%' }}
                />
              </div>
            </div>
            <div className="modal-footer">
              <button onClick={() => setOverrideModalOpen(false)} className="btn-secondary">İptal</button>
              <button
                disabled={!overrideReason.trim()}
                onClick={() => handleAddContainer(true)}
                className="btn-primary"
                style={{ background: 'var(--accent-amber)', color: '#000' }}
              >
                Gerekçeli Onayla ve Ekle
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
