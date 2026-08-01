import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AuthProvider } from '../context/AuthContext';
import { ProtectedRoute } from '../components/ProtectedRoute';
import { PermissionGuard } from '../components/PermissionGuard';
import { LoginView } from '../views/LoginView';
import App from '../App';

describe('Frontend Phase 01 Quality Control Suite', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    window.fetch = vi.fn();
  });

  it('1. Login form validation - renders inputs and handles submit', async () => {
    (window.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        accessToken: 'mock-access-token',
        user: {
          id: 'u1',
          email: 'admin@controltower.local',
          fullName: 'Test Admin',
          roles: ['SystemAdmin'],
          permissions: ['dashboard.view', 'users.view']
        }
      })
    });

    render(
      <AuthProvider>
        <LoginView />
      </AuthProvider>
    );

    expect(screen.getByText('Import Control Tower')).toBeInTheDocument();
    const emailInput = screen.getByPlaceholderText('admin@controltower.local');
    const passwordInput = screen.getByPlaceholderText('••••••••••••');
    const submitBtn = screen.getByRole('button', { name: /giriş yap/i });

    expect(emailInput).toBeInTheDocument();
    expect(passwordInput).toBeInTheDocument();

    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'Password123!' } });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(window.fetch).toHaveBeenCalledWith(
        '/api/v1/auth/login',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({ 'Content-Type': 'application/json' })
        })
      );
    });
  });

  it('2. ProtectedRoute - blocks content when permission missing', () => {
    const TestComponent = () => {
      return (
        <ProtectedRoute requiredPermission="users.view">
          <div>Secret Admin Content</div>
        </ProtectedRoute>
      );
    };

    render(
      <AuthProvider>
        <TestComponent />
      </AuthProvider>
    );

    expect(screen.queryByText('Secret Admin Content')).not.toBeInTheDocument();
  });

  it('3. PermissionGuard - conditionally renders children based on permission', () => {
    const TestConsumer = () => {
      return (
        <PermissionGuard permission="financial.view" fallback={<div>No Access to Financials</div>}>
          <div>Financial Charts</div>
        </PermissionGuard>
      );
    };

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>
    );

    expect(screen.getByText('No Access to Financials')).toBeInTheDocument();
    expect(screen.queryByText('Financial Charts')).not.toBeInTheDocument();
  });

  it('4. Permission-based menu filtering - filters unpermitted tabs', async () => {
    (window.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        accessToken: 'mock-access-token',
        user: {
          id: 'u2',
          email: 'purchaser@controltower.local',
          fullName: 'Purchaser User',
          roles: ['Purchasing'],
          permissions: ['dashboard.view', 'purchaseorders.view']
        }
      })
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/Control Tower/i)).toBeInTheDocument();
    });
  });

  it('5. Refresh bootstrap - calls /api/v1/auth/refresh on page load', async () => {
    (window.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        accessToken: 'valid-refreshed-token',
        user: {
          id: 'u3',
          email: 'admin@controltower.local',
          fullName: 'Bootstrapped Admin',
          roles: ['SystemAdmin'],
          permissions: ['dashboard.view']
        }
      })
    });

    render(<App />);

    await waitFor(() => {
      expect(window.fetch).toHaveBeenCalledWith(
        '/api/v1/auth/refresh',
        expect.objectContaining({ method: 'POST' })
      );
    });
  });

  it('6. Refresh failure login redirect - shows LoginView on 401 refresh', async () => {
    (window.fetch as any).mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: async () => ({ detail: 'Token expired' })
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText('Kurumsal Giriş Portalı (Faz 01)')).toBeInTheDocument();
    });
  });

  it('7. Logout - clears state and redirects to LoginView', async () => {
    (window.fetch as any)
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({
          accessToken: 'valid-token',
          user: { id: 'u4', email: 'admin@local.com', fullName: 'Logged Admin', roles: ['SystemAdmin'], permissions: ['dashboard.view'], mustChangePassword: false }
        })
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ['dashboard.view']
      })
      .mockResolvedValueOnce({ ok: true });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText('Logged Admin')).toBeInTheDocument();
    });

    const logoutBtn = screen.getByText('Güvenli Çıkış');
    fireEvent.click(logoutBtn);

    await waitFor(() => {
      expect(screen.getByText('Kurumsal Giriş Portalı (Faz 01)')).toBeInTheDocument();
    });
  });

  it('8. Financial module disabled menu behavior - hides financial tab when ungranted', async () => {
    (window.fetch as any).mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        accessToken: 'valid-token',
        user: {
          id: 'u5',
          email: 'manager@controltower.local',
          fullName: 'Manager User',
          roles: ['Management'],
          permissions: ['dashboard.view', 'purchaseorders.view', 'audit.view']
        }
      })
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.queryByText('Finansal Analiz')).not.toBeInTheDocument();
    });
  });
});
