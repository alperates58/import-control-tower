import React, { useState, useEffect } from 'react';
import { ImportStatusBadge } from '../components/ImportStatusBadge';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { DataTable, Column, Pagination } from '../components/ui/DataTable';
import { Button } from '../components/ui/Button';

interface ImportHistoryViewProps {
  onSelectBatch: (batchId: string) => void;
}

export const ImportHistoryView: React.FC<ImportHistoryViewProps> = ({ onSelectBatch }) => {
  const { authenticatedFetch } = useAuth();
  const [batches, setBatches] = useState<any[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);

  const fetchHistory = async (p = 1) => {
    setIsLoading(true);
    try {
      const res = await authenticatedFetch(`/api/v1/purchase-order-imports?page=${p}&pageSize=15`);
      if (res.ok) {
        const data = await res.json();
        setBatches(data.items || []);
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
    fetchHistory(page);
  }, [page]);

  const columns: Column<any>[] = [
    {
      key: 'originalFileName',
      header: 'Dosya Adı',
      render: (b) => (
        <span style={{ fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>
          {b.originalFileName}
        </span>
      )
    },
    {
      key: 'status',
      header: 'Durum',
      render: (b) => <ImportStatusBadge status={b.status} />
    },
    {
      key: 'totalRowCount',
      header: 'Satır Sayısı',
      render: (b) => b.totalRowCount
    },
    {
      key: 'importedOrderCount',
      header: 'Oluşturulan PO/Kalem',
      render: (b) => `${b.importedOrderCount} PO / ${b.importedLineCount} Kalem`
    },
    {
      key: 'uploadedByFullName',
      header: 'Yükleyen',
      render: (b) => b.uploadedByFullName || 'Kullanıcı'
    },
    {
      key: 'startedAtUtc',
      header: 'Tarih',
      render: (b) => new Date(b.startedAtUtc).toLocaleString('tr-TR')
    },
    {
      key: 'actions',
      header: 'İşlem',
      align: 'right',
      render: (b) => (
        <Button variant="secondary" size="sm" onClick={() => onSelectBatch(b.id)}>
          Detay / İncele
        </Button>
      )
    }
  ];

  return (
    <div>
      <PageHeader
        title="İçe Aktarma Geçmişi"
        subtitle="Daha önce yüklenmiş olan Excel sipariş dosyalarını ve durumlarını inceleyin."
      />

      <DataTable
        columns={columns}
        data={batches}
        keyExtractor={(b) => b.id}
        isLoading={isLoading}
        onRowClick={(b) => onSelectBatch(b.id)}
        emptyMessage="Henüz bir içe aktarma kaydı bulunmamaktadır."
      />

      {!isLoading && batches.length > 0 && (
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
