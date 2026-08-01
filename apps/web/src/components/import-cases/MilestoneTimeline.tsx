import React, { useState } from 'react';
import { ShipmentMilestone } from '../../types/importCase';
import { importCaseService } from '../../services/importCaseService';
import { useAuth } from '../../context/AuthContext';
import { Button } from '../ui/Button';
import { Input, Select, FormField } from '../ui/Input';
import { Badge } from '../ui/Badge';
import { Section } from '../ui/Card';

interface Props {
  shipmentId: string;
  milestones: ShipmentMilestone[];
  onRefresh: () => void;
}

export const MilestoneTimeline: React.FC<Props> = ({
  shipmentId,
  milestones,
  onRefresh
}) => {
  const { authenticatedFetch } = useAuth();
  const [milestoneType, setMilestoneType] = useState('GateIn');
  const [locationName, setLocationName] = useState('');
  const [timezoneId, setTimezoneId] = useState('Europe/Istanbul');
  const [plannedAt, setPlannedAt] = useState('');
  const [actualAt, setActualAt] = useState('');

  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleAddMilestone = async () => {
    setErrorMsg(null);
    setLoading(true);
    try {
      const nextSeq = milestones.length > 0 ? Math.max(...milestones.map(m => m.sequenceNumber)) + 1 : 10;
      await importCaseService.createMilestone(shipmentId, {
        sequenceNumber: nextSeq,
        milestoneType,
        locationName: locationName || null,
        timezoneId,
        plannedAt: plannedAt || null,
        actualAt: actualAt || null,
        status: actualAt ? 'Completed' : 'Pending'
      }, authenticatedFetch);

      setLocationName('');
      setPlannedAt('');
      setActualAt('');
      onRefresh();
    } catch (err: any) {
      setErrorMsg(err.message || 'Milestone eklenemedi.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
      {errorMsg && (
        <div style={{ marginBottom: 'var(--space-2)' }}>
          <Badge variant="rose" style={{ width: '100%', padding: '0.75rem' }}>
            ⚠️ {errorMsg}
          </Badge>
        </div>
      )}

      <Section title="📍 Kilometre Taşı (Milestone Event) Girişi">
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 'var(--space-3)', alignItems: 'flex-end' }}>
          <FormField label="Aşama / Event *" required>
            <Select
              value={milestoneType}
              onChange={(e) => setMilestoneType(e.target.value)}
              options={[
                { value: 'BookingConfirmed', label: 'Booking Confirmed' },
                { value: 'CargoReceived', label: 'Cargo Received' },
                { value: 'GateIn', label: 'Gate In (Liman Giriş)' },
                { value: 'VesselLoaded', label: 'Vessel Loaded (Yükleme)' },
                { value: 'DepartureFromPort', label: 'Departure From Port (Kalkış)' },
                { value: 'Transshipment', label: 'Transshipment (Aktarma)' },
                { value: 'ArrivalAtPort', label: 'Arrival At Port (Varış)' },
                { value: 'Discharged', label: 'Discharged (Tahliye)' },
                { value: 'CustomsCleared', label: 'Customs Cleared (Gümrük)' },
                { value: 'GateOut', label: 'Gate Out (Liman Çıkış)' },
                { value: 'WarehouseDelivered', label: 'Warehouse Delivered (Depo Varış)' }
              ]}
            />
          </FormField>

          <FormField label="Lokasyon">
            <Input
              type="text"
              placeholder="Örn: Ningbo Port / Ambarlı"
              value={locationName}
              onChange={(e) => setLocationName(e.target.value)}
            />
          </FormField>

          <FormField label="Zaman Dilimi (IANA)">
            <Select
              value={timezoneId}
              onChange={(e) => setTimezoneId(e.target.value)}
              options={[
                { value: 'Europe/Istanbul', label: 'Europe/Istanbul (+03:00)' },
                { value: 'Asia/Shanghai', label: 'Asia/Shanghai (+08:00)' },
                { value: 'Europe/Berlin', label: 'Europe/Berlin (+01:00/+02:00)' },
                { value: 'UTC', label: 'UTC (+00:00)' }
              ]}
            />
          </FormField>

          <FormField label="Planlanan Tarih/Saat">
            <Input
              type="datetime-local"
              value={plannedAt}
              onChange={(e) => setPlannedAt(e.target.value)}
            />
          </FormField>

          <FormField label="Gerçekleşen Tarih/Saat">
            <Input
              type="datetime-local"
              value={actualAt}
              onChange={(e) => setActualAt(e.target.value)}
            />
          </FormField>

          <Button
            disabled={loading}
            onClick={handleAddMilestone}
            variant="primary"
            isLoading={loading}
            style={{ width: '100%', justifyContent: 'center' }}
          >
            + Aşama Ekle
          </Button>
        </div>
      </Section>

      {/* Timeline Display */}
      {milestones.length === 0 ? (
        <div style={{ padding: 'var(--space-6)', textAlign: 'center', color: 'var(--text-muted)', fontSize: 'var(--font-sm)' }}>
          Henüz sevkiyata ait aşama kaydı bulunmamaktadır.
        </div>
      ) : (
        <div style={{ position: 'relative', paddingLeft: '1.5rem', borderLeft: '2px dashed var(--border-subtle)', margin: 'var(--space-3) var(--space-2)', display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
          {milestones.sort((a, b) => a.sequenceNumber - b.sequenceNumber).map((m) => (
            <div key={m.id} style={{ position: 'relative' }}>
              <div style={{
                position: 'absolute',
                left: '-1.95rem',
                top: '0.2rem',
                width: '12px',
                height: '12px',
                borderRadius: '50%',
                background: m.status === 'Completed' ? 'var(--status-success)' : 'var(--bg-surface)',
                border: `2px solid ${m.status === 'Completed' ? 'var(--status-success)' : 'var(--accent-blue)'}`
              }} />

              <div className="panel" style={{ padding: 'var(--space-3) var(--space-4)', marginBottom: 0, background: 'var(--bg-card)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-2)' }}>
                  <span style={{ fontWeight: 'var(--weight-bold)', fontSize: 'var(--font-sm)', color: 'var(--text-main)' }}>
                    {m.milestoneType}
                  </span>
                  <Badge variant={m.status === 'Completed' ? 'emerald' : 'cyan'}>
                    {m.status}
                  </Badge>
                </div>

                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 'var(--space-4)', fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>
                  {m.locationName && <span>📍 Lokasyon: <strong style={{ color: 'var(--text-main)' }}>{m.locationName}</strong></span>}
                  <span>🕒 Zaman Dilimi: <code className="font-mono">{m.timezoneId}</code></span>
                  {m.plannedAtUtc && <span>📅 Planlanan: {new Date(m.plannedAtUtc).toLocaleString('tr-TR')}</span>}
                  {m.actualAtUtc && <span>✅ Gerçekleşen: <strong style={{ color: 'var(--status-success)' }}>{new Date(m.actualAtUtc).toLocaleString('tr-TR')}</strong></span>}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
