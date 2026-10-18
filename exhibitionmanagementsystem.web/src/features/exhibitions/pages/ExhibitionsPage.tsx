import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { Building2, Calendar, Compass, Coins, MapPin, Pencil, Plus, Search, Store, Trash2, X } from 'lucide-react';
import { NavLink, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getAllItems } from '../../../services/pagination';

interface ExhibitionSummary {
  exhibitionID: number;
  name: string;
  type: string;
  startDate: string;
  endDate: string;
  status: string;
  venueName: string;
  entryCurrency?: string;
}

interface VenueSummary {
  venueID: number;
  name: string;
  city: string;
  isActive: boolean;
}

interface CurrencyItem {
  currencyCode: string;
  currencyName: string;
  symbol: string;
}

const defaultCurrencies: CurrencyItem[] = [
  { currencyCode: 'LYD', currencyName: 'دينار ليبي', symbol: 'د.ل' },
  { currencyCode: 'USD', currencyName: 'دولار أمريكي', symbol: '$' },
  { currencyCode: 'EUR', currencyName: 'يورو أوروبي', symbol: '€' },
  { currencyCode: 'SAR', currencyName: 'ريال سعودي', symbol: 'ر.س' },
  { currencyCode: 'AED', currencyName: 'درهم إماراتي', symbol: 'د.إ' },
  { currencyCode: 'GBP', currencyName: 'جنيه إسترليني', symbol: '£' },
];

interface ExhibitionForm {
  name: string;
  type: string;
  venueID: string;
  startDate: string;
  endDate: string;
  description: string;
  status: string;
  entryCurrency: string;
}

const emptyForm: ExhibitionForm = {
  name: '',
  type: 'Technical',
  venueID: '',
  startDate: '',
  endDate: '',
  description: '',
  status: 'Planning',
  entryCurrency: 'LYD',
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
  Planning: 'تخطيط',
  Draft: 'مسودة',
  Open: 'مفتوح',
  Active: 'نشط',
  Closed: 'مغلق',
  Ended: 'منتهي',
  Cancelled: 'ملغي',
};

const formatDate = (value: string) => new Date(value).toLocaleDateString('ar-LY');

const toInputDate = (dateStr?: string) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  if (isNaN(d.getTime())) return '';
  return d.toISOString().split('T')[0];
};

export const ExhibitionsPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const [searchParams, setSearchParams] = useSearchParams();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'SuperAdmin') ?? false;
  const [exhibitions, setExhibitions] = useState<ExhibitionSummary[]>([]);
  const [venues, setVenues] = useState<VenueSummary[]>([]);
  const [currencies, setCurrencies] = useState<CurrencyItem[]>(defaultCurrencies);
  const [search, setSearch] = useState(() => searchParams.get('search') ?? '');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingExhibitionId, setEditingExhibitionId] = useState<number | null>(null);
  const [form, setForm] = useState<ExhibitionForm>(emptyForm);

  const loadData = useCallback(async () => {
    await Promise.resolve();
    setIsLoading(true);
    setLoadError('');
    try {
      const [exhibitionsResponse, venuesResponse, currenciesRes] = await Promise.allSettled([
        getAllItems<ExhibitionSummary>('/api/exhibitions'),
        api.get<VenueSummary[]>('/api/venues/summaries'),
        api.get<CurrencyItem[]>('/api/currencies'),
      ]);
      if (exhibitionsResponse.status === 'fulfilled') {
        setExhibitions(exhibitionsResponse.value);
      }
      if (venuesResponse.status === 'fulfilled') {
        const activeVenues = venuesResponse.value.data.filter(venue => venue.isActive);
        setVenues(activeVenues);
      }
      if (currenciesRes.status === 'fulfilled' && Array.isArray(currenciesRes.value.data) && currenciesRes.value.data.length > 0) {
        setCurrencies(currenciesRes.value.data);
      }
    } catch {
      setLoadError('تعذر تحميل المعارض من الخادم. لم يتم عرض بيانات تجريبية بديلة.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const openCreateModal = useCallback(() => {
    setEditingExhibitionId(null);
    setForm({
      ...emptyForm,
      venueID: venues.length > 0 ? String(venues[0].venueID) : '',
      entryCurrency: user?.baseCurrency || 'LYD',
    });
    setIsModalOpen(true);
  }, [venues, user?.baseCurrency]);

  useEffect(() => {
    if (searchParams.get('action') === 'create') {
      openCreateModal();
      const nextParams = new URLSearchParams(searchParams);
      nextParams.delete('action');
      setSearchParams(nextParams, { replace: true });
    }
  }, [searchParams, setSearchParams, openCreateModal]);

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

  const handleEdit = async (exhibition: ExhibitionSummary) => {
    try {
      const res = await api.get<{
        name?: string;
        type?: string;
        venueID?: number;
        startDate?: string;
        endDate?: string;
        description?: string;
        status?: string;
        entryCurrency?: string;
      }>(`/api/exhibitions/${exhibition.exhibitionID}`);
      const data = res.data;
      setEditingExhibitionId(exhibition.exhibitionID);
      setForm({
        name: data.name || exhibition.name,
        type: data.type || exhibition.type || 'Technical',
        venueID: String(data.venueID || (venues.length > 0 ? venues[0].venueID : '')),
        startDate: toInputDate(data.startDate || exhibition.startDate),
        endDate: toInputDate(data.endDate || exhibition.endDate),
        description: data.description || '',
        status: data.status || exhibition.status || 'Planning',
        entryCurrency: data.entryCurrency || exhibition.entryCurrency || user?.baseCurrency || 'LYD',
      });
      setIsModalOpen(true);
    } catch {
      showToast('تعذر تحميل تفاصيل المعرض للتعديل. أعد المحاولة.', 'danger');
    }
  };

  const handleSubmit = async (event: FormEvent) => {
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
      if (editingExhibitionId) {
        await api.put(`/api/exhibitions/${editingExhibitionId}`, {
          venueID: Number(form.venueID),
          name: form.name.trim(),
          type: form.type,
          startDate: form.startDate,
          endDate: form.endDate,
          description: form.description.trim(),
          status: form.status,
          entryCurrency: form.entryCurrency || user?.baseCurrency || 'LYD',
        });
        showToast('تم تحديث بيانات المعرض بنجاح.', 'success');
      } else {
        await api.post('/api/exhibitions', {
          venueID: Number(form.venueID),
          name: form.name.trim(),
          type: form.type,
          startDate: form.startDate,
          endDate: form.endDate,
          description: form.description.trim(),
          entryCurrency: form.entryCurrency || user?.baseCurrency || 'LYD',
        });
        showToast('تم إنشاء المعرض وحفظه في الخادم.', 'success');
      }
      setForm(emptyForm);
      setEditingExhibitionId(null);
      setIsModalOpen(false);
      await loadData();
    } catch {
      showToast(editingExhibitionId ? 'فشل تحديث بيانات المعرض. تحقق من البيانات والصلاحيات.' : 'فشل إنشاء المعرض. تحقق من البيانات والصلاحيات.', 'danger');
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
          <button className="btn-primary" onClick={openCreateModal}>
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
                <NavLink to={`/exhibitions/${exhibition.exhibitionID}/booths`} style={{ textDecoration: 'none' }}>
                  <h2 style={{ fontSize: '17px', marginBottom: '6px', color: 'var(--text-primary)', cursor: 'pointer' }}>{exhibition.name}</h2>
                </NavLink>
                <span style={{ fontSize: '12px', color: 'var(--primary)' }}>{typeNames[exhibition.type] ?? exhibition.type}</span>
              </div>
              <div style={{ display: 'grid', gap: '8px', color: 'var(--text-secondary)', fontSize: '13px' }}>
                <span><Calendar size={14} style={{ display: 'inline', marginLeft: '6px' }} />{formatDate(exhibition.startDate)} – {formatDate(exhibition.endDate)}</span>
                <span><MapPin size={14} style={{ display: 'inline', marginLeft: '6px' }} />{exhibition.venueName || 'غير محدد'}</span>
                <span><Coins size={14} style={{ display: 'inline', marginLeft: '6px', color: 'var(--primary)' }} />العملة الافتراضية: <strong style={{ color: 'var(--primary)' }}>{exhibition.entryCurrency || 'LYD'}</strong></span>
              </div>

              {/* Main Action to Exhibition Booths & Pricing Hub */}
              <NavLink
                to={`/exhibitions/${exhibition.exhibitionID}/booths`}
                className="btn-primary"
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '8px',
                  fontSize: '13px',
                  padding: '9px 12px',
                  marginTop: '4px',
                  textDecoration: 'none'
                }}
              >
                <Store size={16} />
                <span>إدارة الأجنحة والتسعير</span>
              </NavLink>

              <div style={{ display: 'flex', gap: '8px', marginTop: 'auto', flexWrap: 'wrap' }}>
                {canManage && (
                  <button
                    className="btn-secondary"
                    onClick={() => void handleEdit(exhibition)}
                    style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', fontSize: '13px' }}
                  >
                    <Pencil size={15} /> تعديل
                  </button>
                )}
                {canManage && (
                  <NavLink
                    className="btn-secondary"
                    to={`/booths/designer?exhibitionId=${exhibition.exhibitionID}`}
                    style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', fontSize: '13px' }}
                    title="فتح مصمم المخططات 2D"
                  >
                    <Compass size={15} /> مخطط 2D
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
          <form className="card" onSubmit={handleSubmit} style={{ width: 'min(580px, 100%)', display: 'grid', gap: '15px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>{editingExhibitionId ? 'تعديل بيانات المعرض' : 'إنشاء معرض جديد'}</h2>
              <button type="button" className="btn-icon" onClick={() => { setIsModalOpen(false); setEditingExhibitionId(null); }}><X size={18} /></button>
            </div>

            {venues.length === 0 && (
              <div style={{ padding: '12px 14px', background: '#fffbe6', color: '#d46b08', border: '1px solid #ffe58f', borderRadius: 'var(--radius-md)', fontSize: '13px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <span>⚠️ لا توجد مواقع أو قاعات مسجلة في النظام حتى الآن. يلزم وجود موقع لإسناد المعرض إليه.</span>
                <NavLink to="/venues" className="btn-secondary" style={{ alignSelf: 'flex-start', padding: '6px 12px', fontSize: '12px' }}>
                  <MapPin size={14} /> الانتقال لإضافة موقع وقاعة
                </NavLink>
              </div>
            )}

            <div>
              <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                اسم المعرض
              </label>
              <input className="input-field" required placeholder="مثال: معرض طرابلس الدولي للتقنية" value={form.name} onChange={event => updateForm('name', event.target.value)} />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: editingExhibitionId ? '1fr 1fr' : '1fr', gap: '12px' }}>
              <div>
                <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                  نوع المعرض
                </label>
                <select className="input-field" value={form.type} onChange={event => updateForm('type', event.target.value)}>
                  {Object.entries(typeNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
              </div>

              {editingExhibitionId && (
                <div>
                  <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                    حالة المعرض
                  </label>
                  <select className="input-field" value={form.status} onChange={event => updateForm('status', event.target.value)}>
                    <option value="Planning">تخطيط (Planning)</option>
                    <option value="Open">مفتوح / نشط (Open)</option>
                    <option value="Closed">مغلق (Closed)</option>
                    <option value="Cancelled">ملغي (Cancelled)</option>
                  </select>
                </div>
              )}
            </div>

            <div>
              <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                موقع المعرض
              </label>
              <select className="input-field" required value={form.venueID} onChange={event => updateForm('venueID', event.target.value)} disabled={venues.length === 0}>
                <option value="">{venues.length === 0 ? 'لا توجد مواقع متاحة' : 'اختر موقع المعرض'}</option>
                {venues.map(venue => <option key={venue.venueID} value={venue.venueID}>{venue.name} — {venue.city}</option>)}
              </select>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div>
                <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                  تاريخ البداية
                </label>
                <input aria-label="تاريخ البداية" className="input-field" type="date" required value={form.startDate} onChange={event => updateForm('startDate', event.target.value)} />
              </div>
              <div>
                <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                  تاريخ النهاية
                </label>
                <input aria-label="تاريخ النهاية" className="input-field" type="date" required value={form.endDate} onChange={event => updateForm('endDate', event.target.value)} />
              </div>
            </div>

            <div>
              <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                العملة الافتراضية للمعرض (Default Currency) *
              </label>
              <select
                className="input-field"
                required
                value={form.entryCurrency}
                onChange={event => updateForm('entryCurrency', event.target.value)}
              >
                {currencies.map(c => (
                  <option key={c.currencyCode} value={c.currencyCode}>
                    {c.currencyCode} — {c.currencyName} ({c.symbol})
                  </option>
                ))}
              </select>
              <span style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '2px', display: 'block' }}>
                العملة الأساسية المستخدمة في تسعير أجنحة المعرض، التذاكر، والفواتير الصادرة.
              </span>
            </div>

            <div>
              <label style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-secondary)', marginBottom: '4px', display: 'block' }}>
                وصف المعرض
              </label>
              <textarea className="input-field" placeholder="وصف مختصر لأهداف ومجالات المعرض..." value={form.description} onChange={event => updateForm('description', event.target.value)} rows={3} />
            </div>

            <div style={{ display: 'flex', gap: '12px', marginTop: '6px' }}>
              <button className="btn-primary" type="submit" style={{ flex: 1 }} disabled={isSaving || venues.length === 0}>
                {isSaving ? 'جاري الحفظ…' : editingExhibitionId ? 'تحديث المعرض' : 'حفظ المعرض'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => { setIsModalOpen(false); setEditingExhibitionId(null); }}>
                إلغاء
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};
