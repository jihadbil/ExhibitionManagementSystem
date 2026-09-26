import React, { useState, useEffect } from 'react';
import {
  QrCode,
  Scan,
  CheckCircle2,
  Printer,
  Users,
  ShieldCheck,
  Building2,
  Sparkles,
  ArrowRight,
  Maximize,
  Volume2
} from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import { useToast } from '../../../app/providers/ToastContext';

interface CheckedInAttendee {
  id: string;
  name: string;
  company: string;
  category: string;
  ticketNumber: string;
  checkInTime: string;
  boothLocation: string;
}

const mockRecentCheckIns: CheckedInAttendee[] = [
  { id: '1', name: 'المهندس أحمد الهادي الفرجاني', company: 'مؤسسة التقنية والشبكات', category: 'VIP', ticketNumber: 'TKT-2026-8812', checkInTime: '10:45 ص', boothLocation: 'قاعة الابتكار | جناح A-101' },
  { id: '2', name: 'سارة عبد السلام المنفي', company: 'شركة البرمجيات المتطورة', category: 'عارض', ticketNumber: 'TKT-2026-9043', checkInTime: '10:42 ص', boothLocation: 'صالة B | جناح B-204' },
  { id: '3', name: 'د. عادل محمود الورفلي', company: 'المركز الليبي للبحوث', category: 'زائر', ticketNumber: 'TKT-2026-7731', checkInTime: '10:38 ص', boothLocation: 'المنطقة العامة' },
];

export const CheckInKioskPage: React.FC = () => {
  const { showToast } = useToast();
  const [scanCode, setScanCode] = useState('');
  const [todayCount, setTodayCount] = useState(384);
  const [gateName, setGateName] = useState('البوابة الرئيسية (Gate A)');
  const [autoPrint, setAutoPrint] = useState(true);
  const [recentList, setRecentList] = useState<CheckedInAttendee[]>(mockRecentCheckIns);

  // Success Flash state
  const [currentAttendee, setCurrentAttendee] = useState<CheckedInAttendee | null>(null);
  const [isSuccessFlash, setIsSuccessFlash] = useState(false);

  const handleManualScan = (e: React.FormEvent) => {
    e.preventDefault();
    if (!scanCode.trim()) return;

    processCheckIn(scanCode.trim());
    setScanCode('');
  };

  const processCheckIn = (code: string) => {
    const attendee: CheckedInAttendee = {
      id: Date.now().toString(),
      name: 'د. طارق مصطفى المنصوري',
      company: 'الشركة الليبية لتقنية المعلومات والاتصالات',
      category: 'VIP خاص',
      ticketNumber: code.toUpperCase(),
      checkInTime: new Date().toLocaleTimeString('ar-LY', { hour: '2-digit', minute: '2-digit' }),
      boothLocation: 'القاعة الكبرى | جناح VIP-01'
    };

    setCurrentAttendee(attendee);
    setIsSuccessFlash(true);
    setTodayCount(prev => prev + 1);
    setRecentList(prev => [attendee, ...prev.slice(0, 5)]);

    showToast(`تم تسجيل حضور: ${attendee.name} بنجاح!`, 'success');

    if (autoPrint) {
      setTimeout(() => {
        showToast('جاري إرسال الشارة إلى طابعة البوابة تلقائياً...', 'info');
      }, 800);
    }

    // Dismiss flash after 4 seconds
    setTimeout(() => {
      setIsSuccessFlash(false);
    }, 4500);
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Scan size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              كشك الاستقبال وطباعة الشارات الفورية (Self Check-in Kiosk)
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            تسجيل دخول الحضور بمسح كود QR أو إدخال رقم التذكرة وطباعة شارة الدخول آلياً
          </p>
        </div>

        {/* Stats & Gate Info */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
          <div className="card" style={{ padding: '8px 16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Users size={16} style={{ color: 'var(--success)' }} />
            <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>حضور اليوم:</span>
            <span style={{ fontSize: '15px', fontWeight: 800, color: 'var(--text-primary)' }}>{todayCount}</span>
          </div>

          <select
            value={gateName}
            onChange={(e) => setGateName(e.target.value)}
            className="input-field"
            style={{ width: 'auto', height: '40px', fontWeight: 600, fontSize: '13px' }}
          >
            <option value="البوابة الرئيسية (Gate A)">البوابة الرئيسية (Gate A)</option>
            <option value="بوابة كبار الشخصيات (VIP Gate)">بوابة كبار الشخصيات (VIP Gate)</option>
            <option value="البوابة الشمالية (Gate B)">البوابة الشمالية (Gate B)</option>
            <option value="بوابة العارضين والإعلام (Gate C)">بوابة العارضين والإعلام (Gate C)</option>
          </select>
        </div>
      </div>

      {/* Main Kiosk Split Layout */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1.2fr 1fr',
          gap: '24px',
          alignItems: 'start'
        }}
      >
        {/* Left/Main Scanning Kiosk Area */}
        <div
          className="card"
          style={{
            padding: '36px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            textAlign: 'center',
            gap: '24px',
            backgroundColor: '#ffffff',
            border: isSuccessFlash ? '2px solid var(--success)' : '1px solid var(--border-main)',
            boxShadow: isSuccessFlash ? '0 10px 30px rgba(16, 185, 129, 0.2)' : 'var(--shadow-sm)',
            transition: 'all 0.3s ease'
          }}
        >
          {isSuccessFlash && currentAttendee ? (
            /* Instant Green Confirmation Card */
            <div
              style={{
                width: '100%',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: '16px',
                animation: 'slideIn 0.3s ease-out'
              }}
            >
              <div
                style={{
                  width: '72px',
                  height: '72px',
                  borderRadius: '50%',
                  backgroundColor: 'var(--success-bg)',
                  color: 'var(--success-text)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  boxShadow: '0 8px 16px rgba(16, 185, 129, 0.2)'
                }}
              >
                <CheckCircle2 size={44} />
              </div>

              <div>
                <span
                  style={{
                    backgroundColor: '#d1fae5',
                    color: '#059669',
                    padding: '4px 14px',
                    borderRadius: 'var(--radius-full)',
                    fontSize: '12px',
                    fontWeight: 800
                  }}
                >
                  دخول معتمد بنجاح ✓
                </span>
                <h2 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)', marginTop: '8px' }}>
                  {currentAttendee.name}
                </h2>
                <p style={{ fontSize: '14px', color: 'var(--text-secondary)' }}>
                  {currentAttendee.company} — <strong style={{ color: 'var(--primary)' }}>{currentAttendee.category}</strong>
                </p>
                <div style={{ fontSize: '13px', color: 'var(--text-muted)', marginTop: '4px' }}>
                  {currentAttendee.boothLocation}
                </div>
              </div>

              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  backgroundColor: 'var(--bg-secondary)',
                  padding: '10px 20px',
                  borderRadius: 'var(--radius-lg)',
                  color: 'var(--primary)',
                  fontWeight: 600,
                  fontSize: '13px'
                }}
              >
                <Printer size={18} />
                <span>جاري طباعة شارة الدخول على الطابعة الحرارية...</span>
              </div>
            </div>
          ) : (
            /* Normal Scanning Awaiting Mode */
            <>
              <div
                style={{
                  width: '180px',
                  height: '180px',
                  borderRadius: '24px',
                  backgroundColor: 'var(--bg-secondary)',
                  border: '2px dashed var(--primary)',
                  display: 'flex',
                  flexDirection: 'column',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '12px',
                  color: 'var(--primary)'
                }}
              >
                <QrCode size={64} />
                <span style={{ fontSize: '12px', fontWeight: 700 }}>مرر رمز QR أمام الماسح</span>
              </div>

              <div>
                <h3 style={{ fontSize: '20px', fontWeight: 800, color: 'var(--text-primary)', marginBottom: '6px' }}>
                  جاهز لمسح التذاكر واستقبال الزوار
                </h3>
                <p style={{ fontSize: '13px', color: 'var(--text-secondary)', maxWidth: '400px' }}>
                  وجه رمز الاستجابة السريعة على هاتف الزائر أو التذكرة المطبوعة نحو الكاميرا أو الماسح الضوئي
                </p>
              </div>

              {/* Manual Barcode / Code Input Form */}
              <form
                onSubmit={handleManualScan}
                style={{
                  width: '100%',
                  maxWidth: '420px',
                  display: 'flex',
                  gap: '8px'
                }}
              >
                <input
                  type="text"
                  value={scanCode}
                  onChange={(e) => setScanCode(e.target.value)}
                  placeholder="أو أدخل رقم التذكرة يدوياً (مثال: TKT-2026-VIP)..."
                  className="input-field"
                  style={{ height: '46px' }}
                />
                <button
                  type="submit"
                  className="btn-primary"
                  style={{ height: '46px', padding: '0 20px', flexShrink: 0 }}
                >
                  <span>تحقق ودخول</span>
                </button>
              </form>

              {/* Quick Simulator Buttons */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '12px' }}>
                <span style={{ color: 'var(--text-muted)' }}>أكواد تجريبية للمحاكاة:</span>
                <button
                  type="button"
                  onClick={() => processCheckIn('TKT-VIP-9941')}
                  style={{ color: 'var(--primary)', fontWeight: 600, textDecoration: 'underline' }}
                >
                  TKT-VIP-9941
                </button>
                <span style={{ color: 'var(--text-muted)' }}>•</span>
                <button
                  type="button"
                  onClick={() => processCheckIn('TKT-EXPO-2026')}
                  style={{ color: 'var(--primary)', fontWeight: 600, textDecoration: 'underline' }}
                >
                  TKT-EXPO-2026
                </button>
              </div>
            </>
          )}

          {/* Kiosk Controls Options */}
          <div
            style={{
              width: '100%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              borderTop: '1px solid var(--border-main)',
              paddingTop: '16px',
              fontSize: '13px'
            }}
          >
            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer', color: 'var(--text-secondary)' }}>
              <input
                type="checkbox"
                checked={autoPrint}
                onChange={(e) => setAutoPrint(e.target.checked)}
                style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
              />
              <span>طباعة الشارة تلقائياً عند كل مسح ناجح</span>
            </label>

            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              حالة الطابعة: <strong style={{ color: 'var(--success)' }}>متصلة وجاهزة ✓</strong>
            </span>
          </div>
        </div>

        {/* Right Panel: Recent Check-ins Stream */}
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div
            style={{
              padding: '16px 20px',
              borderBottom: '1px solid var(--border-main)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between'
            }}
          >
            <h3 style={{ fontSize: '15px', fontWeight: 700, color: 'var(--text-primary)' }}>
              سجل الحضور الأخير (Live Stream)
            </h3>
            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{gateName}</span>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column' }}>
            {recentList.map((item, idx) => (
              <div
                key={item.id}
                style={{
                  padding: '14px 20px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  borderBottom: idx !== recentList.length - 1 ? '1px solid var(--border-main)' : 'none',
                  backgroundColor: idx === 0 && isSuccessFlash ? '#f0fdf4' : 'transparent',
                  transition: 'background-color 0.3s ease'
                }}
              >
                <div>
                  <div style={{ fontWeight: 700, fontSize: '13px', color: 'var(--text-primary)' }}>
                    {item.name}
                  </div>
                  <div style={{ fontSize: '11px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    {item.company}
                  </div>
                  <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                    {item.boothLocation}
                  </div>
                </div>

                <div style={{ textAlign: 'left' }}>
                  <span
                    className={`badge ${
                      item.category.includes('VIP')
                        ? 'badge-vip'
                        : item.category.includes('عارض')
                        ? 'badge-featured'
                        : 'badge-standard'
                    }`}
                  >
                    {item.category}
                  </span>
                  <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px', textAlign: 'center' }}>
                    {item.checkInTime}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
