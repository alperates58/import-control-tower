import React, { useEffect, useState } from 'react';
import { useAuth, UserProfile } from '../context/AuthContext';
import { IconUsers } from '../components/Icons';

export const UserManagementView: React.FC = () => {
  const { authenticatedFetch } = useAuth();
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [tempPasswordModal, setTempPasswordModal] = useState<string | null>(null);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await authenticatedFetch('/api/v1/admin/users');
      if (!res.ok) throw new Error('Kullanıcılar yüklenemedi.');
      const data = await res.json();
      setUsers(data);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, []);

  const handleToggleStatus = async (user: UserProfile) => {
    const endpoint = user.isActive ? `/api/v1/admin/users/${user.id}/disable` : `/api/v1/admin/users/${user.id}/enable`;
    try {
      const res = await authenticatedFetch(endpoint, { method: 'POST' });
      if (!res.ok) {
        const err = await res.json();
        alert(err.detail || 'İşlem başarısız.');
        return;
      }
      fetchUsers();
    } catch (err: any) {
      alert(err.message);
    }
  };

  const handleResetPassword = async (userId: string) => {
    if (!confirm('Geçici parola üretilecek ve aktif oturumlar kapatılacak. Onaylıyor musunuz?')) return;
    try {
      const res = await authenticatedFetch(`/api/v1/admin/users/${userId}/reset-password`, { method: 'POST' });
      if (!res.ok) {
        const err = await res.json();
        alert(err.detail || 'Parola sıfırlanamadı.');
        return;
      }
      const data = await res.json();
      setTempPasswordModal(data.temporaryPassword);
    } catch (err: any) {
      alert(err.message);
    }
  };

  const filteredUsers = users.filter(u => 
    u.fullName.toLowerCase().includes(search.toLowerCase()) || 
    u.email.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <div className="panel-title">
            <IconUsers />
            <span>Kullanıcı Yönetimi ({users.length} Kullanıcı)</span>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <input
              type="text"
              className="form-input"
              placeholder="Kullanıcı veya E-Posta Ara..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ width: '240px', padding: '0.4rem 0.75rem', fontSize: '0.82rem' }}
            />
          </div>
        </div>

        {error && (
          <div className="badge-rose" style={{ width: '100%', padding: '0.75rem', borderRadius: '8px', marginBottom: '1rem' }}>
            {error}
          </div>
        )}

        {loading ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>Kullanıcı listesi yükleniyor...</div>
        ) : (
          <div className="data-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Ad Soyad</th>
                  <th>E-Posta / ID</th>
                  <th>Roller</th>
                  <th>Durum</th>
                  <th style={{ textAlign: 'right' }}>İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {filteredUsers.map((u) => (
                  <tr key={u.id}>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                        <div className="avatar-circle" style={{ width: '32px', height: '32px', fontSize: '0.8rem' }}>
                          {u.fullName.charAt(0)}
                        </div>
                        <span style={{ fontWeight: 600 }}>{u.fullName}</span>
                      </div>
                    </td>
                    <td>
                      <div>{u.email}</div>
                      <div style={{ fontSize: '0.72rem', color: '#64748b', fontFamily: 'monospace' }}>{u.id}</div>
                    </td>
                    <td>
                      <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap' }}>
                        {u.roles.map(r => (
                          <span key={r} className="badge badge-cyan">{r}</span>
                        ))}
                      </div>
                    </td>
                    <td>
                      <span className={`badge ${u.isActive ? 'badge-emerald' : 'badge-rose'}`}>
                        {u.isActive ? '● Aktif' : '○ Pasif'}
                      </span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                        <button
                          onClick={() => handleToggleStatus(u)}
                          className="btn-secondary btn-sm"
                        >
                          {u.isActive ? 'Pasifleştir' : 'Aktifleştir'}
                        </button>
                        <button
                          onClick={() => handleResetPassword(u.id)}
                          className="btn-secondary btn-sm"
                          style={{ borderColor: 'rgba(245, 158, 11, 0.4)', color: '#f59e0b' }}
                        >
                          Parola Sıfırla
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Temporary Password Modal */}
      {tempPasswordModal && (
        <div className="modal-overlay">
          <div className="modal-container">
            <div className="modal-header">
              <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: '#f8fafc' }}>
                🔑 Kriptografik Geçici Parola Üretildi
              </h3>
            </div>
            <div className="modal-body" style={{ textAlign: 'center' }}>
              <p style={{ color: '#94a3b8', fontSize: '0.9rem', marginBottom: '1.25rem' }}>
                Kullanıcı için üretilen tek kullanımlık geçici parola aşağıdadır. Güvenlik nedeniyle bu parola veritabanında saklanmaz ve <strong>sadece bir kez</strong> gösterilir.
              </p>
              <div style={{
                background: '#090d16',
                border: '1px border #0284c7',
                padding: '1rem 1.5rem',
                borderRadius: '10px',
                fontSize: '1.4rem',
                fontWeight: 800,
                color: '#38bdf8',
                fontFamily: 'monospace',
                letterSpacing: '0.1em',
                marginBottom: '1rem',
                userSelect: 'all'
              }}>
                {tempPasswordModal}
              </div>
              <div style={{ fontSize: '0.78rem', color: '#f43f5e' }}>
                * Kullanıcı ilk girişinde zorunlu olarak parola değiştirme ekranına yönlendirilecektir.
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-primary" onClick={() => setTempPasswordModal(null)}>
                Anladım, Kapat
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
