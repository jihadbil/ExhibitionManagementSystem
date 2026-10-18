import React, { useEffect, useRef, useState } from 'react';
import {
  QrCode,
  Scan,
  CheckCircle2,
  Printer,
  Users
} from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { BadgeCard, type BadgePrintPayload } from '../../badges/components/BadgeCard';

interface CheckedInAttendee {
  id: string;
  name: string;
  company: string;
  category: string;
  ticketNumber: string;
  checkInTime: string;
  boothLocation: string;
}

interface TicketScanResponse {
  scanID: number;
  ticketID: number;
  qrCode: string;
  scanTime: string;
  scanDirection: string;
  scanLocation: string;
}

export const CheckInKioskPage: React.FC = () => {
  const { showToast } = useToast();
  const [scanCode, setScanCode] = useState('');
  const [todayCount, setTodayCount] = useState(0);
  const [gateName, setGateName] = useState('البوابة الرئيسية (Gate A)');
  const [autoPrint, setAutoPrint] = useState(false);
  const [recentList, setRecentList] = useState<CheckedInAttendee[]>([]);
  const [isScanning, setIsScanning] = useState(false);

  // Success Flash state
  const [currentAttendee, setCurrentAttendee] = useState<CheckedInAttendee | null>(null);
  const [isSuccessFlash, setIsSuccessFlash] = useState(false);
  const [badgePayload, setBadgePayload] = useState<BadgePrintPayload | null>(null);
  const [badgeLoading, setBadgeLoading] = useState(false);
  const [pendingPrint, setPendingPrint] = useState<string | null>(null);
  const requestId = useRef(0);
  const flashTimer = useRef<number | undefined>(undefined);

  useEffect(() => () => {
    requestId.current += 1;
    window.clearTimeout(flashTimer.current);
  }, []);

  // Print only after React has mounted the actual badge, including its QR code.
  useEffect(() => {
    if (!autoPrint || !pendingPrint || badgePayload?.qrCode !== pendingPrint) return;
    let secondFrame = 0;
    const firstFrame = window.requestAnimationFrame(() => {
      secondFrame = window.requestAnimationFrame(() => {
        window.print();
        setPendingPrint(null);
      });
    });
    return () => {
      window.cancelAnimationFrame(firstFrame);
      window.cancelAnimationFrame(secondFrame);
    };
  }, [autoPrint, pendingPrint, badgePayload]);

  const loadBadge = async (qrCode: string, request: number) => {
    setBadgeLoading(true);
    try {
      const { data } = await api.get<BadgePrintPayload>(`/api/badges/render-by-qr/${encodeURIComponent(qrCode)}`);
      if (request !== requestId.current) return;
      setBadgePayload(data);
      setCurrentAttendee(current => current ? {
        ...current, name: data.fullName, company: data.companyName || '',
        category: data.participantTypeTitle,
        boothLocation: [data.venueAndHallName, data.boothNumber].filter(Boolean).join(' | '),
      } : current);
      setRecentList(current => current.map(item => item.ticketNumber === qrCode ? {
        ...item, name: data.fullName, company: data.companyName || '', category: data.participantTypeTitle,
      } : item));
      if (autoPrint) setPendingPrint(data.qrCode);
    } catch {
      if (request === requestId.current) showToast('تم تسجيل الدخول، لكن تعذر تحميل الشارة. أعد تحميلها دون إعادة مسح التذكرة.', 'warning');
    } finally {
      if (request === requestId.current) setBadgeLoading(false);
    }
  };

  const handleManualScan = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!scanCode.trim()) return;

    await processCheckIn(scanCode.trim());
  };

  const processCheckIn = async (code: string) => {
    const request = ++requestId.current;
    window.clearTimeout(flashTimer.current);
    setBadgePayload(null);
    setPendingPrint(null);
    setCurrentAttendee(null);
    setIsSuccessFlash(false);
    setIsScanning(true);
    try {
      const response = await api.post<TicketScanResponse>('/api/tickets/scan', {
        qrCode: code,
        direction: 'In',
        location: gateName,
      });
      const scan = response.data;
      if (request !== requestId.current) return;
      const attendee: CheckedInAttendee = {
        id: scan.scanID.toString(),
        name: `تذكرة رقم ${scan.ticketID}`,
        company: 'تم التحقق من الخادم',
        category: scan.scanDirection,
        ticketNumber: scan.qrCode,
        checkInTime: new Date(scan.scanTime).toLocaleTimeString('ar-LY', { hour: '2-digit', minute: '2-digit' }),
        boothLocation: scan.scanLocation || gateName,
      };

      setCurrentAttendee(attendee);
      setIsSuccessFlash(true);
      setTodayCount(previous => previous + 1);
      setRecentList(previous => [attendee, ...previous.slice(0, 5)]);
      setScanCode('');
      showToast('تم التحقق من التذكرة وتسجيل الدخول في الخادم.', 'success');

      flashTimer.current = window.setTimeout(() => {
        if (request === requestId.current) setIsSuccessFlash(false);
      }, 4500);
      await loadBadge(scan.qrCode, request);
    } catch {
      if (request === requestId.current) {
        setIsSuccessFlash(false);
        showToast('رُفضت عملية المسح. تحقق من الرمز أو حالة التذكرة.', 'danger');
      }
    } finally {
      if (request === requestId.current) setIsScanning(false);
    }
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
            <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>عمليات هذه الجلسة:</span>
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
        className="kiosk-grid"
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
              className="kiosk-confirmation"
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

              {autoPrint && (
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
                  <span>{badgeLoading ? 'جاري تجهيز الشارة للطباعة…' : badgePayload ? 'الطباعة الآلية مفعلة للشارة المحفوظة.' : 'تعذر تجهيز الشارة للطباعة.'}</span>
                </div>
              )}
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
                  disabled={isScanning}
                  style={{ height: '46px', padding: '0 20px', flexShrink: 0 }}
                >
                  <span>{isScanning ? 'جاري التحقق…' : 'تحقق ودخول'}</span>
                </button>
              </form>
            </>
          )}

          {badgePayload && (
            <>
              <BadgeCard className="badge-print-target" data={badgePayload} design={{
                ...badgePayload, categoryBadgeText: badgePayload.categoryBadgeText || badgePayload.participantTypeTitle,
              }} />
              <button className="btn-secondary" onClick={() => window.print()} disabled={isScanning || badgeLoading}>
                <Printer size={18} /> طباعة الشارة
              </button>
            </>
          )}
          {currentAttendee && !badgePayload && (
            <button className="btn-secondary" disabled={isScanning || badgeLoading}
              onClick={() => void loadBadge(currentAttendee.ticketNumber, requestId.current)}>
              {badgeLoading ? 'جاري تجهيز الشارة…' : 'إعادة تحميل الشارة للطباعة'}
            </button>
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
                onChange={(e) => { setPendingPrint(null); setAutoPrint(e.target.checked); }}
                style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
              />
              <span>طباعة الشارة تلقائياً عند كل مسح ناجح</span>
            </label>

            <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
              الطباعة: <strong>عبر مربع حوار المتصفح</strong>
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
