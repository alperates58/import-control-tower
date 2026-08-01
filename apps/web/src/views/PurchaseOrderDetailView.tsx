import React, { useState, useEffect } from 'react';
import { IconArrowLeft } from '../components/Icons';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { IconButton, Button } from '../components/ui/Button';
import { DataTable, Column } from '../components/ui/DataTable';
import { StatusBadge } from '../components/ui/Badge';
import { Section } from '../components/ui/Card';
import { EmptyState, LoadingSkeleton } from '../components/ui/FeedbackState';

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

      <Section title={`Sipariş Stok Kalemleri (${po.lines?.length || 0})`}>
        <DataTable
          columns={lineColumns}
          data={po.lines || []}
          keyExtractor={(l) => l.id}
          emptyMessage="Bu siparişte henüz kalem bulunmuyor."
        />
      </Section>
    </div>
  );
};
