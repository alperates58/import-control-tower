import React from 'react';
import { ImportCaseOperationalSummary } from '../../types/importCase';

interface Props {
  summary: ImportCaseOperationalSummary | null;
}

export const ImportCaseSummaryCards: React.FC<Props> = ({ summary }) => {
  if (!summary) return null;

  const cards = [
    { title: 'Aktif Dosyalar', value: summary.activeCaseCount, color: '#38bdf8' },
    { title: 'Üretimi Geciken', value: summary.productionDelayedCount, color: '#f59e0b' },
    { title: 'Sevk Edilebilir', value: summary.readyForShipmentCount, color: '#10b981' },
    { title: 'Rezervasyon Bekleyen', value: summary.bookingPendingCount, color: '#06b6d4' },
    { title: 'Yoldaki Sevkiyatlar', value: summary.inTransitShipmentCount, color: '#a855f7' },
    { title: 'Geciken Sevkiyatlar', value: summary.delayedShipmentCount, color: '#f43f5e' },
    { title: 'Bu Hafta Varış', value: summary.etaThisWeekCount, color: '#10b981' },
    { title: 'Atanmamış Kalemler', value: summary.unallocatedLineCount, color: '#94a3b8' },
  ];

  return (
    <div className="kpi-grid">
      {cards.map((c, i) => (
        <div key={i} className="kpi-card" style={{ borderLeft: `3px solid ${c.color}` }}>
          <div className="kpi-card-header">
            <span className="kpi-title">{c.title}</span>
          </div>
          <div className="kpi-value" style={{ color: c.color }}>{c.value}</div>
        </div>
      ))}
    </div>
  );
};
