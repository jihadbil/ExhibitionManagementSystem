import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Compass,
  Edit2,
  Filter,
  Plus,
  Search,
  Store,
  Trash2,
  X
} from 'lucide-react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface Venue {
  venueID: number;
  name: string;
}

interface Hall {
  hallID: number;
  venueID: number;
  hallName: string;
}

interface Booth {
  boothID: number;
  hallID: number;
  hallName: string;
  boothNumber: string;
  currentAreaSqM: number;
  originalAreaSqM?: number;
  width?: number;
  height?: number;
  shapeType?: string;
  status: string;
  isMerged: boolean;
  posX?: number;
  posY?: number;
}

interface BoothFormData {
  hallID: string;
  boothNumber: string;
  originalAreaSqM: string;
  width: string;
  height: string;
  shapeType: string;
  status: string;
}

const emptyForm: BoothFormData = {
  hallID: '',
  boothNumber: '',
  originalAreaSqM: '12',
  width: '4',
  height: '3',
  shapeType: 'Standard',
  status: 'Available',
};

const statusNames: Record<string, string> = {
  Available: 'متاح للحجز',
  Reserved: 'محجوز ومؤكد',
  PendingReview: 'قيد المراجعة',
  Occupied: 'مشغول',
};

const shapeNames: Record<string, string> = {
  Standard: 'قياسي (Standard)',
  Corner: 'ناصية (Corner)',
  Premium: 'مميز (Premium)',
  VIP: 'خاص (VIP)',
};

export const BoothsPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin') ?? false;

  const [booths, setBooths] = useState<Booth[]>([]);
  const [halls, setHalls] = useState<Hall[]>([]);
  const [selectedHallFilter, setSelectedHallFilter] = useState<string>('All');
  const [selectedStatusFilter, setSelectedStatusFilter] = useState<string>('All');
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingBoothId, setEditingBoothId] = useState<number | null>(null);
  const [form, setForm] = useState<BoothFormData>(emptyForm);
  const [isSaving, setIsSaving] = useState(false);

  // Load Initial Data (Booths & Halls)
  const loadData = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [boothsRes, venuesRes] = await Promise.allSettled([
        api.get<{ items: Booth[] }>('/api/booths?pageNumber=1&pageSize=200'),
        api.get<Venue[]>('/api/venues'),
      ]);

      if (boothsRes.status === 'fulfilled') {
        setBooths(boothsRes.value.data.items ?? []);
      } else {
        setError('تعذر تحميل بيانات الأجنحة.');
      }

      // Collect halls from venues
      if (venuesRes.status === 'fulfilled' && venuesRes.value.data) {
        const venueList = venuesRes.value.data;
        const allHallsPromises = venueList.map(v => api.get<Hall[]>(`/api/halls/venue/${v.venueID}`));
        const hallsResults = await Promise.allSettled(allHallsPromises);
        const collectedHalls: Hall[] = [];
        hallsResults.forEach(res => {
          if (res.status === 'fulfilled' && Array.isArray(res.value.data)) {
            collectedHalls.push(...res.value.data);
          }
        });
        setHalls(collectedHalls);
        if (collectedHalls.length > 0 && !form.hallID) {
          setForm(prev => ({ ...prev, hallID: collectedHalls[0].hallID.toString() }));
        }
      }
    } catch {
      setError('تعذر استرجاع البيانات من الخادم.');
    } finally {
      setLoading(false);
    }
  }, [form.hallID]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  // Statistics
  const stats = useMemo(() => {
    const total = booths.length;
    const available = booths.filter(b => b.status === 'Available').length;
    const reserved = booths.filter(b => b.status === 'Reserved' || b.status === 'Occupied').length;
    const pending = booths.filter(b => b.status === 'PendingReview').length;
    return { total, available, reserved, pending };
  }, [booths]);

  // Filtered Booths
  const filteredBooths = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return booths.filter(b => {
      const matchesHall = selectedHallFilter === 'All' || b.hallID.toString() === selectedHallFilter;
      const matchesStatus = selectedStatusFilter === 'All' || b.status.toLowerCase() === selectedStatusFilter.toLowerCase();
      const matchesSearch = !q ||
        b.boothNumber.toLowerCase().includes(q) ||
        (b.hallName && b.hallName.toLowerCase().includes(q));
      return matchesHall && matchesStatus && matchesSearch;
    });
  }, [booths, selectedHallFilter, selectedStatusFilter, searchQuery]);

  // Open Create Modal
  const openCreateModal = () => {
    setEditingBoothId(null);
    setForm({
      ...emptyForm,
      hallID: halls[0]?.hallID.toString() ?? '',
    });
    setIsModalOpen(true);
  };

  // Open Edit Modal
  const openEditModal = (booth: Booth) => {
    setEditingBoothId(booth.boothID);
    setForm({
      hallID: booth.hallID ? booth.hallID.toString() : '',
      boothNumber: booth.boothNumber,
      originalAreaSqM: (booth.currentAreaSqM || booth.originalAreaSqM || 12).toString(),
      width: (booth.width || 4).toString(),
      height: (booth.height || 3).toString(),
      shapeType: booth.shapeType || 'Standard',
      status: booth.status || 'Available',
    });
    setIsModalOpen(true);
  };

  // Submit Create / Edit
  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!form.boothNumber.trim() || !form.hallID) {
      showToast('يرجى تحديد القاعة ورقم الجناح.', 'warning');
      return;
    }

    setIsSaving(true);
    try {
      if (editingBoothId) {
        await api.put(`/api/booths/${editingBoothId}`, {
          boothNumber: form.boothNumber.trim(),
          status: form.status,
          width: Number(form.width) || null,
          height: Number(form.height) || null,
          shapeType: form.shapeType,
        });
        showToast('تم تحديث بيانات الجناح بنجاح.', 'success');
      } else {
        await api.post('/api/booths', {
          hallID: Number(form.hallID),
          boothNumber: form.boothNumber.trim(),
          originalAreaSqM: Number(form.originalAreaSqM) || 12,
          width: Number(form.width) || null,
          height: Number(form.height) || null,
          shapeType: form.shapeType,
        });
        showToast('تمت إضافة الجناح بنجاح.', 'success');
      }

      setIsModalOpen(false);
      await loadData();
    } catch {
      showToast('فشل حفظ الجناح. قد يكون رقم الجناح مكرراً في نفس القاعة.', 'danger');
    } finally {
      setIsSaving(false);
    }
  };

  // Delete Booth
  const handleDelete = async (booth: Booth) => {
    if (!window.confirm(`هل أنت متأكد من حذف الجناح «${booth.boothNumber}»؟`)) return;

    try {
      await api.delete(`/api/booths/${booth.boothID}`);
      showToast('تم حذف الجناح بنجاح.', 'success');
      await loadData();
    } catch {
      showToast('تعذر حذف الجناح؛ قد يكون مرتبطاً بحجز نشط.', 'danger');
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Store size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>سجل الأجنحة والمساحات</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>إدارة أجنحة المعارض، المساحات، والأبعاد، وحالة التوافر.</p>
        </div>
        <div style={{ display: 'flex', gap: '10px' }}>
          <NavLink className="btn-secondary" to="/booths/designer">
            <Compass size={17} /> مصمم المخططات 2D
          </NavLink>
          {canManage && (
            <button className="btn-primary" onClick={openCreateModal} disabled={halls.length === 0}>
              <Plus size={18} /> جناح جديد
            </button>
          )}
        </div>
      </div>

      {/* Stat Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '16px' }}>
        <div className="card" style={{ padding: '16px 20px' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>إجمالي الأجنحة</span>
          <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--text-primary)' }}>{stats.total}</h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--success)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المتاحة للحجز</span>
          <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--success)' }}>{stats.available}</h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--primary)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المحجوزة</span>
          <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--primary)' }}>{stats.reserved}</h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--warning)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>قيد المراجعة</span>
          <h2 style={{ fontSize: '24px', fontWeight: 800, marginTop: '4px', color: 'var(--warning)' }}>{stats.pending}</h2>
        </div>
      </div>

      {/* Filters Bar */}
      <div style={{ display: 'flex', gap: '14px', flexWrap: 'wrap', alignItems: 'center' }}>
        <div className="card" style={{ flex: 1, minWidth: '260px', position: 'relative', padding: 0 }}>
          <Search size={18} style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            className="input-field"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            style={{ paddingRight: '44px', border: 0 }}
            placeholder="ابحث برقم الجناح أو القاعة…"
          />
        </div>

        {/* Hall Filter */}
        <div style={{ minWidth: '180px' }}>
          <select
            className="input-field"
            value={selectedHallFilter}
            onChange={e => setSelectedHallFilter(e.target.value)}
          >
            <option value="All">جميع القاعات ({halls.length})</option>
            {halls.map(h => (
              <option key={h.hallID} value={h.hallID.toString()}>{h.hallName}</option>
            ))}
          </select>
        </div>

        {/* Status Filter */}
        <div style={{ minWidth: '160px' }}>
          <select
            className="input-field"
            value={selectedStatusFilter}
            onChange={e => setSelectedStatusFilter(e.target.value)}
          >
            <option value="All">جميع الحالات</option>
            <option value="Available">متاح</option>
            <option value="Reserved">محجوز</option>
            <option value="PendingReview">قيد المراجعة</option>
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
                <th>رقم الجناح</th>
                <th>القاعة</th>
                <th>المساحة</th>
                <th>الأبعاد (عرض × طول)</th>
                <th>النوع / الفئة</th>
                <th>الحالة</th>
                {canManage && <th>الإجراءات</th>}
              </tr>
            </thead>
            <tbody>
              {filteredBooths.map(item => (
                <tr key={item.boothID}>
                  <td><strong>{item.boothNumber}</strong></td>
                  <td>{item.hallName || '—'}</td>
                  <td>{item.currentAreaSqM} م²</td>
                  <td>{item.width && item.height ? `${item.width} × ${item.height} م` : '—'}</td>
                  <td>{shapeNames[item.shapeType ?? 'Standard'] ?? item.shapeType}</td>
                  <td>
                    <span className={`badge ${
                      item.status === 'Available' ? 'badge-success' :
                      item.status === 'Reserved' || item.status === 'Occupied' ? 'badge-primary' : 'badge-warning'
                    }`}>
                      {statusNames[item.status] ?? item.status}
                    </span>
                  </td>
                  {canManage && (
                    <td>
                      <div style={{ display: 'flex', gap: '8px' }}>
                        <button className="btn-icon" title="تعديل الجناح" onClick={() => openEditModal(item)}>
                          <Edit2 size={14} />
                        </button>
                        <button className="btn-icon" title="حذف الجناح" style={{ color: 'var(--danger)' }} onClick={() => void handleDelete(item)}>
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
              {!loading && filteredBooths.length === 0 && (
                <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>لا توجد أجنحة مطابقة.</td></tr>
              )}
              {loading && (
                <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: Create / Edit Booth */}
      {isModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleSubmit} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>{editingBoothId ? 'تعديل بيانات الجناح' : 'إضافة جناح جديد'}</h2>
              <button type="button" className="btn-icon" onClick={() => setIsModalOpen(false)}><X size={18} /></button>
            </div>

            {!editingBoothId && (
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">القاعة *</label>
                <select
                  className="input-field"
                  required
                  value={form.hallID}
                  onChange={e => setForm(prev => ({ ...prev, hallID: e.target.value }))}
                >
                  <option value="">اختر القاعة</option>
                  {halls.map(h => (
                    <option key={h.hallID} value={h.hallID.toString()}>{h.hallName}</option>
                  ))}
                </select>
              </div>
            )}

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">رقم الجناح / الرمز *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: A-101 أو VIP-01"
                value={form.boothNumber}
                onChange={e => setForm(prev => ({ ...prev, boothNumber: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">المساحة (م²)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  required
                  value={form.originalAreaSqM}
                  onChange={e => setForm(prev => ({ ...prev, originalAreaSqM: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">العرض (م)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  value={form.width}
                  onChange={e => setForm(prev => ({ ...prev, width: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الطول (م)</label>
                <input
                  className="input-field"
                  type="number"
                  step="0.1"
                  value={form.height}
                  onChange={e => setForm(prev => ({ ...prev, height: e.target.value }))}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">فئة / نوع الجناح</label>
                <select
                  className="input-field"
                  value={form.shapeType}
                  onChange={e => setForm(prev => ({ ...prev, shapeType: e.target.value }))}
                >
                  <option value="Standard">قياسي (Standard)</option>
                  <option value="Corner">ناصية (Corner)</option>
                  <option value="Premium">مميز (Premium)</option>
                  <option value="VIP">خاص (VIP)</option>
                </select>
              </div>

              {editingBoothId && (
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">حالة الجناح</label>
                  <select
                    className="input-field"
                    value={form.status}
                    onChange={e => setForm(prev => ({ ...prev, status: e.target.value }))}
                  >
                    <option value="Available">متاح للحجز</option>
                    <option value="Reserved">محجوز</option>
                    <option value="PendingReview">قيد المراجعة</option>
                  </select>
                </div>
              )}
            </div>

            <button className="btn-primary" type="submit" disabled={isSaving} style={{ marginTop: '8px' }}>
              {isSaving ? 'جاري الحفظ…' : editingBoothId ? 'تحديث الجناح' : 'إضافة الجناح'}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
