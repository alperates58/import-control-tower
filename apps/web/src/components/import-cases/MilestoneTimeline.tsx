import React, { useState } from 'react';
import { ShipmentMilestone } from '../../types/importCase';
import { importCaseService } from '../../services/importCaseService';
import { useAuth } from '../../context/AuthContext';

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
    <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
      {errorMsg && (
        <div style={{ padding: '0.85rem 1rem', background: 'rgba(244, 63, 94, 0.1)', border: '1px solid rgba(244, 63, 94, 0.3)', borderRadius: '8px', color: 'var(--accent-rose)', fontSize: '0.85rem' }}>
          ⚠️ {errorMsg}
        </div>
      )}

      {/* Milestone Form Card */}
      <div className="panel" style={{ background: 'rgba(15, 23, 42, 0.5)', padding: '1.25rem', marginBottom: 0 }}>
        <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <span>📍</span> Kilometre Taşı (Milestone Event) Girişi
        </h4>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '1rem', alignItems: 'flex-end' }}>
          <div>
            <label className="form-label">Aşama / Event *</label>
            <select
              value={milestoneType}
              onChange={(e) => setMilestoneType(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="BookingConfirmed">Booking Confirmed</option>
              <option value="CargoReceived">Cargo Received</option>
              <option value="GateIn">Gate In (Liman Giriş)</option>
              <option value="VesselLoaded">Vessel Loaded (Yükleme)</option>
              <option value="DepartureFromPort">Departure From Port (Kalkış)</option>
              <option value="Transshipment">Transshipment (Aktarma)</option>
              <option value="ArrivalAtPort">Arrival At Port (Varış)</option>
              <option value="Discharged">Discharged (Tahliye)</option>
              <option value="CustomsCleared">Customs Cleared (Gümrük)</option>
              <option value="GateOut">Gate Out (Liman Çıkış)</option>
              <option value="WarehouseDelivered">Warehouse Delivered (Depo Varış)</option>
            </select>
          </div>

          <div>
            <label className="form-label">Lokasyon</label>
            <input
              type="text"
              placeholder="Örn: Ningbo Port / Ambarlı"
              value={locationName}
              onChange={(e) => setLocationName(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <label className="form-label">Zaman Dilimi (IANA)</label>
            <select
              value={timezoneId}
              onChange={(e) => setTimezoneId(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            >
              <option value="Europe/Istanbul">Europe/Istanbul (+03:00)</option>
              <option value="Asia/Shanghai">Asia/Shanghai (+08:00)</option>
              <option value="Europe/Berlin">Europe/Berlin (+01:00/+02:00)</option>
              <option value="UTC">UTC (+00:00)</option>
            </select>
          </div>

          <div>
            <label className="form-label">Planlanan Tarih/Saat</label>
            <input
              type="datetime-local"
              value={plannedAt}
              onChange={(e) => setPlannedAt(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <label className="form-label">Gerçekleşen Tarih/Saat</label>
            <input
              type="datetime-local"
              value={actualAt}
              onChange={(e) => setActualAt(e.target.value)}
              className="form-input"
              style={{ width: '100%' }}
            />
          </div>

          <div>
            <button
              disabled={loading}
              onClick={handleAddMilestone}
              className="btn-primary"
              style={{ width: '100%', justifyContent: 'center' }}
            >
              {loading ? 'Kaydediliyor...' : '+ Aşama Ekle'}
            </button>
          </div>
        </div>
      </div>

      {/* Timeline Display */}
      {milestones.length === 0 ? (
        <div style={{ padding: '2rem', textAlign: 'center', color: 'var(--text-muted)', fontSize: '0.88rem' }}>
          Henüz sevkiyata ait aşama kaydı bulunmamaktadır.
        </div>
      ) : (
        <div style={{ position: 'relative', paddingLeft: '1.5rem', borderLeft: '2px dashed var(--border-color)', margin: '1rem 0.5rem', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
          {milestones.sort((a, b) => a.sequenceNumber - b.sequenceNumber).map((m) => (
            <div key={m.id} style={{ position: 'relative' }}>
              {/* Bullet Node */}
              <div style={{
                position: 'absolute',
                left: '-1.95rem',
                top: '0.2rem',
                width: '14px',
                height: '14px',
                borderRadius: '50%',
                background: m.status === 'Completed' ? 'var(--accent-emerald)' : m.status === 'InProgress' ? 'var(--accent-amber)' : 'var(--bg-surface)',
                border: `3px solid ${m.status === 'Completed' ? 'var(--accent-emerald)' : 'var(--accent-blue)'}`,
                boxShadow: m.status === 'Completed' ? '0 0 10px var(--accent-emerald)' : 'none'
              }} />

              <div className="panel" style={{ padding: '1rem 1.25rem', marginBottom: 0, background: 'rgba(30, 41, 59, 0.4)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.4rem' }}>
                  <span style={{ fontWeight: 700, fontSize: '0.95rem', color: 'var(--text-main)' }}>
                    {m.milestoneType}
                  </span>
                  <span className={`badge ${m.status === 'Completed' ? 'badge-emerald' : 'badge-cyan'}`}>
                    {m.status}
                  </span>
                </div>

                <div style={{ display: 'flex', flexWrap: 'wrap', gap: '1.5rem', fontSize: '0.82rem', color: 'var(--text-muted)' }}>
                  {m.locationName && <span>📍 Lokasyon: <strong style={{ color: 'var(--text-main)' }}>{m.locationName}</strong></span>}
                  <span>🕒 Zaman Dilimi: <code>{m.timezoneId}</code></span>
                  {m.plannedAtUtc && <span>📅 Planlanan: {new Date(m.plannedAtUtc).toLocaleString('tr-TR')}</span>}
                  {m.actualAtUtc && <span>✅ Gerçekleşen: <strong style={{ color: 'var(--accent-emerald)' }}>{new Date(m.actualAtUtc).toLocaleString('tr-TR')}</strong></span>}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
