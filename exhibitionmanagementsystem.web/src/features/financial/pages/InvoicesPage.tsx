import React, { useState } from 'react';
import {
  Receipt,
  Search,
  Filter,
  Download,
  Printer,
  CheckCircle2,
  Clock,
  AlertCircle,
  Eye,
  X,
  CreditCard,
  FileText
} from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface InvoiceItem {
  id: string;
  invoiceNumber: string;
  companyName: string;
  exhibitionName: string;
  issueDate: string;
  dueDate: string;
  totalAmount: number;
  paidAmount: number;
  balance: number;
  status: 'Paid' | 'Pending' | 'Overdue' | 'Cancelled';
  items?: Array<{ name: string; qty: number; unitPrice: number; total: number }>;
}

const mockInvoices: InvoiceItem[] = [
  {
    id: '1',
    invoiceNumber: 'INV-2026-0811',
    companyName: 'الشركة الليبية للاتصالات',
    exhibitionName: 'معرض طرابلس الدولي للتقنية 2026',
    issueDate: '2026-09-01',
    dueDate: '2026-09-15',
    totalAmount: 14200,
    paidAmount: 14200,
    balance: 0,
    status: 'Paid',
    items: [
      { name: 'حجز جناح رئيسي VIP-01 (30م²)', qty: 1, unitPrice: 12000, total: 12000 },
      { name: 'خط إنترنت فائق السرعة مخصص للأجنحة', qty: 1, unitPrice: 1200, total: 1200 },
      { name: 'خدمات طاقة كهربائية إضافية (3 فاز)', qty: 1, unitPrice: 1000, total: 1000 }
    ]
  },
  {
    id: '2',
    invoiceNumber: 'INV-2026-0812',
    companyName: 'شركة مسار للحلول الرقمية',
    exhibitionName: 'معرض طرابلس الدولي للتقنية 2026',
    issueDate: '2026-09-05',
    dueDate: '2026-09-20',
    totalAmount: 6400,
    paidAmount: 3200,
    balance: 3200,
    status: 'Pending',
    items: [
      { name: 'حجز جناح مميز A-102 (16م²)', qty: 1, unitPrice: 5600, total: 5600 },
      { name: 'خدمات النظافة والضيافة للأجنحة', qty: 1, unitPrice: 800, total: 800 }
    ]
  },
  {
    id: '3',
    invoiceNumber: 'INV-2026-0798',
    companyName: 'مؤسسة التقنية والشبكات',
    exhibitionName: 'معرض طرابلس الدولي للتقنية 2026',
    issueDate: '2026-08-20',
    dueDate: '2026-09-05',
    totalAmount: 9800,
    paidAmount: 2000,
    balance: 7800,
    status: 'Overdue',
    items: [
      { name: 'حجز جناح زاوية B-201 (20م²)', qty: 1, unitPrice: 8000, total: 8000 },
      { name: 'إضاءة إضافية ومكبرات صوت', qty: 1, unitPrice: 1800, total: 1800 }
    ]
  },
  {
    id: '4',
    invoiceNumber: 'INV-2026-0824',
    companyName: 'شركة الدواء الحديث للمستلزمات',
    exhibitionName: 'المعرض الليبي الدولي للصحة (MedExpo)',
    issueDate: '2026-09-10',
    dueDate: '2026-09-25',
    totalAmount: 18500,
    paidAmount: 18500,
    balance: 0,
    status: 'Paid',
    items: [
      { name: 'حجز جناح مركزي C-301 (35م²)', qty: 1, unitPrice: 16000, total: 16000 },
      { name: 'تجهيزات أرضيات وسجاد مخصص', qty: 1, unitPrice: 2500, total: 2500 }
    ]
  },
];

export const InvoicesPage: React.FC = () => {
  const { showToast } = useToast();
  const [invoices, setInvoices] = useState<InvoiceItem[]>(mockInvoices);
  const [statusFilter, setStatusFilter] = useState<'All' | 'Paid' | 'Pending' | 'Overdue'>('All');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedInvoice, setSelectedInvoice] = useState<InvoiceItem | null>(null);

  const filtered = invoices.filter(inv => {
    const matchesFilter = statusFilter === 'All' || inv.status === statusFilter;
    const matchesSearch = inv.invoiceNumber.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          inv.companyName.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesFilter && matchesSearch;
  });

  const totalBilled = invoices.reduce((acc, curr) => acc + curr.totalAmount, 0);
  const totalPaid = invoices.reduce((acc, curr) => acc + curr.paidAmount, 0);
  const totalOverdue = invoices.filter(i => i.status === 'Overdue').reduce((acc, curr) => acc + curr.balance, 0);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Receipt size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              الإدارة المالية والفواتير
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            متابعة استحقاقات حجز الأجنحة والخدمات الإضافية والتحصيل المالي
          </p>
        </div>

        <button
          onClick={() => window.print()}
          className="btn-secondary"
          style={{ height: '42px', padding: '0 16px' }}
        >
          <Download size={16} />
          <span>تصدير كشف الفواتير</span>
        </button>
      </div>

      {/* 3 Financial Overview Cards */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))',
          gap: '20px'
        }}
      >
        <div className="card" style={{ padding: '24px' }}>
          <span style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 600 }}>إجمالي الفواتير الصادرة</span>
          <div style={{ fontSize: '28px', fontWeight: 800, color: 'var(--text-primary)', marginTop: '6px' }}>
            {totalBilled.toLocaleString()} د.ل
          </div>
          <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '8px' }}>
            تشمل أجنحة المعارض والخدمات والرعاية
          </div>
        </div>

        <div className="card" style={{ padding: '24px', borderRight: '4px solid var(--success)' }}>
          <span style={{ fontSize: '13px', color: 'var(--success-text)', fontWeight: 600 }}>المبالغ المحصلة فعلياً</span>
          <div style={{ fontSize: '28px', fontWeight: 800, color: 'var(--success-text)', marginTop: '6px' }}>
            {totalPaid.toLocaleString()} د.ل
          </div>
          <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '8px' }}>
            نسبة تحصيل {((totalPaid / totalBilled) * 100).toFixed(0)}% من إجمالي المطالبات
          </div>
        </div>

        <div className="card" style={{ padding: '24px', borderRight: '4px solid var(--danger)' }}>
          <span style={{ fontSize: '13px', color: 'var(--danger-text)', fontWeight: 600 }}>المتأخرات غير المسددة</span>
          <div style={{ fontSize: '28px', fontWeight: 800, color: 'var(--danger-text)', marginTop: '6px' }}>
            {totalOverdue.toLocaleString()} د.ل
          </div>
          <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '8px' }}>
            فواتير تجاوزت تاريخ الاستحقاق المحدد
          </div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="card" style={{ padding: '16px 20px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '16px', flexWrap: 'wrap' }}>
        <div style={{ position: 'relative', width: '380px', maxWidth: '100%' }}>
          <Search
            size={18}
            style={{
              position: 'absolute',
              right: '14px',
              top: '50%',
              transform: 'translateY(-50%)',
              color: 'var(--text-muted)'
            }}
          />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="بحث برقم الفاتورة أو اسم الشركة..."
            className="input-field"
            style={{ paddingRight: '42px', height: '42px' }}
          />
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          {(['All', 'Paid', 'Pending', 'Overdue'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setStatusFilter(tab)}
              style={{
                padding: '8px 16px',
                borderRadius: 'var(--radius-lg)',
                fontSize: '13px',
                fontWeight: 600,
                background: statusFilter === tab ? 'var(--primary-gradient)' : 'var(--bg-secondary)',
                color: statusFilter === tab ? '#ffffff' : 'var(--text-secondary)',
                boxShadow: statusFilter === tab ? 'var(--primary-glow)' : 'none',
                transition: 'all var(--transition-normal)'
              }}
            >
              {tab === 'All' ? 'جميع الفواتير' : tab === 'Paid' ? 'مسددة بالكامل' : tab === 'Pending' ? 'قيد السداد' : 'متأخرة'}
            </button>
          ))}
        </div>
      </div>

      {/* Invoices Data Table */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: '15%' }}>رقم الفاتورة</th>
                <th style={{ width: '25%' }}>الشركة العارضة</th>
                <th style={{ width: '22%' }}>المعرض التابع له</th>
                <th style={{ width: '12%' }}>تاريخ الاستحقاق</th>
                <th style={{ width: '12%' }}>المبلغ الإجمالي</th>
                <th style={{ width: '12%' }}>الحالة</th>
                <th style={{ width: '10%', textAlign: 'center' }}>معاينة</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map(inv => (
                <tr key={inv.id}>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <FileText size={16} style={{ color: 'var(--primary)' }} />
                      <span style={{ fontWeight: 800, fontSize: '13px', color: 'var(--text-primary)' }}>
                        {inv.invoiceNumber}
                      </span>
                    </div>
                  </td>
                  <td>
                    <span style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '13px' }}>
                      {inv.companyName}
                    </span>
                  </td>
                  <td>
                    <span style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>
                      {inv.exhibitionName}
                    </span>
                  </td>
                  <td>
                    <span style={{ fontSize: '12px', color: inv.status === 'Overdue' ? 'var(--danger-text)' : 'var(--text-secondary)' }}>
                      {inv.dueDate}
                    </span>
                  </td>
                  <td>
                    <div style={{ fontWeight: 800, fontSize: '14px', color: 'var(--text-primary)' }}>
                      {inv.totalAmount.toLocaleString()} د.ل
                    </div>
                    {inv.balance > 0 && (
                      <span style={{ fontSize: '11px', color: 'var(--danger-text)', display: 'block' }}>
                        متبقي: {inv.balance.toLocaleString()} د.ل
                      </span>
                    )}
                  </td>
                  <td>
                    <span
                      className={`badge ${
                        inv.status === 'Paid'
                          ? 'badge-success'
                          : inv.status === 'Pending'
                          ? 'badge-warning'
                          : 'badge-danger'
                      }`}
                    >
                      {inv.status === 'Paid' ? 'مدفوعة ✓' : inv.status === 'Pending' ? 'معلقة' : 'متأخرة ⚠️'}
                    </span>
                  </td>
                  <td style={{ textAlign: 'center' }}>
                    <button
                      onClick={() => setSelectedInvoice(inv)}
                      className="btn-icon"
                      title="عرض تفاصيل الفاتورة وبنودها"
                    >
                      <Eye size={16} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Invoice Details Drawer / Modal */}
      {selectedInvoice && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(15, 23, 42, 0.4)',
            backdropFilter: 'blur(4px)',
            zIndex: 100,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: '24px'
          }}
        >
          <div
            className="card"
            style={{
              width: '100%',
              maxWidth: '640px',
              padding: '32px',
              boxShadow: 'var(--shadow-xl)',
              animation: 'slideIn 0.25s ease-out'
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderBottom: '1px solid var(--border-main)', paddingBottom: '16px', marginBottom: '20px' }}>
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <h3 style={{ fontSize: '18px', fontWeight: 800, color: 'var(--text-primary)' }}>
                    تفاصيل الفاتورة {selectedInvoice.invoiceNumber}
                  </h3>
                  <span className={`badge ${selectedInvoice.status === 'Paid' ? 'badge-success' : 'badge-warning'}`}>
                    {selectedInvoice.status}
                  </span>
                </div>
                <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
                  {selectedInvoice.companyName} — {selectedInvoice.exhibitionName}
                </p>
              </div>

              <button
                onClick={() => setSelectedInvoice(null)}
                className="btn-icon"
              >
                <X size={18} />
              </button>
            </div>

            {/* Line Items Table */}
            <div style={{ marginBottom: '24px' }}>
              <div style={{ fontSize: '13px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: '8px' }}>
                بنود ومشتملات الفاتورة:
              </div>
              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>البند والبيان</th>
                      <th style={{ width: '12%', textAlign: 'center' }}>الكمية</th>
                      <th style={{ width: '22%' }}>سعر الوحدة</th>
                      <th style={{ width: '22%' }}>الإجمالي</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedInvoice.items?.map((item, idx) => (
                      <tr key={idx}>
                        <td style={{ fontWeight: 600 }}>{item.name}</td>
                        <td style={{ textAlign: 'center' }}>{item.qty}</td>
                        <td>{item.unitPrice.toLocaleString()} د.ل</td>
                        <td style={{ fontWeight: 700 }}>{item.total.toLocaleString()} د.ل</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Total Balance Summary Box */}
            <div
              style={{
                backgroundColor: 'var(--bg-secondary)',
                borderRadius: 'var(--radius-lg)',
                padding: '16px 20px',
                display: 'flex',
                flexDirection: 'column',
                gap: '8px',
                marginBottom: '24px'
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px' }}>
                <span style={{ color: 'var(--text-secondary)' }}>إجمالي المبلغ المستحق:</span>
                <span style={{ fontWeight: 800, color: 'var(--text-primary)' }}>{selectedInvoice.totalAmount.toLocaleString()} د.ل</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '13px' }}>
                <span style={{ color: 'var(--text-secondary)' }}>المدفوع:</span>
                <span style={{ fontWeight: 700, color: 'var(--success-text)' }}>{selectedInvoice.paidAmount.toLocaleString()} د.ل</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderTop: '1px solid var(--border-main)', paddingTop: '8px' }}>
                <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>المتبقي المستحق:</span>
                <span style={{ fontWeight: 800, color: selectedInvoice.balance > 0 ? 'var(--danger-text)' : 'var(--success-text)' }}>
                  {selectedInvoice.balance.toLocaleString()} د.ل
                </span>
              </div>
            </div>

            {/* Actions */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: '12px' }}>
              <button
                type="button"
                onClick={() => setSelectedInvoice(null)}
                className="btn-secondary"
              >
                إغلاق
              </button>

              <button
                type="button"
                onClick={() => {
                  window.print();
                  showToast('جاري طباعة إيصال الفاتورة...', 'info');
                }}
                className="btn-primary"
              >
                <Printer size={16} />
                <span>طباعة إيصال الفاتورة</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
