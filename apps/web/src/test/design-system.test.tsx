import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { Button } from '../components/ui/Button';
import { PageHeader } from '../components/ui/PageHeader';
import { Modal, ConfirmDialog } from '../components/ui/Modal';
import { Drawer } from '../components/ui/Drawer';
import { DropdownMenu } from '../components/ui/DropdownMenu';
import { DataTable } from '../components/ui/DataTable';
import { EmptyState, ErrorState, LoadingSkeleton } from '../components/ui/FeedbackState';

describe('Design System V2 Component Suite', () => {
  it('1. PageHeader - renders title, subtitle and action slots', () => {
    render(
      <PageHeader
        title="Test Page Title"
        subtitle="Test Subtitle"
        actions={<Button>Action Btn</Button>}
      />
    );

    expect(screen.getByText('Test Page Title')).toBeInTheDocument();
    expect(screen.getByText('Test Subtitle')).toBeInTheDocument();
    expect(screen.getByText('Action Btn')).toBeInTheDocument();
  });

  it('2. Modal - renders when open and responds to close/escape', () => {
    const handleClose = vi.fn();
    render(
      <Modal isOpen={true} onClose={handleClose} title="Modal Title">
        <div>Modal Content Body</div>
      </Modal>
    );

    expect(screen.getByText('Modal Title')).toBeInTheDocument();
    expect(screen.getByText('Modal Content Body')).toBeInTheDocument();

    fireEvent.keyDown(window, { key: 'Escape' });
    expect(handleClose).toHaveBeenCalled();
  });

  it('3. Drawer - renders side panel when open', () => {
    const handleClose = vi.fn();
    render(
      <Drawer isOpen={true} onClose={handleClose} title="Drawer Title">
        <div>Drawer Body Content</div>
      </Drawer>
    );

    expect(screen.getByText('Drawer Title')).toBeInTheDocument();
    expect(screen.getByText('Drawer Body Content')).toBeInTheDocument();
  });

  it('4. DropdownMenu - toggles menu on trigger click and executes item action', () => {
    const handleAction = vi.fn();
    render(
      <DropdownMenu
        items={[
          { label: 'Detay Görüntüle', onClick: handleAction }
        ]}
      />
    );

    const trigger = screen.getByRole('button', { name: 'İşlemler Menüsü' });
    expect(trigger).toBeInTheDocument();

    fireEvent.click(trigger);
    expect(screen.getByText('Detay Görüntüle')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Detay Görüntüle'));
    expect(handleAction).toHaveBeenCalled();
  });

  it('5. DataTable - renders columns, rows and handles row click', () => {
    const handleRowClick = vi.fn();
    const items = [{ id: '1', name: 'Alpha' }, { id: '2', name: 'Beta' }];

    render(
      <DataTable
        columns={[
          { key: 'name', header: 'İsim' }
        ]}
        data={items}
        keyExtractor={(item) => item.id}
        onRowClick={handleRowClick}
      />
    );

    expect(screen.getByText('Alpha')).toBeInTheDocument();
    expect(screen.getByText('Beta')).toBeInTheDocument();

    fireEvent.click(screen.getByText('Alpha'));
    expect(handleRowClick).toHaveBeenCalledWith(items[0]);
  });

  it('6. EmptyState, ErrorState & LoadingSkeleton - renders correctly', () => {
    const { rerender } = render(<EmptyState title="Veri Yok" description="Açıklama" />);
    expect(screen.getByText('Veri Yok')).toBeInTheDocument();
    expect(screen.getByText('Açıklama')).toBeInTheDocument();

    const handleRetry = vi.fn();
    rerender(<ErrorState title="Hata Yüklendi" onRetry={handleRetry} />);
    expect(screen.getByText('Hata Yüklendi')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Tekrar Dene'));
    expect(handleRetry).toHaveBeenCalled();

    const { container } = render(<LoadingSkeleton rows={3} />);
    expect(container.querySelectorAll('.skeleton').length).toBe(3);
  });

  it('7. ConfirmDialog - triggers confirm callback', () => {
    const handleConfirm = vi.fn();
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={vi.fn()}
        onConfirm={handleConfirm}
        title="Silme Onayı"
        message="Silmek istediğinize emin misiniz?"
      />
    );

    expect(screen.getByText('Silme Onayı')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Onayla'));
    expect(handleConfirm).toHaveBeenCalled();
  });
});
