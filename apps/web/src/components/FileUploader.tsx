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
          border: `2px dashed ${isDragOver ? 'var(--accent-blue)' : 'var(--border-strong)'}`,
          borderRadius: 'var(--radius-lg)',
          padding: 'var(--space-8) var(--space-4)',
          textAlign: 'center',
          background: isDragOver ? 'var(--primary-light)' : 'var(--bg-input)',
          cursor: isLoading ? 'not-allowed' : 'pointer',
          transition: 'all var(--transition-fast)',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 'var(--space-3)'
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
            width: '48px',
            height: '48px',
            borderRadius: 'var(--radius-lg)',
            background: 'var(--primary-light)',
            color: 'var(--accent-blue)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          {isLoading ? (
            <div className="pulse-dot" style={{ width: '12px', height: '12px' }} />
          ) : (
            <IconUpload />
          )}
        </div>

        <div>
          <h3 style={{ margin: '0 0 var(--space-1) 0', fontSize: 'var(--font-md)', fontWeight: 'var(--weight-bold)', color: 'var(--text-main)' }}>
            {isLoading ? 'Dosya Yükleniyor ve Okunuyor...' : 'Excel Dosyasını Sürükleyip Bırakın'}
          </h3>
          <p style={{ margin: 0, fontSize: 'var(--font-sm)', color: 'var(--text-muted)' }}>
            veya cihazınızdan bir <strong>.xlsx</strong> dosyası seçmek için tıklayın
          </p>
        </div>

        <div style={{ display: 'flex', gap: 'var(--space-3)', marginTop: 'var(--space-2)', fontSize: 'var(--font-xs)', color: 'var(--text-dim)' }}>
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
            marginTop: 'var(--space-4)',
            padding: 'var(--space-3) var(--space-4)',
            borderRadius: 'var(--radius-md)',
            background: 'var(--status-danger-bg)',
            border: '1px solid var(--status-danger-border)',
            color: 'var(--accent-rose)',
            display: 'flex',
            alignItems: 'center',
            gap: 'var(--space-3)',
            fontSize: 'var(--font-sm)'
          }}
        >
          <IconAlert />
          <span>{errorMessage}</span>
        </div>
      )}
    </div>
  );
};
