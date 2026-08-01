import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { SearchInput } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { DataTable, Column, Pagination } from '../components/ui/DataTable';
import { StatusBadge } from '../components/ui/Badge';

interface PurchaseOrderListViewProps {
  onSelectOrder: (orderId: string) => void;
}

export const PurchaseOrderListView: React.FC<PurchaseOrderListViewProps> = ({ onSelectOrder }) => {
  const { authenticatedFetch } = useAuth();
  const [orders, setOrders] = useState<any[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  const fetchOrders = async (p = 1, q = search) => {
    setIsLoading(true);
    try {
      let url = `/api/v1/purchase-orders?page=${p}&pageSize=15`;
      if (q) url += `&search=${encodeURIComponent(q)}`;
      const res = await authenticatedFetch(url);
      if (res.ok) {
        const data = await res.json();
        setOrders(data.items || []);
        setTotalPages(data.totalPages || 1);
        setTotalCount(data.totalCount || (data.items ? data.items.length : 0));
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

  const columns: Column<any>[] = [
    {
      key: 'orderNumber',
      header: 'Sipariş No',
      render: (po) => (
        <span className="font-mono" style={{ fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>
          {po.orderNumber}
        </span>
      )
    },
    {
      key: 'supplierName',
      header: 'Firma (Tedarikçi)',
      render: (po) => po.supplierName
    },
    {
      key: 'orderDate',
      header: 'Sipariş Tarihi',
      render: (po) => new Date(po.orderDate).toLocaleDateString('tr-TR')
    },
    {
      key: 'lineCount',
      header: 'Kalem Sayısı',
      render: (po) => `${po.lineCount} Kalem`
    },
    {
      key: 'totalOrderedQuantity',
      header: 'Toplam Miktar',
      align: 'right',
      render: (po) => (
        <span style={{ color: 'var(--status-success)', fontWeight: 'var(--weight-semibold)' }}>
          {po.totalOrderedQuantity}
        </span>
      )
    },
    {
      key: 'totalRemainingQuantity',
      header: 'Toplam Kalan Miktar',
      align: 'right',
      render: (po) => (
        <span style={{ color: 'var(--status-warning)', fontWeight: 'var(--weight-semibold)' }}>
          {po.totalRemainingQuantity}
        </span>
      )
    },
    {
      key: 'status',
      header: 'Durum',
      render: (po) => <StatusBadge status={po.status} />
    },
    {
      key: 'actions',
      header: 'Detay',
      align: 'right',
      render: (po) => (
        <Button variant="secondary" size="sm" onClick={() => onSelectOrder(po.id)}>
          İncele
        </Button>
      )
    }
  ];

  return (
    <div>
      <PageHeader
        title="Satın Alma Siparişleri"
        subtitle="Sistemde kayıtlı açık ve tamamlanmış tüm satın alma siparişleri listesi."
        actions={
          <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.5rem', width: '320px' }}>
            <SearchInput
              placeholder="Sipariş No veya Tedarikçi Ara..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <Button type="submit" variant="primary" size="sm">
              Ara
            </Button>
          </form>
        }
      />

      <DataTable
        columns={columns}
        data={orders}
        keyExtractor={(po) => po.id}
        isLoading={isLoading}
        onRowClick={(po) => onSelectOrder(po.id)}
        emptyMessage="Kayıtlı satın alma siparişi bulunamadı."
      />

      {!isLoading && orders.length > 0 && (
        <Pagination
          currentPage={page}
          totalPages={totalPages}
          totalCount={totalCount}
          onPageChange={(newPage) => setPage(newPage)}
        />
      )}
    </div>
  );
};
