import React, { useState } from 'react';
import { FileText, Search, Activity, Clock, ShieldCheck, Database } from 'lucide-react';

interface AuditLogItem {
  id: number;
  tableName: string;
  action: 'Insert' | 'Update' | 'Delete' | 'Login';
  performedBy: string;
  timestamp: string;
  details: string;
}

const mockAuditLogs: AuditLogItem[] = [
  { id: 1, tableName: 'Booths', action: 'Update', performedBy: 'jihad@expomanager.com', timestamp: '2026-09-24 10:45:12', details: 'تحديث أبعاد وحالة الجناح VIP-01 إلى محجوز' },
  { id: 2, tableName: 'Reservations', action: 'Insert', performedBy: 'ahmed.f@expomanager.com', timestamp: '2026-09-24 10:32:05', details: 'إنشاء طلب حجز جديد برقم RES-2026-004' },
  { id: 3, tableName: 'Auth', action: 'Login', performedBy: 'sara.m@expomanager.com', timestamp: '2026-09-24 09:15:40', details: 'تسجيل دخول ناجح من عنوان IP 192.168.1.104' },
  { id: 4, tableName: 'Exhibitions', action: 'Update', performedBy: 'jihad@expomanager.com', timestamp: '2026-09-23 16:20:18', details: 'تعديل تواريخ معرض طرابلس للتقنية 2026' },
  { id: 5, tableName: 'Invoices', action: 'Insert', performedBy: 'sara.m@expomanager.com', timestamp: '2026-09-23 14:10:22', details: 'إصدار فاتورة حجز جناح INV-2026-0824' },
];

export const AdminAuditPage: React.FC = () => {
  const [logs, setLogs] = useState<AuditLogItem[]>(mockAuditLogs);
  const [searchTable, setSearchTable] = useState('');

  const filtered = logs.filter(l =>
    l.tableName.toLowerCase().includes(searchTable.toLowerCase()) ||
    l.details.toLowerCase().includes(searchTable.toLowerCase()) ||
    l.performedBy.toLowerCase().includes(searchTable.toLowerCase())
  );

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <FileText size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              سجلات الرقابة والعمليات (Audit Logs)
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            تتبع التغييرات وحركات المستخدمين وسجلات قواعد البيانات لضمان النزاهة والامتثال
          </p>
        </div>
      </div>

      <div className="card" style={{ padding: '16px 20px', display: 'flex', alignItems: 'center', gap: '16px' }}>
        <div style={{ position: 'relative', width: '380px', maxWidth: '100%' }}>
          <Search size={18} style={{ position: 'absolute', right: '14px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            type="text"
            value={searchTable}
            onChange={(e) => setSearchTable(e.target.value)}
            placeholder="بحث بالجدول، المستخدم، أو تفاصيل العملية..."
            className="input-field"
            style={{ paddingRight: '42px', height: '42px' }}
          />
        </div>
      </div>

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: '15%' }}>الجدول / الكيان</th>
                <th style={{ width: '12%' }}>نوع الإجراء</th>
                <th style={{ width: '22%' }}>المستخدم المنفذ</th>
                <th style={{ width: '18%' }}>الوقت والتاريخ</th>
                <th style={{ width: '33%' }}>تفاصيل التغيير</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(log => (
                <tr key={log.id}>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontWeight: 700, color: 'var(--text-primary)' }}>
                      <Database size={15} style={{ color: 'var(--primary)' }} />
                      <span>{log.tableName}</span>
                    </div>
                  </td>
                  <td>
                    <span
                      className={`badge ${
                        log.action === 'Insert'
                          ? 'badge-success'
                          : log.action === 'Update'
                          ? 'badge-info'
                          : log.action === 'Delete'
                          ? 'badge-danger'
                          : 'badge-featured'
                      }`}
                    >
                      {log.action}
                    </span>
                  </td>
                  <td>
                    <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>{log.performedBy}</span>
                  </td>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '12px', color: 'var(--text-muted)' }}>
                      <Clock size={13} />
                      <span>{log.timestamp}</span>
                    </div>
                  </td>
                  <td>
                    <span style={{ fontSize: '13px', color: 'var(--text-primary)' }}>{log.details}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
