import React from 'react';
import { ImportCaseOperationalSummary } from '../../types/importCase';
import { KPICard } from '../ui/Card';

interface Props {
  summary: ImportCaseOperationalSummary | null;
}

export const ImportCaseSummaryCards: React.FC<Props> = ({ summary }) => {
  if (!summary) return null;

  const cards = [
    { title: 'Aktif Dosyalar', value: summary.activeCaseCount, color: summary.activeCaseCount > 0 ? 'var(--accent-blue)' : 'var(--text-muted)' },
    { title: 'Üretimi Geciken', value: summary.productionDelayedCount, color: summary.productionDelayedCount > 0 ? 'var(--status-warning)' : 'var(--text-muted)' },
    { title: 'Sevk Edilebilir', value: summary.readyForShipmentCount, color: summary.readyForShipmentCount > 0 ? 'var(--status-success)' : 'var(--text-muted)' },
    { title: 'Rezervasyon Bekleyen', value: summary.bookingPendingCount, color: summary.bookingPendingCount > 0 ? 'var(--accent-cyan)' : 'var(--text-muted)' },
    { title: 'Yoldaki Sevkiyatlar', value: summary.inTransitShipmentCount, color: summary.inTransitShipmentCount > 0 ? 'var(--accent-purple)' : 'var(--text-muted)' },
    { title: 'Geciken Sevkiyatlar', value: summary.delayedShipmentCount, color: summary.delayedShipmentCount > 0 ? 'var(--status-danger)' : 'var(--text-muted)' },
    { title: 'Bu Hafta Varış', value: summary.etaThisWeekCount, color: summary.etaThisWeekCount > 0 ? 'var(--status-success)' : 'var(--text-muted)' },
    { title: 'Atanmamış Kalemler', value: summary.unallocatedLineCount, color: summary.unallocatedLineCount > 0 ? 'var(--status-danger)' : 'var(--text-muted)' }
  ];

  return (
    <div className="kpi-grid">
      {cards.map((c, i) => (
        <KPICard
          key={i}
          title={c.title}
          value={c.value}
          valueColor={c.color}
        />
      ))}
    </div>
  );
};
