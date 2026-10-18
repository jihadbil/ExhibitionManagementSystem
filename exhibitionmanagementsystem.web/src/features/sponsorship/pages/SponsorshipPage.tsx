import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Building2,
  Edit2,
  FileCheck,
  Layout,
  Plus,
  Sparkles,
  Trash2,
  X
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getAllItems } from '../../../services/pagination';
import { getApiErrorMessage } from '../../../services/apiError';

interface Exhibition {
  exhibitionID: number;
  name: string;
}

interface Sponsor {
  sponsorID: number;
  companyName: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  websiteUrl?: string;
  isActive: boolean;
  totalContractsCount: number;
}

interface SponsorshipPackage {
  packageID: number;
  exhibitionID: number;
  packageName: string;
  price: number;
  currencyCode: string;
  level: string;
  levelName: string;
  maxSponsorsCount: number;
  allocatedSponsorsCount: number;
  entitlementsDescription?: string;
  isSoldOut: boolean;
  isActive: boolean;
}

interface AdvertisingSpace {
  spaceID: number;
  hallID?: number;
  hallName?: string;
  spaceName: string;
  locationDescription: string;
  basePrice: number;
  currencyCode: string;
  isAvailable: boolean;
}

interface SponsorshipContract {
  contractID: number;
  sponsorID: number;
  sponsorCompanyName: string;
  packageID?: number;
  packageName?: string;
  exhibitionID: number;
  totalAmount: number;
  currencyCode: string;
  status: number;
  statusName: string;
  signedDate: string;
}

const sponsorshipLevels: Record<string, number> = {
  Platinum: 1, Gold: 2, Silver: 3, Bronze: 4, TitleSponsor: 5, Custom: 6,
};

export const SponsorshipPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'EventManager') ?? false;

  const [activeTab, setActiveTab] = useState<'sponsors' | 'packages' | 'spaces' | 'contracts'>('sponsors');
  const [exhibitions, setExhibitions] = useState<Exhibition[]>([]);
  const [selectedExhibitionId, setSelectedExhibitionId] = useState<string>('');

  const [sponsors, setSponsors] = useState<Sponsor[]>([]);
  const [packages, setPackages] = useState<SponsorshipPackage[]>([]);
  const [spaces, setSpaces] = useState<AdvertisingSpace[]>([]);
  const [contracts, setContracts] = useState<SponsorshipContract[]>([]);

  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modals
  const [isSponsorModalOpen, setIsSponsorModalOpen] = useState(false);
  const [editingSponsorId, setEditingSponsorId] = useState<number | null>(null);
  const [sponsorForm, setSponsorForm] = useState({
    companyName: '',
    contactPerson: '',
    phone: '',
    email: '',
    websiteUrl: '',
    isActive: true,
  });

  const [isPackageModalOpen, setIsPackageModalOpen] = useState(false);
  const [packageForm, setPackageForm] = useState({
    packageName: '',
    level: 'Gold',
    price: '5000',
    currencyCode: 'LYD',
    maxSponsorsCount: '3',
    entitlementsDescription: '',
  });

  const [isSaving, setIsSaving] = useState(false);

  // 1. Load Exhibitions
  useEffect(() => {
    const loadExhibitions = async () => {
      try {
        const list = await getAllItems<Exhibition>('/api/exhibitions');
        setExhibitions(list);
        if (list.length > 0) {
          setSelectedExhibitionId(list[0].exhibitionID.toString());
        }
      } catch {
        setError('تعذر تحميل المعارض.');
      }
    };
    void loadExhibitions();
  }, []);

  // 2. Load Sponsors
  const loadSponsors = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<Sponsor[]>('/api/sponsorship/sponsors');
      setSponsors(response.data ?? []);
    } catch {
      setError('تعذر تحميل الرعاة.');
    } finally {
      setLoading(false);
    }
  }, []);

  // 3. Load Packages
  const loadPackages = useCallback(async (exId: string) => {
    if (!exId) return;
    setLoading(true);
    try {
      const response = await api.get<SponsorshipPackage[]>(`/api/sponsorship/packages?exhibitionId=${exId}`);
      setPackages(response.data ?? []);
    } catch {
      setError('تعذر تحميل باقات الرعاية.');
    } finally {
      setLoading(false);
    }
  }, []);

  // 4. Load Ad Spaces
  const loadSpaces = useCallback(async () => {
    setLoading(true);
    try {
      const response = await api.get<AdvertisingSpace[]>('/api/sponsorship/spaces');
      setSpaces(response.data ?? []);
    } catch {
      setError('تعذر تحميل المساحات الإعلانية.');
    } finally {
      setLoading(false);
    }
  }, []);

  // 5. Load Contracts
  const loadContracts = useCallback(async (exId: string) => {
    if (!exId) return;
    setLoading(true);
    try {
      const response = await api.get<SponsorshipContract[]>(`/api/sponsorship/contracts?exhibitionId=${exId}`);
      setContracts(response.data ?? []);
    } catch {
      setError('تعذر تحميل عقود الرعاية.');
    } finally {
      setLoading(false);
    }
  }, []);

  // Trigger loads based on activeTab
  useEffect(() => {
    if (activeTab === 'sponsors') {
      void loadSponsors();
    } else if (activeTab === 'packages' && selectedExhibitionId) {
      void loadPackages(selectedExhibitionId);
    } else if (activeTab === 'spaces') {
      void loadSpaces();
    } else if (activeTab === 'contracts' && selectedExhibitionId) {
      void loadContracts(selectedExhibitionId);
    }
  }, [activeTab, selectedExhibitionId, loadSponsors, loadPackages, loadSpaces, loadContracts]);

  // Create / Edit Sponsor
  const handleSponsorSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!sponsorForm.companyName.trim()) {
      showToast('يرجى إدخال اسم الشركة الراعية.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      const payload = {
        companyName: sponsorForm.companyName.trim(),
        contactPerson: sponsorForm.contactPerson.trim(),
        phone: sponsorForm.phone.trim(),
        email: sponsorForm.email.trim(),
        websiteUrl: sponsorForm.websiteUrl.trim(),
        isActive: sponsorForm.isActive,
      };

      if (editingSponsorId) {
        await api.put(`/api/sponsorship/sponsors/${editingSponsorId}`, payload);
        showToast('تم تحديث بيانات الراعي بنجاح.', 'success');
      } else {
        await api.post('/api/sponsorship/sponsors', payload);
        showToast('تمت إضافة الراعي بنجاح.', 'success');
      }

      setIsSponsorModalOpen(false);
      await loadSponsors();
    } catch {
      showToast('فشل حفظ بيانات الراعي.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  const handleDeleteSponsor = async (s: Sponsor) => {
    if (!window.confirm(`هل أنت متأكد من حذف راعي «${s.companyName}»؟`)) return;
    try {
      await api.delete(`/api/sponsorship/sponsors/${s.sponsorID}`);
      showToast('تم حذف الراعي بنجاح.', 'success');
      await loadSponsors();
    } catch (error) {
      showToast(getApiErrorMessage(error, 'تعذر حذف الراعي لوجود عقود مرتبطة به.'), 'danger');
    }
  };

  // Create Package
  const handlePackageSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!packageForm.packageName.trim() || !selectedExhibitionId) {
      showToast('يرجى ملء جميع الحقول المطلوبة.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      await api.post('/api/sponsorship/packages', {
        exhibitionID: Number(selectedExhibitionId),
        packageName: packageForm.packageName.trim(),
        level: sponsorshipLevels[packageForm.level],
        price: Number(packageForm.price) || 0,
        currencyCode: packageForm.currencyCode,
        maxSponsorsCount: Number(packageForm.maxSponsorsCount) || 1,
        entitlementsDescription: packageForm.entitlementsDescription.trim(),
        isActive: true,
      });

      showToast('تمت إضافة باقة الرعاية بنجاح.', 'success');
      setIsPackageModalOpen(false);
      setPackageForm({ packageName: '', level: 'Gold', price: '5000', currencyCode: 'LYD', maxSponsorsCount: '3', entitlementsDescription: '' });
      await loadPackages(selectedExhibitionId);
    } catch {
      showToast('فشل إضافة باقة الرعاية.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  const handleDeletePackage = async (pkg: SponsorshipPackage) => {
    if (!window.confirm(`هل أنت متأكد من حذف باقة «${pkg.packageName}»؟`)) return;
    try {
      await api.delete(`/api/sponsorship/packages/${pkg.packageID}`);
      showToast('تم حذف الباقة بنجاح.', 'success');
      if (selectedExhibitionId) await loadPackages(selectedExhibitionId);
    } catch {
      showToast('تعذر حذف الباقة؛ قد تكون مرتبطة بعقود.', 'danger');
    }
  };

  // Filtered Sponsors
  const filteredSponsors = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return sponsors;
    return sponsors.filter(s =>
      s.companyName.toLowerCase().includes(q) ||
      (s.contactPerson && s.contactPerson.toLowerCase().includes(q))
    );
  }, [sponsors, searchQuery]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Sparkles size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>الرعاية والمساحات الإعلانية</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>إدارة الشركات الراعية، باقات الترويج، اللوحات الإعلانية، والعقود.</p>
        </div>

        {canManage && activeTab === 'sponsors' && (
          <button className="btn-primary" onClick={() => { setEditingSponsorId(null); setSponsorForm({ companyName: '', contactPerson: '', phone: '', email: '', websiteUrl: '', isActive: true }); setIsSponsorModalOpen(true); }}>
            <Plus size={18} /> راعي جديد
          </button>
        )}

        {canManage && activeTab === 'packages' && (
          <button className="btn-primary" onClick={() => setIsPackageModalOpen(true)} disabled={exhibitions.length === 0}>
            <Plus size={18} /> باقة رعاية جديدة
          </button>
        )}
      </div>

      {/* Tabs */}
      <div style={{ display: 'flex', gap: '10px', borderBottom: '1px solid var(--border-main)', paddingBottom: '10px', overflowX: 'auto' }}>
        <button
          className={activeTab === 'sponsors' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => setActiveTab('sponsors')}
          style={{ height: '38px', padding: '0 16px', fontSize: '13px' }}
        >
          <Building2 size={16} />
          <span>الشركات الراعية ({sponsors.length})</span>
        </button>
        <button
          className={activeTab === 'packages' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => setActiveTab('packages')}
          style={{ height: '38px', padding: '0 16px', fontSize: '13px' }}
        >
          <Sparkles size={16} />
          <span>باقات الرعاية ({packages.length})</span>
        </button>
        <button
          className={activeTab === 'spaces' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => setActiveTab('spaces')}
          style={{ height: '38px', padding: '0 16px', fontSize: '13px' }}
        >
          <Layout size={16} />
          <span>المساحات الإعلانية ({spaces.length})</span>
        </button>
        <button
          className={activeTab === 'contracts' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => setActiveTab('contracts')}
          style={{ height: '38px', padding: '0 16px', fontSize: '13px' }}
        >
          <FileCheck size={16} />
          <span>عقود الرعاية ({contracts.length})</span>
        </button>
      </div>

      {/* Exhibition selector for tabs that depend on Exhibition */}
      {(activeTab === 'packages' || activeTab === 'contracts') && (
        <div className="card" style={{ padding: '12px 16px', display: 'flex', alignItems: 'center', gap: '12px' }}>
          <span style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-secondary)' }}>اختر المعرض:</span>
          <select
            className="input-field"
            style={{ width: '260px' }}
            value={selectedExhibitionId}
            onChange={e => setSelectedExhibitionId(e.target.value)}
          >
            {exhibitions.map(ex => (
              <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>{ex.name}</option>
            ))}
          </select>
        </div>
      )}

      {activeTab === 'sponsors' && (
        <input
          className="input-field"
          value={searchQuery}
          onChange={event => setSearchQuery(event.target.value)}
          placeholder="ابحث باسم الشركة الراعية أو جهة الاتصال…"
          style={{ maxWidth: '420px' }}
        />
      )}

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Tab 1: Sponsors */}
      {activeTab === 'sponsors' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="table-container" style={{ border: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>الشركة الراعية</th>
                  <th>ضابط الاتصال</th>
                  <th>الهاتف</th>
                  <th>البريد</th>
                  <th>إجمالي العقود</th>
                  <th>الحالة</th>
                  {canManage && <th>إجراءات</th>}
                </tr>
              </thead>
              <tbody>
                {filteredSponsors.map(item => (
                  <tr key={item.sponsorID}>
                    <td><strong>{item.companyName}</strong></td>
                    <td>{item.contactPerson || '—'}</td>
                    <td dir="ltr">{item.phone || '—'}</td>
                    <td dir="ltr">{item.email || '—'}</td>
                    <td><span className="badge badge-info">{item.totalContractsCount} عقود</span></td>
                    <td>
                      <span className={`badge ${item.isActive ? 'badge-success' : 'badge-danger'}`}>
                        {item.isActive ? 'نشط' : 'متوقف'}
                      </span>
                    </td>
                    {canManage && (
                      <td>
                        <div style={{ display: 'flex', gap: '8px' }}>
                          <button
                            className="btn-icon"
                            title="تعديل الراعي"
                            onClick={() => {
                              setEditingSponsorId(item.sponsorID);
                              setSponsorForm({
                                companyName: item.companyName,
                                contactPerson: item.contactPerson || '',
                                phone: item.phone || '',
                                email: item.email || '',
                                websiteUrl: item.websiteUrl || '',
                                isActive: item.isActive,
                              });
                              setIsSponsorModalOpen(true);
                            }}
                          >
                            <Edit2 size={14} />
                          </button>
                          <button className="btn-icon" title="حذف" style={{ color: 'var(--danger)' }} onClick={() => void handleDeleteSponsor(item)}>
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
                {!loading && filteredSponsors.length === 0 && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>لا يوجد رعاة مسجلون.</td></tr>
                )}
                {loading && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 2: Packages */}
      {activeTab === 'packages' && (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '20px' }}>
          {packages.map(pkg => (
            <article key={pkg.packageID} className="card" style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span className="badge badge-primary">{pkg.packageName} — {pkg.levelName}</span>
                <span style={{ fontSize: '18px', fontWeight: 800, color: 'var(--text-primary)' }}>
                  {pkg.price.toLocaleString()} {pkg.currencyCode}
                </span>
              </div>
              <p style={{ fontSize: '13px', color: 'var(--text-secondary)', lineHeight: 1.5 }}>
                {pkg.entitlementsDescription || 'لم تُسجل مزايا لهذه الباقة.'}
              </p>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', color: 'var(--text-secondary)', borderTop: '1px solid var(--border-main)', paddingTop: '10px' }}>
                <span>السعة: {pkg.allocatedSponsorsCount} / {pkg.maxSponsorsCount}</span>
                <span>{pkg.isSoldOut ? 'مكتملة' : 'متاحة'}</span>
              </div>
              {canManage && (
                <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '6px' }}>
                  <button className="btn-icon" title="حذف الباقة" style={{ color: 'var(--danger)' }} onClick={() => void handleDeletePackage(pkg)}>
                    <Trash2 size={14} />
                  </button>
                </div>
              )}
            </article>
          ))}
          {!loading && packages.length === 0 && (
            <div className="card" style={{ gridColumn: '1 / -1', textAlign: 'center', padding: '40px', color: 'var(--text-secondary)' }}>
              لا توجد باقات رعاية مضافة لهذا المعرض.
            </div>
          )}
        </div>
      )}

      {/* Tab 3: Advertising Spaces */}
      {activeTab === 'spaces' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="table-container" style={{ border: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>المساحة الإعلانية</th>
                  <th>القاعة / الموقع</th>
                  <th>وصف الموقع</th>
                  <th>السعر القياسي</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {spaces.map(sp => (
                  <tr key={sp.spaceID}>
                    <td><strong>{sp.spaceName}</strong></td>
                    <td>{sp.hallName || 'الممرات العامة'}</td>
                    <td>{sp.locationDescription || 'لوحة مدخل رئيسية'}</td>
                    <td><strong>{sp.basePrice.toLocaleString()} {sp.currencyCode}</strong></td>
                    <td>
                      <span className={`badge ${sp.isAvailable ? 'badge-success' : 'badge-primary'}`}>
                        {sp.isAvailable ? 'متاحة للحجز' : 'محجوزة'}
                      </span>
                    </td>
                  </tr>
                ))}
                {!loading && spaces.length === 0 && (
                  <tr><td colSpan={5} style={{ textAlign: 'center', padding: '30px' }}>لا توجد مساحات إعلانية مسجلة.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 4: Sponsorship Contracts */}
      {activeTab === 'contracts' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="table-container" style={{ border: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>رقم العقد</th>
                  <th>الشركة الراعية</th>
                  <th>الباقة المختارة</th>
                  <th>قيمة العقد</th>
                  <th>تاريخ التوقيع</th>
                  <th>الحالة</th>
                </tr>
              </thead>
              <tbody>
                {contracts.map(c => (
                  <tr key={c.contractID}>
                    <td><strong>#{c.contractID}</strong></td>
                    <td>{c.sponsorCompanyName}</td>
                    <td><span className="badge badge-info">{c.packageName || 'رعاية مخصصة'}</span></td>
                    <td><strong>{c.totalAmount.toLocaleString()} {c.currencyCode}</strong></td>
                    <td>{new Date(c.signedDate).toLocaleDateString('ar-LY')}</td>
                    <td>
                      <span className={`badge ${c.statusName === 'Confirmed' || c.statusName === 'Active' ? 'badge-success' : c.statusName === 'Cancelled' ? 'badge-danger' : 'badge-warning'}`}>
                        {c.statusName}
                      </span>
                    </td>
                  </tr>
                ))}
                {!loading && contracts.length === 0 && (
                  <tr><td colSpan={6} style={{ textAlign: 'center', padding: '30px' }}>لا توجد عقود رعاية لهذا المعرض.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modal: Create/Edit Sponsor */}
      {isSponsorModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleSponsorSubmit} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>{editingSponsorId ? 'تعديل بيانات الراعي' : 'إضافة شركة راعية جديدة'}</h2>
              <button type="button" className="btn-icon" onClick={() => setIsSponsorModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم الشركة الراعية *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: مصرف الجمهورية"
                value={sponsorForm.companyName}
                onChange={e => setSponsorForm(prev => ({ ...prev, companyName: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">ضابط الاتصال</label>
                <input
                  className="input-field"
                  placeholder="مدير التسويق"
                  value={sponsorForm.contactPerson}
                  onChange={e => setSponsorForm(prev => ({ ...prev, contactPerson: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">رقم الهاتف</label>
                <input
                  className="input-field"
                  placeholder="+218 91 0000000"
                  value={sponsorForm.phone}
                  onChange={e => setSponsorForm(prev => ({ ...prev, phone: e.target.value }))}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">البريد الإلكتروني</label>
                <input
                  className="input-field"
                  type="email"
                  placeholder="sponsor@bank.ly"
                  value={sponsorForm.email}
                  onChange={e => setSponsorForm(prev => ({ ...prev, email: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الموقع الإلكتروني</label>
                <input
                  className="input-field"
                  placeholder="https://sponsor.ly"
                  value={sponsorForm.websiteUrl}
                  onChange={e => setSponsorForm(prev => ({ ...prev, websiteUrl: e.target.value }))}
                />
              </div>
            </div>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري الحفظ…' : editingSponsorId ? 'تحديث الراعي' : 'حفظ الراعي'}
            </button>
          </form>
        </div>
      )}

      {/* Modal: Create Package */}
      {isPackageModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handlePackageSubmit} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>إضافة باقة رعاية جديدة</h2>
              <button type="button" className="btn-icon" onClick={() => setIsPackageModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">اسم باقة الرعاية *</label>
              <input
                className="input-field"
                required
                placeholder="الرعاية البلاتينية / الراعي الماسي"
                value={packageForm.packageName}
                onChange={e => setPackageForm(prev => ({ ...prev, packageName: e.target.value }))}
              />
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">مستوى الرعاية *</label>
              <select
                className="input-field"
                required
                value={packageForm.level}
                onChange={e => setPackageForm(prev => ({ ...prev, level: e.target.value }))}
              >
                <option value="Platinum">بلاتينية</option>
                <option value="Gold">ذهبية</option>
                <option value="Silver">فضية</option>
                <option value="Bronze">برونزية</option>
                <option value="TitleSponsor">الراعي الرسمي</option>
                <option value="Custom">مخصصة</option>
              </select>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">قيمة الرعاية (د.ل)</label>
                <input
                  className="input-field"
                  type="number"
                  min="0"
                  required
                  value={packageForm.price}
                  onChange={e => setPackageForm(prev => ({ ...prev, price: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الحد الأقصى للرعاة</label>
                <input
                  className="input-field"
                  type="number"
                  min="1"
                  value={packageForm.maxSponsorsCount}
                  onChange={e => setPackageForm(prev => ({ ...prev, maxSponsorsCount: e.target.value }))}
                />
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">تفاصيل ومميزات الباقة</label>
              <textarea
                className="input-field"
                rows={3}
                placeholder="وضع الشعار في شاشات القاعة، كلمة افتتاحية، بطاقات VIP مجانية…"
                value={packageForm.entitlementsDescription}
                onChange={e => setPackageForm(prev => ({ ...prev, entitlementsDescription: e.target.value }))}
              />
            </div>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري الحفظ…' : 'حفظ الباقة'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
