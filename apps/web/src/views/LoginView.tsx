import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { Button } from '../components/ui/Button';
import { Input, FormField } from '../components/ui/Input';
import { Badge } from '../components/ui/Badge';
import { ThemeToggle } from '../components/ui/ThemeToggle';

export const LoginView: React.FC<{ onSuccess?: () => void }> = ({ onSuccess }) => {
  const { login } = useAuth();
  const [email, setEmail] = useState('admin@controltower.local');
  const [password, setPassword] = useState('AdminSecurePassword123!');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
      if (onSuccess) onSuccess();
    } catch (err: any) {
      setError(err.message || 'Giriş başarısız.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: '100vh',
      width: '100vw',
      backgroundColor: 'var(--bg-base)',
      padding: 'var(--space-4)',
      position: 'relative'
    }}>
      <div style={{ position: 'absolute', top: '1.5rem', right: '1.5rem', zIndex: 10 }}>
        <ThemeToggle />
      </div>
      <div style={{
        width: '100%',
        maxWidth: '420px',
        backgroundColor: 'var(--bg-surface)',
        border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius-xl)',
        padding: 'var(--space-8)',
        boxShadow: 'var(--shadow-modal)'
      }}>
        <div style={{ textAlign: 'center', marginBottom: 'var(--space-6)' }}>
          <div style={{
            width: '48px',
            height: '48px',
            margin: '0 auto var(--space-3)',
            background: 'linear-gradient(135deg, var(--primary), var(--accent-cyan))',
            borderRadius: 'var(--radius-lg)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#fff',
            boxShadow: 'var(--shadow-sm)'
          }}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polygon points="12 2 2 7 12 12 22 7 12 2" />
              <polyline points="2 17 12 22 22 17" />
              <polyline points="2 12 12 17 22 12" />
            </svg>
          </div>
          <h1 style={{ fontSize: 'var(--font-xl)', fontWeight: 'var(--weight-bold)', color: 'var(--text-main)', letterSpacing: '-0.01em' }}>
            Import Control Tower
          </h1>
          <div style={{ fontSize: 'var(--font-xs)', color: 'var(--accent-blue)', fontWeight: 'var(--weight-semibold)', letterSpacing: '0.06em', textTransform: 'uppercase', marginTop: 'var(--space-1)' }}>
            Kurumsal Giriş Portalı (Faz 01)
          </div>
        </div>

        {error && (
          <div style={{ marginBottom: 'var(--space-4)', textAlign: 'center' }}>
            <Badge variant="rose" style={{ width: '100%', padding: '0.5rem', justifyContent: 'center' }}>
              {error}
            </Badge>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <FormField label="E-Posta / Kullanıcı Adı" required>
            <Input
              type="text"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              placeholder="admin@controltower.local"
            />
          </FormField>

          <FormField label="Parola" required style={{ marginBottom: 'var(--space-6)' }}>
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="••••••••••••"
            />
          </FormField>

          <Button
            type="submit"
            variant="primary"
            isLoading={loading}
            style={{ width: '100%', justifyContent: 'center' }}
          >
            {loading ? 'Giriş Yapılıyor...' : 'Sisteme Giriş Yap'}
          </Button>
        </form>

        <div style={{ marginTop: 'var(--space-6)', paddingTop: 'var(--space-4)', borderTop: '1px solid var(--border-subtle)', textAlign: 'center', fontSize: 'var(--font-xs)', color: 'var(--text-dim)' }}>
          Güvenli Same-Origin Proxy Portalı
        </div>
      </div>
    </div>
  );
};
