import React, { useState, useEffect } from 'react';

interface PurchaseOrderListViewProps {
  onSelectOrder: (orderId: string) => void;
}

export const PurchaseOrderListView: React.FC<PurchaseOrderListViewProps> = ({ onSelectOrder }) => {
  const [orders, setOrders] = useState<any[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const fetchOrders = async (p = 1, q = search) => {
    setIsLoading(true);
    try {
      let url = `/api/v1/purchase-orders?page=${p}&pageSize=15`;
      if (q) url += `&search=${encodeURIComponent(q)}`;
      const res = await fetch(url);
      if (res.ok) {
        const data = await res.json();
        setOrders(data.items);
        setTotalPages(data.totalPages);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders(page, search);
  }, [page]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    fetchOrders(1, search);
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h2 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700, color: '#f8fafc' }}>
            Satın Alma Siparişleri
          </h2>
          <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.85rem', color: '#94a3b8' }}>
            Sistemde kayıtlı açık ve tamamlanmış tüm satın alma siparişleri listesi.
          </p>
        </div>

        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.5rem' }}>
          <input
            type="text"
            placeholder="Sipariş No veya Tedarikçi Ara..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{
              padding: '0.6rem 1rem',
              borderRadius: '10px',
              background: 'rgba(30, 41, 59, 0.8)',
              border: '1px solid rgba(255, 255, 255, 0.15)',
              color: '#f8fafc',
              fontSize: '0.875rem',
              minWidth: '260px'
            }}
          />
          <button
            type="submit"
            style={{
              padding: '0.6rem 1.2rem',
              borderRadius: '10px',
              background: '#3b82f6',
              border: 'none',
              color: '#ffffff',
              fontWeight: 600,
              cursor: 'pointer'
            }}
          >
            Ara
          </button>
        </form>
      </div>

      <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: '16px', overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem', textAlign: 'left' }}>
          <thead>
            <tr style={{ background: 'rgba(30, 41, 59, 0.6)', borderBottom: '1px solid rgba(255, 255, 255, 0.08)', color: '#94a3b8' }}>
              <th style={{ padding: '0.75rem 1rem' }}>Sipariş No</th>
              <th style={{ padding: '0.75rem 1rem' }}>Firma (Tedarikçi)</th>
              <th style={{ padding: '0.75rem 1rem' }}>Sipariş Tarihi</th>
              <th style={{ padding: '0.75rem 1rem' }}>Kalem Sayısı</th>
              <th style={{ padding: '0.75rem 1rem' }}>Toplam Miktar</th>
              <th style={{ padding: '0.75rem 1rem' }}>Toplam Kalan Miktar</th>
              <th style={{ padding: '0.75rem 1rem' }}>Durum</th>
              <th style={{ padding: '0.75rem 1rem', textAlign: 'right' }}>Detay</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={8} style={{ textAlign: 'center', padding: '3rem', color: '#94a3b8' }}>
                  Siparişler Yükleniyor...
                </td>
              </tr>
            ) : orders.length === 0 ? (
              <tr>
                <td colSpan={8} style={{ textAlign: 'center', padding: '3rem', color: '#94a3b8' }}>
                  Kayıtlı satın alma siparişi bulunamadı.
                </td>
              </tr>
            ) : (
              orders.map((po) => (
                <tr key={po.id} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.05)' }}>
                  <td style={{ padding: '0.75rem 1rem', fontWeight: 600, color: '#f8fafc', fontFamily: 'monospace' }}>{po.orderNumber}</td>
                  <td style={{ padding: '0.75rem 1rem', color: '#e2e8f0' }}>{po.supplierName}</td>
                  <td style={{ padding: '0.75rem 1rem', color: '#94a3b8' }}>{new Date(po.orderDate).toLocaleDateString('tr-TR')}</td>
                  <td style={{ padding: '0.75rem 1rem', color: '#cbd5e1' }}>{po.lineCount} Kalem</td>
                  <td style={{ padding: '0.75rem 1rem', color: '#34d399', fontWeight: 600 }}>{po.totalOrderedQuantity}</td>
                  <td style={{ padding: '0.75rem 1rem', color: '#fbbf24', fontWeight: 600 }}>{po.totalRemainingQuantity}</td>
                  <td style={{ padding: '0.75rem 1rem' }}>
                    <span style={{ fontSize: '0.75rem', padding: '0.2rem 0.5rem', borderRadius: '4px', background: 'rgba(59, 130, 246, 0.15)', color: '#60a5fa', fontWeight: 600 }}>
                      {po.status}
                    </span>
                  </td>
                  <td style={{ padding: '0.75rem 1rem', textAlign: 'right' }}>
                    <button
                      onClick={() => onSelectOrder(po.id)}
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
                      İncele
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
