import React, { useEffect, useState } from 'react';
import { ImportCaseSummary, ImportCaseOperationalSummary } from '../types/importCase';
import { importCaseService } from '../services/importCaseService';
import { ImportCaseSummaryCards } from '../components/import-cases/ImportCaseSummaryCards';
import { ImportCaseCreateModal } from './ImportCaseCreateModal';
import { useAuth } from '../context/AuthContext';

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
      case 'Draft': return <span className="badge badge-cyan">Taslak</span>;
      case 'Active': return <span className="badge badge-emerald">Aktif</span>;
      case 'Completed': return <span className="badge badge-purple">Tamamlandı</span>;
      case 'Closed': return <span className="badge badge-emerald">Kapatıldı</span>;
      case 'Cancelled': return <span className="badge badge-rose">İptal Edildi</span>;
      default: return <span className="badge">{s}</span>;
    }
  };

  const getProductionStatusBadge = (ps: string) => {
    switch (ps) {
      case 'NotStarted': return <span className="badge" style={{ background: 'rgba(148, 163, 184, 0.1)', color: '#94a3b8' }}>Başlamadı</span>;
      case 'InProduction': return <span className="badge badge-amber">Üretimde</span>;
      case 'Completed': return <span className="badge badge-emerald">Üretim Bitti</span>;
      case 'Delayed': return <span className="badge badge-rose">Üretim Gecikti</span>;
      default: return <span className="badge">{ps}</span>;
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
      {/* Header Bar */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h1 style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text-main)', letterSpacing: '-0.02em' }}>
            İthalat Dosyaları ve Sevkiyat Takibi
          </h1>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>
            Faz 03 — İthalat dosyaları, sipariş kalemi tahsisleri ve sevkiyat yönetimi
          </p>
        </div>
        <button
          onClick={() => setCreateModalOpen(true)}
          className="btn-primary"
        >
          <span style={{ fontSize: '1.1rem', lineHeight: 1 }}>+</span>
          <span>Yeni İthalat Dosyası</span>
        </button>
      </div>

      {/* KPI Cards */}
      <ImportCaseSummaryCards summary={summary} />

      {/* Filter Toolbar Panel */}
      <div className="panel" style={{ marginBottom: 0 }}>
        <div className="panel-header" style={{ marginBottom: '1rem', paddingBottom: '0.75rem' }}>
          <div className="panel-title" style={{ fontSize: '0.95rem' }}>
            <span>🔍 Arama ve Filtreleme Toolbar</span>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', alignItems: 'flex-end' }}>
          <div style={{ gridColumn: 'span 2' }}>
            <label className="form-label">Arama</label>
            <input
              type="text"
              placeholder="Dosya No, Başlık veya Tedarikçi Ara..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(1); }}
              className="form-input"
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <label className="form-label">Dosya Durumu</label>
            <select
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="">Tüm Dosya Durumları</option>
              <option value="Draft">Taslak (Draft)</option>
              <option value="Active">Aktif (Active)</option>
              <option value="Completed">Tamamlandı (Completed)</option>
              <option value="Closed">Kapatıldı (Closed)</option>
              <option value="Cancelled">İptal Edildi (Cancelled)</option>
            </select>
          </div>

          <div>
            <label className="form-label">Üretim Durumu</label>
            <select
              value={productionStatus}
              onChange={(e) => { setProductionStatus(e.target.value); setPage(1); }}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="">Tüm Üretim Durumları</option>
              <option value="NotStarted">Başlamadı</option>
              <option value="InProduction">Üretimde</option>
              <option value="Completed">Üretim Bitti</option>
              <option value="Delayed">Gecikti</option>
            </select>
          </div>

          <div>
            <label className="form-label">Taşıma Modu</label>
            <select
              value={defaultTransportMode}
              onChange={(e) => { setDefaultTransportMode(e.target.value); setPage(1); }}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="">Tüm Taşıma Modları</option>
              <option value="Sea">Deniz (Sea)</option>
              <option value="Air">Hava (Air)</option>
              <option value="Road">Kara (Road)</option>
              <option value="Rail">Demiryolu (Rail)</option>
              <option value="Courier">Kurye (Courier)</option>
              <option value="Multimodal">Multimodal</option>
            </select>
          </div>

          <div>
            <label className="form-label">Sıralama</label>
            <select
              value={sort}
              onChange={(e) => setSort(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="createdat">Son Oluşturulan</option>
              <option value="createdat_asc">İlk Oluşturulan</option>
              <option value="casenumber">Dosya No (A-Z)</option>
              <option value="supplier">Tedarikçi (A-Z)</option>
            </select>
          </div>
        </div>

        <div style={{ marginTop: '1rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer', fontSize: '0.82rem', color: 'var(--text-muted)' }}>
            <input
              type="checkbox"
              checked={delayedOnly}
              onChange={(e) => { setDelayedOnly(e.target.checked); setPage(1); }}
              style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
            />
            <span style={{ color: delayedOnly ? 'var(--accent-amber)' : 'inherit', fontWeight: 600 }}>
              ⚠️ Yalnızca Gecikmedeki Dosyaları Göster
            </span>
          </label>
        </div>
      </div>

      {/* Main Table / State Container */}
      <div className="panel">
        {loading ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: 'var(--accent-blue)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '1rem' }}>
            <div style={{ width: '32px', height: '32px', border: '3px solid var(--border-color)', borderTopColor: 'var(--accent-blue)', borderRadius: '50%', animation: 'spin 1s linear infinite' }}></div>
            <div style={{ fontSize: '0.9rem', fontWeight: 600 }}>İthalat dosyaları yükleniyor...</div>
          </div>
        ) : errorMsg ? (
          <div style={{ padding: '1.5rem', background: 'rgba(244, 63, 94, 0.1)', border: '1px solid rgba(244, 63, 94, 0.3)', borderRadius: '10px', color: 'var(--accent-rose)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <div>
              <strong style={{ display: 'block', marginBottom: '0.25rem' }}>Hata Oluştu</strong>
              <span style={{ fontSize: '0.85rem' }}>{errorMsg}</span>
            </div>
            <button onClick={fetchCases} className="btn-secondary btn-sm">Yeniden Denetle</button>
          </div>
        ) : cases.length === 0 ? (
          <div style={{ padding: '4rem 2rem', textAlign: 'center', color: 'var(--text-muted)' }}>
            <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>📁</div>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.4rem' }}>
              İthalat Dosyası Bulunamadı
            </h3>
            <p style={{ fontSize: '0.85rem', maxWidth: '400px', margin: '0 auto 1.25rem' }}>
              Arama kriterlerinize uygun ithalat dosyası bulunamadı veya henüz dosya oluşturulmadı.
            </p>
            <button onClick={() => setCreateModalOpen(true)} className="btn-primary btn-sm">
              + İlk İthalat Dosyasını Oluştur
            </button>
          </div>
        ) : (
          <>
            <div className="data-table-wrapper">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Dosya No</th>
                    <th>Başlık</th>
                    <th>Tedarikçi</th>
                    <th>Mod</th>
                    <th>Incoterm</th>
                    <th>Durum</th>
                    <th>Üretim Durumu</th>
                    <th>Sevkiyat Adedi</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {cases.map((c) => (
                    <tr key={c.id} style={{ cursor: 'pointer' }} onClick={() => onSelectCase(c.id)}>
                      <td style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, color: 'var(--accent-blue)' }}>
                        {c.caseNumber}
                      </td>
                      <td style={{ fontWeight: 600 }}>{c.title}</td>
                      <td style={{ color: 'var(--text-main)' }}>{c.supplierName}</td>
                      <td>
                        <span className="badge" style={{ background: 'rgba(51, 65, 85, 0.4)', color: 'var(--text-main)' }}>
                          {c.defaultTransportMode || '-'}
                        </span>
                      </td>
                      <td style={{ fontFamily: 'var(--font-mono)', fontSize: '0.8rem' }}>{c.incoterm || '-'}</td>
                      <td>{getStatusBadge(c.status)}</td>
                      <td>{getProductionStatusBadge(c.productionStatus)}</td>
                      <td style={{ textAlign: 'center', fontWeight: 700 }}>{c.shipmentCount}</td>
                      <td>
                        <button
                          onClick={(e) => { e.stopPropagation(); onSelectCase(c.id); }}
                          className="btn-secondary btn-sm"
                        >
                          Detay & Detaylar
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination Controls */}
            <div style={{ marginTop: '1.25rem', display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '1rem' }}>
              <div style={{ fontSize: '0.82rem', color: 'var(--text-muted)' }}>
                Toplam <strong>{totalCount}</strong> dosyadan <strong>{(page - 1) * pageSize + 1}</strong> - <strong>{Math.min(page * pageSize, totalCount)}</strong> arası gösteriliyor
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <button
                  disabled={page <= 1}
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  className="btn-secondary btn-sm"
                  style={{ opacity: page <= 1 ? 0.5 : 1 }}
                >
                  Önceki
                </button>
                <span style={{ fontSize: '0.85rem', fontWeight: 600, padding: '0 0.5rem', color: 'var(--text-main)' }}>
                  Sayfa {page} / {totalPages}
                </span>
                <button
                  disabled={page >= totalPages}
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                  className="btn-secondary btn-sm"
                  style={{ opacity: page >= totalPages ? 0.5 : 1 }}
                >
                  Sonraki
                </button>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Real Overlay Dialog Modal */}
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
