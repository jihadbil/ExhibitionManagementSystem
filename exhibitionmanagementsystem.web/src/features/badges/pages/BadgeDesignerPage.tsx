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
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getAllItems } from '../../../services/pagination';
import { BadgeCard } from '../components/BadgeCard';

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
  targetParticipantType: number; // 1=Visitor, 2=Exhibitor, 3=Speaker, 4=VIP
  widthMm: number;
  heightMm: number;
  orientation: number; // 1=Vertical, 2=Horizontal
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
  1: 'زائر عادي (Visitor)',
  2: 'عارض مشارك (Exhibitor)',
  3: 'متحدث / محاضر (Speaker)',
  4: 'شخصية هامة (VIP)',
  5: 'صحافة وإعلام (Press)',
  6: 'فريق التنظيم (Organizer)',
  7: 'راعٍ (Sponsor)',
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
  const [participantType, setParticipantType] = useState<number>(1);
  const [orientation, setOrientation] = useState<'Vertical' | 'Horizontal'>('Vertical');
  const [headerColor, setHeaderColor] = useState('#6366f1');
  const [headerTextColor, setHeaderTextColor] = useState('#ffffff');
  const [accentColor, setAccentColor] = useState('#4f46e5');
  const [categoryText, setCategoryText] = useState('زائر / VISITOR');
  const [showLogo, setShowLogo] = useState(true);
  const [showQR, setShowQR] = useState(true);
  const [showCompany, setShowCompany] = useState(true);
  const [showJobTitle, setShowJobTitle] = useState(true);
  const [showHall, setShowHall] = useState(true);
  const [footerText, setFooterText] = useState('ExpoManager — بطاقة دخول رقمية معتمدة');

  // Sample Attendee Data for Live Preview (Editable by user)
  const [attendeeName, setAttendeeName] = useState('د. طارق مصطفى المنصوري');
  const [companyName, setCompanyName] = useState('الشركة الليبية لتقنية المعلومات والاتصالات');
  const [jobTitle, setJobTitle] = useState('الرئيس التنفيذي للابتكار الرقمي');
  const [boothLocation, setBoothLocation] = useState('القاعة الكبرى | جناح VIP-01');
  const qrPayload = 'EXPO-2026-VISITOR-9941';

  const currentExhibitionName = exhibitions.find(e => e.exhibitionID.toString() === selectedExhibitionId)?.name || 'معرض طرابلس الدولي للتقنية 2026';

  const applyTemplate = useCallback((template: BadgeTemplate) => {
    setEditingTemplateId(template.templateID);
    setTemplateName(template.templateName);
    setParticipantType(template.targetParticipantType);
    setOrientation(template.orientation === 2 ? 'Horizontal' : 'Vertical');
    setHeaderColor(template.headerBgColor || '#6366f1');
    setHeaderTextColor(template.headerTextColor || '#ffffff');
    setAccentColor(template.accentColor || '#4f46e5');
    setCategoryText(template.categoryBadgeText || 'زائر معتمد');
    setShowLogo(template.showLogo);
    setShowQR(template.showQRCode);
    setShowCompany(template.showCompanyName);
    setShowJobTitle(template.showJobTitle);
    setShowHall(template.showHallName);
    setFooterText(template.customFooterText || 'ExpoManager — بطاقة دخول رقمية معتمدة');
  }, []);

  const resetToNew = useCallback(() => {
    setEditingTemplateId(null);
    setTemplateName('قالب شارة جديد');
    setParticipantType(1);
    setOrientation('Vertical');
    setHeaderColor('#6366f1');
    setHeaderTextColor('#ffffff');
    setAccentColor('#4f46e5');
    setCategoryText('زائر / VISITOR');
    setShowLogo(true);
    setShowQR(true);
    setShowCompany(true);
    setShowJobTitle(true);
    setShowHall(true);
    setFooterText('ExpoManager — بطاقة دخول رقمية معتمدة');
  }, []);

  // 1. Load Exhibitions
  useEffect(() => {
    const loadExhibitions = async () => {
      try {
        const list = await getAllItems<Exhibition>('/api/exhibitions');
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
        const selectedId = Number(exId);
        const preferredTemplate = data.find(template =>
          template.exhibitionID === selectedId &&
          template.targetParticipantType === 1 &&
          template.isDefault
        ) ?? data.find(template => template.exhibitionID === selectedId && template.isDefault)
          ?? data.find(template => template.isDefault)
          ?? data[0];
        applyTemplate(preferredTemplate);
      } else {
        resetToNew();
      }
    } catch {
      setTemplates([]);
      resetToNew();
      showToast('تعذر تحميل قوالب الشارات لهذا المعرض.', 'danger');
    }
  }, [applyTemplate, resetToNew, showToast]);

  useEffect(() => {
    if (selectedExhibitionId) {
      void loadTemplates(selectedExhibitionId);
    }
  }, [selectedExhibitionId, loadTemplates]);

  const handlePrint = () => {
    window.print();
  };

  const handleSave = async () => {
    if (!templateName.trim()) {
      showToast('يرجى إدخال اسم القالب.', 'warning');
      return;
    }
    if (!selectedExhibitionId) {
      showToast('يرجى اختيار معرض قبل حفظ القالب.', 'warning');
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
        orientation: orientation === 'Horizontal' ? 2 : 1,
        headerBgColor: headerColor,
        headerTextColor,
        accentColor,
        categoryBadgeText: categoryText.trim(),
        showLogo,
        showQRCode: showQR,
        showBarcode: true,
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
        className="badge-designer-grid"
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

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }}>
            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label" style={{ fontSize: '12px' }}>خلفية الشريط</label>
              <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                <input
                  type="color"
                  value={headerColor}
                  onChange={(e) => setHeaderColor(e.target.value)}
                  style={{ width: '38px', height: '38px', padding: 0, border: '1px solid var(--border-main)', borderRadius: '6px', cursor: 'pointer' }}
                />
                <input
                  type="text"
                  value={headerColor}
                  onChange={(e) => setHeaderColor(e.target.value)}
                  className="input-field"
                  style={{ flex: 1, padding: '6px 8px', fontSize: '12px' }}
                />
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label" style={{ fontSize: '12px' }}>لون نص الشريط</label>
              <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                <input
                  type="color"
                  value={headerTextColor}
                  onChange={(e) => setHeaderTextColor(e.target.value)}
                  style={{ width: '38px', height: '38px', padding: 0, border: '1px solid var(--border-main)', borderRadius: '6px', cursor: 'pointer' }}
                />
                <input
                  type="text"
                  value={headerTextColor}
                  onChange={(e) => setHeaderTextColor(e.target.value)}
                  className="input-field"
                  style={{ flex: 1, padding: '6px 8px', fontSize: '12px' }}
                />
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label" style={{ fontSize: '12px' }}>لون شريط التمييز</label>
              <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                <input
                  type="color"
                  value={accentColor}
                  onChange={(e) => setAccentColor(e.target.value)}
                  style={{ width: '38px', height: '38px', padding: 0, border: '1px solid var(--border-main)', borderRadius: '6px', cursor: 'pointer' }}
                />
                <input
                  type="text"
                  value={accentColor}
                  onChange={(e) => setAccentColor(e.target.value)}
                  className="input-field"
                  style={{ flex: 1, padding: '6px 8px', fontSize: '12px' }}
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
          <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', marginTop: '4px' }}>
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

          {/* Sample Attendee Preview Customization */}
          <div style={{ borderTop: '1px solid var(--border-main)', paddingTop: '14px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-secondary)' }}>بيانات المعاينة التجريبية (Live Sample Data):</span>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px' }}>
              <div>
                <label className="form-label" style={{ fontSize: '11px', marginBottom: '2px' }}>اسم المشارك</label>
                <input
                  type="text"
                  value={attendeeName}
                  onChange={e => setAttendeeName(e.target.value)}
                  className="input-field"
                  style={{ fontSize: '12px', padding: '6px 10px' }}
                />
              </div>
              <div>
                <label className="form-label" style={{ fontSize: '11px', marginBottom: '2px' }}>المسمى الوظيفي</label>
                <input
                  type="text"
                  value={jobTitle}
                  onChange={e => setJobTitle(e.target.value)}
                  className="input-field"
                  style={{ fontSize: '12px', padding: '6px 10px' }}
                />
              </div>
              <div>
                <label className="form-label" style={{ fontSize: '11px', marginBottom: '2px' }}>الشركة / المؤسسة</label>
                <input
                  type="text"
                  value={companyName}
                  onChange={e => setCompanyName(e.target.value)}
                  className="input-field"
                  style={{ fontSize: '12px', padding: '6px 10px' }}
                />
              </div>
              <div>
                <label className="form-label" style={{ fontSize: '11px', marginBottom: '2px' }}>الجناح / الموقع</label>
                <input
                  type="text"
                  value={boothLocation}
                  onChange={e => setBoothLocation(e.target.value)}
                  className="input-field"
                  style={{ fontSize: '12px', padding: '6px 10px' }}
                />
              </div>
            </div>
          </div>
        </div>

        {/* Live Badge Preview Area */}
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '14px', position: 'sticky', top: '24px' }}>
          <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-secondary)' }}>
            المعاينة الحية الفورية (Live Print Preview)
          </span>

          <BadgeCard
            id="printable-badge"
            className="badge-print-target"
            data={{
              fullName: attendeeName,
              companyName,
              jobTitle,
              qrCode: qrPayload,
              barcode: 'TKT-009941',
              exhibitionName: currentExhibitionName,
              venueAndHallName: boothLocation,
            }}
            design={{
              widthMm: orientation === 'Vertical' ? 86 : 120,
              heightMm: orientation === 'Vertical' ? 120 : 86,
              orientation,
              headerBgColor: headerColor,
              headerTextColor,
              accentColor,
              categoryBadgeText: categoryText,
              showLogo,
              showQRCode: showQR,
              showBarcode: true,
              showCompanyName: showCompany,
              showJobTitle,
              showHallName: showHall,
              showDates: true,
              footerText,
            }}
          />
        </div>
      </div>
    </div>
  );
};
