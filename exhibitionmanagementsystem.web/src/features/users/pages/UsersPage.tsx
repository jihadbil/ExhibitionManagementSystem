import { useCallback, useEffect, useState } from 'react';
import { Users } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface ManagedUser {
  userId: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  isActive: boolean;
  emailConfirmed: boolean;
  lastLogin?: string;
  roles: string[];
}

export const UsersPage = () => {
  const { showToast } = useToast();
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    await Promise.resolve();
    setIsLoading(true);
    try {
      const response = await api.get<{ items: ManagedUser[] }>('/api/users?page=1&pageSize=100');
      setUsers(response.data.items ?? []);
      setError('');
    } catch {
      setError('تعذر تحميل المستخدمين من الخادم.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    // تحميل القائمة الخارجية هو الغرض المقصود من هذا الأثر.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const toggleStatus = async (user: ManagedUser) => {
    try {
      await api.patch(`/api/users/${encodeURIComponent(user.userId)}/status`, !user.isActive, { headers: { 'Content-Type': 'application/json' } });
      showToast('تم تحديث حالة المستخدم.', 'success');
      await load();
    } catch {
      showToast('تعذر تحديث حالة المستخدم.', 'danger');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div><h1 style={{ display: 'flex', alignItems: 'center', gap: '10px' }}><Users color="var(--primary)" />المستخدمون والصلاحيات</h1><p style={{ color: 'var(--text-secondary)' }}>الحسابات والأدوار الحالية للمستأجر.</p></div>
      {error && <div role="alert" style={{ color: 'var(--danger-text)' }}>{error}</div>}
      <div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>الاسم</th><th>البريد</th><th>الأدوار</th><th>آخر دخول</th><th>الحالة</th><th>إجراء</th></tr></thead><tbody>
        {users.map(user => <tr key={user.userId}><td>{user.fullName}</td><td>{user.email}</td><td>{user.roles.join('، ') || '—'}</td><td>{user.lastLogin ? new Date(user.lastLogin).toLocaleString('ar-LY') : '—'}</td><td>{user.isActive ? 'نشط' : 'موقوف'}</td><td><button className="btn-secondary" onClick={() => void toggleStatus(user)}>{user.isActive ? 'إيقاف' : 'تفعيل'}</button></td></tr>)}
        {!isLoading && users.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center' }}>لا يوجد مستخدمون.</td></tr>}
        {isLoading && <tr><td colSpan={6} style={{ textAlign: 'center' }}>جاري التحميل…</td></tr>}
      </tbody></table></div></div>
    </div>
  );
};
