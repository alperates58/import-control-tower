import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DocumentChecklistWidget } from '../components/documents/DocumentChecklistWidget';
import { DocumentVersionDrawer } from '../components/documents/DocumentVersionDrawer';
import { AuthProvider } from '../context/AuthContext';

describe('Frontend Phase 04 Component Suite', () => {
  it('1. DocumentChecklistWidget - renders checklist items', async () => {
    render(
      <AuthProvider>
        <DocumentChecklistWidget scopeType="ImportCase" scopeId="case-123" />
      </AuthProvider>
    );

    // Initial loading state test
    expect(screen.getByText('Evrak checklist kontrol ediliyor...')).toBeInTheDocument();
  });

  it('2. DocumentVersionDrawer - renders version history modal header when open', () => {
    render(
      <AuthProvider>
        <DocumentVersionDrawer
          documentId="doc-123"
          documentTitle="Commercial Invoice - INV99"
          isOpen={true}
          onClose={vi.fn()}
        />
      </AuthProvider>
    );

    expect(screen.getByText('📜 Belge Versiyon Geçmişi')).toBeInTheDocument();
    expect(screen.getByText('Commercial Invoice - INV99')).toBeInTheDocument();
  });

  it('3. DocumentVersionDrawer - returns null when closed', () => {
    const { container } = render(
      <AuthProvider>
        <DocumentVersionDrawer
          documentId="doc-123"
          documentTitle="Commercial Invoice - INV99"
          isOpen={false}
          onClose={vi.fn()}
        />
      </AuthProvider>
    );

    expect(container.firstChild).toBeNull();
  });
});
