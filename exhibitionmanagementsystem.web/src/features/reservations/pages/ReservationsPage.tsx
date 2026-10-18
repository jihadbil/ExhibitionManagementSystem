import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  CheckCircle,
  FileCheck2,
  Plus,
  Search,
  X,
  XCircle
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getAllItems } from '../../../services/pagination';

interface ExhibitionSummary {
  exhibitionID: number;
  name: string;
  entryCurrency?: string;
}

interface ExhibitorItem {
  exhibitorID: number;
  companyName: string;
  exhibitorCategory: string;
  preferredCurrencyCode: string;
  isActive: boolean;
}

interface BoothItem {
  boothID: number;
  boothNumber: string;
  hallName?: string;
  status: string;
  currentAreaSqM: number;
  boothType: string;
}

interface ServiceItem {
  serviceID: number;
  serviceName: string;
  category: string;
  unit: string;
  defaultPrice?: number | null;
  defaultCurrencyCode: string;
  isMandatory: boolean;
  isActive: boolean;
}

interface CurrencyOption {
  currencyCode: string;
  currencyName: string;
  symbol: string;
  isActive: boolean;
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

interface CreatedReservation {
  reservationID: number;
  services?: Array<{ serviceID: number }>;
}

interface PagedResult<T> {
  items: T[];
  totalCount?: number;
}

interface ReservationFormData {
  exhibitionID: string;
  exhibitorID: string;
  boothID: string;
  boothTypeSelected: string;
  requestedAreaSqM: string;
  exhibitorCategory: string;
  currencyCode: string;
  logisticNotes: string;
}

const emptyReservationForm: ReservationFormData = {
  exhibitionID: '',
  exhibitorID: '',
  boothID: '',
  boothTypeSelected: 'Standard',
  requestedAreaSqM: '',
  exhibitorCategory: 'Local',
  currencyCode: 'LYD',
  logisticNotes: '',
};

const normalizeIntegerInput = (value: string): string => value
  .replace(/[٠-٩]/g, digit => String('٠١٢٣٤٥٦٧٨٩'.indexOf(digit)))
  .replace(/[۰-۹]/g, digit => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)))
  .replace(/\D/g, '');

const loadAllAvailableBooths = async (exhibitionId: string) => {
  const pageSize = 200;
  const booths: BoothItem[] = [];
  for (let pageNumber = 1; ; pageNumber += 1) {
    const response = await api.get<PagedResult<BoothItem>>(
      `/api/booths?pageNumber=${pageNumber}&pageSize=${pageSize}&exhibitionId=${exhibitionId}&status=Available`,
    );
    const items = response.data.items ?? [];
    booths.push(...items);
    if (items.length < pageSize || booths.length >= (response.data.totalCount ?? booths.length)) break;
  }
  return booths;
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
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [currencies, setCurrencies] = useState<CurrencyOption[]>([]);
  const [selectedServices, setSelectedServices] = useState<Record<number, string>>({});
  const [form, setForm] = useState<ReservationFormData>(emptyReservationForm);
  const [isSaving, setIsSaving] = useState(false);

  // 1. Load Exhibitions
  useEffect(() => {
    const loadExhibitions = async () => {
      try {
        const items = await getAllItems<ExhibitionSummary>('/api/exhibitions');
        setExhibitions(items);
        if (items.length > 0) {
          setSelectedExhibitionID(items[0].exhibitionID.toString());
          setForm(prev => ({
            ...prev,
            exhibitionID: items[0].exhibitionID.toString(),
            currencyCode: items[0].entryCurrency || 'LYD'
          }));
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
      setReservations(await getAllItems<ReservationSummary>(`/api/reservations/exhibition/${selectedExhibitionID}`));
    } catch {
      setError('تعذر تحميل حجوزات المعرض من الخادم.');
    } finally {
      setIsLoading(false);
    }
  }, [selectedExhibitionID]);

  useEffect(() => {
    void loadReservations();
  }, [loadReservations]);

  useEffect(() => {
    void api.get<CurrencyOption[]>('/api/currencies')
      .then(response => setCurrencies((response.data ?? []).filter(currency => currency.isActive)))
      .catch(() => setCurrencies([]));
  }, []);

  // Load Exhibitors and Booths when opening modal
  const openNewReservationModal = async () => {
    const targetExhibitionId = selectedExhibitionID || (exhibitions[0]?.exhibitionID.toString() ?? '');
    const currentEx = exhibitions.find(e => e.exhibitionID.toString() === targetExhibitionId);
    const defaultCurrency = currentEx?.entryCurrency || 'LYD';

    setForm({
      ...emptyReservationForm,
      exhibitionID: targetExhibitionId,
      currencyCode: defaultCurrency,
    });
    setServices([]);
    setSelectedServices({});
    setIsModalOpen(true);

    try {
      const [exhRes, boothsRes, servicesRes] = await Promise.allSettled([
        getAllItems<ExhibitorItem>('/api/exhibitors'),
        targetExhibitionId
          ? loadAllAvailableBooths(targetExhibitionId)
          : Promise.resolve([]),
        api.get<ServiceItem[]>('/api/services'),
      ]);

      if (exhRes.status === 'fulfilled') {
        const exList = exhRes.value;
        const activeExhibitors = exList.filter(item => item.isActive);
        setExhibitors(activeExhibitors);
        if (activeExhibitors.length > 0) {
          setForm(prev => ({
            ...prev,
            exhibitorID: activeExhibitors[0].exhibitorID.toString(),
            exhibitorCategory: activeExhibitors[0].exhibitorCategory || 'Local',
            currencyCode: activeExhibitors[0].preferredCurrencyCode || defaultCurrency,
          }));
        }
      }

      if (boothsRes.status === 'fulfilled') {
        const bList = boothsRes.value;
        setBooths(bList);
        if (bList.length > 0) {
          setForm(prev => ({
            ...prev,
            boothID: bList[0].boothID.toString(),
            requestedAreaSqM: bList[0].currentAreaSqM.toString(),
            boothTypeSelected: bList[0].boothType || 'Standard',
          }));
        } else if (targetExhibitionId) {
          showToast('لا توجد أجنحة متاحة في موقع هذا المعرض.', 'warning');
        }
      }

      if (servicesRes.status === 'fulfilled') {
        const activeServices = (servicesRes.value.data ?? []).filter(service => service.isActive);
        setServices(activeServices);
        setSelectedServices(Object.fromEntries(
          activeServices
            .filter(service => service.isMandatory)
            .map(service => [service.serviceID, '1']),
        ));
      } else {
        showToast('تعذر تحميل الخدمات الإضافية المتاحة.', 'warning');
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

    const requestedServices = Object.entries(selectedServices).map(([serviceID, quantity]) => ({
      serviceID: Number(serviceID),
      quantity: Number(quantity),
      currencyCode: form.currencyCode,
    }));
    if (requestedServices.some(service => !Number.isInteger(service.quantity) || service.quantity <= 0)) {
      showToast('يرجى إدخال كمية صحيحة وموجبة لكل خدمة محددة.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      const createResponse = await api.post<CreatedReservation>('/api/reservations', {
        exhibitionID: Number(form.exhibitionID),
        exhibitorID: Number(form.exhibitorID),
        boothID: Number(form.boothID),
        boothTypeSelected: form.boothTypeSelected,
        requestedAreaSqM: Number(form.requestedAreaSqM),
        exhibitorCategory: form.exhibitorCategory,
        currencyCode: form.currencyCode,
        logisticNotes: form.logisticNotes.trim(),
        services: requestedServices,
      });

      // Compatibility fallback for an API process that has not yet been restarted
      // with atomic reservation-service creation. The legacy endpoint updates the
      // reservation totals and the existing invoice item list itself.
      const persistedServiceIds = new Set(
        (createResponse.data.services ?? []).map(service => service.serviceID),
      );
      const missingServices = requestedServices.filter(
        service => !persistedServiceIds.has(service.serviceID),
      );
      let failedServices = 0;
      for (const service of missingServices) {
        try {
          await api.post(`/api/reservations/${createResponse.data.reservationID}/services`, service);
        } catch {
          failedServices += 1;
        }
      }

      showToast(
        failedServices === 0
          ? 'تم إنشاء طلب الحجز وإضافة خدماته إلى الفاتورة بنجاح.'
          : `تم إنشاء الحجز، لكن تعذرت إضافة ${failedServices} من الخدمات إلى الفاتورة.`,
        failedServices === 0 ? 'success' : 'warning',
      );
      setIsModalOpen(false);
      await loadReservations();
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string; title?: string } } };
      const msg = axiosErr.response?.data?.message || axiosErr.response?.data?.title || 'فشل إنشاء الحجز. قد يكون الجناح محجوزاً بالفعل أو البيانات غير مكتملة.';
      showToast(msg, 'danger');
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

  const serviceSourceEstimates = useMemo(() => {
    const totals = new Map<string, number>();
    services.forEach(service => {
      const quantity = Number(selectedServices[service.serviceID] ?? 0);
      if (quantity <= 0) return;
      const currency = service.defaultCurrencyCode || 'LYD';
      totals.set(currency, (totals.get(currency) ?? 0) + (Number(service.defaultPrice) || 0) * quantity);
    });
    return Array.from(totals.entries());
  }, [services, selectedServices]);

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
            <option value="Confirmed">مؤكد</option>
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
                      reservation.status.toLowerCase() === 'confirmed' ? 'badge-success' :
                      reservation.status.toLowerCase() === 'pending' ? 'badge-warning' : 'badge-danger'
                    }`}>
                      {({ Pending: 'قيد المراجعة', Confirmed: 'مؤكد', Cancelled: 'ملغى' } as Record<string, string>)[reservation.status] ?? reservation.status}
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
          <form className="card" onSubmit={handleCreateReservation} style={{ width: 'min(680px, 100%)', display: 'grid', gap: '14px', maxHeight: '90vh', overflowY: 'auto' }}>
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
                disabled
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
                onChange={e => {
                  const exhibitor = exhibitors.find(item => item.exhibitorID === Number(e.target.value));
                  setForm(prev => ({
                    ...prev,
                    exhibitorID: e.target.value,
                    exhibitorCategory: exhibitor?.exhibitorCategory || 'Local',
                    currencyCode: exhibitor?.preferredCurrencyCode || prev.currencyCode,
                  }));
                }}
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
                onChange={e => {
                  const booth = booths.find(item => item.boothID === Number(e.target.value));
                  setForm(prev => ({
                    ...prev,
                    boothID: e.target.value,
                    requestedAreaSqM: booth?.currentAreaSqM.toString() || '',
                    boothTypeSelected: booth?.boothType || 'Standard',
                  }));
                }}
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
              <label className="form-label">نوع الجناح المطلوب *</label>
              <select
                className="input-field"
                required
                disabled
                value={form.boothTypeSelected}
                onChange={e => setForm(prev => ({ ...prev, boothTypeSelected: e.target.value }))}
              >
                <option value="Standard">قياسي</option>
                <option value="Corner">زاوية</option>
                <option value="Premium">مميز</option>
                <option value="VIP">كبار الشخصيات</option>
                <option value="Custom">مخصص</option>
              </select>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">المساحة المطلوبة (م²) *</label>
                <input
                  className="input-field"
                  type="number"
                  min="0.01"
                  step="0.01"
                  required
                  readOnly
                  value={form.requestedAreaSqM}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">العملة *</label>
                <select
                  className="input-field"
                  required
                  value={form.currencyCode}
                  onChange={e => setForm(prev => ({ ...prev, currencyCode: e.target.value.toUpperCase() }))}
                >
                  {currencies.map(currency => (
                    <option key={currency.currencyCode} value={currency.currencyCode}>
                      {currency.currencyName} ({currency.currencyCode})
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">متطلبات لوجستية أو شروط خاصة</label>
              <textarea
                className="input-field"
                rows={2}
                placeholder="أي اشتراطات كهرباء أو تجهيزات إضافية…"
                value={form.logisticNotes}
                onChange={e => setForm(prev => ({ ...prev, logisticNotes: e.target.value }))}
              />
            </div>

            <div style={{ borderTop: '1px solid var(--border-main)', paddingTop: '14px', display: 'grid', gap: '10px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: '12px', alignItems: 'center' }}>
                <div>
                  <h3 style={{ fontSize: '14px', fontWeight: 800 }}>الخدمات الإضافية</h3>
                  <span style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>اختر الخدمات التي ستظهر ضمن بنود الفاتورة.</span>
                </div>
                {serviceSourceEstimates.length > 0 && (
                  <span style={{ color: 'var(--primary)', fontSize: '12px' }}>
                    السعر الأصلي: {serviceSourceEstimates.map(([currency, amount]) => `${amount.toLocaleString('ar-LY')} ${currency}`).join(' + ')}
                    {' — '}يحوّله الخادم إلى {form.currencyCode} بسعر الصرف المحفوظ.
                  </span>
                )}
              </div>

              {services.length === 0 ? (
                <div style={{ padding: '12px', borderRadius: 'var(--radius-md)', background: 'var(--bg-main)', color: 'var(--text-secondary)', fontSize: '13px' }}>
                  لا توجد خدمات نشطة متاحة حاليًا.
                </div>
              ) : services.map(service => {
                const isSelected = selectedServices[service.serviceID] !== undefined;
                return (
                  <div
                    key={service.serviceID}
                    style={{ display: 'grid', gridTemplateColumns: 'minmax(0, 1fr) 120px', gap: '12px', alignItems: 'center', padding: '10px 12px', border: '1px solid var(--border-main)', borderRadius: 'var(--radius-md)' }}
                  >
                    <label style={{ display: 'flex', alignItems: 'flex-start', gap: '10px', cursor: service.isMandatory ? 'default' : 'pointer' }}>
                      <input
                        type="checkbox"
                        checked={isSelected}
                        disabled={service.isMandatory}
                        onChange={event => setSelectedServices(previous => {
                          if (event.target.checked) return { ...previous, [service.serviceID]: '1' };
                          const next = { ...previous };
                          delete next[service.serviceID];
                          return next;
                        })}
                      />
                      <span>
                        <strong style={{ display: 'block', fontSize: '13px' }}>
                          {service.serviceName} {service.isMandatory && <span className="badge badge-warning">إلزامية</span>}
                        </strong>
                        <span style={{ color: 'var(--text-secondary)', fontSize: '11px' }}>
                          {service.category || 'خدمة'}
                          {service.defaultPrice != null && ` — ${Number(service.defaultPrice).toLocaleString('ar-LY')} ${service.defaultCurrencyCode || 'LYD'} / ${service.unit || 'وحدة'}`}
                        </span>
                      </span>
                    </label>
                    <input
                      className="input-field"
                      type="text"
                      inputMode="numeric"
                      pattern="[0-9]+"
                      required={isSelected}
                      disabled={!isSelected}
                      aria-label={`كمية ${service.serviceName}`}
                      value={selectedServices[service.serviceID] ?? ''}
                      onChange={event => setSelectedServices(previous => ({
                        ...previous,
                        [service.serviceID]: normalizeIntegerInput(event.target.value),
                      }))}
                      placeholder="الكمية"
                    />
                  </div>
                );
              })}
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
