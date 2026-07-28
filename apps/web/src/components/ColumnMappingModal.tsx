import React, { useState } from 'react';
import { IconX } from './Icons';

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
        // Prevent duplicate target mapping
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
    // Check required targets
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
    <div
      style={{
        position: 'fixed',
        inset: 0,
        zIndex: 50,
        background: 'rgba(0, 0, 0, 0.75)',
        backdropFilter: 'blur(8px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '1.5rem'
      }}
    >
      <div
        style={{
          width: '100%',
          maxWidth: '640px',
          maxHeight: '90vh',
          background: 'rgba(15, 23, 42, 0.95)',
          border: '1px solid rgba(255, 255, 255, 0.15)',
          borderRadius: '20px',
          display: 'flex',
          flexDirection: 'column',
          boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.5)',
          overflow: 'hidden'
        }}
      >
        <div style={{ padding: '1.5rem', borderBottom: '1px solid rgba(255, 255, 255, 0.1)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <h3 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 600, color: '#f8fafc' }}>Manuel Kolon Eşleştirme</h3>
            <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.85rem', color: '#94a3b8' }}>
              Excel dosyanızdaki kolon başlıklarını hedef sistem alanlarıyla eşleştiriniz.
            </p>
          </div>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', color: '#94a3b8', cursor: 'pointer', padding: '0.5rem' }}>
            <IconX />
          </button>
        </div>

        <div style={{ padding: '1.5rem', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {errorMsg && (
            <div style={{ padding: '0.75rem 1rem', borderRadius: '8px', background: 'rgba(239, 68, 68, 0.15)', border: '1px solid rgba(239, 68, 68, 0.3)', color: '#f87171', fontSize: '0.875rem' }}>
              {errorMsg}
            </div>
          )}

          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.1)', color: '#94a3b8', textAlign: 'left' }}>
                <th style={{ padding: '0.75rem' }}>Excel Kolon Başlığı</th>
                <th style={{ padding: '0.75rem' }}>Hedef Sistem Alanı</th>
              </tr>
            </thead>
            <tbody>
              {unmappedHeaders.map((header) => {
                const selectedTarget = mapping[header] || 'IGNORE';
                return (
                  <tr key={header} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.05)' }}>
                    <td style={{ padding: '0.75rem', fontWeight: 500, color: '#f8fafc' }}>{header}</td>
                    <td style={{ padding: '0.75rem' }}>
                      <select
                        value={selectedTarget}
                        onChange={(e) => handleSelectChange(header, e.target.value)}
                        style={{
                          width: '100%',
                          padding: '0.5rem 0.75rem',
                          borderRadius: '8px',
                          background: 'rgba(30, 41, 59, 0.8)',
                          border: '1px solid rgba(255, 255, 255, 0.15)',
                          color: '#f8fafc',
                          fontSize: '0.85rem'
                        }}
                      >
                        {TARGET_FIELDS.map((tf) => (
                          <option key={tf.key} value={tf.key}>
                            {tf.label}
                          </option>
                        ))}
                      </select>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div style={{ padding: '1.25rem 1.5rem', borderTop: '1px solid rgba(255, 255, 255, 0.1)', display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
          <button
            onClick={onClose}
            style={{ padding: '0.6rem 1.2rem', borderRadius: '10px', background: 'rgba(255, 255, 255, 0.08)', border: '1px solid rgba(255, 255, 255, 0.1)', color: '#f8fafc', fontWeight: 500, cursor: 'pointer' }}
          >
            İptal
          </button>
          <button
            onClick={handleSave}
            style={{ padding: '0.6rem 1.4rem', borderRadius: '10px', background: '#3b82f6', border: 'none', color: '#ffffff', fontWeight: 600, cursor: 'pointer' }}
          >
            Haritayı Kaydet ve Yeniden Doğrula
          </button>
        </div>
      </div>
    </div>
  );
};
