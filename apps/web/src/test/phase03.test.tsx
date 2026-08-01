import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ImportCaseSummaryCards } from '../components/import-cases/ImportCaseSummaryCards';
import { ContainerManagementPanel } from '../components/import-cases/ContainerManagementPanel';
import { MilestoneTimeline } from '../components/import-cases/MilestoneTimeline';
import { AuthProvider } from '../context/AuthContext';
import { ImportCaseOperationalSummary, ShipmentContainer, ShipmentMilestone } from '../types/importCase';

describe('Frontend Phase 03 Component Suite', () => {
  it('1. ImportCaseSummaryCards - renders operational summary counts', () => {
    const summary: ImportCaseOperationalSummary = {
      activeCaseCount: 12,
      productionDelayedCount: 3,
      readyForShipmentCount: 4,
      bookingPendingCount: 2,
      inTransitShipmentCount: 5,
      delayedShipmentCount: 1,
      etaThisWeekCount: 8,
      unallocatedLineCount: 15
    };

    render(<ImportCaseSummaryCards summary={summary} />);

    expect(screen.getByText('12')).toBeInTheDocument();
    expect(screen.getByText('Aktif Dosyalar')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('Üretimi Geciken')).toBeInTheDocument();
    expect(screen.getByText('Yoldaki Sevkiyatlar')).toBeInTheDocument();
  });

  it('2. ContainerManagementPanel - renders container list and sea transport validation', () => {
    const containers: ShipmentContainer[] = [
      {
        id: 'cont-1',
        shipmentId: 'ship-1',
        containerNumber: 'MSC U123456 7',
        normalizedContainerNumber: 'MSCU1234567',
        containerType: '40HC',
        sealNumber: 'SEAL-999',
        grossWeightKg: 24000,
        netWeightKg: 20000,
        packageCount: 1500,
        status: 'Assigned',
        createdAtUtc: '2026-07-30T00:00:00Z',
        updatedAtUtc: '2026-07-30T00:00:00Z',
        rowVersion: 1
      }
    ];

    render(
      <AuthProvider>
        <ContainerManagementPanel
          shipmentId="ship-1"
          transportMode="Sea"
          containers={containers}
          onRefresh={vi.fn()}
        />
      </AuthProvider>
    );

    expect(screen.getByText('MSCU1234567')).toBeInTheDocument();
    expect(screen.getByText('40HC')).toBeInTheDocument();
    expect(screen.getByText('SEAL-999')).toBeInTheDocument();
    expect(screen.getByText(/Yeni Konteyner Ekle/i)).toBeInTheDocument();
  });

  it('3. ContainerManagementPanel - hides container creation for non-sea transport modes', () => {
    render(
      <AuthProvider>
        <ContainerManagementPanel
          shipmentId="ship-1"
          transportMode="Air"
          containers={[]}
          onRefresh={vi.fn()}
        />
      </AuthProvider>
    );

    expect(screen.getByText(/Air/)).toBeInTheDocument();
    expect(screen.getByText(/taşımasında konteyner kullanılamaz/i)).toBeInTheDocument();
    expect(screen.queryByText('Yeni Konteyner Ekle (ISO 6346)')).not.toBeInTheDocument();
  });

  it('4. MilestoneTimeline - renders shipment milestones timeline', () => {
    const milestones: ShipmentMilestone[] = [
      {
        id: 'm-1',
        shipmentId: 'ship-1',
        sequenceNumber: 10,
        milestoneType: 'DepartureFromPort',
        locationName: 'Port of Shanghai',
        timezoneId: 'Asia/Shanghai',
        plannedAtUtc: '2026-07-01T00:00:00Z',
        estimatedAtUtc: '2026-07-02T00:00:00Z',
        actualAtUtc: '2026-07-02T05:00:00Z',
        status: 'Completed',
        source: 'Manual',
        createdAtUtc: '2026-07-30T00:00:00Z',
        updatedAtUtc: '2026-07-30T00:00:00Z',
        rowVersion: 1
      }
    ];

    render(
      <AuthProvider>
        <MilestoneTimeline
          shipmentId="ship-1"
          milestones={milestones}
          onRefresh={vi.fn()}
        />
      </AuthProvider>
    );

    expect(screen.getByText(/Port of Shanghai/i)).toBeInTheDocument();
    expect(screen.getByText('DepartureFromPort')).toBeInTheDocument();
    expect(screen.getByText('Completed')).toBeInTheDocument();
  });
});
