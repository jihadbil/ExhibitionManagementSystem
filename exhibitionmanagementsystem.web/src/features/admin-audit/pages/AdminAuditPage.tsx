import { useEffect, useMemo, useState } from 'react';
import type { CSSProperties } from 'react';
import type { LucideIcon } from 'lucide-react';
import {
  Search,
  Eye,
  X,
  ShieldCheck,
  User,
  Clock,
  Layers,
  Filter,
  CheckCircle2,
  AlertCircle,
  PlusCircle,
  Edit3,
  Trash2,
  Info
} from 'lucide-react';
import { getAllItems } from '../../../services/pagination';
import { normalizeAuditAction } from '../utils/auditActions';

interface AuditLog {
  logID: number;
  tenantID?: number;
  userId?: string;
  userName: string;
  tableName: string;
  recordID: string;
  action: string;
  oldValues?: string;
  newValues?: string;
  actionAt: string;
  ipAddress?: string;
}

// Friendly Arabic translations for Database Tables / Entities
const entityMap: Record<string, { label: string; category: string; description: string }> = {
  BoothReservations: { label: 'حجز جناح', category: 'reservations', description: 'طلبات وعقود حجز أجنحة المعرض' },
  Reservation: { label: 'حجز جناح', category: 'reservations', description: 'طلبات وعقود حجز أجنحة المعرض' },
  Booths: { label: 'جناح المعرض', category: 'booths', description: 'بيانات الأجنحة والمساحات' },
  Booth: { label: 'جناح المعرض', category: 'booths', description: 'بيانات الأجنحة والمساحات' },
  BoothPriceRules: { label: 'قواعد تسعير الأجنحة', category: 'booths', description: 'قواعد احتساب أسعار المتر المربع' },
  BoothPriceRule: { label: 'قواعد تسعير الأجنحة', category: 'booths', description: 'قواعد احتساب أسعار المتر المربع' },
  Exhibitions: { label: 'معرض', category: 'booths', description: 'بيانات وتنظيم المعارض' },
  Exhibition: { label: 'معرض', category: 'booths', description: 'بيانات وتنظيم المعارض' },
  Exhibitors: { label: 'شركة عارضة', category: 'reservations', description: 'بيانات وملفات الشركات العارضة' },
  Exhibitor: { label: 'شركة عارضة', category: 'reservations', description: 'بيانات وملفات الشركات العارضة' },
  Invoices: { label: 'فاتورة مالية', category: 'finance', description: 'فواتير ومستحقات الحجوزات' },
  Invoice: { label: 'فاتورة مالية', category: 'finance', description: 'فواتير ومستحقات الحجوزات' },
  InvoiceItem: { label: 'بند فاتورة', category: 'finance', description: 'بنود وتفاصيل الفاتورة المالية' },
  Payments: { label: 'سند قبض / دفعة مالية', category: 'finance', description: 'عمليات السداد والتحصيل' },
  Payment: { label: 'سند قبض / دفعة مالية', category: 'finance', description: 'عمليات السداد والتحصيل' },
  Tickets: { label: 'تذكرة دخول', category: 'visitors', description: 'تذاكر الدخول المصدرة للزوار' },
  Ticket: { label: 'تذكرة دخول', category: 'visitors', description: 'تذاكر الدخول المصدرة للزوار' },
  Visitors: { label: 'زائر مسجل', category: 'visitors', description: 'سجلات وبيانات الزوار' },
  Visitor: { label: 'زائر مسجل', category: 'visitors', description: 'سجلات وبيانات الزوار' },
  SupportingServices: { label: 'خدمة مساندة', category: 'finance', description: 'دليل الخدمات الإضافية المساندة' },
  BadgeTemplates: { label: 'قالب شارة الدخول', category: 'visitors', description: 'قوالب وتصاميم بطاقات الحضور' },
  Venues: { label: 'موقع / مركز معارض', category: 'booths', description: 'مراكز ومواقع استضافة الفعاليات' },
  Venue: { label: 'موقع / مركز معارض', category: 'booths', description: 'مراكز ومواقع استضافة الفعاليات' },
  Halls: { label: 'قاعة عرض', category: 'booths', description: 'قاعات وأجنحة مراكز المعارض' },
  Hall: { label: 'قاعة عرض', category: 'booths', description: 'قاعات وأجنحة مراكز المعارض' },
  ApplicationUser: { label: 'حساب مستخدم', category: 'system', description: 'حسابات وصلاحيات مستخدمي المنظومة' },
  Users: { label: 'حساب مستخدم', category: 'system', description: 'حسابات وصلاحيات مستخدمي المنظومة' },
};

// Friendly Arabic translations for Actions
const actionMap: Record<string, { label: string; badgeStyle: CSSProperties; icon: LucideIcon }> = {
  Insert: {
    label: 'إضافة جديدة',
    badgeStyle: { backgroundColor: 'rgba(16, 185, 129, 0.12)', color: '#059669', border: '1px solid rgba(16, 185, 129, 0.3)' },
    icon: PlusCircle
  },
  Create: {
    label: 'إضافة جديدة',
    badgeStyle: { backgroundColor: 'rgba(16, 185, 129, 0.12)', color: '#059669', border: '1px solid rgba(16, 185, 129, 0.3)' },
    icon: PlusCircle
  },
  Update: {
    label: 'تعديل بيانات',
    badgeStyle: { backgroundColor: 'rgba(59, 130, 246, 0.12)', color: '#2563eb', border: '1px solid rgba(59, 130, 246, 0.3)' },
    icon: Edit3
  },
  Modify: {
    label: 'تعديل بيانات',
    badgeStyle: { backgroundColor: 'rgba(59, 130, 246, 0.12)', color: '#2563eb', border: '1px solid rgba(59, 130, 246, 0.3)' },
    icon: Edit3
  },
  Delete: {
    label: 'حذف',
    badgeStyle: { backgroundColor: 'rgba(239, 68, 68, 0.12)', color: '#dc2626', border: '1px solid rgba(239, 68, 68, 0.3)' },
    icon: Trash2
  },
  Approve: {
    label: 'اعتماد وموافقة',
    badgeStyle: { backgroundColor: 'rgba(139, 92, 246, 0.12)', color: '#7c3aed', border: '1px solid rgba(139, 92, 246, 0.3)' },
    icon: CheckCircle2
  },
  Cancel: {
    label: 'إلغاء',
    badgeStyle: { backgroundColor: 'rgba(245, 158, 11, 0.12)', color: '#d97706', border: '1px solid rgba(245, 158, 11, 0.3)' },
    icon: AlertCircle
  }
};

const parseJsonSafely = (raw?: string): Record<string, unknown> | null => {
  if (!raw) return null;
  try {
    const obj: unknown = JSON.parse(raw);
    return typeof obj === 'object' && obj !== null && !Array.isArray(obj)
      ? obj as Record<string, unknown>
      : null;
  } catch {
    return null;
  }
};

export const AdminAuditPage = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [query, setQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [actionFilter, setActionFilter] = useState('all');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Selected Log Details Modal
  const [selectedLog, setSelectedLog] = useState<AuditLog | null>(null);

  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const list = await getAllItems<AuditLog>('/api/admin/audit-logs');
        if (active) setLogs(list);
      } catch {
        if (active) setError('تعذر تحميل سجل التدقيق والرقابة كاملًا من الخادم.');
      } finally {
        if (active) setIsLoading(false);
      }
    };
    void load();
    return () => { active = false; };
  }, []);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    return logs.filter(log => {
      const entity = entityMap[log.tableName] || { label: log.tableName, category: 'other' };
      const action = actionMap[normalizeAuditAction(log.action)] || { label: log.action };

      // Category filter
      if (categoryFilter !== 'all' && entity.category !== categoryFilter) {
        return false;
      }

      // Action filter
      if (actionFilter !== 'all' && normalizeAuditAction(log.action).toLowerCase() !== actionFilter.toLowerCase()) {
        return false;
      }

      // Query filter
      if (!q) return true;
      return (
        entity.label.toLowerCase().includes(q) ||
        action.label.toLowerCase().includes(q) ||
        log.tableName.toLowerCase().includes(q) ||
        log.action.toLowerCase().includes(q) ||
        (log.userName || '').toLowerCase().includes(q) ||
        (log.recordID || '').toString().toLowerCase().includes(q) ||
        (log.ipAddress || '').toLowerCase().includes(q)
      );
    });
  }, [logs, query, categoryFilter, actionFilter]);

  // Statistics counters
  const stats = useMemo(() => {
    const inserts = logs.filter(l => normalizeAuditAction(l.action) === 'Insert').length;
    const updates = logs.filter(l => normalizeAuditAction(l.action) === 'Update').length;
    const deletes = logs.filter(l => normalizeAuditAction(l.action) === 'Delete').length;
    return { total: logs.length, inserts, updates, deletes };
  }, [logs]);

  // Parse Old and New values of the selected log
  const parsedOld = useMemo(() => parseJsonSafely(selectedLog?.oldValues), [selectedLog]);
  const parsedNew = useMemo(() => parseJsonSafely(selectedLog?.newValues), [selectedLog]);

  // All distinct keys between old and new
  const allDiffKeys = useMemo(() => {
    const keys = new Set<string>();
    if (parsedOld) Object.keys(parsedOld).forEach(k => keys.add(k));
    if (parsedNew) Object.keys(parsedNew).forEach(k => keys.add(k));
    return Array.from(keys);
  }, [parsedOld, parsedNew]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Top Banner & Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div style={{ width: '40px', height: '40px', borderRadius: 'var(--radius-md)', background: 'rgba(99, 102, 241, 0.1)', color: 'var(--primary)', display: 'grid', placeItems: 'center' }}>
              <ShieldCheck size={24} />
            </div>
            <div>
              <h1 style={{ fontSize: '22px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                سجل التدقيق والرقابة وتتبع العمليات
              </h1>
              <p style={{ color: 'var(--text-secondary)', fontSize: '13px', margin: '3px 0 0' }}>
                سجل موثق وغير قابل للتعديل يوضح كافة عمليات الإنشاء، التعديل، والحذف مع تفاصيل كل إجراء.
              </p>
            </div>
          </div>
        </div>

        {/* Quick Statistics Pills */}
        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
          <div className="card" style={{ padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '8px', minWidth: '120px' }}>
            <Layers size={18} style={{ color: 'var(--primary)' }} />
            <div>
              <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>إجمالي الحركات</span>
              <strong style={{ fontSize: '16px' }}>{stats.total}</strong>
            </div>
          </div>
          <div className="card" style={{ padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '8px', minWidth: '120px' }}>
            <PlusCircle size={18} style={{ color: '#10b981' }} />
            <div>
              <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>عمليات الإضافة</span>
              <strong style={{ fontSize: '16px', color: '#10b981' }}>{stats.inserts}</strong>
            </div>
          </div>
          <div className="card" style={{ padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '8px', minWidth: '120px' }}>
            <Edit3 size={18} style={{ color: '#3b82f6' }} />
            <div>
              <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>عمليات التعديل</span>
              <strong style={{ fontSize: '16px', color: '#3b82f6' }}>{stats.updates}</strong>
            </div>
          </div>
        </div>
      </div>

      {/* Category Tabs */}
      <div style={{ display: 'flex', gap: '8px', borderBottom: '1px solid var(--border-main)', paddingBottom: '10px', overflowX: 'auto' }}>
        {[
          { id: 'all', label: 'كافة السجلات' },
          { id: 'reservations', label: 'الحجوزات والعارضون' },
          { id: 'finance', label: 'المالية والفواتير' },
          { id: 'booths', label: 'الأجنحة والمعارض' },
          { id: 'visitors', label: 'الزوار والتذاكر' },
          { id: 'system', label: 'النظام والمستخدمون' },
        ].map(tab => (
          <button
            key={tab.id}
            onClick={() => setCategoryFilter(tab.id)}
            className={categoryFilter === tab.id ? 'btn-primary' : 'btn-secondary'}
            style={{ height: '36px', padding: '0 16px', fontSize: '13px', whiteSpace: 'nowrap' }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Filter and Search Bar */}
      <div style={{ display: 'flex', gap: '14px', flexWrap: 'wrap', alignItems: 'center' }}>
        <div className="card" style={{ flex: 1, minWidth: '260px', position: 'relative', padding: 0 }}>
          <Search size={18} style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            className="input-field"
            value={query}
            onChange={e => setQuery(e.target.value)}
            style={{ paddingRight: '44px', border: 0 }}
            placeholder="ابحث باسم المستخدم، الإجراء، اسم الكيان، أو المعرف…"
          />
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Filter size={16} style={{ color: 'var(--text-secondary)' }} />
          <select
            className="input-field"
            value={actionFilter}
            onChange={e => setActionFilter(e.target.value)}
            style={{ minWidth: '150px', height: '42px', fontSize: '13px' }}
          >
            <option value="all">كافة أنواع الإجراءات</option>
            <option value="insert">إضافة جديدة (Insert)</option>
            <option value="update">تعديل (Update)</option>
            <option value="delete">حذف (Delete)</option>
          </select>
        </div>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Main Friendly Audit Table */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>تاريخ وتوقيت الإجراء</th>
                <th>المستخدم المسؤول</th>
                <th>نوع الإجراء</th>
                <th>القسم / الكيان المتأثر</th>
                <th>ملخص التغيير</th>
                <th>عنوان IP</th>
                <th>التفاصيل</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(log => {
                const entity = entityMap[log.tableName] || { label: log.tableName, category: 'other', description: '' };
                const action = actionMap[normalizeAuditAction(log.action)] || {
                  label: log.action,
                  badgeStyle: { backgroundColor: 'var(--bg-main)', color: 'inherit', border: '1px solid var(--border-main)' },
                  icon: Info
                };
                const ActionIcon = action.icon;

                return (
                  <tr key={log.logID}>
                    {/* Time */}
                    <td style={{ whiteSpace: 'nowrap' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px' }}>
                        <Clock size={13} style={{ color: 'var(--text-muted)' }} />
                        <span>{new Date(log.actionAt).toLocaleString('ar-LY', { dateStyle: 'short', timeStyle: 'medium' })}</span>
                      </div>
                    </td>

                    {/* User */}
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                        <User size={14} style={{ color: 'var(--primary)' }} />
                        <strong>{log.userName || 'النظام التلقائي'}</strong>
                      </div>
                    </td>

                    {/* Action Badge */}
                    <td>
                      <span
                        style={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '5px',
                          padding: '3px 10px',
                          borderRadius: '16px',
                          fontSize: '11px',
                          fontWeight: 700,
                          ...action.badgeStyle
                        }}
                      >
                        <ActionIcon size={12} />
                        {action.label}
                      </span>
                    </td>

                    {/* Entity */}
                    <td>
                      <div>
                        <strong style={{ fontSize: '13px', color: 'var(--text-primary)' }}>{entity.label}</strong>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>
                          سجل #{log.recordID}
                        </span>
                      </div>
                    </td>

                    {/* Plain Language Summary */}
                    <td style={{ fontSize: '12px', color: 'var(--text-secondary)', maxWidth: '280px' }}>
                      قام <strong>{log.userName || 'النظام'}</strong> بـ <span style={{ fontWeight: 600 }}>{action.label}</span> في {entity.label} (رقم السجل: {log.recordID})
                    </td>

                    {/* IP Address */}
                    <td dir="ltr" style={{ textAlign: 'right', fontSize: '11px', color: 'var(--text-muted)' }}>
                      {log.ipAddress || '—'}
                    </td>

                    {/* Action Button */}
                    <td>
                      <button
                        className="btn-secondary"
                        onClick={() => setSelectedLog(log)}
                        style={{ height: '30px', padding: '0 10px', fontSize: '12px', display: 'inline-flex', alignItems: 'center', gap: '4px' }}
                      >
                        <Eye size={13} />
                        <span>معاينة</span>
                      </button>
                    </td>
                  </tr>
                );
              })}

              {!isLoading && filtered.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '36px', color: 'var(--text-secondary)' }}>
                    لا توجد سجلات تدقيق مطابقة لمعايير البحث الحالية.
                  </td>
                </tr>
              )}

              {isLoading && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '36px' }}>
                    جاري تحميل سجلات التدقيق…
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* ━━━━━━━━━━━━━━━━━ MODAL: AUDIT RECORD DETAIL VIEWER ━━━━━━━━━━━━━━━━━ */}
      {selectedLog && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15, 23, 42, 0.55)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <div className="card" style={{ width: 'min(700px, 100%)', maxHeight: '90vh', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '18px', padding: '24px' }}>
            {/* Modal Header */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', borderBottom: '1px solid var(--border-main)', paddingBottom: '14px' }}>
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <ShieldCheck size={20} style={{ color: 'var(--primary)' }} />
                  <h2 style={{ fontSize: '18px', fontWeight: 800, margin: 0 }}>تفاصيل العملية وسجل التغييرات</h2>
                </div>
                <p style={{ fontSize: '12px', color: 'var(--text-secondary)', margin: '4px 0 0' }}>
                  سجل حركة رقم #{selectedLog.logID} في قسم «{(entityMap[selectedLog.tableName]?.label) || selectedLog.tableName}»
                </p>
              </div>
              <button className="btn-icon" onClick={() => setSelectedLog(null)}><X size={18} /></button>
            </div>

            {/* Event Summary Box */}
            <div
              style={{
                backgroundColor: 'rgba(99, 102, 241, 0.06)',
                border: '1px solid rgba(99, 102, 241, 0.2)',
                borderRadius: 'var(--radius-lg)',
                padding: '14px 18px',
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))',
                gap: '12px'
              }}
            >
              <div>
                <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>المستخدم المسؤول:</span>
                <strong style={{ fontSize: '13px' }}>{selectedLog.userName || 'النظام'}</strong>
              </div>
              <div>
                <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>نوع العملية:</span>
                <strong style={{ fontSize: '13px' }}>{(actionMap[normalizeAuditAction(selectedLog.action)]?.label) || selectedLog.action}</strong>
              </div>
              <div>
                <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>تاريخ وتوقيت الإجراء:</span>
                <strong style={{ fontSize: '12px' }}>{new Date(selectedLog.actionAt).toLocaleString('ar-LY')}</strong>
              </div>
              <div>
                <span style={{ fontSize: '11px', color: 'var(--text-secondary)', display: 'block' }}>عنوان IP:</span>
                <span dir="ltr" style={{ fontSize: '12px', fontWeight: 600 }}>{selectedLog.ipAddress || '—'}</span>
              </div>
            </div>

            {/* Changes Inspection */}
            <div>
              <h3 style={{ fontSize: '14px', fontWeight: 700, marginBottom: '10px' }}>
                مقارنة القيم والتعديلات (Field-by-Field Changes):
              </h3>

              {allDiffKeys.length > 0 ? (
                <div className="table-container" style={{ maxHeight: '320px', overflowY: 'auto' }}>
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>اسم الحقل</th>
                        <th>القيمة السابقة (قبل التعديل)</th>
                        <th>القيمة الجديدة (بعد التعديل)</th>
                      </tr>
                    </thead>
                    <tbody>
                      {allDiffKeys.map(key => {
                        const oldVal = parsedOld ? parsedOld[key] : undefined;
                        const newVal = parsedNew ? parsedNew[key] : undefined;
                        const isChanged = JSON.stringify(oldVal) !== JSON.stringify(newVal);

                        return (
                          <tr key={key} style={{ backgroundColor: isChanged ? 'rgba(59, 130, 246, 0.04)' : undefined }}>
                            <td>
                              <strong style={{ fontSize: '12px', color: isChanged ? 'var(--primary)' : 'inherit' }}>
                                {key}
                              </strong>
                            </td>
                            <td style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>
                              {oldVal !== undefined ? String(oldVal) : <span style={{ color: 'var(--text-muted)' }}>—</span>}
                            </td>
                            <td style={{ fontSize: '12px', fontWeight: isChanged ? 700 : 400, color: isChanged ? '#059669' : 'inherit' }}>
                              {newVal !== undefined ? String(newVal) : <span style={{ color: 'var(--text-muted)' }}>—</span>}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div style={{ padding: '20px', textAlign: 'center', background: 'var(--bg-main)', borderRadius: 'var(--radius-md)', color: 'var(--text-secondary)', fontSize: '13px' }}>
                  {selectedLog.newValues ? (
                    <pre style={{ textAlign: 'left', direction: 'ltr', fontSize: '11px', maxHeight: '200px', overflowY: 'auto' }}>
                      {selectedLog.newValues}
                    </pre>
                  ) : (
                    'لا توجد تفاصيل حقول مسجلة في هذه الحركة (مثل عمليات القراءة أو تسجيل الدخول).'
                  )}
                </div>
              )}
            </div>

            {/* Modal Actions */}
            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '6px' }}>
              <button className="btn-secondary" onClick={() => setSelectedLog(null)} style={{ padding: '0 24px', height: '38px' }}>
                إغلاق
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
