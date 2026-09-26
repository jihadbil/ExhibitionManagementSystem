import React, { useState } from 'react';
import { Users, Plus, ShieldCheck, Mail, CheckCircle2, XCircle } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface UserAccount {
  id: string;
  name: string;
  email: string;
  role: string;
  status: 'Active' | 'Inactive';
  lastLogin: string;
}

const mockUsers: UserAccount[] = [
  { id: '1', name: 'المهندس جهاد بلعيد', email: 'jihad@expomanager.com', role: 'SuperAdmin (مدير عام)', status: 'Active', lastLogin: 'اليوم، 10:30 ص' },
  { id: '2', name: 'أحمد مصطفى الفرجاني', email: 'ahmed.f@expomanager.com', role: 'EventOrganizer (منظم معارض)', status: 'Active', lastLogin: 'أمس، 04:15 م' },
  { id: '3', name: 'سارة عبد السلام المنفي', email: 'sara.m@expomanager.com', role: 'FinanceManager (المسؤول المالي)', status: 'Active', lastLogin: '22 سبتمبر 2026' },
  { id: '4', name: 'عمر الطاهر السويحلي', email: 'omar.s@expomanager.com', role: 'KioskOperator (مشغل الكشك)', status: 'Active', lastLogin: 'اليوم، 09:12 ص' },
];

export const UsersPage: React.FC = () => {
  const { showToast } = useToast();
  const [users, setUsers] = useState<UserAccount[]>(mockUsers);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Users size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              إدارة المستخدمين والصلاحيات
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            التحكم في حسابات فريق العمل والأدوار الوظيفية في المنظومة
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج إضافة مستخدم جديد جاهز', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إضافة مستخدم جديد</span>
        </button>
      </div>

      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>الاسم الكامل</th>
                <th>البريد الإلكتروني</th>
                <th>الدور والصلاحية</th>
                <th>آخر تسجيل دخول</th>
                <th>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id}>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      <div style={{ width: '36px', height: '36px', borderRadius: '50%', background: 'var(--primary-gradient)', color: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 700, fontSize: '13px' }}>
                        {u.name[0]}
                      </div>
                      <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{u.name}</span>
                    </div>
                  </td>
                  <td>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>{u.email}</span>
                  </td>
                  <td>
                    <span className="badge badge-featured">{u.role}</span>
                  </td>
                  <td>
                    <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{u.lastLogin}</span>
                  </td>
                  <td>
                    <span className={`badge ${u.status === 'Active' ? 'badge-success' : 'badge-danger'}`}>
                      {u.status === 'Active' ? 'نشط' : 'معطل'}
                    </span>
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
