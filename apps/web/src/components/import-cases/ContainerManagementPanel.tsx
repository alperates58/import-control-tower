import React, { useState } from 'react';
import { ShipmentContainer } from '../../types/importCase';
import { importCaseService } from '../../services/importCaseService';
import { useAuth } from '../../context/AuthContext';
import { Button } from '../ui/Button';
import { Input, Select, Textarea, FormField } from '../ui/Input';
import { DataTable, Column } from '../ui/DataTable';
import { Badge } from '../ui/Badge';
import { Modal } from '../ui/Modal';
import { Section } from '../ui/Card';

interface Props {
  shipmentId: string;
  transportMode: string;
  containers: ShipmentContainer[];
  onRefresh: () => void;
}

export const ContainerManagementPanel: React.FC<Props> = ({
  shipmentId,
  transportMode,
  containers,
  onRefresh
}) => {
  const { authenticatedFetch } = useAuth();
  const [containerNumber, setContainerNumber] = useState('');
  const [containerType, setContainerType] = useState('40HC');
  const [sealNumber, setSealNumber] = useState('');
  const [grossWeightKg, setGrossWeightKg] = useState('');
  const [netWeightKg, setNetWeightKg] = useState('');
  const [packageCount, setPackageCount] = useState('');
  const [notes, setNotes] = useState('');

  const [overrideModalOpen, setOverrideModalOpen] = useState(false);
  const [overrideReason, setOverrideReason] = useState('');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const isSeaOrMultimodal = transportMode === 'Sea' || transportMode === 'Multimodal';

  const handleAddContainer = async (override: boolean = false) => {
    setErrorMsg(null);
    setLoading(true);

    try {
      await importCaseService.addContainer(shipmentId, {
        containerNumber,
        containerType,
        sealNumber: sealNumber || null,
        grossWeightKg: grossWeightKg ? parseFloat(grossWeightKg) : null,
        netWeightKg: netWeightKg ? parseFloat(netWeightKg) : null,
        packageCount: packageCount ? parseInt(packageCount) : null,
        overrideCheckDigit: override,
        overrideReason: override ? overrideReason : null,
        notes: notes || null
      }, authenticatedFetch);

      setContainerNumber('');
      setSealNumber('');
      setGrossWeightKg('');
      setNetWeightKg('');
      setPackageCount('');
      setNotes('');
      setOverrideModalOpen(false);
      setOverrideReason('');
      onRefresh();
    } catch (err: any) {
      if (err.message.includes('CONTAINER_CHECK_DIGIT_INVALID')) {
        setOverrideModalOpen(true);
      } else {
        setErrorMsg(err.message || 'Konteyner eklenemedi.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleCancelContainer = async (containerId: string) => {
    if (!confirm('Bu konteyneri kaldırmak istediğinize emin misiniz?')) return;
    try {
      await importCaseService.cancelContainer(shipmentId, containerId, authenticatedFetch);
      onRefresh();
    } catch (err: any) {
      alert(err.message);
    }
  };

  if (!isSeaOrMultimodal) {
    return (
      <div style={{ padding: 'var(--space-3) var(--space-4)', background: 'var(--status-warning-bg)', border: '1px solid var(--status-warning-border)', borderRadius: 'var(--radius-md)', color: 'var(--status-warning)', fontSize: 'var(--font-sm)' }}>
        ⚠️ Konteyner ekleme yalnızca Deniz (Sea) veya Multimodal taşımalarda geçerlidir. ({transportMode} taşımasında konteyner kullanılamaz).
      </div>
    );
  }

  const columns: Column<ShipmentContainer>[] = [
    {
      key: 'normalizedContainerNumber',
      header: 'Konteyner No',
      render: (c) => (
        <span className="font-mono" style={{ fontWeight: 'var(--weight-bold)', color: 'var(--accent-cyan)' }}>
          {c.normalizedContainerNumber}
        </span>
      )
    },
    {
      key: 'containerType',
      header: 'Tip',
      render: (c) => <Badge variant="cyan">{c.containerType}</Badge>
    },
    {
      key: 'sealNumber',
      header: 'Mühür No',
      render: (c) => c.sealNumber || '-'
    },
    {
      key: 'grossWeightKg',
      header: 'Brüt Ağırlık',
      render: (c) => (c.grossWeightKg ? `${c.grossWeightKg.toLocaleString()} kg` : '-')
    },
    {
      key: 'status',
      header: 'Durum',
      render: (c) => (c.status === 'Active' ? <Badge variant="emerald">Aktif</Badge> : <Badge variant="rose">İptal Edildi</Badge>)
    },
    {
      key: 'actions',
      header: 'İşlem',
      align: 'right',
      render: (c) => (
        c.status === 'Active' && (
          <Button variant="danger" size="sm" onClick={() => handleCancelContainer(c.id)}>
            Kaldır
          </Button>
        )
      )
    }
  ];

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
      {errorMsg && (
        <div style={{ marginBottom: 'var(--space-2)' }}>
          <Badge variant="rose" style={{ width: '100%', padding: '0.75rem' }}>
            ⚠️ {errorMsg}
          </Badge>
        </div>
      )}

      <Section title="📦 Yeni Konteyner Ekle (ISO 6346 Doğrulamalı)">
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 'var(--space-3)', alignItems: 'flex-end' }}>
          <FormField label="Konteyner No *" required>
            <Input
              type="text"
              placeholder="Örn: CSQU3054383"
              value={containerNumber}
              onChange={(e) => setContainerNumber(e.target.value.toUpperCase())}
              className="font-mono"
            />
          </FormField>

          <FormField label="Konteyner Tipi *" required>
            <Select
              value={containerType}
              onChange={(e) => setContainerType(e.target.value)}
              options={[
                { value: '20DV', label: '20DV (Standard)' },
                { value: '40DV', label: '40DV (Standard)' },
                { value: '40HC', label: '40HC (High Cube)' },
                { value: '45HC', label: '45HC (High Cube)' },
                { value: '20RF', label: '20RF (Reefer)' },
                { value: '40RF', label: '40RF (Reefer)' },
                { value: '20OT', label: '20OT (Open Top)' },
                { value: '40OT', label: '40OT (Open Top)' },
                { value: '20FR', label: '20FR (Flat Rack)' },
                { value: '40FR', label: '40FR (Flat Rack)' }
              ]}
            />
          </FormField>

          <FormField label="Mühür No (Seal No)">
            <Input
              type="text"
              placeholder="Örn: SEAL-998877"
              value={sealNumber}
              onChange={(e) => setSealNumber(e.target.value)}
            />
          </FormField>

          <FormField label="Brüt Ağırlık (KG)">
            <Input
              type="number"
              placeholder="Örn: 22000"
              value={grossWeightKg}
              onChange={(e) => setGrossWeightKg(e.target.value)}
            />
          </FormField>

          <Button
            disabled={loading || !containerNumber}
            onClick={() => handleAddContainer(false)}
            variant="primary"
            isLoading={loading}
            style={{ width: '100%', justifyContent: 'center' }}
          >
            + Konteyner Ekle
          </Button>
        </div>
      </Section>

      <DataTable
        columns={columns}
        data={containers}
        keyExtractor={(c) => c.id}
        emptyMessage="Henüz bu sevkiyata bağlı konteyner bulunmuyor."
      />

      <Modal
        isOpen={overrideModalOpen}
        onClose={() => setOverrideModalOpen(false)}
        title={<span style={{ color: 'var(--status-warning)' }}>⚠️ ISO 6346 Check Digit Uyarısı</span>}
        footer={
          <>
            <Button variant="secondary" onClick={() => setOverrideModalOpen(false)}>
              İptal
            </Button>
            <Button
              disabled={!overrideReason.trim()}
              onClick={() => handleAddContainer(true)}
              variant="primary"
              style={{ background: 'var(--status-warning)', color: '#000' }}
            >
              Gerekçeli Onayla ve Ekle
            </Button>
          </>
        }
      >
        <p style={{ fontSize: 'var(--font-sm)', color: 'var(--text-main)', marginBottom: 'var(--space-4)', lineHeight: 'var(--lh-normal)' }}>
          Girdiğiniz <strong>{containerNumber}</strong> konteyner numarası ISO 6346 standart kontrol basamağı algoritmasını geçemedi. Kontrol basamağını atlayıp (Override) yine de eklemek istiyorsanız bir audit gerekçesi belirtiniz.
        </p>
        <FormField label="Audit Geçersiz Kılma Gerekçesi *" required>
          <Textarea
            rows={3}
            required
            placeholder="Örn: Taşıyıcı konşimentosunda geçen hatalı numara doğrulandı..."
            value={overrideReason}
            onChange={(e) => setOverrideReason(e.target.value)}
          />
        </FormField>
      </Modal>
    </div>
  );
};
