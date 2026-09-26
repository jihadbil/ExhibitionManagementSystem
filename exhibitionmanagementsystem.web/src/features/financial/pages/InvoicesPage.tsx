import { useEffect, useState } from 'react';
import { Receipt } from 'lucide-react';
import { api } from '../../../services/api';

interface Invoice { invoiceID: number; exhibitorName: string; invoiceNumber: string; invoiceDate: string; totalAmount: number; adjustedTotalAmount: number; refundedAmount: number; currencyCode: string; status: string; dueDate?: string }

export const InvoicesPage = () => {
  const [items, setItems] = useState<Invoice[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<{ items: Invoice[] }>('/api/financial/invoices?page=1&pageSize=100'); setItems(response.data.items ?? []); } catch { setError('تعذر تحميل الفواتير.'); } finally { setLoading(false); } }; void load(); }, []);
  return <div style={{ display: 'grid', gap: 24 }}><div><h1><Receipt color="var(--primary)" /> الفواتير والتحصيل</h1><p style={{ color: 'var(--text-secondary)' }}>السجل المالي الفعلي للمستأجر.</p></div>{error && <div role="alert">{error}</div>}<div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>رقم الفاتورة</th><th>العارض</th><th>التاريخ</th><th>الإجمالي</th><th>المسترد</th><th>الاستحقاق</th><th>الحالة</th></tr></thead><tbody>{items.map(item => <tr key={item.invoiceID}><td>{item.invoiceNumber}</td><td>{item.exhibitorName}</td><td>{new Date(item.invoiceDate).toLocaleDateString('ar-LY')}</td><td>{(item.adjustedTotalAmount || item.totalAmount).toLocaleString()} {item.currencyCode}</td><td>{item.refundedAmount.toLocaleString()}</td><td>{item.dueDate ? new Date(item.dueDate).toLocaleDateString('ar-LY') : '—'}</td><td>{item.status}</td></tr>)}{!loading && items.length === 0 && <tr><td colSpan={7}>لا توجد فواتير.</td></tr>}{loading && <tr><td colSpan={7}>جاري التحميل…</td></tr>}</tbody></table></div></div></div>;
};
