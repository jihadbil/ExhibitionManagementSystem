import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  AlertCircle,
  CheckCircle,
  CreditCard,
  DollarSign,
  Eye,
  FileText,
  Plus,
  Receipt,
  Search,
  X
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';

interface Invoice {
  invoiceID: number;
  exhibitorID?: number;
  exhibitorName: string;
  reservationID?: number;
  invoiceNumber: string;
  invoiceDate: string;
  totalAmount: number;
  discountAmount?: number;
  taxAmount?: number;
  adjustedTotalAmount: number;
  refundedAmount: number;
  currencyCode: string;
  currencySymbol?: string;
  status: string; // 'Pending' | 'Paid' | 'Overdue' | 'Cancelled'
  dueDate?: string;
  notes?: string;
}

interface InvoiceItem {
  itemID: number;
  invoiceID: number;
  description: string;
  itemType: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

interface Payment {
  paymentID: number;
  invoiceID: number;
  amount: number;
  paymentMethod: string; // 'Cash' | 'BankTransfer' | 'Check'
  paymentReference?: string;
  paymentDate: string;
  status: string;
  notes?: string;
}

interface PaymentFormData {
  amount: string;
  paymentMethod: string;
  paymentReference: string;
  notes: string;
}

const emptyPaymentForm: PaymentFormData = {
  amount: '',
  paymentMethod: 'Cash',
  paymentReference: '',
  notes: '',
};

const statusNames: Record<string, string> = {
  Pending: 'قيد الانتظار',
  Paid: 'مدفوعة بالكامل',
  Overdue: 'متأخرة السداد',
  Cancelled: 'ملغاة',
};

const paymentMethodNames: Record<string, string> = {
  Cash: 'نقداً (كاش)',
  BankTransfer: 'تحويل مصرفي',
  Check: 'شيك مصدق',
};

export const InvoicesPage = () => {
  const { user } = useAuth();
  const { showToast } = useToast();
  const canManageFinance = user?.roles.some(role => role === 'Admin' || role === 'TenantAdmin' || role === 'Finance') ?? false;

  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [statusFilter, setStatusFilter] = useState<string>('All');
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Details Modal State
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [invoiceItems, setInvoiceItems] = useState<InvoiceItem[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [loadingDetails, setLoadingDetails] = useState(false);

  // Record Payment Form State
  const [paymentForm, setPaymentForm] = useState<PaymentFormData>(emptyPaymentForm);
  const [isSavingPayment, setIsSavingPayment] = useState(false);

  // Load Invoices
  const loadInvoices = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get<{ items: Invoice[] }>('/api/financial/invoices?page=1&pageSize=150');
      setInvoices(response.data.items ?? []);
    } catch {
      setError('تعذر تحميل الفواتير المالية من الخادم.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadInvoices();
  }, [loadInvoices]);

  // Financial Statistics
  const stats = useMemo(() => {
    let totalBilled = 0;
    let totalCollected = 0;
    let overdueCount = 0;
    let pendingCount = 0;

    invoices.forEach(inv => {
      const amount = inv.adjustedTotalAmount || inv.totalAmount;
      totalBilled += amount;
      if (inv.status === 'Paid') {
        totalCollected += amount;
      } else if (inv.status === 'Overdue') {
        overdueCount++;
      } else if (inv.status === 'Pending') {
        pendingCount++;
      }
    });

    const remainingBalance = Math.max(0, totalBilled - totalCollected);
    const currency = invoices[0]?.currencyCode || 'LYD';

    return { totalBilled, totalCollected, remainingBalance, overdueCount, pendingCount, currency };
  }, [invoices]);

  // Filtered Invoices
  const filteredInvoices = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return invoices.filter(inv => {
      const matchesStatus = statusFilter === 'All' || inv.status.toLowerCase() === statusFilter.toLowerCase();
      const matchesSearch = !q ||
        inv.invoiceNumber.toLowerCase().includes(q) ||
        inv.exhibitorName.toLowerCase().includes(q);
      return matchesStatus && matchesSearch;
    });
  }, [invoices, statusFilter, searchQuery]);

  // Open Invoice Details Modal & Load Payments/Items
  const openInvoiceDetails = async (invoice: Invoice) => {
    setSelectedInvoice(invoice);
    setLoadingDetails(true);
    setInvoiceItems([]);
    setPayments([]);

    try {
      const [itemsRes, paymentsRes] = await Promise.allSettled([
        api.get<InvoiceItem[]>(`/api/invoiceitems/invoice/${invoice.invoiceID}`),
        api.get<Payment[]>(`/api/financial/payments/invoice/${invoice.invoiceID}`),
      ]);

      if (itemsRes.status === 'fulfilled') {
        setInvoiceItems(itemsRes.value.data ?? []);
      }
      if (paymentsRes.status === 'fulfilled') {
        setPayments(paymentsRes.value.data ?? []);
      }

      // Pre-fill payment form with remaining amount
      const paid = (paymentsRes.status === 'fulfilled' && paymentsRes.value.data)
        ? paymentsRes.value.data.reduce((sum, p) => sum + p.amount, 0)
        : 0;
      const remaining = Math.max(0, (invoice.adjustedTotalAmount || invoice.totalAmount) - paid);
      setPaymentForm({
        amount: remaining > 0 ? remaining.toString() : '',
        paymentMethod: 'Cash',
        paymentReference: '',
        notes: '',
      });
    } catch {
      showToast('تعذر تحميل تفاصيل الفاتورة كاملة.', 'warning');
    } finally {
      setLoadingDetails(false);
    }
  };

  // Record Payment
  const handleRecordPayment = async (e: FormEvent) => {
    e.preventDefault();
    if (!selectedInvoice) return;
    const amountNum = Number(paymentForm.amount);
    if (!amountNum || amountNum <= 0) {
      showToast('يرجى إدخال مبلغ دفع صالح.', 'warning');
      return;
    }

    setIsSavingPayment(true);
    try {
      await api.post('/api/financial/payments', {
        invoiceID: selectedInvoice.invoiceID,
        amount: amountNum,
        paymentMethod: paymentForm.paymentMethod,
        paymentReference: paymentForm.paymentReference.trim(),
        notes: paymentForm.notes.trim(),
      });

      showToast('تم تسجيل سند القبض والدفعة بنجاح.', 'success');
      await loadInvoices();
      // Reload current modal payments
      const paymentsRes = await api.get<Payment[]>(`/api/financial/payments/invoice/${selectedInvoice.invoiceID}`);
      setPayments(paymentsRes.data ?? []);
      setPaymentForm(emptyPaymentForm);
    } catch {
      showToast('فشل تسجيل الدفعة. تحقق من صلاحيات المالية والمبلغ.', 'danger');
    } finally {
      setIsSavingPayment(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Receipt size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800 }}>الفواتير والتحصيل المالي</h1>
          </div>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>إدارة مستحقات الأجنحة والخدمات، تتبع سندات القبض والدفعات.</p>
        </div>
      </div>

      {/* Financial Overview Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '16px' }}>
        <div className="card" style={{ padding: '16px 20px' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>إجمالي الفواتير الصادرة</span>
          <h2 style={{ fontSize: '22px', fontWeight: 800, marginTop: '4px', color: 'var(--text-primary)' }}>
            {stats.totalBilled.toLocaleString()} <small style={{ fontSize: '13px', fontWeight: 600 }}>{stats.currency}</small>
          </h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--success)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المحصل فعلياً</span>
          <h2 style={{ fontSize: '22px', fontWeight: 800, marginTop: '4px', color: 'var(--success)' }}>
            {stats.totalCollected.toLocaleString()} <small style={{ fontSize: '13px', fontWeight: 600 }}>{stats.currency}</small>
          </h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--primary)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المتبقي قيد التحصيل</span>
          <h2 style={{ fontSize: '22px', fontWeight: 800, marginTop: '4px', color: 'var(--primary)' }}>
            {stats.remainingBalance.toLocaleString()} <small style={{ fontSize: '13px', fontWeight: 600 }}>{stats.currency}</small>
          </h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--danger)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>الفواتير المتأخرة</span>
          <h2 style={{ fontSize: '22px', fontWeight: 800, marginTop: '4px', color: 'var(--danger)' }}>
            {stats.overdueCount} <small style={{ fontSize: '13px', fontWeight: 600 }}>فاتورة</small>
          </h2>
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
            placeholder="ابحث برقم الفاتورة أو اسم العارض…"
          />
        </div>

        {/* Status Filters */}
        <div style={{ display: 'flex', gap: '8px' }}>
          {['All', 'Pending', 'Paid', 'Overdue'].map(st => (
            <button
              key={st}
              onClick={() => setStatusFilter(st)}
              className={statusFilter === st ? 'btn-primary' : 'btn-secondary'}
              style={{ height: '42px', padding: '0 16px', fontSize: '13px' }}
            >
              {st === 'All' ? 'جميع الفواتير' : statusNames[st] ?? st}
            </button>
          ))}
        </div>
      </div>

      {error && <div role="alert" style={{ padding: '14px', background: 'var(--danger-bg)', color: 'var(--danger-text)', borderRadius: 'var(--radius-lg)' }}>{error}</div>}

      {/* Table Card */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>رقم الفاتورة</th>
                <th>الشركة العارضة</th>
                <th>تاريخ الفاتورة</th>
                <th>الإجمالي المطلوب</th>
                <th>المسترد</th>
                <th>تاريخ الاستحقاق</th>
                <th>الحالة</th>
                <th>الإجراءات</th>
              </tr>
            </thead>
            <tbody>
              {filteredInvoices.map(invoice => (
                <tr key={invoice.invoiceID}>
                  <td><strong>{invoice.invoiceNumber}</strong></td>
                  <td>{invoice.exhibitorName}</td>
                  <td>{new Date(invoice.invoiceDate).toLocaleDateString('ar-LY')}</td>
                  <td><strong>{(invoice.adjustedTotalAmount || invoice.totalAmount).toLocaleString()} {invoice.currencyCode}</strong></td>
                  <td>{invoice.refundedAmount ? `${invoice.refundedAmount.toLocaleString()} ${invoice.currencyCode}` : '—'}</td>
                  <td>{invoice.dueDate ? new Date(invoice.dueDate).toLocaleDateString('ar-LY') : '—'}</td>
                  <td>
                    <span className={`badge ${
                      invoice.status === 'Paid' ? 'badge-success' :
                      invoice.status === 'Pending' ? 'badge-warning' :
                      invoice.status === 'Overdue' ? 'badge-danger' : 'badge-info'
                    }`}>
                      {statusNames[invoice.status] ?? invoice.status}
                    </span>
                  </td>
                  <td>
                    <button
                      className="btn-secondary"
                      style={{ height: '30px', padding: '0 10px', fontSize: '12px' }}
                      onClick={() => void openInvoiceDetails(invoice)}
                    >
                      <Eye size={13} /> عرض وسداد
                    </button>
                  </td>
                </tr>
              ))}
              {!loading && filteredInvoices.length === 0 && (
                <tr><td colSpan={8} style={{ textAlign: 'center', padding: '30px' }}>لا توجد فواتير مطابقة.</td></tr>
              )}
              {loading && (
                <tr><td colSpan={8} style={{ textAlign: 'center', padding: '30px' }}>جاري التحميل…</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: Invoice Details & Record Payment */}
      {selectedInvoice && (
        <div style={{ position: 'fixed', inset: 0, zIndex: 100, background: 'rgba(15,23,42,.45)', display: 'grid', placeItems: 'center', padding: '20px' }}>
          <div className="card" style={{ width: 'min(720px, 100%)', display: 'flex', flexDirection: 'column', gap: '20px', maxHeight: '90vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--border-main)', paddingBottom: '14px' }}>
              <div>
                <h2>فاتورة رقم {selectedInvoice.invoiceNumber}</h2>
                <small style={{ color: 'var(--text-secondary)' }}>العارض: {selectedInvoice.exhibitorName}</small>
              </div>
              <button type="button" className="btn-icon" onClick={() => setSelectedInvoice(null)}><X size={18} /></button>
            </div>

            {loadingDetails ? (
              <div style={{ textAlign: 'center', padding: '30px' }}>جاري جلب تفاصيل الفاتورة…</div>
            ) : (
              <>
                {/* Financial Summary Row */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '12px', background: 'var(--bg-main)', padding: '14px', borderRadius: 'var(--radius-md)' }}>
                  <div>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>إجمالي الفاتورة</span>
                    <strong>{(selectedInvoice.adjustedTotalAmount || selectedInvoice.totalAmount).toLocaleString()} {selectedInvoice.currencyCode}</strong>
                  </div>
                  <div>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>إجمالي المدفوع</span>
                    <strong style={{ color: 'var(--success)' }}>
                      {payments.reduce((s, p) => s + p.amount, 0).toLocaleString()} {selectedInvoice.currencyCode}
                    </strong>
                  </div>
                  <div>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>المتبقي للسداد</span>
                    <strong style={{ color: 'var(--primary)' }}>
                      {Math.max(0, (selectedInvoice.adjustedTotalAmount || selectedInvoice.totalAmount) - payments.reduce((s, p) => s + p.amount, 0)).toLocaleString()} {selectedInvoice.currencyCode}
                    </strong>
                  </div>
                </div>

                {/* Items Table */}
                {invoiceItems.length > 0 && (
                  <div>
                    <h3 style={{ fontSize: '14px', fontWeight: 700, marginBottom: '8px' }}>بنود الفاتورة</h3>
                    <table className="data-table" style={{ fontSize: '12px' }}>
                      <thead>
                        <tr>
                          <th>الوصف</th>
                          <th>سعر الوحدة</th>
                          <th>الكمية</th>
                          <th>الإجمالي</th>
                        </tr>
                      </thead>
                      <tbody>
                        {invoiceItems.map(item => (
                          <tr key={item.itemID}>
                            <td>{item.description}</td>
                            <td>{item.unitPrice.toLocaleString()}</td>
                            <td>{item.quantity}</td>
                            <td><strong>{item.lineTotal.toLocaleString()}</strong></td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {/* Payments History */}
                <div>
                  <h3 style={{ fontSize: '14px', fontWeight: 700, marginBottom: '8px' }}>سندات القبض المسجلة ({payments.length})</h3>
                  {payments.length === 0 ? (
                    <div style={{ fontSize: '13px', color: 'var(--text-secondary)', padding: '10px 0' }}>لم تسجل أي مدفوعات لهذه الفاتورة بعد.</div>
                  ) : (
                    <table className="data-table" style={{ fontSize: '12px' }}>
                      <thead>
                        <tr>
                          <th>التاريخ</th>
                          <th>المبلغ</th>
                          <th>طريقة الدفع</th>
                          <th>الرقم المرجعي</th>
                        </tr>
                      </thead>
                      <tbody>
                        {payments.map(p => (
                          <tr key={p.paymentID}>
                            <td>{new Date(p.paymentDate).toLocaleDateString('ar-LY')}</td>
                            <td><strong style={{ color: 'var(--success)' }}>{p.amount.toLocaleString()} {selectedInvoice.currencyCode}</strong></td>
                            <td>{paymentMethodNames[p.paymentMethod] ?? p.paymentMethod}</td>
                            <td dir="ltr">{p.paymentReference || '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>

                {/* Form to Record New Payment */}
                {canManageFinance && selectedInvoice.status !== 'Paid' && (
                  <form onSubmit={handleRecordPayment} style={{ borderTop: '1px solid var(--border-main)', paddingTop: '16px', display: 'grid', gap: '12px' }}>
                    <h3 style={{ fontSize: '14px', fontWeight: 800, color: 'var(--text-primary)' }}>تسجيل دفعة / سند قبض جديد</h3>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                      <div className="form-group" style={{ margin: 0 }}>
                        <label className="form-label">المبلغ المدفوع *</label>
                        <input
                          className="input-field"
                          type="number"
                          step="0.01"
                          required
                          value={paymentForm.amount}
                          onChange={e => setPaymentForm(prev => ({ ...prev, amount: e.target.value }))}
                        />
                      </div>
                      <div className="form-group" style={{ margin: 0 }}>
                        <label className="form-label">طريقة السداد</label>
                        <select
                          className="input-field"
                          value={paymentForm.paymentMethod}
                          onChange={e => setPaymentForm(prev => ({ ...prev, paymentMethod: e.target.value }))}
                        >
                          <option value="Cash">نقداً (كاش)</option>
                          <option value="BankTransfer">تحويل مصرفي</option>
                          <option value="Check">شيك مصدق</option>
                        </select>
                      </div>
                    </div>
                    <div className="form-group" style={{ margin: 0 }}>
                      <label className="form-label">رقم الإيصال / المرجع المصرفي</label>
                      <input
                        className="input-field"
                        placeholder="رقم الشيك أو الحوالة"
                        value={paymentForm.paymentReference}
                        onChange={e => setPaymentForm(prev => ({ ...prev, paymentReference: e.target.value }))}
                      />
                    </div>
                    <button className="btn-primary" type="submit" disabled={isSavingPayment}>
                      {isSavingPayment ? 'جاري تسجيل الدفعة…' : 'تسجيل سند القبض'}
                    </button>
                  </form>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
