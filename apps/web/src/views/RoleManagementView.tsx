import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { IconRoles } from '../components/Icons';

interface Role {
  id: string;
  name: string;
  description: string;
  isSystemRole: boolean;
  permissions: string[];
}

interface PermissionItem {
  code: string;
  groupName: string;
  description: string;
}

export const RoleManagementView: React.FC = () => {
  const { authenticatedFetch } = useAuth();
  const [roles, setRoles] = useState<Role[]>([]);
  const [allPermissions, setAllPermissions] = useState<PermissionItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [rolesRes, permsRes] = await Promise.all([
        authenticatedFetch('/api/v1/admin/roles'),
        authenticatedFetch('/api/v1/admin/permissions')
      ]);
      if (rolesRes.ok && permsRes.ok) {
        setRoles(await rolesRes.json());
        setAllPermissions(await permsRes.json());
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <div className="panel-title">
            <IconRoles />
            <span>Rol ve İzin Yönetimi (İzin Kataloğu)</span>
          </div>
          <div className="badge badge-purple" style={{ fontSize: '0.8rem' }}>
            Toplam {allPermissions.length} İzin Aktif
          </div>
        </div>

        {loading ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>Rol matrisi yükleniyor...</div>
        ) : (
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '1.25rem' }}>
            {roles.map((role) => (
              <div key={role.id} className="card" style={{ display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
                <div>
                  <div className="card-header" style={{ marginBottom: '0.75rem' }}>
                    <div style={{ fontWeight: 700, fontSize: '1.05rem', color: '#f8fafc' }}>
                      {role.name}
                    </div>
                    {role.isSystemRole && (
                      <span className="badge badge-cyan">Sistem Rolü</span>
                    )}
                  </div>
                  <p style={{ color: '#94a3b8', fontSize: '0.85rem', marginBottom: '1.25rem', lineHeight: 1.5 }}>
                    {role.description}
                  </p>
                </div>

                <div style={{ borderTop: '1px solid rgba(51, 65, 85, 0.4)', paddingTop: '1rem', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <div style={{ fontSize: '0.8rem', color: '#64748b' }}>
                    Atanmış İzin: <strong style={{ color: '#38bdf8' }}>{role.permissions.length}</strong> / {allPermissions.length}
                  </div>
                  <button
                    className="btn-secondary btn-sm"
                    onClick={() => setSelectedRole(role)}
                  >
                    İzin Detayları
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Role Permission Detail Modal */}
      {selectedRole && (
        <div className="modal-overlay">
          <div className="modal-container" style={{ maxWidth: '680px' }}>
            <div className="modal-header">
              <div>
                <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: '#f8fafc' }}>
                  {selectedRole.name} - İzin Detay Kataloğu
                </h3>
                <div style={{ fontSize: '0.78rem', color: '#94a3b8', marginTop: '0.2rem' }}>
                  {selectedRole.description}
                </div>
              </div>
              <button className="btn-secondary btn-sm" onClick={() => setSelectedRole(null)}>✕</button>
            </div>
            <div className="modal-body">
              <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                {Array.from(new Set(allPermissions.map(p => p.groupName))).map(group => {
                  const groupPerms = allPermissions.filter(p => p.groupName === group);
                  return (
                    <div key={group} style={{ background: 'rgba(15, 23, 42, 0.6)', padding: '1rem', borderRadius: '10px', border: '1px solid var(--border-color)' }}>
                      <div style={{ fontSize: '0.8rem', fontWeight: 700, color: '#38bdf8', textTransform: 'uppercase', marginBottom: '0.6rem' }}>
                        {group} Modülü
                      </div>
                      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '0.5rem' }}>
                        {groupPerms.map(perm => {
                          const hasIt = selectedRole.permissions.includes(perm.code);
                          return (
                            <div key={perm.code} style={{
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'space-between',
                              padding: '0.4rem 0.6rem',
                              borderRadius: '6px',
                              background: hasIt ? 'rgba(16, 185, 129, 0.08)' : 'rgba(255, 255, 255, 0.02)',
                              border: hasIt ? '1px solid rgba(16, 185, 129, 0.2)' : '1px solid rgba(255, 255, 255, 0.04)'
                            }}>
                              <span style={{ fontSize: '0.78rem', fontFamily: 'monospace', color: hasIt ? '#f8fafc' : '#64748b' }}>{perm.code}</span>
                              <span className={hasIt ? 'badge badge-emerald' : 'badge badge-rose'} style={{ fontSize: '0.68rem', padding: '0.1rem 0.4rem' }}>
                                {hasIt ? 'Var' : 'Yok'}
                              </span>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn-primary" onClick={() => setSelectedRole(null)}>
                Kapat
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
