import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  AlertCircle,
  ArrowLeft,
  Building2,
  CheckCircle2,
  Eye,
  EyeOff,
  Globe2,
  Lock,
  Mail,
  Phone,
  UserRound,
} from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';
import { getApiErrorMessage } from '../../../services/apiError';
import { authService } from '../services/auth.service';
import type { CompanyRegistrationRequestDto } from '../types/auth.types';
import './RegistrationPage.css';

const currencies = [
  { code: 'LYD', label: 'دينار ليبي (LYD)' },
  { code: 'USD', label: 'دولار أمريكي (USD)' },
  { code: 'EUR', label: 'يورو (EUR)' },
  { code: 'SAR', label: 'ريال سعودي (SAR)' },
  { code: 'AED', label: 'درهم إماراتي (AED)' },
  { code: 'GBP', label: 'جنيه إسترليني (GBP)' },
];

const initialForm: CompanyRegistrationRequestDto = {
  companyName: '',
  subdomain: '',
  adminFullName: '',
  adminEmail: '',
  adminPhoneNumber: '',
  password: '',
  confirmPassword: '',
  baseCurrency: 'LYD',
  acceptTerms: false,
};

type IconFieldProps = React.InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  icon: React.ReactNode;
  hint?: string;
};

const IconField = ({ label, icon, hint, ...props }: IconFieldProps) => (
  <div className="registration-field">
    <label htmlFor={props.id}>{label}</label>
    <div className="registration-input-wrap">
      <span className="registration-input-icon">{icon}</span>
      <input {...props} className="input-field" />
    </div>
    {hint && <small>{hint}</small>}
  </div>
);

export const RegistrationPage: React.FC = () => {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const [form, setForm] = useState(initialForm);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  const update = <K extends keyof CompanyRegistrationRequestDto>(
    field: K,
    value: CompanyRegistrationRequestDto[K],
  ) => setForm(current => ({ ...current, [field]: value }));

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setErrorMessage('');

    if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}/.test(form.password)) {
      setErrorMessage('يجب أن تتكون كلمة المرور من 8 أحرف على الأقل، وتشمل حرفاً كبيراً وصغيراً ورقماً ورمزاً خاصاً.');
      return;
    }
    if (form.password !== form.confirmPassword) {
      setErrorMessage('كلمتا المرور غير متطابقتين.');
      return;
    }
    if (!form.acceptTerms) {
      setErrorMessage('يجب الموافقة على شروط الاستخدام وسياسة الخصوصية.');
      return;
    }

    setIsLoading(true);
    try {
      const result = await authService.registerCompany({
        ...form,
        companyName: form.companyName.trim(),
        subdomain: form.subdomain.trim(),
        adminFullName: form.adminFullName.trim(),
        adminEmail: form.adminEmail.trim(),
        adminPhoneNumber: form.adminPhoneNumber?.trim() || undefined,
      });
      showToast(result.message, 'success');
      navigate('/login', { replace: true, state: { email: result.adminEmail } });
    } catch (error) {
      const message = getApiErrorMessage(error, 'تعذر إنشاء حساب الشركة. يرجى مراجعة البيانات والمحاولة مجدداً.');
      setErrorMessage(message);
      showToast(message, 'danger');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="registration-page" dir="rtl">
      <section className="registration-hero">
        <div className="registration-brand">
          <span className="registration-logo"><Building2 size={28} /></span>
          <div>
            <strong>ExpoManager</strong>
            <span>المنظومة السحابية لإدارة المعارض</span>
          </div>
        </div>

        <div className="registration-hero-copy">
          <span className="registration-kicker">ابدأ خلال دقائق</span>
          <h1>أنشئ مساحة عمل شركتك وأدِر معارضك من مكان واحد</h1>
          <p>حساب مستقل وآمن لشركتك يشمل إدارة المواقع والأجنحة والحجوزات والتذاكر والتقارير المالية.</p>
          <ul>
            <li><CheckCircle2 size={19} /> فترة تجريبية مجانية لمدة 14 يوماً</li>
            <li><CheckCircle2 size={19} /> حساب مدير بصلاحيات إدارة الشركة</li>
            <li><CheckCircle2 size={19} /> عزل كامل وآمن لبيانات كل شركة</li>
          </ul>
        </div>

        <p className="registration-copyright">© 2026 ExpoManager — جميع الحقوق محفوظة</p>
      </section>

      <section className="registration-form-side">
        <div className="registration-form-card">
          <div className="registration-heading">
            <span className="registration-step">تسجيل شركة إدارة معارض</span>
            <h2>أنشئ حساب شركتك</h2>
            <p>أدخل بيانات الشركة ومدير الحساب. يمكنك إضافة باقي فريق العمل لاحقاً.</p>
          </div>

          {errorMessage && (
            <div className="registration-error" role="alert">
              <AlertCircle size={19} />
              <span>{errorMessage}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} className="registration-form">
            <div className="registration-grid">
              <IconField
                id="companyName"
                label="اسم الشركة"
                icon={<Building2 size={18} />}
                value={form.companyName}
                onChange={event => update('companyName', event.target.value)}
                placeholder="شركة الريادة لتنظيم المعارض"
                minLength={2}
                maxLength={200}
                autoComplete="organization"
                required
              />
              <IconField
                id="subdomain"
                label="النطاق المختصر"
                icon={<Globe2 size={18} />}
                value={form.subdomain}
                onChange={event => update('subdomain', event.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
                placeholder="alriyada-expo"
                pattern="[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?"
                maxLength={63}
                dir="ltr"
                hint="أحرف إنجليزية صغيرة وأرقام وشرطة فقط"
                required
              />
              <IconField
                id="adminFullName"
                label="اسم مدير الحساب"
                icon={<UserRound size={18} />}
                value={form.adminFullName}
                onChange={event => update('adminFullName', event.target.value)}
                placeholder="الاسم الكامل"
                minLength={2}
                maxLength={100}
                autoComplete="name"
                required
              />
              <IconField
                id="adminPhoneNumber"
                label="رقم الهاتف (اختياري)"
                icon={<Phone size={18} />}
                value={form.adminPhoneNumber}
                onChange={event => update('adminPhoneNumber', event.target.value)}
                placeholder="+218 91 000 0000"
                maxLength={30}
                autoComplete="tel"
                dir="ltr"
              />
            </div>

            <IconField
              id="adminEmail"
              label="البريد الإلكتروني لمدير الحساب"
              icon={<Mail size={18} />}
              type="email"
              value={form.adminEmail}
              onChange={event => update('adminEmail', event.target.value)}
              placeholder="admin@company.com"
              maxLength={256}
              autoComplete="email"
              dir="ltr"
              required
            />

            <div className="registration-field">
              <label htmlFor="baseCurrency">العملة الأساسية للشركة</label>
              <select
                id="baseCurrency"
                className="input-field registration-select"
                value={form.baseCurrency}
                onChange={event => update('baseCurrency', event.target.value)}
              >
                {currencies.map(currency => (
                  <option key={currency.code} value={currency.code}>{currency.label}</option>
                ))}
              </select>
            </div>

            <div className="registration-grid">
              <div className="registration-field">
                <label htmlFor="password">كلمة المرور</label>
                <div className="registration-input-wrap">
                  <span className="registration-input-icon"><Lock size={18} /></span>
                  <input
                    id="password"
                    className="input-field"
                    type={showPassword ? 'text' : 'password'}
                    value={form.password}
                    onChange={event => update('password', event.target.value)}
                    placeholder="8 أحرف على الأقل"
                    minLength={8}
                    maxLength={100}
                    autoComplete="new-password"
                    required
                  />
                  <button
                    type="button"
                    className="registration-password-toggle"
                    onClick={() => setShowPassword(value => !value)}
                    aria-label={showPassword ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور'}
                  >
                    {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                </div>
                <small>تتضمن حرفاً كبيراً وصغيراً ورقماً ورمزاً خاصاً</small>
              </div>
              <IconField
                id="confirmPassword"
                label="تأكيد كلمة المرور"
                icon={<Lock size={18} />}
                type={showPassword ? 'text' : 'password'}
                value={form.confirmPassword}
                onChange={event => update('confirmPassword', event.target.value)}
                placeholder="أعد إدخال كلمة المرور"
                minLength={8}
                maxLength={100}
                autoComplete="new-password"
                required
              />
            </div>

            <label className="registration-terms">
              <input
                type="checkbox"
                checked={form.acceptTerms}
                onChange={event => update('acceptTerms', event.target.checked)}
                required
              />
              <span>أوافق على شروط الاستخدام وسياسة الخصوصية الخاصة بالمنصة.</span>
            </label>

            <button type="submit" className="btn-primary registration-submit" disabled={isLoading}>
              {isLoading ? 'جاري إنشاء حساب الشركة...' : (
                <>إنشاء حساب الشركة <ArrowLeft size={18} /></>
              )}
            </button>
          </form>

          <p className="registration-login-link">
            لديك حساب بالفعل؟ <Link to="/login">تسجيل الدخول</Link>
          </p>
        </div>
      </section>
    </main>
  );
};
