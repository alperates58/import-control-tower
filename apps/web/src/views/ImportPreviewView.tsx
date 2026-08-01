import React, { useState, useEffect } from 'react';
import { ImportStatusBadge } from '../components/ImportStatusBadge';
import { ColumnMappingModal } from '../components/ColumnMappingModal';
import { IconArrowLeft } from '../components/Icons';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { Button, IconButton } from '../components/ui/Button';
import { KPICard } from '../components/ui/Card';
import { DataTable, Column, Pagination } from '../components/ui/DataTable';
import { Badge } from '../components/ui/Badge';
import { Tabs } from '../components/ui/PageHeader';

interface ImportPreviewViewProps {
  batchId: string;
  onBack: () => void;
  onConfirmSuccess: () => void;
}

export const ImportPreviewView: React.FC<ImportPreviewViewProps> = ({ batchId, onBack, onConfirmSuccess }) => {
  const { authenticatedFetch } = useAuth();
  const [batchData, setBatchData] = useState<any>(null);
  const [rows, setRows] = useState<any[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [statusFilter, setStatusFilter] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isConfirming, setIsConfirming] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isMappingModalOpen, setIsMappingModalOpen] = useState(false);

  const fetchBatchDetail = async () => {
    try {
      const res = await authenticatedFetch(`/api/v1/purchase-order-imports/${batchId}`);
      if (res.ok) {
        const data = await res.json();
        setBatchData(data);
      }
    } catch (e) {
      console.error(e);
    }
  };

  const fetchRows = async (p = 1, stat = statusFilter) => {
    setIsLoading(true);
    try {
      let url = `/api/v1/purchase-order-imports/${batchId}/rows?page=${p}&pageSize=20`;
      if (stat) url += `&status=${stat}`;
      const res = await authenticatedFetch(url);
      if (res.ok) {
        const data = await res.json();
        setRows(data.items || []);
        setTotalPages(data.totalPages || 1);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchBatchDetail();
    fetchRows(page, statusFilter);
  }, [batchId, page, statusFilter]);

  const handleConfirm = async () => {
    if (isConfirming) return;
    setIsConfirming(true);
    setErrorMessage(null);

    const idempotencyKey = crypto.randomUUID();

    try {
      const res = await authenticatedFetch(`/api/v1/purchase-order-imports/${batchId}/confirm`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-ICT-CSRF-Protection': '1',
          'Idempotency-Key': idempotencyKey
        }
      });

      if (res.ok) {
        onConfirmSuccess();
      } else {
        const err = await res.json().catch(() => null);
        setErrorMessage(err?.detail || err?.title || 'İçe aktarma onaylanırken hata oluştu.');
      }
    } catch (e) {
      setErrorMessage('Ağ bağlantı hatası oluştu.');
    } finally {
      setIsConfirming(false);
    }
  };

  const batch = batchData?.batch;
  const isReady = batch?.status === 'ReadyForConfirmation';

  const columns: Column<any>[] = [
    {
      key: 'rowNumber',
      header: 'Satır #',
      render: (r) => r.rowNumber
    },
    {
      key: 'validationStatus',
      header: 'Durum',
      render: (r) => {
        const isError = r.validationStatus === 'Error';
        const isWarn = r.validationStatus === 'Warning';
        return (
          <Badge variant={isError ? 'rose' : isWarn ? 'amber' : 'emerald'}>
            {r.validationStatus}
          </Badge>
        );
      }
    },
    {
      key: 'importAction',
      header: 'İşlem',
      render: (r) => r.importAction
    },
    {
      key: 'rawDataJson',
      header: 'Ham Veri',
      render: (r) => (
        <span className="font-mono" style={{ fontSize: 'var(--font-xs)', color: 'var(--text-dim)', maxWidth: '280px', display: 'inline-block', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {r.rawDataJson}
        </span>
      )
    },
    {
      key: 'errorCodes',
      header: 'Hata / Uyarı Kodları',
      render: (r) => (
        <div style={{ display: 'flex', gap: '0.2rem', flexWrap: 'wrap' }}>
          {r.errorCodes.map((e: string) => (
            <Badge key={e} variant="rose" style={{ fontSize: '0.65rem' }}>
              {e}
            </Badge>
          ))}
          {r.warningCodes.map((w: string) => (
            <Badge key={w} variant="amber" style={{ fontSize: '0.65rem' }}>
              {w}
            </Badge>
          ))}
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
            <span>İçe Aktarma Ön İzlemesi</span>
            {batch && <ImportStatusBadge status={batch.status} />}
          </div>
        }
        subtitle={
          <span>Dosya: <strong>{batch?.originalFileName}</strong> ({batch?.totalRowCount} Satır)</span>
        }
        actions={
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            {batch?.status === 'MappingRequired' && (
              <Button variant="secondary" onClick={() => setIsMappingModalOpen(true)}>
                Kolon Haritasını Düzenle
              </Button>
            )}
            <Button
              variant="primary"
              onClick={handleConfirm}
              disabled={!isReady || isConfirming}
              isLoading={isConfirming}
            >
              {isConfirming ? 'Aktarılıyor...' : 'Aktarımı Onayla ve Tamamla'}
            </Button>
          </div>
        }
      />

      {errorMessage && (
        <div style={{ marginBottom: 'var(--space-4)' }}>
          <Badge variant="rose" style={{ width: '100%', padding: '0.75rem' }}>
            {errorMessage}
          </Badge>
        </div>
      )}

      {batch && (
        <div className="kpi-grid">
          <KPICard title="Toplam Satır" value={batch.totalRowCount} />
          <KPICard title="Geçerli Satırlar" value={batch.validRowCount} valueColor="var(--status-success)" />
          <KPICard title="Hatalı Satırlar" value={batch.invalidRowCount} valueColor="var(--status-danger)" />
          <KPICard title="Uyarılı Satırlar" value={batch.warningRowCount} valueColor="var(--status-warning)" />
        </div>
      )}

      <Tabs
        tabs={[
          { id: 'ALL', label: 'Tüm Satırlar' },
          { id: 'Error', label: 'Sadece Hatalılar' },
          { id: 'Warning', label: 'Sadece Uyarılılar' }
        ]}
        activeTab={statusFilter || 'ALL'}
        onChange={(id) => {
          setStatusFilter(id === 'ALL' ? null : id);
          setPage(1);
        }}
      />

      <DataTable
        columns={columns}
        data={rows}
        keyExtractor={(r) => r.id}
        isLoading={isLoading}
        emptyMessage="Gösterilecek satır bulunamadı."
      />

      {!isLoading && rows.length > 0 && (
        <Pagination
          currentPage={page}
          totalPages={totalPages}
          totalCount={batch?.totalRowCount || rows.length}
          onPageChange={(newPage) => setPage(newPage)}
        />
      )}

      <ColumnMappingModal
        isOpen={isMappingModalOpen}
        onClose={() => setIsMappingModalOpen(false)}
        unmappedHeaders={batchData?.unmappedColumns || []}
        missingRequired={batchData?.missingRequiredColumns || []}
        currentMapping={batchData?.columnMapping || {}}
        onSaveMapping={() => { fetchBatchDetail(); fetchRows(1); }}
      />
    </div>
  );
};
