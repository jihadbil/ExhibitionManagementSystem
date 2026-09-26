import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Building2, Mail, Lock, LogIn, AlertCircle, CheckCircle, ShieldCheck } from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const { showToast } = useToast();

  const [email, setEmail] = useState('admin@expomanager.com');
  const [password, setPassword] = useState('Admin@123456');
  const [rememberMe, setRememberMe] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) {
      setErrorMessage('يرجى ملء جميع الحقول المطلوبة.');
      return;
    }

    setIsLoading(true);
    setErrorMessage('');

    try {
      await login({ email, password, rememberMe });
      showToast('تم تسجيل الدخول بنجاح! مرحباً بك في ExpoManager', 'success');
      navigate('/dashboard');
    } catch (err: unknown) {
      const error = err as { response?: { data?: { errorMessage?: string; message?: string } } };
      const msg = error.response?.data?.errorMessage || error.response?.data?.message || 'فشل تسجيل الدخول. يرجى التحقق من صحة البريد الإلكتروني وكلمة المرور.';
      setErrorMessage(msg);
      showToast(msg, 'danger');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        minHeight: '100vh',
        width: '100%',
        backgroundColor: '#ffffff',
        direction: 'rtl'
      }}
    >
      {/* Right Side (50%): Hero Brand Branding with Primary Gradient */}
      <div
        style={{
          flex: 1,
          background: 'var(--primary-gradient)',
          color: '#ffffff',
          display: 'flex',
          flexDirection: 'column',
          justifyContent: 'space-between',
          padding: '64px',
          position: 'relative',
          overflow: 'hidden'
        }}
      >
        {/* Subtle geometric circles */}
        <div
          style={{
            position: 'absolute',
            width: '450px',
            height: '450px',
            borderRadius: '50%',
            background: 'rgba(255, 255, 255, 0.08)',
            top: '-100px',
            right: '-100px',
            pointerEvents: 'none'
          }}
        />
        <div
          style={{
            position: 'absolute',
            width: '350px',
            height: '350px',
            borderRadius: '50%',
            background: 'rgba(255, 255, 255, 0.05)',
            bottom: '-50px',
            left: '-50px',
            pointerEvents: 'none'
          }}
        />

        {/* Top Header Logo */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px', zIndex: 1 }}>
          <div
            style={{
              width: '48px',
              height: '48px',
              borderRadius: 'var(--radius-lg)',
              background: 'rgba(255, 255, 255, 0.2)',
              backdropFilter: 'blur(8px)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              boxShadow: '0 8px 16px rgba(0,0,0,0.1)'
            }}
          >
            <Building2 size={28} color="#ffffff" />
          </div>
          <div>
            <h2 style={{ fontSize: '24px', fontWeight: 800, color: '#ffffff' }}>ExpoManager</h2>
            <p style={{ fontSize: '13px', color: 'rgba(255,255,255,0.85)' }}>المنظومة السحابية لإدارة المعارض والمؤتمرات</p>
          </div>
        </div>

        {/* Center Presentation Message */}
        <div style={{ maxWidth: '520px', zIndex: 1, marginTop: 'auto', marginBottom: 'auto' }}>
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              padding: '6px 14px',
              borderRadius: 'var(--radius-full)',
              background: 'rgba(255, 255, 255, 0.15)',
              backdropFilter: 'blur(8px)',
              fontSize: '12px',
              fontWeight: 600,
              marginBottom: '20px'
            }}
          >
            <ShieldCheck size={16} /> منصة سحابية معتمدة متعددة المستأجرين
          </span>

          <h1
            style={{
              fontSize: '38px',
              fontWeight: 800,
              lineHeight: 1.25,
              marginBottom: '18px',
              color: '#ffffff'
            }}
          >
            تحكم ذكي ومتكامل في جميع أجنحة وفعاليات معارضك
          </h1>

          <p style={{ fontSize: '16px', lineHeight: 1.7, color: 'rgba(255, 255, 255, 0.9)' }}>
            صمم مخططات الأجنحة ثنائية الأبعاد، تتبع مبيعات التذاكر، واطبع بطاقات الدخول فورياً عبر منظومة موحدة فائقة السرعة والأمان.
          </p>

          <div style={{ display: 'flex', gap: '24px', marginTop: '32px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <CheckCircle size={18} color="#ffffff" />
              <span style={{ fontSize: '14px', fontWeight: 500 }}>مصمم أجنحة 2D تفاعلي</span>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <CheckCircle size={18} color="#ffffff" />
              <span style={{ fontSize: '14px', fontWeight: 500 }}>كشك استقبال وطباعة شارات</span>
            </div>
          </div>
        </div>

        {/* Footer info */}
        <div style={{ fontSize: '12px', color: 'rgba(255,255,255,0.7)', zIndex: 1 }}>
          © 2026 جميع الحقوق محفوظة — نظام ExpoManager لإدارة المعارض
        </div>
      </div>

      {/* Left Side (50%): Login Form in Clean Pure Light Style */}
      <div
        style={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '48px',
          backgroundColor: '#ffffff'
        }}
      >
        <div style={{ width: '100%', maxWidth: '420px' }}>
          <div style={{ marginBottom: '32px' }}>
            <h2 style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text-primary)', marginBottom: '8px' }}>
              تسجيل الدخول
            </h2>
            <p style={{ color: 'var(--text-secondary)', fontSize: '14px' }}>
              أدخل بيانات حسابك للوصول إلى لوحة التحكم والعمليات
            </p>
          </div>

          {errorMessage && (
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '10px',
                padding: '12px 16px',
                backgroundColor: 'var(--danger-bg)',
                color: 'var(--danger-text)',
                borderRadius: 'var(--radius-lg)',
                borderRight: '4px solid var(--danger)',
                fontSize: '13px',
                fontWeight: 500,
                marginBottom: '20px'
              }}
            >
              <AlertCircle size={18} style={{ flexShrink: 0 }} />
              <div>{errorMessage}</div>
            </div>
          )}

          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            {/* Email Field */}
            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label" htmlFor="email">
                البريد الإلكتروني
              </label>
              <div style={{ position: 'relative' }}>
                <Mail
                  size={18}
                  style={{
                    position: 'absolute',
                    right: '14px',
                    top: '50%',
                    transform: 'translateY(-50%)',
                    color: 'var(--text-muted)'
                  }}
                />
                <input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="admin@expomanager.com"
                  className="input-field"
                  style={{ paddingRight: '42px' }}
                />
              </div>
            </div>

            {/* Password Field */}
            <div className="form-group" style={{ margin: 0 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '6px' }}>
                <label className="form-label" htmlFor="password" style={{ margin: 0 }}>
                  كلمة المرور
                </label>
                <a href="#forgot" style={{ fontSize: '12px', fontWeight: 600 }}>
                  نسيت كلمة المرور؟
                </a>
              </div>
              <div style={{ position: 'relative' }}>
                <Lock
                  size={18}
                  style={{
                    position: 'absolute',
                    right: '14px',
                    top: '50%',
                    transform: 'translateY(-50%)',
                    color: 'var(--text-muted)'
                  }}
                />
                <input
                  id="password"
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="input-field"
                  style={{ paddingRight: '42px' }}
                />
              </div>
            </div>

            {/* Remember Me Checkbox */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <input
                id="remember"
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                style={{ width: '16px', height: '16px', accentColor: 'var(--primary)', cursor: 'pointer' }}
              />
              <label htmlFor="remember" style={{ fontSize: '13px', color: 'var(--text-secondary)', cursor: 'pointer' }}>
                تذكرني على هذا الجهاز
              </label>
            </div>

            {/* Submit Button with Gradient and Glow */}
            <button
              type="submit"
              disabled={isLoading}
              className="btn-primary"
              style={{
                width: '100%',
                height: '48px',
                fontSize: '15px',
                marginTop: '10px'
              }}
            >
              {isLoading ? (
                <span>جاري تسجيل الدخول...</span>
              ) : (
                <>
                  <LogIn size={18} />
                  <span>دخول إلى لوحة التحكم</span>
                </>
              )}
            </button>
          </form>

          {/* Quick Demo Credentials Reminder */}
          <div
            style={{
              marginTop: '32px',
              padding: '16px',
              backgroundColor: 'var(--bg-secondary)',
              borderRadius: 'var(--radius-lg)',
              border: '1px dashed var(--border-main)',
              fontSize: '12px',
              color: 'var(--text-secondary)'
            }}
          >
            <div style={{ fontWeight: 700, color: 'var(--text-primary)', marginBottom: '4px' }}>
              بيانات الدخول الافتراضية للتجربة السريعة:
            </div>
            <div>البريد: <code>admin@expomanager.com</code></div>
            <div>كلمة المرور: <code>Admin@123456</code></div>
          </div>
        </div>
      </div>
    </div>
  );
};
