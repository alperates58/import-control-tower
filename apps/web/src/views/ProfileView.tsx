import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';

export const ProfileView: React.FC = () => {
  const { user, changePassword } = useAuth();
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
      {user.mustChangePassword && (
        <div className="badge-rose" style={{ width: '100%', padding: '1rem 1.25rem', borderRadius: '10px', marginBottom: '1.5rem', display: 'block', fontSize: '0.9rem', lineHeight: 1.5 }}>
          <strong>🚨 Zorunlu Parola Değişimi:</strong> Hesabınıza geçici parola veya ilk seed parolası ile giriş yapılmıştır. Lütfen güvenliğiniz için aşağıdan parolanızı derhal güncelleyin.
        </div>
      )}

      <div className="panel">
        <div className="panel-header">
          <div className="panel-title">
            <span>👤 Kullanıcı Profili ve Oturum Bilgileri</span>
          </div>
          <span className="badge badge-emerald">Aktif Oturum</span>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '1rem' }}>
          <div style={{ background: 'rgba(15, 23, 42, 0.5)', padding: '1rem', borderRadius: '10px', border: '1px solid var(--border-color)' }}>
            <div style={{ fontSize: '0.78rem', color: '#94a3b8' }}>Ad Soyad</div>
            <div style={{ fontSize: '1.05rem', fontWeight: 700, color: '#f8fafc', marginTop: '0.2rem' }}>{user.fullName}</div>
          </div>

          <div style={{ background: 'rgba(15, 23, 42, 0.5)', padding: '1rem', borderRadius: '10px', border: '1px solid var(--border-color)' }}>
            <div style={{ fontSize: '0.78rem', color: '#94a3b8' }}>E-Posta</div>
            <div style={{ fontSize: '0.95rem', fontWeight: 600, color: '#38bdf8', marginTop: '0.2rem' }}>{user.email}</div>
          </div>

          <div style={{ background: 'rgba(15, 23, 42, 0.5)', padding: '1rem', borderRadius: '10px', border: '1px solid var(--border-color)' }}>
            <div style={{ fontSize: '0.78rem', color: '#94a3b8' }}>Roller</div>
            <div style={{ display: 'flex', gap: '0.35rem', marginTop: '0.3rem' }}>
              {user.roles.map(r => <span key={r} className="badge badge-cyan">{r}</span>)}
            </div>
          </div>

          <div style={{ background: 'rgba(15, 23, 42, 0.5)', padding: '1rem', borderRadius: '10px', border: '1px solid var(--border-color)' }}>
            <div style={{ fontSize: '0.78rem', color: '#94a3b8' }}>Yetkili İzin Sayısı</div>
            <div style={{ fontSize: '1.05rem', fontWeight: 700, color: '#10b981', marginTop: '0.2rem' }}>
              {user.roles.includes('SystemAdmin') ? 'Tam Yetki (32/32)' : `${user.permissions.length} İzin`}
            </div>
          </div>
        </div>
      </div>

      <div className="panel">
        <div className="panel-header">
          <div className="panel-title">
            <span>🔒 Parola Değiştirme</span>
          </div>
        </div>

        {msg && (
          <div className={msg.error ? 'badge-rose' : 'badge-emerald'} style={{ width: '100%', padding: '0.85rem 1rem', borderRadius: '8px', marginBottom: '1.25rem', display: 'block' }}>
            {msg.text}
          </div>
        )}

        <form onSubmit={handleChangePassword}>
          <div className="form-group">
            <label className="form-label">Mevcut Parola</label>
            <input
              type="password"
              className="form-input"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              placeholder="Mevcut parolanız"
            />
          </div>

          <div className="form-group" style={{ marginBottom: '1.75rem' }}>
            <label className="form-label">Yeni Parola</label>
            <input
              type="password"
              className="form-input"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              placeholder="En az 8 karakter, büyük/küçük harf, rakam ve sembol"
            />
          </div>

          <button type="submit" className="btn-primary" style={{ width: '100%', justifyContent: 'center' }}>
            Parolayı Güvenli Şekilde Güncelle
          </button>
        </form>
      </div>
    </div>
  );
};
