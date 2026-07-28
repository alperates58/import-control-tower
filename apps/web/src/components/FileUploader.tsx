import React, { useState, useRef } from 'react';
import { IconUpload, IconAlert } from './Icons';

interface FileUploaderProps {
  onFileSelected: (file: File) => void;
  isLoading: boolean;
  errorMessage?: string | null;
}

export const FileUploader: React.FC<FileUploaderProps> = ({ onFileSelected, isLoading, errorMessage }) => {
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const file = e.dataTransfer.files[0];
      validateAndSelect(file);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const file = e.target.files[0];
      validateAndSelect(file);
    }
  };

  const validateAndSelect = (file: File) => {
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      alert('Yalnızca .xlsx uzantılı Excel dosyaları yüklenebilir.');
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      alert('Dosya boyutu 10 MB sınırını aşıyor.');
      return;
    }
    onFileSelected(file);
  };

  return (
    <div style={{ width: '100%' }}>
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => !isLoading && fileInputRef.current?.click()}
        style={{
          border: `2px dashed ${isDragOver ? '#3b82f6' : 'rgba(255, 255, 255, 0.15)'}`,
          borderRadius: '16px',
          padding: '3rem 2rem',
          textAlign: 'center',
          background: isDragOver ? 'rgba(59, 130, 246, 0.08)' : 'rgba(15, 23, 42, 0.4)',
          cursor: isLoading ? 'not-allowed' : 'pointer',
          transition: 'all 0.2s ease',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: '1rem'
        }}
      >
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileChange}
          accept=".xlsx"
          style={{ display: 'none' }}
          disabled={isLoading}
        />

        <div
          style={{
            width: '64px',
            height: '64px',
            borderRadius: '16px',
            background: 'rgba(59, 130, 246, 0.12)',
            color: '#3b82f6',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          {isLoading ? (
            <div style={{ width: '24px', height: '24px', border: '3px solid #3b82f6', borderTopColor: 'transparent', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
          ) : (
            <IconUpload />
          )}
        </div>

        <div>
          <h3 style={{ margin: '0 0 0.5rem 0', fontSize: '1.1rem', fontWeight: 600, color: '#f8fafc' }}>
            {isLoading ? 'Dosya Yükleniyor ve Okunuyor...' : 'Excel Dosyasını Sürükleyip Bırakın'}
          </h3>
          <p style={{ margin: 0, fontSize: '0.875rem', color: '#94a3b8' }}>
            veya cihazınızdan bir <strong>.xlsx</strong> dosyası seçmek için tıklayın
          </p>
        </div>

        <div style={{ display: 'flex', gap: '1rem', marginTop: '0.5rem', fontSize: '0.75rem', color: '#64748b' }}>
          <span>Maksimum 10 MB</span>
          <span>•</span>
          <span>Maksimum 20.000 Satır</span>
          <span>•</span>
          <span>Yalnızca .xlsx</span>
        </div>
      </div>

      {errorMessage && (
        <div
          style={{
            marginTop: '1rem',
            padding: '0.85rem 1rem',
            borderRadius: '10px',
            background: 'rgba(239, 68, 68, 0.12)',
            border: '1px solid rgba(239, 68, 68, 0.3)',
            color: '#f87171',
            display: 'flex',
            alignItems: 'center',
            gap: '0.75rem',
            fontSize: '0.875rem'
          }}
        >
          <IconAlert />
          <span>{errorMessage}</span>
        </div>
      )}
    </div>
  );
};
