import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { Button } from '../components/ui/Button';
import { Input, FormField } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { IconLogout } from '../components/Icons';

export const ForceChangePasswordView: React.FC = () => {
  const { user, changePassword, login, logout } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!user) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);

    if (newPassword !== confirmPassword) {
      setErrorMsg('Yeni parolalar birbiriyle eşleşmiyor.');
      return;
    }

    if (newPassword.length < 8) {
      setErrorMsg('Yeni parola en az 8 karakter olmalıdır.');
      return;
    }

    setIsSubmitting(true);

    try {
      await changePassword(currentPassword, newPassword);
      await login(user.email, newPassword);
    } catch (err: any) {
      setErrorMsg(err.message || 'Parola değiştirilemedi. Lütfen bilgilerinizi kontrol edip tekrar deneyin.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div style={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      minHeight: '100vh',
      backgroundColor: 'var(--bg-base)',
      padding: 'var(--space-4)'
    }}>
      <div style={{
        width: '100%',
        maxWidth: '460px',
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius-xl)',
        boxShadow: 'var(--shadow-modal)',
        overflow: 'hidden'
      }}>
        <div style={{
          padding: 'var(--space-6) var(--space-6) var(--space-4) var(--space-6)',
          borderBottom: '1px solid var(--border-subtle)',
          textAlign: 'center'
        }}>
          <div style={{
            width: '48px',
            height: '48px',
            borderRadius: 'var(--radius-lg)',
            background: 'var(--status-danger-bg)',
            color: 'var(--status-danger)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto var(--space-3) auto',
            fontSize: '1.4rem'
          }}>
            🔒
          </div>
          <h2 style={{ fontSize: 'var(--font-lg)', fontWeight: 'var(--weight-bold)', color: 'var(--text-main)', marginBottom: 'var(--space-2)' }}>
            Zorunlu Parola Değişimi
          </h2>
          <p style={{ fontSize: 'var(--font-sm)', color: 'var(--text-muted)', lineHeight: 'var(--lh-normal)', margin: 0 }}>
            Sayın <strong>{user.fullName}</strong>, hesabınıza varsayılan ilk seed parolası veya geçici parola ile giriş yapılmıştır. Devam etmek için lütfen yeni parolanızı belirleyin.
          </p>
        </div>

        <form onSubmit={handleSubmit} style={{ padding: 'var(--space-6)' }}>
          {errorMsg && (
            <div style={{ marginBottom: 'var(--space-4)' }}>
              <Badge variant="rose" style={{ width: '100%', padding: '0.75rem', display: 'block' }}>
                {errorMsg}
              </Badge>
            </div>
          )}

          <FormField label="Mevcut Parola" required>
            <Input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              disabled={isSubmitting}
              placeholder="Mevcut parolanız"
            />
          </FormField>

          <FormField label="Yeni Parola" required>
            <Input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              disabled={isSubmitting}
              placeholder="En az 8 karakter, harf, rakam ve sembol"
            />
          </FormField>

          <FormField label="Yeni Parola (Tekrar)" required style={{ marginBottom: 'var(--space-6)' }}>
            <Input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              disabled={isSubmitting}
              placeholder="Yeni parolanızı tekrar girin"
            />
          </FormField>

          <Button
            type="submit"
            variant="primary"
            isLoading={isSubmitting}
            style={{ width: '100%', justifyContent: 'center', marginBottom: 'var(--space-3)' }}
          >
            {isSubmitting ? 'Parola Güncelleniyor...' : 'Parolayı Güncelle ve Devam Et'}
          </Button>

          <Button
            type="button"
            variant="secondary"
            onClick={logout}
            disabled={isSubmitting}
            style={{ width: '100%', justifyContent: 'center' }}
            icon={<IconLogout />}
          >
            Güvenli Çıkış Yap
          </Button>
        </form>
      </div>
    </div>
  );
};
