import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { Building2, Calendar, Compass, MapPin, Plus, Search, Trash2, X } from 'lucide-react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface ExhibitionSummary {
  exhibitionID: number;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
  status: string;
  venueName: string;
}

interface VenueSummary {
  venueID: number;
  name: string;
  city: string;
  isActive: boolean;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

interface ExhibitionForm {
  name: string;
  type: string;
  venueID: string;
  startDate: string;
  endDate: string;
  description: string;
}

const emptyForm: ExhibitionForm = {
  name: '',
  type: 'Technical',
  venueID: '',
  startDate: '',
  endDate: '',
  description: '',
};

const typeNames: Record<string, string> = {
  Technical: 'تقني',
  Medical: 'طبي',
  Industrial: 'صناعي',
  Commercial: 'تجاري',
  Educational: 'تعليمي',
  Automotive: 'سيارات ونقل',
};

const statusNames: Record<string, string> = {
  Draft: 'مسودة',
  Open: 'مفتوح',
  Closed: 'مغلق',
  Cancelled: 'ملغي',
};

const formatDate = (value: string) => new Date(value).toLocaleDateString('ar-LY');

export const ExhibitionsPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin') ?? false;
  const [exhibitions, setExhibitions] = useState<ExhibitionSummary[]>([]);
  const [venues, setVenues] = useState<VenueSummary[]>([]);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [form, setForm] = useState<ExhibitionForm>(emptyForm);

  const loadData = useCallback(async () => {
    await Promise.resolve();
    setIsLoading(true);
    setLoadError('');
    try {
      const [exhibitionsResponse, venuesResponse] = await Promise.all([
        api.get<PagedResult<ExhibitionSummary>>('/api/exhibitions?page=1&pageSize=100'),
        api.get<VenueSummary[]>('/api/venues/summaries'),
      ]);
      setExhibitions(exhibitionsResponse.data.items ?? []);
      setVenues(venuesResponse.data.filter(venue => venue.isActive));
    } catch {
      setLoadError('تعذر تحميل المعارض من الخادم. لم يتم عرض بيانات تجريبية بديلة.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    // تحميل البيانات الخارجية هو الغرض المقصود من هذا الأثر.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void loadData();
  }, [loadData]);

  const filteredExhibitions = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return exhibitions;
    return exhibitions.filter(exhibition =>
      exhibition.name.toLowerCase().includes(query)
      || exhibition.venueName.toLowerCase().includes(query)
      || exhibition.type.toLowerCase().includes(query));
  }, [exhibitions, search]);

  const updateForm = (key: keyof ExhibitionForm, value: string) => {
    setForm(current => ({ ...current, [key]: value }));
  };

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault();
    if (!form.name || !form.venueID || !form.startDate || !form.endDate) {
      showToast('يرجى إدخال الاسم والموقع وتاريخي البداية والنهاية.', 'warning');
      return;
    }
    if (new Date(form.endDate) < new Date(form.startDate)) {
      showToast('تاريخ النهاية يجب ألا يسبق تاريخ البداية.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      await api.post('/api/exhibitions', {
        venueID: Number(form.venueID),
        name: form.name.trim(),
        type: form.type,
        startDate: form.startDate,
        endDate: form.endDate,
        description: form.description.trim(),
        entryCurrency: user?.baseCurrency || 'LYD',
      });
      showToast('تم إنشاء المعرض وحفظه في الخادم.', 'success');
      setForm(emptyForm);
      setIsModalOpen(false);
      await loadData();
    } catch {
      showToast('فشل إنشاء المعرض. تحقق من البيانات والصلاحيات.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (exhibition: ExhibitionSummary) => {
    if (!window.confirm(`هل تريد حذف معرض «${exhibition.name}»؟`)) return;

    try {
      await api.delete(`/api/exhibitions/${exhibition.exhibitionID}`);
      setExhibitions(current => current.filter(item => item.exhibitionID !== exhibition.exhibitionID));
      showToast('تم حذف المعرض من الخادم.', 'success');
    } catch {
      showToast('تعذر حذف المعرض؛ قد يكون مرتبطاً بحجوزات أو لا تملك الصلاحية.', 'danger');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <h1 style={{ fontSize: '24px', fontWeight: 800 }}>إدارة المعارض</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>البيانات المعروضة متزامنة مباشرة مع الخادم.</p>
        </div>
        {canManage && (
          <button className="btn-primary" onClick={() => setIsModalOpen(true)} disabled={venues.length === 0}>
            <Plus size={18} /> إنشاء معرض
          </button>
        )}
      </div>

      <div className="card" style={{ position: 'relative' }}>
        <Search size={18} style={{ position: 'absolute', right: '28px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
        <input
          className="input-field"
          value={search}
          onChange={event => setSearch(event.target.value)}
          placeholder="ابحث باسم المعرض أو الموقع أو النوع"
          style={{ paddingRight: '42px' }}
        />
      </div>

      {loadError && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{loadError}</div>}

      {isLoading ? (
        <div className="card" style={{ textAlign: 'center' }}>جاري تحميل المعارض…</div>
      ) : filteredExhibitions.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', color: 'var(--text-secondary)' }}>لا توجد معارض مطابقة.</div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '20px' }}>
          {filteredExhibitions.map(exhibition => (
            <article key={exhibition.exhibitionID} className="card" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: '12px' }}>
                <Building2 size={24} style={{ color: 'var(--primary)', flexShrink: 0 }} />
                <span className="badge badge-info">{statusNames[exhibition.status] ?? exhibition.status}</span>
              </div>
              <div>
                <h2 style={{ fontSize: '17px', marginBottom: '6px' }}>{exhibition.name}</h2>
                <span style={{ fontSize: '12px', color: 'var(--primary)' }}>{typeNames[exhibition.type] ?? exhibition.type}</span>
              </div>
              <div style={{ display: 'grid', gap: '8px', color: 'var(--text-secondary)', fontSize: '13px' }}>
                <span><Calendar size={14} style={{ display: 'inline', marginLeft: '6px' }} />{formatDate(exhibition.startDate)} – {formatDate(exhibition.endDate)}</span>
                <span><MapPin size={14} style={{ display: 'inline', marginLeft: '6px' }} />{exhibition.venueName || 'غير محدد'}</span>
              </div>
              <div style={{ display: 'flex', gap: '8px', marginTop: 'auto' }}>
                {canManage && (
                  <NavLink className="btn-secondary" to={`/booths/designer?exhibitionId=${exhibition.exhibitionID}`} style={{ flex: 1 }}>
                    <Compass size={15} /> مخطط الأجنحة
                  </NavLink>
                )}
                {canManage && (
                  <button className="btn-icon" title="حذف المعرض" onClick={() => void handleDelete(exhibition)} style={{ color: 'var(--danger)' }}>
                    <Trash2 size={17} />
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>
      )}

      {isModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleCreate} style={{ width: 'min(580px, 100%)', display: 'grid', gap: '15px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>إنشاء معرض جديد</h2>
              <button type="button" className="btn-icon" onClick={() => setIsModalOpen(false)}><X size={18} /></button>
            </div>
            <input className="input-field" required placeholder="اسم المعرض" value={form.name} onChange={event => updateForm('name', event.target.value)} />
            <select className="input-field" value={form.type} onChange={event => updateForm('type', event.target.value)}>
              {Object.entries(typeNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
            <select className="input-field" required value={form.venueID} onChange={event => updateForm('venueID', event.target.value)}>
              <option value="">اختر موقع المعرض</option>
              {venues.map(venue => <option key={venue.venueID} value={venue.venueID}>{venue.name} — {venue.city}</option>)}
            </select>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <input aria-label="تاريخ البداية" className="input-field" type="date" required value={form.startDate} onChange={event => updateForm('startDate', event.target.value)} />
              <input aria-label="تاريخ النهاية" className="input-field" type="date" required value={form.endDate} onChange={event => updateForm('endDate', event.target.value)} />
            </div>
            <textarea className="input-field" placeholder="وصف مختصر" value={form.description} onChange={event => updateForm('description', event.target.value)} rows={3} />
            <button className="btn-primary" type="submit" disabled={isSaving}>{isSaving ? 'جاري الحفظ…' : 'حفظ المعرض'}</button>
          </form>
        </div>
      )}
    </div>
  );
};
