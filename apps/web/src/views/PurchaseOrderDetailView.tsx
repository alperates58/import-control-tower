import React, { useState, useEffect } from 'react';
import { IconArrowLeft } from '../components/Icons';
import { useAuth } from '../context/AuthContext';
import { PageHeader, Tabs } from '../components/ui/PageHeader';
import { IconButton, Button } from '../components/ui/Button';
import { DataTable, Column } from '../components/ui/DataTable';
import { StatusBadge, Badge } from '../components/ui/Badge';
import { Section } from '../components/ui/Card';
import { EmptyState, LoadingSkeleton } from '../components/ui/FeedbackState';
import { DocumentChecklistWidget } from '../components/documents/DocumentChecklistWidget';
import { ContainerManagementPanel } from '../components/import-cases/ContainerManagementPanel';
import { MilestoneTimeline } from '../components/import-cases/MilestoneTimeline';
import { DocumentVersionDrawer } from '../components/documents/DocumentVersionDrawer';
import { documentService } from '../services/documentService';
import { importCaseService } from '../services/importCaseService';
import { DocumentSummary } from '../types/document';
import { ShipmentDetail } from '../types/importCase';

interface PurchaseOrderDetailViewProps {
  orderId: string;
  onBack: () => void;
  onNavigateToCase?: (caseId: string) => void;
}

export const PurchaseOrderDetailView: React.FC<PurchaseOrderDetailViewProps> = ({
  orderId,
  onBack,
  onNavigateToCase
}) => {
  const { authenticatedFetch } = useAuth();
  const [po, setPo] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'items' | 'documents' | 'shipments'>('items');

  // Related case shipment detail
  const [activeShipmentDetail, setActiveShipmentDetail] = useState<ShipmentDetail | null>(null);

  // Related documents & drawer
  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [docsLoading, setDocsLoading] = useState(false);
  const [versionDrawerOpen, setVersionDrawerOpen] = useState(false);
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);
  const [selectedDocTitle, setSelectedDocTitle] = useState('');

  const refreshCaseDetails = async (caseId: string) => {
    try {
      const caseData = await importCaseService.getCaseById(caseId, authenticatedFetch);
      if (caseData && caseData.shipments.length > 0) {
        const sData = await importCaseService.getShipmentById(caseData.shipments[0].id, authenticatedFetch);
        setActiveShipmentDetail(sData);
      }
    } catch {
      setActiveShipmentDetail(null);
    }
  };

  useEffect(() => {
    const fetchDetail = async () => {
      setIsLoading(true);
      try {
        const res = await authenticatedFetch(`/api/v1/purchase-orders/${orderId}`);
        if (res.ok) {
          const data = await res.json();
          setPo(data);

          // Fetch associated documents & shipment if importCaseId exists
          if (data?.importCaseId) {
            setDocsLoading(true);
            try {
              const docs = await documentService.getDocuments(
                { importCaseId: data.importCaseId },
                authenticatedFetch
              );
              setDocuments(docs);
            } catch {
              setDocuments([]);
            } finally {
              setDocsLoading(false);
            }

            await refreshCaseDetails(data.importCaseId);
          }
        }
      } catch (e) {
        console.error(e);
      } finally {
        setIsLoading(false);
      }
    };

    fetchDetail();
  }, [orderId]);

  if (isLoading) {
    return <LoadingSkeleton rows={4} height="50px" />;
  }

  if (!po) {
    return (
      <EmptyState
        title="Sipariş Detayı Yüklenemedi"
        description="Sipariş detay verilerine erişilemedi veya sipariş silinmiş olabilir."
        action={
          <Button variant="secondary" onClick={onBack}>
            Geri Dön
          </Button>
        }
      />
    );
  }

  const lineColumns: Column<any>[] = [
    {
      key: 'lineNumber',
      header: 'Kalem #',
      render: (l) => l.lineNumber
    },
    {
      key: 'stockCode',
      header: 'Stok Kodu',
      render: (l) => (
        <span className="font-mono" style={{ fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>
          {l.stockCode}
        </span>
      )
    },
    {
      key: 'stockName',
      header: 'Stok İsmi',
      render: (l) => l.stockName
    },
    {
      key: 'orderedQuantity',
      header: 'Sipariş Miktarı',
      align: 'right',
      render: (l) => (
        <span style={{ color: 'var(--status-success)', fontWeight: 'var(--weight-semibold)' }}>
          {l.orderedQuantity}
        </span>
      )
    },
    {
      key: 'remainingQuantity',
      header: 'Kalan Miktar',
      align: 'right',
      render: (l) => (
        <span style={{ color: 'var(--status-warning)', fontWeight: 'var(--weight-semibold)' }}>
          {l.remainingQuantity}
        </span>
      )
    },
    {
      key: 'sasDate',
      header: 'SAS Tarihi',
      render: (l) => (l.sasDate ? new Date(l.sasDate).toLocaleDateString('tr-TR') : '-')
    }
  ];

  const docColumns: Column<DocumentSummary>[] = [
    {
      key: 'documentType',
      header: 'Evrak Tipi',
      render: (d) => <strong>{d.title || d.documentType}</strong>
    },
    {
      key: 'fileName',
      header: 'Dosya Adı',
      render: (d) => <span className="font-mono">{d.currentVersion?.originalFileName || '-'}</span>
    },
    {
      key: 'status',
      header: 'Durum',
      render: (d) => (
        <Badge variant={d.status === 'Approved' ? 'emerald' : d.status === 'PendingReview' ? 'amber' : 'neutral'}>
          {d.status}
        </Badge>
      )
    },
    {
      key: 'createdAtUtc',
      header: 'Yüklenme Tarihi',
      render: (d) => (d.createdAtUtc ? new Date(d.createdAtUtc).toLocaleDateString('tr-TR') : '-')
    },
    {
      key: 'actions',
      header: 'İşlem',
      align: 'right',
      render: (d) => (
        <Button
          variant="secondary"
          size="sm"
          onClick={() => {
            setSelectedDocId(d.id);
            setSelectedDocTitle(d.title || d.documentType);
            setVersionDrawerOpen(true);
          }}
        >
          Sürümler & Detay
        </Button>
      )
    }
  ];

  const caseId = po.importCaseId;

  return (
    <div>
      <PageHeader
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <IconButton icon={<IconArrowLeft />} onClick={onBack} aria-label="Geri Dön" />
            <span>Sipariş No: {po.orderNumber}</span>
            <StatusBadge status={po.status} />
          </div>
        }
        subtitle={
          <span>
            Tedarikçi: <strong>{po.supplierName}</strong> | Sipariş Tarihi:{' '}
            <strong>{new Date(po.orderDate).toLocaleDateString('tr-TR')}</strong>
          </span>
        }
      />

      {/* 🚀 Step-by-Step Progressive Process Stepper */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: 'var(--space-3)',
          marginBottom: 'var(--space-6)',
          padding: 'var(--space-3)',
          background: 'var(--bg-surface)',
          border: '1px solid var(--border-color)',
          borderRadius: 'var(--radius-lg)'
        }}
      >
        <button
          onClick={() => setActiveTab('items')}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            padding: 'var(--space-3)',
            borderRadius: 'var(--radius-md)',
            border: activeTab === 'items' ? '1px solid var(--accent-blue)' : '1px solid var(--border-subtle)',
            background: activeTab === 'items' ? 'var(--bg-card-hover)' : 'transparent',
            textAlign: 'left',
            cursor: 'pointer',
            transition: 'all var(--transition-fast)'
          }}
        >
          <div style={{ width: 28, height: 28, borderRadius: '50%', background: 'var(--accent-blue)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 'var(--weight-bold)', fontSize: '0.8rem' }}>1</div>
          <div>
            <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>Adım 1</div>
            <div style={{ fontSize: 'var(--font-sm)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>Sipariş Kalemleri</div>
          </div>
        </button>

        <button
          onClick={() => {
            if (caseId && onNavigateToCase) {
              onNavigateToCase(caseId);
            } else {
              setActiveTab('items');
            }
          }}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            padding: 'var(--space-3)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-subtle)',
            background: 'transparent',
            textAlign: 'left',
            cursor: caseId ? 'pointer' : 'default',
            opacity: caseId ? 1 : 0.65,
            transition: 'all var(--transition-fast)'
          }}
          title={caseId ? "Bağlı İthalat Dosyasına Git" : "Henüz bir İthalat Dosyasına bağlanmamış"}
        >
          <div style={{ width: 28, height: 28, borderRadius: '50%', background: caseId ? 'var(--accent-cyan)' : 'var(--text-dim)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 'var(--weight-bold)', fontSize: '0.8rem' }}>2</div>
          <div>
            <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>Adım 2</div>
            <div style={{ fontSize: 'var(--font-sm)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>
              {caseId ? `İthalat Dosyası (IMP)` : 'İthalat Bağlantısı'}
            </div>
          </div>
        </button>

        <button
          onClick={() => setActiveTab('documents')}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            padding: 'var(--space-3)',
            borderRadius: 'var(--radius-md)',
            border: activeTab === 'documents' ? '1px solid var(--accent-blue)' : '1px solid var(--border-subtle)',
            background: activeTab === 'documents' ? 'var(--bg-card-hover)' : 'transparent',
            textAlign: 'left',
            cursor: 'pointer',
            transition: 'all var(--transition-fast)'
          }}
        >
          <div style={{ width: 28, height: 28, borderRadius: '50%', background: 'var(--accent-amber)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 'var(--weight-bold)', fontSize: '0.8rem' }}>3</div>
          <div>
            <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>Adım 3</div>
            <div style={{ fontSize: 'var(--font-sm)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>Operasyonel Evraklar</div>
          </div>
        </button>

        <button
          onClick={() => setActiveTab('shipments')}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            padding: 'var(--space-3)',
            borderRadius: 'var(--radius-md)',
            border: activeTab === 'shipments' ? '1px solid var(--accent-blue)' : '1px solid var(--border-subtle)',
            background: activeTab === 'shipments' ? 'var(--bg-card-hover)' : 'transparent',
            textAlign: 'left',
            cursor: 'pointer',
            transition: 'all var(--transition-fast)'
          }}
        >
          <div style={{ width: 28, height: 28, borderRadius: '50%', background: 'var(--accent-emerald)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 'var(--weight-bold)', fontSize: '0.8rem' }}>4</div>
          <div>
            <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>Adım 4</div>
            <div style={{ fontSize: 'var(--font-sm)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>Sevkiyat & Konteyner</div>
          </div>
        </button>
      </div>

      {/* Navigation Tabs */}
      <Tabs
        tabs={[
          { id: 'items', label: `Sipariş Stok Kalemleri (${po.lines?.length || 0})` },
          { id: 'documents', label: `Operasyonel Evraklar (${documents.length})` },
          { id: 'shipments', label: 'Sevkiyat & Konteyner Takibi' }
        ]}
        activeTab={activeTab}
        onChange={(id) => setActiveTab(id as any)}
      />

      {/* Tab 1: Order Stock Line Items */}
      {activeTab === 'items' && (
        <Section title={`Stok Kalemleri Listesi (${po.lines?.length || 0})`}>
          <DataTable
            columns={lineColumns}
            data={po.lines || []}
            keyExtractor={(l) => l.id}
            emptyMessage="Bu siparişte henüz kalem bulunmuyor."
          />
        </Section>
      )}

      {/* Tab 2: Operational Documents */}
      {activeTab === 'documents' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-5)' }}>
          {caseId ? (
            <DocumentChecklistWidget scopeType="ImportCase" scopeId={caseId} />
          ) : (
            <div className="card" style={{ padding: 'var(--space-4)', background: 'var(--bg-surface)' }}>
              <div style={{ fontSize: 'var(--font-sm)', color: 'var(--text-muted)' }}>
                ℹ️ Bu sipariş henüz bir İthalat Dosyasına bağlanmadığı için evrak kontrol listesi oluşmamıştır.
              </div>
            </div>
          )}

          <Section title={`Yüklü Operasyonel Evraklar (${documents.length})`}>
            {docsLoading ? (
              <LoadingSkeleton rows={3} height="40px" />
            ) : (
              <DataTable
                columns={docColumns}
                data={documents}
                keyExtractor={(d) => d.id}
                emptyMessage="Bu siparişe veya bağlı dosyaya ait henüz evrak yüklenmemiş."
              />
            )}
          </Section>
        </div>
      )}

      {/* Tab 3: Shipments & Containers */}
      {activeTab === 'shipments' && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-6)' }}>
          {activeShipmentDetail ? (
            <>
              <ContainerManagementPanel
                shipmentId={activeShipmentDetail.id}
                transportMode={activeShipmentDetail.transportMode}
                containers={activeShipmentDetail.containers || []}
                onRefresh={() => caseId && refreshCaseDetails(caseId)}
              />
              <MilestoneTimeline
                shipmentId={activeShipmentDetail.id}
                milestones={activeShipmentDetail.milestones || []}
                onRefresh={() => caseId && refreshCaseDetails(caseId)}
              />
            </>
          ) : (
            <EmptyState
              title="Sevkiyat & Konteyner Bulunmuyor"
              description="Bu siparişe bağlı henüz aktif bir deniz/hava/karayolu sevkiyatı ve konteyner kaydı oluşturulmamıştır."
            />
          )}
        </div>
      )}

      {/* Document Version Drawer */}
      {selectedDocId && (
        <DocumentVersionDrawer
          documentId={selectedDocId}
          documentTitle={selectedDocTitle}
          isOpen={versionDrawerOpen}
          onClose={() => {
            setVersionDrawerOpen(false);
            setSelectedDocId(null);
          }}
        />
      )}
    </div>
  );
};
