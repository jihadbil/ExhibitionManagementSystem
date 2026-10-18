import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  ArrowRight,
  Building,
  Building2,
  Edit2,
  DraftingCompass,
  Layers,
  MapPin,
  Pencil,
  Plus,
  Search,
  Trash2,
  Users,
  X
} from 'lucide-react';
import { NavLink, useParams, useSearchParams } from 'react-router-dom';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface Venue {
  venueID: number;
  name: string;
  address: string;
  city: string;
  country: string;
  totalCapacity?: number;
  mapImageURL?: string;
  isActive: boolean;
  hallsCount: number;
}

interface Hall {
  hallID: number;
  venueID: number;
  venueName?: string;
  hallName: string;
  areaSqM?: number;
  maxBooths?: number;
  floorPlanWidth?: number;
  floorPlanHeight?: number;
  isActive: boolean;
  boothsCount: number;
}

interface VenueFormData {
  name: string;
  address: string;
  city: string;
  country: string;
  totalCapacity: string;
  mapImageURL: string;
  isActive: boolean;
}

const emptyVenueForm: VenueFormData = {
  name: '',
  address: '',
  city: 'طرابلس',
  country: 'ليبيا',
  totalCapacity: '',
  mapImageURL: '',
  isActive: true,
};

interface HallFormData {
  hallName: string;
  areaSqM: string;
  maxBooths: string;
  floorPlanWidth: string;
  floorPlanHeight: string;
  isActive: boolean;
}

const emptyHallForm: HallFormData = {
  hallName: '',
  areaSqM: '',
  maxBooths: '',
  floorPlanWidth: '40',
  floorPlanHeight: '30',
  isActive: true,
};

export const VenuesPage = () => {
  const { venueId: routeVenueId } = useParams<{ venueId: string }>();
  const [searchParams] = useSearchParams();
  const activeVenueId = routeVenueId || searchParams.get('venueId') || '';

  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'SuperAdmin') ?? false;

  const [venues, setVenues] = useState<Venue[]>([]);
  const [currentVenue, setCurrentVenue] = useState<Venue | null>(null);
  const [halls, setHalls] = useState<Hall[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [hallSearchQuery, setHallSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadingHalls, setLoadingHalls] = useState(false);
  const [error, setError] = useState('');

  // Venue Modal state
  const [isVenueModalOpen, setIsVenueModalOpen] = useState(false);
  const [editingVenueId, setEditingVenueId] = useState<number | null>(null);
  const [venueForm, setVenueForm] = useState<VenueFormData>(emptyVenueForm);
  const [isSavingVenue, setIsSavingVenue] = useState(false);

  // Hall Modal state
  const [isHallModalOpen, setIsHallModalOpen] = useState(false);
  const [editingHallId, setEditingHallId] = useState<number | null>(null);
  const [hallForm, setHallForm] = useState<HallFormData>(emptyHallForm);
  const [isSavingHall, setIsSavingHall] = useState(false);

  // Load All Venues
  const loadVenues = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<Venue[]>('/api/venues');
      const data = response.data ?? [];
      setVenues(data);
      return data;
    } catch {
      setError('تعذر تحميل المواقع والقاعات من الخادم.');
      return [];
    } finally {
      setLoading(false);
    }
  }, []);

  // Load Halls for a specific venue
  const loadHallsForVenue = useCallback(async (vId: number) => {
    setLoadingHalls(true);
    try {
      const [hallsRes, venueRes] = await Promise.allSettled([
        api.get<Hall[]>(`/api/halls/venue/${vId}`),
        api.get<Venue>(`/api/venues/${vId}`)
      ]);

      if (hallsRes.status === 'fulfilled') {
        setHalls(hallsRes.value.data ?? []);
      } else {
        setHalls([]);
      }

      if (venueRes.status === 'fulfilled') {
        setCurrentVenue(venueRes.value.data);
      }
    } catch {
      showToast('تعذر تحميل بيانات القاعات للموقع المحدد.', 'danger');
    } finally {
      setLoadingHalls(false);
    }
  }, [showToast]);

  useEffect(() => {
    void (async () => {
      const venueList = await loadVenues();
      if (activeVenueId) {
        const parsedId = Number(activeVenueId);
        const match = venueList.find(v => v.venueID === parsedId);
        if (match) setCurrentVenue(match);
        void loadHallsForVenue(parsedId);
      } else {
        setCurrentVenue(null);
        setHalls([]);
      }
    })();
  }, [activeVenueId, loadVenues, loadHallsForVenue]);

  // Filtered Venues
  const filteredVenues = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return venues;
    return venues.filter(v =>
      v.name.toLowerCase().includes(q) ||
      v.city.toLowerCase().includes(q) ||
      v.address.toLowerCase().includes(q)
    );
  }, [venues, searchQuery]);

  // Filtered Halls
  const filteredHalls = useMemo(() => {
    const q = hallSearchQuery.trim().toLowerCase();
    if (!q) return halls;
    return halls.filter(h =>
      h.hallName.toLowerCase().includes(q)
    );
  }, [halls, hallSearchQuery]);

  // Statistics for active venue halls
  const hallStats = useMemo(() => {
    const totalHalls = halls.length;
    const totalArea = halls.reduce((acc, h) => acc + (h.areaSqM || 0), 0);
    const totalMaxBooths = halls.reduce((acc, h) => acc + (h.maxBooths || 0), 0);
    const totalBoothsRegistered = halls.reduce((acc, h) => acc + (h.boothsCount || 0), 0);
    return { totalHalls, totalArea, totalMaxBooths, totalBoothsRegistered };
  }, [halls]);

  // Open Create Venue Modal
  const openCreateVenue = () => {
    setEditingVenueId(null);
    setVenueForm(emptyVenueForm);
    setIsVenueModalOpen(true);
  };

  // Open Edit Venue Modal
  const openEditVenue = (v: Venue) => {
    setEditingVenueId(v.venueID);
    setVenueForm({
      name: v.name,
      address: v.address || '',
      city: v.city || '',
      country: v.country || '',
      totalCapacity: v.totalCapacity ? v.totalCapacity.toString() : '',
      mapImageURL: v.mapImageURL || '',
      isActive: v.isActive,
    });
    setIsVenueModalOpen(true);
  };

  // Submit Venue Form (Create / Update)
  const handleVenueSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!venueForm.name.trim()) {
      showToast('يرجى إدخال اسم الموقع.', 'warning');
      return;
    }

    setIsSavingVenue(true);
    try {
      const payload = {
        name: venueForm.name.trim(),
        address: venueForm.address.trim(),
        city: venueForm.city.trim(),
        country: venueForm.country.trim(),
        totalCapacity: venueForm.totalCapacity ? Number(venueForm.totalCapacity) : null,
        mapImageURL: venueForm.mapImageURL.trim(),
        isActive: venueForm.isActive,
      };

      if (editingVenueId) {
        await api.put(`/api/venues/${editingVenueId}`, payload);
        showToast('تم تحديث بيانات الموقع بنجاح.', 'success');
      } else {
        await api.post('/api/venues', payload);
        showToast('تمت إضافة الموقع بنجاح.', 'success');
      }

      setIsVenueModalOpen(false);
      await loadVenues();
      if (activeVenueId) {
        void loadHallsForVenue(Number(activeVenueId));
      }
    } catch {
      showToast('فشل حفظ بيانات الموقع. يرجى التحقق من المدخلات والصلاحيات.', 'danger');
    } finally {
      setIsSavingVenue(false);
    }
  };

  // Delete Venue
  const handleDeleteVenue = async (venue: Venue) => {
    if (!window.confirm(`هل أنت متأكد من حذف موقع «${venue.name}» وجميع القاعات التابعة له؟`)) return;

    try {
      await api.delete(`/api/venues/${venue.venueID}`);
      showToast('تم حذف الموقع بنجاح.', 'success');
      await loadVenues();
    } catch {
      showToast('تعذر حذف الموقع؛ قد يكون مرتبطاً بمعارض نشطة.', 'danger');
    }
  };

  // Open Create Hall Modal
  const openCreateHall = () => {
    if (!currentVenue && !activeVenueId) return;
    setEditingHallId(null);
    setHallForm(emptyHallForm);
    setIsHallModalOpen(true);
  };

  // Open Edit Hall Modal
  const openEditHall = (hall: Hall) => {
    setEditingHallId(hall.hallID);
    setHallForm({
      hallName: hall.hallName,
      areaSqM: hall.areaSqM ? hall.areaSqM.toString() : '',
      maxBooths: hall.maxBooths ? hall.maxBooths.toString() : '',
      floorPlanWidth: hall.floorPlanWidth ? hall.floorPlanWidth.toString() : '',
      floorPlanHeight: hall.floorPlanHeight ? hall.floorPlanHeight.toString() : '',
      isActive: hall.isActive,
    });
    setIsHallModalOpen(true);
  };

  // Submit Hall Form (Create / Update)
  const handleHallSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const vId = currentVenue?.venueID || Number(activeVenueId);
    if (!vId) return;
    if (!hallForm.hallName.trim()) {
      showToast('يرجى إدخال اسم القاعة.', 'warning');
      return;
    }

    const planWidth = Number(hallForm.floorPlanWidth);
    const planHeight = Number(hallForm.floorPlanHeight);
    if (!Number.isFinite(planWidth) || !Number.isFinite(planHeight) || planWidth < 5 || planHeight < 5) {
      showToast('يجب أن يكون عرض وطول القاعة 5 أمتار على الأقل.', 'warning');
      return;
    }

    setIsSavingHall(true);
    try {
      const payload = {
        venueID: vId,
        hallName: hallForm.hallName.trim(),
        areaSqM: hallForm.areaSqM ? Number(hallForm.areaSqM) : planWidth * planHeight,
        maxBooths: hallForm.maxBooths ? Number(hallForm.maxBooths) : null,
        floorPlanWidth: planWidth,
        floorPlanHeight: planHeight,
        isActive: hallForm.isActive,
      };

      if (editingHallId) {
        await api.put(`/api/halls/${editingHallId}`, payload);
        showToast('تم تحديث بيانات القاعة بنجاح.', 'success');
      } else {
        await api.post('/api/halls', payload);
        showToast('تمت إضافة القاعة بنجاح للموقع.', 'success');
      }

      setIsHallModalOpen(false);
      await loadHallsForVenue(vId);
      await loadVenues();
    } catch {
      showToast('فشل حفظ القاعة. يرجى التحقق من المدخلات.', 'danger');
    } finally {
      setIsSavingHall(false);
    }
  };

  // Delete Hall
  const handleDeleteHall = async (hall: Hall) => {
    if (!window.confirm(`هل أنت متأكد من حذف قاعة «${hall.hallName}»؟`)) return;

    try {
      await api.delete(`/api/halls/${hall.hallID}`);
      showToast('تم حذف القاعة بنجاح.', 'success');
      const vId = currentVenue?.venueID || Number(activeVenueId);
      if (vId) {
        await loadHallsForVenue(vId);
        await loadVenues();
      }
    } catch {
      showToast('تعذر حذف القاعة؛ قد تكون مرتبطة بأجنحة أو حجوزات.', 'danger');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* ━━━━━━━━━━━━━━━━━ VIEW 1: VENUE HALLS COMMAND CENTER ━━━━━━━━━━━━━━━━━ */}
      {activeVenueId ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          {/* Top Navigation Bar */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
            <NavLink
              to="/venues"
              className="btn-secondary"
              style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', fontSize: '13px', textDecoration: 'none' }}
            >
              <ArrowRight size={16} />
              <span>العودة لكافة المواقع</span>
            </NavLink>

            {venues.length > 1 && (
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>تبديل الموقع:</span>
                <select
                  className="input-field"
                  value={activeVenueId}
                  onChange={e => window.location.assign(`/venues/${e.target.value}/halls`)}
                  style={{ minWidth: '220px', padding: '6px 12px', fontSize: '13px' }}
                >
                  {venues.map(v => (
                    <option key={v.venueID} value={v.venueID.toString()}>
                      {v.name} ({v.city})
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>

          {/* Venue Banner Header Card */}
          {currentVenue && (
            <div
              className="card"
              style={{
                background: 'linear-gradient(135deg, #1e293b 0%, #0f172a 100%)',
                color: '#ffffff',
                padding: '24px 28px',
                borderRadius: 'var(--radius-xl)',
                boxShadow: '0 10px 25px -5px rgba(15, 23, 42, 0.3)',
                display: 'flex',
                flexDirection: 'column',
                gap: '16px'
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '16px', flexWrap: 'wrap' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                  <div
                    style={{
                      width: '48px',
                      height: '48px',
                      borderRadius: 'var(--radius-lg)',
                      background: 'var(--primary-gradient)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: '#ffffff',
                      boxShadow: 'var(--primary-glow)'
                    }}
                  >
                    <Building size={26} />
                  </div>
                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
                      <h1 style={{ fontSize: '22px', fontWeight: 800, color: '#ffffff', margin: 0 }}>{currentVenue.name}</h1>
                      <span className={`badge ${currentVenue.isActive ? 'badge-success' : 'badge-danger'}`}>
                        {currentVenue.isActive ? 'نشط ومتاح' : 'موقوف مؤقتاً'}
                      </span>
                    </div>
                    <p style={{ color: '#94a3b8', fontSize: '13px', marginTop: '4px', margin: 0 }}>
                      إدارة قاعات العرض، أبعاد المساحات، الطاقة الاستيعابية، والمخططات الهندسية.
                    </p>
                  </div>
                </div>

                <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                  {canManage && (
                    <NavLink
                      to={`/venues/${currentVenue.venueID}/designer`}
                      className="btn-primary"
                      style={{ display: 'flex', alignItems: 'center', gap: '6px', textDecoration: 'none' }}
                    >
                      <DraftingCompass size={17} />
                      <span>مخطط الموقع البصري</span>
                    </NavLink>
                  )}
                  {canManage && (
                    <button
                      className="btn-secondary"
                      onClick={() => openEditVenue(currentVenue)}
                      style={{
                        background: 'rgba(255,255,255,0.12)',
                        color: '#ffffff',
                        borderColor: 'rgba(255,255,255,0.2)',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '6px'
                      }}
                    >
                      <Pencil size={15} />
                      <span>تعديل بيانات الموقع</span>
                    </button>
                  )}

                  {canManage && (
                    <button
                      className="btn-primary"
                      onClick={openCreateHall}
                      style={{ display: 'flex', alignItems: 'center', gap: '6px' }}
                    >
                      <Plus size={17} />
                      <span>إضافة قاعة جديدة</span>
                    </button>
                  )}
                </div>
              </div>

              <div style={{ display: 'flex', gap: '24px', flexWrap: 'wrap', borderTop: '1px solid rgba(255,255,255,0.1)', paddingTop: '14px', fontSize: '13px', color: '#cbd5e1' }}>
                <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <MapPin size={15} style={{ color: '#38bdf8' }} />
                  المدينة: <strong>{currentVenue.city} {currentVenue.country ? `— ${currentVenue.country}` : ''}</strong>
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Building2 size={15} style={{ color: '#38bdf8' }} />
                  العنوان: <strong>{currentVenue.address || 'غير محدد'}</strong>
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Users size={15} style={{ color: '#38bdf8' }} />
                  السعة الإجمالية: <strong>{currentVenue.totalCapacity ? currentVenue.totalCapacity.toLocaleString() : 'غير محدد'} زائر</strong>
                </span>
                <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Layers size={15} style={{ color: '#38bdf8' }} />
                  إجمالي القاعات: <strong>{halls.length} قاعات</strong>
                </span>
              </div>
            </div>
          )}

          {/* KPI Summary Cards */}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px' }}>
            <div className="card" style={{ padding: '16px 20px' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>إجمالي قاعات الموقع</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--text-primary)' }}>{hallStats.totalHalls}</h2>
            </div>
            <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--primary)' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المساحة الكلية للقاعات</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--primary)' }}>{hallStats.totalArea.toLocaleString()} م²</h2>
            </div>
            <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--success)' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>أقصى استيعاب للأجنحة</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--success)' }}>{hallStats.totalMaxBooths} جناح</h2>
            </div>
            <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid #6366f1' }}>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>الأجنحة المسجلة حالياً</span>
              <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: '#6366f1' }}>{hallStats.totalBoothsRegistered} جناح</h2>
            </div>
          </div>

          {/* Search & Action Bar */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
            <div className="card" style={{ flex: 1, minWidth: '260px', position: 'relative', padding: 0 }}>
              <Search size={18} style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
              <input
                className="input-field"
                value={hallSearchQuery}
                onChange={e => setHallSearchQuery(e.target.value)}
                style={{ paddingRight: '44px', border: 0 }}
                placeholder="ابحث باسم القاعة ومواصفاتها…"
              />
            </div>

            {canManage && (
              <button className="btn-primary" onClick={openCreateHall}>
                <Plus size={17} /> إضافة قاعة جديدة
              </button>
            )}
          </div>

          {/* Halls Grid Cards */}
          {loadingHalls ? (
            <div className="card" style={{ textAlign: 'center', padding: '40px' }}>جاري تحميل قاعات الموقع…</div>
          ) : filteredHalls.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', padding: '40px', color: 'var(--text-secondary)' }}>
              لا توجد قاعات مضافة لهذا الموقع حتى الآن. اضغط على «إضافة قاعة جديدة» لإنشاء أول قاعة.
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '20px' }}>
              {filteredHalls.map(hall => (
                <article key={hall.hallID} className="card" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      <div
                        style={{
                          width: '38px',
                          height: '38px',
                          borderRadius: 'var(--radius-md)',
                          background: 'rgba(59, 130, 246, 0.1)',
                          color: 'var(--primary)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center'
                        }}
                      >
                        <Building2 size={20} />
                      </div>
                      <div>
                        <h3 style={{ fontSize: '16px', fontWeight: 800 }}>{hall.hallName}</h3>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>قاعة عرض رئيسية</span>
                      </div>
                    </div>
                    <span className={`badge ${hall.isActive ? 'badge-success' : 'badge-danger'}`}>
                      {hall.isActive ? 'نشطة' : 'موقوفة'}
                    </span>
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', fontSize: '13px' }}>
                    <div style={{ background: 'var(--bg-main)', padding: '10px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>المساحة الإجمالية</span>
                      <strong style={{ fontSize: '15px' }}>{hall.areaSqM ? `${hall.areaSqM.toLocaleString()} م²` : '—'}</strong>
                    </div>
                    <div style={{ background: 'var(--bg-main)', padding: '10px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>أقصى أجنحة</span>
                      <strong style={{ fontSize: '15px' }}>{hall.maxBooths ? `${hall.maxBooths} جناح` : '—'}</strong>
                    </div>
                    <div style={{ background: 'var(--bg-main)', padding: '10px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>الأبعاد (العرض × الطول)</span>
                      <strong>{hall.floorPlanWidth && hall.floorPlanHeight ? `${hall.floorPlanWidth} × ${hall.floorPlanHeight} م` : '—'}</strong>
                    </div>
                    <div style={{ background: 'var(--bg-main)', padding: '10px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>الأجنحة المسجلة</span>
                      <strong>{hall.boothsCount} جناح</strong>
                    </div>
                  </div>

                  {canManage && (
                    <div style={{ display: 'flex', gap: '8px', marginTop: 'auto', borderTop: '1px solid var(--border-main)', paddingTop: '12px' }}>
                      <button
                        className="btn-secondary"
                        onClick={() => openEditHall(hall)}
                        style={{ flex: 1, padding: '9px 10px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', fontSize: '12px' }}
                      >
                        <Edit2 size={14} /> تعديل
                      </button>
                      <button
                        className="btn-icon"
                        title="حذف القاعة"
                        style={{ color: 'var(--danger)' }}
                        onClick={() => void handleDeleteHall(hall)}
                      >
                        <Trash2 size={16} />
                      </button>
                    </div>
                  )}
                </article>
              ))}
            </div>
          )}
        </div>
      ) : (
        /* ━━━━━━━━━━━━━━━━━ VIEW 2: ALL VENUES DIRECTORY ━━━━━━━━━━━━━━━━━ */
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          {/* Header */}
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <MapPin size={24} style={{ color: 'var(--primary)' }} />
                <h1 style={{ fontSize: '24px', fontWeight: 800 }}>المواقع وقاعات المعارض</h1>
              </div>
              <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>
                اختر الموقع للدخول المباشر إلى إدارة قاعاته، مساحاته، ومخططاته الهندسية.
              </p>
            </div>
            {canManage && (
              <button className="btn-primary" onClick={openCreateVenue}>
                <Plus size={18} /> موقع جديد
              </button>
            )}
          </div>

          {/* Search Bar */}
          <div className="card" style={{ position: 'relative' }}>
            <Search size={18} style={{ position: 'absolute', right: '28px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
            <input
              className="input-field"
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
              placeholder="ابحث باسم الموقع أو المدينة أو العنوان…"
              style={{ paddingRight: '42px' }}
            />
          </div>

          {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

          {/* Venues Cards Grid */}
          {loading ? (
            <div className="card" style={{ textAlign: 'center', padding: '40px' }}>جاري تحميل المواقع…</div>
          ) : filteredVenues.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', padding: '40px', color: 'var(--text-secondary)' }}>
              لا توجد مواقع مسجلة. اضغط على «موقع جديد» لإضافة مركز معارض.
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: '20px' }}>
              {filteredVenues.map(venue => (
                <article
                  key={venue.venueID}
                  className="card"
                  style={{
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '16px',
                    transition: 'all 0.2s',
                    position: 'relative'
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '12px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      <div
                        style={{
                          width: '42px',
                          height: '42px',
                          borderRadius: 'var(--radius-lg)',
                          background: 'var(--primary-gradient)',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          color: '#ffffff',
                          boxShadow: 'var(--primary-glow)',
                          flexShrink: 0
                        }}
                      >
                        <Building size={22} />
                      </div>
                      <div>
                        <NavLink to={`/venues/${venue.venueID}/halls`} style={{ textDecoration: 'none' }}>
                          <h2 style={{ fontSize: '17px', fontWeight: 800, color: 'var(--text-primary)', cursor: 'pointer' }}>
                            {venue.name}
                          </h2>
                        </NavLink>
                        <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                          {venue.city} {venue.country ? `· ${venue.country}` : ''}
                        </span>
                      </div>
                    </div>
                    <span className={`badge ${venue.isActive ? 'badge-success' : 'badge-danger'}`}>
                      {venue.isActive ? 'نشط ومتاح' : 'موقوف'}
                    </span>
                  </div>

                  <p style={{ fontSize: '13px', color: 'var(--text-secondary)', margin: 0 }}>
                    {venue.address || 'لا يوجد عنوان تفصيلي مسجل'}
                  </p>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '10px', fontSize: '13px', color: 'var(--text-secondary)', background: 'var(--bg-main)', padding: '10px 12px', borderRadius: 'var(--radius-md)' }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <Layers size={15} style={{ color: 'var(--primary)' }} />
                      القاعات: <strong>{venue.hallsCount} قاعات</strong>
                    </span>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                      <Users size={15} style={{ color: 'var(--primary)' }} />
                      السعة: <strong>{venue.totalCapacity ? venue.totalCapacity.toLocaleString() : 'غير محدد'}</strong>
                    </span>
                  </div>

                  {/* Primary Action Button to Venue Halls Hub */}
                  <NavLink
                    to={`/venues/${venue.venueID}/halls`}
                    className="btn-primary"
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      gap: '8px',
                      fontSize: '13px',
                      padding: '9px 14px',
                      textDecoration: 'none',
                      marginTop: '4px'
                    }}
                  >
                    <Layers size={16} />
                    <span>إدارة قاعات الموقع ({venue.hallsCount} قاعات)</span>
                  </NavLink>

                  {/* Secondary Action Row */}
                  <div style={{ display: 'flex', gap: '8px', marginTop: 'auto' }}>
                    {canManage && (
                      <button
                        className="btn-secondary"
                        onClick={() => openEditVenue(venue)}
                        style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '6px', fontSize: '13px' }}
                      >
                        <Pencil size={14} /> تعديل
                      </button>
                    )}
                    {canManage && (
                      <button
                        className="btn-icon"
                        title="حذف الموقع"
                        style={{ color: 'var(--danger)' }}
                        onClick={() => void handleDeleteVenue(venue)}
                      >
                        <Trash2 size={16} />
                      </button>
                    )}
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      )}

      {/* ━━━━━━━━━━━━━━━━━ MODAL: CREATE / EDIT VENUE ━━━━━━━━━━━━━━━━━ */}
      {isVenueModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleVenueSubmit} style={{ width: 'min(560px, 100%)', display: 'grid', gap: '14px', maxHeight: '90vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>{editingVenueId ? 'تعديل بيانات الموقع' : 'إضافة موقع جديد'}</h2>
              <button type="button" className="btn-icon" onClick={() => setIsVenueModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم الموقع / المركز المعرضي *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: مركز طرابلس الدولي للمعارض"
                value={venueForm.name}
                onChange={e => setVenueForm(prev => ({ ...prev, name: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">المدينة</label>
                <input
                  className="input-field"
                  placeholder="طرابلس"
                  value={venueForm.city}
                  onChange={e => setVenueForm(prev => ({ ...prev, city: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الدولة</label>
                <input
                  className="input-field"
                  placeholder="ليبيا"
                  value={venueForm.country}
                  onChange={e => setVenueForm(prev => ({ ...prev, country: e.target.value }))}
                />
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">العنوان التفصيلي</label>
              <input
                className="input-field"
                placeholder="شارع عمر المختار، طرابلس"
                value={venueForm.address}
                onChange={e => setVenueForm(prev => ({ ...prev, address: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">السعة القصوى للزوار</label>
                <input
                  className="input-field"
                  type="number"
                  min="0"
                  placeholder="10000"
                  value={venueForm.totalCapacity}
                  onChange={e => setVenueForm(prev => ({ ...prev, totalCapacity: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الحالة التشغيلية</label>
                <select
                  className="input-field"
                  value={venueForm.isActive ? 'true' : 'false'}
                  onChange={e => setVenueForm(prev => ({ ...prev, isActive: e.target.value === 'true' }))}
                >
                  <option value="true">نشط ومتاح</option>
                  <option value="false">موقوف مؤقتاً</option>
                </select>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '10px', marginTop: '8px' }}>
              <button className="btn-primary" type="submit" disabled={isSavingVenue} style={{ flex: 1 }}>
                {isSavingVenue ? 'جاري الحفظ…' : editingVenueId ? 'تحديث الموقع' : 'حفظ الموقع الجديد'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setIsVenueModalOpen(false)}>
                إلغاء
              </button>
            </div>
          </form>
        </div>
      )}

      {/* ━━━━━━━━━━━━━━━━━ MODAL: CREATE / EDIT HALL ━━━━━━━━━━━━━━━━━ */}
      {isHallModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleHallSubmit} style={{ width: 'min(520px, 100%)', display: 'grid', gap: '14px', maxHeight: '90vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h2>{editingHallId ? 'تعديل بيانات القاعة' : 'إضافة قاعة جديدة'}</h2>
                <small style={{ color: 'var(--text-secondary)' }}>للموقع: {currentVenue?.name}</small>
              </div>
              <button type="button" className="btn-icon" onClick={() => setIsHallModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم القاعة *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: القاعة الكبرى (Hall 1)"
                value={hallForm.hallName}
                onChange={e => setHallForm(prev => ({ ...prev, hallName: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">المساحة الإجمالية (م²)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  min="0"
                  placeholder="2000"
                  value={hallForm.areaSqM}
                  onChange={e => setHallForm(prev => ({ ...prev, areaSqM: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">أقصى عدد للأجنحة</label>
                <input
                  className="input-field"
                  type="number"
                  min="0"
                  placeholder="80"
                  value={hallForm.maxBooths}
                  onChange={e => setHallForm(prev => ({ ...prev, maxBooths: e.target.value }))}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">عرض القاعة (بالمتر) *</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  min="5"
                  placeholder="50"
                  value={hallForm.floorPlanWidth}
                  onChange={e => setHallForm(prev => ({ ...prev, floorPlanWidth: e.target.value }))}
                  required
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">طول القاعة (بالمتر) *</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  min="5"
                  placeholder="40"
                  value={hallForm.floorPlanHeight}
                  onChange={e => setHallForm(prev => ({ ...prev, floorPlanHeight: e.target.value }))}
                  required
                />
              </div>
            </div>

            <small style={{ color: 'var(--text-secondary)', lineHeight: 1.7 }}>
              ستظهر القاعة تلقائياً داخل مخطط الموقع بحجم نسبي مطابق لهذه الأبعاد.
            </small>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الحالة</label>
              <select
                className="input-field"
                value={hallForm.isActive ? 'true' : 'false'}
                onChange={e => setHallForm(prev => ({ ...prev, isActive: e.target.value === 'true' }))}
              >
                <option value="true">نشطة وجاهزة للاستخدام</option>
                <option value="false">تحت الصيانة / موقوفة</option>
              </select>
            </div>

            <div style={{ display: 'flex', gap: '10px', marginTop: '8px' }}>
              <button className="btn-primary" type="submit" disabled={isSavingHall} style={{ flex: 1 }}>
                {isSavingHall ? 'جاري الحفظ…' : editingHallId ? 'تحديث القاعة' : 'إضافة القاعة'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setIsHallModalOpen(false)}>
                إلغاء
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};
