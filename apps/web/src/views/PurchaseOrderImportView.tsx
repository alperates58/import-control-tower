import React, { useState } from 'react';
import { FileUploader } from '../components/FileUploader';
import { IconFileSpreadsheet } from '../components/Icons';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { Button } from '../components/ui/Button';
import { Section } from '../components/ui/Card';

interface PurchaseOrderImportViewProps {
  onBatchCreated: (batchId: string) => void;
}

export const PurchaseOrderImportView: React.FC<PurchaseOrderImportViewProps> = ({ onBatchCreated }) => {
  const { authenticatedFetch } = useAuth();
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [conflictBatchId, setConflictBatchId] = useState<string | null>(null);

  const handleFileSelected = async (file: File) => {
    setIsLoading(true);
    setErrorMessage(null);
    setConflictBatchId(null);

    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await authenticatedFetch('/api/v1/purchase-order-imports/upload', {
        method: 'POST',
        headers: {
          'X-ICT-CSRF-Protection': '1'
        },
        body: formData
      });

      if (response.status === 201) {
        const data = await response.json();
        onBatchCreated(data.batch.id);
      } else {
        const errData = await response.json().catch(() => null);
        const bId = errData?.batchId || errData?.extensions?.batchId;
        if (bId) {
          setConflictBatchId(bId);
        }
        setErrorMessage(errData?.detail || errData?.title || 'Dosya yüklenirken bir hata oluştu.');
      }
    } catch (err: any) {
      setErrorMessage('Ağ veya sunucu bağlantı hatası oluştu.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDownloadTemplate = async () => {
    try {
      const response = await authenticatedFetch('/api/v1/purchase-order-imports/template');
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'purchase-order-import-template.xlsx';
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
      } else {
        setErrorMessage('Şablon indirilemedi (Yetkisiz erişim veya sunucu hatası).');
      }
    } catch (err) {
      setErrorMessage('Şablon indirilirken bir bağlantı hatası oluştu.');
    }
  };

  return (
    <div>
      <PageHeader
        title="Excel Sipariş İçe Aktarma"
        subtitle="ERP veya operasyonel Excel dosyalarınızdan açık satın alma siparişlerini güvenle aktarın."
        actions={
          <Button variant="secondary" onClick={handleDownloadTemplate} icon={<IconFileSpreadsheet />}>
            Örnek Şablon İndir (.xlsx)
          </Button>
        }
      />

      <div className="panel" style={{ padding: 'var(--space-6)' }}>
        <FileUploader
          onFileSelected={handleFileSelected}
          isLoading={isLoading}
          errorMessage={errorMessage}
        />
        {conflictBatchId && (
          <div style={{ marginTop: 'var(--space-4)', textAlign: 'center' }}>
            <Button variant="primary" onClick={() => onBatchCreated(conflictBatchId)}>
              Devam Eden Aktarım Ekranına Git &rarr;
            </Button>
          </div>
        )}
      </div>

      <Section title="İçe Aktarma Kuralları ve Güvenlik Limitleri">
        <ul style={{ margin: 0, paddingLeft: '1.25rem', fontSize: 'var(--font-sm)', color: 'var(--text-muted)', display: 'flex', flexDirection: 'column', gap: '0.4rem' }}>
          <li>Dosyanızdaki <strong>Sipariş No</strong> ve <strong>Stok Kodu</strong> kolonlarının metin olarak saklandığından emin olunuz (baştaki sıfırlar korunur).</li>
          <li>Tarihler <code>dd.MM.yyyy</code> veya <code>yyyy-MM-dd</code> biçiminde olmalıdır; belirsiz slash tarihler reddedilir.</li>
          <li>Aynı sipariş numarasına ait satırlarda Sipariş Tarihi ve Firma Adı tutarlı olmalıdır.</li>
          <li>Aktarım öncesinde <strong>Ön İzleme</strong> ekranında etkilenecek satırlar tarafınıza sunulacak, onayınız ile veri tabanına işlenecektir.</li>
        </ul>
      </Section>
    </div>
  );
};
