import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Lock,
  Plus,
  Search,
  Trash2,
  UserCheck,
  UserX,
  Users,
  X
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getAllItems } from '../../../services/pagination';

interface ManagedUser {
  userId: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  lastLogin?: string;
  roles: string[];
}

interface RoleItem {
  id?: string;
  name: string;
  description?: string;
}

interface CreateUserFormData {
  fullName: string;
  email: string;
  password: string;
  role: string;
  isActive: boolean;
}

const emptyUserForm: CreateUserFormData = {
  fullName: '',
  email: '',
  password: '',
  role: 'EventManager',
  isActive: true,
};

const roleTranslations: Record<string, string> = {
  SuperAdmin: 'مدير عام المنظومة',
  Admin: 'مدير النظام',
  TenantAdmin: 'مسؤول المنشأة',
  EventManager: 'مدير المعارض والفعاليات',
  Finance: 'المسؤول المالي',
  GateOperator: 'مشغل البوابات والاستقبال',
};

export const UsersPage = () => {
  const { user: currentUser } = useAuth();
  const { showToast } = useToast();
  const canManage = currentUser?.roles.some(role => role === 'Admin' || role === 'TenantAdmin') ?? false;

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [availableRoles, setAvailableRoles] = useState<RoleItem[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form, setForm] = useState<CreateUserFormData>(emptyUserForm);
  const [isSaving, setIsSaving] = useState(false);

  // Load Users & Roles
  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setError('');
    try {
      const [usersRes, rolesRes] = await Promise.allSettled([
        getAllItems<ManagedUser>('/api/users'),
        api.get<RoleItem[]>('/api/roles'),
      ]);

      if (usersRes.status === 'fulfilled') {
        setUsers(usersRes.value);
      } else {
        setError('تعذر تحميل قائمة المستخدمين من الخادم.');
      }

      if (rolesRes.status === 'fulfilled') {
        const roles = rolesRes.value.data ?? [];
        setAvailableRoles(roles.length > 0 ? roles : [
          { name: 'TenantAdmin', description: 'مسؤول المنشأة' },
          { name: 'EventManager', description: 'مدير المعارض والفعاليات' },
          { name: 'Finance', description: 'المسؤول المالي' },
          { name: 'GateOperator', description: 'مشغل البوابات' }
        ]);
      }
    } catch {
      setError('تعذر تحميل البيانات من الخادم.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadUsers();
  }, [loadUsers]);

  // Filtered Users
  const filteredUsers = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return users;
    return users.filter(u =>
      u.fullName.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q) ||
      u.roles.some(r => r.toLowerCase().includes(q))
    );
  }, [users, searchQuery]);

  // Toggle user status
  const toggleStatus = async (user: ManagedUser) => {
    if (user.userId === currentUser?.userId) {
      showToast('لا يمكنك تغيير حالة حسابك الحالي.', 'warning');
      return;
    }

    try {
      await api.patch(`/api/users/${encodeURIComponent(user.userId)}/status`, !user.isActive, {
        headers: { 'Content-Type': 'application/json' },
      });
      showToast(`تم ${!user.isActive ? 'تفعيل' : 'إيقاف'} حساب المستخدم.`, 'success');
      await loadUsers();
    } catch {
      showToast('تعذر تحديث حالة المستخدم في الخادم.', 'danger');
    }
  };

  // Delete user
  const handleDeleteUser = async (user: ManagedUser) => {
    if (user.userId === currentUser?.userId) {
      showToast('لا يمكنك حذف حسابك الحالي.', 'warning');
      return;
    }

    if (!window.confirm(`هل أنت متأكد من حذف حساب «${user.fullName}» نهائياً؟`)) return;

    try {
      await api.delete(`/api/users/${encodeURIComponent(user.userId)}`);
      showToast('تم حذف المستخدم بنجاح.', 'success');
      await loadUsers();
    } catch {
      showToast('تعذر حذف المستخدم؛ قد يكون مرتبطاً بسجلات تدقيق أو عمليات.', 'danger');
    }
  };

  // Submit Create User
  const handleCreateUser = async (e: FormEvent) => {
    e.preventDefault();
    if (!form.fullName.trim() || !form.email.trim() || !form.password) {
      showToast('يرجى ملء جميع الحقول المطلوبة.', 'warning');
      return;
    }
    if (form.password.length < 8) {
      showToast('كلمة المرور يجب ألا تقل عن 8 أحرف وأرقام.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      await api.post('/api/users', {
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        password: form.password,
        roles: [form.role],
        isActive: form.isActive,
      });

      showToast('تم إنشاء حساب المستخدم بنجاح.', 'success');
      setIsModalOpen(false);
      setForm(emptyUserForm);
      await loadUsers();
    } catch {
      showToast('فشل إنشاء الحساب. قد يكون البريد الإلكتروني مسجلاً مسبقاً.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Users size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>المستخدمون والصلاحيات</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>إدارة فرق العمل، تعيين الصلاحيات، ومتابعة نشاط الحسابات.</p>
        </div>
        {canManage && (
          <button className="btn-primary" onClick={() => setIsModalOpen(true)}>
            <Plus size={18} /> مستخدم جديد
          </button>
        )}
      </div>

      {/* Search Bar */}
      <div className="card" style={{ position: 'relative' }}>
        <Search size={18} style={{ position: 'absolute', right: '28px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
        <input
          className="input-field"
          value={searchQuery}
          onChange={e => setSearchQuery(e.target.value)}
          placeholder="ابحث بالاسم أو البريد الإلكتروني أو الدور الوظيفي…"
          style={{ paddingRight: '42px' }}
        />
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Table Card */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>المستخدم</th>
                <th>البريد الإلكتروني</th>
                <th>الأدوار والصلاحيات</th>
                <th>آخر تسجيل دخول</th>
                <th>الحالة</th>
                <th>الإجراءات</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map(user => {
                const isSelf = user.userId === currentUser?.userId;
                return (
                  <tr key={user.userId}>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <div style={{ width: '32px', height: '32px', borderRadius: '50%', background: 'var(--primary-subtle, rgba(99,102,241,0.1))', color: 'var(--primary)', display: 'grid', placeItems: 'center', fontWeight: 700, fontSize: '12px' }}>
                          {user.fullName.slice(0, 1).toUpperCase()}
                        </div>
                        <div>
                          <strong>{user.fullName}</strong>
                          {isSelf && <span style={{ marginRight: '6px', fontSize: '11px', color: 'var(--primary)', fontWeight: 600 }}>(أنت)</span>}
                        </div>
                      </div>
                    </td>
                    <td dir="ltr" style={{ textAlign: 'right' }}>{user.email}</td>
                    <td>
                      <div style={{ display: 'flex', gap: '4px', flexWrap: 'wrap' }}>
                        {user.roles.map(r => (
                          <span key={r} className="badge badge-info" style={{ fontSize: '11px' }}>
                            {roleTranslations[r] ?? r}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td>{user.lastLogin ? new Date(user.lastLogin).toLocaleString('ar-LY') : 'لم يسجل دخول'}</td>
                    <td>
                      <span className={`badge ${user.isActive ? 'badge-success' : 'badge-danger'}`}>
                        {user.isActive ? 'نشط' : 'موقوف'}
                      </span>
                    </td>
                    <td>
                      {canManage && (
                        <div style={{ display: 'flex', gap: '8px' }}>
                          <button
                            className="btn-secondary"
                            style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                            onClick={() => void toggleStatus(user)}
                            disabled={isSelf}
                            title={user.isActive ? 'إيقاف الحساب' : 'تفعيل الحساب'}
                          >
                            {user.isActive ? <UserX size={14} /> : <UserCheck size={14} />}
                            <span style={{ marginRight: '4px' }}>{user.isActive ? 'إيقاف' : 'تفعيل'}</span>
                          </button>
                          <button
                            className="btn-icon"
                            style={{ color: 'var(--danger)' }}
                            onClick={() => void handleDeleteUser(user)}
                            disabled={isSelf}
                            title="حذف المستخدم"
                          >
                            <Trash2 size={15} />
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
              {!isLoading && filteredUsers.length === 0 && (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: '30px' }}>لا يوجد مستخدمون مطابقون.</td></tr>
              )}
              {isLoading && (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: Create User */}
      {isModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleCreateUser} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>إنشاء مستخدم جديد</h2>
              <button type="button" className="btn-icon" onClick={() => setIsModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الاسم الكامل *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: أحمد عبد الله"
                value={form.fullName}
                onChange={e => setForm(prev => ({ ...prev, fullName: e.target.value }))}
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">البريد الإلكتروني (لتسجيل الدخول) *</label>
              <input
                className="input-field"
                type="email"
                required
                placeholder="user@organization.ly"
                value={form.email}
                onChange={e => setForm(prev => ({ ...prev, email: e.target.value }))}
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">كلمة المرور الأولية * (8 أحرف على الأقل)</label>
              <div style={{ position: 'relative' }}>
                <Lock size={16} style={{ position: 'absolute', right: '14px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
                <input
                  className="input-field"
                  type="password"
                  required
                  minLength={8}
                  placeholder="••••••••"
                  style={{ paddingRight: '40px' }}
                  value={form.password}
                  onChange={e => setForm(prev => ({ ...prev, password: e.target.value }))}
                />
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الدور الوظيفي والصلاحية *</label>
              <select
                className="input-field"
                value={form.role}
                onChange={e => setForm(prev => ({ ...prev, role: e.target.value }))}
              >
                {availableRoles.length > 0 ? (
                  availableRoles.map(r => (
                    <option key={r.name} value={r.name}>
                      {roleTranslations[r.name] ?? r.name} {r.description ? `(${r.description})` : ''}
                    </option>
                  ))
                ) : (
                  <>
                    <option value="TenantAdmin">مسؤول المنشأة (TenantAdmin)</option>
                    <option value="EventManager">مدير المعارض والفعاليات (EventManager)</option>
                    <option value="Finance">المسؤول المالي (Finance)</option>
                    <option value="GateOperator">مشغل البوابات (GateOperator)</option>
                  </>
                )}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">حالة الحساب</label>
              <select
                className="input-field"
                value={form.isActive ? 'true' : 'false'}
                onChange={e => setForm(prev => ({ ...prev, isActive: e.target.value === 'true' }))}
              >
                <option value="true">نشط فوراً</option>
                <option value="false">معطل مؤقتاً</option>
              </select>
            </div>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري الإنشاء…' : 'إنشاء المستخدم'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
