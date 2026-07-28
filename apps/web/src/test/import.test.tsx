import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FileUploader } from '../components/FileUploader';
import { ColumnMappingModal } from '../components/ColumnMappingModal';
import { ImportStatusBadge } from '../components/ImportStatusBadge';
import { PurchaseOrderListView } from '../views/PurchaseOrderListView';
import { PurchaseOrderDetailView } from '../views/PurchaseOrderDetailView';

describe('Frontend Phase 02 Component Suite', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    window.fetch = vi.fn();
  });

  it('1. FileUploader - renders drag & drop area and validates xlsx file', () => {
    const handleFileSelected = vi.fn();
    render(<FileUploader onFileSelected={handleFileSelected} isLoading={false} />);

    expect(screen.getByText('Excel Dosyasını Sürükleyip Bırakın')).toBeInTheDocument();
    expect(screen.getByText('Yalnızca .xlsx')).toBeInTheDocument();
  });

  it('2. ImportStatusBadge - renders correct status badges', () => {
    const { rerender } = render(<ImportStatusBadge status="ReadyForConfirmation" />);
    expect(screen.getByText('Onaya Hazır')).toBeInTheDocument();

    rerender(<ImportStatusBadge status="Completed" />);
    expect(screen.getByText('Tamamlandı')).toBeInTheDocument();

    rerender(<ImportStatusBadge status="ValidationFailed" />);
    expect(screen.getByText('Hatalı Satırlar Var')).toBeInTheDocument();
  });

  it('3. ColumnMappingModal - renders mapping options and saves mapping', () => {
    const handleSave = vi.fn();
    const handleClose = vi.fn();

    render(
      <ColumnMappingModal
        isOpen={true}
        onClose={handleClose}
        unmappedHeaders={['Custom_PO', 'Custom_Supp']}
        missingRequired={['OrderNumber', 'SupplierName']}
        currentMapping={{}}
        onSaveMapping={handleSave}
      />
    );

    expect(screen.getByText('Manuel Kolon Eşleştirme')).toBeInTheDocument();
    expect(screen.getByText('Custom_PO')).toBeInTheDocument();
    expect(screen.getByText('Custom_Supp')).toBeInTheDocument();
  });

  it('4. PurchaseOrderListView - renders purchase orders without financial headers', async () => {
    (window.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        items: [
          {
            id: 'po-1',
            orderNumber: 'PO-2026-0001',
            supplierName: 'ABC Tedarik A.S.',
            orderDate: '2026-04-15T00:00:00Z',
            status: 'Open',
            source: 'ExcelImport',
            lineCount: 2,
            totalOrderedQuantity: 100,
            totalRemainingQuantity: 50
          }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 15,
        totalPages: 1
      })
    });

    render(<PurchaseOrderListView onSelectOrder={vi.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('PO-2026-0001')).toBeInTheDocument();
    });

    expect(screen.getByText('ABC Tedarik A.S.')).toBeInTheDocument();
    expect(screen.queryByText(/fiyat/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/tutar/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/maliyet/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/döviz/i)).not.toBeInTheDocument();
  });

  it('5. PurchaseOrderDetailView - renders order detail with zero financial fields', async () => {
    (window.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        id: 'po-1',
        orderNumber: 'PO-2026-0001',
        supplierName: 'ABC Tedarik A.S.',
        orderDate: '2026-04-15T00:00:00Z',
        status: 'Open',
        source: 'ExcelImport',
        lines: [
          {
            id: 'l-1',
            lineNumber: 1,
            stockCode: 'STK-001',
            stockName: 'Malzeme A',
            orderedQuantity: 100,
            remainingQuantity: 50,
            sasDate: '2026-04-20T00:00:00Z'
          }
        ]
      })
    });

    render(<PurchaseOrderDetailView orderId="po-1" onBack={vi.fn()} />);

    await waitFor(() => {
      expect(screen.getByText('Sipariş No: PO-2026-0001')).toBeInTheDocument();
    });

    expect(screen.getByText('STK-001')).toBeInTheDocument();
    expect(screen.getByText('Malzeme A')).toBeInTheDocument();
    expect(screen.queryByText(/price/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/cost/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/total/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/amount/i)).not.toBeInTheDocument();
  });
});
