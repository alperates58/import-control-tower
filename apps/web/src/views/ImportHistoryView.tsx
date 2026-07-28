import React, { useState, useEffect } from 'react';
import { ImportStatusBadge } from '../components/ImportStatusBadge';

interface ImportHistoryViewProps {
  onSelectBatch: (batchId: string) => void;
}

export const ImportHistoryView: React.FC<ImportHistoryViewProps> = ({ onSelectBatch }) => {
  const [batches, setBatches] = useState<any[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [isLoading, setIsLoading] = useState(true);

  const fetchHistory = async (p = 1) => {
    setIsLoading(true);
    try {
      const res = await fetch(`/api/v1/purchase-order-imports?page=${p}&pageSize=15`);
      if (res.ok) {
        const data = await res.json();
        setBatches(data.items);
        setTotalPages(data.totalPages);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchHistory(page);
  }, [page]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      <div>
        <h2 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700, color: '#f8fafc' }}>
          İçe Aktarma Geçmişi
        </h2>
        <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.85rem', color: '#94a3b8' }}>
          Daha önce yüklenmiş olan Excel sipariş dosyalarını ve durumlarını inceleyin.
        </p>
      </div>

      <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: '16px', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem', textAlign: 'left' }}>
          <thead>
            <tr style={{ background: 'rgba(30, 41, 59, 0.6)', borderBottom: '1px solid rgba(255, 255, 255, 0.08)', color: '#94a3b8' }}>
              <th style={{ padding: '0.75rem 1rem' }}>Dosya Adı</th>
              <th style={{ padding: '0.75rem 1rem' }}>Durum</th>
              <th style={{ padding: '0.75rem 1rem' }}>Satır Sayısı</th>
              <th style={{ padding: '0.75rem 1rem' }}>Oluşturulan PO/Kalem</th>
              <th style={{ padding: '0.75rem 1rem' }}>Yükleyen</th>
              <th style={{ padding: '0.75rem 1rem' }}>Tarih</th>
              <th style={{ padding: '0.75rem 1rem', textAlign: 'right' }}>İşlem</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={7} style={{ textAlign: 'center', padding: '3rem', color: '#94a3b8' }}>
                  Geçmiş Yükleniyor...
                </td>
              </tr>
            ) : batches.length === 0 ? (
              <tr>
                <td colSpan={7} style={{ textAlign: 'center', padding: '3rem', color: '#94a3b8' }}>
                  Henüz bir içe aktarma kaydı bulunmamaktadır.
                </td>
              </tr>
            ) : (
              batches.map((b) => (
                <tr key={b.id} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.05)' }}>
                  <td style={{ padding: '0.75rem 1rem', fontWeight: 600, color: '#f8fafc' }}>{b.originalFileName}</td>
                  <td style={{ padding: '0.75rem 1rem' }}>
                    <ImportStatusBadge status={b.status} />
                  </td>
                  <td style={{ padding: '0.75rem 1rem', color: '#cbd5e1' }}>{b.totalRowCount}</td>
                  <td style={{ padding: '0.75rem 1rem', color: '#cbd5e1' }}>
                    {b.importedOrderCount} PO / {b.importedLineCount} Kalem
                  </td>
                  <td style={{ padding: '0.75rem 1rem', color: '#94a3b8' }}>{b.uploadedByFullName || 'Kullanıcı'}</td>
                  <td style={{ padding: '0.75rem 1rem', color: '#94a3b8' }}>
                    {new Date(b.startedAtUtc).toLocaleString('tr-TR')}
                  </td>
                  <td style={{ padding: '0.75rem 1rem', textAlign: 'right' }}>
                    <button
                      onClick={() => onSelectBatch(b.id)}
                      style={{
                        padding: '0.4rem 0.8rem',
                        borderRadius: '8px',
                        background: 'rgba(59, 130, 246, 0.12)',
                        border: '1px solid rgba(59, 130, 246, 0.3)',
                        color: '#60a5fa',
                        fontSize: '0.8rem',
                        fontWeight: 600,
                        cursor: 'pointer'
                      }}
                    >
                      Detay / İncele
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        <div style={{ padding: '1rem', borderTop: '1px solid rgba(255, 255, 255, 0.08)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <span style={{ fontSize: '0.85rem', color: '#94a3b8' }}>Sayfa {page} / {totalPages}</span>
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
    </div>
  );
};
