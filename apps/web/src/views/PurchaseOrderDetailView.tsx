import React, { useState, useEffect } from 'react';
import { IconArrowLeft } from '../components/Icons';
import { useAuth } from '../context/AuthContext';

interface PurchaseOrderDetailViewProps {
  orderId: string;
  onBack: () => void;
}

export const PurchaseOrderDetailView: React.FC<PurchaseOrderDetailViewProps> = ({ orderId, onBack }) => {
  const { authenticatedFetch } = useAuth();
  const [po, setPo] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchDetail = async () => {
      setIsLoading(true);
      try {
        const res = await authenticatedFetch(`/api/v1/purchase-orders/${orderId}`);
        if (res.ok) {
          const data = await res.json();
          setPo(data);
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
    return <div style={{ textAlign: 'center', padding: '3rem', color: '#94a3b8' }}>Sipariş Detayı Yükleniyor...</div>;
  }

  if (!po) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: '#f87171' }}>
        Sipariş detayları yüklenemedi.
        <button onClick={onBack} style={{ marginTop: '1rem', display: 'block', margin: '1rem auto', padding: '0.5rem 1rem', borderRadius: '8px', background: 'rgba(255, 255, 255, 0.1)', border: 'none', color: '#fff', cursor: 'pointer' }}>
          Geri Dön
        </button>
      </div>
    );
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
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
              Sipariş No: {po.orderNumber}
            </h2>
            <span style={{ fontSize: '0.75rem', padding: '0.2rem 0.5rem', borderRadius: '4px', background: 'rgba(59, 130, 246, 0.15)', color: '#60a5fa', fontWeight: 600 }}>
              {po.status}
            </span>
          </div>
          <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.85rem', color: '#94a3b8' }}>
            Tedarikçi: <strong>{po.supplierName}</strong> | Sipariş Tarihi: <strong>{new Date(po.orderDate).toLocaleDateString('tr-TR')}</strong>
          </p>
        </div>
      </div>

      <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid rgba(255, 255, 255, 0.08)', borderRadius: '16px', padding: '1.5rem' }}>
        <h4 style={{ margin: '0 0 1rem 0', fontSize: '1rem', fontWeight: 600, color: '#f8fafc' }}>Sipariş Stok Kalemleri ({po.lines?.length || 0})</h4>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem', textAlign: 'left' }}>
          <thead>
            <tr style={{ background: 'rgba(30, 41, 59, 0.6)', borderBottom: '1px solid rgba(255, 255, 255, 0.08)', color: '#94a3b8' }}>
              <th style={{ padding: '0.75rem 1rem' }}>Kalem #</th>
              <th style={{ padding: '0.75rem 1rem' }}>Stok Kodu</th>
              <th style={{ padding: '0.75rem 1rem' }}>Stok İsmi</th>
              <th style={{ padding: '0.75rem 1rem' }}>Sipariş Miktarı</th>
              <th style={{ padding: '0.75rem 1rem' }}>Kalan Miktar</th>
              <th style={{ padding: '0.75rem 1rem' }}>SAS Tarihi</th>
            </tr>
          </thead>
          <tbody>
            {po.lines?.map((l: any) => (
              <tr key={l.id} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.05)' }}>
                <td style={{ padding: '0.75rem 1rem', color: '#94a3b8' }}>{l.lineNumber}</td>
                <td style={{ padding: '0.75rem 1rem', fontWeight: 600, color: '#f8fafc', fontFamily: 'monospace' }}>{l.stockCode}</td>
                <td style={{ padding: '0.75rem 1rem', color: '#e2e8f0' }}>{l.stockName}</td>
                <td style={{ padding: '0.75rem 1rem', color: '#34d399', fontWeight: 600 }}>{l.orderedQuantity}</td>
                <td style={{ padding: '0.75rem 1rem', color: '#fbbf24', fontWeight: 600 }}>{l.remainingQuantity}</td>
                <td style={{ padding: '0.75rem 1rem', color: '#94a3b8' }}>
                  {l.sasDate ? new Date(l.sasDate).toLocaleDateString('tr-TR') : '-'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
