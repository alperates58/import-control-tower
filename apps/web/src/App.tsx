import React, { useState, useEffect } from 'react';
import { ThemeProvider } from './context/ThemeContext';
import { AuthProvider, useAuth } from './context/AuthContext';
import { setImportCaseServiceFetch } from './services/importCaseService';
import { setDocumentServiceFetch } from './services/documentService';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginView } from './views/LoginView';
import { UserManagementView } from './views/UserManagementView';
import { RoleManagementView } from './views/RoleManagementView';
import { AuditLogsView } from './views/AuditLogsView';
import { ProfileView } from './views/ProfileView';
import { PurchaseOrderImportView } from './views/PurchaseOrderImportView';
import { ImportPreviewView } from './views/ImportPreviewView';
import { ImportHistoryView } from './views/ImportHistoryView';
import { PurchaseOrderListView } from './views/PurchaseOrderListView';
import { PurchaseOrderDetailView } from './views/PurchaseOrderDetailView';
import { ImportCaseListView } from './views/ImportCaseListView';
import { ImportCaseDetailView } from './views/ImportCaseDetailView';
import { DocumentListView } from './views/DocumentListView';
import { ForceChangePasswordView } from './views/ForceChangePasswordView';

import { KPICard, Section } from './components/ui/Card';
import { EmptyState } from './components/ui/FeedbackState';
import { PageHeader } from './components/ui/PageHeader';
import { ThemeToggle } from './components/ui/ThemeToggle';

import {
  IconDashboard,
  IconOrders,
  IconCases,
  IconShipments,
  IconContainers,
  IconDocuments,
  IconTasks,
  IconUsers,
  IconRoles,
  IconAudit,
  IconLogout,
  IconFileSpreadsheet
} from './components/Icons';

const MainApp: React.FC = () => {
  const { user, isAuthenticated, isBootstrapping, logout, hasPermission, authenticatedFetch, catalogPermissionCount } = useAuth();
  const [activeTab, setActiveTab] = useState<string>('dashboard');
  const [selectedBatchId, setSelectedBatchId] = useState<string | null>(null);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [sidebarCollapsed, setSidebarCollapsed] = useState<boolean>(() => {
    try {
      return localStorage.getItem('ict_sidebar_collapsed') === 'true';
    } catch {
      return false;
    }
  });

  const toggleSidebar = () => {
    setSidebarCollapsed((prev) => {
      const next = !prev;
      try {
        localStorage.setItem('ict_sidebar_collapsed', String(next));
      } catch {}
      return next;
    });
  };

  useEffect(() => {
    if (isAuthenticated) {
      setImportCaseServiceFetch(authenticatedFetch);
      setDocumentServiceFetch(authenticatedFetch);
    }
  }, [isAuthenticated, authenticatedFetch]);

  if (isBootstrapping) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', height: '100vh', background: 'var(--bg-base)', color: 'var(--accent-blue)', gap: '1rem' }}>
        <div className="pulse-dot" style={{ width: '24px', height: '24px' }} />
        <div style={{ fontWeight: 600, fontSize: 'var(--font-sm)' }}>Import Control Tower Yükleniyor...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <LoginView />;
  }

  if (user?.mustChangePassword) {
    return <ForceChangePasswordView />;
  }

  const menuSections = [
    {
      title: 'Operasyon',
      items: [
        { id: 'dashboard', label: 'Genel Bakış', perm: 'dashboard.view', icon: <IconDashboard /> },
        { id: 'purchase-orders', label: 'Satın Alma Siparişleri', perm: 'purchaseorders.view', icon: <IconOrders /> },
        { id: 'po-import', label: 'Excel İçe Aktarma', perm: 'purchaseorders.import', icon: <IconFileSpreadsheet /> },
        { id: 'po-history', label: 'Aktarım Geçmişi', perm: 'purchaseorders.import', icon: <IconFileSpreadsheet /> },
        { id: 'import-cases', label: 'İthalat Dosyaları', perm: 'importcases.view', icon: <IconCases /> },
        { id: 'shipments', label: 'Sevkiyatlar', perm: 'shipments.view', icon: <IconShipments /> },
        { id: 'containers', label: 'Konteyner Takibi', perm: 'containers.view', icon: <IconContainers /> },
        { id: 'documents', label: 'Evraklar', perm: 'documents.view', icon: <IconDocuments /> },
        { id: 'tasks', label: 'Görevlerim', perm: 'tasks.view_own', icon: <IconTasks /> }
      ]
    },
    {
      title: 'Yönetim & Güvenlik',
      items: [
        { id: 'users', label: 'Kullanıcı Yönetimi', perm: 'users.view', icon: <IconUsers /> },
        { id: 'roles', label: 'Rol Yönetimi', perm: 'roles.view', icon: <IconRoles /> },
        { id: 'audit', label: 'Audit Logları', perm: 'audit.view', icon: <IconAudit /> }
      ]
    }
  ];

  return (
    <div className={`app-layout ${sidebarCollapsed ? 'sidebar-collapsed' : ''}`}>
      {/* Mobile Drawer Overlay */}
      <div
        className={`sidebar-overlay ${mobileOpen ? 'mobile-open' : ''}`}
        onClick={() => setMobileOpen(false)}
      />

      {/* Sidebar */}
      <aside className={`app-sidebar ${mobileOpen ? 'mobile-open' : ''} ${sidebarCollapsed ? 'collapsed' : ''}`}>
        <div className="sidebar-header">
          <div className="brand-logo-box" title="Control Tower">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polygon points="12 2 2 7 12 12 22 7 12 2" />
              <polyline points="2 17 12 22 22 17" />
              <polyline points="2 12 12 17 22 12" />
            </svg>
          </div>
          <div className="brand-title-group">
            <h1>Control Tower</h1>
            <span>Import Tower v0.4</span>
          </div>
          <button
            className="sidebar-collapse-toggle"
            onClick={toggleSidebar}
            title={sidebarCollapsed ? "Menüyü Genişlet" : "Menüyü Daralt"}
            aria-label="Sol Menü Gizle / Göster"
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
              <line x1="9" y1="3" x2="9" y2="21" />
              <path d={sidebarCollapsed ? "M14 9l3 3-3 3" : "M16 15l-3-3 3-3"} />
            </svg>
          </button>
        </div>

        <nav className="sidebar-nav">
          {menuSections.map((sec, idx) => {
            const visibleItems = sec.items.filter(item => hasPermission(item.perm));
            if (visibleItems.length === 0) return null;
            return (
              <div key={idx}>
                <div className="nav-section-title">{sec.title}</div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.2rem' }}>
                  {visibleItems.map((item) => (
                    <button
                      key={item.id}
                      onClick={() => {
                        setActiveTab(item.id);
                        setMobileOpen(false);
                      }}
                      className={`nav-item-btn ${activeTab === item.id ? 'active' : ''}`}
                      title={item.label}
                    >
                      <span className="nav-icon">{item.icon}</span>
                      <span className="nav-text">{item.label}</span>
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
        </nav>

        {/* User Footer */}
        <div className="sidebar-user-footer">
          <div
            className="user-profile-card"
            onClick={() => {
              setActiveTab('profile');
              setMobileOpen(false);
            }}
            title="Profil ve Güvenlik Ayarları"
          >
            <div className="avatar-circle">
              {user?.fullName?.charAt(0) || 'U'}
            </div>
            <div className="user-info-text">
              <div className="user-name">{user?.fullName}</div>
              <div className="user-role-badge">{user?.roles.join(', ')}</div>
            </div>
          </div>

          <button onClick={logout} className="btn-logout" title="Güvenli Çıkış">
            <IconLogout />
            <span>Güvenli Çıkış</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <div className="app-main">
        <header className="app-topbar">
          <div className="topbar-left">
            <button
              className="mobile-menu-btn"
              onClick={() => setMobileOpen(!mobileOpen)}
              aria-label="Mobil Menü"
            >
              ☰
            </button>
            <button
              className="desktop-sidebar-toggle"
              onClick={toggleSidebar}
              title={sidebarCollapsed ? "Sol Menüyü Genişlet" : "Sol Menüyü Daralt"}
              aria-label="Masaüstü Menü Aç/Kapat"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                <line x1="9" y1="3" x2="9" y2="21" />
                <path d={sidebarCollapsed ? "M14 9l3 3-3 3" : "M16 15l-3-3 3-3"} />
              </svg>
            </button>
            <div className="topbar-search-box">
              <span>🔍</span>
              <input
                type="text"
                placeholder="Dosya, PO veya evrak ara..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </div>

          <div className="topbar-right-controls" style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)' }}>
            <ThemeToggle />
            <div className="connection-status-pill">
              <span className="pulse-dot" />
              <span>Sistem Çevrimiçi</span>
            </div>
          </div>
        </header>

        <main className="content-viewport">
          {activeTab === 'dashboard' && (
            <ProtectedRoute requiredPermission="dashboard.view">
              <PageHeader
                title="Genel Bakış"
                subtitle="Import Control Tower operasyonel durum ve erişim yetkileri"
              />
              <div className="kpi-grid" style={{ marginBottom: 'var(--space-6)' }}>
                <KPICard
                  title="Aktif Oturum"
                  value={user?.email || '-'}
                  subtext="Güvenli Oturum Aktif"
                  icon={<IconUsers />}
                />
                <KPICard
                  title="Erişim Rolü"
                  value={user?.roles[0] || '-'}
                  valueColor="var(--accent-blue)"
                  subtext="Sistem Yetkisi"
                  icon={<IconRoles />}
                />
                <KPICard
                  title="Aktif İzinler"
                  value={
                    catalogPermissionCount !== null
                      ? `${user?.permissions.length} / ${catalogPermissionCount}`
                      : `${user?.permissions.length}`
                  }
                  valueColor="var(--status-success)"
                  subtext={
                    user?.roles.includes('SystemAdmin')
                      ? 'Sistem Yöneticisi Yetkisi Tam'
                      : 'Atanmış İzinler'
                  }
                  icon={<IconAudit />}
                />
              </div>

              <Section title="Operasyonel Modüller">
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 'var(--space-4)' }}>
                  <div className="card" style={{ padding: 'var(--space-5)', cursor: 'pointer' }} onClick={() => setActiveTab('import-cases')}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', marginBottom: 'var(--space-2)' }}>
                      <div style={{ padding: 'var(--space-2)', borderRadius: 'var(--radius-md)', background: 'var(--primary-light)', color: 'var(--accent-blue)' }}>
                        <IconCases />
                      </div>
                      <h4 style={{ margin: 0, fontSize: 'var(--font-base)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>İthalat Dosyaları</h4>
                    </div>
                    <p style={{ margin: 0, fontSize: 'var(--font-sm)', color: 'var(--text-muted)' }}>
                      Aktif ithalat dosyalarını, sipariş kalemlerini ve konteyner eşleşmelerini takip edin.
                    </p>
                  </div>

                  <div className="card" style={{ padding: 'var(--space-5)', cursor: 'pointer' }} onClick={() => setActiveTab('purchase-orders')}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', marginBottom: 'var(--space-2)' }}>
                      <div style={{ padding: 'var(--space-2)', borderRadius: 'var(--radius-md)', background: 'rgba(16, 185, 129, 0.15)', color: 'var(--accent-emerald)' }}>
                        <IconOrders />
                      </div>
                      <h4 style={{ margin: 0, fontSize: 'var(--font-base)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>Satın Alma Siparişleri</h4>
                    </div>
                    <p style={{ margin: 0, fontSize: 'var(--font-sm)', color: 'var(--text-muted)' }}>
                      İçe aktarılan sipariş listelerini inceleyin ve dosyalara bağlayın.
                    </p>
                  </div>

                  <div className="card" style={{ padding: 'var(--space-5)', cursor: 'pointer' }} onClick={() => setActiveTab('documents')}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 'var(--space-3)', marginBottom: 'var(--space-2)' }}>
                      <div style={{ padding: 'var(--space-2)', borderRadius: 'var(--radius-md)', background: 'rgba(245, 158, 11, 0.15)', color: 'var(--accent-amber)' }}>
                        <IconDocuments />
                      </div>
                      <h4 style={{ margin: 0, fontSize: 'var(--font-base)', fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>İthalat Evrakları</h4>
                    </div>
                    <p style={{ margin: 0, fontSize: 'var(--font-sm)', color: 'var(--text-muted)' }}>
                      Gümrük ve nakliye evraklarını versiyonlarıyla birlikte yönetin.
                    </p>
                  </div>
                </div>
              </Section>
            </ProtectedRoute>
          )}

          {(activeTab === 'import-cases' || activeTab === 'shipments' || activeTab === 'containers') && (
            <ProtectedRoute requiredPermission={activeTab === 'shipments' ? 'shipments.view' : activeTab === 'containers' ? 'containers.view' : 'importcases.view'}>
              <ImportCaseListView
                onSelectCase={(caseId) => {
                  setSelectedCaseId(caseId);
                  setActiveTab('case-detail');
                }}
              />
            </ProtectedRoute>
          )}

          {activeTab === 'case-detail' && selectedCaseId && (
            <ProtectedRoute requiredPermission="importcases.view">
              <ImportCaseDetailView
                caseId={selectedCaseId}
                onBack={() => setActiveTab('import-cases')}
              />
            </ProtectedRoute>
          )}

          {activeTab === 'po-import' && (
            <ProtectedRoute requiredPermission="purchaseorders.import">
              <PurchaseOrderImportView
                onBatchCreated={(batchId) => {
                  setSelectedBatchId(batchId);
                  setActiveTab('po-preview');
                }}
              />
            </ProtectedRoute>
          )}

          {activeTab === 'po-preview' && selectedBatchId && (
            <ProtectedRoute requiredPermission="purchaseorders.import">
              <ImportPreviewView
                batchId={selectedBatchId}
                onBack={() => setActiveTab('po-import')}
                onConfirmSuccess={() => setActiveTab('purchase-orders')}
              />
            </ProtectedRoute>
          )}

          {activeTab === 'po-history' && (
            <ProtectedRoute requiredPermission="purchaseorders.import">
              <ImportHistoryView
                onSelectBatch={(batchId) => {
                  setSelectedBatchId(batchId);
                  setActiveTab('po-preview');
                }}
              />
            </ProtectedRoute>
          )}

          {activeTab === 'purchase-orders' && (
            <ProtectedRoute requiredPermission="purchaseorders.view">
              <PurchaseOrderListView
                onSelectOrder={(orderId) => {
                  setSelectedOrderId(orderId);
                  setActiveTab('po-detail');
                }}
              />
            </ProtectedRoute>
          )}

          {activeTab === 'po-detail' && selectedOrderId && (
            <ProtectedRoute requiredPermission="purchaseorders.view">
              <PurchaseOrderDetailView
                orderId={selectedOrderId}
                onBack={() => setActiveTab('purchase-orders')}
              />
            </ProtectedRoute>
          )}

          {activeTab === 'users' && (
            <ProtectedRoute requiredPermission="users.view">
              <UserManagementView />
            </ProtectedRoute>
          )}

          {activeTab === 'roles' && (
            <ProtectedRoute requiredPermission="roles.view">
              <RoleManagementView />
            </ProtectedRoute>
          )}

          {activeTab === 'audit' && (
            <ProtectedRoute requiredPermission="audit.view">
              <AuditLogsView />
            </ProtectedRoute>
          )}

          {activeTab === 'profile' && (
            <ProfileView />
          )}

          {activeTab === 'documents' && (
            <ProtectedRoute requiredPermission="documents.view">
              <DocumentListView />
            </ProtectedRoute>
          )}

          {!['dashboard', 'import-cases', 'shipments', 'containers', 'case-detail', 'documents', 'po-import', 'po-preview', 'po-history', 'purchase-orders', 'po-detail', 'users', 'roles', 'audit', 'profile'].includes(activeTab) && (
            <div className="panel">
              <EmptyState
                title="Modül Hazırlanıyor"
                description="Bu modül sonraki fazlarda entegre edilecektir."
                action={
                  <button className="btn btn-secondary btn-sm" onClick={() => setActiveTab('dashboard')}>
                    Genel Bakışa Dön
                  </button>
                }
              />
            </div>
          )}
        </main>
      </div>
    </div>
  );
};

export function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <MainApp />
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
