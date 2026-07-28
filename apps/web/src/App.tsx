import React, { useState } from 'react';
import { AuthProvider, useAuth } from './context/AuthContext';
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
  IconFinancial,
  IconLogout,
  IconFileSpreadsheet
} from './components/Icons';

const MainApp: React.FC = () => {
  const { user, isAuthenticated, isBootstrapping, logout, hasPermission } = useAuth();
  const [activeTab, setActiveTab] = useState<string>('dashboard');
  const [selectedBatchId, setSelectedBatchId] = useState<string | null>(null);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);

  if (isBootstrapping) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', height: '100vh', background: '#090d16', color: '#38bdf8', gap: '1rem', fontFamily: 'system-ui' }}>
        <div style={{ width: '40px', height: '40px', border: '3px solid #1e293b', borderTopColor: '#38bdf8', borderRadius: '50%', animation: 'spin 1s linear infinite' }}></div>
        <div style={{ fontWeight: 600, fontSize: '0.95rem' }}>Import Control Tower Yükleniyor...</div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <LoginView />;
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
        { id: 'audit', label: 'Audit Logları', perm: 'audit.view', icon: <IconAudit /> },
        { id: 'financials', label: 'Finansal Analiz', perm: 'financial.view', icon: <IconFinancial /> }
      ]
    }
  ];

  const getActiveTabTitle = () => {
    for (const sec of menuSections) {
      const found = sec.items.find(i => i.id === activeTab);
      if (found) return found.label;
    }
    if (activeTab === 'profile') return 'Kullanıcı Profili';
    if (activeTab === 'po-preview') return 'İçe Aktarma Ön İzlemesi';
    if (activeTab === 'po-detail') return 'Sipariş Detayı';
    return 'Genel Bakış';
  };

  return (
    <div className="app-layout">
      {/* Sidebar */}
      <aside className="app-sidebar">
        <div className="sidebar-header">
          <div className="brand-logo-box">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polygon points="12 2 2 7 12 12 22 7 12 2" />
              <polyline points="2 17 12 22 22 17" />
              <polyline points="2 12 12 17 22 12" />
            </svg>
          </div>
          <div className="brand-title-group">
            <h1>Control Tower</h1>
            <span>Import Tower v0.2</span>
          </div>
        </div>

        <nav className="sidebar-nav">
          {menuSections.map((sec, idx) => {
            const visibleItems = sec.items.filter(item => hasPermission(item.perm));
            if (visibleItems.length === 0) return null;
            return (
              <div key={idx}>
                <div className="nav-section-title">{sec.title}</div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                  {visibleItems.map((item) => (
                    <button
                      key={item.id}
                      onClick={() => setActiveTab(item.id)}
                      className={`nav-item-btn ${activeTab === item.id ? 'active' : ''}`}
                    >
                      <span className="nav-icon">{item.icon}</span>
                      <span>{item.label}</span>
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
            onClick={() => setActiveTab('profile')}
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

          <button onClick={logout} className="btn-logout">
            <IconLogout />
            <span>Güvenli Çıkış</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <div className="app-main">
        <header className="app-topbar">
          <div className="topbar-title-group">
            <h2>{getActiveTabTitle()}</h2>
          </div>

          <div className="topbar-right-controls">
            <div className="status-badge-pill">
              <span className="pulse-dot"></span>
              <span>Canlı Bağlantı — PostgreSQL 18</span>
            </div>
          </div>
        </header>

        <main className="content-viewport">
          {activeTab === 'dashboard' && (
            <ProtectedRoute requiredPermission="dashboard.view">
              <div className="kpi-grid">
                <div className="kpi-card">
                  <div className="kpi-card-header">
                    <span className="kpi-title">Aktif Oturum</span>
                    <div className="kpi-icon-box"><IconUsers /></div>
                  </div>
                  <div className="kpi-value">{user?.email}</div>
                  <div className="kpi-subtext">Güvenli JWT / Refresh Cookie Aktif</div>
                </div>

                <div className="kpi-card">
                  <div className="kpi-card-header">
                    <span className="kpi-title">Erişim Rolü</span>
                    <div className="kpi-icon-box"><IconRoles /></div>
                  </div>
                  <div className="kpi-value" style={{ color: '#38bdf8' }}>{user?.roles[0]}</div>
                  <div className="kpi-subtext">Tam Yetkili Sistem Yöneticisi</div>
                </div>

                <div className="kpi-card">
                  <div className="kpi-card-header">
                    <span className="kpi-title">Toplam İzin</span>
                    <div className="kpi-icon-box"><IconAudit /></div>
                  </div>
                  <div className="kpi-value" style={{ color: '#10b981' }}>{user?.permissions.length} / 32</div>
                  <div className="kpi-subtext">Katalog İzin Sayısı Doğrulandı</div>
                </div>
              </div>

              <div className="panel">
                <div className="panel-header">
                  <div className="panel-title">
                    <IconDashboard />
                    <span>Faz 02 — Excel Sipariş İçe Aktarma Aktif</span>
                  </div>
                </div>
                <div style={{ color: '#94a3b8', lineHeight: 1.6, fontSize: '0.95rem' }}>
                  <p style={{ marginBottom: '1rem' }}>
                    Açık satın alma siparişlerinizi Excel (.xlsx) üzerinden güvenli ve transactional biçimde yüklemek için sol menüdeki <strong>Excel İçe Aktarma</strong> sekmesini kullanabilirsiniz.
                  </p>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1rem', marginTop: '1.5rem' }}>
                    <div style={{ background: 'rgba(15, 23, 42, 0.6)', padding: '1rem', borderRadius: '10px', border: '1px solid rgba(56, 189, 248, 0.2)' }}>
                      <div style={{ fontWeight: 600, color: '#38bdf8', marginBottom: '0.4rem' }}>📊 Forward-Only ExcelDataReader</div>
                      <div style={{ fontSize: '0.85rem' }}>Bütün workbook belleğe yüklenmeden yüksek hızlı veri okuma sağlandı.</div>
                    </div>
                    <div style={{ background: 'rgba(15, 23, 42, 0.6)', padding: '1rem', borderRadius: '10px', border: '1px solid rgba(16, 185, 129, 0.2)' }}>
                      <div style={{ fontWeight: 600, color: '#10b981', marginBottom: '0.4rem' }}>🛡️ Idempotent & Single Transaction</div>
                      <div style={{ fontSize: '0.85rem' }}>Tek transaction ile veri bütünlüğü ve idempotency request takibi kuruldu.</div>
                    </div>
                  </div>
                </div>
              </div>
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

          {!['dashboard', 'po-import', 'po-preview', 'po-history', 'purchase-orders', 'po-detail', 'users', 'roles', 'audit', 'profile'].includes(activeTab) && (
            <div className="panel" style={{ textAlign: 'center', padding: '4rem 2rem' }}>
              <div style={{ fontSize: '2.5rem', marginBottom: '1rem' }}>📦</div>
              <h2 style={{ fontSize: '1.3rem', fontWeight: 700, marginBottom: '0.5rem' }}>
                {menuSections.flatMap(s => s.items).find(m => m.id === activeTab)?.label}
              </h2>
              <p style={{ color: '#94a3b8', maxWidth: '500px', margin: '0 auto 1.5rem' }}>
                Bu modül sonraki fazlarda entegre edilecektir.
              </p>
              <button className="btn-secondary" onClick={() => setActiveTab('dashboard')}>
                Genel Bakışa Dön
              </button>
            </div>
          )}
        </main>
      </div>
    </div>
  );
};

export function App() {
  return (
    <AuthProvider>
      <MainApp />
    </AuthProvider>
  );
}

export default App;
