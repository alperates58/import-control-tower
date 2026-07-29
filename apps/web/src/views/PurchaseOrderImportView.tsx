import React, { useState } from 'react';
import { FileUploader } from '../components/FileUploader';
import { IconFileSpreadsheet } from '../components/Icons';
import { useAuth } from '../context/AuthContext';

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
    <div style={{ display: 'flex', flexDirection: 'column', gap: '2rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '1.75rem', fontWeight: 700, color: '#f8fafc' }}>
            Excel Sipariş İçe Aktarma
          </h1>
          <p style={{ margin: '0.25rem 0 0 0', fontSize: '0.9rem', color: '#94a3b8' }}>
            ERP veya operasyonel Excel dosyalarınızdan açık satın alma siparişlerini güvenle aktarın.
          </p>
        </div>

        <button
          onClick={handleDownloadTemplate}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.5rem',
            padding: '0.65rem 1.25rem',
            borderRadius: '12px',
            background: 'rgba(59, 130, 246, 0.12)',
            border: '1px solid rgba(59, 130, 246, 0.3)',
            color: '#60a5fa',
            fontSize: '0.875rem',
            fontWeight: 600,
            cursor: 'pointer',
            transition: 'all 0.2s ease'
          }}
        >
          <IconFileSpreadsheet />
          <span>Örnek Şablon İndir (.xlsx)</span>
        </button>
      </div>

      <div
        style={{
          background: 'rgba(15, 23, 42, 0.6)',
          backdropFilter: 'blur(12px)',
          border: '1px solid rgba(255, 255, 255, 0.1)',
          borderRadius: '24px',
          padding: '2rem'
        }}
      >
        <FileUploader
          onFileSelected={handleFileSelected}
          isLoading={isLoading}
          errorMessage={errorMessage}
        />
        {conflictBatchId && (
          <div style={{ marginTop: '1.25rem', textAlign: 'center' }}>
            <button
              onClick={() => onBatchCreated(conflictBatchId)}
              style={{
                padding: '0.65rem 1.25rem',
                borderRadius: '12px',
                background: '#3b82f6',
                border: 'none',
                color: '#ffffff',
                fontSize: '0.875rem',
                fontWeight: 600,
                cursor: 'pointer',
                boxShadow: '0 4px 12px rgba(59, 130, 246, 0.3)',
                transition: 'all 0.2s ease'
              }}
            >
              Devam Eden Aktarım Ekranına Git &rarr;
            </button>
          </div>
        )}
      </div>

      <div style={{ background: 'rgba(15, 23, 42, 0.4)', borderRadius: '16px', padding: '1.5rem', border: '1px solid rgba(255, 255, 255, 0.05)' }}>
        <h4 style={{ margin: '0 0 0.75rem 0', fontSize: '0.95rem', fontWeight: 600, color: '#e2e8f0' }}>İçe Aktarma Kuralları ve Güvenlik Limitleri</h4>
        <ul style={{ margin: 0, paddingLeft: '1.25rem', fontSize: '0.85rem', color: '#94a3b8', display: 'flex', flexDirection: 'column', gap: '0.4rem' }}>
          <li>Dosyanızdaki <strong>Sipariş No</strong> ve <strong>Stok Kodu</strong> kolonlarının metin olarak saklandığından emin olunuz (baştaki sıfırlar korunur).</li>
          <li>Tarihler <code>dd.MM.yyyy</code> veya <code>yyyy-MM-dd</code> biçiminde olmalıdır; belirsiz slash tarihler reddedilir.</li>
          <li>Aynı sipariş numarasına ait satırlarda Sipariş Tarihi ve Firma Adı tutarlı olmalıdır.</li>
          <li>Aktarım öncesinde <strong>Ön İzleme</strong> ekranında etkilenecek satırlar tarafınıza sunulacak, onayınız ile veri tabanına işlenecektir.</li>
        </ul>
      </div>
    </div>
  );
};
