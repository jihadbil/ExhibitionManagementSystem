import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Layers,
  Plus,
  Trash2,
  X
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface Service {
  serviceID: number;
  serviceName: string;
  category: string;
  unit: string;
  defaultPrice?: number;
  defaultCurrencyCode: string;
  isMandatory: boolean;
  description: string;
  isActive: boolean;
}

interface CurrencyOption {
  currencyCode: string;
  currencyName: string;
  isActive: boolean;
}

export const ServicesPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin') ?? false;

  const [services, setServices] = useState<Service[]>([]);
  const [currencies, setCurrencies] = useState<CurrencyOption[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Service Modal State
  const [isServiceModalOpen, setIsServiceModalOpen] = useState(false);
  const [serviceForm, setServiceForm] = useState({
    serviceName: '',
    category: 'كهرباء وتجهيزات',
    unit: 'وحدة',
    defaultPrice: '100',
    defaultCurrencyCode: 'LYD',
    isMandatory: false,
    description: '',
  });

  const [isSaving, setIsSaving] = useState(false);

  // Load Services
  const loadServices = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<Service[]>('/api/services');
      setServices(response.data ?? []);
    } catch {
      setError('تعذر تحميل كتالوج الخدمات المساندة.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadServices();
    void api.get<CurrencyOption[]>('/api/currencies')
      .then(response => setCurrencies((response.data ?? []).filter(currency => currency.isActive)))
      .catch(() => setCurrencies([]));
  }, [loadServices]);

  // Handle Service Submit
  const handleServiceSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!serviceForm.serviceName.trim()) {
      showToast('يرجى إدخال اسم الخدمة.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      await api.post('/api/services', {
        serviceName: serviceForm.serviceName.trim(),
        category: serviceForm.category,
        unit: serviceForm.unit,
        defaultPrice: Number(serviceForm.defaultPrice) || 0,
        defaultCurrencyCode: serviceForm.defaultCurrencyCode,
        isMandatory: serviceForm.isMandatory,
        description: serviceForm.description.trim(),
        isActive: true,
      });

      showToast('تمت إضافة الخدمة المساندة بنجاح.', 'success');
      setIsServiceModalOpen(false);
      setServiceForm({
        serviceName: '',
        category: 'كهرباء وتجهيزات',
        unit: 'وحدة',
        defaultPrice: '100',
        defaultCurrencyCode: 'LYD',
        isMandatory: false,
        description: '',
      });
      await loadServices();
    } catch {
      showToast('فشل حفظ الخدمة المساندة في الخادم.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  // Delete Service
  const handleDeleteService = async (service: Service) => {
    if (!window.confirm(`هل أنت متأكد من حذف الخدمة «${service.serviceName}»؟`)) return;

    try {
      await api.delete(`/api/services/${service.serviceID}`);
      showToast('تم حذف الخدمة بنجاح.', 'success');
      await loadServices();
    } catch {
      showToast('تعذر حذف الخدمة؛ قد تكون مرتبطة بحجوزات قائمة.', 'danger');
    }
  };

  const filteredServices = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return services;
    return services.filter(s =>
      s.serviceName.toLowerCase().includes(q) ||
      s.category.toLowerCase().includes(q)
    );
  }, [services, searchQuery]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Layers size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>الخدمات المساندة</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>
            كتالوج الخدمات الإضافية والتجهيزات المتاحة للأجنحة والعارضين.
          </p>
        </div>

        {canManage && (
          <button className="btn-primary" onClick={() => setIsServiceModalOpen(true)}>
            <Plus size={18} /> خدمة جديدة
          </button>
        )}
      </div>

      {/* Search Input */}
      <input
        className="input-field"
        value={searchQuery}
        onChange={event => setSearchQuery(event.target.value)}
        placeholder="ابحث باسم الخدمة أو الفئة…"
        style={{ maxWidth: '420px' }}
      />

      {error && (
        <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>
          {error}
        </div>
      )}

      {/* Services Table */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>اسم الخدمة</th>
                <th>التصنيف</th>
                <th>وحدة القياس</th>
                <th>السعر الافتراضي</th>
                <th>نوع الخدمة</th>
                <th>الوصف</th>
                {canManage && <th>إجراءات</th>}
              </tr>
            </thead>
            <tbody>
              {filteredServices.map(service => (
                <tr key={service.serviceID}>
                  <td><strong>{service.serviceName}</strong></td>
                  <td><span className="badge badge-info">{service.category}</span></td>
                  <td>{service.unit}</td>
                  <td><strong>{service.defaultPrice?.toLocaleString() ?? 0} {service.defaultCurrencyCode || 'LYD'}</strong></td>
                  <td>
                    <span className={`badge ${service.isMandatory ? 'badge-warning' : 'badge-neutral'}`}>
                      {service.isMandatory ? 'إلزامية للأجنحة' : 'اختيارية'}
                    </span>
                  </td>
                  <td>{service.description || '—'}</td>
                  {canManage && (
                    <td>
                      <button className="btn-icon" style={{ color: 'var(--danger)' }} onClick={() => void handleDeleteService(service)}>
                        <Trash2 size={15} />
                      </button>
                    </td>
                  )}
                </tr>
              ))}
              {!loading && filteredServices.length === 0 && (
                <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>لا توجد خدمات مسجلة.</td></tr>
              )}
              {loading && (
                <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: Create Service */}
      {isServiceModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleServiceSubmit} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>إضافة خدمة مساندة جديدة</h2>
              <button type="button" className="btn-icon" onClick={() => setIsServiceModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم الخدمة *</label>
              <input
                className="input-field"
                required
                placeholder="توصيلة كهرباء ثلاثية، إنترنت فائق السرعة، طاولة إضافية…"
                value={serviceForm.serviceName}
                onChange={e => setServiceForm(prev => ({ ...prev, serviceName: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الفئة</label>
                <select
                  className="input-field"
                  value={serviceForm.category}
                  onChange={e => setServiceForm(prev => ({ ...prev, category: e.target.value }))}
                >
                  <option value="كهرباء وتجهيزات">كهرباء وتجهيزات</option>
                  <option value="أثاث وديكور">أثاث وديكور</option>
                  <option value="اتصالات وإنترنت">اتصالات وإنترنت</option>
                  <option value="نظافة وضيافة">نظافة وضيافة</option>
                  <option value="أمن وحراسة">أمن وحراسة</option>
                </select>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">وحدة القياس</label>
                <select
                  className="input-field"
                  value={serviceForm.unit}
                  onChange={e => setServiceForm(prev => ({ ...prev, unit: e.target.value }))}
                >
                  <option value="وحدة">وحدة / قطعة</option>
                  <option value="يوم">يوم</option>
                  <option value="ساعة">ساعة</option>
                  <option value="متر مربع">متر مربع</option>
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">السعر القياسي الافتراضي</label>
                <input
                  className="input-field"
                  type="number"
                  min="0"
                  step="0.01"
                  value={serviceForm.defaultPrice}
                  onChange={e => setServiceForm(prev => ({ ...prev, defaultPrice: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">عملة السعر</label>
                <select
                  className="input-field"
                  required
                  value={serviceForm.defaultCurrencyCode}
                  onChange={e => setServiceForm(prev => ({ ...prev, defaultCurrencyCode: e.target.value }))}
                >
                  {currencies.map(currency => (
                    <option key={currency.currencyCode} value={currency.currencyCode}>
                      {currency.currencyCode}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الوصف التفصيلي</label>
              <textarea
                className="input-field"
                rows={2}
                placeholder="مواصفات الخدمة، أوقات التسليم…"
                value={serviceForm.description}
                onChange={e => setServiceForm(prev => ({ ...prev, description: e.target.value }))}
              />
            </div>

            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '13px', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={serviceForm.isMandatory}
                onChange={e => setServiceForm(prev => ({ ...prev, isMandatory: e.target.checked }))}
              />
              <span>خدمة إلزامية لكافة الأجنحة</span>
            </label>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري الحفظ…' : 'حفظ الخدمة'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
