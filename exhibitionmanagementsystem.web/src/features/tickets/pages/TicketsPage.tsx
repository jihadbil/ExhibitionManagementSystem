import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  IdCard,
  Plus,
  Printer,
  QrCode,
  Search,
  Ticket,
  UserCheck,
  UserPlus,
  Users,
  X
} from 'lucide-react';
import { QRCodeSVG } from 'qrcode.react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

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
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Modals State
  const [isVisitorModalOpen, setIsVisitorModalOpen] = useState(false);
  const [visitorForm, setVisitorForm] = useState<VisitorFormData>(emptyVisitorForm);
  const [isSavingVisitor, setIsSavingVisitor] = useState(false);

  const [isIssueModalOpen, setIsIssueModalOpen] = useState(false);
  const [issueForm, setIssueForm] = useState<IssueTicketFormData>(emptyIssueForm);
  const [isIssuingTicket, setIsIssuingTicket] = useState(false);

  // Print/View Ticket Modal
  const [selectedTicketForPrint, setSelectedTicketForPrint] = useState<TicketItem | null>(null);

  // 1. Load Exhibitions on mount
  useEffect(() => {
    const loadExhibitions = async () => {
      try {
        const response = await api.get<{ items: Exhibition[] }>('/api/exhibitions?page=1&pageSize=100');
        const list = response.data.items ?? [];
        setExhibitions(list);
        if (list.length > 0) {
          setSelectedExhibition(list[0].exhibitionID.toString());
          setIssueForm(prev => ({ ...prev, exhibitionID: list[0].exhibitionID.toString() }));
        }
      } catch {
        setError('تعذر تحميل قائمة المعارض.');
      }
    };
    void loadExhibitions();
  }, []);

  // 2. Load Tickets when Exhibition changes
  const loadTickets = useCallback(async (exId: string) => {
    if (!exId) return;
    setLoading(true);
    setError('');
    try {
      const response = await api.get<TicketItem[]>(`/api/tickets/exhibition/${exId}`);
      setTickets(response.data ?? []);
    } catch {
      setError('تعذر تحميل تذاكر المعرض من الخادم.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab === 'tickets' && selectedExhibition) {
      void loadTickets(selectedExhibition);
    }
  }, [selectedExhibition, activeTab, loadTickets]);

  // 3. Load Visitors
  const loadVisitors = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<{ items: Visitor[] }>('/api/visitors?page=1&pageSize=100');
      setVisitors(response.data.items ?? []);
    } catch {
      setError('تعذر تحميل دليل الزوار من الخادم.');
    } finally {
      setLoading(false);
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
      t.visitorName.toLowerCase().includes(q) ||
      t.ticketType.toLowerCase().includes(q) ||
      t.qrCode.toLowerCase().includes(q) ||
      t.ticketID.toString().includes(q)
    );
  }, [tickets, searchQuery]);

  const filteredVisitors = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return visitors;
    return visitors.filter(v =>
      v.fullName.toLowerCase().includes(q) ||
      (v.email && v.email.toLowerCase().includes(q)) ||
      (v.phone && v.phone.toLowerCase().includes(q)) ||
      (v.nationality && v.nationality.toLowerCase().includes(q))
    );
  }, [visitors, searchQuery]);

  // Register New Visitor
  const handleRegisterVisitor = async (e: FormEvent) => {
    e.preventDefault();
    if (!visitorForm.fullName.trim()) {
      showToast('يرجى إدخال اسم الزائر.', 'warning');
      return;
    }

    setIsSavingVisitor(true);
    try {
      await api.post('/api/visitors/register', {
        fullName: visitorForm.fullName.trim(),
        email: visitorForm.email.trim(),
        phone: visitorForm.phone.trim(),
        nationality: visitorForm.nationality.trim(),
        visitorType: visitorForm.visitorType,
      });

      showToast('تم تسجيل الزائر بنجاح.', 'success');
      setIsVisitorModalOpen(false);
      setVisitorForm(emptyVisitorForm);
      await loadVisitors();
    } catch {
      showToast('فشل تسجيل الزائر. تحقق من البيانات المدخلة.', 'danger');
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
      setSelectedTicketForPrint(response.data);
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
              <button className="btn-secondary" onClick={() => setIsVisitorModalOpen(true)}>
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
          onClick={() => setActiveTab('tickets')}
          style={{ height: '38px', padding: '0 18px', fontSize: '13px' }}
        >
          <Ticket size={16} />
          <span>التذاكر الصادرة ({tickets.length})</span>
        </button>
        <button
          className={activeTab === 'visitors' ? 'btn-primary' : 'btn-secondary'}
          onClick={() => setActiveTab('visitors')}
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
                      <span className={`badge ${ticket.status === 'Valid' ? 'badge-success' : 'badge-danger'}`}>
                        {ticket.status === 'Valid' ? 'صالحة' : ticket.status}
                      </span>
                    </td>
                    <td>
                      <button
                        className="btn-secondary"
                        style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                        onClick={() => setSelectedTicketForPrint(ticket)}
                      >
                        <QrCode size={14} /> عرض التذكرة
                      </button>
                    </td>
                  </tr>
                ))}
                {!loading && filteredTickets.length === 0 && (
                  <tr><td colSpan={8} style={{ textAlign: 'center', padding: '30px' }}>لا توجد تذاكر مسجلة لهذا المعرض.</td></tr>
                )}
                {loading && (
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
                        <button
                          className="btn-secondary"
                          style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                          onClick={() => openIssueForVisitor(visitor)}
                        >
                          <Ticket size={14} /> إصدار تذكرة
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
                {!loading && filteredVisitors.length === 0 && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>لا يوجد زوار مسجلون.</td></tr>
                )}
                {loading && (
                  <tr><td colSpan={canManage ? 7 : 6} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Modal: Register Visitor */}
      {isVisitorModalOpen && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <form className="card" onSubmit={handleRegisterVisitor} style={{ width: 'min(500px, 100%)', display: 'grid', gap: '14px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2>تسجيل زائر جديد</h2>
              <button type="button" className="btn-icon" onClick={() => setIsVisitorModalOpen(false)}><X size={18} /></button>
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
                <label className="form-label">البريد الإلكتروني</label>
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
              {isSavingVisitor ? 'جاري التسجيل…' : 'تسجيل الزائر'}
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
          <div className="card" style={{ width: 'min(440px, 100%)', display: 'flex', flexDirection: 'column', gap: '16px', textAlign: 'center' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h2 style={{ fontSize: '18px' }}>تذكرة الدخول الرسمية</h2>
              <button type="button" className="btn-icon" onClick={() => setSelectedTicketForPrint(null)}><X size={18} /></button>
            </div>

            <div style={{ border: '2px dashed var(--primary)', borderRadius: '12px', padding: '24px 20px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px', background: '#ffffff' }}>
              <span className="badge badge-primary">{selectedTicketForPrint.ticketType} PASS</span>
              <h3 style={{ fontSize: '18px', fontWeight: 800, margin: 0 }}>{selectedTicketForPrint.visitorName}</h3>
              <p style={{ fontSize: '13px', color: 'var(--text-secondary)', margin: 0 }}>{selectedTicketForPrint.exhibitionName}</p>

              <div style={{ padding: '12px', background: '#fff', border: '1px solid #e2e8f0', borderRadius: '10px', marginTop: '6px' }}>
                <QRCodeSVG value={selectedTicketForPrint.qrCode || `TICKET-${selectedTicketForPrint.ticketID}`} size={140} level="M" />
              </div>

              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '4px' }}>
                <div>رقم التذكرة: <strong>#{selectedTicketForPrint.ticketID}</strong></div>
                <div dir="ltr">{selectedTicketForPrint.qrCode}</div>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '10px' }}>
              <button className="btn-secondary" style={{ flex: 1 }} onClick={() => setSelectedTicketForPrint(null)}>
                إغلاق
              </button>
              <button className="btn-primary" style={{ flex: 1 }} onClick={() => window.print()}>
                <Printer size={16} /> طباعة
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
