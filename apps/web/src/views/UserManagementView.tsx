import React, { useEffect, useState } from 'react';
import { useAuth, UserProfile } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { Button } from '../components/ui/Button';
import { SearchInput } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { DataTable, Column } from '../components/ui/DataTable';
import { DropdownMenu } from '../components/ui/DropdownMenu';
import { Drawer } from '../components/ui/Drawer';
import { Modal, ConfirmDialog } from '../components/ui/Modal';
import { DetailField } from '../components/ui/Card';
import { IconUsers } from '../components/Icons';

export const UserManagementView: React.FC = () => {
  const { authenticatedFetch } = useAuth();
  const [users, setUsers] = useState<UserProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  
  // Drawer & Modal States
  const [selectedUserForDrawer, setSelectedUserForDrawer] = useState<UserProfile | null>(null);
  const [toggleStatusTarget, setToggleStatusTarget] = useState<UserProfile | null>(null);
  const [resetPassTargetId, setResetPassTargetId] = useState<string | null>(null);
  const [tempPasswordModal, setTempPasswordModal] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

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

  const executeToggleStatus = async () => {
    if (!toggleStatusTarget) return;
    setActionLoading(true);
    const endpoint = toggleStatusTarget.isActive
      ? `/api/v1/admin/users/${toggleStatusTarget.id}/disable`
      : `/api/v1/admin/users/${toggleStatusTarget.id}/enable`;

    try {
      const res = await authenticatedFetch(endpoint, { method: 'POST' });
      if (!res.ok) {
        const err = await res.json();
        alert(err.detail || 'İşlem başarısız.');
        return;
      }
      setToggleStatusTarget(null);
      fetchUsers();
    } catch (err: any) {
      alert(err.message);
    } finally {
      setActionLoading(false);
    }
  };

  const executeResetPassword = async () => {
    if (!resetPassTargetId) return;
    setActionLoading(true);
    try {
      const res = await authenticatedFetch(`/api/v1/admin/users/${resetPassTargetId}/reset-password`, { method: 'POST' });
      if (!res.ok) {
        const err = await res.json();
        alert(err.detail || 'Parola sıfırlanamadı.');
        return;
      }
      const data = await res.json();
      setResetPassTargetId(null);
      setTempPasswordModal(data.temporaryPassword);
    } catch (err: any) {
      alert(err.message);
    } finally {
      setActionLoading(false);
    }
  };

  const filteredUsers = users.filter(u =>
    u.fullName.toLowerCase().includes(search.toLowerCase()) ||
    u.email.toLowerCase().includes(search.toLowerCase())
  );

  const columns: Column<UserProfile>[] = [
    {
      key: 'fullName',
      header: 'Ad Soyad',
      render: (u) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <div className="avatar-circle">
            {u.fullName.charAt(0)}
          </div>
          <div>
            <div style={{ fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>{u.fullName}</div>
          </div>
        </div>
      )
    },
    {
      key: 'email',
      header: 'E-Posta / ID',
      render: (u) => (
        <div>
          <div>{u.email}</div>
          <div className="font-mono" style={{ fontSize: 'var(--font-xs)', color: 'var(--text-dim)' }}>{u.id}</div>
        </div>
      )
    },
    {
      key: 'roles',
      header: 'Roller',
      render: (u) => (
        <div style={{ display: 'flex', gap: '0.25rem', flexWrap: 'wrap' }}>
          {u.roles.map(r => (
            <Badge key={r} variant="cyan">{r}</Badge>
          ))}
        </div>
      )
    },
    {
      key: 'isActive',
      header: 'Durum',
      render: (u) => (
        <Badge variant={u.isActive ? 'emerald' : 'rose'}>
          {u.isActive ? '● Aktif' : '○ Pasif'}
        </Badge>
      )
    },
    {
      key: 'actions',
      header: 'İşlemler',
      align: 'right',
      render: (u) => (
        <DropdownMenu
          items={[
            {
              label: 'Kullanıcı Detayı',
              onClick: () => setSelectedUserForDrawer(u)
            },
            {
              label: u.isActive ? 'Pasifleştir' : 'Aktifleştir',
              isDanger: u.isActive,
              onClick: () => setToggleStatusTarget(u)
            },
            {
              label: 'Parola Sıfırla',
              onClick: () => setResetPassTargetId(u.id)
            }
          ]}
        />
      )
    }
  ];

  return (
    <div>
      <PageHeader
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <IconUsers />
            <span>Kullanıcı Yönetimi ({users.length} Kullanıcı)</span>
          </div>
        }
        actions={
          <div style={{ width: '260px' }}>
            <SearchInput
              placeholder="Kullanıcı veya E-Posta Ara..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        }
      />

      {error && (
        <div style={{ marginBottom: 'var(--space-4)' }}>
          <Badge variant="rose" style={{ width: '100%', padding: '0.75rem' }}>
            {error}
          </Badge>
        </div>
      )}

      <DataTable
        columns={columns}
        data={filteredUsers}
        keyExtractor={(u) => u.id}
        isLoading={loading}
        emptyMessage="Kullanıcı bulunamadı."
      />

      {/* User Detail Drawer */}
      <Drawer
        isOpen={!!selectedUserForDrawer}
        onClose={() => setSelectedUserForDrawer(null)}
        title="Kullanıcı Detay Profili"
        footer={
          <Button variant="secondary" onClick={() => setSelectedUserForDrawer(null)}>
            Kapat
          </Button>
        }
      >
        {selectedUserForDrawer && (
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem', paddingBottom: '1rem', borderBottom: '1px solid var(--border-subtle)' }}>
              <div className="avatar-circle" style={{ width: '48px', height: '48px', fontSize: '1.2rem' }}>
                {selectedUserForDrawer.fullName.charAt(0)}
              </div>
              <div>
                <h4 style={{ fontSize: 'var(--font-md)', fontWeight: 'var(--weight-bold)', color: 'var(--text-main)' }}>
                  {selectedUserForDrawer.fullName}
                </h4>
                <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>
                  {selectedUserForDrawer.email}
                </div>
              </div>
            </div>

            <DetailField label="Kullanıcı ID" value={selectedUserForDrawer.id} isMono />
            <DetailField label="Hesap Durumu" value={
              <Badge variant={selectedUserForDrawer.isActive ? 'emerald' : 'rose'}>
                {selectedUserForDrawer.isActive ? 'Aktif Hesap' : 'Pasif Hesap'}
              </Badge>
            } />
            <DetailField label="Atanmış Roller" value={
              <div style={{ display: 'flex', gap: '0.35rem', flexWrap: 'wrap', marginTop: '0.2rem' }}>
                {selectedUserForDrawer.roles.map(r => (
                  <Badge key={r} variant="cyan">{r}</Badge>
                ))}
              </div>
            } />
            <DetailField label="İzinler" value={
              <div style={{ display: 'flex', gap: '0.25rem', flexWrap: 'wrap', marginTop: '0.2rem' }}>
                {selectedUserForDrawer.permissions.map(p => (
                  <span key={p} className="font-mono" style={{ fontSize: '0.72rem', background: 'rgba(255,255,255,0.05)', padding: '0.15rem 0.4rem', borderRadius: '4px', color: 'var(--text-secondary)' }}>
                    {p}
                  </span>
                ))}
              </div>
            } />
          </div>
        )}
      </Drawer>

      {/* Confirm Dialog for Status Toggle */}
      <ConfirmDialog
        isOpen={!!toggleStatusTarget}
        onClose={() => setToggleStatusTarget(null)}
        onConfirm={executeToggleStatus}
        title={toggleStatusTarget?.isActive ? 'Kullanıcıyı Pasifleştir' : 'Kullanıcıyı Aktifleştir'}
        message={`"${toggleStatusTarget?.fullName}" adlı kullanıcının durumunu değiştirmek istediğinize emin misiniz?`}
        confirmText={toggleStatusTarget?.isActive ? 'Pasifleştir' : 'Aktifleştir'}
        isDanger={toggleStatusTarget?.isActive}
        isLoading={actionLoading}
      />

      {/* Confirm Dialog for Password Reset */}
      <ConfirmDialog
        isOpen={!!resetPassTargetId}
        onClose={() => setResetPassTargetId(null)}
        onConfirm={executeResetPassword}
        title="Parola Sıfırlama Onayı"
        message="Geçici bir parola üretilecek ve aktif tüm oturumlar sonlandırılacaktır. Onaylıyor musunuz?"
        confirmText="Parolayı Sıfırla"
        isDanger
        isLoading={actionLoading}
      />

      {/* Temporary Password Modal */}
      <Modal
        isOpen={!!tempPasswordModal}
        onClose={() => setTempPasswordModal(null)}
        title="🔑 Kriptografik Geçici Parola Üretildi"
        footer={
          <Button variant="primary" onClick={() => setTempPasswordModal(null)}>
            Anladım, Kapat
          </Button>
        }
      >
        <div style={{ textAlign: 'center' }}>
          <p style={{ color: 'var(--text-muted)', fontSize: 'var(--font-sm)', marginBottom: '1.25rem' }}>
            Kullanıcı için üretilen tek kullanımlık geçici parola aşağıdadır. Güvenlik nedeniyle bu parola veritabanında saklanmaz ve <strong>sadece bir kez</strong> gösterilir.
          </p>
          <div className="font-mono" style={{
            background: 'var(--bg-base)',
            border: '1px solid var(--primary)',
            padding: '1rem 1.5rem',
            borderRadius: 'var(--radius-md)',
            fontSize: '1.4rem',
            fontWeight: 'var(--weight-bold)',
            color: 'var(--accent-blue)',
            letterSpacing: '0.1em',
            marginBottom: '1rem',
            userSelect: 'all'
          }}>
            {tempPasswordModal}
          </div>
          <div style={{ fontSize: 'var(--font-xs)', color: 'var(--accent-rose)' }}>
            * Kullanıcı ilk girişinde zorunlu olarak parola değiştirme ekranına yönlendirilecektir.
          </div>
        </div>
      </Modal>
    </div>
  );
};
