import React, { useEffect, useState, useMemo } from 'react';
import {
  CalendarDays,
  Plus,
  Trash2,
  Search,
  Clock,
  User,
  MapPin,
  Users,
  CheckCircle2,
  XCircle,
  X,
  Sparkles,
  AlertCircle
} from 'lucide-react';
import { api } from '../../../services/api';
import { useToast } from '../../../app/providers/ToastContext';

interface Exhibition {
  exhibitionID: number;
  name: string;
}

interface Hall {
  hallID: number;
  name: string;
  venueName?: string;
}

interface Schedule {
  scheduleID: number;
  exhibitionID: number;
  hallID?: number;
  hallName?: string;
  eventName: string;
  eventType?: string;
  startDateTime: string;
  endDateTime: string;
  speakerName: string;
  maxAttendees?: number;
  description?: string;
  isPublic: boolean;
}

interface NewScheduleForm {
  eventName: string;
  eventType: string;
  hallID: string;
  speakerName: string;
  startDateTime: string;
  endDateTime: string;
  maxAttendees: string;
  description: string;
  isPublic: boolean;
}

const EVENT_TYPES = [
  'ندوة',
  'ورشة عمل',
  'محاضرة',
  'حفل افتتاح',
  'جلسة حوارية',
  'عرض تقديمي',
  'حفل ختام',
  'اجتماع ثنائي (B2B)'
];

export const EventsPage: React.FC = () => {
  const { showToast } = useToast();

  const [exhibitions, setExhibitions] = useState<Exhibition[]>([]);
  const [selectedExhibitionId, setSelectedExhibitionId] = useState<string>('');
  const [halls, setHalls] = useState<Hall[]>([]);
  const [schedules, setSchedules] = useState<Schedule[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string>('');

  // Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState('ALL');
  const [selectedPrivacy, setSelectedPrivacy] = useState<'ALL' | 'PUBLIC' | 'PRIVATE'>('ALL');

  // Modals
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [scheduleToDelete, setScheduleToDelete] = useState<Schedule | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Form State
  const initialForm: NewScheduleForm = {
    eventName: '',
    eventType: 'ندوة',
    hallID: '',
    speakerName: '',
    startDateTime: '',
    endDateTime: '',
    maxAttendees: '100',
    description: '',
    isPublic: true
  };
  const [formData, setFormData] = useState<NewScheduleForm>(initialForm);

  // Load Exhibitions and Halls
  useEffect(() => {
    const fetchMetadata = async () => {
      try {
        const [exhibitionsRes, hallsRes] = await Promise.all([
          api.get<{ items: Exhibition[] }>('/api/exhibitions?page=1&pageSize=100'),
          api.get<Hall[]>('/api/halls').catch(() => ({ data: [] }))
        ]);

        const exList = exhibitionsRes.data.items ?? [];
        setExhibitions(exList);
        if (exList.length > 0) {
          setSelectedExhibitionId(exList[0].exhibitionID.toString());
        }

        if (Array.isArray(hallsRes.data)) {
          setHalls(hallsRes.data);
        }
      } catch (err: unknown) {
        console.error('Failed to load initial data:', err);
        setError('تعذر تحميل بيانات المعارض أو القاعات.');
      }
    };

    void fetchMetadata();
  }, []);

  // Load Schedules when selected exhibition changes
  const loadSchedules = async () => {
    if (!selectedExhibitionId) {
      setSchedules([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    setError('');
    try {
      const res = await api.get<Schedule[]>(`/api/exhibitions/${selectedExhibitionId}/schedules`);
      setSchedules(res.data || []);
    } catch (err: unknown) {
      console.error('Failed to load schedules:', err);
      setError('تعذر تحميل جدول الفعاليات لهذا المعرض.');
      setSchedules([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadSchedules();
  }, [selectedExhibitionId]);

  // Filtered schedules
  const filteredSchedules = useMemo(() => {
    return schedules.filter((s) => {
      const matchesSearch =
        s.eventName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (s.speakerName && s.speakerName.toLowerCase().includes(searchQuery.toLowerCase())) ||
        (s.hallName && s.hallName.toLowerCase().includes(searchQuery.toLowerCase()));

      const matchesType = selectedType === 'ALL' || s.eventType === selectedType;

      const matchesPrivacy =
        selectedPrivacy === 'ALL' ||
        (selectedPrivacy === 'PUBLIC' && s.isPublic) ||
        (selectedPrivacy === 'PRIVATE' && !s.isPublic);

      return matchesSearch && matchesType && matchesPrivacy;
    });
  }, [schedules, searchQuery, selectedType, selectedPrivacy]);

  // Statistics
  const stats = useMemo(() => {
    const total = schedules.length;
    const publicCount = schedules.filter((s) => s.isPublic).length;
    const speakersCount = new Set(schedules.map((s) => s.speakerName).filter(Boolean)).size;
    const totalCapacity = schedules.reduce((acc, s) => acc + (s.maxAttendees || 0), 0);

    return { total, publicCount, speakersCount, totalCapacity };
  }, [schedules]);

  // Handle Add Schedule
  const handleOpenAddModal = () => {
    const now = new Date();
    now.setMinutes(0, 0, 0);
    now.setHours(now.getHours() + 1);
    const startIso = new Date(now.getTime() - now.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);

    const end = new Date(now.getTime() + 2 * 60 * 60 * 1000);
    const endIso = new Date(end.getTime() - end.getTimezoneOffset() * 60000)
      .toISOString()
      .slice(0, 16);

    setFormData({
      ...initialForm,
      startDateTime: startIso,
      endDateTime: endIso
    });
    setIsAddModalOpen(true);
  };

  const handleCreateSchedule = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedExhibitionId) {
      showToast('الرجاء اختيار المعرض أولاً', 'danger');
      return;
    }

    if (!formData.eventName.trim()) {
      showToast('الرجاء إدخال اسم الفعالية', 'danger');
      return;
    }

    if (!formData.startDateTime || !formData.endDateTime) {
      showToast('الرجاء تحديد موعدي البداية والنهاية', 'danger');
      return;
    }

    const start = new Date(formData.startDateTime);
    const end = new Date(formData.endDateTime);
    if (start >= end) {
      showToast('تاريخ البدء يجب أن يكون قبل تاريخ الانتهاء', 'danger');
      return;
    }

    setSubmitting(true);
    try {
      const payload = {
        exhibitionID: parseInt(selectedExhibitionId, 10),
        hallID: formData.hallID ? parseInt(formData.hallID, 10) : null,
        eventName: formData.eventName.trim(),
        eventType: formData.eventType,
        speakerName: formData.speakerName.trim(),
        startDateTime: start.toISOString(),
        endDateTime: end.toISOString(),
        maxAttendees: formData.maxAttendees ? parseInt(formData.maxAttendees, 10) : null,
        description: formData.description.trim(),
        isPublic: formData.isPublic
      };

      await api.post(`/api/exhibitions/${selectedExhibitionId}/schedules`, payload);
      showToast('تمت إضافة الفعالية بنجاح إلى جدول المعرض ✓', 'success');
      setIsAddModalOpen(false);
      await loadSchedules();
    } catch (err: any) {
      console.error('Failed to create schedule:', err);
      const msg = err.response?.data?.message || err.response?.data || 'تعذر إضافة الفعالية.';
      showToast(typeof msg === 'string' ? msg : 'تعذر إضافة الفعالية', 'danger');
    } finally {
      setSubmitting(false);
    }
  };

  // Handle Delete Schedule
  const handleDeleteSchedule = async () => {
    if (!scheduleToDelete) return;
    setSubmitting(true);
    try {
      await api.delete(`/api/exhibitions/schedules/${scheduleToDelete.scheduleID}`);
      showToast('تم حذف الفعالية من الجدول بنجاح ✓', 'success');
      setScheduleToDelete(null);
      await loadSchedules();
    } catch (err: any) {
      console.error('Failed to delete schedule:', err);
      const msg = err.response?.data?.message || 'تعذر حذف الفعالية.';
      showToast(msg, 'danger');
    } finally {
      setSubmitting(false);
    }
  };

  const getEventTypeBadgeClass = (type?: string) => {
    switch (type) {
      case 'ندوة':
        return 'badge-info';
      case 'ورشة عمل':
        return 'badge-primary';
      case 'محاضرة':
        return 'badge-warning';
      case 'حفل افتتاح':
      case 'حفل ختام':
        return 'badge-success';
      default:
        return 'badge-secondary';
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <CalendarDays size={26} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)', margin: 0 }}>
              جدول الفعاليات والجلسات العلمية
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '4px', margin: 0 }}>
            إدارة الندوات، الجلسات، ورش العمل، والمتحدثين الرسميين للمعرض
          </p>
        </div>

        <button
          onClick={handleOpenAddModal}
          disabled={!selectedExhibitionId}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px', gap: '8px' }}
        >
          <Plus size={18} />
          <span>إضافة فعالية جديدة</span>
        </button>
      </div>

      {/* KPI Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '16px' }}>
        <div className="card" style={{ padding: '18px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'var(--primary-light)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--primary)' }}>
            <CalendarDays size={24} />
          </div>
          <div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', fontWeight: 600 }}>إجمالي الفعاليات</div>
            <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>{stats.total}</div>
          </div>
        </div>

        <div className="card" style={{ padding: '18px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'rgba(16, 185, 129, 0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--success)' }}>
            <CheckCircle2 size={24} />
          </div>
          <div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', fontWeight: 600 }}>جلسات عامة مفتوحة</div>
            <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>{stats.publicCount}</div>
          </div>
        </div>

        <div className="card" style={{ padding: '18px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'rgba(245, 158, 11, 0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--warning)' }}>
            <User size={24} />
          </div>
          <div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', fontWeight: 600 }}>المتحدثون والمدربون</div>
            <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>{stats.speakersCount}</div>
          </div>
        </div>

        <div className="card" style={{ padding: '18px', display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'rgba(99, 102, 241, 0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: '#6366f1' }}>
            <Users size={24} />
          </div>
          <div>
            <div style={{ fontSize: '12px', color: 'var(--text-secondary)', fontWeight: 600 }}>إجمالي السعة الاستيعابية</div>
            <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>{stats.totalCapacity.toLocaleString('ar-LY')}</div>
          </div>
        </div>
      </div>

      {/* Exhibition Selector and Filters Card */}
      <div className="card" style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '16px' }}>
          {/* Exhibition Picker */}
          <div>
            <label className="form-label" style={{ marginBottom: '6px' }}>المعرض المستهدف</label>
            <select
              className="input-field"
              value={selectedExhibitionId}
              onChange={(e) => setSelectedExhibitionId(e.target.value)}
            >
              {exhibitions.length === 0 && <option value="">لا توجد معارض متاحة</option>}
              {exhibitions.map((item) => (
                <option key={item.exhibitionID} value={item.exhibitionID}>
                  {item.name}
                </option>
              ))}
            </select>
          </div>

          {/* Search */}
          <div>
            <label className="form-label" style={{ marginBottom: '6px' }}>بحث بالفعالية أو المتحدث</label>
            <div style={{ position: 'relative' }}>
              <input
                type="text"
                placeholder="ابحث بالاسم، المتحدث، أو القاعة..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="input-field"
                style={{ paddingRight: '36px' }}
              />
              <Search
                size={16}
                style={{
                  position: 'absolute',
                  right: '12px',
                  top: '50%',
                  transform: 'translateY(-50%)',
                  color: 'var(--text-secondary)'
                }}
              />
            </div>
          </div>

          {/* Type Filter */}
          <div>
            <label className="form-label" style={{ marginBottom: '6px' }}>نوع الفعالية</label>
            <select
              className="input-field"
              value={selectedType}
              onChange={(e) => setSelectedType(e.target.value)}
            >
              <option value="ALL">جميع الأنواع</option>
              {EVENT_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </div>

          {/* Privacy Filter */}
          <div>
            <label className="form-label" style={{ marginBottom: '6px' }}>مستوى الحضور</label>
            <select
              className="input-field"
              value={selectedPrivacy}
              onChange={(e) => setSelectedPrivacy(e.target.value as any)}
            >
              <option value="ALL">الكل (عامة ومغلقة)</option>
              <option value="PUBLIC">فعاليات عامة للجميع</option>
              <option value="PRIVATE">فعاليات خاصة / مقيدة</option>
            </select>
          </div>
        </div>
      </div>

      {/* Error Banner */}
      {error && (
        <div
          role="alert"
          style={{
            padding: '12px 16px',
            borderRadius: 'var(--radius-md)',
            backgroundColor: 'rgba(239, 68, 68, 0.1)',
            color: 'var(--danger)',
            fontSize: '14px',
            display: 'flex',
            alignItems: 'center',
            gap: '8px'
          }}
        >
          <AlertCircle size={18} />
          <span>{error}</span>
        </div>
      )}

      {/* Schedules Table Card */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>عنوان الفعالية</th>
                <th>النوع</th>
                <th>القاعة</th>
                <th>المتحدث / المدرب</th>
                <th>التوقيت والمدة</th>
                <th>السعة</th>
                <th>الوصول</th>
                <th style={{ textAlign: 'center' }}>الإجراءات</th>
              </tr>
            </thead>
            <tbody>
              {filteredSchedules.map((item) => {
                const start = new Date(item.startDateTime);
                const end = new Date(item.endDateTime);
                return (
                  <tr key={item.scheduleID}>
                    <td>
                      <div style={{ fontWeight: 700, color: 'var(--text-primary)' }}>
                        {item.eventName}
                      </div>
                      {item.description && (
                        <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px', maxWidth: '280px' }} className="truncate">
                          {item.description}
                        </div>
                      )}
                    </td>
                    <td>
                      <span className={`badge ${getEventTypeBadgeClass(item.eventType)}`}>
                        {item.eventType || 'فعالية'}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--text-secondary)' }}>
                        <MapPin size={14} />
                        <span>{item.hallName || 'غير محددة'}</span>
                      </div>
                    </td>
                    <td>
                      {item.speakerName ? (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                          <User size={14} style={{ color: 'var(--primary)' }} />
                          <span style={{ fontWeight: 600 }}>{item.speakerName}</span>
                        </div>
                      ) : (
                        <span style={{ color: 'var(--text-secondary)' }}>—</span>
                      )}
                    </td>
                    <td>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '2px', fontSize: '12px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '4px', color: 'var(--text-primary)' }}>
                          <Clock size={12} style={{ color: 'var(--primary)' }} />
                          <span>{start.toLocaleDateString('ar-LY', { month: 'short', day: 'numeric' })} {start.toLocaleTimeString('ar-LY', { hour: '2-digit', minute: '2-digit' })}</span>
                        </div>
                        <div style={{ color: 'var(--text-secondary)', marginRight: '16px' }}>
                          إلى {end.toLocaleTimeString('ar-LY', { hour: '2-digit', minute: '2-digit' })}
                        </div>
                      </div>
                    </td>
                    <td>
                      {item.maxAttendees ? (
                        <span className="badge badge-secondary" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                          <Users size={12} />
                          {item.maxAttendees} مقعد
                        </span>
                      ) : (
                        <span style={{ color: 'var(--text-secondary)' }}>غير محدد</span>
                      )}
                    </td>
                    <td>
                      {item.isPublic ? (
                        <span className="badge badge-success" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                          <CheckCircle2 size={12} />
                          عامة
                        </span>
                      ) : (
                        <span className="badge badge-warning" style={{ display: 'inline-flex', alignItems: 'center', gap: '4px' }}>
                          <XCircle size={12} />
                          مغلقة / خاصة
                        </span>
                      )}
                    </td>
                    <td style={{ textAlign: 'center' }}>
                      <button
                        onClick={() => setScheduleToDelete(item)}
                        className="btn-danger"
                        style={{ height: '32px', width: '32px', padding: 0, display: 'inline-flex', alignItems: 'center', justifyContent: 'center', borderRadius: '8px' }}
                        title="حذف الفعالية"
                      >
                        <Trash2 size={15} />
                      </button>
                    </td>
                  </tr>
                );
              })}

              {!loading && filteredSchedules.length === 0 && (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '48px 16px' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
                      <CalendarDays size={42} style={{ color: 'var(--border-dark)', strokeWidth: 1.5 }} />
                      <div style={{ fontWeight: 700, color: 'var(--text-primary)', fontSize: '15px' }}>
                        لا توجد فعاليات مسجلة لهذا المعرض
                      </div>
                      <p style={{ color: 'var(--text-secondary)', fontSize: '13px', margin: 0 }}>
                        اضغط على "إضافة فعالية جديدة" لإدراج ندوات وجلسات المعرض
                      </p>
                    </div>
                  </td>
                </tr>
              )}

              {loading && (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '36px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '10px', color: 'var(--text-secondary)' }}>
                      <Sparkles className="animate-spin" size={18} />
                      <span>جاري تحميل جدول الفعاليات...</span>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Add Schedule Modal */}
      {isAddModalOpen && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '16px'
          }}
        >
          <div
            className="card"
            style={{
              width: '100%',
              maxWidth: '620px',
              padding: '24px',
              maxHeight: '90vh',
              overflowY: 'auto'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <CalendarDays size={22} style={{ color: 'var(--primary)' }} />
                <h3 style={{ fontSize: '18px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                  إضافة فعالية أو جلسة جديدة
                </h3>
              </div>
              <button
                onClick={() => setIsAddModalOpen(false)}
                style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-secondary)' }}
              >
                <X size={20} />
              </button>
            </div>

            <form onSubmit={handleCreateSchedule} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">
                  عنوان الفعالية <span style={{ color: 'var(--danger)' }}>*</span>
                </label>
                <input
                  type="text"
                  required
                  placeholder="مثال: ندوة التحول الرقمي وتحديات المستقبل"
                  value={formData.eventName}
                  onChange={(e) => setFormData({ ...formData, eventName: e.target.value })}
                  className="input-field"
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">نوع الفعالية</label>
                  <select
                    value={formData.eventType}
                    onChange={(e) => setFormData({ ...formData, eventType: e.target.value })}
                    className="input-field"
                  >
                    {EVENT_TYPES.map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">القاعة المخصصة</label>
                  <select
                    value={formData.hallID}
                    onChange={(e) => setFormData({ ...formData, hallID: e.target.value })}
                    className="input-field"
                  >
                    <option value="">-- بدون قاعة محددة --</option>
                    {halls.map((h) => (
                      <option key={h.hallID} value={h.hallID}>
                        {h.name} {h.venueName ? `(${h.venueName})` : ''}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">المتحدث / المدرب الرئيسي</label>
                  <input
                    type="text"
                    placeholder="اسم المتحدث أو المحاضر"
                    value={formData.speakerName}
                    onChange={(e) => setFormData({ ...formData, speakerName: e.target.value })}
                    className="input-field"
                  />
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">الحد الأقصى للحضور</label>
                  <input
                    type="number"
                    min="1"
                    placeholder="100"
                    value={formData.maxAttendees}
                    onChange={(e) => setFormData({ ...formData, maxAttendees: e.target.value })}
                    className="input-field"
                  />
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">
                    تاريخ ووقت البدء <span style={{ color: 'var(--danger)' }}>*</span>
                  </label>
                  <input
                    type="datetime-local"
                    required
                    value={formData.startDateTime}
                    onChange={(e) => setFormData({ ...formData, startDateTime: e.target.value })}
                    className="input-field"
                  />
                </div>

                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">
                    تاريخ ووقت الانتهاء <span style={{ color: 'var(--danger)' }}>*</span>
                  </label>
                  <input
                    type="datetime-local"
                    required
                    value={formData.endDateTime}
                    onChange={(e) => setFormData({ ...formData, endDateTime: e.target.value })}
                    className="input-field"
                  />
                </div>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">وصف الفعالية أو محاور الجلسة</label>
                <textarea
                  rows={3}
                  placeholder="نبذة مختصرة عن أهداف ومحاور الفعالية..."
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="input-field"
                  style={{ resize: 'vertical' }}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '10px', padding: '8px 0' }}>
                <input
                  type="checkbox"
                  id="isPublicCheck"
                  checked={formData.isPublic}
                  onChange={(e) => setFormData({ ...formData, isPublic: e.target.checked })}
                  style={{ width: '18px', height: '18px', accentColor: 'var(--primary)', cursor: 'pointer' }}
                />
                <label htmlFor="isPublicCheck" style={{ fontSize: '14px', fontWeight: 600, color: 'var(--text-primary)', cursor: 'pointer' }}>
                  فعالية عامة ومفتوحة لجميع زوار المعرض
                </label>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px', marginTop: '12px' }}>
                <button
                  type="button"
                  onClick={() => setIsAddModalOpen(false)}
                  className="btn-secondary"
                  style={{ height: '40px', padding: '0 18px' }}
                >
                  إلغاء
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="btn-primary"
                  style={{ height: '40px', padding: '0 24px' }}
                >
                  {submitting ? 'جاري الحفظ...' : 'إضافة الفعالية'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {scheduleToDelete && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '16px'
          }}
        >
          <div className="card" style={{ width: '100%', maxWidth: '440px', padding: '24px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
              <div style={{ width: '40px', height: '40px', borderRadius: '50%', backgroundColor: 'rgba(239, 68, 68, 0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--danger)' }}>
                <Trash2 size={20} />
              </div>
              <h3 style={{ fontSize: '18px', fontWeight: 800, margin: 0, color: 'var(--text-primary)' }}>
                تأكيد حذف الفعالية
              </h3>
            </div>

            <p style={{ fontSize: '14px', color: 'var(--text-secondary)', marginBottom: '20px', lineHeight: 1.6 }}>
              هل أنت متأكد من رغبتك في حذف فعالية <strong style={{ color: 'var(--text-primary)' }}>"{scheduleToDelete.eventName}"</strong> من جدول المعرض؟ لن يمكن التراجع عن هذا الإجراء.
            </p>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
              <button
                type="button"
                onClick={() => setScheduleToDelete(null)}
                className="btn-secondary"
                style={{ height: '38px', padding: '0 16px' }}
              >
                إلغاء
              </button>
              <button
                type="button"
                onClick={handleDeleteSchedule}
                disabled={submitting}
                className="btn-danger"
                style={{ height: '38px', padding: '0 20px' }}
              >
                {submitting ? 'جاري الحذف...' : 'نعم، حذف'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
