import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  DollarSign,
  Layers,
  Plus,
  Search,
  Settings2,
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
  isMandatory: boolean;
  description: string;
  isActive: boolean;
}

interface BoothPriceRule {
  ruleID: number;
  exhibitionID?: number;
  exhibitionName?: string;
  ruleName: string;
  boothType: string;
  category: string;
  pricePerSqM: number;
  currencyCode: string;
  minArea?: number;
  isActive: boolean;
}

interface Exhibition {
  exhibitionID: number;
  name: string;
}

export const ServicesPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin') ?? false;

  const [activeTab, setActiveTab] = useState<'services' | 'pricing'>('services');
  const [services, setServices] = useState<Service[]>([]);
  const [pricingRules, setPricingRules] = useState<BoothPriceRule[]>([]);
  const [exhibitions, setExhibitions] = useState<Exhibition[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modals State
  const [isServiceModalOpen, setIsServiceModalOpen] = useState(false);
  const [serviceForm, setServiceForm] = useState({
    serviceName: '',
    category: 'كهرباء وتجهيزات',
    unit: 'وحدة',
    defaultPrice: '100',
    isMandatory: false,
    description: '',
  });

  const [isPricingModalOpen, setIsPricingModalOpen] = useState(false);
  const [pricingForm, setPricingForm] = useState({
    ruleName: '',
    exhibitionID: '',
    boothType: 'Standard',
    category: 'Local',
    pricePerSqM: '150',
    currencyCode: 'LYD',
    minArea: '9',
  });

  const [isSaving, setIsSaving] = useState(false);

  // 1. Load Services
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

  // 2. Load Pricing Rules & Exhibitions
  const loadPricingRules = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [rulesRes, exRes] = await Promise.allSettled([
        api.get<BoothPriceRule[]>('/api/pricing/booth-rules'),
        api.get<{ items: Exhibition[] }>('/api/exhibitions?page=1&pageSize=50'),
      ]);

      if (rulesRes.status === 'fulfilled') {
        setPricingRules(rulesRes.value.data ?? []);
      }
      if (exRes.status === 'fulfilled') {
        setExhibitions(exRes.value.data.items ?? []);
      }
    } catch {
      setError('تعذر تحميل قواعد التسعير.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab === 'services') {
      void loadServices();
    } else {
      void loadPricingRules();
    }
  }, [activeTab, loadServices, loadPricingRules]);

  // Submit New Service
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
        isMandatory: serviceForm.isMandatory,
        description: serviceForm.description.trim(),
      });

      showToast('تمت إضافة الخدمة المساندة بنجاح.', 'success');
      setIsServiceModalOpen(false);
      setServiceForm({ serviceName: '', category: 'كهرباء وتجهيزات', unit: 'وحدة', defaultPrice: '100', isMandatory: false, description: '' });
      await loadServices();
    } catch {
      showToast('فشل إضافة الخدمة. يرجى التحقق من المدخلات.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  // Submit New Pricing Rule
  const handlePricingSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!pricingForm.ruleName.trim()) {
      showToast('يرجى إدخال اسم قاعدة التسعير.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      await api.post('/api/pricing/booth-rules', {
        ruleName: pricingForm.ruleName.trim(),
        exhibitionID: pricingForm.exhibitionID ? Number(pricingForm.exhibitionID) : null,
        boothType: pricingForm.boothType,
        category: pricingForm.category,
        pricePerSqM: Number(pricingForm.pricePerSqM) || 0,
        currencyCode: pricingForm.currencyCode,
        minArea: pricingForm.minArea ? Number(pricingForm.minArea) : null,
      });

      showToast('تم إنشاء قاعدة تسعير الأجنحة بنجاح.', 'success');
      setIsPricingModalOpen(false);
      setPricingForm({ ruleName: '', exhibitionID: '', boothType: 'Standard', category: 'Local', pricePerSqM: '150', currencyCode: 'LYD', minArea: '9' });
      await loadPricingRules();
    } catch {
      showToast('فشل إنشاء قاعدة التسعير.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  // Delete Pricing Rule
  const handleDeletePricingRule = async (rule: BoothPriceRule) => {
    if (!window.confirm(`هل أنت متأكد من حذف قاعدة «${rule.ruleName}»؟`)) return;
    try {
      await api.delete(`/api/pricing/booth-rules/${rule.ruleID}`);
      showToast('تم حذف قاعدة التسعير بنجاح.', 'success');
      await loadPricingRules();
    } catch {
      showToast('تعذر حذف قاعدة التسعير.', 'danger');
    }
  };

  // Filtered Services
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
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>الخدمات المساندة وقواعد التسعير</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>كتالوج الخدمات الإضافية للأجنحة، وقواعد احتساب أسعار المتر المربع.</p>
        </div>

        {canManage && activeTab === 'services' && (
          <button className="btn-primary" onClick={() => setIsServiceModalOpen(true)}>
            <Plus size={18} /> خدمة جديدة
          </button>
        )}

        {canManage && activeTab === 'pricing' && (
          <button className="btn-primary" onClick={() => setIsPricingModalOpen(true)}>
            <Plus size={18} /> قاعدة تسعير جديدة
          </button>
        )}
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '10px', borderBottom: '1px solid var(--border-main)', paddingBottom: '10px' }}>
        <button
          className={activeTab === 'services' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => setActiveTab('services')}
          style={{ height: '38px', padding: '0 16px', fontSize: '13px' }}
        >
          <Layers size={16} />
          <span>دليل الخدمات ({services.length})</span>
        </button>
        <button
          className={activeTab === 'pricing' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => setActiveTab('pricing')}
          style={{ height: '38px', padding: '0 16px', fontSize: '13px' }}
        >
          <DollarSign size={16} />
          <span>قواعد تسعير الأجنحة ({pricingRules.length})</span>
        </button>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Tab 1: Services Table */}
      {activeTab === 'services' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="table-container" style={{ border: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>الخدمة</th>
                  <th>الفئة</th>
                  <th>وحدة القياس</th>
                  <th>السعر القياسي</th>
                  <th>إلزامية</th>
                  <th>الوصف</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {filteredServices.map(service => (
                  <tr key={service.serviceID}>
                    <td><strong>{service.serviceName}</strong></td>
                    <td><span className="badge badge-info">{service.category}</span></td>
                    <td>{service.unit}</td>
                    <td><strong>{service.defaultPrice?.toLocaleString() ?? '—'} د.ل</strong></td>
                    <td>{service.isMandatory ? <span style={{ color: 'var(--primary)', fontWeight: 700 }}>نعم</span> : 'اختيارية'}</td>
                    <td style={{ color: 'var(--text-secondary)' }}>{service.description || '—'}</td>
                    <td>
                      <span className={`badge ${service.isActive ? 'badge-success' : 'badge-danger'}`}>
                        {service.isActive ? 'نشطة' : 'متوقفة'}
                      </span>
                    </td>
                  </tr>
                ))}
                {!loading && filteredServices.length === 0 && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: '30px' }}>لا توجد خدمات مسجلة.</td></tr>
                )}
                {loading && (
                  <tr><td colSpan={7} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 2: Pricing Rules Table */}
      {activeTab === 'pricing' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="table-container" style={{ border: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>اسم القاعدة</th>
                  <th>المعرض المحدد</th>
                  <th>نوع الجناح</th>
                  <th>فئة العارض</th>
                  <th>سعر المتر المربع (م²)</th>
                  <th>أدنى مساحة</th>
                  {canManage && <th>إجراءات</th>}
                </tr>
              </thead>
              <tbody>
                {pricingRules.map(rule => (
                  <tr key={rule.ruleID}>
                    <td><strong>{rule.ruleName}</strong></td>
                    <td>{rule.exhibitionName || 'عام (كافة المعارض)'}</td>
                    <td><span className="badge badge-info">{rule.boothType}</span></td>
                    <td>{rule.category}</td>
                    <td><strong style={{ color: 'var(--primary)' }}>{rule.pricePerSqM.toLocaleString()} {rule.currencyCode}</strong></td>
                    <td>{rule.minArea ? `${rule.minArea} م²` : '—'}</td>
                    {canManage && (
                      <td>
                        <button className="btn-icon" style={{ color: 'var(--danger)' }} onClick={() => void handleDeletePricingRule(rule)}>
                          <Trash2 size={15} />
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
                {!loading && pricingRules.length === 0 && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>لا توجد قواعد تسعير مسجلة.</td></tr>
                )}
                {loading && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

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

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">السعر القياسي الافتراضي (د.ل)</label>
              <input
                className="input-field"
                type="number"
                min="0"
                value={serviceForm.defaultPrice}
                onChange={e => setServiceForm(prev => ({ ...prev, defaultPrice: e.target.value }))}
              />
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

      {/* Modal: Create Pricing Rule */}
      {isPricingModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handlePricingSubmit} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>قاعدة تسعير جديدة للأجنحة</h2>
              <button type="button" className="btn-icon" onClick={() => setIsPricingModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم القاعدة التعريفية *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: تسعير الأجنحة المميزة للمعارض التقنية"
                value={pricingForm.ruleName}
                onChange={e => setPricingForm(prev => ({ ...prev, ruleName: e.target.value }))}
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">المعرض المرتبط (اختياري)</label>
              <select
                className="input-field"
                value={pricingForm.exhibitionID}
                onChange={e => setPricingForm(prev => ({ ...prev, exhibitionID: e.target.value }))}
              >
                <option value="">عام لكافة المعارض</option>
                {exhibitions.map(ex => (
                  <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>{ex.name}</option>
                ))}
              </select>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">نوع الجناح</label>
                <select
                  className="input-field"
                  value={pricingForm.boothType}
                  onChange={e => setPricingForm(prev => ({ ...prev, boothType: e.target.value }))}
                >
                  <option value="Standard">Standard (قياسي)</option>
                  <option value="Corner">Corner (ناصية)</option>
                  <option value="Premium">Premium (مميز)</option>
                  <option value="VIP">VIP (خاص)</option>
                </select>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">فئة العارض</label>
                <select
                  className="input-field"
                  value={pricingForm.category}
                  onChange={e => setPricingForm(prev => ({ ...prev, category: e.target.value }))}
                >
                  <option value="Local">محلي</option>
                  <option value="International">دولي</option>
                  <option value="Government">حكومي</option>
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">سعر المتر المربع (م²)</label>
                <input
                  className="input-field"
                  type="number"
                  min="0"
                  required
                  value={pricingForm.pricePerSqM}
                  onChange={e => setPricingForm(prev => ({ ...prev, pricePerSqM: e.target.value }))}
                />
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الحد الأدنى للمساحة (م²)</label>
                <input
                  className="input-field"
                  type="number"
                  min="1"
                  value={pricingForm.minArea}
                  onChange={e => setPricingForm(prev => ({ ...prev, minArea: e.target.value }))}
                />
              </div>
            </div>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري الحفظ…' : 'حفظ قاعدة التسعير'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
