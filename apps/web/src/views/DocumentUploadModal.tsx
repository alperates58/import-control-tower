import React, { useState, useEffect } from 'react';
import { documentService } from '../services/documentService';
import { useAuth } from '../context/AuthContext';
import { Modal } from '../components/ui/Modal';
import { Button } from '../components/ui/Button';
import { Input, Select, Textarea, FormField } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  scopeType?: 'ImportCase' | 'Shipment' | 'Container';
  scopeId?: string;
  existingDocumentId?: string;
}

export const DocumentUploadModal: React.FC<Props> = ({
  isOpen,
  onClose,
  onSuccess,
  scopeType,
  scopeId,
  existingDocumentId
}) => {
  const { authenticatedFetch } = useAuth();
  const [title, setTitle] = useState('');
  const [documentType, setDocumentType] = useState('CommercialInvoice');
  const [documentNumber, setDocumentNumber] = useState('');
  const [documentDate, setDocumentDate] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [notes, setNotes] = useState('');
  const [file, setFile] = useState<File | null>(null);

  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setTitle('');
      setDocumentNumber('');
      setDocumentDate('');
      setExpiryDate('');
      setNotes('');
      setFile(null);
      setErrorMsg(null);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const selected = e.target.files[0];
      if (selected.size > 25 * 1024 * 1024) {
        setErrorMsg('Dosya boyutu 25 MB sınırını aşamaz (HTTP 413).');
        setFile(null);
        return;
      }
      setErrorMsg(null);
      setFile(selected);
      if (!title) {
        setTitle(selected.name.replace(/\.[^/.]+$/, ''));
      }
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setErrorMsg('Lütfen yüklenecek bir dosya seçiniz.');
      return;
    }

    setLoading(true);
    setErrorMsg(null);

    const idempotencyKey = `doc-upload-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
    const formData = new FormData();
    formData.append('file', file);

    try {
      if (existingDocumentId) {
        await documentService.addVersion(existingDocumentId, formData, idempotencyKey, authenticatedFetch);
      } else {
        formData.append('title', title || file.name);
        formData.append('documentType', documentType);
        if (documentNumber) formData.append('documentNumber', documentNumber);
        if (documentDate) formData.append('documentDate', documentDate);
        if (expiryDate) formData.append('expiryDate', expiryDate);
        if (notes) formData.append('notes', notes);

        if (scopeType === 'ImportCase' && scopeId) {
          await documentService.uploadCaseDocument(scopeId, formData, idempotencyKey, authenticatedFetch);
        } else if (scopeType === 'Shipment' && scopeId) {
          await documentService.uploadShipmentDocument(scopeId, formData, idempotencyKey, authenticatedFetch);
        } else if (scopeType === 'Container' && scopeId) {
          await documentService.uploadContainerDocument(scopeId, formData, idempotencyKey, authenticatedFetch);
        } else {
          throw new Error('Geçerli bir evrak kapsama alanı seçilmedi.');
        }
      }

      onSuccess();
      onClose();
    } catch (err: any) {
      setErrorMsg(err.message || 'Evrak yüklenirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={existingDocumentId ? '➕ Yeni Evrak Versiyonu Yükle' : '📁 Yeni İthalat Evrakı Yükle'}
      maxWidth="580px"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={loading}>
            Vazgeç
          </Button>
          <Button type="submit" form="upload-doc-form" variant="primary" isLoading={loading} disabled={!file}>
            {loading ? 'Yükleniyor...' : 'Evrak Yükle'}
          </Button>
        </>
      }
    >
      <form id="upload-doc-form" onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
        {errorMsg && (
          <Badge variant="rose" style={{ width: '100%', padding: '0.75rem' }}>
            ⚠️ {errorMsg}
          </Badge>
        )}

        <FormField label="Dosya Seçiniz (Max 25 MB) *" required helpText="Desteklenen formatlar: PDF, DOCX, XLSX, PNG, JPG/JPEG.">
          <Input
            type="file"
            required
            accept=".pdf,.docx,.xlsx,.png,.jpg,.jpeg"
            onChange={handleFileChange}
            style={{ padding: '0.4rem' }}
          />
        </FormField>

        {!existingDocumentId && (
          <>
            <FormField label="Evrak Başlığı *" required>
              <Input
                type="text"
                required
                placeholder="Örn: 2026 Ticari Fatura - INV9901"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
              />
            </FormField>

            <div className="form-grid-2">
              <FormField label="Evrak Türü *" required>
                <Select
                  value={documentType}
                  onChange={(e) => setDocumentType(e.target.value)}
                  options={[
                    { value: 'CommercialInvoice', label: 'Commercial Invoice (Ticari Fatura)' },
                    { value: 'ProformaInvoice', label: 'Proforma Invoice' },
                    { value: 'PackingList', label: 'Packing List (Çeki Listesi)' },
                    { value: 'BillOfLading', label: 'Bill of Lading (Konşimento)' },
                    { value: 'SeaWaybill', label: 'Sea Waybill' },
                    { value: 'AirWaybill', label: 'Air Waybill (AWB)' },
                    { value: 'CMR', label: 'CMR (Kara Taşıma Senedi)' },
                    { value: 'CertificateOfOrigin', label: 'Certificate of Origin (Menşe)' },
                    { value: 'ATR', label: 'A.TR Dolaşım Belgesi' },
                    { value: 'EUR1', label: 'EUR.1 Dolaşım Belgesi' },
                    { value: 'InsuranceCertificate', label: 'Sigorta Poliçesi' },
                    { value: 'CustomsDeclaration', label: 'Gümrük Beyannamesi' },
                    { value: 'MSDS', label: 'MSDS / SDS' },
                    { value: 'Other', label: 'Diğer Belge' }
                  ]}
                />
              </FormField>

              <FormField label="Evrak / Fatura No">
                <Input
                  type="text"
                  placeholder="Örn: INV-2026-9901"
                  value={documentNumber}
                  onChange={(e) => setDocumentNumber(e.target.value)}
                />
              </FormField>
            </div>

            <div className="form-grid-2">
              <FormField label="Evrak Tarihi">
                <Input
                  type="date"
                  value={documentDate}
                  onChange={(e) => setDocumentDate(e.target.value)}
                />
              </FormField>

              <FormField label="Geçerlilik Bitiş Tarihi">
                <Input
                  type="date"
                  value={expiryDate}
                  onChange={(e) => setExpiryDate(e.target.value)}
                />
              </FormField>
            </div>

            <FormField label="Notlar">
              <Textarea
                rows={2}
                placeholder="Evrak hakkında ek notlar..."
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
              />
            </FormField>
          </>
        )}
      </form>
    </Modal>
  );
};
