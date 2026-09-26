import React, { useState } from 'react';
import {
  IdCard,
  Printer,
  Building2,
  Save,
} from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import { useToast } from '../../../app/providers/ToastContext';

export const BadgeDesignerPage: React.FC = () => {
  const { showToast } = useToast();

  // Template state
  const [templateName, setTemplateName] = useState('شارة الحضور VIP المعتمدة');
  const [participantType, setParticipantType] = useState('VIP');
  const [orientation, setOrientation] = useState<'Vertical' | 'Horizontal'>('Vertical');
  const [headerColor, setHeaderColor] = useState('#6366f1');
  const [headerTextColor] = useState('#ffffff');
  const [categoryText, setCategoryText] = useState('شخصية هامة / VIP GUEST');
  const [showLogo, setShowLogo] = useState(true);
  const [showQR, setShowQR] = useState(true);
  const [showCompany, setShowCompany] = useState(true);
  const [showJobTitle, setShowJobTitle] = useState(true);
  const [showHall, setShowHall] = useState(true);
  const [footerText] = useState('ExpoManager — بطاقة دخول رقمية معتمدة');

  // Sample Attendee Data for Live Preview
  const [attendeeName] = useState('د. طارق مصطفى المنصوري');
  const [companyName] = useState('الشركة الليبية لتقنية المعلومات والاتصالات');
  const [jobTitle] = useState('الرئيس التنفيذي للابتكار الرقمي');
  const [exhibitionName] = useState('معرض طرابلس الدولي للتقنية 2026');
  const [boothLocation] = useState('القاعة الكبرى | جناح VIP-01');
  const [qrPayload] = useState('EXPO-2026-VIP-9941');

  const handlePrint = () => {
    window.print();
  };

  const handleSave = () => {
    showToast('المعاينة محلية حالياً؛ لم يُحفظ قالب الشارة في الخادم.', 'warning');
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <IdCard size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              مصمم ومخصص شارات وبطاقات الدخول
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            تصميم وتخصيص قوالب بطاقات الدخول للزوار والعارضين وطباعتها بأعلى دقة
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <button
            onClick={handlePrint}
            className="btn-secondary"
            style={{ height: '42px', padding: '0 18px' }}
          >
            <Printer size={16} />
            <span>طباعة تجريبية</span>
          </button>

          <button
            onClick={handleSave}
            className="btn-primary"
            style={{ height: '42px', padding: '0 20px' }}
          >
            <Save size={16} />
            <span>حفظ القالب</span>
          </button>
        </div>
      </div>

      {/* Main Split Layout: Left Controls + Right Live Preview */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 440px',
          gap: '28px',
          alignItems: 'start'
        }}
      >
        {/* Controls Configuration Panel */}
        <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div style={{ borderBottom: '1px solid var(--border-main)', paddingBottom: '12px' }}>
            <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>
              إعدادات وتخصيص القالب
            </h3>
            <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
              تحكم في ألوان النصوص والشعارات والعناصر المعروضة على البطاقة
            </p>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم القالب</label>
              <input
                type="text"
                value={templateName}
                onChange={(e) => setTemplateName(e.target.value)}
                className="input-field"
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">فئة المشارك المستهدفة</label>
              <select
                value={participantType}
                onChange={(e) => {
                  setParticipantType(e.target.value);
                  if (e.target.value === 'VIP') {
                    setHeaderColor('#6366f1');
                    setCategoryText('شخصية هامة / VIP GUEST');
                  } else if (e.target.value === 'Exhibitor') {
                    setHeaderColor('#3b82f6');
                    setCategoryText('عارض مشارك / EXHIBITOR');
                  } else if (e.target.value === 'Speaker') {
                    setHeaderColor('#8b5cf6');
                    setCategoryText('متحدث رسمي / SPEAKER');
                  } else {
                    setHeaderColor('#10b981');
                    setCategoryText('زائر معتمد / VISITOR');
                  }
                }}
                className="input-field"
              >
                <option value="VIP">شخصية هامة (VIP)</option>
                <option value="Exhibitor">عارض معتمد (Exhibitor)</option>
                <option value="Speaker">متحدث (Speaker)</option>
                <option value="Visitor">زائر عادي (Visitor)</option>
                <option value="Press">صحافة وإعلام (Press)</option>
                <option value="Organizer">لجنة التنظيم (Organizer)</option>
              </select>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اتجاه البطاقة</label>
              <div style={{ display: 'flex', gap: '8px' }}>
                <button
                  type="button"
                  onClick={() => setOrientation('Vertical')}
                  className={orientation === 'Vertical' ? 'btn-primary' : 'btn-secondary'}
                  style={{ flex: 1, padding: '8px', fontSize: '13px' }}
                >
                  عمودي (86×120 مم)
                </button>
                <button
                  type="button"
                  onClick={() => setOrientation('Horizontal')}
                  className={orientation === 'Horizontal' ? 'btn-primary' : 'btn-secondary'}
                  style={{ flex: 1, padding: '8px', fontSize: '13px' }}
                >
                  أفقي (120×86 مم)
                </button>
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">لون الشريط العلوي</label>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <input
                  type="color"
                  value={headerColor}
                  onChange={(e) => setHeaderColor(e.target.value)}
                  style={{ width: '44px', height: '44px', borderRadius: '8px', border: '1px solid var(--border-main)', cursor: 'pointer' }}
                />
                <input
                  type="text"
                  value={headerColor}
                  onChange={(e) => setHeaderColor(e.target.value)}
                  className="input-field"
                  style={{ flex: 1 }}
                />
              </div>
            </div>
          </div>

          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">نص شارة الفئة (Category Badge)</label>
            <input
              type="text"
              value={categoryText}
              onChange={(e) => setCategoryText(e.target.value)}
              className="input-field"
            />
          </div>

          {/* Toggle Switches for Fields */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', borderTop: '1px solid var(--border-main)', paddingTop: '16px' }}>
            <label style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-primary)' }}>
              العناصر المرئية على الشارة:
            </label>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={showLogo}
                  onChange={(e) => setShowLogo(e.target.checked)}
                  style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
                />
                <span>شعار المعرض والمنظومة</span>
              </label>

              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={showQR}
                  onChange={(e) => setShowQR(e.target.checked)}
                  style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
                />
                <span>رمز الاستجابة السريعة (QR Code)</span>
              </label>

              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={showCompany}
                  onChange={(e) => setShowCompany(e.target.checked)}
                  style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
                />
                <span>اسم الشركة أو المؤسسة</span>
              </label>

              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={showJobTitle}
                  onChange={(e) => setShowJobTitle(e.target.checked)}
                  style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
                />
                <span>المسمى والمنصب الوظيفي</span>
              </label>

              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={showHall}
                  onChange={(e) => setShowHall(e.target.checked)}
                  style={{ accentColor: 'var(--primary)', width: '16px', height: '16px' }}
                />
                <span>القاعة المخصصة وموقع الجناح</span>
              </label>
            </div>
          </div>
        </div>

        {/* Live Attendee Badge Preview Container */}
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '16px' }}>
          <div style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-secondary)' }}>
            معاينة حية دقيقة للشارة المطبوعة
          </div>

          {/* Physical Badge Render Card */}
          <div
            id="printable-badge"
            style={{
              width: orientation === 'Vertical' ? '320px' : '420px',
              minHeight: orientation === 'Vertical' ? '460px' : '300px',
              backgroundColor: '#ffffff',
              borderRadius: '16px',
              boxShadow: '0 20px 40px -10px rgba(0, 0, 0, 0.15)',
              border: '1px solid #cbd5e1',
              display: 'flex',
              flexDirection: 'column',
              overflow: 'hidden',
              position: 'relative',
              textAlign: 'center'
            }}
          >
            {/* Lanyard Hole Mockup */}
            <div
              style={{
                position: 'absolute',
                top: '8px',
                left: '50%',
                transform: 'translateX(-50%)',
                width: '36px',
                height: '6px',
                backgroundColor: '#cbd5e1',
                borderRadius: '3px'
              }}
            />

            {/* Badge Header with Custom Color */}
            <div
              style={{
                backgroundColor: headerColor,
                padding: '28px 16px 16px',
                color: headerTextColor,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: '6px'
              }}
            >
              {showLogo && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', fontWeight: 800 }}>
                  <Building2 size={16} />
                  <span>ExpoManager</span>
                </div>
              )}

              <div style={{ fontSize: '13px', fontWeight: 700, opacity: 0.95 }}>
                {exhibitionName}
              </div>

              {/* Category Pill Tag */}
              <div
                style={{
                  marginTop: '4px',
                  backgroundColor: 'rgba(255, 255, 255, 0.25)',
                  backdropFilter: 'blur(4px)',
                  padding: '3px 14px',
                  borderRadius: '12px',
                  fontSize: '11px',
                  fontWeight: 800,
                  letterSpacing: '0.5px'
                }}
              >
                {categoryText}
              </div>
            </div>

            {/* Attendee Details Body */}
            <div
              style={{
                padding: '24px 20px',
                flex: 1,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: '14px'
              }}
            >
              <div>
                <h2 style={{ fontSize: '20px', fontWeight: 800, color: 'var(--text-primary)', marginBottom: '4px' }}>
                  {attendeeName}
                </h2>

                {showJobTitle && (
                  <div style={{ fontSize: '13px', fontWeight: 600, color: 'var(--primary)', marginBottom: '2px' }}>
                    {jobTitle}
                  </div>
                )}

                {showCompany && (
                  <div style={{ fontSize: '12px', color: 'var(--text-secondary)', fontWeight: 500 }}>
                    {companyName}
                  </div>
                )}
              </div>

              {showHall && (
                <div
                  style={{
                    backgroundColor: 'var(--bg-secondary)',
                    padding: '6px 14px',
                    borderRadius: '8px',
                    fontSize: '11px',
                    fontWeight: 600,
                    color: 'var(--text-secondary)'
                  }}
                >
                  {boothLocation}
                </div>
              )}

              {/* QR Code */}
              {showQR && (
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '6px' }}>
                  <div
                    style={{
                      padding: '8px',
                      backgroundColor: '#ffffff',
                      borderRadius: '12px',
                      border: '1px solid var(--border-main)',
                      boxShadow: 'var(--shadow-sm)'
                    }}
                  >
                    <QRCodeSVG value={qrPayload} size={90} />
                  </div>
                  <span style={{ fontSize: '10px', color: 'var(--text-muted)', fontFamily: 'monospace' }}>
                    {qrPayload}
                  </span>
                </div>
              )}

              {/* Footer text */}
              <div style={{ fontSize: '9px', color: 'var(--text-muted)', borderTop: '1px solid #f1f5f9', paddingTop: '8px', width: '100%' }}>
                {footerText}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
