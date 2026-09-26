import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import {
  Building2,
  Plus,
  Search,
  Filter,
  Calendar,
  MapPin,
  Users,
  Building,
  Store,
  Compass,
  X,
  Edit2,
  Trash2
} from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface Exhibition {
  id: number;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
  venueName: string;
  city: string;
  visitorsCount: number;
  exhibitorsCount: number;
  boothsCount: number;
  status: 'Active' | 'Upcoming' | 'Completed';
  description?: string;
}

const initialExhibitions: Exhibition[] = [
  {
    id: 1,
    name: 'معرض طرابلس الدولي للتقنية والابتكار (TechExpo 2026)',
    type: 'Technical',
    startDate: '2026-10-15',
    endDate: '2026-10-20',
    venueName: 'مركز المعارض الدولي — قاعة الابتكار',
    city: 'طرابلس',
    visitorsCount: 15400,
    exhibitorsCount: 68,
    boothsCount: 60,
    status: 'Active',
    description: 'أكبر ملتقى سنوي لشركات التقنية والاتصالات وحلول الذكاء الاصطناعي والحوسبة السحابية.'
  },
  {
    id: 2,
    name: 'المعرض الليبي الدولي للصحة والمستلزمات الطبية (MedExpo)',
    type: 'Medical',
    startDate: '2026-11-02',
    endDate: '2026-11-06',
    venueName: 'مجمع قاعات المؤتمرات الكبرى',
    city: 'بنغازي',
    visitorsCount: 8900,
    exhibitorsCount: 45,
    boothsCount: 45,
    status: 'Upcoming',
    description: 'ملتقى رائد يجمع كبرى شركات صناعة الأدوية والأجهزة الطبية والمستشفيات التخصصية.'
  },
  {
    id: 3,
    name: 'الملتقى الصناعي والتصنيع المتقدم (Industry Pro)',
    type: 'Industrial',
    startDate: '2026-11-20',
    endDate: '2026-11-25',
    venueName: 'صالة المعارض الصناعية (Hall 2)',
    city: 'مصراتة',
    visitorsCount: 6200,
    exhibitorsCount: 40,
    boothsCount: 50,
    status: 'Upcoming',
    description: 'معرض متخصص في المعدات الثقيلة، خطوط الإنتاج الآلي، والطاقة البديلة.'
  },
  {
    id: 4,
    name: 'معرض التجارة والأغذية والصناعات الاستهلاكية',
    type: 'Commercial',
    startDate: '2026-12-01',
    endDate: '2026-12-05',
    venueName: 'مركز المعارض — الصالة B',
    city: 'طرابلس',
    visitorsCount: 22000,
    exhibitorsCount: 95,
    boothsCount: 80,
    status: 'Upcoming',
    description: 'منصة تسويقية كبرى لقطاعات التجزئة والأغذية وتجهيزات الفنادق والمطاعم.'
  },
  {
    id: 5,
    name: 'المعرض الدولي للتعليم العالي والتدريب وبناء القدرات',
    type: 'Educational',
    startDate: '2026-12-15',
    endDate: '2026-12-18',
    venueName: 'قاعة المؤتمرات الدولية',
    city: 'طرابلس',
    visitorsCount: 11000,
    exhibitorsCount: 35,
    boothsCount: 35,
    status: 'Upcoming',
    description: 'يستضيف أكثر من 30 جامعة وأكاديمية دولية لتقديم البرامج الدراسية والمنح التدريبية.'
  },
  {
    id: 6,
    name: 'معرض السيارات والنقل والخدمات اللوجستية',
    type: 'Automotive',
    startDate: '2026-12-22',
    endDate: '2026-12-28',
    venueName: 'صالة المعارض الكبرى والساحة الخارجية',
    city: 'طرابلس',
    visitorsCount: 18500,
    exhibitorsCount: 52,
    boothsCount: 70,
    status: 'Upcoming',
    description: 'استعراض أحدث موديلات السيارات والشاحنات التجارية ومعدات النقل والخدمات اللوجستية.'
  }
];

export const ExhibitionsPage: React.FC = () => {
  const { showToast } = useToast();
  const [exhibitions, setExhibitions] = useState<Exhibition[]>(initialExhibitions);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState('All');
  const [isModalOpen, setIsModalOpen] = useState(false);

  // Form state for creating exhibition
  const [formName, setFormName] = useState('');
  const [formType, setFormType] = useState('Technical');
  const [formVenue, setFormVenue] = useState('مركز المعارض الدولي');
  const [formCity, setFormCity] = useState('طرابلس');
  const [formStartDate, setFormStartDate] = useState('');
  const [formEndDate, setFormEndDate] = useState('');
  const [formBooths, setFormBooths] = useState('50');

  const getGradientByType = (type: string) => {
    switch (type.toLowerCase()) {
      case 'technical': return 'var(--tech-gradient)';
      case 'medical': return 'var(--medical-gradient)';
      case 'industrial': return 'var(--industrial-gradient)';
      case 'commercial': return 'var(--commercial-gradient)';
      case 'educational': return 'var(--educational-gradient)';
      case 'automotive': return 'var(--automotive-gradient)';
      default: return 'var(--primary-gradient)';
    }
  };

  const getTypeNameArabic = (type: string) => {
    switch (type.toLowerCase()) {
      case 'technical': return 'معرض تقني';
      case 'medical': return 'معرض طبي';
      case 'industrial': return 'معرض صناعي';
      case 'commercial': return 'معرض تجاري';
      case 'educational': return 'معرض تعليمي';
      case 'automotive': return 'معرض سيارات';
      default: return type;
    }
  };

  const filteredExhibitions = exhibitions.filter((ex) => {
    const matchesSearch = ex.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          ex.city.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          ex.venueName.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesType = selectedType === 'All' || ex.type.toLowerCase() === selectedType.toLowerCase();
    return matchesSearch && matchesType;
  });

  const handleCreateExhibition = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formName || !formStartDate || !formEndDate) {
      showToast('يرجى ملء جميع الحقول الإلزامية', 'warning');
      return;
    }

    const newEx: Exhibition = {
      id: Date.now(),
      name: formName,
      type: formType,
      startDate: formStartDate,
      endDate: formEndDate,
      venueName: formVenue,
      city: formCity,
      visitorsCount: 0,
      exhibitorsCount: 0,
      boothsCount: parseInt(formBooths, 10) || 40,
      status: 'Upcoming',
      description: 'معرض جديد تم إنشاؤه حديثاً عبر المنظومة.'
    };

    setExhibitions([newEx, ...exhibitions]);
    setIsModalOpen(false);
    showToast('تمت إضافة المعرض بنجاح!', 'success');

    // Reset Form
    setFormName('');
    setFormStartDate('');
    setFormEndDate('');
  };

  const handleDeleteExhibition = (id: number) => {
    if (window.confirm('هل أنت متأكد من رغبتك في حذف هذا المعرض؟')) {
      setExhibitions(exhibitions.filter(e => e.id !== id));
      showToast('تم حذف المعرض بنجاح', 'info');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      {/* Top Action Bar */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
            إدارة المعارض والفعاليات
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
            استعراض وتعديل وإنشاء المعارض وتخصيص الأجنحة والخدمات
          </p>
        </div>

        <button
          onClick={() => setIsModalOpen(true)}
          className="btn-primary"
          style={{ height: '44px', padding: '0 24px' }}
        >
          <Plus size={18} />
          <span>إنشاء معرض جديد</span>
        </button>
      </div>

      {/* Filter and Search Bar */}
      <div className="card" style={{ padding: '16px 20px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
          <div style={{ flex: 1, position: 'relative', minWidth: '280px' }}>
            <Search
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
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="ابحث بالاسم، المدينة، أو مركز المعارض..."
              className="input-field"
              style={{ paddingRight: '42px', height: '42px' }}
            />
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-secondary)', fontSize: '13px', fontWeight: 600 }}>
            <Filter size={16} />
            <span>التصنيف:</span>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', overflowX: 'auto', paddingBottom: '4px' }}>
            {['All', 'Technical', 'Medical', 'Industrial', 'Commercial', 'Educational', 'Automotive'].map((t) => (
              <button
                key={t}
                onClick={() => setSelectedType(t)}
                style={{
                  padding: '6px 14px',
                  borderRadius: 'var(--radius-full)',
                  fontSize: '12px',
                  fontWeight: 600,
                  whiteSpace: 'nowrap',
                  background: selectedType === t ? 'var(--primary-gradient)' : 'var(--bg-secondary)',
                  color: selectedType === t ? '#ffffff' : 'var(--text-secondary)',
                  boxShadow: selectedType === t ? 'var(--primary-glow)' : 'none',
                  transition: 'all var(--transition-normal)'
                }}
              >
                {t === 'All' ? 'جميع المعارض' : getTypeNameArabic(t)}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* 3-Column Exhibition Cards Grid (SCREEN_10 from DESIGN.md) */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))',
          gap: '24px'
        }}
      >
        {filteredExhibitions.map((ex) => {
          const gradient = getGradientByType(ex.type);

          return (
            <div
              key={ex.id}
              className="card"
              style={{
                padding: 0,
                display: 'flex',
                flexDirection: 'column',
                overflow: 'hidden',
                position: 'relative'
              }}
            >
              {/* Header: 128px height with Solid/Themed Gradient */}
              <div
                style={{
                  height: '128px',
                  background: gradient,
                  padding: '16px 20px',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between',
                  position: 'relative',
                  color: '#ffffff'
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <span
                    style={{
                      background: 'rgba(255, 255, 255, 0.25)',
                      backdropFilter: 'blur(8px)',
                      padding: '4px 12px',
                      borderRadius: 'var(--radius-full)',
                      fontSize: '11px',
                      fontWeight: 700,
                      letterSpacing: '0.3px'
                    }}
                  >
                    {getTypeNameArabic(ex.type)}
                  </span>

                  <span
                    style={{
                      background: ex.status === 'Active' ? '#10b981' : '#ffffff',
                      color: ex.status === 'Active' ? '#ffffff' : '#1e293b',
                      padding: '4px 10px',
                      borderRadius: 'var(--radius-full)',
                      fontSize: '11px',
                      fontWeight: 700
                    }}
                  >
                    {ex.status === 'Active' ? 'نشط حالياً' : 'قريباً'}
                  </span>
                </div>

                <h3
                  style={{
                    color: '#ffffff',
                    fontSize: '17px',
                    fontWeight: 800,
                    lineHeight: 1.3,
                    textShadow: '0 1px 3px rgba(0,0,0,0.15)'
                  }}
                >
                  {ex.name}
                </h3>
              </div>

              {/* Content Body */}
              <div style={{ padding: '20px', flex: 1, display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', fontSize: '13px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-secondary)' }}>
                    <Calendar size={15} style={{ color: 'var(--primary)', flexShrink: 0 }} />
                    <span>من {ex.startDate} إلى {ex.endDate}</span>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--text-secondary)' }}>
                    <MapPin size={15} style={{ color: 'var(--primary)', flexShrink: 0 }} />
                    <span>{ex.venueName} — {ex.city}</span>
                  </div>
                </div>

                {/* 3 Small Stat Tiles (Visitors, Exhibitors, Booths) */}
                <div
                  style={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(3, 1fr)',
                    gap: '8px',
                    padding: '12px',
                    backgroundColor: 'var(--bg-secondary)',
                    borderRadius: 'var(--radius-lg)',
                    textAlign: 'center'
                  }}
                >
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '4px', color: 'var(--text-muted)', fontSize: '11px', marginBottom: '2px' }}>
                      <Users size={12} />
                      <span>الزوار</span>
                    </div>
                    <div style={{ fontWeight: 800, fontSize: '14px', color: 'var(--text-primary)' }}>
                      {ex.visitorsCount > 0 ? ex.visitorsCount.toLocaleString() : '—'}
                    </div>
                  </div>

                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '4px', color: 'var(--text-muted)', fontSize: '11px', marginBottom: '2px' }}>
                      <Building size={12} />
                      <span>العارضون</span>
                    </div>
                    <div style={{ fontWeight: 800, fontSize: '14px', color: 'var(--text-primary)' }}>
                      {ex.exhibitorsCount}
                    </div>
                  </div>

                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '4px', color: 'var(--text-muted)', fontSize: '11px', marginBottom: '2px' }}>
                      <Store size={12} />
                      <span>الأجنحة</span>
                    </div>
                    <div style={{ fontWeight: 800, fontSize: '14px', color: 'var(--text-primary)' }}>
                      {ex.boothsCount}
                    </div>
                  </div>
                </div>

                {/* Action Buttons Row */}
                <div
                  style={{
                    marginTop: 'auto',
                    paddingTop: '14px',
                    borderTop: '1px solid var(--border-main)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: '8px'
                  }}
                >
                  <NavLink
                    to={`/booths/designer?exhibitionId=${ex.id}`}
                    className="btn-primary"
                    style={{
                      flex: 1,
                      padding: '8px 12px',
                      fontSize: '12px'
                    }}
                  >
                    <Compass size={15} />
                    <span>مخطط الأجنحة 2D</span>
                  </NavLink>

                  <button
                    onClick={() => handleDeleteExhibition(ex.id)}
                    className="btn-icon"
                    title="حذف المعرض"
                    style={{ color: 'var(--danger)' }}
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>

      {/* Create Exhibition Modal */}
      {isModalOpen && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(15, 23, 42, 0.4)',
            backdropFilter: 'blur(4px)',
            zIndex: 100,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: '24px'
          }}
        >
          <div
            className="card"
            style={{
              width: '100%',
              maxWidth: '560px',
              padding: '28px',
              boxShadow: 'var(--shadow-xl)',
              animation: 'slideIn 0.25s ease-out'
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <div
                  style={{
                    width: '36px',
                    height: '36px',
                    borderRadius: 'var(--radius-md)',
                    background: 'var(--primary-light)',
                    color: 'var(--primary)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center'
                  }}
                >
                  <Building2 size={20} />
                </div>
                <h3 style={{ fontSize: '18px', fontWeight: 800, color: 'var(--text-primary)' }}>
                  إضافة وتأسيس معرض جديد
                </h3>
              </div>

              <button
                onClick={() => setIsModalOpen(false)}
                className="btn-icon"
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleCreateExhibition} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">اسم المعرض *</label>
                <input
                  type="text"
                  required
                  value={formName}
                  onChange={(e) => setFormName(e.target.value)}
                  placeholder="مثال: المعرض الدولي للتقنيات والابتكار 2026"
                  className="input-field"
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">نوع وتخصص المعرض *</label>
                  <select
                    value={formType}
                    onChange={(e) => setFormType(e.target.value)}
                    className="input-field"
                    style={{ cursor: 'pointer' }}
                  >
                    <option value="Technical">تقني (Technical)</option>
                    <option value="Medical">طبي (Medical)</option>
                    <option value="Industrial">صناعي (Industrial)</option>
                    <option value="Commercial">تجاري (Commercial)</option>
                    <option value="Educational">تعليمي (Educational)</option>
                    <option value="Automotive">سيارات ونقل (Automotive)</option>
                  </select>
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">إجمالي الأجنحة المخططة</label>
                  <input
                    type="number"
                    value={formBooths}
                    onChange={(e) => setFormBooths(e.target.value)}
                    className="input-field"
                  />
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">مركز المعارض أو الصالة</label>
                  <input
                    type="text"
                    value={formVenue}
                    onChange={(e) => setFormVenue(e.target.value)}
                    className="input-field"
                  />
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">المدينة</label>
                  <input
                    type="text"
                    value={formCity}
                    onChange={(e) => setFormCity(e.target.value)}
                    className="input-field"
                  />
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">تاريخ البداية *</label>
                  <input
                    type="date"
                    required
                    value={formStartDate}
                    onChange={(e) => setFormStartDate(e.target.value)}
                    className="input-field"
                  />
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">تاريخ الانتهاء *</label>
                  <input
                    type="date"
                    required
                    value={formEndDate}
                    onChange={(e) => setFormEndDate(e.target.value)}
                    className="input-field"
                  />
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: '12px', marginTop: '16px' }}>
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="btn-secondary"
                >
                  إلغاء
                </button>

                <button
                  type="submit"
                  className="btn-primary"
                >
                  <Plus size={16} />
                  <span>تأكيد وحفظ المعرض</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
