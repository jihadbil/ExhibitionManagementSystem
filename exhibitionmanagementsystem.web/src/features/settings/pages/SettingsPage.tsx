import React, { useState } from 'react';
import { Settings, Save, Sun } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';
import { useAuth } from '../../../app/providers/AuthContext';

export const SettingsPage: React.FC = () => {
  const { showToast } = useToast();
  const { user } = useAuth();

  const [companyName, setCompanyName] = useState(user?.tenantName || 'الهيئة العامة للمعارض الليبية');
  const [currency, setCurrency] = useState('LYD');
  const [defaultLanguage, setDefaultLanguage] = useState('ar');

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    showToast('هذه الإعدادات غير موصولة بعقد حفظ في الخادم بعد؛ لم تُحفظ أي تغييرات.', 'warning');
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Settings size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              إعدادات النظام العامة
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            تخصيص تفضيلات المنشأة، العملة الافتراضية، وتثبيت النمط الفاتح المعتمد (DESIGN.md)
          </p>
        </div>

        <button
          onClick={handleSave}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Save size={16} />
          <span>حفظ التغييرات</span>
        </button>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(420px, 1fr))', gap: '24px' }}>
        {/* Organization Profile Settings */}
        <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)', borderBottom: '1px solid var(--border-main)', paddingBottom: '12px' }}>
            ملف المنظمة والمستأجر
          </h3>

          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">اسم المؤسسة أو الهيئة المنظمة</label>
            <input
              type="text"
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
              className="input-field"
            />
          </div>

          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">العملة الافتراضية للفواتير والأسعار</label>
            <select
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className="input-field"
            >
              <option value="LYD">دينار ليبي (LYD)</option>
              <option value="USD">دولار أمريكي (USD)</option>
              <option value="EUR">يورو أوروبي (EUR)</option>
            </select>
          </div>
        </div>

        {/* Visual Appearance & Theme Settings (Pure Light Mode Locked) */}
        <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)', borderBottom: '1px solid var(--border-main)', paddingBottom: '12px' }}>
            المظهر ونظام التصميم البصري
          </h3>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <label className="form-label" style={{ margin: 0 }}>نمط الواجهة المعتمد</label>

            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '16px',
                borderRadius: 'var(--radius-lg)',
                border: '2px solid var(--primary)',
                backgroundColor: 'var(--primary-light)'
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <Sun size={24} style={{ color: 'var(--primary)' }} />
                <div>
                  <div style={{ fontWeight: 800, color: 'var(--text-primary)', fontSize: '14px' }}>
                    النمط الفاتح النقي (Pure Light Mode)
                  </div>
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                    مثبت ومعتمد وفق ملف DESIGN.md لضمان أعلى مستويات التباين والوضوح
                  </div>
                </div>
              </div>

              <span className="badge badge-success">مفعل افتراضياً</span>
            </div>
          </div>

          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">لغة الواجهة الأساسية</label>
            <select
              value={defaultLanguage}
              onChange={(e) => setDefaultLanguage(e.target.value)}
              className="input-field"
            >
              <option value="ar">العربية (RTL) — الافتراضية</option>
              <option value="en">English (LTR)</option>
            </select>
          </div>
        </div>
      </div>
    </div>
  );
};
