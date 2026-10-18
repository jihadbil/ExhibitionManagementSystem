import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import {
  Eye,
  Printer,
  Receipt,
  Search,
  X
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { useToast } from '../../../app/providers/ToastContext';
import { api } from '../../../services/api';
import { summarizeInvoiceTotals } from '../utils/invoiceSummary';

interface Invoice {
  invoiceID: number;
  exhibitorID?: number;
  exhibitorName: string;
  reservationID?: number;
  invoiceNumber: string;
  invoiceDate: string;
  subTotal: number;
  taxRate: number;
  totalAmount: number;
  discountAmount?: number;
  taxAmount?: number;
  adjustedTotalAmount: number;
  refundedAmount: number;
  paidAmount: number;
  remainingAmount: number;
  baseCurrencyCode: string;
  adjustedTotalAmountInBaseCurrency: number;
  paidAmountInBaseCurrency: number;
  remainingAmountInBaseCurrency: number;
  isOverdue: boolean;
  creditNotes: { creditNoteID: number; type: string; amount: number; reason: string; createdAt: string }[];
  currencyCode: string;
  currencySymbol?: string;
  status: string; // 'Pending' | 'Paid' | 'Overdue' | 'Cancelled'
  dueDate?: string;
  notes?: string;
}

interface InvoiceItem {
  invoiceItemID: number;
  invoiceID: number;
  itemName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

interface Payment {
  paymentID: number;
  invoiceID: number;
  amount: number;
  method: string;
  referenceNo?: string;
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
  Draft: 'مسودة',
  Issued: 'صادرة ومستحقة',
  PartiallyPaid: 'مدفوعة جزئياً',
  Paid: 'مدفوعة بالكامل',
  Overdue: 'متأخرة السداد',
  Cancelled: 'ملغاة',
};

const paymentMethodNames: Record<string, string> = {
  Cash: 'نقداً (كاش)',
  BankTransfer: 'تحويل مصرفي',
  Cheque: 'شيك مصدق',
  Online: 'دفع إلكتروني',
};

const asAmount = (value: unknown): number => {
  const amount = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(amount) ? amount : 0;
};

const formatAmount = (value: unknown): string => asAmount(value).toLocaleString('ar-LY');

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
  const detailsRequest = useRef(0);
  const closeInvoiceDetails = () => {
    detailsRequest.current++;
    setSelectedInvoice(null);
  };

  // Record Payment Form State
  const [paymentForm, setPaymentForm] = useState<PaymentFormData>(emptyPaymentForm);
  const [isSavingPayment, setIsSavingPayment] = useState(false);

  // Load Invoices
  const loadInvoices = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const all: Invoice[] = [];
      for (let page = 1; ; page++) {
        const response = await api.get<{ items: Invoice[]; totalCount: number }>(`/api/financial/invoices?page=${page}&pageSize=200`);
        const items = response.data.items ?? [];
        all.push(...items);
        if (items.length < 200 || all.length >= response.data.totalCount) break;
      }
      setInvoices(all);
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
  const stats = useMemo(() => summarizeInvoiceTotals(invoices), [invoices]);

  const displayTotal = (field: 'totalBilled' | 'totalCollected' | 'remainingBalance') => stats.totals.length === 0
    ? formatAmount(0)
    : stats.totals.map(group => <span key={group.currency} style={{ display: 'block' }}>
      {formatAmount(group[field])} <small style={{ fontSize: '13px' }}>{group.currency}</small>
    </span>);

  // Filtered Invoices
  const filteredInvoices = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return invoices.filter(inv => {
      const matchesStatus = statusFilter === 'All' || (statusFilter === 'Overdue' ? inv.isOverdue : inv.status === statusFilter);
      const matchesSearch = !q ||
        inv.invoiceNumber.toLowerCase().includes(q) ||
        inv.exhibitorName.toLowerCase().includes(q);
      return matchesStatus && matchesSearch;
    });
  }, [invoices, statusFilter, searchQuery]);

  // Open Invoice Details Modal & Load Payments/Items
  const openInvoiceDetails = async (invoice: Invoice) => {
    const request = ++detailsRequest.current;
    setSelectedInvoice(invoice);
    setLoadingDetails(true);
    setInvoiceItems([]);
    setPayments([]);

    try {
      const response = await api.get<Invoice & { items: InvoiceItem[]; payments: Payment[] }>(`/api/financial/invoices/${invoice.invoiceID}`);
      if (request !== detailsRequest.current) return;
      setSelectedInvoice(response.data);
      setInvoiceItems(response.data.items ?? []);
      setPayments(response.data.payments ?? []);
      const remaining = asAmount(response.data.remainingAmount);
      setPaymentForm({
        amount: remaining > 0 ? remaining.toString() : '',
        paymentMethod: 'Cash',
        paymentReference: '',
        notes: '',
      });
    } catch {
      if (request === detailsRequest.current) {
        setSelectedInvoice(null);
        showToast('تعذر تحميل تفاصيل الفاتورة كاملة.', 'warning');
      }
    } finally {
      if (request === detailsRequest.current) setLoadingDetails(false);
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
    const activeRequest = detailsRequest.current;
    try {
      await api.post('/api/financial/payments', {
        invoiceID: selectedInvoice.invoiceID,
        amount: amountNum,
        currencyCode: selectedInvoice.currencyCode,
        method: paymentForm.paymentMethod,
        referenceNo: paymentForm.paymentReference.trim(),
        notes: paymentForm.notes.trim(),
      });

      showToast('تم تسجيل سند القبض والدفعة بنجاح.', 'success');
      await loadInvoices();
      // Reload current modal payments
      if (activeRequest === detailsRequest.current) await openInvoiceDetails(selectedInvoice);
    } catch {
      showToast('فشل تسجيل الدفعة. تحقق من صلاحيات المالية والمبلغ.', 'danger');
    } finally {
      setIsSavingPayment(false);
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <style>{`
        @media print {
          @page { size: A4; margin: 14mm; }
          body { background: #fff !important; }
          body * { visibility: hidden !important; }
          .invoice-print-area, .invoice-print-area * { visibility: visible !important; }
          .invoice-print-area {
            position: absolute !important;
            inset: 0 !important;
            width: 100% !important;
            max-height: none !important;
            overflow: visible !important;
            box-shadow: none !important;
            border: 0 !important;
            color: #111827 !important;
            background: #fff !important;
          }
          .invoice-print-hide { display: none !important; }
          .invoice-print-area table { width: 100% !important; border-collapse: collapse !important; }
          .invoice-print-area th, .invoice-print-area td { border: 1px solid #d1d5db !important; color: #111827 !important; }
        }
      `}</style>
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
            {displayTotal('totalBilled')}
          </h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--success)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المحصل فعلياً</span>
          <h2 style={{ fontSize: '22px', fontWeight: 800, marginTop: '4px', color: 'var(--success)' }}>
            {displayTotal('totalCollected')}
          </h2>
        </div>
        <div className="card" style={{ padding: '16px 20px', borderLeft: '4px solid var(--primary)' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>المتبقي قيد التحصيل</span>
          <h2 style={{ fontSize: '22px', fontWeight: 800, marginTop: '4px', color: 'var(--primary)' }}>
            {displayTotal('remainingBalance')}
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
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
          {['All', 'Draft', 'Issued', 'PartiallyPaid', 'Paid', 'Overdue', 'Cancelled'].map(st => (
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
                  <td><strong>{formatAmount(invoice.adjustedTotalAmount)} {invoice.currencyCode}</strong></td>
                  <td>{asAmount(invoice.refundedAmount) > 0 ? `${formatAmount(invoice.refundedAmount)} ${invoice.currencyCode}` : '—'}</td>
                  <td>{invoice.dueDate ? new Date(invoice.dueDate).toLocaleDateString('ar-LY') : '—'}</td>
                  <td>
                    <span className={`badge ${
                      invoice.status === 'Paid' ? 'badge-success' :
                      invoice.isOverdue ? 'badge-danger' :
                      invoice.status === 'Issued' ? 'badge-warning' : 'badge-info'
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
          <div className="card invoice-print-area" dir="rtl" style={{ width: 'min(720px, 100%)', display: 'flex', flexDirection: 'column', gap: '20px', maxHeight: '90vh', overflowY: 'auto' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--border-main)', paddingBottom: '14px' }}>
              <div>
                <h2>فاتورة رقم {selectedInvoice.invoiceNumber}</h2>
                <small style={{ color: 'var(--text-secondary)' }}>العارض: {selectedInvoice.exhibitorName}</small>
              </div>
              <div className="invoice-print-hide" style={{ display: 'flex', gap: '8px' }}>
                <button type="button" className="btn-secondary" onClick={() => window.print()} disabled={loadingDetails}>
                  <Printer size={16} /> طباعة الفاتورة
                </button>
                <button type="button" className="btn-icon" onClick={closeInvoiceDetails}><X size={18} /></button>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '8px', fontSize: '12px' }}>
              <div><strong>تاريخ الفاتورة:</strong> {new Date(selectedInvoice.invoiceDate).toLocaleDateString('ar-LY')}</div>
              <div><strong>تاريخ الاستحقاق:</strong> {selectedInvoice.dueDate ? new Date(selectedInvoice.dueDate).toLocaleDateString('ar-LY') : '—'}</div>
            </div>

            {loadingDetails ? (
              <div style={{ textAlign: 'center', padding: '30px' }}>جاري جلب تفاصيل الفاتورة…</div>
            ) : (
              <>
                {/* Financial Summary Row */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '12px', background: 'var(--bg-main)', padding: '14px', borderRadius: 'var(--radius-md)' }}>
                  <div>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>إجمالي الفاتورة</span>
                    <strong>{formatAmount(selectedInvoice.adjustedTotalAmount)} {selectedInvoice.currencyCode}</strong>
                  </div>
                  <div>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>إجمالي المدفوع</span>
                    <strong style={{ color: 'var(--success)' }}>
                      {formatAmount(selectedInvoice.paidAmount)} {selectedInvoice.currencyCode}
                    </strong>
                  </div>
                  <div>
                    <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block' }}>المتبقي للسداد</span>
                    <strong style={{ color: 'var(--primary)' }}>
                      {formatAmount(selectedInvoice.remainingAmount)} {selectedInvoice.currencyCode}
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
                          <tr key={item.invoiceItemID}>
                            <td>{item.itemName || '—'}</td>
                            <td>{formatAmount(item.unitPrice)}</td>
                            <td>{formatAmount(item.quantity)}</td>
                            <td><strong>{formatAmount(item.totalPrice)}</strong></td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {(selectedInvoice.creditNotes?.length ?? 0) > 0 && (
                  <div>
                    <h3 style={{ fontSize: '14px', marginBottom: '8px' }}>الإشعارات الدائنة والاستردادات</h3>
                    <table className="data-table" style={{ fontSize: '12px' }}>
                      <thead><tr><th>التاريخ</th><th>النوع</th><th>المبلغ</th><th>السبب</th></tr></thead>
                      <tbody>{selectedInvoice.creditNotes.map(note => <tr key={note.creditNoteID}>
                        <td>{new Date(note.createdAt).toLocaleDateString('ar-LY')}</td>
                        <td>{note.type === 'Refund' ? 'استرداد' : 'إشعار دائن'}</td>
                        <td>{formatAmount(note.amount)} {selectedInvoice.currencyCode}</td>
                        <td>{note.reason}</td>
                      </tr>)}</tbody>
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
                            <td><strong style={{ color: 'var(--success)' }}>{formatAmount(p.amount)} {selectedInvoice.currencyCode}</strong></td>
                            <td>{paymentMethodNames[p.method] ?? p.method}</td>
                            <td dir="ltr">{p.referenceNo || '—'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  )}
                </div>

                {/* Form to Record New Payment */}
                {canManageFinance && ['Issued', 'PartiallyPaid'].includes(selectedInvoice.status) && selectedInvoice.remainingAmount > 0 && (
                  <form className="invoice-print-hide" onSubmit={handleRecordPayment} style={{ borderTop: '1px solid var(--border-main)', paddingTop: '16px', display: 'grid', gap: '12px' }}>
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
                          <option value="Cheque">شيك مصدق</option>
                          <option value="Online">دفع إلكتروني</option>
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
