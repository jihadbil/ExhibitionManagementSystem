import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Building,
  Edit2,
  Globe,
  Mail,
  Phone,
  Plus,
  Search,
  Trash2,
  User,
  X
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface Exhibitor {
  exhibitorID: number;
  companyName: string;
  sector: string;
  nationality: string;
  exhibitorCategory: string;
  contactPerson?: string;
  email?: string;
  phone?: string;
  logoURL?: string;
  companyProfile?: string;
  isActive: boolean;
}

interface ExhibitorFormData {
  companyName: string;
  sector: string;
  nationality: string;
  exhibitorCategory: string;
  contactPerson: string;
  email: string;
  phone: string;
  logoURL: string;
  companyProfile: string;
}

const emptyForm: ExhibitorFormData = {
  companyName: '',
  sector: '',
  nationality: 'ليبي',
  exhibitorCategory: 'Local',
  contactPerson: '',
  email: '',
  phone: '',
  logoURL: '',
  companyProfile: '',
};

const categoryNames: Record<string, string> = {
  Local: 'محلي',
  International: 'دولي',
  Government: 'حكومي',
};

export const CompaniesPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'EventManager') ?? false;

  const [items, setItems] = useState<Exhibitor[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<ExhibitorFormData>(emptyForm);
  const [isSaving, setIsSaving] = useState(false);

  // Load Exhibitors
  const loadExhibitors = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<{ items: Exhibitor[] }>('/api/exhibitors?page=1&pageSize=150');
      setItems(response.data.items ?? []);
    } catch {
      setError('تعذر تحميل بيانات الشركات العارضة من الخادم.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadExhibitors();
  }, [loadExhibitors]);

  // Filtered List
  const filtered = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return items.filter(item => {
      const matchesCategory = selectedCategory === 'All' || item.exhibitorCategory === selectedCategory;
      const matchesSearch = !q ||
        item.companyName.toLowerCase().includes(q) ||
        (item.sector && item.sector.toLowerCase().includes(q)) ||
        (item.nationality && item.nationality.toLowerCase().includes(q)) ||
        (item.contactPerson && item.contactPerson.toLowerCase().includes(q)) ||
        (item.email && item.email.toLowerCase().includes(q));
      return matchesCategory && matchesSearch;
    });
  }, [items, searchQuery, selectedCategory]);

  // Open Create Modal
  const openCreateModal = () => {
    setEditingId(null);
    setForm(emptyForm);
    setIsModalOpen(true);
  };

  // Open Edit Modal
  const openEditModal = (ex: Exhibitor) => {
    setEditingId(ex.exhibitorID);
    setForm({
      companyName: ex.companyName,
      sector: ex.sector || '',
      nationality: ex.nationality || 'ليبي',
      exhibitorCategory: ex.exhibitorCategory || 'Local',
      contactPerson: ex.contactPerson || '',
      email: ex.email || '',
      phone: ex.phone || '',
      logoURL: ex.logoURL || '',
      companyProfile: ex.companyProfile || '',
    });
    setIsModalOpen(true);
  };

  // Submit Create / Edit
  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!form.companyName.trim()) {
      showToast('يرجى إدخال اسم الشركة.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      const payload = {
        companyName: form.companyName.trim(),
        sector: form.sector.trim(),
        nationality: form.nationality.trim(),
        exhibitorCategory: form.exhibitorCategory,
        contactPerson: form.contactPerson.trim(),
        email: form.email.trim(),
        phone: form.phone.trim(),
        logoURL: form.logoURL.trim(),
        companyProfile: form.companyProfile.trim(),
      };

      if (editingId) {
        await api.put(`/api/exhibitors/${editingId}`, payload);
        showToast('تم تحديث بيانات العارض بنجاح.', 'success');
      } else {
        await api.post('/api/exhibitors', payload);
        showToast('تمت إضافة الشركة العارضة بنجاح.', 'success');
      }

      setIsModalOpen(false);
      await loadExhibitors();
    } catch {
      showToast('فشل حفظ بيانات العارض. يرجى التحقق من صحة البيانات.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  // Delete Exhibitor
  const handleDelete = async (ex: Exhibitor) => {
    if (!window.confirm(`هل أنت متأكد من حذف شركة «${ex.companyName}»؟`)) return;

    try {
      await api.delete(`/api/exhibitors/${ex.exhibitorID}`);
      showToast('تم حذف الشركة العارضة بنجاح.', 'success');
      await loadExhibitors();
    } catch {
      showToast('تعذر حذف الشركة؛ قد تكون مرتبطة بحجوزات أو فواتير قائمة.', 'danger');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Building size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>الشركات العارضة</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>دليل العارضين، بيانات الاتصال، وتصنيف الأجنحة والمشاركات.</p>
        </div>
        {canManage && (
          <button className="btn-primary" onClick={openCreateModal}>
            <Plus size={18} /> شركة عارضة جديدة
          </button>
        )}
      </div>

      {/* Filters and Search Bar */}
      <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap', alignItems: 'center' }}>
        <div className="card" style={{ flex: 1, minWidth: '280px', position: 'relative', padding: 0 }}>
          <Search size={18} style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            className="input-field"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            style={{ paddingRight: '44px', border: 0 }}
            placeholder="ابحث باسم الشركة أو القطاع أو ضابط الاتصال…"
          />
        </div>

        {/* Category Pills */}
        <div style={{ display: 'flex', gap: '8px' }}>
          {['All', 'Local', 'International', 'Government'].map(cat => (
            <button
              key={cat}
              onClick={() => setSelectedCategory(cat)}
              className={selectedCategory === cat ? 'btn-primary' : 'btn-secondary'}
              style={{ height: '42px', padding: '0 16px', fontSize: '13px' }}
            >
              {cat === 'All' ? 'جميع الفئات' : categoryNames[cat] ?? cat}
            </button>
          ))}
        </div>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Exhibitors Grid */}
      {loading ? (
        <div className="card" style={{ textAlign: 'center' }}>جاري تحميل الشركات العارضة…</div>
      ) : filtered.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', color: 'var(--text-secondary)' }}>لا توجد شركات عارضة مطابقة.</div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(340px, 1fr))', gap: '20px' }}>
          {filtered.map(item => (
            <article key={item.exhibitorID} className="card" style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                  <div style={{ width: '40px', height: '40px', borderRadius: 'var(--radius-md)', background: 'var(--bg-main)', display: 'grid', placeItems: 'center', color: 'var(--primary)' }}>
                    <Building size={20} />
                  </div>
                  <div>
                    <h2 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>{item.companyName}</h2>
                    <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{item.sector || 'قطاع عام'}</span>
                  </div>
                </div>
                <span className="badge badge-info">
                  {categoryNames[item.exhibitorCategory] ?? item.exhibitorCategory}
                </span>
              </div>

              {item.companyProfile && (
                <p style={{ fontSize: '13px', color: 'var(--text-secondary)', lineHeight: 1.5, overflow: 'hidden', textOverflow: 'ellipsis', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
                  {item.companyProfile}
                </p>
              )}

              <div style={{ display: 'grid', gap: '6px', fontSize: '12px', color: 'var(--text-secondary)', borderTop: '1px solid var(--border-main)', paddingTop: '10px' }}>
                {item.contactPerson && (
                  <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <User size={13} style={{ color: 'var(--primary)' }} /> ضابط الاتصال: {item.contactPerson}
                  </span>
                )}
                {item.email && (
                  <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }} dir="ltr">
                    <Mail size={13} style={{ color: 'var(--primary)' }} /> {item.email}
                  </span>
                )}
                {item.phone && (
                  <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }} dir="ltr">
                    <Phone size={13} style={{ color: 'var(--primary)' }} /> {item.phone}
                  </span>
                )}
                {item.nationality && (
                  <span style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <Globe size={13} style={{ color: 'var(--primary)' }} /> الجنسية: {item.nationality}
                  </span>
                )}
              </div>

              {canManage && (
                <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '8px', marginTop: 'auto', borderTop: '1px solid var(--border-main)', paddingTop: '10px' }}>
                  <button className="btn-secondary" style={{ padding: '0 12px', height: '32px', fontSize: '12px' }} onClick={() => openEditModal(item)}>
                    <Edit2 size={13} /> تعديل
                  </button>
                  <button className="btn-icon" title="حذف العارض" style={{ color: 'var(--danger)' }} onClick={() => void handleDelete(item)}>
                    <Trash2 size={15} />
                  </button>
                </div>
              )}
            </article>
          ))}
        </div>
      )}

      {/* Modal: Create / Edit Exhibitor */}
      {isModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleSubmit} style={{ width: 'min(580px, 100%)', display: 'grid', gap: '14px', maxHeight: '90vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>{editingId ? 'تعديل بيانات الشركة العارضة' : 'إضافة شركة عارضة جديدة'}</h2>
              <button type="button" className="btn-icon" onClick={() => setIsModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم الشركة / المؤسسة *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: الشركة الليبية للاتصالات والتقنية"
                value={form.companyName}
                onChange={e => setForm(prev => ({ ...prev, companyName: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">القطاع / المجال</label>
                <input
                  className="input-field"
                  placeholder="تقنية المعلومات، الاتصالات، الصناعة…"
                  value={form.sector}
                  onChange={e => setForm(prev => ({ ...prev, sector: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">فئة العارض</label>
                <select
                  className="input-field"
                  value={form.exhibitorCategory}
                  onChange={e => setForm(prev => ({ ...prev, exhibitorCategory: e.target.value }))}
                >
                  <option value="Local">محلي</option>
                  <option value="International">دولي</option>
                  <option value="Government">حكومي</option>
                </select>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">اسم ضابط الاتصال</label>
                <input
                  className="input-field"
                  placeholder="المهندس / المدير المسؤول"
                  value={form.contactPerson}
                  onChange={e => setForm(prev => ({ ...prev, contactPerson: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الجنسية / الموطن</label>
                <input
                  className="input-field"
                  placeholder="ليبي"
                  value={form.nationality}
                  onChange={e => setForm(prev => ({ ...prev, nationality: e.target.value }))}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">البريد الإلكتروني</label>
                <input
                  className="input-field"
                  type="email"
                  placeholder="info@company.ly"
                  value={form.email}
                  onChange={e => setForm(prev => ({ ...prev, email: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">رقم الهاتف</label>
                <input
                  className="input-field"
                  placeholder="+218 91 0000000"
                  value={form.phone}
                  onChange={e => setForm(prev => ({ ...prev, phone: e.target.value }))}
                />
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">رابط الشعار (Logo URL)</label>
              <input
                className="input-field"
                placeholder="https://example.com/logo.png"
                value={form.logoURL}
                onChange={e => setForm(prev => ({ ...prev, logoURL: e.target.value }))}
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">نبذة تعريفية عن الشركة ونشاطها</label>
              <textarea
                className="input-field"
                rows={3}
                placeholder="وصف مختصر للشركة والخدمات أو المنتجات التي تقدمها…"
                value={form.companyProfile}
                onChange={e => setForm(prev => ({ ...prev, companyProfile: e.target.value }))}
              />
            </div>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري الحفظ…' : editingId ? 'تحديث بيانات الشركة' : 'حفظ الشركة العارضة'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
