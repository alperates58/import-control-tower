import React, { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { PageHeader } from '../components/ui/PageHeader';
import { DataTable, Column } from '../components/ui/DataTable';
import { Badge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Modal } from '../components/ui/Modal';
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

  const columns: Column<AuditLog>[] = [
    {
      key: 'timestampUtc',
      header: 'Tarih & Zaman (TR)',
      render: (log) => (
        <span style={{ whiteSpace: 'nowrap', fontSize: 'var(--font-xs)', color: 'var(--text-muted)' }}>
          {new Date(log.timestampUtc).toLocaleString('tr-TR')}
        </span>
      )
    },
    {
      key: 'actorUsername',
      header: 'Aktör',
      render: (log) => (
        <div>
          <div style={{ fontWeight: 'var(--weight-semibold)', color: 'var(--text-main)' }}>
            {log.actorUsername || log.actorType}
          </div>
          <div style={{ fontSize: 'var(--font-xs)', color: 'var(--text-dim)' }}>{log.actorType}</div>
        </div>
      )
    },
    {
      key: 'action',
      header: 'Aksiyon',
      render: (log) => (
        <Badge variant="purple" style={{ fontFamily: 'var(--font-mono)' }}>
          {log.action}
        </Badge>
      )
    },
    {
      key: 'entityType',
      header: 'Hedef Varlık',
      render: (log) => (
        <div>
          <div>{log.entityType}</div>
          <div className="font-mono" style={{ fontSize: 'var(--font-xs)', color: 'var(--text-dim)' }}>
            {log.entityId}
          </div>
        </div>
      )
    },
    {
      key: 'ipAddress',
      header: 'IP Adresi',
      render: (log) => (
        <span className="font-mono" style={{ fontSize: 'var(--font-xs)', color: 'var(--accent-blue)' }}>
          {log.ipAddress}
        </span>
      )
    },
    {
      key: 'actions',
      header: 'Detay',
      align: 'right',
      render: (log) => (
        <Button variant="secondary" size="sm" onClick={() => setSelectedLog(log)}>
          JSON Oku
        </Button>
      )
    }
  ];

  return (
    <div>
      <PageHeader
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <IconAudit />
            <span>Güvenlik & Uyum Kayıtları (Audit Logs)</span>
          </div>
        }
        actions={
          <Badge variant="cyan">
            {logs.length} Kayıt Listelendi
          </Badge>
        }
      />

      <DataTable
        columns={columns}
        data={logs}
        keyExtractor={(log) => log.id}
        isLoading={loading}
        emptyMessage="Henüz audit kaydı bulunmuyor."
      />

      {/* JSON Viewer Modal */}
      <Modal
        isOpen={!!selectedLog}
        onClose={() => setSelectedLog(null)}
        title={selectedLog ? `Audit Log Detayı (${selectedLog.action})` : ''}
        footer={
          <Button variant="primary" onClick={() => setSelectedLog(null)}>
            Kapat
          </Button>
        }
      >
        {selectedLog && (
          <pre style={{
            background: 'var(--bg-base)',
            padding: 'var(--space-4)',
            borderRadius: 'var(--radius-md)',
            border: '1px solid var(--border-color)',
            color: 'var(--accent-blue)',
            fontFamily: 'var(--font-mono)',
            fontSize: 'var(--font-xs)',
            overflowX: 'auto',
            whiteSpace: 'pre-wrap'
          }}>
            {JSON.stringify(JSON.parse(selectedLog.metadataJson || '{}'), null, 2)}
          </pre>
        )}
      </Modal>
    </div>
  );
};
