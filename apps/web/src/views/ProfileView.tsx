import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { Section, Card, DetailField } from '../components/ui/Card';
import { Badge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Input, FormField } from '../components/ui/Input';

export const ProfileView: React.FC = () => {
  const { user, changePassword, catalogPermissionCount } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [msg, setMsg] = useState<{ text: string; error: boolean } | null>(null);

  if (!user) return null;

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(null);
    try {
      await changePassword(currentPassword, newPassword);
      setMsg({ text: 'Parola başarıyla değiştirildi. Oturum güvenlik güncellemesi nedeniyle yeniden başlatılıyor...', error: false });
      setCurrentPassword('');
      setNewPassword('');
    } catch (err: any) {
      setMsg({ text: err.message || 'Parola değiştirilemedi.', error: true });
    }
  };

  return (
    <div style={{ maxWidth: '720px' }}>
      <PageHeader
        title="Kullanıcı Profili ve Oturum Bilgileri"
        subtitle="Güvenlik ayarlarınızı ve yetki detaylarınızı inceleyin"
      />

      {user.mustChangePassword && (
        <div style={{ marginBottom: 'var(--space-5)' }}>
          <Badge variant="rose" style={{ width: '100%', padding: '0.85rem 1rem', display: 'block', fontSize: 'var(--font-sm)', lineHeight: 'var(--lh-normal)' }}>
            <strong>🚨 Zorunlu Parola Değişimi:</strong> Hesabınıza geçici parola veya ilk seed parolası ile giriş yapılmıştır. Lütfen güvenliğiniz için aşağıdan parolanızı derhal güncelleyin.
          </Badge>
        </div>
      )}

      <Section
        title="Oturum & Yetki Bilgileri"
        action={<Badge variant="emerald">Aktif Oturum</Badge>}
      >
        <div className="card-grid">
          <Card>
            <DetailField label="Ad Soyad" value={user.fullName} />
          </Card>
          <Card>
            <DetailField label="E-Posta" value={<span style={{ color: 'var(--accent-blue)' }}>{user.email}</span>} />
          </Card>
          <Card>
            <DetailField label="Roller" value={
              <div style={{ display: 'flex', gap: '0.25rem', marginTop: '0.2rem' }}>
                {user.roles.map(r => <Badge key={r} variant="cyan">{r}</Badge>)}
              </div>
            } />
          </Card>
          <Card>
            <DetailField label="Yetkili İzin Sayısı" value={
              <span style={{ color: 'var(--status-success)', fontWeight: 'var(--weight-bold)' }}>
                {user.roles.includes('SystemAdmin')
                  ? (catalogPermissionCount !== null ? `Tam Yetki (${user.permissions.length}/${catalogPermissionCount})` : 'Tam Yetki')
                  : (catalogPermissionCount !== null ? `${user.permissions.length} / ${catalogPermissionCount} İzin` : `${user.permissions.length} İzin`)}
              </span>
            } />
          </Card>
        </div>
      </Section>

      <Section title="🔒 Parola Değiştirme">
        {msg && (
          <div style={{ marginBottom: 'var(--space-4)' }}>
            <Badge variant={msg.error ? 'rose' : 'emerald'} style={{ width: '100%', padding: '0.75rem', display: 'block' }}>
              {msg.text}
            </Badge>
          </div>
        )}

        <form onSubmit={handleChangePassword}>
          <FormField label="Mevcut Parola" required>
            <Input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              placeholder="Mevcut parolanız"
            />
          </FormField>

          <FormField label="Yeni Parola" required helpText="En az 8 karakter, büyük/küçük harf, rakam ve sembol" style={{ marginBottom: 'var(--space-6)' }}>
            <Input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              placeholder="Yeni parolanız"
            />
          </FormField>

          <Button type="submit" variant="primary" style={{ width: '100%', justifyContent: 'center' }}>
            Parolayı Güvenli Şekilde Güncelle
          </Button>
        </form>
      </Section>
    </div>
  );
};
