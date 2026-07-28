import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { IconAudit } from '../components/Icons';

interface AuditLog {
  id: string;
  actorUsername: string | null;
  actorType: string;
  action: string;
  entityType: string;
  entityId: string;
  timestampUtc: string;
  ipAddress: string;
  metadataJson: string;
}

export const AuditLogsView: React.FC = () => {
  const { authenticatedFetch } = useAuth();
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null);

  useEffect(() => {
    const fetchLogs = async () => {
      try {
        const res = await authenticatedFetch('/api/v1/admin/audit-logs');
        if (res.ok) {
          setLogs(await res.json());
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchLogs();
  }, []);

  return (
    <div>
      <div className="panel">
        <div className="panel-header">
          <div className="panel-title">
            <IconAudit />
            <span>Güvenlik & Uyum Kayıtları (Audit Logs)</span>
          </div>
          <div className="badge badge-cyan">
            {logs.length} Kayıt Listelendi
          </div>
        </div>

        {loading ? (
          <div style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>Audit kayıtları yükleniyor...</div>
        ) : (
          <div className="data-table-wrapper">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Tarih & Zaman (TR)</th>
                  <th>Aktör</th>
                  <th>Aksiyon</th>
                  <th>Hedef Varlık</th>
                  <th>IP Adresi</th>
                  <th style={{ textAlign: 'right' }}>Detay</th>
                </tr>
              </thead>
              <tbody>
                {logs.map((log) => (
                  <tr key={log.id}>
                    <td style={{ whiteSpace: 'nowrap', fontSize: '0.8rem', color: '#94a3b8' }}>
                      {new Date(log.timestampUtc).toLocaleString('tr-TR')}
                    </td>
                    <td>
                      <div style={{ fontWeight: 600 }}>{log.actorUsername || log.actorType}</div>
                      <div style={{ fontSize: '0.72rem', color: '#64748b' }}>{log.actorType}</div>
                    </td>
                    <td>
                      <span className="badge badge-purple" style={{ fontFamily: 'monospace' }}>
                        {log.action}
                      </span>
                    </td>
                    <td>
                      <div>{log.entityType}</div>
                      <div style={{ fontSize: '0.72rem', color: '#64748b', fontFamily: 'monospace' }}>{log.entityId}</div>
                    </td>
                    <td style={{ fontFamily: 'monospace', fontSize: '0.8rem', color: '#38bdf8' }}>
                      {log.ipAddress}
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <button
                        className="btn-secondary btn-sm"
                        onClick={() => setSelectedLog(log)}
                      >
                        JSON Oku
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* JSON Viewer Modal */}
      {selectedLog && (
        <div className="modal-overlay">
          <div className="modal-container">
            <div className="modal-header">
              <h3 style={{ fontSize: '1.1rem', fontWeight: 700, color: '#f8fafc' }}>
                Audit Log Detayı ({selectedLog.action})
              </h3>
              <button className="btn-secondary btn-sm" onClick={() => setSelectedLog(null)}>✕</button>
            </div>
            <div className="modal-body">
              <pre style={{
                background: '#090d16',
                padding: '1.25rem',
                borderRadius: '10px',
                border: '1px solid var(--border-color)',
                color: '#38bdf8',
                fontFamily: 'monospace',
                fontSize: '0.82rem',
                overflowX: 'auto',
                whiteSpace: 'pre-wrap'
              }}>
                {JSON.stringify(JSON.parse(selectedLog.metadataJson || '{}'), null, 2)}
              </pre>
            </div>
            <div className="modal-footer">
              <button className="btn-primary" onClick={() => setSelectedLog(null)}>
                Kapat
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
