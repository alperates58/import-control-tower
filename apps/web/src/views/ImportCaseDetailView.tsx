import React, { useEffect, useState } from 'react';
import { ImportCaseDetail, AvailablePurchaseOrderLine, ShipmentDetail } from '../types/importCase';
import { DocumentSummary } from '../types/document';
import { importCaseService } from '../services/importCaseService';
import { documentService } from '../services/documentService';
import { ContainerManagementPanel } from '../components/import-cases/ContainerManagementPanel';
import { MilestoneTimeline } from '../components/import-cases/MilestoneTimeline';
import { DocumentChecklistWidget } from '../components/documents/DocumentChecklistWidget';
import { DocumentUploadModal } from './DocumentUploadModal';
import { DocumentVersionDrawer } from '../components/documents/DocumentVersionDrawer';
import { useAuth } from '../context/AuthContext';

interface Props {
  caseId: string;
  onBack: () => void;
}

export const ImportCaseDetailView: React.FC<Props> = ({ caseId, onBack }) => {
  const { authenticatedFetch, hasPermission } = useAuth();
  const [detail, setDetail] = useState<ImportCaseDetail | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'orders' | 'shipments' | 'documents'>('overview');
  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Documents
  const [caseDocuments, setCaseDocuments] = useState<DocumentSummary[]>([]);
  const [docUploadModalOpen, setDocUploadModalOpen] = useState(false);
  const [versionDrawerOpen, setVersionDrawerOpen] = useState(false);
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);
  const [selectedDocTitle, setSelectedDocTitle] = useState('');

  // Available PO Lines for Allocation
  const [availablePoLines, setAvailablePoLines] = useState<AvailablePurchaseOrderLine[]>([]);
  const [selectedPoLineId, setSelectedPoLineId] = useState('');
  const [allocateQty, setAllocateQty] = useState('');
  const [allocateLoading, setAllocateLoading] = useState(false);

  // New Shipment State
  const [createShipmentModalOpen, setCreateShipmentModalOpen] = useState(false);
  const [transportMode, setTransportMode] = useState('Sea');
  const [originLocation, setOriginLocation] = useState('');
  const [destinationLocation, setDestinationLocation] = useState('');
  const [originTz] = useState('Asia/Shanghai');
  const [destTz] = useState('Europe/Istanbul');
  const [etd] = useState('');
  const [eta] = useState('');
  const [shipmentLoading, setShipmentLoading] = useState(false);

  // Active Shipment Detail
  const [activeShipmentId, setActiveShipmentId] = useState<string | null>(null);
  const [activeShipmentDetail, setActiveShipmentDetail] = useState<ShipmentDetail | null>(null);

  // Abort Modal State
  const [abortModalOpen, setAbortModalOpen] = useState(false);
  const [abortReason, setAbortReason] = useState('');
  const [abortShipmentId, setAbortShipmentId] = useState<string | null>(null);

  const fetchDetail = async () => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const data = await importCaseService.getCaseById(caseId, authenticatedFetch);
      setDetail(data);
      if (data.shipments.length > 0 && !activeShipmentId) {
        setActiveShipmentId(data.shipments[0].id);
      }

      const available = await importCaseService.getAvailablePurchaseOrders(caseId, undefined, authenticatedFetch);
      setAvailablePoLines(available);

      const docs = await documentService.getDocuments({ importCaseId: caseId }, authenticatedFetch);
      setCaseDocuments(docs);
    } catch (err: any) {
      setErrorMsg(err.message || 'Dosya detayı yüklenemedi.');
    } finally {
      setLoading(false);
    }
  };

  const fetchActiveShipment = async (sId: string) => {
    try {
      const data = await importCaseService.getShipmentById(sId, authenticatedFetch);
      setActiveShipmentDetail(data);
    } catch {
      setActiveShipmentDetail(null);
    }
  };

  useEffect(() => {
    fetchDetail();
  }, [caseId]);

  useEffect(() => {
    if (activeShipmentId) {
      fetchActiveShipment(activeShipmentId);
    } else {
      setActiveShipmentDetail(null);
    }
  }, [activeShipmentId]);

  const refreshAll = async () => {
    await fetchDetail();
    if (activeShipmentId) {
      await fetchActiveShipment(activeShipmentId);
    }
  };

  if (loading) {
    return (
      <div style={{ padding: '4rem', textAlign: 'center', color: 'var(--accent-blue)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '1rem' }}>
        <div style={{ width: '36px', height: '36px', border: '3px solid var(--border-color)', borderTopColor: 'var(--accent-blue)', borderRadius: '50%', animation: 'spin 1s linear infinite' }}></div>
        <div style={{ fontWeight: 600 }}>İthalat dosyası yükleniyor...</div>
      </div>
    );
  }

  if (errorMsg || !detail) {
    return (
      <div style={{ padding: '1.5rem', background: 'rgba(244, 63, 94, 0.1)', border: '1px solid rgba(244, 63, 94, 0.3)', borderRadius: '10px', color: 'var(--accent-rose)' }}>
        ⚠️ {errorMsg || 'Dosya bulunamadı.'}
        <div style={{ marginTop: '1rem' }}>
          <button onClick={onBack} className="btn-secondary btn-sm">Listeye Dön</button>
        </div>
      </div>
    );
  }

  const handleAllocateLine = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPoLineId || !allocateQty) return;
    setAllocateLoading(true);
    try {
      await importCaseService.allocateOrderLine(caseId, {
        purchaseOrderLineId: selectedPoLineId,
        allocatedQuantity: parseFloat(allocateQty)
      }, authenticatedFetch);
      setSelectedPoLineId('');
      setAllocateQty('');
      await fetchDetail();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setAllocateLoading(false);
    }
  };

  const handleCreateShipment = async (e: React.FormEvent) => {
    e.preventDefault();
    setShipmentLoading(true);
    const idempotencyKey = `shipment-create-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;

    try {
      const created = await importCaseService.createShipment(caseId, {
        transportMode,
        originLocation,
        destinationLocation,
        originTimezoneId: originTz,
        destinationTimezoneId: destTz,
        etd: etd || null,
        eta: eta || null
      }, idempotencyKey, authenticatedFetch);

      setCreateShipmentModalOpen(false);
      await fetchDetail();
      setActiveShipmentId(created.id);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setShipmentLoading(false);
    }
  };

  const handleConfirmAbort = async () => {
    if (!abortShipmentId || abortReason.length < 10) {
      alert('Abort gerekçesi en az 10 karakter olmalıdır.');
      return;
    }

    if (!activeShipmentDetail) return;

    try {
      await importCaseService.abortShipment(abortShipmentId, abortReason, activeShipmentDetail.rowVersion, authenticatedFetch);
      setAbortModalOpen(false);
      setAbortReason('');
      setAbortShipmentId(null);
      await refreshAll();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const handleDownloadDoc = async (docId: string) => {
    try {
      const res = await documentService.getDownloadUrl(docId, undefined, authenticatedFetch);
      window.open(res.downloadUrl, '_blank');
    } catch (err: any) {
      alert(err.message);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Header Bar */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <button onClick={onBack} className="btn-secondary btn-sm">
            ← Listeye Dön
          </button>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <h1 style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--accent-blue)', fontFamily: 'var(--font-mono)' }}>
                {detail.caseNumber}
              </h1>
              <span className="badge badge-cyan">{detail.status}</span>
              <span className="badge badge-emerald">{detail.supplierName}</span>
            </div>
            <p style={{ fontSize: '0.9rem', fontWeight: 600, color: 'var(--text-main)', marginTop: '0.2rem' }}>
              {detail.title}
            </p>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          {detail.status !== 'Closed' && detail.status !== 'Cancelled' && (
            <button
              onClick={async () => {
                if (confirm('İthalat dosyasını kapatmak istediğinize emin misiniz?')) {
                  try {
                    await importCaseService.closeCase(detail.id, authenticatedFetch);
                    await fetchDetail();
                  } catch (err: any) { alert(err.message); }
                }
              }}
              className="btn-secondary btn-sm"
              style={{ color: 'var(--accent-emerald)', borderColor: 'rgba(16, 185, 129, 0.3)' }}
            >
              🔒 Dosyayı Kapat
            </button>
          )}
        </div>
      </div>

      {/* Detail Tab Navigation */}
      <div style={{ display: 'flex', gap: '0.5rem', borderBottom: '1px solid var(--border-color)', paddingBottom: '0.5rem' }}>
        <button
          onClick={() => setActiveTab('overview')}
          className={`btn-secondary btn-sm ${activeTab === 'overview' ? 'active' : ''}`}
          style={{ background: activeTab === 'overview' ? 'var(--primary)' : 'transparent', color: activeTab === 'overview' ? '#fff' : 'var(--text-muted)' }}
        >
          📋 Genel Bakış
        </button>
        <button
          onClick={() => setActiveTab('orders')}
          className={`btn-secondary btn-sm ${activeTab === 'orders' ? 'active' : ''}`}
          style={{ background: activeTab === 'orders' ? 'var(--primary)' : 'transparent', color: activeTab === 'orders' ? '#fff' : 'var(--text-muted)' }}
        >
          📦 Bağlı Sipariş Kalemleri ({detail.lines.length})
        </button>
        <button
          onClick={() => setActiveTab('shipments')}
          className={`btn-secondary btn-sm ${activeTab === 'shipments' ? 'active' : ''}`}
          style={{ background: activeTab === 'shipments' ? 'var(--primary)' : 'transparent', color: activeTab === 'shipments' ? '#fff' : 'var(--text-muted)' }}
        >
          🚢 Sevkiyatlar & Konteynerler ({detail.shipments.length})
        </button>
        <button
          onClick={() => setActiveTab('documents')}
          className={`btn-secondary btn-sm ${activeTab === 'documents' ? 'active' : ''}`}
          style={{ background: activeTab === 'documents' ? 'var(--primary)' : 'transparent', color: activeTab === 'documents' ? '#fff' : 'var(--text-muted)' }}
        >
          📄 İthalat Evrakları ({caseDocuments.length})
        </button>
      </div>

      {/* Tab 1: Overview */}
      {activeTab === 'overview' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <DocumentChecklistWidget scopeType="ImportCase" scopeId={caseId} />

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '1.25rem' }}>
            <div className="panel" style={{ marginBottom: 0 }}>
              <div className="panel-header">
                <div className="panel-title">Dosya Bilgileri</div>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', fontSize: '0.85rem' }}>
                <div><span style={{ color: 'var(--text-dim)', display: 'block' }}>Tedarikçi</span><strong>{detail.supplierName}</strong></div>
                <div><span style={{ color: 'var(--text-dim)', display: 'block' }}>Varsayılan Mod</span><strong>{detail.defaultTransportMode || '-'}</strong></div>
                <div><span style={{ color: 'var(--text-dim)', display: 'block' }}>Incoterm</span><strong>{detail.incoterm || '-'}</strong></div>
                <div><span style={{ color: 'var(--text-dim)', display: 'block' }}>Menşei Ülke</span><strong>{detail.originCountry || '-'}</strong></div>
                <div><span style={{ color: 'var(--text-dim)', display: 'block' }}>Oluşturulma</span><span>{new Date(detail.createdAtUtc).toLocaleString('tr-TR')}</span></div>
                <div><span style={{ color: 'var(--text-dim)', display: 'block' }}>Notlar</span><span>{detail.notes || '-'}</span></div>
              </div>
            </div>

            <div className="panel" style={{ marginBottom: 0 }}>
              <div className="panel-header">
                <div className="panel-title">Sevkiyat & Evrak Özet Kartı</div>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                <div className="kpi-card" style={{ padding: '1rem' }}>
                  <span className="kpi-title">Toplam Sipariş Kalemi</span>
                  <div className="kpi-value">{detail.lines.length}</div>
                </div>
                <div className="kpi-card" style={{ padding: '1rem' }}>
                  <span className="kpi-title">Evrak Sayısı</span>
                  <div className="kpi-value" style={{ color: 'var(--accent-blue)' }}>{caseDocuments.length}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab 2: Orders Allocation */}
      {activeTab === 'orders' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          {detail.status !== 'Closed' && detail.status !== 'Cancelled' && (
            <div className="panel" style={{ background: 'rgba(15, 23, 42, 0.5)', marginBottom: 0 }}>
              <div className="panel-header" style={{ marginBottom: '1rem', paddingBottom: '0.75rem' }}>
                <div className="panel-title" style={{ fontSize: '0.95rem' }}>
                  <span>➕ Satın Alma Sipariş Kalemi Atama / Tahsis</span>
                </div>
              </div>

              <form onSubmit={handleAllocateLine} style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', alignItems: 'flex-end' }}>
                <div style={{ gridColumn: 'span 2' }}>
                  <label className="form-label">Sipariş Kalemi Seçiniz *</label>
                  <select
                    required
                    value={selectedPoLineId}
                    onChange={(e) => setSelectedPoLineId(e.target.value)}
                    className="form-input"
                    style={{ width: '100%' }}
                  >
                    <option value="">-- Atanabilir Sipariş Kalemi Seçiniz --</option>
                    {availablePoLines.map(po => (
                      <option key={po.purchaseOrderLineId} value={po.purchaseOrderLineId}>
                        {po.orderNumber} | {po.stockCode} - {po.stockName} (Bakiye: {po.remainingQuantity})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="form-label">Tahsis Edilecek Miktar *</label>
                  <input
                    type="number"
                    step="0.0001"
                    required
                    placeholder="Miktar giriniz"
                    value={allocateQty}
                    onChange={(e) => setAllocateQty(e.target.value)}
                    className="form-input"
                    style={{ width: '100%' }}
                  />
                </div>

                <div>
                  <button
                    disabled={allocateLoading || !selectedPoLineId || !allocateQty}
                    type="submit"
                    className="btn-primary"
                    style={{ width: '100%', justifyContent: 'center' }}
                  >
                    {allocateLoading ? 'Tahsis Ediliyor...' : '+ Kalem Tahsis Et'}
                  </button>
                </div>
              </form>
            </div>
          )}

          <div className="panel">
            <div className="panel-header">
              <div className="panel-title">Dosyadaki Sipariş Kalemleri</div>
            </div>

            {detail.lines.length === 0 ? (
              <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                Henüz bu ithalat dosyasına sipariş kalemi tahsis edilmedi.
              </div>
            ) : (
              <div className="data-table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Sipariş No</th>
                      <th>Stok Kodu</th>
                      <th>Stok Adı</th>
                      <th>Tahsis Miktarı</th>
                      <th>Serbest (Released)</th>
                      <th>Etkin Miktar</th>
                      <th>Sevk Edilen</th>
                      <th>Teslim Alınan</th>
                      <th>Durum</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.lines.map((l) => (
                      <tr key={l.id}>
                        <td style={{ fontFamily: 'var(--font-mono)', color: 'var(--accent-blue)', fontWeight: 600 }}>{l.orderNumber}</td>
                        <td style={{ fontFamily: 'var(--font-mono)' }}>{l.stockCode}</td>
                        <td style={{ fontWeight: 600 }}>{l.stockName}</td>
                        <td>{l.allocatedQuantity.toLocaleString()}</td>
                        <td style={{ color: 'var(--accent-amber)' }}>{l.releasedQuantity.toLocaleString()}</td>
                        <td style={{ fontWeight: 700 }}>{l.effectiveAllocatedQuantity.toLocaleString()}</td>
                        <td style={{ color: 'var(--accent-emerald)' }}>{l.shippedQuantity.toLocaleString()}</td>
                        <td style={{ color: 'var(--accent-purple)' }}>{l.receivedQuantity.toLocaleString()}</td>
                        <td><span className="badge badge-cyan">{l.status}</span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Tab 3: Shipments */}
      {activeTab === 'shipments' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: '1rem', fontWeight: 700, color: 'var(--text-main)' }}>
              Sevkiyat Yönetimi
            </div>
            {detail.status !== 'Closed' && detail.status !== 'Cancelled' && (
              <button onClick={() => setCreateShipmentModalOpen(true)} className="btn-primary btn-sm">
                + Yeni Sevkiyat Oluştur
              </button>
            )}
          </div>

          {detail.shipments.length === 0 ? (
            <div className="panel" style={{ textAlign: 'center', padding: '3rem 2rem', color: 'var(--text-muted)' }}>
              <div style={{ fontSize: '2rem', marginBottom: '0.5rem' }}>🚢</div>
              <h3 style={{ fontWeight: 700, color: 'var(--text-main)' }}>Sevkiyat Bulunmuyor</h3>
              <p style={{ fontSize: '0.85rem', marginBottom: '1rem' }}>Bu ithalat dosyası için henüz bir sevkiyat oluşturulmamıştır.</p>
              <button onClick={() => setCreateShipmentModalOpen(true)} className="btn-primary btn-sm">+ Sevkiyat Oluştur</button>
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: '280px 1fr', gap: '1.25rem' }}>
              <div className="panel" style={{ padding: '1rem', marginBottom: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <div style={{ fontSize: '0.78rem', fontWeight: 700, color: 'var(--text-dim)', textTransform: 'uppercase', marginBottom: '0.5rem' }}>Sevkiyat Listesi</div>
                {detail.shipments.map((s) => (
                  <button
                    key={s.id}
                    onClick={() => setActiveShipmentId(s.id)}
                    className="btn-secondary"
                    style={{
                      width: '100%',
                      justifyContent: 'space-between',
                      background: activeShipmentId === s.id ? 'var(--primary)' : 'rgba(30, 41, 59, 0.4)',
                      borderColor: activeShipmentId === s.id ? 'var(--accent-blue)' : 'var(--border-color)',
                      color: '#fff'
                    }}
                  >
                    <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 700 }}>{s.shipmentNumber}</span>
                    <span className="badge badge-sm" style={{ background: 'rgba(0,0,0,0.3)', color: '#fff' }}>{s.transportMode}</span>
                  </button>
                ))}
              </div>

              {activeShipmentDetail && (
                <div className="panel" style={{ marginBottom: 0 }}>
                  <div className="panel-header">
                    <div>
                      <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--accent-cyan)', fontFamily: 'var(--font-mono)' }}>
                        {activeShipmentDetail.shipmentNumber}
                      </h3>
                      <div style={{ fontSize: '0.82rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>
                        {activeShipmentDetail.originLocation} ➔ {activeShipmentDetail.destinationLocation} ({activeShipmentDetail.transportMode})
                      </div>
                    </div>

                    {activeShipmentDetail.status !== 'Delivered' && activeShipmentDetail.status !== 'Aborted' && activeShipmentDetail.status !== 'Cancelled' && (
                      <button
                        onClick={() => {
                          setAbortShipmentId(activeShipmentDetail.id);
                          setAbortModalOpen(true);
                        }}
                        className="btn-secondary btn-sm"
                        style={{ color: 'var(--accent-rose)', borderColor: 'rgba(244, 63, 94, 0.3)' }}
                      >
                        ⚠️ Sevkiyatı Abort Et
                      </button>
                    )}
                  </div>

                  <div style={{ marginTop: '1rem' }}>
                    <DocumentChecklistWidget scopeType="Shipment" scopeId={activeShipmentDetail.id} />
                  </div>

                  <div style={{ marginTop: '1.5rem' }}>
                    <ContainerManagementPanel
                      shipmentId={activeShipmentDetail.id}
                      transportMode={activeShipmentDetail.transportMode}
                      containers={activeShipmentDetail.containers || []}
                      onRefresh={refreshAll}
                    />
                  </div>

                  <div style={{ marginTop: '2rem' }}>
                    <MilestoneTimeline
                      shipmentId={activeShipmentDetail.id}
                      milestones={activeShipmentDetail.milestones || []}
                      onRefresh={refreshAll}
                    />
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* Tab 4: Documents */}
      {activeTab === 'documents' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: '1rem', fontWeight: 700, color: 'var(--text-main)' }}>
              İthalat Dosyasına Bağlı Evraklar
            </div>
            {hasPermission('documents.upload') && (
              <button onClick={() => setDocUploadModalOpen(true)} className="btn-primary btn-sm">
                + Yeni Evrak Yükle
              </button>
            )}
          </div>

          <DocumentChecklistWidget scopeType="ImportCase" scopeId={caseId} />

          <div className="panel">
            {caseDocuments.length === 0 ? (
              <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                <div style={{ fontSize: '2rem', marginBottom: '0.5rem' }}>📁</div>
                <h3>Henüz evrak yüklenmedi</h3>
                <p style={{ fontSize: '0.85rem', marginBottom: '1rem' }}>Bu ithalat dosyasına ait henüz bir belge bulunmamaktadır.</p>
                {hasPermission('documents.upload') && (
                  <button onClick={() => setDocUploadModalOpen(true)} className="btn-primary btn-sm">+ İlk Evrakı Yükle</button>
                )}
              </div>
            ) : (
              <div className="data-table-wrapper">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Evrak Başlığı</th>
                      <th>Tür</th>
                      <th>No</th>
                      <th>Aktif Sürüm</th>
                      <th>Boyut</th>
                      <th>Yüklenme</th>
                      <th>Durum</th>
                      <th>İşlemler</th>
                    </tr>
                  </thead>
                  <tbody>
                    {caseDocuments.map((d) => (
                      <tr key={d.id}>
                        <td style={{ fontWeight: 600 }}>📄 {d.title}</td>
                        <td><span className="badge badge-cyan">{d.documentType}</span></td>
                        <td style={{ fontFamily: 'var(--font-mono)' }}>{d.documentNumber || '-'}</td>
                        <td>
                          {d.currentVersion ? (
                            <span className="badge badge-emerald" style={{ fontFamily: 'var(--font-mono)' }}>
                              v{d.currentVersion.versionNumber} ({d.currentVersion.fileExtension})
                            </span>
                          ) : '-'}
                        </td>
                        <td style={{ fontSize: '0.8rem' }}>
                          {d.currentVersion ? `${(d.currentVersion.fileSizeBytes / 1024).toFixed(1)} KB` : '-'}
                        </td>
                        <td style={{ fontSize: '0.8rem' }}>
                          {new Date(d.createdAtUtc).toLocaleDateString('tr-TR')}
                        </td>
                        <td>
                          {d.status === 'Active' ? <span className="badge badge-emerald">Aktif</span> : <span className="badge badge-rose">İptal Edildi</span>}
                        </td>
                        <td>
                          <div style={{ display: 'flex', gap: '0.4rem' }}>
                            {d.status === 'Active' && (
                              <button onClick={() => handleDownloadDoc(d.id)} className="btn-secondary btn-sm">📥 İndir</button>
                            )}
                            <button
                              onClick={() => {
                                setSelectedDocId(d.id);
                                setSelectedDocTitle(d.title);
                                setVersionDrawerOpen(true);
                              }}
                              className="btn-secondary btn-sm"
                            >
                              📜 Geçmiş
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Upload Modal */}
      <DocumentUploadModal
        isOpen={docUploadModalOpen}
        onClose={() => setDocUploadModalOpen(false)}
        onSuccess={fetchDetail}
        scopeType="ImportCase"
        scopeId={caseId}
      />

      {/* Version Drawer */}
      {selectedDocId && (
        <DocumentVersionDrawer
          documentId={selectedDocId}
          documentTitle={selectedDocTitle}
          isOpen={versionDrawerOpen}
          onClose={() => setVersionDrawerOpen(false)}
        />
      )}

      {/* Create Shipment Modal */}
      {createShipmentModalOpen && (
        <div className="modal-overlay">
          <div className="modal-container" style={{ maxWidth: '580px' }}>
            <div className="modal-header">
              <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)' }}>Yeni Sevkiyat Oluştur</h3>
              <button onClick={() => setCreateShipmentModalOpen(false)} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: '1.4rem' }}>&times;</button>
            </div>
            <div className="modal-body">
              <form id="create-shipment-form" onSubmit={handleCreateShipment} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                <div className="form-group" style={{ marginBottom: 0 }}>
                  <label className="form-label">Taşıma Modu *</label>
                  <select value={transportMode} onChange={(e) => setTransportMode(e.target.value)} className="form-input" style={{ width: '100%' }}>
                    <option value="Sea">Deniz (Sea)</option>
                    <option value="Air">Hava (Air)</option>
                    <option value="Road">Kara (Road)</option>
                    <option value="Rail">Demiryolu (Rail)</option>
                    <option value="Courier">Kurye (Courier)</option>
                    <option value="Multimodal">Multimodal</option>
                  </select>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Çıkış Lokasyonu *</label>
                    <input type="text" required placeholder="Örn: Ningbo Port" value={originLocation} onChange={(e) => setOriginLocation(e.target.value)} className="form-input" style={{ width: '100%' }} />
                  </div>
                  <div className="form-group" style={{ marginBottom: 0 }}>
                    <label className="form-label">Varış Lokasyonu *</label>
                    <input type="text" required placeholder="Örn: Ambarlı Limanı" value={destinationLocation} onChange={(e) => setDestinationLocation(e.target.value)} className="form-input" style={{ width: '100%' }} />
                  </div>
                </div>
              </form>
            </div>
            <div className="modal-footer">
              <button onClick={() => setCreateShipmentModalOpen(false)} className="btn-secondary">Vazgeç</button>
              <button type="submit" form="create-shipment-form" disabled={shipmentLoading} className="btn-primary">
                {shipmentLoading ? 'Oluşturuluyor...' : 'Sevkiyat Oluştur'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Abort Modal */}
      {abortModalOpen && (
        <div className="modal-overlay">
          <div className="modal-container" style={{ maxWidth: '500px' }}>
            <div className="modal-header">
              <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--accent-rose)' }}>⚠️ Sevkiyat Abort İptal Onayı</h3>
              <button onClick={() => setAbortModalOpen(false)} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', fontSize: '1.4rem' }}>&times;</button>
            </div>
            <div className="modal-body">
              <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginBottom: '1rem' }}>
                Sevkiyatı Abort ettiğinizde sevk edilmemiş bakiye serbest bırakılacak ve sevkiyat sonlandırılacaktır.
              </p>
              <div className="form-group">
                <label className="form-label">Zorunlu Abort Gerekçesi (Min 10 Karakter) *</label>
                <textarea
                  rows={3}
                  required
                  placeholder="Sevkiyatın sonlandırılma gerekçesini yazınız..."
                  value={abortReason}
                  onChange={(e) => setAbortReason(e.target.value)}
                  className="form-input"
                  style={{ width: '100%' }}
                />
              </div>
            </div>
            <div className="modal-footer">
              <button onClick={() => setAbortModalOpen(false)} className="btn-secondary">Vazgeç</button>
              <button
                disabled={abortReason.length < 10}
                onClick={handleConfirmAbort}
                className="btn-primary"
                style={{ background: 'var(--accent-rose)', color: '#fff' }}
              >
                Abort Et ve Serbest Bırak
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
