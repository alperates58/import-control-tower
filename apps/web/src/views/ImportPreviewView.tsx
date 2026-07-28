import React, { useState, useEffect } from 'react';
import { ImportStatusBadge } from '../components/ImportStatusBadge';
import { ColumnMappingModal } from '../components/ColumnMappingModal';
import { IconArrowLeft } from '../components/Icons';

interface ImportPreviewViewProps {
  batchId: string;
  onBack: () => void;
  onConfirmSuccess: () => void;
}

export const ImportPreviewView: React.FC<ImportPreviewViewProps> = ({ batchId, onBack, onConfirmSuccess }) => {
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
      const res = await fetch(`/api/v1/purchase-order-imports/${batchId}`);
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
      const res = await fetch(url);
      if (res.ok) {
        const data = await res.json();
        setRows(data.items);
        setTotalPages(data.totalPages);
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
      const res = await fetch(`/api/v1/purchase-order-imports/${batchId}/confirm`, {
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

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <button
            onClick={onBack}
            style={{
              background: 'rgba(255, 255, 255, 0.08)',
              border: '1px solid rgba(255, 255, 255, 0.1)',
              borderRadius: '10px',
              color: '#f8fafc',
              padding: '0.5rem',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center'
            }}
          >
            <IconArrowLeft />
          </button>

          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <h2 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700, color: '#f8fafc' }}>
                İçe Aktarma Ön İzlemesi
              </h2>
              {batch && <ImportStatusBadge status={batch.status} />}
            </div>
            <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.85rem', color: '#94a3b8' }}>
              Dosya: <strong>{batch?.originalFileName}</strong> ({batch?.totalRowCount} Satır)
            </p>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          {batch?.status === 'MappingRequired' && (
            <button
              onClick={() => setIsMappingModalOpen(true)}
              style={{
                padding: '0.65rem 1.25rem',
                borderRadius: '12px',
                background: 'rgba(245, 158, 11, 0.15)',
                border: '1px solid rgba(245, 158, 11, 0.3)',
                color: '#fbbf24',
                fontWeight: 600,
                cursor: 'pointer'
              }}
            >
              Kolon Haritasını Düzenle
            </button>
          )}

          <button
            onClick={handleConfirm}
            disabled={!isReady || isConfirming}
            style={{
              padding: '0.65rem 1.5rem',
              borderRadius: '12px',
              background: isReady ? '#3b82f6' : 'rgba(255, 255, 255, 0.08)',
              border: 'none',
              color: isReady ? '#ffffff' : '#64748b',
              fontWeight: 600,
              cursor: isReady && !isConfirming ? 'pointer' : 'not-allowed',
              opacity: isConfirming ? 0.7 : 1,
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem'
            }}
          >
            {isConfirming ? 'Aktarılıyor...' : 'Aktarımı Onayla ve Tamamla'}
          </button>
        </div>
      </div>

      {errorMessage && (
        <div style={{ padding: '1rem', borderRadius: '12px', background: 'rgba(239, 68, 68, 0.12)', border: '1px solid rgba(239, 68, 68, 0.3)', color: '#f87171', fontSize: '0.875rem' }}>
          {errorMessage}
        </div>
      )}

      {/* KPI Cards */}
      {batch && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
          <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: '16px', padding: '1.25rem' }}>
            <span style={{ fontSize: '0.8rem', color: '#94a3b8' }}>Toplam Satır</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#f8fafc', marginTop: '0.25rem' }}>{batch.totalRowCount}</div>
          </div>
          <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(16, 185, 129, 0.2)', borderRadius: '16px', padding: '1.25rem' }}>
            <span style={{ fontSize: '0.8rem', color: '#34d399' }}>Geçerli Satırlar</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#34d399', marginTop: '0.25rem' }}>{batch.validRowCount}</div>
          </div>
          <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(239, 68, 68, 0.2)', borderRadius: '16px', padding: '1.25rem' }}>
            <span style={{ fontSize: '0.8rem', color: '#f87171' }}>Hatalı Satırlar</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#f87171', marginTop: '0.25rem' }}>{batch.invalidRowCount}</div>
          </div>
          <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(245, 158, 11, 0.2)', borderRadius: '16px', padding: '1.25rem' }}>
            <span style={{ fontSize: '0.8rem', color: '#fbbf24' }}>Uyarılı Satırlar</span>
            <div style={{ fontSize: '1.5rem', fontWeight: 700, color: '#fbbf24', marginTop: '0.25rem' }}>{batch.warningRowCount}</div>
          </div>
        </div>
      )}

      {/* Filter Tabs */}
      <div style={{ display: 'flex', gap: '0.5rem', borderBottom: '1px solid rgba(255, 255, 255, 0.08)', paddingBottom: '0.5rem' }}>
        <button
          onClick={() => { setStatusFilter(null); setPage(1); }}
          style={{
            padding: '0.5rem 1rem',
            borderRadius: '8px',
            background: statusFilter === null ? 'rgba(59, 130, 246, 0.2)' : 'transparent',
            border: 'none',
            color: statusFilter === null ? '#60a5fa' : '#94a3b8',
            fontSize: '0.85rem',
            fontWeight: 600,
            cursor: 'pointer'
          }}
        >
          Tüm Satırlar
        </button>
        <button
          onClick={() => { setStatusFilter('Error'); setPage(1); }}
          style={{
            padding: '0.5rem 1rem',
            borderRadius: '8px',
            background: statusFilter === 'Error' ? 'rgba(239, 68, 68, 0.2)' : 'transparent',
            border: 'none',
            color: statusFilter === 'Error' ? '#f87171' : '#94a3b8',
            fontSize: '0.85rem',
            fontWeight: 600,
            cursor: 'pointer'
          }}
        >
          Sadece Hatalılar
        </button>
        <button
          onClick={() => { setStatusFilter('Warning'); setPage(1); }}
          style={{
            padding: '0.5rem 1rem',
            borderRadius: '8px',
            background: statusFilter === 'Warning' ? 'rgba(245, 158, 11, 0.2)' : 'transparent',
            border: 'none',
            color: statusFilter === 'Warning' ? '#fbbf24' : '#94a3b8',
            fontSize: '0.85rem',
            fontWeight: 600,
            cursor: 'pointer'
          }}
        >
          Sadece Uyarılılar
        </button>
      </div>

      {/* Rows Table */}
      <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: '16px', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem', textAlign: 'left' }}>
          <thead>
            <tr style={{ background: 'rgba(30, 41, 59, 0.6)', borderBottom: '1px solid rgba(255, 255, 255, 0.08)', color: '#94a3b8' }}>
              <th style={{ padding: '0.75rem 1rem' }}>Satır #</th>
              <th style={{ padding: '0.75rem 1rem' }}>Durum</th>
              <th style={{ padding: '0.75rem 1rem' }}>İşlem</th>
              <th style={{ padding: '0.75rem 1rem' }}>Ham Veri</th>
              <th style={{ padding: '0.75rem 1rem' }}>Hata / Uarı Kodları</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={5} style={{ textAlign: 'center', padding: '3rem', color: '#94a3b8' }}>
                  Satırlar Yükleniyor...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={5} style={{ textAlign: 'center', padding: '3rem', color: '#94a3b8' }}>
                  Gösterilecek satır bulunamadı.
                </td>
              </tr>
            ) : (
              rows.map((row) => {
                const isError = row.validationStatus === 'Error';
                const isWarn = row.validationStatus === 'Warning';

                return (
                  <tr
                    key={row.id}
                    style={{
                      borderBottom: '1px solid rgba(255, 255, 255, 0.05)',
                      background: isError ? 'rgba(239, 68, 68, 0.08)' : (isWarn ? 'rgba(245, 158, 11, 0.05)' : 'transparent')
                    }}
                  >
                    <td style={{ padding: '0.75rem 1rem', fontWeight: 600, color: '#f8fafc' }}>{row.rowNumber}</td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      <span
                        style={{
                          fontSize: '0.75rem',
                          fontWeight: 600,
                          padding: '0.2rem 0.5rem',
                          borderRadius: '4px',
                          background: isError ? 'rgba(239, 68, 68, 0.2)' : (isWarn ? 'rgba(245, 158, 11, 0.2)' : 'rgba(16, 185, 129, 0.2)'),
                          color: isError ? '#f87171' : (isWarn ? '#fbbf24' : '#34d399')
                        }}
                      >
                        {row.validationStatus}
                      </span>
                    </td>
                    <td style={{ padding: '0.75rem 1rem', color: '#cbd5e1' }}>{row.importAction}</td>
                    <td style={{ padding: '0.75rem 1rem', fontFamily: 'monospace', fontSize: '0.75rem', color: '#94a3b8', maxWidth: '300px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {row.rawDataJson}
                    </td>
                    <td style={{ padding: '0.75rem 1rem' }}>
                      {row.errorCodes.map((e: string) => (
                        <span key={e} style={{ display: 'inline-block', margin: '0.1rem', padding: '0.15rem 0.4rem', borderRadius: '4px', background: 'rgba(239, 68, 68, 0.25)', color: '#f87171', fontSize: '0.7rem' }}>
                          {e}
                        </span>
                      ))}
                      {row.warningCodes.map((w: string) => (
                        <span key={w} style={{ display: 'inline-block', margin: '0.1rem', padding: '0.15rem 0.4rem', borderRadius: '4px', background: 'rgba(245, 158, 11, 0.25)', color: '#fbbf24', fontSize: '0.7rem' }}>
                          {w}
                        </span>
                      ))}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>

        {/* Pagination */}
        <div style={{ padding: '1rem', borderTop: '1px solid rgba(255, 255, 255, 0.08)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span style={{ fontSize: '0.85rem', color: '#94a3b8' }}>
            Sayfa {page} / {totalPages}
          </span>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button
              disabled={page <= 1}
              onClick={() => setPage(page - 1)}
              style={{ padding: '0.4rem 0.8rem', borderRadius: '6px', background: 'rgba(255, 255, 255, 0.08)', border: 'none', color: '#f8fafc', fontSize: '0.8rem', cursor: page <= 1 ? 'not-allowed' : 'pointer' }}
            >
              Önceki
            </button>
            <button
              disabled={page >= totalPages}
              onClick={() => setPage(page + 1)}
              style={{ padding: '0.4rem 0.8rem', borderRadius: '6px', background: 'rgba(255, 255, 255, 0.08)', border: 'none', color: '#f8fafc', fontSize: '0.8rem', cursor: page >= totalPages ? 'not-allowed' : 'pointer' }}
            >
              Sonraki
            </button>
          </div>
        </div>
      </div>

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
