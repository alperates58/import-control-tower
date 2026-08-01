import React, { useState, useEffect } from 'react';
import { SupplierLookup } from '../types/importCase';
import { importCaseService } from '../services/importCaseService';
import { useAuth } from '../context/AuthContext';
import { Modal } from '../components/ui/Modal';
import { Button } from '../components/ui/Button';
import { Input, Select, Textarea, FormField } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (caseId: string) => void;
}

export const ImportCaseCreateModal: React.FC<Props> = ({ isOpen, onClose, onSuccess }) => {
  const { authenticatedFetch } = useAuth();
  const [title, setTitle] = useState('');
  const [supplierName, setSupplierName] = useState('');
  const [defaultTransportMode, setDefaultTransportMode] = useState('Sea');
  const [originCountry, setOriginCountry] = useState('Çin');
  const [incoterm, setIncoterm] = useState('FOB');
  const [estimatedProductionCompletionDate, setEstimatedProductionCompletionDate] = useState('');
  const [notes, setNotes] = useState('');

  const [suppliers, setSuppliers] = useState<SupplierLookup[]>([]);
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      importCaseService.getAvailableSuppliers(undefined, authenticatedFetch).then(setSuppliers).catch(() => {});
    }
  }, [isOpen, authenticatedFetch]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);

    if (!title.trim() || !supplierName) {
      setErrorMsg('Başlık ve Tedarikçi seçimi zorunludur.');
      return;
    }

    setLoading(true);
    const idempotencyKey = `case-create-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;

    try {
      const created = await importCaseService.createCase({
        title: title.trim(),
        supplierName,
        defaultTransportMode,
        originCountry: originCountry || null,
        incoterm: incoterm || null,
        estimatedProductionCompletionDate: estimatedProductionCompletionDate || null,
        notes: notes || null
      }, idempotencyKey, authenticatedFetch);

      onSuccess(created.id);
    } catch (err: any) {
      setErrorMsg(err.message || 'İthalat dosyası oluşturulamadı.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="📁 Yeni İthalat Dosyası Oluştur"
      maxWidth="640px"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={loading}>
            Vazgeç
          </Button>
          <Button type="submit" form="create-case-form" variant="primary" isLoading={loading}>
            {loading ? 'Oluşturuluyor...' : 'Dosya Oluştur'}
          </Button>
        </>
      }
    >
      <form id="create-case-form" onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {errorMsg && (
          <Badge variant="rose" style={{ width: '100%', padding: '0.75rem' }}>
            ⚠️ {errorMsg}
          </Badge>
        )}

        <FormField label="Dosya Başlığı *" required>
          <Input
            type="text"
            required
            placeholder="Örn: 2026 Q3 Hammadde ve Aksam İthalatı"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
        </FormField>

        <FormField label="Tedarikçi Seçimi * (Single Supplier)" required helpText="İthalat dosyası kural gereği tek bir tedarikçiye ait siparişleri barındırır.">
          <Select
            required
            value={supplierName}
            onChange={(e) => setSupplierName(e.target.value)}
            options={[
              { value: '', label: '-- Tedarikçi Seçiniz --' },
              ...suppliers.map((s) => ({
                value: s.supplierName,
                label: `${s.supplierName} (${s.activeOrderCount} Açık Sipariş)`
              }))
            ]}
          />
        </FormField>

        <div className="form-grid-2">
          <FormField label="Varsayılan Taşıma Modu">
            <Select
              value={defaultTransportMode}
              onChange={(e) => setDefaultTransportMode(e.target.value)}
              options={[
                { value: 'Sea', label: 'Deniz (Sea)' },
                { value: 'Air', label: 'Hava (Air)' },
                { value: 'Road', label: 'Kara (Road)' },
                { value: 'Rail', label: 'Demiryolu (Rail)' },
                { value: 'Courier', label: 'Kurye (Courier)' },
                { value: 'Multimodal', label: 'Multimodal' }
              ]}
            />
          </FormField>

          <FormField label="Incoterm">
            <Select
              value={incoterm}
              onChange={(e) => setIncoterm(e.target.value)}
              options={[
                { value: 'FOB', label: 'FOB (Free on Board)' },
                { value: 'EXW', label: 'EXW (Ex Works)' },
                { value: 'CIF', label: 'CIF (Cost, Insurance & Freight)' },
                { value: 'CFR', label: 'CFR (Cost and Freight)' },
                { value: 'DDP', label: 'DDP (Delivered Duty Paid)' },
                { value: 'FCA', label: 'FCA (Free Carrier)' },
                { value: 'DAP', label: 'DAP (Delivered at Place)' }
              ]}
            />
          </FormField>
        </div>

        <div className="form-grid-2">
          <FormField label="Menşei Ülke">
            <Input
              type="text"
              placeholder="Örn: Çin, Almanya, İtalya"
              value={originCountry}
              onChange={(e) => setOriginCountry(e.target.value)}
            />
          </FormField>

          <FormField label="Tahmini Üretim Bitiş Tarihi">
            <Input
              type="date"
              value={estimatedProductionCompletionDate}
              onChange={(e) => setEstimatedProductionCompletionDate(e.target.value)}
            />
          </FormField>
        </div>

        <FormField label="Notlar & Operasyonel Açıklamalar">
          <Textarea
            rows={3}
            placeholder="Dosya ile ilgili özel gereksinimler, yükleme notları..."
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </FormField>
      </form>
    </Modal>
  );
};
