import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  CheckCircle,
  FileCheck2,
  Plus,
  Search,
  Store,
  User,
  X,
  XCircle
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface ExhibitionSummary {
  exhibitionID: number;
  name: string;
}

interface ExhibitorItem {
  exhibitorID: number;
  companyName: string;
}

interface BoothItem {
  boothID: number;
  boothNumber: string;
  hallName?: string;
  status: string;
  currentAreaSqM: number;
}

interface ReservationSummary {
  reservationID: number;
  exhibitorName: string;
  boothNumber: string;
  exhibitionName: string;
  totalAmount: number;
  currencyCode: string;
  status: string;
  reservationDate: string;
}

interface PagedResult<T> {
  items: T[];
}

interface ReservationFormData {
  exhibitionID: string;
  exhibitorID: string;
  boothID: string;
  reservationDate: string;
  notes: string;
}

const emptyReservationForm: ReservationFormData = {
  exhibitionID: '',
  exhibitorID: '',
  boothID: '',
  reservationDate: new Date().toISOString().split('T')[0],
  notes: '',
};

export const ReservationsPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'EventManager') ?? false;

  const [exhibitions, setExhibitions] = useState<ExhibitionSummary[]>([]);
  const [selectedExhibitionID, setSelectedExhibitionID] = useState('');
  const [reservations, setReservations] = useState<ReservationSummary[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // New Reservation Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [exhibitors, setExhibitors] = useState<ExhibitorItem[]>([]);
  const [booths, setBooths] = useState<BoothItem[]>([]);
  const [form, setForm] = useState<ReservationFormData>(emptyReservationForm);
  const [isSaving, setIsSaving] = useState(false);

  // 1. Load Exhibitions
  useEffect(() => {
    const loadExhibitions = async () => {
      try {
        const response = await api.get<PagedResult<ExhibitionSummary>>('/api/exhibitions?page=1&pageSize=100');
        const items = response.data.items ?? [];
        setExhibitions(items);
        if (items.length > 0) {
          setSelectedExhibitionID(items[0].exhibitionID.toString());
          setForm(prev => ({ ...prev, exhibitionID: items[0].exhibitionID.toString() }));
        }
      } catch {
        setError('تعذر تحميل قائمة المعارض.');
        setIsLoading(false);
      }
    };
    void loadExhibitions();
  }, []);

  // 2. Load Reservations for Selected Exhibition
  const loadReservations = useCallback(async () => {
    if (!selectedExhibitionID) {
      setReservations([]);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setError('');
    try {
      const response = await api.get<PagedResult<ReservationSummary>>(
        `/api/reservations/exhibition/${selectedExhibitionID}?page=1&pageSize=100`,
      );
      setReservations(response.data.items ?? []);
    } catch {
      setError('تعذر تحميل حجوزات المعرض من الخادم.');
    } finally {
      setIsLoading(false);
    }
  }, [selectedExhibitionID]);

  useEffect(() => {
    void loadReservations();
  }, [loadReservations]);

  // Load Exhibitors and Booths when opening modal
  const openNewReservationModal = async () => {
    setForm(prev => ({
      ...emptyReservationForm,
      exhibitionID: selectedExhibitionID || (exhibitions[0]?.exhibitionID.toString() ?? ''),
    }));
    setIsModalOpen(true);

    try {
      const [exhRes, boothsRes] = await Promise.allSettled([
        api.get<{ items: ExhibitorItem[] }>('/api/exhibitors?page=1&pageSize=100'),
        api.get<{ items: BoothItem[] }>('/api/booths?pageNumber=1&pageSize=150'),
      ]);

      if (exhRes.status === 'fulfilled') {
        const exList = exhRes.value.data.items ?? [];
        setExhibitors(exList);
        if (exList.length > 0) {
          setForm(prev => ({ ...prev, exhibitorID: exList[0].exhibitorID.toString() }));
        }
      }

      if (boothsRes.status === 'fulfilled') {
        const bList = boothsRes.value.data.items ?? [];
        setBooths(bList);
        if (bList.length > 0) {
          setForm(prev => ({ ...prev, boothID: bList[0].boothID.toString() }));
        }
      }
    } catch {
      showToast('تعذر تحميل قائمة العارضين أو الأجنحة.', 'warning');
    }
  };

  // Submit New Reservation
  const handleCreateReservation = async (e: FormEvent) => {
    e.preventDefault();
    if (!form.exhibitionID || !form.exhibitorID || !form.boothID) {
      showToast('يرجى تحديد المعرض والعارض والجناح.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      await api.post('/api/reservations', {
        exhibitionID: Number(form.exhibitionID),
        exhibitorID: Number(form.exhibitorID),
        boothID: Number(form.boothID),
        reservationDate: new Date(form.reservationDate).toISOString(),
        notes: form.notes.trim(),
      });

      showToast('تم إنشاء طلب الحجز بنجاح.', 'success');
      setIsModalOpen(false);
      await loadReservations();
    } catch {
      showToast('فشل إنشاء الحجز. قد يكون الجناح محجوزاً بالفعل.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  // Change Status (Approve / Cancel)
  const changeStatus = async (reservationID: number, action: 'approve' | 'cancel') => {
    const isApprove = action === 'approve';
    if (!window.confirm(`هل أنت متأكد من ${isApprove ? 'اعتماد' : 'إلغاء'} هذا الحجز؟`)) return;

    try {
      await api.patch(`/api/reservations/${reservationID}/${action}`);
      showToast(isApprove ? 'تم اعتماد الحجز بنجاح في الخادم.' : 'تم إلغاء الحجز في الخادم.', 'success');
      await loadReservations();
    } catch {
      showToast('تعذر تحديث حالة الحجز. تحقق من الصلاحيات وحالة الحجز الحالية.', 'danger');
    }
  };

  // Filtered Reservations
  const filteredReservations = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return reservations.filter(res => {
      const matchesStatus = statusFilter === 'All' || res.status.toLowerCase() === statusFilter.toLowerCase();
      const matchesSearch = !q ||
        res.exhibitorName.toLowerCase().includes(q) ||
        res.boothNumber.toLowerCase().includes(q) ||
        res.reservationID.toString().includes(q);
      return matchesStatus && matchesSearch;
    });
  }, [reservations, statusFilter, searchQuery]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <FileCheck2 size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>طلبات الحجز وعقود الأجنحة</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>إدارة ومتابعة طلبات حجز الأجنحة، الاعتماد، والربط مع الفواتير.</p>
        </div>

        {canManage && (
          <button className="btn-primary" onClick={openNewReservationModal} disabled={exhibitions.length === 0}>
            <Plus size={18} /> طلب حجز جديد
          </button>
        )}
      </div>

      {/* Filters Bar */}
      <div style={{ display: 'flex', gap: '14px', flexWrap: 'wrap', alignItems: 'center' }}>
        <div style={{ minWidth: '240px' }}>
          <select
            className="input-field"
            value={selectedExhibitionID}
            onChange={e => setSelectedExhibitionID(e.target.value)}
          >
            {exhibitions.length === 0 && <option value="">لا توجد معارض مسجلة</option>}
            {exhibitions.map(ex => (
              <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>{ex.name}</option>
            ))}
          </select>
        </div>

        <div className="card" style={{ flex: 1, minWidth: '260px', position: 'relative', padding: 0 }}>
          <Search size={18} style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            className="input-field"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            style={{ paddingRight: '44px', border: 0 }}
            placeholder="ابحث باسم العارض أو رقم الجناح أو رقم الطلب…"
          />
        </div>

        <div style={{ minWidth: '160px' }}>
          <select
            className="input-field"
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
          >
            <option value="All">جميع الحالات</option>
            <option value="Pending">قيد المراجعة (Pending)</option>
            <option value="Approved">معتمد (Approved)</option>
            <option value="Cancelled">ملغي (Cancelled)</option>
          </select>
        </div>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Table Card */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>رقم الطلب</th>
                <th>الشركة العارضة</th>
                <th>الجناح المحجوز</th>
                <th>تاريخ الحجز</th>
                <th>قيمة العقد</th>
                <th>الحالة</th>
                <th>الإجراءات</th>
              </tr>
            </thead>
            <tbody>
              {filteredReservations.map(reservation => (
                <tr key={reservation.reservationID}>
                  <td><strong>#{reservation.reservationID}</strong></td>
                  <td>{reservation.exhibitorName}</td>
                  <td><span className="badge badge-info">{reservation.boothNumber}</span></td>
                  <td>{new Date(reservation.reservationDate).toLocaleDateString('ar-LY')}</td>
                  <td><strong>{reservation.totalAmount.toLocaleString()} {reservation.currencyCode}</strong></td>
                  <td>
                    <span className={`badge ${
                      reservation.status.toLowerCase() === 'approved' ? 'badge-success' :
                      reservation.status.toLowerCase() === 'pending' ? 'badge-warning' : 'badge-danger'
                    }`}>
                      {reservation.status}
                    </span>
                  </td>
                  <td>
                    {canManage && (
                      <div style={{ display: 'flex', gap: '8px' }}>
                        {reservation.status.toLowerCase() === 'pending' && (
                          <button
                            className="btn-primary"
                            style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                            onClick={() => void changeStatus(reservation.reservationID, 'approve')}
                          >
                            <CheckCircle size={13} /> اعتماد
                          </button>
                        )}
                        {reservation.status.toLowerCase() !== 'cancelled' && (
                          <button
                            className="btn-secondary"
                            style={{ height: '30px', padding: '0 10px', fontSize: '12px', color: 'var(--danger)' }}
                            onClick={() => void changeStatus(reservation.reservationID, 'cancel')}
                          >
                            <XCircle size={13} /> إلغاء
                          </button>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
              {!isLoading && filteredReservations.length === 0 && (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '30px' }}>لا توجد طلبات حجز مطابقة.</td></tr>
              )}
              {isLoading && (
                <tr><td colSpan={7} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: New Reservation */}
      {isModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleCreateReservation} style={{ width: 'min(520px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>طلب حجز جناح جديد</h2>
              <button type="button" className="btn-icon" onClick={() => setIsModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">المعرض *</label>
              <select
                className="input-field"
                required
                value={form.exhibitionID}
                onChange={e => setForm(prev => ({ ...prev, exhibitionID: e.target.value }))}
              >
                {exhibitions.map(ex => (
                  <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>{ex.name}</option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الشركة العارضة *</label>
              <select
                className="input-field"
                required
                value={form.exhibitorID}
                onChange={e => setForm(prev => ({ ...prev, exhibitorID: e.target.value }))}
              >
                <option value="">اختر الشركة العارضة</option>
                {exhibitors.map(ex => (
                  <option key={ex.exhibitorID} value={ex.exhibitorID.toString()}>{ex.companyName}</option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الجناح المطلوب *</label>
              <select
                className="input-field"
                required
                value={form.boothID}
                onChange={e => setForm(prev => ({ ...prev, boothID: e.target.value }))}
              >
                <option value="">اختر الجناح</option>
                {booths.map(b => (
                  <option key={b.boothID} value={b.boothID.toString()}>
                    جناح {b.boothNumber} ({b.currentAreaSqM} م²) — {b.status}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">تاريخ الحجز</label>
              <input
                className="input-field"
                type="date"
                required
                value={form.reservationDate}
                onChange={e => setForm(prev => ({ ...prev, reservationDate: e.target.value }))}
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">ملاحظات أو شروط خاصة</label>
              <textarea
                className="input-field"
                rows={2}
                placeholder="أي اشتراطات كهرباء أو تجهيزات إضافية…"
                value={form.notes}
                onChange={e => setForm(prev => ({ ...prev, notes: e.target.value }))}
              />
            </div>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري إنشاء الحجز…' : 'تأكيد طلب الحجز'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
