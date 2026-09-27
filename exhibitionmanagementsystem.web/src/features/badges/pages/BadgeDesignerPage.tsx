import React, { useState, useEffect, useCallback } from 'react';
import {
  IdCard,
  Printer,
  Building2,
  Save,
  Plus,
  Trash2,
  LayoutTemplate
} from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface Exhibition {
  exhibitionID: number;
  name: string;
}

interface BadgeTemplate {
  templateID: number;
  tenantID: number;
  exhibitionID?: number;
  exhibitionName?: string;
  templateName: string;
  targetParticipantType: number; // 0=Visitor, 1=Exhibitor, 2=VIP, 3=Speaker
  widthMm: number;
  heightMm: number;
  orientation: number; // 0=Vertical, 1=Horizontal
  headerBgColor: string;
  headerTextColor: string;
  accentColor: string;
  categoryBadgeText: string;
  showLogo: boolean;
  logoUrl?: string;
  showQRCode: boolean;
  showBarcode: boolean;
  showCompanyName: boolean;
  showJobTitle: boolean;
  showHallName: boolean;
  showDates: boolean;
  customFooterText?: string;
  isDefault: boolean;
}

const participantTypeMap: Record<number, string> = {
  0: 'زائر عادي (Visitor)',
  1: 'عارض مشارك (Exhibitor)',
  2: 'شخصية هامة (VIP)',
  3: 'متحدث / محاضر (Speaker)',
  4: 'فريق التنظيم (Organizer)',
  5: 'صحافة وإعلام (Press)',
};

export const BadgeDesignerPage: React.FC = () => {
  const { showToast } = useToast();

  // Exhibitions & Templates State
  const [exhibitions, setExhibitions] = useState<Exhibition[]>([]);
  const [selectedExhibitionId, setSelectedExhibitionId] = useState<string>('');
  const [templates, setTemplates] = useState<BadgeTemplate[]>([]);
  const [editingTemplateId, setEditingTemplateId] = useState<number | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  // Template Form State
  const [templateName, setTemplateName] = useState('شارة الحضور المعتمدة');
  const [participantType, setParticipantType] = useState<number>(2); // VIP
  const [orientation, setOrientation] = useState<'Vertical' | 'Horizontal'>('Vertical');
  const [headerColor, setHeaderColor] = useState('#6366f1');
  const [headerTextColor] = useState('#ffffff');
  const [accentColor, setAccentColor] = useState('#4f46e5');
  const [categoryText, setCategoryText] = useState('شخصية هامة / VIP GUEST');
  const [showLogo, setShowLogo] = useState(true);
  const [showQR, setShowQR] = useState(true);
  const [showCompany, setShowCompany] = useState(true);
  const [showJobTitle, setShowJobTitle] = useState(true);
  const [showHall, setShowHall] = useState(true);
  const [footerText, setFooterText] = useState('ExpoManager — بطاقة دخول رقمية معتمدة');

  // Sample Attendee Data for Live Preview
  const [attendeeName] = useState('د. طارق مصطفى المنصوري');
  const [companyName] = useState('الشركة الليبية لتقنية المعلومات والاتصالات');
  const [jobTitle] = useState('الرئيس التنفيذي للابتكار الرقمي');
  const [sampleExhibitionName] = useState('معرض طرابلس الدولي للتقنية 2026');
  const [boothLocation] = useState('القاعة الكبرى | جناح VIP-01');
  const [qrPayload] = useState('EXPO-2026-VIP-9941');

  // 1. Load Exhibitions
  useEffect(() => {
    const loadExhibitions = async () => {
      try {
        const response = await api.get<{ items: Exhibition[] }>('/api/exhibitions?page=1&pageSize=50');
        const list = response.data.items ?? [];
        setExhibitions(list);
        if (list.length > 0) {
          setSelectedExhibitionId(list[0].exhibitionID.toString());
        }
      } catch {
        showToast('تعذر تحميل المعارض لقوالب الشارات.', 'danger');
      }
    };
    void loadExhibitions();
  }, [showToast]);

  // 2. Load Templates for selected exhibition
  const loadTemplates = useCallback(async (exId: string) => {
    try {
      const response = await api.get<BadgeTemplate[]>(`/api/badges/templates?exhibitionId=${exId}`);
      const data = response.data ?? [];
      setTemplates(data);
      if (data.length > 0) {
        applyTemplate(data[0]);
      } else {
        resetToNew();
      }
    } catch {
      // Fallback
      setTemplates([]);
    }
  }, []);

  useEffect(() => {
    if (selectedExhibitionId) {
      void loadTemplates(selectedExhibitionId);
    }
  }, [selectedExhibitionId, loadTemplates]);

  const applyTemplate = (t: BadgeTemplate) => {
    setEditingTemplateId(t.templateID);
    setTemplateName(t.templateName);
    setParticipantType(t.targetParticipantType);
    setOrientation(t.orientation === 1 ? 'Horizontal' : 'Vertical');
    setHeaderColor(t.headerBgColor || '#6366f1');
    setAccentColor(t.accentColor || '#4f46e5');
    setCategoryText(t.categoryBadgeText || 'زائر معتمد');
    setShowLogo(t.showLogo);
    setShowQR(t.showQRCode);
    setShowCompany(t.showCompanyName);
    setShowJobTitle(t.showJobTitle);
    setShowHall(t.showHallName);
    setFooterText(t.customFooterText || 'ExpoManager — بطاقة دخول رقمية معتمدة');
  };

  const resetToNew = () => {
    setEditingTemplateId(null);
    setTemplateName('قالب شارة جديد');
    setParticipantType(0);
    setOrientation('Vertical');
    setHeaderColor('#6366f1');
    setCategoryText('زائر / VISITOR');
    setShowLogo(true);
    setShowQR(true);
    setShowCompany(true);
    setShowJobTitle(true);
    setShowHall(true);
    setFooterText('ExpoManager — بطاقة دخول رقمية معتمدة');
  };

  const handlePrint = () => {
    window.print();
  };

  const handleSave = async () => {
    if (!templateName.trim()) {
      showToast('يرجى إدخال اسم القالب.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      const payload = {
        exhibitionID: selectedExhibitionId ? Number(selectedExhibitionId) : null,
        templateName: templateName.trim(),
        targetParticipantType: participantType,
        widthMm: orientation === 'Vertical' ? 86 : 120,
        heightMm: orientation === 'Vertical' ? 120 : 86,
        orientation: orientation === 'Horizontal' ? 1 : 0,
        headerBgColor: headerColor,
        headerTextColor,
        accentColor,
        categoryBadgeText: categoryText.trim(),
        showLogo,
        showQRCode: showQR,
        showBarcode: false,
        showCompanyName: showCompany,
        showJobTitle,
        showHallName: showHall,
        showDates: true,
        customFooterText: footerText.trim(),
        isDefault: true,
      };

      if (editingTemplateId) {
        await api.put(`/api/badges/templates/${editingTemplateId}`, payload);
        showToast('تم تحديث قالب الشارة في الخادم بنجاح.', 'success');
      } else {
        const res = await api.post<BadgeTemplate>('/api/badges/templates', payload);
        setEditingTemplateId(res.data.templateID);
        showToast('تم حفظ قالب الشارة الجديد في الخادم.', 'success');
      }

      if (selectedExhibitionId) {
        await loadTemplates(selectedExhibitionId);
      }
    } catch {
      showToast('فشل حفظ القالب في الخادم. تحقق من البيانات.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  const handleDeleteTemplate = async () => {
    if (!editingTemplateId) return;
    if (!window.confirm('هل أنت متأكد من حذف هذا القالب؟')) return;

    try {
      await api.delete(`/api/badges/templates/${editingTemplateId}`);
      showToast('تم حذف قالب الشارة بنجاح.', 'success');
      if (selectedExhibitionId) {
        await loadTemplates(selectedExhibitionId);
      }
    } catch {
      showToast('تعذر حذف القالب من الخادم.', 'danger');
    }
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
            تصميم وتخصيص قوالب بطاقات الدخول للزوار والعارضين وحفظها بالخادم وطباعتها.
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
          {/* Exhibition Selector */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <Building2 size={16} style={{ color: 'var(--primary)' }} />
            <select
              className="input-field"
              style={{ width: '220px', height: '40px', padding: '0 10px', fontSize: '13px' }}
              value={selectedExhibitionId}
              onChange={e => setSelectedExhibitionId(e.target.value)}
            >
              {exhibitions.map(ex => (
                <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>{ex.name}</option>
              ))}
            </select>
          </div>

          <button
            onClick={resetToNew}
            className="btn-secondary"
            style={{ height: '40px', padding: '0 14px' }}
          >
            <Plus size={16} />
            <span>قالب جديد</span>
          </button>

          <button
            onClick={handlePrint}
            className="btn-secondary"
            style={{ height: '40px', padding: '0 16px' }}
          >
            <Printer size={16} />
            <span>طباعة تجريبية</span>
          </button>

          <button
            onClick={handleSave}
            className="btn-primary"
            style={{ height: '40px', padding: '0 20px' }}
            disabled={isSaving}
          >
            <Save size={16} />
            <span>{isSaving ? 'جاري الحفظ…' : editingTemplateId ? 'تحديث القالب' : 'حفظ القالب'}</span>
          </button>
        </div>
      </div>

      {/* Templates Selector Pill Bar */}
      {templates.length > 0 && (
        <div className="card" style={{ padding: '12px 18px', display: 'flex', alignItems: 'center', gap: '12px', overflowX: 'auto' }}>
          <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-secondary)', whiteSpace: 'nowrap', display: 'flex', alignItems: 'center', gap: '6px' }}>
            <LayoutTemplate size={16} style={{ color: 'var(--primary)' }} /> القوالب المحفوظة:
          </span>
          <div style={{ display: 'flex', gap: '8px' }}>
            {templates.map(t => (
              <button
                key={t.templateID}
                onClick={() => applyTemplate(t)}
                className={editingTemplateId === t.templateID ? 'btn-primary' : 'btn-secondary'}
                style={{ height: '32px', padding: '0 12px', fontSize: '12px' }}
              >
                {t.templateName}
              </button>
            ))}
          </div>
          {editingTemplateId && (
            <button
              onClick={handleDeleteTemplate}
              className="btn-icon"
              title="حذف هذا القالب"
              style={{ color: 'var(--danger)', marginRight: 'auto' }}
            >
              <Trash2 size={16} />
            </button>
          )}
        </div>
      )}

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

          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">اسم القالب</label>
            <input
              type="text"
              value={templateName}
              onChange={(e) => setTemplateName(e.target.value)}
              className="input-field"
              placeholder="مثال: شارة كبار الزوار VIP"
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">فئة المشارك المستهدفة</label>
              <select
                value={participantType}
                onChange={(e) => setParticipantType(Number(e.target.value))}
                className="input-field"
              >
                {Object.entries(participantTypeMap).map(([val, label]) => (
                  <option key={val} value={val}>{label}</option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اتجاه البطاقة</label>
              <select
                value={orientation}
                onChange={(e) => setOrientation(e.target.value as 'Vertical' | 'Horizontal')}
                className="input-field"
              >
                <option value="Vertical">عمودي (Vertical - 86×120 مم)</option>
                <option value="Horizontal">أفقي (Horizontal - 120×86 مم)</option>
              </select>
            </div>
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">لون الشريط العلوي</label>
              <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                <input
                  type="color"
                  value={headerColor}
                  onChange={(e) => setHeaderColor(e.target.value)}
                  style={{ width: '42px', height: '40px', padding: 0, border: '1px solid var(--border-main)', borderRadius: '6px', cursor: 'pointer' }}
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

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">لون الزخرفة / التمييز</label>
              <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                <input
                  type="color"
                  value={accentColor}
                  onChange={(e) => setAccentColor(e.target.value)}
                  style={{ width: '42px', height: '40px', padding: 0, border: '1px solid var(--border-main)', borderRadius: '6px', cursor: 'pointer' }}
                />
                <input
                  type="text"
                  value={accentColor}
                  onChange={(e) => setAccentColor(e.target.value)}
                  className="input-field"
                  style={{ flex: 1 }}
                />
              </div>
            </div>
          </div>

          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">نص شارة التصنيف (Badge Label)</label>
            <input
              type="text"
              value={categoryText}
              onChange={(e) => setCategoryText(e.target.value)}
              className="input-field"
              placeholder="مثال: ضيف شرف / VIP GUEST"
            />
          </div>

          <div className="form-group" style={{ margin: 0 }}>
            <label className="form-label">نص التذييل والتحقق</label>
            <input
              type="text"
              value={footerText}
              onChange={(e) => setFooterText(e.target.value)}
              className="input-field"
              placeholder="ExpoManager — بطاقة دخول معتمدة"
            />
          </div>

          {/* Visibility Checkboxes */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', marginTop: '6px' }}>
            <label className="form-label" style={{ marginBottom: '2px' }}>العناصر المعروضة على الشارة</label>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input type="checkbox" checked={showLogo} onChange={e => setShowLogo(e.target.checked)} />
                <span>شعار المعرض / المنظومة</span>
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input type="checkbox" checked={showQR} onChange={e => setShowQR(e.target.checked)} />
                <span>رمز الاستجابة السريعة (QR)</span>
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input type="checkbox" checked={showCompany} onChange={e => setShowCompany(e.target.checked)} />
                <span>اسم المؤسسة / الشركة</span>
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input type="checkbox" checked={showJobTitle} onChange={e => setShowJobTitle(e.target.checked)} />
                <span>المسمى الوظيفي</span>
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
                <input type="checkbox" checked={showHall} onChange={e => setShowHall(e.target.checked)} />
                <span>القاعة والجناح</span>
              </label>
            </div>
          </div>
        </div>

        {/* Live Badge Preview Area */}
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '14px', position: 'sticky', top: '24px' }}>
          <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-secondary)' }}>
            المعاينة الحية الفورية (Live Print Preview)
          </span>

          {/* Badge Physical Frame */}
          <div
            id="printable-badge"
            style={{
              width: orientation === 'Vertical' ? '320px' : '400px',
              minHeight: orientation === 'Vertical' ? '460px' : '300px',
              backgroundColor: '#ffffff',
              borderRadius: '16px',
              overflow: 'hidden',
              boxShadow: 'var(--shadow-xl)',
              border: '1px solid #e2e8f0',
              display: 'flex',
              flexDirection: 'column',
              position: 'relative'
            }}
          >
            {/* Top Lanyard Hole Visual Indicator */}
            <div
              style={{
                position: 'absolute',
                top: '8px',
                left: '50%',
                transform: 'translateX(-50%)',
                width: '36px',
                height: '7px',
                borderRadius: '4px',
                backgroundColor: 'rgba(0, 0, 0, 0.2)',
                zIndex: 20
              }}
            />

            {/* Header Ribbon */}
            <div
              style={{
                backgroundColor: headerColor,
                padding: '24px 18px 16px',
                textAlign: 'center',
                color: headerTextColor,
                position: 'relative'
              }}
            >
              {showLogo && (
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', marginBottom: '6px' }}>
                  <Building2 size={16} />
                  <span style={{ fontSize: '12px', fontWeight: 800, letterSpacing: '0.5px' }}>
                    ExpoManager System
                  </span>
                </div>
              )}
              <h2 style={{ fontSize: '14px', fontWeight: 700, margin: 0, opacity: 0.95 }}>
                {sampleExhibitionName}
              </h2>
            </div>

            {/* Badge Category Ribbon */}
            <div
              style={{
                backgroundColor: accentColor,
                color: '#ffffff',
                padding: '6px 12px',
                textAlign: 'center',
                fontSize: '11px',
                fontWeight: 800,
                letterSpacing: '1px'
              }}
            >
              {categoryText}
            </div>

            {/* Attendee Info Body */}
            <div
              style={{
                padding: '24px 20px',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                gap: '12px',
                textAlign: 'center',
                flex: 1,
                justifyContent: 'center'
              }}
            >
              <div>
                <h3 style={{ fontSize: '18px', fontWeight: 800, color: '#0f172a', margin: 0 }}>
                  {attendeeName}
                </h3>
                {showJobTitle && (
                  <p style={{ fontSize: '12px', fontWeight: 600, color: '#64748b', marginTop: '3px' }}>
                    {jobTitle}
                  </p>
                )}
                {showCompany && (
                  <p style={{ fontSize: '12px', color: '#475569', marginTop: '4px', maxWidth: '280px', lineHeight: 1.3 }}>
                    {companyName}
                  </p>
                )}
              </div>

              {showHall && (
                <div style={{ backgroundColor: '#f1f5f9', padding: '6px 14px', borderRadius: '20px', fontSize: '11px', fontWeight: 700, color: '#334155' }}>
                  {boothLocation}
                </div>
              )}

              {showQR && (
                <div style={{ marginTop: '4px', padding: '8px', backgroundColor: '#ffffff', border: '1px solid #e2e8f0', borderRadius: '10px' }}>
                  <QRCodeSVG value={qrPayload} size={84} level="M" />
                </div>
              )}
            </div>

            {/* Footer */}
            <div
              style={{
                padding: '8px 16px',
                borderTop: '1px dashed #cbd5e1',
                textAlign: 'center',
                fontSize: '10px',
                color: '#64748b',
                backgroundColor: '#f8fafc'
              }}
            >
              {footerText}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
