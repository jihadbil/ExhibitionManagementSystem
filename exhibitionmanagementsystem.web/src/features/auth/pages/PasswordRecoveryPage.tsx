import { useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authService } from '../services/auth.service';
import { getApiErrorMessage } from '../../../services/apiError';

export const PasswordRecoveryPage = ({ reset = false }: { reset?: boolean }) => {
  const [params] = useSearchParams();
  const [email, setEmail] = useState(params.get('email') ?? '');
  const token = params.get('token') ?? '';
  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const invalidLink = reset && (!token || !params.get('email'));

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError('');
    if (reset && password !== confirmation) { setError('كلمتا المرور غير متطابقتين.'); return; }
    setBusy(true);
    try {
      if (reset) {
        await authService.resetPassword(email.trim(), token, password, confirmation);
        setPassword(''); setConfirmation('');
        setMessage('تم تعيين كلمة المرور. يمكنك تسجيل الدخول بكلمة المرور الجديدة.');
      } else {
        await authService.requestPasswordReset(email.trim());
        setMessage('إذا كان البريد مسجلاً، ستصلك رسالة تحتوي رابط إعادة تعيين كلمة المرور.');
      }
    } catch (cause) {
      setError(getApiErrorMessage(cause, reset ? 'الرابط غير صالح أو منتهي. اطلب رابطًا جديدًا.' : 'تعذر إرسال الطلب. حاول مرة أخرى.'));
    } finally { setBusy(false); }
  };

  return <main dir="rtl" style={{ minHeight: '100vh', display: 'grid', placeItems: 'center', padding: 24, background: 'var(--bg-main)' }}>
    <section className="card" style={{ width: 'min(440px, 100%)', padding: 32 }}>
      <h1 style={{ marginBottom: 20 }}>{reset ? 'تعيين كلمة مرور جديدة' : 'استعادة كلمة المرور'}</h1>
      {invalidLink ? <p role="alert">رابط الاستعادة غير مكتمل. اطلب رابطًا جديدًا.</p> : message ? <p role="status">{message}</p> :
        <form onSubmit={submit} style={{ display: 'grid', gap: 16 }}>
          <label style={{ display: 'grid', gap: 8 }}>البريد الإلكتروني<input className="input-field" dir="ltr" type="email" required autoComplete="email" value={email} readOnly={reset} onChange={event => setEmail(event.target.value)} /></label>
          {reset && <>
            <label style={{ display: 'grid', gap: 8 }}>كلمة المرور الجديدة<input className="input-field" type="password" required minLength={8} autoComplete="new-password" value={password} onChange={event => setPassword(event.target.value)} /></label>
            <label style={{ display: 'grid', gap: 8 }}>تأكيد كلمة المرور<input className="input-field" type="password" required minLength={8} autoComplete="new-password" value={confirmation} onChange={event => setConfirmation(event.target.value)} /></label>
            <p style={{ fontSize: 13 }}>استخدم 8 أحرف على الأقل، وحروفًا كبيرة وصغيرة ورقمًا ورمزًا.</p>
          </>}
          {error && <p role="alert" style={{ color: 'var(--danger)' }}>{error}</p>}
          <button className="btn-primary" type="submit" disabled={busy}>{busy ? 'جاري التنفيذ…' : reset ? 'حفظ كلمة المرور' : 'إرسال رابط الاستعادة'}</button>
        </form>}
      <nav style={{ display: 'flex', gap: 20, marginTop: 24 }}><Link to="/login">العودة لتسجيل الدخول</Link>{reset && <Link to="/forgot-password">طلب رابط جديد</Link>}</nav>
    </section>
  </main>;
};

export const ResetPasswordPage = () => <PasswordRecoveryPage reset />;
