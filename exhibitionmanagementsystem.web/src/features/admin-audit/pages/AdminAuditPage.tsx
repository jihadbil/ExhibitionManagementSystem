import { useEffect, useMemo, useState } from 'react';
import { FileText, Search } from 'lucide-react';
import { api } from '../../../services/api';

interface AuditLog {
  logID: number;
  userName: string;
  tableName: string;
  recordID: string;
  action: string;
  actionAt: string;
  ipAddress: string;
}

export const AdminAuditPage = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [query, setQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        const response = await api.get<{ items: AuditLog[] }>('/api/admin/audit-logs?page=1&pageSize=100');
        setLogs(response.data.items ?? []);
      } catch {
        setError('تعذر تحميل سجل الرقابة من الخادم.');
      } finally {
        setIsLoading(false);
      }
    };
    void load();
  }, []);

  const filtered = useMemo(() => {
    const value = query.trim().toLowerCase();
    return value ? logs.filter(log => [log.tableName, log.action, log.userName, log.recordID].some(field => field.toLowerCase().includes(value))) : logs;
  }, [logs, query]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div><h1 style={{ display: 'flex', gap: '10px', alignItems: 'center' }}><FileText color="var(--primary)" />سجل الرقابة</h1><p style={{ color: 'var(--text-secondary)' }}>سجل غير قابل للتعديل، محمّل من الخادم.</p></div>
      <div className="card" style={{ position: 'relative' }}><Search size={18} style={{ position: 'absolute', right: '28px', top: '50%', transform: 'translateY(-50%)' }} /><input className="input-field" value={query} onChange={event => setQuery(event.target.value)} placeholder="بحث في السجل" style={{ paddingRight: '42px' }} /></div>
      {error && <div role="alert" style={{ color: 'var(--danger-text)' }}>{error}</div>}
      <div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>الوقت</th><th>المستخدم</th><th>الإجراء</th><th>الكيان</th><th>المعرف</th><th>عنوان IP</th></tr></thead><tbody>
        {filtered.map(log => <tr key={log.logID}><td>{new Date(log.actionAt).toLocaleString('ar-LY')}</td><td>{log.userName || 'النظام'}</td><td>{log.action}</td><td>{log.tableName}</td><td>{log.recordID}</td><td dir="ltr">{log.ipAddress || '—'}</td></tr>)}
        {!isLoading && filtered.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center' }}>لا توجد سجلات.</td></tr>}
        {isLoading && <tr><td colSpan={6} style={{ textAlign: 'center' }}>جاري التحميل…</td></tr>}
      </tbody></table></div></div>
    </div>
  );
};
