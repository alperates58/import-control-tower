import React, { useEffect, useState } from 'react';
import { DocumentSummary } from '../types/document';
import { documentService } from '../services/documentService';
import { DocumentUploadModal } from './DocumentUploadModal';
import { DocumentVersionDrawer } from '../components/documents/DocumentVersionDrawer';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { Button } from '../components/ui/Button';
import { Input, Select, FormField } from '../components/ui/Input';
import { DataTable, Column } from '../components/ui/DataTable';
import { Badge } from '../components/ui/Badge';
import { ErrorState } from '../components/ui/FeedbackState';
import { DropdownMenu } from '../components/ui/DropdownMenu';
import { ConfirmDialog } from '../components/ui/Modal';

export const DOCUMENT_TYPE_LABELS: Record<string, string> = {
  CommercialInvoice: 'Ticari Fatura',
  ProformaInvoice: 'Proforma Fatura',
  PackingList: 'Çeki Listesi',
  BillOfLading: 'Konşimento',
  SeaWaybill: 'Sea Waybill',
  AirWaybill: 'Hava Taşıma Senedi (AWB)',
  CMR: 'Kara Taşıma Senedi (CMR)',
  CertificateOfOrigin: 'Menşe Şahadetnamesi',
  ATR: 'A.TR Dolaşım Belgesi',
  EUR1: 'EUR.1 Dolaşım Belgesi',
  InsuranceCertificate: 'Sigorta Poliçesi',
  CustomsDeclaration: 'Gümrük Beyannamesi',
  MSDS: 'MSDS / Güvenlik Formu',
  Other: 'Diğer Belge'
};

export const getDocumentTypeLabel = (type: string) => DOCUMENT_TYPE_LABELS[type] || type;

export const DocumentListView: React.FC = () => {
  const { authenticatedFetch, hasPermission } = useAuth();
  const [documents, setDocuments] = useState<DocumentSummary[]>([]);
  const [search, setSearch] = useState('');
  const [documentType, setDocumentType] = useState('');
  const [status, setStatus] = useState('Active');

  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // Modals & Drawers
  const [uploadModalOpen, setUploadModalOpen] = useState(false);
  const [selectedDocId, setSelectedDocId] = useState<string | null>(null);
  const [selectedDocTitle, setSelectedDocTitle] = useState('');
  const [versionDrawerOpen, setVersionDrawerOpen] = useState(false);
  const [cancelTargetDoc, setCancelTargetDoc] = useState<DocumentSummary | null>(null);

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

  const executeCancelDocument = async () => {
    if (!cancelTargetDoc) return;
    try {
      await documentService.cancelDocument(cancelTargetDoc.id, cancelTargetDoc.rowVersion, authenticatedFetch);
      setCancelTargetDoc(null);
      await fetchDocuments();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const columns: Column<DocumentSummary>[] = [
    {
      key: 'title',
      header: 'Evrak Başlığı',
      render: (d) => <span style={{ fontWeight: 'var(--weight-semibold)' }}>📄 {d.title}</span>
    },
    {
      key: 'documentType',
      header: 'Evrak Türü',
      render: (d) => <Badge variant="cyan">{getDocumentTypeLabel(d.documentType)}</Badge>
    },
    {
      key: 'documentNumber',
      header: 'Evrak / Fatura No',
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
      render: (d) => {
        if (!d.currentVersion) return '-';
        const bytes = d.currentVersion.fileSizeBytes;
        if (bytes <= 0) return '0.0 KB';
        return bytes >= 1024 * 1024
          ? `${(bytes / (1024 * 1024)).toFixed(1)} MB`
          : `${(bytes / 1024).toFixed(1)} KB`;
      }
    },
    {
      key: 'createdAtUtc',
      header: 'Oluşturulma',
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
      render: (d) => {
        const menuItems: { label: React.ReactNode; onClick: () => void; isDanger?: boolean }[] = [
          {
            label: 'Detay & Versiyonlar',
            onClick: () => {
              setSelectedDocId(d.id);
              setSelectedDocTitle(d.title);
              setVersionDrawerOpen(true);
            }
          }
        ];

        if (d.status === 'Active') {
          menuItems.unshift({
            label: 'Evrak İndir',
            onClick: () => handleDownload(d.id)
          });

          if (hasPermission('documents.version')) {
            menuItems.push({
              label: 'Yeni Sürüm Yükle',
              onClick: () => {
                setSelectedDocId(d.id);
                setSelectedDocTitle(d.title);
                setUploadModalOpen(true);
              }
            });
          }

          if (hasPermission('documents.cancel')) {
            menuItems.push({
              label: 'Evrakı İptal Et',
              isDanger: true,
              onClick: () => setCancelTargetDoc(d)
            });
          }
        }

        return <DropdownMenu items={menuItems} />;
      }
    }
  ];

  return (
    <div>
      <PageHeader
        title="İthalat Evrakları ve Belge Yönetimi"
        subtitle="Gümrük, nakliye ve sipariş belgelerini versiyon bazında takip edin."
        actions={
          hasPermission('documents.create') && (
            <Button variant="primary" onClick={() => { setSelectedDocId(null); setUploadModalOpen(true); }}>
              + Yeni Evrak Yükle
            </Button>
          )
        }
      />

      <div className="card" style={{ padding: 'var(--space-4)', marginBottom: 'var(--space-6)' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 'var(--space-4)' }}>
          <FormField label="Arama">
            <Input
              placeholder="Başlık veya Belge No..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </FormField>

          <FormField label="Evrak Türü">
            <Select
              value={documentType}
              onChange={(e) => setDocumentType(e.target.value)}
              options={[
                { value: '', label: 'Tüm Evrak Türleri' },
                ...Object.entries(DOCUMENT_TYPE_LABELS).map(([value, label]) => ({ value, label }))
              ]}
            />
          </FormField>

          <FormField label="Durum">
            <Select
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              options={[
                { value: '', label: 'Tüm Durumlar' },
                { value: 'Active', label: 'Aktif' },
                { value: 'Cancelled', label: 'İptal Edildi' }
              ]}
            />
          </FormField>
        </div>
      </div>

      {errorMsg ? (
        <ErrorState description={errorMsg} onRetry={fetchDocuments} />
      ) : (
        <DataTable
          columns={columns}
          data={documents}
          keyExtractor={(d) => d.id}
          isLoading={loading}
          emptyMessage="Arama kriterlerinize uygun evrak kaydı bulunamadı."
        />
      )}

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
      <DocumentUploadModal
        isOpen={uploadModalOpen}
        onClose={() => setUploadModalOpen(false)}
        onSuccess={fetchDocuments}
        existingDocumentId={selectedDocId || undefined}
      />

      {/* Confirm Cancellation Dialog */}
      <ConfirmDialog
        isOpen={Boolean(cancelTargetDoc)}
        onClose={() => setCancelTargetDoc(null)}
        onConfirm={executeCancelDocument}
        title="Evrak İptal Onayı"
        message={`"${cancelTargetDoc?.title}" evrakını iptal etmek istediğinize emin misiniz? Bu işlem geri alınamaz.`}
        confirmText="Evrakı İptal Et"
        isDanger
      />
    </div>
  );
};
