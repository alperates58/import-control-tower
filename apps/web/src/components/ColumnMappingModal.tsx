import React, { useState } from 'react';
import { Modal } from './ui/Modal';
import { Button } from './ui/Button';
import { Select } from './ui/Input';
import { Badge } from './ui/Badge';

interface ColumnMappingModalProps {
  isOpen: boolean;
  onClose: () => void;
  unmappedHeaders: string[];
  missingRequired?: string[];
  currentMapping: Record<string, string>;
  onSaveMapping: (newMapping: Record<string, string>) => void;
}

const TARGET_FIELDS = [
  { key: 'IGNORE', label: '-- Kullanma (Yok Say) --' },
  { key: 'OrderNumber', label: 'Sipariş No (Zorunlu)' },
  { key: 'SupplierName', label: 'Firma Adı (Zorunlu)' },
  { key: 'OrderDate', label: 'Sipariş Tarihi (Zorunlu)' },
  { key: 'StockCode', label: 'Stok Kodu (Zorunlu)' },
  { key: 'StockName', label: 'Stok İsmi (Zorunlu)' },
  { key: 'OrderedQuantity', label: 'Sipariş Miktarı (Zorunlu)' },
  { key: 'RemainingQuantity', label: 'Sipariş Kalan Miktarı (Zorunlu)' },
  { key: 'SasDate', label: 'SAS Tarihi (Opsiyonel)' }
];

export const ColumnMappingModal: React.FC<ColumnMappingModalProps> = ({
  isOpen,
  onClose,
  unmappedHeaders,
  missingRequired: _missingRequired,
  currentMapping,
  onSaveMapping
}) => {
  const [mapping, setMapping] = useState<Record<string, string>>({ ...currentMapping });
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSelectChange = (header: string, target: string) => {
    setErrorMsg(null);
    setMapping((prev) => {
      const next = { ...prev };
      if (target === 'IGNORE') {
        delete next[header];
      } else {
        for (const [k, v] of Object.entries(next)) {
          if (v === target && k !== header) {
            delete next[k];
          }
        }
        next[header] = target;
      }
      return next;
    });
  };

  const handleSave = () => {
    const required = ['OrderNumber', 'SupplierName', 'OrderDate', 'StockCode', 'StockName', 'OrderedQuantity', 'RemainingQuantity'];
    const mappedValues = Object.values(mapping);
    const missing = required.filter((r) => !mappedValues.includes(r));

    if (missing.length > 0) {
      setErrorMsg(`Lütfen zorunlu olan şu kolonları eşleştiriniz: ${missing.join(', ')}`);
      return;
    }

    onSaveMapping(mapping);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Manuel Kolon Eşleştirme"
      maxWidth="640px"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            İptal
          </Button>
          <Button variant="primary" onClick={handleSave}>
            Haritayı Kaydet ve Yeniden Doğrula
          </Button>
        </>
      }
    >
      <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>
          Excel dosyanızdaki kolon başlıklarını hedef sistem alanlarıyla eşleştiriniz.
        </div>

        {errorMsg && (
          <Badge variant="rose" style={{ width: '100%', padding: '0.75rem' }}>
            {errorMsg}
          </Badge>
        )}

        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 'var(--font-sm)' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', textAlign: 'left' }}>
              <th style={{ padding: 'var(--space-2)' }}>Excel Kolon Başlığı</th>
              <th style={{ padding: 'var(--space-2)' }}>Hedef Sistem Alanı</th>
            </tr>
          </thead>
          <tbody>
            {unmappedHeaders.map((header) => {
              const selectedTarget = mapping[header] || 'IGNORE';
              return (
                <tr key={header} style={{ borderBottom: '1px solid var(--border-subtle)' }}>
                  <td style={{ padding: 'var(--space-2)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>
                    {header}
                  </td>
                  <td style={{ padding: 'var(--space-2)' }}>
                    <Select
                      value={selectedTarget}
                      onChange={(e) => handleSelectChange(header, e.target.value)}
                      options={TARGET_FIELDS.map((tf) => ({ value: tf.key, label: tf.label }))}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </Modal>
  );
};
