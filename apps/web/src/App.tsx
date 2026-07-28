import { useState, useEffect } from 'react';
import { 
  LayoutDashboard, 
  ShoppingBag, 
  FileCheck, 
  Truck, 
  Box, 
  CheckSquare, 
  FileText, 
  Activity, 
  RefreshCw,
  Server,
  Database,
  Clock
} from 'lucide-react';

interface SystemInfo {
  appName: string;
  version: string;
  environment: string;
  serverTimeUtc: string;
  serverTimeIstanbul: string;
  databaseStatus: string;
}

export default function App() {
  const [activeTab, setActiveTab] = useState('overview');
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);
  const [healthLive, setHealthLive] = useState<boolean | null>(null);
  const [healthReady, setHealthReady] = useState<boolean | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  const menuItems = [
    { id: 'overview', label: 'Genel Bakış', icon: LayoutDashboard },
    { id: 'orders', label: 'Satın Alma Siparişleri', icon: ShoppingBag },
    { id: 'imports', label: 'İthalat Dosyaları', icon: FileCheck },
    { id: 'shipments', label: 'Sevkiyatlar', icon: Truck },
    { id: 'containers', label: 'Konteynerler', icon: Box },
    { id: 'tasks', label: 'Görevler', icon: CheckSquare },
    { id: 'documents', label: 'Belgeler', icon: FileText },
    { id: 'system', label: 'Sistem Durumu', icon: Activity },
  ];

  const fetchSystemData = async () => {
    setLoading(true);
    try {
      // Fetch System Info
      const infoRes = await fetch('/api/v1/system/info');
      if (infoRes.ok) {
        const data = await infoRes.json();
        setSystemInfo(data);
      } else {
        setSystemInfo(null);
      }

      // Fetch Liveness
      const liveRes = await fetch('/health/live');
      setHealthLive(liveRes.ok);

      // Fetch Readiness
      const readyRes = await fetch('/health/ready');
      setHealthReady(readyRes.ok);
    } catch (err) {
      console.error('Error connecting to backend:', err);
      setSystemInfo(null);
      setHealthLive(false);
      setHealthReady(false);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSystemData();
    const interval = setInterval(fetchSystemData, 15000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="app-container">
      {/* Sidebar */}
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-icon">ICT</div>
          <div className="brand-text">
            <h1>Control Tower</h1>
            <span>Faz 00 Foundation</span>
          </div>
        </div>

        <ul className="nav-list">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = activeTab === item.id;
            return (
              <li
                key={item.id}
                className={`nav-item ${isActive ? 'active' : ''}`}
                onClick={() => setActiveTab(item.id)}
              >
                <Icon size={18} />
                <span>{item.label}</span>
              </li>
            );
          })}
        </ul>
      </aside>

      {/* Main Content Area */}
      <div className="main-wrapper">
        <header className="top-header">
          <div className="header-title">
            {menuItems.find((m) => m.id === activeTab)?.label}
          </div>
          <div className="header-actions">
            <span className="badge-environment">
              {systemInfo?.environment || 'FAZ-00'}
            </span>
            <button className="btn-refresh" onClick={fetchSystemData} disabled={loading}>
              <RefreshCw size={14} className={loading ? 'spin' : ''} />
            </button>
          </div>
        </header>

        <main className="content-area">
          {activeTab === 'overview' || activeTab === 'system' ? (
            <div>
              <h2>Sistem Temel Altyapı Durumu</h2>
              <p style={{ color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                Import Control Tower Faz 00 teknik altyapı metrikleri ve servis durumları.
              </p>

              <div className="dashboard-grid">
                {/* API Status Card */}
                <div className="card">
                  <div className="card-header">
                    <div className="card-title">
                      <Server size={18} color="var(--accent-blue)" />
                      <span>API Servis Durumu</span>
                    </div>
                    <span className={`status-indicator ${healthLive ? 'status-online' : 'status-offline'}`} />
                  </div>
                  <div className="info-list">
                    <div className="info-row">
                      <span className="info-label">Uygulama:</span>
                      <span className="info-value">{systemInfo?.appName || 'Bağlanıyor...'}</span>
                    </div>
                    <div className="info-row">
                      <span className="info-label">Sürüm:</span>
                      <span className="info-value">{systemInfo?.version || '-'}</span>
                    </div>
                    <div className="info-row">
                      <span className="info-label">Liveness (/health/live):</span>
                      <span className="info-value">{healthLive ? '200 OK' : 'Hata'}</span>
                    </div>
                    <div className="info-row">
                      <span className="info-label">Readiness (/health/ready):</span>
                      <span className="info-value">{healthReady ? '200 OK' : 'Bekleniyor'}</span>
                    </div>
                  </div>
                </div>

                {/* Database Status Card */}
                <div className="card">
                  <div className="card-header">
                    <div className="card-title">
                      <Database size={18} color="var(--accent-emerald)" />
                      <span>Veritabanı (PostgreSQL 18)</span>
                    </div>
                    <span className={`status-indicator ${systemInfo?.databaseStatus === 'Connected' ? 'status-online' : 'status-offline'}`} />
                  </div>
                  <div className="info-list">
                    <div className="info-row">
                      <span className="info-label">PostgreSQL Bağlantısı:</span>
                      <span className="info-value">{systemInfo?.databaseStatus || 'Kontrol ediliyor...'}</span>
                    </div>
                    <div className="info-row">
                      <span className="info-label">Migration Tablosu:</span>
                      <span className="info-value">system_migrations (Aktif)</span>
                    </div>
                    <div className="info-row">
                      <span className="info-label">ORM:</span>
                      <span className="info-value">EF Core 10</span>
                    </div>
                  </div>
                </div>

                {/* Server Time Card */}
                <div className="card">
                  <div className="card-header">
                    <div className="card-title">
                      <Clock size={18} color="var(--accent-cyan)" />
                      <span>Zaman Dönüşüm Servisi</span>
                    </div>
                  </div>
                  <div className="info-list">
                    <div className="info-row">
                      <span className="info-label">Sunucu UTC Zamanı:</span>
                      <span className="info-value">
                        {systemInfo?.serverTimeUtc 
                          ? new Date(systemInfo.serverTimeUtc).toISOString() 
                          : '-'}
                      </span>
                    </div>
                    <div className="info-row">
                      <span className="info-label">Türkiye Gösterim Zamanı:</span>
                      <span className="info-value">{systemInfo?.serverTimeIstanbul || '-'}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div>
              <h2>{menuItems.find((m) => m.id === activeTab)?.label}</h2>
              <p style={{ color: 'var(--text-muted)', marginBottom: '1.5rem', marginTop: '0.5rem' }}>
                Bu modül henüz kurulmadı (Faz 01 sonrası aktif edilecek).
              </p>
              <div className="placeholder-box">
                <p>Faz 00 Temel Altyapı modülüdür. İş modülleri ve veritabanı tabloları sonraki fazlarda eklenecektir.</p>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
}
