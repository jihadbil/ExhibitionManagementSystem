import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  Plus,
  Printer,
  QrCode,
  Search,
  Ticket,
  Pencil,
  UserPlus,
  Users,
  X
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { getAllItems } from '../../../services/pagination';
import { getApiErrorMessage } from '../../../services/apiError';
import { BadgeCard } from '../../badges/components/BadgeCard';

interface Exhibition {
  exhibitionID: number;
  name: string;
}

interface Visitor {
  visitorID: number;
  fullName: string;
  email?: string;
  phone?: string;
  nationality?: string;
  visitorType: string;
  registeredAt: string;
  ticketsCount?: number;
}

interface TicketItem {
  ticketID: number;
  visitorID: number;
  visitorName: string;
  exhibitionID: number;
  exhibitionName: string;
  ticketType: string;
  price: number;
  currencyCode: string;
  qrCode: string;
  validDate?: string;
  status: string;
  issuedAt: string;
  scansCount: number;
}

interface BadgePrintPayload {
  fullName: string;
  companyName?: string;
  jobTitle?: string;
  participantTypeTitle: string;
  qrCode: string;
  barcode?: string;
  exhibitionName: string;
  venueAndHallName?: string;
  datesText?: string;
  boothNumber?: string;
  headerBgColor: string;
  headerTextColor: string;
  accentColor: string;
  categoryBadgeText: string;
  showLogo: boolean;
  showQRCode: boolean;
  showBarcode: boolean;
  showCompanyName: boolean;
  showJobTitle: boolean;
  showHallName: boolean;
  showDates: boolean;
  logoUrl?: string;
  footerText?: string;
  widthMm: number;
  heightMm: number;
  orientation: number;
}

interface VisitorFormData {
  fullName: string;
  email: string;
  phone: string;
  nationality: string;
  visitorType: string;
}

const emptyVisitorForm: VisitorFormData = {
  fullName: '',
  email: '',
  phone: '',
  nationality: 'ليبي',
  visitorType: 'Regular',
};

interface IssueTicketFormData {
  visitorID: string;
  exhibitionID: string;
  ticketType: string;
  price: string;
  currencyCode: string;
  validDate: string;
}

const emptyIssueForm: IssueTicketFormData = {
  visitorID: '',
  exhibitionID: '',
  ticketType: 'Regular',
  price: '25',
  currencyCode: 'LYD',
  validDate: '',
};

export const TicketsPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManage = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'EventManager') ?? false;

  const [activeTab, setActiveTab] = useState<'tickets' | 'visitors'>('tickets');
  const [exhibitions, setExhibitions] = useState<Exhibition[]>([]);
  const [selectedExhibition, setSelectedExhibition] = useState('');
  const [tickets, setTickets] = useState<TicketItem[]>([]);
  const [visitors, setVisitors] = useState<Visitor[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [ticketsLoading, setTicketsLoading] = useState(false);
  const [visitorsLoading, setVisitorsLoading] = useState(false);
  const [error, setError] = useState('');

  // Modals State
  const [isVisitorModalOpen, setIsVisitorModalOpen] = useState(false);
  const [editingVisitor, setEditingVisitor] = useState<Visitor | null>(null);
  const [visitorForm, setVisitorForm] = useState<VisitorFormData>(emptyVisitorForm);
  const [isSavingVisitor, setIsSavingVisitor] = useState(false);

  const [isIssueModalOpen, setIsIssueModalOpen] = useState(false);
  const [issueForm, setIssueForm] = useState<IssueTicketFormData>(emptyIssueForm);
  const [isIssuingTicket, setIsIssuingTicket] = useState(false);

  // Print/View Ticket Modal
  const [selectedTicketForPrint, setSelectedTicketForPrint] = useState<TicketItem | null>(null);
  const [badgePrintPayload, setBadgePrintPayload] = useState<BadgePrintPayload | null>(null);
  const [badgePreviewLoading, setBadgePreviewLoading] = useState(false);
  const [badgePreviewError, setBadgePreviewError] = useState('');

  // 1. Initial Load: Exhibitions & Visitors on mount
  useEffect(() => {
    const initData = async () => {
      setVisitorsLoading(true);
      try {
        const [exRes, visRes] = await Promise.allSettled([
          getAllItems<Exhibition>('/api/exhibitions'),
          getAllItems<Visitor>('/api/visitors')
        ]);

        if (exRes.status === 'fulfilled') {
          const list = exRes.value;
          setExhibitions(list);
          if (list.length > 0) {
            setSelectedExhibition(list[0].exhibitionID.toString());
            setIssueForm(prev => ({ ...prev, exhibitionID: list[0].exhibitionID.toString() }));
          }
        }

        if (visRes.status === 'fulfilled') {
          setVisitors(visRes.value);
        } else {
          setError('تعذر تحميل دليل الزوار من الخادم.');
        }
      } catch {
        setError('تعذر تحميل البيانات الأولية للمعرض والزوار.');
      } finally {
        setVisitorsLoading(false);
      }
    };
    void initData();
  }, []);

  // 2. Load Tickets when Exhibition changes
  const loadTickets = useCallback(async (exId: string) => {
    if (!exId) return;
    setTicketsLoading(true);
    setError('');
    try {
      const response = await api.get<TicketItem[]>(`/api/tickets/exhibition/${exId}`);
      setTickets(response.data ?? []);
    } catch {
      setError('تعذر تحميل تذاكر المعرض من الخادم.');
    } finally {
      setTicketsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab === 'tickets' && selectedExhibition) {
      void loadTickets(selectedExhibition);
    }
  }, [selectedExhibition, activeTab, loadTickets]);

  // 3. Load Visitors
  const loadVisitors = useCallback(async () => {
    setVisitorsLoading(true);
    setError('');
    try {
      setVisitors(await getAllItems<Visitor>('/api/visitors'));
    } catch {
      setError('تعذر تحميل دليل الزوار من الخادم.');
    } finally {
      setVisitorsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab === 'visitors') {
      void loadVisitors();
    }
  }, [activeTab, loadVisitors]);

  // Filtered Lists
  const filteredTickets = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return tickets;
    return tickets.filter(t =>
      (t.visitorName || '').toLowerCase().includes(q) ||
      (t.ticketType || '').toLowerCase().includes(q) ||
      (t.qrCode || '').toLowerCase().includes(q) ||
      t.ticketID.toString().includes(q)
    );
  }, [tickets, searchQuery]);

  const filteredVisitors = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return visitors;
    return visitors.filter(v =>
      (v.fullName || '').toLowerCase().includes(q) ||
      ((v.email || '') && (v.email || '').toLowerCase().includes(q)) ||
      ((v.phone || '') && (v.phone || '').toLowerCase().includes(q)) ||
      ((v.nationality || '') && (v.nationality || '').toLowerCase().includes(q)) ||
      ((v.visitorType || '') && (v.visitorType || '').toLowerCase().includes(q))
    );
  }, [visitors, searchQuery]);

  const closeTicketPreview = () => {
    setSelectedTicketForPrint(null);
    setBadgePrintPayload(null);
    setBadgePreviewError('');
  };

  const openTicketPreview = async (ticket: TicketItem) => {
    setSelectedTicketForPrint(ticket);
    setBadgePrintPayload(null);
    setBadgePreviewError('');
    setBadgePreviewLoading(true);

    try {
      const response = await api.get<BadgePrintPayload>(
        `/api/badges/render-by-qr/${encodeURIComponent(ticket.qrCode)}`
      );
      setBadgePrintPayload(response.data);
    } catch {
      setBadgePreviewError('تعذر إنشاء المعاينة من قالب الشارة المحفوظ. تأكد من وجود قالب مناسب لهذا النوع.');
      showToast('تعذر تحميل قالب الشارة لهذه التذكرة.', 'danger');
    } finally {
      setBadgePreviewLoading(false);
    }
  };

  const openCreateVisitor = () => {
    setEditingVisitor(null);
    setVisitorForm(emptyVisitorForm);
    setIsVisitorModalOpen(true);
  };

  const openEditVisitor = (visitor: Visitor) => {
    setEditingVisitor(visitor);
    setVisitorForm({
      fullName: visitor.fullName,
      email: visitor.email ?? '',
      phone: visitor.phone ?? '',
      nationality: visitor.nationality ?? '',
      visitorType: visitor.visitorType || 'Regular',
    });
    setIsVisitorModalOpen(true);
  };

  const closeVisitorModal = () => {
    setIsVisitorModalOpen(false);
    setEditingVisitor(null);
    setVisitorForm(emptyVisitorForm);
  };

  // Register or update a visitor
  const handleSaveVisitor = async (e: FormEvent) => {
    e.preventDefault();
    if (!visitorForm.fullName.trim()) {
      showToast('يرجى إدخال اسم الزائر.', 'warning');
      return;
    }

    setIsSavingVisitor(true);
    try {
      const payload = {
        fullName: visitorForm.fullName.trim(),
        email: visitorForm.email.trim() || null,
        phone: visitorForm.phone.trim(),
        nationality: visitorForm.nationality.trim(),
        visitorType: visitorForm.visitorType,
      };

      if (editingVisitor) {
        await api.put(`/api/visitors/${editingVisitor.visitorID}`, payload);
      } else {
        await api.post('/api/visitors/register', payload);
      }

      showToast(editingVisitor ? 'تم تحديث بيانات الزائر بنجاح.' : 'تم تسجيل الزائر بنجاح.', 'success');
      closeVisitorModal();
      await loadVisitors();
    } catch (error: unknown) {
      showToast(
        getApiErrorMessage(
          error,
          editingVisitor
            ? 'فشل تحديث بيانات الزائر. تحقق من البيانات المدخلة.'
            : 'فشل تسجيل الزائر. تحقق من البيانات المدخلة.',
        ),
        'danger',
      );
    } finally {
      setIsSavingVisitor(false);
    }
  };

  // Issue New Ticket
  const handleIssueTicket = async (e: FormEvent) => {
    e.preventDefault();
    if (!issueForm.visitorID || !issueForm.exhibitionID) {
      showToast('يرجى تحديد الزائر والمعرض.', 'warning');
      return;
    }

    setIsIssuingTicket(true);
    try {
      const response = await api.post<TicketItem>('/api/tickets', {
        visitorID: Number(issueForm.visitorID),
        exhibitionID: Number(issueForm.exhibitionID),
        ticketType: issueForm.ticketType,
        price: Number(issueForm.price) || 0,
        currencyCode: issueForm.currencyCode,
        validDate: issueForm.validDate ? new Date(issueForm.validDate).toISOString() : null,
      });

      showToast('تم إصدار التذكرة بنجاح.', 'success');
      setIsIssueModalOpen(false);
      if (selectedExhibition === issueForm.exhibitionID) {
        await loadTickets(selectedExhibition);
      }
      await openTicketPreview(response.data);
    } catch {
      showToast('فشل إصدار التذكرة. تحقق من صحة البيانات.', 'danger');
    } finally {
      setIsIssuingTicket(false);
    }
  };

  const openIssueForVisitor = (v: Visitor) => {
    setIssueForm({
      visitorID: v.visitorID.toString(),
      exhibitionID: selectedExhibition || (exhibitions[0]?.exhibitionID.toString() ?? ''),
      ticketType: 'Regular',
      price: '25',
      currencyCode: 'LYD',
      validDate: '',
    });
    setIsIssueModalOpen(true);
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Page Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Ticket size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>التذاكر والزوار</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>إدارة سجل الزوار، إصدار التذاكر الفورية، ورموز الدخول الذاتية.</p>
        </div>

        <div style={{ display: 'flex', gap: '10px' }}>
          {canManage && (
            <>
              <button className="btn-secondary" onClick={openCreateVisitor}>
                <UserPlus size={16} /> تسجيل زائر جديد
              </button>
              <button
                className="btn-primary"
                onClick={() => {
                  setIssueForm({
                    ...emptyIssueForm,
                    exhibitionID: selectedExhibition || (exhibitions[0]?.exhibitionID.toString() ?? ''),
                    visitorID: visitors[0]?.visitorID.toString() ?? '',
                  });
                  setIsIssueModalOpen(true);
                }}
                disabled={exhibitions.length === 0}
              >
                <Plus size={18} /> إصدار تذكرة
              </button>
            </>
          )}
        </div>
      </div>

      {/* Tabs Switcher */}
      <div style={{ display: 'flex', gap: '10px', borderBottom: '1px solid var(--border-main)', paddingBottom: '10px' }}>
        <button
          className={activeTab === 'tickets' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => { setActiveTab('tickets'); setSearchQuery(''); }}
          style={{ height: '38px', padding: '0 18px', fontSize: '13px' }}
        >
          <Ticket size={16} />
          <span>التذاكر الصادرة ({tickets.length})</span>
        </button>
        <button
          className={activeTab === 'visitors' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => { setActiveTab('visitors'); setSearchQuery(''); }}
          style={{ height: '38px', padding: '0 18px', fontSize: '13px' }}
        >
          <Users size={16} />
          <span>دليل الزوار المسجلين</span>
        </button>
      </div>

      {/* Filters and Controls */}
      <div style={{ display: 'flex', gap: '14px', flexWrap: 'wrap', alignItems: 'center' }}>
        {activeTab === 'tickets' && (
          <div style={{ minWidth: '240px' }}>
            <select
              className="input-field"
              value={selectedExhibition}
              onChange={e => setSelectedExhibition(e.target.value)}
            >
              {exhibitions.length === 0 && <option value="">لا توجد معارض مسجلة</option>}
              {exhibitions.map(ex => (
                <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>{ex.name}</option>
              ))}
            </select>
          </div>
        )}

        <div className="card" style={{ flex: 1, minWidth: '260px', position: 'relative', padding: 0 }}>
          <Search size={18} style={{ position: 'absolute', right: '16px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            className="input-field"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            style={{ paddingRight: '44px', border: 0 }}
            placeholder={activeTab === 'tickets' ? 'ابحث باسم الزائر أو نوع التذكرة أو الرمز…' : 'ابحث باسم الزائر أو الهاتف أو البريد…'}
          />
        </div>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Tab 1: Issued Tickets Table */}
      {activeTab === 'tickets' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="table-container" style={{ border: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>رقم التذكرة</th>
                  <th>اسم الزائر</th>
                  <th>الفئة / النوع</th>
                  <th>القيمة والعملة</th>
                  <th>تاريخ الإصدار</th>
                  <th>مرات المسح</th>
                  <th>الحالة</th>
                  <th>معاينة وطباعة</th>
                </tr>
              </thead>
              <tbody>
                {filteredTickets.map(ticket => (
                  <tr key={ticket.ticketID}>
                    <td><strong>#{ticket.ticketID}</strong></td>
                    <td>{ticket.visitorName}</td>
                    <td><span className="badge badge-info">{ticket.ticketType}</span></td>
                    <td>{ticket.price.toLocaleString()} {ticket.currencyCode}</td>
                    <td>{new Date(ticket.issuedAt).toLocaleString('ar-LY')}</td>
                    <td>
                      <span className="badge" style={{ backgroundColor: ticket.scansCount > 0 ? 'var(--primary-subtle, rgba(99,102,241,0.1))' : 'var(--bg-main)', color: ticket.scansCount > 0 ? 'var(--primary)' : 'inherit' }}>
                        {ticket.scansCount} مسح
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${ticket.status === 'Active' ? 'badge-success' : ticket.status === 'Used' ? 'badge-info' : 'badge-danger'}`}>
                        {{ Active: 'صالحة', Used: 'مستخدمة', Cancelled: 'ملغاة', Expired: 'منتهية' }[ticket.status] ?? ticket.status}
                      </span>
                    </td>
                    <td>
                      <button
                        className="btn-secondary"
                        style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                        onClick={() => void openTicketPreview(ticket)}
                      >
                        <QrCode size={14} /> عرض التذكرة
                      </button>
                    </td>
                  </tr>
                ))}
                {!ticketsLoading && filteredTickets.length === 0 && (
                  <tr><td colSpan={8} style={{ textAlign: 'center', padding: '30px' }}>لا توجد تذاكر مسجلة لهذا المعرض.</td></tr>
                )}
                {ticketsLoading && (
                  <tr><td colSpan={8} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Tab 2: Visitors Directory */}
      {activeTab === 'visitors' && (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <div className="table-container" style={{ border: 0 }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>اسم الزائر</th>
                  <th>البريد الإلكتروني</th>
                  <th>الهاتف</th>
                  <th>الجنسية</th>
                  <th>الفئة</th>
                  <th>تاريخ التسجيل</th>
                  {canManage && <th>إجراءات</th>}
                </tr>
              </thead>
              <tbody>
                {filteredVisitors.map(visitor => (
                  <tr key={visitor.visitorID}>
                    <td><strong>{visitor.fullName}</strong></td>
                    <td dir="ltr" style={{ textAlign: 'right' }}>{visitor.email || '—'}</td>
                    <td dir="ltr" style={{ textAlign: 'right' }}>{visitor.phone || '—'}</td>
                    <td>{visitor.nationality || 'ليبي'}</td>
                    <td><span className="badge badge-info">{visitor.visitorType}</span></td>
                    <td>{new Date(visitor.registeredAt).toLocaleDateString('ar-LY')}</td>
                    {canManage && (
                      <td>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', flexWrap: 'wrap' }}>
                          <button
                            className="btn-secondary"
                            style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                            onClick={() => openEditVisitor(visitor)}
                          >
                            <Pencil size={14} /> تعديل
                          </button>
                          <button
                            className="btn-secondary"
                            style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                            onClick={() => openIssueForVisitor(visitor)}
                          >
                            <Ticket size={14} /> إصدار تذكرة
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
                {!visitorsLoading && filteredVisitors.length === 0 && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>لا يوجد زوار مسجلون.</td></tr>
                )}
                {visitorsLoading && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modal: Register or edit visitor */}
      {isVisitorModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleSaveVisitor} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
              <h2>{editingVisitor ? 'تعديل بيانات الزائر' : 'تسجيل زائر جديد'}</h2>
              <button type="button" className="btn-icon" onClick={closeVisitorModal}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الاسم الكامل *</label>
              <input
                className="input-field"
                required
                placeholder="مثال: د. علي رمضان"
                value={visitorForm.fullName}
                onChange={e => setVisitorForm(prev => ({ ...prev, fullName: e.target.value }))}
              />
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">البريد الإلكتروني (اختياري)</label>
                <input
                  className="input-field"
                  type="email"
                  placeholder="visitor@mail.ly"
                  value={visitorForm.email}
                  onChange={e => setVisitorForm(prev => ({ ...prev, email: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">رقم الهاتف</label>
                <input
                  className="input-field"
                  placeholder="+218 92 0000000"
                  value={visitorForm.phone}
                  onChange={e => setVisitorForm(prev => ({ ...prev, phone: e.target.value }))}
                />
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">الجنسية</label>
                <input
                  className="input-field"
                  placeholder="ليبي"
                  value={visitorForm.nationality}
                  onChange={e => setVisitorForm(prev => ({ ...prev, nationality: e.target.value }))}
                />
              </div>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">فئة الزائر</label>
                <select
                  className="input-field"
                  value={visitorForm.visitorType}
                  onChange={e => setVisitorForm(prev => ({ ...prev, visitorType: e.target.value }))}
                >
                  <option value="Regular">زائر عادي (Regular)</option>
                  <option value="VIP">شخصية هامة (VIP)</option>
                  <option value="Student">طالب / أكاديمي (Student)</option>
                  <option value="Delegate">وفد رسمي (Delegate)</option>
                </select>
              </div>
            </div>

            <button className="btn-primary" type="submit" disabled={isSavingVisitor} style={{ marginTop: '8px' }}>
              {isSavingVisitor
                ? 'جاري الحفظ…'
                : editingVisitor ? 'حفظ التعديلات' : 'تسجيل الزائر'}
            </button>
          </form>
        </div>
      )}

      {/* Modal: Issue Ticket */}
      {isIssueModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleIssueTicket} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>إصدار تذكرة حضور</h2>
              <button type="button" className="btn-icon" onClick={() => setIsIssueModalOpen(false)}><X size={18} /></button>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">المعرض المستهدف *</label>
              <select
                className="input-field"
                required
                value={issueForm.exhibitionID}
                onChange={e => setIssueForm(prev => ({ ...prev, exhibitionID: e.target.value }))}
              >
                {exhibitions.map(ex => (
                  <option key={ex.exhibitionID} value={ex.exhibitionID.toString()}>{ex.name}</option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">الزائر المستفيد *</label>
              <select
                className="input-field"
                required
                value={issueForm.visitorID}
                onChange={e => setIssueForm(prev => ({ ...prev, visitorID: e.target.value }))}
              >
                <option value="">اختر الزائر المسجل</option>
                {visitors.map(v => (
                  <option key={v.visitorID} value={v.visitorID.toString()}>{v.fullName} ({v.phone || v.email || 'بدون هاتف'})</option>
                ))}
              </select>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">نوع التذكرة</label>
                <select
                  className="input-field"
                  value={issueForm.ticketType}
                  onChange={e => setIssueForm(prev => ({ ...prev, ticketType: e.target.value }))}
                >
                  <option value="Regular">عادية (Regular)</option>
                  <option value="VIP">شخصية هامة (VIP)</option>
                  <option value="Student">طلاب (Student)</option>
                  <option value="EarlyBird">تسجيل مبكر (EarlyBird)</option>
                </select>
              </div>

              <div className="form-group" style={{ margin: 0 }}>
                <label className="form-label">سعر التذكرة</label>
                <div style={{ display: 'flex', gap: '6px' }}>
                  <input
                    className="input-field"
                    type="number"
                    min="0"
                    value={issueForm.price}
                    onChange={e => setIssueForm(prev => ({ ...prev, price: e.target.value }))}
                  />
                  <input
                    className="input-field"
                    style={{ width: '70px', textAlign: 'center' }}
                    value={issueForm.currencyCode}
                    onChange={e => setIssueForm(prev => ({ ...prev, currencyCode: e.target.value }))}
                  />
                </div>
              </div>
            </div>

            <div className="form-group" style={{ margin: 0 }}>
              <label className="form-label">تاريخ الصلاحية (اختياري)</label>
              <input
                className="input-field"
                type="date"
                value={issueForm.validDate}
                onChange={e => setIssueForm(prev => ({ ...prev, validDate: e.target.value }))}
              />
            </div>

            <button className="btn-primary" type="submit" disabled={isIssuingTicket} style={{ marginTop: '8px' }}>
              {isIssuingTicket ? 'جاري الإصدار…' : 'إصدار التذكرة الآن'}
            </button>
          </form>
        </div>
      )}

      {/* Modal: View & Print Ticket */}
      {selectedTicketForPrint && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <div className="card" style={{ width: 'min(560px, 100%)', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '16px', textAlign: 'center', maxHeight: '95vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', width: '100%' }}>
              <h2 style={{ fontSize: '18px' }}>تذكرة الدخول الرسمية</h2>
              <button type="button" className="btn-icon" onClick={closeTicketPreview}><X size={18} /></button>
            </div>

            {badgePreviewLoading && (
              <div style={{ padding: '48px 20px', color: 'var(--text-secondary)' }}>جاري تطبيق قالب الشارة المحفوظ…</div>
            )}

            {badgePreviewError && (
              <div role="alert" style={{ padding: '16px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>
                {badgePreviewError}
              </div>
            )}

            {badgePrintPayload && (
              <BadgeCard
                className="ticket-print-target"
                data={{
                  fullName: badgePrintPayload.fullName,
                  companyName: badgePrintPayload.companyName,
                  jobTitle: badgePrintPayload.jobTitle,
                  qrCode: badgePrintPayload.qrCode,
                  barcode: badgePrintPayload.barcode,
                  exhibitionName: badgePrintPayload.exhibitionName,
                  venueAndHallName: badgePrintPayload.venueAndHallName,
                  datesText: badgePrintPayload.datesText,
                  boothNumber: badgePrintPayload.boothNumber,
                }}
                design={{
                  widthMm: badgePrintPayload.widthMm,
                  heightMm: badgePrintPayload.heightMm,
                  orientation: badgePrintPayload.orientation,
                  headerBgColor: badgePrintPayload.headerBgColor,
                  headerTextColor: badgePrintPayload.headerTextColor,
                  accentColor: badgePrintPayload.accentColor,
                  categoryBadgeText: badgePrintPayload.categoryBadgeText || badgePrintPayload.participantTypeTitle,
                  showLogo: badgePrintPayload.showLogo,
                  logoUrl: badgePrintPayload.logoUrl,
                  showQRCode: badgePrintPayload.showQRCode,
                  showBarcode: badgePrintPayload.showBarcode,
                  showCompanyName: badgePrintPayload.showCompanyName,
                  showJobTitle: badgePrintPayload.showJobTitle,
                  showHallName: badgePrintPayload.showHallName,
                  showDates: badgePrintPayload.showDates,
                  footerText: badgePrintPayload.footerText,
                }}
              />
            )}

            <div style={{ display: 'flex', gap: '10px', width: '100%' }}>
              <button className="btn-secondary" style={{ flex: 1 }} onClick={closeTicketPreview}>
                إغلاق
              </button>
              <button className="btn-primary" style={{ flex: 1 }} onClick={() => window.print()} disabled={!badgePrintPayload || badgePreviewLoading}>
                <Printer size={16} /> طباعة
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
