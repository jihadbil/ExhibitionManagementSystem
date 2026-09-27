import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Building,
  Building2,
  Edit2,
  Layers,
  MapPin,
  Maximize2,
  Plus,
  Search,
  Trash2,
  Users,
  X
} from 'lucide-react';
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
}

const emptyHallForm: HallFormData = {
  hallName: '',
  areaSqM: '',
  maxBooths: '',
  floorPlanWidth: '',
  floorPlanHeight: '',
};

export const VenuesPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin') ?? false;

  const [venues, setVenues] = useState<Venue[]>([]);
  const [selectedVenue, setSelectedVenue] = useState<Venue | null>(null);
  const [halls, setHalls] = useState<Hall[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
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

  // Load Venues
  const loadVenues = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<Venue[]>('/api/venues');
      const data = response.data ?? [];
      setVenues(data);
      if (data.length > 0) {
        setSelectedVenue(prev => {
          if (!prev) return data[0];
          const found = data.find(v => v.venueID === prev.venueID);
          return found || data[0];
        });
      } else {
        setSelectedVenue(null);
      }
    } catch {
      setError('تعذر تحميل المواقع والقاعات من الخادم.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadVenues();
  }, [loadVenues]);

  // Load Halls for selected venue
  const loadHalls = useCallback(async (venueId: number) => {
    setLoadingHalls(true);
    try {
      const response = await api.get<Hall[]>(`/api/halls/venue/${venueId}`);
      setHalls(response.data ?? []);
    } catch {
      showToast('تعذر تحميل القاعات للموقع المحدد.', 'danger');
    } finally {
      setLoadingHalls(false);
    }
  }, [showToast]);

  useEffect(() => {
    if (selectedVenue) {
      void loadHalls(selectedVenue.venueID);
    } else {
      setHalls([]);
    }
  }, [selectedVenue, loadHalls]);

  const filteredVenues = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return venues;
    return venues.filter(v =>
      v.name.toLowerCase().includes(q) ||
      v.city.toLowerCase().includes(q) ||
      v.address.toLowerCase().includes(q)
    );
  }, [venues, searchQuery]);

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
      if (selectedVenue?.venueID === venue.venueID) {
        setSelectedVenue(null);
      }
      await loadVenues();
    } catch {
      showToast('تعذر حذف الموقع؛ قد يكون مرتبطاً بمعارض نشطة.', 'danger');
    }
  };

  // Open Create Hall Modal
  const openCreateHall = () => {
    if (!selectedVenue) return;
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
    });
    setIsHallModalOpen(true);
  };

  // Submit Hall Form (Create / Update)
  const handleHallSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!selectedVenue) return;
    if (!hallForm.hallName.trim()) {
      showToast('يرجى إدخال اسم القاعة.', 'warning');
      return;
    }

    setIsSavingHall(true);
    try {
      const payload = {
        venueID: selectedVenue.venueID,
        hallName: hallForm.hallName.trim(),
        areaSqM: hallForm.areaSqM ? Number(hallForm.areaSqM) : null,
        maxBooths: hallForm.maxBooths ? Number(hallForm.maxBooths) : null,
        floorPlanWidth: hallForm.floorPlanWidth ? Number(hallForm.floorPlanWidth) : null,
        floorPlanHeight: hallForm.floorPlanHeight ? Number(hallForm.floorPlanHeight) : null,
        floorPlanJSON: '',
      };

      if (editingHallId) {
        await api.put(`/api/halls/${editingHallId}`, payload);
        showToast('تم تحديث بيانات القاعة بنجاح.', 'success');
      } else {
        await api.post('/api/halls', payload);
        showToast('تمت إضافة القاعة بنجاح.', 'success');
      }

      setIsHallModalOpen(false);
      await loadHalls(selectedVenue.venueID);
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
      if (selectedVenue) {
        await loadHalls(selectedVenue.venueID);
        await loadVenues();
      }
    } catch {
      showToast('تعذر حذف القاعة؛ قد تكون مرتبطة بأجنحة أو حجوزات.', 'danger');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <MapPin size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>المواقع والقاعات</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>إدارة مراكز المعارض وقاعات العرض وأبعاد المساحات.</p>
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
          placeholder="ابحث باسم الموقع أو المدينة أو العنوان"
          style={{ paddingRight: '42px' }}
        />
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Main Split Grid: Venues List on Left/Right and Hall Details on the other */}
      <div style={{ display: 'grid', gridTemplateColumns: 'minmax(320px, 1fr) minmax(350px, 1.3fr)', gap: '24px' }}>
        {/* Venues Column */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <h2 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Building size={18} style={{ color: 'var(--primary)' }} />
            مواقع المعارض ({filteredVenues.length})
          </h2>

          {loading ? (
            <div className="card" style={{ textAlign: 'center' }}>جاري تحميل المواقع…</div>
          ) : filteredVenues.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', color: 'var(--text-secondary)' }}>لا توجد مواقع مسجلة.</div>
          ) : (
            filteredVenues.map(venue => {
              const isSelected = selectedVenue?.venueID === venue.venueID;
              return (
                <article
                  key={venue.venueID}
                  className="card"
                  onClick={() => setSelectedVenue(venue)}
                  style={{
                    cursor: 'pointer',
                    borderColor: isSelected ? 'var(--primary)' : 'var(--border-main)',
                    boxShadow: isSelected ? '0 0 0 1px var(--primary)' : undefined,
                    transition: 'all 0.2s',
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '12px'
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                    <div>
                      <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>{venue.name}</h3>
                      <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                        {venue.city} {venue.country ? `· ${venue.country}` : ''}
                      </p>
                    </div>
                    <span className={`badge ${venue.isActive ? 'badge-success' : 'badge-danger'}`}>
                      {venue.isActive ? 'نشط' : 'موقوف'}
                    </span>
                  </div>

                  <p style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
                    {venue.address || 'لا يوجد عنوان تفصيلي'}
                  </p>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '16px', fontSize: '12px', color: 'var(--text-secondary)', borderTop: '1px solid var(--border-main)', paddingTop: '10px' }}>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <Layers size={14} style={{ color: 'var(--primary)' }} /> {venue.hallsCount} قاعات
                    </span>
                    <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <Users size={14} style={{ color: 'var(--primary)' }} /> السعة: {venue.totalCapacity?.toLocaleString() || 'غير محدد'}
                    </span>
                  </div>

                  {canManage && (
                    <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: '4px' }} onClick={e => e.stopPropagation()}>
                      <button
                        className="btn-icon"
                        title="تعديل الموقع"
                        onClick={() => openEditVenue(venue)}
                      >
                        <Edit2 size={15} />
                      </button>
                      <button
                        className="btn-icon"
                        title="حذف الموقع"
                        style={{ color: 'var(--danger)' }}
                        onClick={() => void handleDeleteVenue(venue)}
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  )}
                </article>
              );
            })
          )}
        </div>

        {/* Halls Column for Selected Venue */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h2 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Layers size={18} style={{ color: 'var(--primary)' }} />
              قاعات {selectedVenue ? `«${selectedVenue.name}»` : ''} ({halls.length})
            </h2>
            {canManage && selectedVenue && (
              <button className="btn-secondary" onClick={openCreateHall} style={{ height: '34px', padding: '0 12px', fontSize: '13px' }}>
                <Plus size={15} /> إضافة قاعة
              </button>
            )}
          </div>

          {!selectedVenue ? (
            <div className="card" style={{ textAlign: 'center', padding: '40px', color: 'var(--text-secondary)' }}>
              اختر موقعاً من القائمة لعرض القاعات التابعة له.
            </div>
          ) : loadingHalls ? (
            <div className="card" style={{ textAlign: 'center' }}>جاري تحميل القاعات…</div>
          ) : halls.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', padding: '30px', color: 'var(--text-secondary)' }}>
              لا توجد قاعات مضافة لهذا الموقع حتى الآن.
            </div>
          ) : (
            <div style={{ display: 'grid', gap: '14px' }}>
              {halls.map(hall => (
                <article key={hall.hallID} className="card" style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <Building2 size={18} style={{ color: 'var(--primary)' }} />
                      <h3 style={{ fontSize: '15px', fontWeight: 700 }}>{hall.hallName}</h3>
                    </div>
                    {canManage && (
                      <div style={{ display: 'flex', gap: '6px' }}>
                        <button className="btn-icon" title="تعديل القاعة" onClick={() => openEditHall(hall)}>
                          <Edit2 size={14} />
                        </button>
                        <button className="btn-icon" title="حذف القاعة" style={{ color: 'var(--danger)' }} onClick={() => void handleDeleteHall(hall)}>
                          <Trash2 size={14} />
                        </button>
                      </div>
                    )}
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))', gap: '10px', fontSize: '13px', color: 'var(--text-secondary)' }}>
                    <div style={{ background: 'var(--bg-main)', padding: '8px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>المساحة</span>
                      <strong>{hall.areaSqM ? `${hall.areaSqM} م²` : '—'}</strong>
                    </div>
                    <div style={{ background: 'var(--bg-main)', padding: '8px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>أقصى أجنحة</span>
                      <strong>{hall.maxBooths ? `${hall.maxBooths} جناح` : '—'}</strong>
                    </div>
                    <div style={{ background: 'var(--bg-main)', padding: '8px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>الأبعاد (طول×عرض)</span>
                      <strong>{hall.floorPlanWidth && hall.floorPlanHeight ? `${hall.floorPlanWidth} × ${hall.floorPlanHeight} م` : '—'}</strong>
                    </div>
                    <div style={{ background: 'var(--bg-main)', padding: '8px 12px', borderRadius: 'var(--radius-md)' }}>
                      <span style={{ fontSize: '11px', display: 'block', color: 'var(--text-muted)' }}>الأجنحة المسجلة</span>
                      <strong>{hall.boothsCount} جناح</strong>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Modal: Create / Edit Venue */}
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
                placeholder="مثال: معرض طرابلس الدولي"
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

            <button className="btn-primary" type="submit" disabled={isSavingVenue} style={{ marginTop: '8px' }}>
              {isSavingVenue ? 'جاري الحفظ…' : editingVenueId ? 'تحديث الموقع' : 'حفظ الموقع الجديد'}
            </button>
          </form>
        </div>
      )}

      {/* Modal: Create / Edit Hall */}
      {isHallModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleHallSubmit} style={{ width: 'min(520px, 100%)', display: 'grid', gap: '14px', maxHeight: '90vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h2>{editingHallId ? 'تعديل بيانات القاعة' : 'إضافة قاعة جديدة'}</h2>
                <small style={{ color: 'var(--text-secondary)' }}>للموقع: {selectedVenue?.name}</small>
              </div>
              <button type="button" className="btn-icon" onClick={() => setIsHallModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم القاعة *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: القاعة الرئيسية (القاعة الكبرى)"
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
                <label className="form-label">عرض المخطط (بالمتر)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  min="0"
                  placeholder="50"
                  value={hallForm.floorPlanWidth}
                  onChange={e => setHallForm(prev => ({ ...prev, floorPlanWidth: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">طول المخطط (بالمتر)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  min="0"
                  placeholder="40"
                  value={hallForm.floorPlanHeight}
                  onChange={e => setHallForm(prev => ({ ...prev, floorPlanHeight: e.target.value }))}
                />
              </div>
            </div>

            <button className="btn-primary" type="submit" disabled={isSavingHall} style={{ marginTop: '8px' }}>
              {isSavingHall ? 'جاري الحفظ…' : editingHallId ? 'تحديث القاعة' : 'إضافة القاعة'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
