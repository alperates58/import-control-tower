import React, { useEffect, useState } from 'react';
import { ImportCaseSummary, ImportCaseOperationalSummary } from '../types/importCase';
import { importCaseService } from '../services/importCaseService';
import { ImportCaseSummaryCards } from '../components/import-cases/ImportCaseSummaryCards';
import { ImportCaseCreateModal } from './ImportCaseCreateModal';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { Button } from '../components/ui/Button';
import { Input, Select, Checkbox, FormField } from '../components/ui/Input';
import { DataTable, Column, Pagination } from '../components/ui/DataTable';
import { Badge } from '../components/ui/Badge';
import { ErrorState } from '../components/ui/FeedbackState';

interface Props {
  onSelectCase: (caseId: string) => void;
}

export const ImportCaseListView: React.FC<Props> = ({ onSelectCase }) => {
  const { authenticatedFetch } = useAuth();
  const [cases, setCases] = useState<ImportCaseSummary[]>([]);
  const [summary, setSummary] = useState<ImportCaseOperationalSummary | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);

  // Filters
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [productionStatus, setProductionStatus] = useState('');
  const [defaultTransportMode, setDefaultTransportMode] = useState('');
  const [delayedOnly, setDelayedOnly] = useState(false);
  const [sort, setSort] = useState('createdat');

  const [loading, setLoading] = useState(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [createModalOpen, setCreateModalOpen] = useState(false);

  const fetchCases = async () => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const data = await importCaseService.getCases({
        page,
        pageSize,
        search: search || undefined,
        status: status || undefined,
        productionStatus: productionStatus || undefined,
        defaultTransportMode: defaultTransportMode || undefined,
        delayedOnly: delayedOnly || undefined,
        sort
      }, authenticatedFetch);
      setCases(data.items || []);
      setTotalCount(data.totalCount || 0);
      setTotalPages(data.totalPages || 1);

      const summaryData = await importCaseService.getSummary(authenticatedFetch);
      setSummary(summaryData);
    } catch (err: any) {
      setErrorMsg(err.message || 'İthalat dosyaları yüklenemedi.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCases();
  }, [page, search, status, productionStatus, defaultTransportMode, delayedOnly, sort]);

  const getStatusBadge = (s: string) => {
    switch (s) {
      case 'Draft': return <Badge variant="cyan">Taslak</Badge>;
      case 'Active': return <Badge variant="emerald">Aktif</Badge>;
      case 'Completed': return <Badge variant="purple">Tamamlandı</Badge>;
      case 'Closed': return <Badge variant="emerald">Kapatıldı</Badge>;
      case 'Cancelled': return <Badge variant="rose">İptal Edildi</Badge>;
      default: return <Badge variant="neutral">{s}</Badge>;
    }
  };

  const getProductionStatusBadge = (ps: string) => {
    switch (ps) {
      case 'NotStarted': return <Badge variant="neutral">Başlamadı</Badge>;
      case 'InProduction': return <Badge variant="amber">Üretimde</Badge>;
      case 'Completed': return <Badge variant="emerald">Üretim Bitti</Badge>;
      case 'Delayed': return <Badge variant="rose">Üretim Gecikti</Badge>;
      default: return <Badge variant="neutral">{ps}</Badge>;
    }
  };

  const columns: Column<ImportCaseSummary>[] = [
    {
      key: 'caseNumber',
      header: 'Dosya No',
      render: (c) => (
        <span className="font-mono" style={{ fontWeight: 'var(--weight-bold)', color: 'var(--accent-blue)' }}>
          {c.caseNumber}
        </span>
      )
    },
    {
      key: 'title',
      header: 'Başlık',
      render: (c) => <span style={{ fontWeight: 'var(--weight-semibold)' }}>{c.title}</span>
    },
    {
      key: 'supplierName',
      header: 'Tedarikçi',
      render: (c) => c.supplierName
    },
    {
      key: 'defaultTransportMode',
      header: 'Mod',
      render: (c) => <Badge variant="neutral">{c.defaultTransportMode || '-'}</Badge>
    },
    {
      key: 'incoterm',
      header: 'Incoterm',
      render: (c) => <span className="font-mono" style={{ fontSize: 'var(--font-xs)' }}>{c.incoterm || '-'}</span>
    },
    {
      key: 'status',
      header: 'Durum',
      render: (c) => getStatusBadge(c.status)
    },
    {
      key: 'productionStatus',
      header: 'Üretim Durumu',
      render: (c) => getProductionStatusBadge(c.productionStatus)
    },
    {
      key: 'shipmentCount',
      header: 'Sevkiyat Adedi',
      align: 'center',
      render: (c) => <strong>{c.shipmentCount}</strong>
    },
    {
      key: 'actions',
      header: 'İşlemler',
      align: 'right',
      render: (c) => (
        <Button
          variant="secondary"
          size="sm"
          onClick={(e) => { e.stopPropagation(); onSelectCase(c.id); }}
        >
          İncele
        </Button>
      )
    }
  ];

  return (
    <div>
      <PageHeader
        title="İthalat Dosyaları ve Sevkiyat Takibi"
        subtitle="İthalat dosyaları, sipariş kalemi tahsisleri ve sevkiyat yönetimi"
        actions={
          <Button variant="primary" onClick={() => setCreateModalOpen(true)}>
            + Yeni İthalat Dosyası
          </Button>
        }
      />

      <ImportCaseSummaryCards summary={summary} />

      <div className="panel" style={{ marginBottom: 'var(--space-4)' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 'var(--space-3)', alignItems: 'flex-end' }}>
          <div style={{ gridColumn: 'span 2' }}>
            <FormField label="Arama">
              <Input
                type="text"
                placeholder="Dosya No, Başlık veya Tedarikçi Ara..."
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPage(1); }}
              />
            </FormField>
          </div>

          <FormField label="Dosya Durumu">
            <Select
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
              options={[
                { value: '', label: 'Tüm Dosya Durumları' },
                { value: 'Draft', label: 'Taslak (Draft)' },
                { value: 'Active', label: 'Aktif (Active)' },
                { value: 'Completed', label: 'Tamamlandı (Completed)' },
                { value: 'Closed', label: 'Kapatıldı (Closed)' },
                { value: 'Cancelled', label: 'İptal Edildi (Cancelled)' }
              ]}
            />
          </FormField>

          <FormField label="Üretim Durumu">
            <Select
              value={productionStatus}
              onChange={(e) => { setProductionStatus(e.target.value); setPage(1); }}
              options={[
                { value: '', label: 'Tüm Üretim Durumları' },
                { value: 'NotStarted', label: 'Başlamadı' },
                { value: 'InProduction', label: 'Üretimde' },
                { value: 'Completed', label: 'Üretim Bitti' },
                { value: 'Delayed', label: 'Gecikti' }
              ]}
            />
          </FormField>

          <FormField label="Taşıma Modu">
            <Select
              value={defaultTransportMode}
              onChange={(e) => { setDefaultTransportMode(e.target.value); setPage(1); }}
              options={[
                { value: '', label: 'Tüm Taşıma Modları' },
                { value: 'Sea', label: 'Deniz (Sea)' },
                { value: 'Air', label: 'Hava (Air)' },
                { value: 'Road', label: 'Kara (Road)' },
                { value: 'Rail', label: 'Demiryolu (Rail)' },
                { value: 'Courier', label: 'Kurye (Courier)' },
                { value: 'Multimodal', label: 'Multimodal' }
              ]}
            />
          </FormField>

          <FormField label="Sıralama">
            <Select
              value={sort}
              onChange={(e) => setSort(e.target.value)}
              options={[
                { value: 'createdat', label: 'Son Oluşturulan' },
                { value: 'createdat_asc', label: 'İlk Oluşturulan' },
                { value: 'casenumber', label: 'Dosya No (A-Z)' },
                { value: 'supplier', label: 'Tedarikçi (A-Z)' }
              ]}
            />
          </FormField>
        </div>

        <div style={{ marginTop: 'var(--space-3)', paddingTop: 'var(--space-3)', borderTop: '1px solid var(--border-subtle)' }}>
          <Checkbox
            checked={delayedOnly}
            onChange={(e) => { setDelayedOnly(e.target.checked); setPage(1); }}
            label="⚠️ Yalnızca Gecikmedeki Dosyaları Göster"
          />
        </div>
      </div>

      {errorMsg ? (
        <ErrorState description={errorMsg} onRetry={fetchCases} />
      ) : (
        <>
          <DataTable
            columns={columns}
            data={cases}
            keyExtractor={(c) => c.id}
            isLoading={loading}
            onRowClick={(c) => onSelectCase(c.id)}
            emptyMessage="Arama kriterlerinize uygun ithalat dosyası bulunamadı."
          />

          {!loading && cases.length > 0 && (
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              totalCount={totalCount}
              onPageChange={(p) => setPage(p)}
            />
          )}
        </>
      )}

      <ImportCaseCreateModal
        isOpen={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        onSuccess={(caseId) => {
          setCreateModalOpen(false);
          fetchCases();
          onSelectCase(caseId);
        }}
      />
    </div>
  );
};
