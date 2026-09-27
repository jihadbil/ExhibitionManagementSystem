import React, { useState, useEffect } from 'react';
import {
  Settings,
  User,
  Shield,
  History,
  Palette,
  Save,
  KeyRound,
  Building2,
  Mail,
  Phone,
  Sun,
  CheckCircle2,
  AlertCircle,
  Clock,
  Sparkles,
  Eye,
  EyeOff
} from 'lucide-react';
import { api } from '../../../services/api';
import { useToast } from '../../../app/providers/ToastContext';
import { useAuth } from '../../../app/providers/AuthContext';

interface UserProfile {
  userId: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  tenantID: number;
  tenantName: string;
  isActive: boolean;
  lastLogin?: string;
  roles: string[];
}

interface AuditLog {
  logID: number;
  tableName: string;
  recordID: string;
  action: string;
  actionAt: string;
  ipAddress: string;
}

export const SettingsPage: React.FC = () => {
  const { showToast } = useToast();
  const { user } = useAuth();

  const [activeTab, setActiveTab] = useState<'profile' | 'security' | 'audit' | 'appearance'>('profile');
  const [loading, setLoading] = useState(true);
  const [savingProfile, setSavingProfile] = useState(false);
  const [savingPassword, setSavingPassword] = useState(false);

  // Profile State
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [fullName, setFullName] = useState(user?.fullName || '');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [currency, setCurrency] = useState(user?.baseCurrency || 'LYD');
  const [language, setLanguage] = useState('ar');

  // Password State
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showCurrentPass, setShowCurrentPass] = useState(false);
  const [showNewPass, setShowNewPass] = useState(false);

  // Audit Logs State
  const [auditLogs, setAuditLogs] = useState<AuditLog[]>([]);
  const [loadingLogs, setLoadingLogs] = useState(false);

  // Fetch Profile
  const loadProfile = async () => {
    setLoading(true);
    try {
      const res = await api.get<UserProfile>('/api/profile');
      if (res.data) {
        setProfile(res.data);
        setFullName(res.data.fullName || '');
        setPhoneNumber(res.data.phoneNumber || '');
      }
    } catch (err) {
      console.error('Failed to load profile:', err);
      // Fallback from auth session
      if (user) {
        setFullName(user.fullName);
      }
    } finally {
      setLoading(false);
    }
  };

  // Fetch Audit Logs
  const loadAuditLogs = async () => {
    setLoadingLogs(true);
    try {
      const res = await api.get<{ items: AuditLog[] }>('/api/profile/audit-logs?pageNumber=1&pageSize=15');
      setAuditLogs(res.data.items || []);
    } catch (err) {
      console.error('Failed to load audit logs:', err);
    } finally {
      setLoadingLogs(false);
    }
  };

  useEffect(() => {
    void loadProfile();
  }, []);

  useEffect(() => {
    if (activeTab === 'audit' && auditLogs.length === 0) {
      void loadAuditLogs();
    }
  }, [activeTab]);

  // Handle Save Profile
  const handleSaveProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullName.trim()) {
      showToast('الرجاء إدخال الاسم الكامل', 'danger');
      return;
    }

    setSavingProfile(true);
    try {
      await api.put('/api/profile', {
        fullName: fullName.trim(),
        phoneNumber: phoneNumber.trim()
      });

      // Update local storage user object if exists
      const stored = localStorage.getItem('auth_user');
      if (stored) {
        try {
          const parsed = JSON.parse(stored);
          parsed.fullName = fullName.trim();
          localStorage.setItem('auth_user', JSON.stringify(parsed));
        } catch {}
      }

      showToast('تم حفظ تعديلات الملف الشخصي بنجاح ✓', 'success');
      await loadProfile();
    } catch (err: any) {
      console.error('Failed to save profile:', err);
      const msg = err.response?.data?.message || 'فشل حفظ الملف الشخصي.';
      showToast(msg, 'danger');
    } finally {
      setSavingProfile(false);
    }
  };

  // Handle Change Password
  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!currentPassword) {
      showToast('الرجاء إدخال كلمة المرور الحالية', 'danger');
      return;
    }

    if (!newPassword || newPassword.length < 6) {
      showToast('كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف أو أرقام', 'danger');
      return;
    }

    if (newPassword !== confirmPassword) {
      showToast('كلمة المرور الجديدة غير مطابقة لتأكيد كلمة المرور', 'danger');
      return;
    }

    setSavingPassword(true);
    try {
      await api.post('/api/auth/change-password', {
        currentPassword,
        newPassword,
        confirmNewPassword: confirmPassword
      });

      showToast('تم تغيير كلمة المرور بنجاح ✓', 'success');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err: any) {
      console.error('Failed to change password:', err);
      const msg =
        err.response?.data?.message ||
        err.response?.data?.title ||
        'فشل تغيير كلمة المرور. يرجى التأكد من صحة كلمة المرور الحالية.';
      showToast(typeof msg === 'string' ? msg : 'فشل تغيير كلمة المرور', 'danger');
    } finally {
      setSavingPassword(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Settings size={26} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)', margin: 0 }}>
              إعدادات النظام والملف الشخصي
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '4px', margin: 0 }}>
            إدارة بيانات حسابك، كلمة المرور، أمان النظام وتفضيلات المظهر
          </p>
        </div>

        {/* User Badges */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span className="badge badge-primary">
            {profile?.roles?.[0] || user?.roles?.[0] || 'مدير النظام'}
          </span>
          <span className="badge badge-secondary">
            {profile?.tenantName || user?.tenantName || 'المؤسسة الرئيسية'}
          </span>
        </div>
      </div>

      {/* Tabs Bar */}
      <div style={{ display: 'flex', gap: '8px', borderBottom: '2px solid var(--border-light)', paddingBottom: '2px' }}>
        <button
          onClick={() => setActiveTab('profile')}
          className={`tab-btn ${activeTab === 'profile' ? 'active' : ''}`}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '10px 18px',
            borderRadius: '8px 8px 0 0',
            border: 'none',
            background: activeTab === 'profile' ? 'var(--surface)' : 'transparent',
            color: activeTab === 'profile' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: activeTab === 'profile' ? 700 : 500,
            borderBottom: activeTab === 'profile' ? '2px solid var(--primary)' : 'none',
            cursor: 'pointer'
          }}
        >
          <User size={16} />
          <span>الملف الشخصي والمؤسسة</span>
        </button>

        <button
          onClick={() => setActiveTab('security')}
          className={`tab-btn ${activeTab === 'security' ? 'active' : ''}`}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '10px 18px',
            borderRadius: '8px 8px 0 0',
            border: 'none',
            background: activeTab === 'security' ? 'var(--surface)' : 'transparent',
            color: activeTab === 'security' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: activeTab === 'security' ? 700 : 500,
            borderBottom: activeTab === 'security' ? '2px solid var(--primary)' : 'none',
            cursor: 'pointer'
          }}
        >
          <Shield size={16} />
          <span>أمان الحساب وكلمة المرور</span>
        </button>

        <button
          onClick={() => setActiveTab('audit')}
          className={`tab-btn ${activeTab === 'audit' ? 'active' : ''}`}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '10px 18px',
            borderRadius: '8px 8px 0 0',
            border: 'none',
            background: activeTab === 'audit' ? 'var(--surface)' : 'transparent',
            color: activeTab === 'audit' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: activeTab === 'audit' ? 700 : 500,
            borderBottom: activeTab === 'audit' ? '2px solid var(--primary)' : 'none',
            cursor: 'pointer'
          }}
        >
          <History size={16} />
          <span>سجل نشاط الحساب</span>
        </button>

        <button
          onClick={() => setActiveTab('appearance')}
          className={`tab-btn ${activeTab === 'appearance' ? 'active' : ''}`}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            padding: '10px 18px',
            borderRadius: '8px 8px 0 0',
            border: 'none',
            background: activeTab === 'appearance' ? 'var(--surface)' : 'transparent',
            color: activeTab === 'appearance' ? 'var(--primary)' : 'var(--text-secondary)',
            fontWeight: activeTab === 'appearance' ? 700 : 500,
            borderBottom: activeTab === 'appearance' ? '2px solid var(--primary)' : 'none',
            cursor: 'pointer'
          }}
        >
          <Palette size={16} />
          <span>المظهر والنمط البصري</span>
        </button>
      </div>

      {/* Tab 1: Profile & Organization */}
      {activeTab === 'profile' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(420px, 1fr))', gap: '24px' }}>
          {/* User Details */}
          <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', borderBottom: '1px solid var(--border-light)', paddingBottom: '12px' }}>
              <User size={18} style={{ color: 'var(--primary)' }} />
              <h3 style={{ fontSize: '16px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                بيانات المستخدم الشخصية
              </h3>
            </div>

            <form onSubmit={handleSaveProfile} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الاسم الكامل</label>
                <input
                  type="text"
                  required
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  className="input-field"
                />
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">البريد الإلكتروني (المعرف الأساسي)</label>
                <div style={{ position: 'relative' }}>
                  <input
                    type="email"
                    disabled
                    value={profile?.email || user?.email || ''}
                    className="input-field"
                    style={{ backgroundColor: 'var(--surface-sunken)', color: 'var(--text-secondary)', paddingRight: '36px' }}
                  />
                  <Mail size={16} style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                </div>
                <span style={{ fontSize: '11px', color: 'var(--text-secondary)', marginTop: '4px' }}>
                  لتعديل البريد الإلكتروني، يرجى التواصل مع المسؤول المباشر.
                </span>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">رقم الهاتف للتواصل</label>
                <div style={{ position: 'relative' }}>
                  <input
                    type="tel"
                    placeholder="مثال: +218 91 0000000"
                    value={phoneNumber}
                    onChange={(e) => setPhoneNumber(e.target.value)}
                    className="input-field"
                    style={{ paddingRight: '36px' }}
                  />
                  <Phone size={16} style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-secondary)' }} />
                </div>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '8px' }}>
                <button
                  type="submit"
                  disabled={savingProfile}
                  className="btn-primary"
                  style={{ height: '40px', padding: '0 24px', gap: '8px' }}
                >
                  <Save size={16} />
                  <span>{savingProfile ? 'جاري الحفظ...' : 'حفظ تعديلات الملف'}</span>
                </button>
              </div>
            </form>
          </div>

          {/* Organization & Preferences */}
          <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', borderBottom: '1px solid var(--border-light)', paddingBottom: '12px' }}>
              <Building2 size={18} style={{ color: 'var(--primary)' }} />
              <h3 style={{ fontSize: '16px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                بيانات المستأجر والتفضيلات التشغيلية
              </h3>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">المستأجر / المنظمة التابع لها</label>
                <input
                  type="text"
                  disabled
                  value={profile?.tenantName || user?.tenantName || 'الهيئة العامة للمعارض'}
                  className="input-field"
                  style={{ backgroundColor: 'var(--surface-sunken)', color: 'var(--text-secondary)' }}
                />
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">العملة الافتراضية للتعاملات والفوترة</label>
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

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">لغة النظام والتقارير</label>
                <select
                  value={language}
                  onChange={(e) => setLanguage(e.target.value)}
                  className="input-field"
                >
                  <option value="ar">العربية (ليبيا) — RTL</option>
                  <option value="en">English (US) — LTR</option>
                </select>
              </div>

              <div style={{ padding: '14px', borderRadius: 'var(--radius-md)', backgroundColor: 'var(--surface-sunken)', border: '1px solid var(--border-light)', display: 'flex', flexDirection: 'column', gap: '6px' }}>
                <div style={{ fontSize: '12px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  حالة الحساب والتسجيل
                </div>
                <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                  المعرف الرقمي: <code style={{ color: 'var(--primary)' }}>{profile?.userId || user?.userId || '—'}</code>
                </div>
                {profile?.lastLogin && (
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                    آخر تسجيل دخول: {new Date(profile.lastLogin).toLocaleString('ar-LY')}
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab 2: Security & Password */}
      {activeTab === 'security' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(420px, 1fr))', gap: '24px' }}>
          <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', borderBottom: '1px solid var(--border-light)', paddingBottom: '12px' }}>
              <KeyRound size={18} style={{ color: 'var(--primary)' }} />
              <h3 style={{ fontSize: '16px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                تغيير كلمة المرور
              </h3>
            </div>

            <form onSubmit={handleChangePassword} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">كلمة المرور الحالية</label>
                <div style={{ position: 'relative' }}>
                  <input
                    type={showCurrentPass ? 'text' : 'password'}
                    required
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    className="input-field"
                    style={{ paddingRight: '40px' }}
                  />
                  <button
                    type="button"
                    onClick={() => setShowCurrentPass(!showCurrentPass)}
                    style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-secondary)' }}
                  >
                    {showCurrentPass ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">كلمة المرور الجديدة</label>
                <div style={{ position: 'relative' }}>
                  <input
                    type={showNewPass ? 'text' : 'password'}
                    required
                    placeholder="6 أحرف أو أرقام على الأقل"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="input-field"
                    style={{ paddingRight: '40px' }}
                  />
                  <button
                    type="button"
                    onClick={() => setShowNewPass(!showNewPass)}
                    style={{ position: 'absolute', right: '12px', top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-secondary)' }}
                  >
                    {showNewPass ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">تأكيد كلمة المرور الجديدة</label>
                <input
                  type="password"
                  required
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  className="input-field"
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '8px' }}>
                <button
                  type="submit"
                  disabled={savingPassword}
                  className="btn-primary"
                  style={{ height: '40px', padding: '0 24px', gap: '8px' }}
                >
                  <Shield size={16} />
                  <span>{savingPassword ? 'جاري التحديث...' : 'تحديث كلمة المرور'}</span>
                </button>
              </div>
            </form>
          </div>

          {/* Security Standards Card */}
          <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', borderBottom: '1px solid var(--border-light)', paddingBottom: '12px' }}>
              <Shield size={18} style={{ color: 'var(--success)' }} />
              <h3 style={{ fontSize: '16px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                معايير الأمان والحماية
              </h3>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                <CheckCircle2 size={16} style={{ color: 'var(--success)', marginTop: '2px', flexShrink: 0 }} />
                <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                  تشفير كلمات المرور باستخدام خوارزميات التجزئة الآمنة (Argon2 / PBKDF2).
                </span>
              </div>

              <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                <CheckCircle2 size={16} style={{ color: 'var(--success)', marginTop: '2px', flexShrink: 0 }} />
                <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                  جلسات العمل محمية برمز JWT الآمن وتتجدد تلقائياً عبر Refresh Token.
                </span>
              </div>

              <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
                <CheckCircle2 size={16} style={{ color: 'var(--success)', marginTop: '2px', flexShrink: 0 }} />
                <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                  تسجيل جميع العمليات الحساسة في سجل تدقيق العمليات (Audit Logs) لمنع التلاعب.
                </span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab 3: Recent Audit Activity */}
      {activeTab === 'audit' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ padding: '16px 20px', borderBottom: '1px solid var(--border-light)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <History size={18} style={{ color: 'var(--primary)' }} />
              <h3 style={{ fontSize: '16px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                سجل العمليات والنشاطات الأخيرة لحسابك
              </h3>
            </div>
            <button
              onClick={loadAuditLogs}
              disabled={loadingLogs}
              className="btn-secondary"
              style={{ height: '34px', padding: '0 14px', fontSize: '12px' }}
            >
              تحديث السجل
            </button>
          </div>

          <div className="table-container">
            <table className="data-table">
              <thead>
                <tr>
                  <th>رقم السجل</th>
                  <th>الإجراء المتخذ</th>
                  <th>الجدول / العنصر</th>
                  <th>المعرف</th>
                  <th>عنوان IP</th>
                  <th>التوقيت</th>
                </tr>
              </thead>
              <tbody>
                {auditLogs.map((log) => (
                  <tr key={log.logID}>
                    <td>
                      <code style={{ fontSize: '12px' }}>#{log.logID}</code>
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          log.action?.includes('Create') || log.action?.includes('Add')
                            ? 'badge-success'
                            : log.action?.includes('Delete')
                            ? 'badge-danger'
                            : 'badge-info'
                        }`}
                      >
                        {log.action}
                      </span>
                    </td>
                    <td style={{ fontWeight: 600 }}>{log.tableName}</td>
                    <td>{log.recordID || '—'}</td>
                    <td style={{ fontFamily: 'monospace', fontSize: '12px' }}>{log.ipAddress || '127.0.0.1'}</td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', color: 'var(--text-secondary)' }}>
                        <Clock size={12} />
                        <span>{new Date(log.actionAt).toLocaleString('ar-LY')}</span>
                      </div>
                    </td>
                  </tr>
                ))}

                {!loadingLogs && auditLogs.length === 0 && (
                  <tr>
                    <td colSpan={6} style={{ textAlign: 'center', padding: '36px' }}>
                      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px' }}>
                        <History size={36} style={{ color: 'var(--border-dark)' }} />
                        <div style={{ fontWeight: 600, color: 'var(--text-secondary)' }}>
                          لا توجد سجلات تدقيق سابقة لهذا الحساب
                        </div>
                      </div>
                    </td>
                  </tr>
                )}

                {loadingLogs && (
                  <tr>
                    <td colSpan={6} style={{ textAlign: 'center', padding: '36px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', color: 'var(--text-secondary)' }}>
                        <Sparkles className="animate-spin" size={16} />
                        <span>جاري تحميل سجل العمليات...</span>
                      </div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 4: Appearance & Design */}
      {activeTab === 'appearance' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(420px, 1fr))', gap: '24px' }}>
          <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)', borderBottom: '1px solid var(--border-light)', paddingBottom: '12px', margin: 0 }}>
              المظهر ونظام التصميم البصري المعتمد
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
              <label className="form-label">اتجاه الواجهة (RTL)</label>
              <div style={{ padding: '12px', borderRadius: 'var(--radius-md)', backgroundColor: 'var(--surface-sunken)', fontSize: '13px', color: 'var(--text-secondary)' }}>
                نظام إدارة المعارض يعمل افتراضياً بالاتجاه من اليمين إلى اليسار (RTL) ليتناسب تماماً مع اللغة العربية ومعايير الهيئات الحكومية والخاصة.
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
