import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { Card } from '../components/ui/Card';
import { Badge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Modal } from '../components/ui/Modal';
import { LoadingSkeleton } from '../components/ui/FeedbackState';
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
      <PageHeader
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <IconRoles />
            <span>Rol ve İzin Yönetimi (İzin Kataloğu)</span>
          </div>
        }
        actions={
          <Badge variant="purple">
            Toplam {allPermissions.length} İzin Aktif
          </Badge>
        }
      />

      {loading ? (
        <LoadingSkeleton rows={5} height="120px" />
      ) : (
        <div className="card-grid">
          {roles.map((role) => (
            <Card key={role.id} style={{ display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}>
              <div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 'var(--space-2)' }}>
                  <div style={{ fontWeight: 'var(--weight-bold)', fontSize: 'var(--font-md)', color: 'var(--text-main)' }}>
                    {role.name}
                  </div>
                  {role.isSystemRole && (
                    <Badge variant="cyan">Sistem Rolü</Badge>
                  )}
                </div>
                <p style={{ color: 'var(--text-muted)', fontSize: 'var(--font-sm)', marginBottom: 'var(--space-4)', lineHeight: 'var(--lh-normal)' }}>
                  {role.description}
                </p>
              </div>

              <div style={{ borderTop: '1px solid var(--border-subtle)', paddingTop: 'var(--space-3)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-dim)' }}>
                  Atanmış İzin: <strong style={{ color: 'var(--accent-blue)' }}>{role.permissions.length}</strong> / {allPermissions.length}
                </div>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setSelectedRole(role)}
                >
                  İzin Detayları
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Role Permission Detail Modal */}
      <Modal
        isOpen={!!selectedRole}
        onClose={() => setSelectedRole(null)}
        title={selectedRole ? `${selectedRole.name} - İzin Detay Kataloğu` : ''}
        maxWidth="680px"
        footer={
          <Button variant="primary" onClick={() => setSelectedRole(null)}>
            Kapat
          </Button>
        }
      >
        {selectedRole && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-4)' }}>
            <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>
              {selectedRole.description}
            </div>
            {Array.from(new Set(allPermissions.map(p => p.groupName))).map(group => {
              const groupPerms = allPermissions.filter(p => p.groupName === group);
              return (
                <div key={group} className="panel" style={{ padding: 'var(--space-3)', margin: 0 }}>
                  <div style={{ fontSize: 'var(--font-xs)', fontWeight: 'var(--weight-bold)', color: 'var(--accent-blue)', textTransform: 'uppercase', marginBottom: 'var(--space-2)' }}>
                    {group} Modülü
                  </div>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '0.4rem' }}>
                    {groupPerms.map(perm => {
                      const hasIt = selectedRole.permissions.includes(perm.code);
                      return (
                        <div key={perm.code} style={{
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                          padding: '0.35rem 0.5rem',
                          borderRadius: 'var(--radius-sm)',
                          background: hasIt ? 'var(--status-success-bg)' : 'rgba(255, 255, 255, 0.02)',
                          border: hasIt ? '1px solid var(--status-success-border)' : '1px solid var(--border-subtle)'
                        }}>
                          <span className="font-mono" style={{ fontSize: 'var(--font-xs)', color: hasIt ? 'var(--text-main)' : 'var(--text-dim)' }}>
                            {perm.code}
                          </span>
                          <Badge variant={hasIt ? 'emerald' : 'rose'} style={{ fontSize: '0.65rem' }}>
                            {hasIt ? 'Var' : 'Yok'}
                          </Badge>
                        </div>
                      );
                    })}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </Modal>
    </div>
  );
};
