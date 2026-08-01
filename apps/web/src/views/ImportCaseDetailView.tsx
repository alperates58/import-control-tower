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
import { PageHeader } from '../components/ui/PageHeader';
import { Button, IconButton } from '../components/ui/Button';
import { Input, Select, Textarea, FormField } from '../components/ui/Input';
import { DataTable, Column } from '../components/ui/DataTable';
import { Badge } from '../components/ui/Badge';
import { KPICard, Section, DetailField } from '../components/ui/Card';
import { Modal } from '../components/ui/Modal';
import { Tabs } from '../components/ui/PageHeader';
import { EmptyState, ErrorState, LoadingSkeleton } from '../components/ui/FeedbackState';
import { IconArrowLeft } from '../components/Icons';

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
    return <LoadingSkeleton rows={5} height="60px" />;
  }

  if (errorMsg || !detail) {
    return (
      <ErrorState
        description={errorMsg || 'Dosya bulunamadı.'}
        onRetry={onBack}
      />
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

  const orderLineColumns: Column<any>[] = [
    {
      key: 'orderNumber',
      header: 'Sipariş No',
      render: (l) => <span className="font-mono" style={{ color: 'var(--accent-blue)', fontWeight: 'var(--weight-semibold)' }}>{l.orderNumber}</span>
    },
    {
      key: 'stockCode',
      header: 'Stok Kodu',
      render: (l) => <span className="font-mono">{l.stockCode}</span>
    },
    {
      key: 'stockName',
      header: 'Stok Adı',
      render: (l) => <span style={{ fontWeight: 'var(--weight-semibold)' }}>{l.stockName}</span>
    },
    {
      key: 'allocatedQuantity',
      header: 'Tahsis Miktarı',
      align: 'right',
      render: (l) => l.allocatedQuantity.toLocaleString()
    },
    {
      key: 'releasedQuantity',
      header: 'Serbest (Released)',
      align: 'right',
      render: (l) => <span style={{ color: 'var(--status-warning)' }}>{l.releasedQuantity.toLocaleString()}</span>
    },
    {
      key: 'effectiveAllocatedQuantity',
      header: 'Etkin Miktar',
      align: 'right',
      render: (l) => <strong>{l.effectiveAllocatedQuantity.toLocaleString()}</strong>
    },
    {
      key: 'shippedQuantity',
      header: 'Sevk Edilen',
      align: 'right',
      render: (l) => <span style={{ color: 'var(--status-success)' }}>{l.shippedQuantity.toLocaleString()}</span>
    },
    {
      key: 'receivedQuantity',
      header: 'Teslim Alınan',
      align: 'right',
      render: (l) => <span style={{ color: 'var(--accent-purple)' }}>{l.receivedQuantity.toLocaleString()}</span>
    },
    {
      key: 'status',
      header: 'Durum',
      render: (l) => <Badge variant="cyan">{l.status}</Badge>
    }
  ];

  const docColumns: Column<DocumentSummary>[] = [
    {
      key: 'title',
      header: 'Evrak Başlığı',
      render: (d) => <span style={{ fontWeight: 'var(--weight-semibold)' }}>📄 {d.title}</span>
    },
    {
      key: 'documentType',
      header: 'Tür',
      render: (d) => <Badge variant="cyan">{d.documentType}</Badge>
    },
    {
      key: 'documentNumber',
      header: 'No',
      render: (d) => <span className="font-mono">{d.documentNumber || '-'}</span>
    },
    {
      key: 'currentVersion',
      header: 'Aktif Sürüm',
      render: (d) => (
        d.currentVersion ? (
          <Badge variant="emerald" style={{ fontFamily: 'var(--font-mono)' }}>
            v{d.currentVersion.versionNumber} ({d.currentVersion.fileExtension})
          </Badge>
        ) : '-'
      )
    },
    {
      key: 'fileSizeBytes',
      header: 'Boyut',
      render: (d) => (d.currentVersion ? `${(d.currentVersion.fileSizeBytes / 1024).toFixed(1)} KB` : '-')
    },
    {
      key: 'createdAtUtc',
      header: 'Yüklenme',
      render: (d) => new Date(d.createdAtUtc).toLocaleDateString('tr-TR')
    },
    {
      key: 'status',
      header: 'Durum',
      render: (d) => (d.status === 'Active' ? <Badge variant="emerald">Aktif</Badge> : <Badge variant="rose">İptal Edildi</Badge>)
    },
    {
      key: 'actions',
      header: 'İşlemler',
      align: 'right',
      render: (d) => (
        <div style={{ display: 'flex', gap: '0.3rem', justifyContent: 'flex-end' }}>
          {d.status === 'Active' && (
            <Button variant="secondary" size="sm" onClick={() => handleDownloadDoc(d.id)}>
              📥 İndir
            </Button>
          )}
          <Button
            variant="secondary"
            size="sm"
            onClick={() => {
              setSelectedDocId(d.id);
              setSelectedDocTitle(d.title);
              setVersionDrawerOpen(true);
            }}
          >
            📜 Geçmiş
          </Button>
        </div>
      )
    }
  ];

  return (
    <div>
      <PageHeader
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <IconButton icon={<IconArrowLeft />} onClick={onBack} aria-label="Geri Dön" />
            <span className="font-mono" style={{ color: 'var(--accent-blue)', fontWeight: 'var(--weight-bold)' }}>
              {detail.caseNumber}
            </span>
            <Badge variant="cyan">{detail.status}</Badge>
            <Badge variant="emerald">{detail.supplierName}</Badge>
          </div>
        }
        subtitle={detail.title}
        actions={
          detail.status !== 'Closed' && detail.status !== 'Cancelled' ? (
            <Button
              variant="secondary"
              size="sm"
              onClick={async () => {
                if (confirm('İthalat dosyasını kapatmak istediğinize emin misiniz?')) {
                  try {
                    await importCaseService.closeCase(detail.id, authenticatedFetch);
                    await fetchDetail();
                  } catch (err: any) { alert(err.message); }
                }
              }}
            >
              🔒 Dosyayı Kapat
            </Button>
          ) : undefined
        }
      />

      <Tabs
        tabs={[
          { id: 'overview', label: '📋 Genel Bakış' },
          { id: 'orders', label: `📦 Bağlı Sipariş Kalemleri (${detail.lines.length})` },
          { id: 'shipments', label: `🚢 Sevkiyatlar & Konteynerler (${detail.shipments.length})` },
          { id: 'documents', label: `📄 İthalat Evrakları (${caseDocuments.length})` }
        ]}
        activeTab={activeTab}
        onChange={(id) => setActiveTab(id as any)}
      />

      {/* Tab 1: Overview */}
      {activeTab === 'overview' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-5)' }}>
          <DocumentChecklistWidget scopeType="ImportCase" scopeId={caseId} />

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: 'var(--space-4)' }}>
            <Section title="Dosya Bilgileri">
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 'var(--space-3)' }}>
                <DetailField label="Tedarikçi" value={detail.supplierName} />
                <DetailField label="Varsayılan Mod" value={detail.defaultTransportMode || '-'} />
                <DetailField label="Incoterm" value={detail.incoterm || '-'} isMono />
                <DetailField label="Menşei Ülke" value={detail.originCountry || '-'} />
                <DetailField label="Oluşturulma" value={new Date(detail.createdAtUtc).toLocaleString('tr-TR')} />
                <DetailField label="Notlar" value={detail.notes || '-'} />
              </div>
            </Section>

            <Section title="Sevkiyat & Evrak Özet Kartı">
              <div className="card-grid">
                <KPICard title="Toplam Sipariş Kalemi" value={detail.lines.length} />
                <KPICard title="Evrak Sayısı" value={caseDocuments.length} valueColor="var(--accent-blue)" />
              </div>
            </Section>
          </div>
        </div>
      )}

      {/* Tab 2: Orders Allocation */}
      {activeTab === 'orders' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-5)' }}>
          {detail.status !== 'Closed' && detail.status !== 'Cancelled' && (
            <Section title="➕ Satın Alma Sipariş Kalemi Atama / Tahsis">
              <form onSubmit={handleAllocateLine} style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 'var(--space-4)', alignItems: 'flex-end' }}>
                <div style={{ gridColumn: 'span 2' }}>
                  <FormField label="Sipariş Kalemi Seçiniz *" required>
                    <Select
                      required
                      value={selectedPoLineId}
                      onChange={(e) => setSelectedPoLineId(e.target.value)}
                    >
                      <option value="">-- Atanabilir Sipariş Kalemi Seçiniz --</option>
                      {availablePoLines.map(po => (
                        <option key={po.purchaseOrderLineId} value={po.purchaseOrderLineId}>
                          {po.orderNumber} | {po.stockCode} - {po.stockName} (Bakiye: {po.remainingQuantity})
                        </option>
                      ))}
                    </Select>
                  </FormField>
                </div>

                <FormField label="Tahsis Edilecek Miktar *" required>
                  <Input
                    type="number"
                    step="0.0001"
                    required
                    placeholder="Miktar giriniz"
                    value={allocateQty}
                    onChange={(e) => setAllocateQty(e.target.value)}
                  />
                </FormField>

                <Button
                  disabled={allocateLoading || !selectedPoLineId || !allocateQty}
                  type="submit"
                  variant="primary"
                  isLoading={allocateLoading}
                >
                  + Kalem Tahsis Et
                </Button>
              </form>
            </Section>
          )}

          <Section title="Dosyadaki Sipariş Kalemleri">
            <DataTable
              columns={orderLineColumns}
              data={detail.lines}
              keyExtractor={(l) => l.id}
              emptyMessage="Henüz bu ithalat dosyasına sipariş kalemi tahsis edilmedi."
            />
          </Section>
        </div>
      )}

      {/* Tab 3: Shipments */}
      {activeTab === 'shipments' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-5)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: 'var(--font-md)', fontWeight: 'var(--weight-bold)', color: 'var(--text-main)' }}>
              Sevkiyat Yönetimi
            </div>
            {detail.status !== 'Closed' && detail.status !== 'Cancelled' && (
              <Button variant="primary" size="sm" onClick={() => setCreateShipmentModalOpen(true)}>
                + Yeni Sevkiyat Oluştur
              </Button>
            )}
          </div>

          {detail.shipments.length === 0 ? (
            <EmptyState
              title="Sevkiyat Bulunmuyor"
              description="Bu ithalat dosyası için henüz bir sevkiyat oluşturulmamıştır."
              action={
                <Button variant="primary" size="sm" onClick={() => setCreateShipmentModalOpen(true)}>
                  + Sevkiyat Oluştur
                </Button>
              }
            />
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: '260px 1fr', gap: 'var(--space-5)' }}>
              <div className="panel" style={{ padding: 'var(--space-3)', marginBottom: 0, display: 'flex', flexDirection: 'column', gap: 'var(--space-2)' }}>
                <div style={{ fontSize: 'var(--font-xs)', fontWeight: 'var(--weight-bold)', color: 'var(--text-dim)', textTransform: 'uppercase', marginBottom: 'var(--space-2)' }}>
                  Sevkiyat Listesi
                </div>
                {detail.shipments.map((s) => (
                  <Button
                    key={s.id}
                    variant={activeShipmentId === s.id ? 'primary' : 'secondary'}
                    onClick={() => setActiveShipmentId(s.id)}
                    style={{ width: '100%', justifyContent: 'space-between' }}
                  >
                    <span className="font-mono">{s.shipmentNumber}</span>
                    <Badge variant="neutral">{s.transportMode}</Badge>
                  </Button>
                ))}
              </div>

              {activeShipmentDetail && (
                <div className="panel" style={{ marginBottom: 0 }}>
                  <div className="panel-header">
                    <div>
                      <h3 className="font-mono" style={{ fontSize: 'var(--font-md)', fontWeight: 'var(--weight-bold)', color: 'var(--accent-cyan)' }}>
                        {activeShipmentDetail.shipmentNumber}
                      </h3>
                      <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)', marginTop: '0.2rem' }}>
                        {activeShipmentDetail.originLocation} ➔ {activeShipmentDetail.destinationLocation} ({activeShipmentDetail.transportMode})
                      </div>
                    </div>

                    {activeShipmentDetail.status !== 'Delivered' && activeShipmentDetail.status !== 'Aborted' && activeShipmentDetail.status !== 'Cancelled' && (
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => {
                          setAbortShipmentId(activeShipmentDetail.id);
                          setAbortModalOpen(true);
                        }}
                      >
                        ⚠️ Sevkiyatı Abort Et
                      </Button>
                    )}
                  </div>

                  <div style={{ marginTop: 'var(--space-4)' }}>
                    <DocumentChecklistWidget scopeType="Shipment" scopeId={activeShipmentDetail.id} />
                  </div>

                  <div style={{ marginTop: 'var(--space-5)' }}>
                    <ContainerManagementPanel
                      shipmentId={activeShipmentDetail.id}
                      transportMode={activeShipmentDetail.transportMode}
                      containers={activeShipmentDetail.containers || []}
                      onRefresh={refreshAll}
                    />
                  </div>

                  <div style={{ marginTop: 'var(--space-6)' }}>
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
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-5)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ fontSize: 'var(--font-md)', fontWeight: 'var(--weight-bold)', color: 'var(--text-main)' }}>
              İthalat Dosyasına Bağlı Evraklar
            </div>
            {hasPermission('documents.upload') && (
              <Button variant="primary" size="sm" onClick={() => setDocUploadModalOpen(true)}>
                + Yeni Evrak Yükle
              </Button>
            )}
          </div>

          <DocumentChecklistWidget scopeType="ImportCase" scopeId={caseId} />

          <DataTable
            columns={docColumns}
            data={caseDocuments}
            keyExtractor={(d) => d.id}
            emptyMessage="Bu ithalat dosyasına ait henüz bir belge bulunmamaktadır."
          />
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
      <Modal
        isOpen={createShipmentModalOpen}
        onClose={() => setCreateShipmentModalOpen(false)}
        title="Yeni Sevkiyat Oluştur"
        footer={
          <>
            <Button variant="secondary" onClick={() => setCreateShipmentModalOpen(false)}>
              Vazgeç
            </Button>
            <Button type="submit" form="create-shipment-form" variant="primary" isLoading={shipmentLoading}>
              {shipmentLoading ? 'Oluşturuluyor...' : 'Sevkiyat Oluştur'}
            </Button>
          </>
        }
      >
        <form id="create-shipment-form" onSubmit={handleCreateShipment} style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
          <FormField label="Taşıma Modu *" required>
            <Select value={transportMode} onChange={(e) => setTransportMode(e.target.value)}>
              <option value="Sea">Deniz (Sea)</option>
              <option value="Air">Hava (Air)</option>
              <option value="Road">Kara (Road)</option>
              <option value="Rail">Demiryolu (Rail)</option>
              <option value="Courier">Kurye (Courier)</option>
              <option value="Multimodal">Multimodal</option>
            </Select>
          </FormField>

          <div className="form-grid-2">
            <FormField label="Çıkış Lokasyonu *" required>
              <Input type="text" required placeholder="Örn: Ningbo Port" value={originLocation} onChange={(e) => setOriginLocation(e.target.value)} />
            </FormField>
            <FormField label="Varış Lokasyonu *" required>
              <Input type="text" required placeholder="Örn: Ambarlı Limanı" value={destinationLocation} onChange={(e) => setDestinationLocation(e.target.value)} />
            </FormField>
          </div>
        </form>
      </Modal>

      {/* Abort Modal */}
      <Modal
        isOpen={abortModalOpen}
        onClose={() => setAbortModalOpen(false)}
        title={<span style={{ color: 'var(--accent-rose)' }}>⚠️ Sevkiyat Abort İptal Onayı</span>}
        footer={
          <>
            <Button variant="secondary" onClick={() => setAbortModalOpen(false)}>
              Vazgeç
            </Button>
            <Button
              variant="danger"
              disabled={abortReason.length < 10}
              onClick={handleConfirmAbort}
            >
              Abort Et ve Serbest Bırak
            </Button>
          </>
        }
      >
        <p style={{ fontSize: 'var(--font-sm)', color: 'var(--text-muted)', marginBottom: 'var(--space-4)' }}>
          Sevkiyatı Abort ettiğinizde sevk edilmemiş bakiye serbest bırakılacak ve sevkiyat sonlandırılacaktır.
        </p>
        <FormField label="Zorunlu Abort Gerekçesi (Min 10 Karakter) *" required>
          <Textarea
            rows={3}
            required
            placeholder="Sevkiyatın sonlandırılma gerekçesini yazınız..."
            value={abortReason}
            onChange={(e) => setAbortReason(e.target.value)}
          />
        </FormField>
      </Modal>
    </div>
  );
};
