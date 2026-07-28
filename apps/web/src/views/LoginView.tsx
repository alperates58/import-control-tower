import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';

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
      background: 'radial-gradient(circle at 50% 30%, #1e293b 0%, #090d16 80%)',
      padding: '1.5rem'
    }}>
      <div style={{
        width: '100%',
        maxWidth: '440px',
        background: 'rgba(15, 23, 42, 0.8)',
        backdropFilter: 'blur(16px)',
        border: '1px solid rgba(56, 189, 248, 0.25)',
        borderRadius: '20px',
        padding: '2.5rem',
        boxShadow: '0 20px 40px -15px rgba(0,0,0,0.7), 0 0 30px rgba(56, 189, 248, 0.1)'
      }}>
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{
            width: '54px',
            height: '54px',
            margin: '0 auto 1rem',
            background: 'linear-gradient(135deg, #06b6d4, #0284c7)',
            borderRadius: '14px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#fff',
            boxShadow: '0 6px 20px rgba(6, 182, 212, 0.4)'
          }}>
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
              <polygon points="12 2 2 7 12 12 22 7 12 2" />
              <polyline points="2 17 12 22 22 17" />
              <polyline points="2 12 12 17 22 12" />
            </svg>
          </div>
          <h1 style={{ fontSize: '1.4rem', fontWeight: 800, color: '#f8fafc', letterSpacing: '-0.02em' }}>
            Import Control Tower
          </h1>
          <div style={{ fontSize: '0.78rem', color: '#38bdf8', fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', marginTop: '0.25rem' }}>
            Kurumsal Giriş Portalı (Faz 01)
          </div>
        </div>
        
        {error && (
          <div className="badge-rose" style={{ width: '100%', padding: '0.75rem 1rem', borderRadius: '8px', marginBottom: '1.25rem', fontSize: '0.85rem', display: 'block', textAlign: 'center' }}>
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label className="form-label">E-Posta / Kullanıcı Adı</label>
            <input
              type="text"
              className="form-input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              placeholder="admin@controltower.local"
            />
          </div>

          <div className="form-group" style={{ marginBottom: '1.75rem' }}>
            <label className="form-label">Parola</label>
            <input
              type="password"
              className="form-input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="••••••••••••"
            />
          </div>

          <button
            type="submit"
            className="btn-primary"
            disabled={loading}
            style={{ width: '100%', justifyContent: 'center', padding: '0.85rem', fontSize: '0.95rem' }}
          >
            {loading ? 'Giriş Yapılıyor...' : 'Sisteme Giriş Yap'}
          </button>
        </form>

        <div style={{ marginTop: '2rem', paddingTop: '1.25rem', borderTop: '1px solid rgba(51, 65, 85, 0.4)', textAlign: 'center', fontSize: '0.78rem', color: '#64748b' }}>
          Güvenli Same-Origin Proxy & PostgreSQL 18 Korumalı
        </div>
      </div>
    </div>
  );
};
