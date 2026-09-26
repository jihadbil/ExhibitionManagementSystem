import { useEffect, useState } from 'react';
import { TrendingUp } from 'lucide-react';
import { api } from '../../../services/api';

interface RevenuePoint { month: string; revenue: number; paid: number; pending: number }

export const AnalyticsPage = () => {
  const [items, setItems] = useState<RevenuePoint[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<RevenuePoint[]>('/api/dashboard/revenue-chart?period=12m'); setItems(response.data); } catch { setError('تعذر تحميل التحليلات المالية.'); } finally { setLoading(false); } }; void load(); }, []);
  const totals = items.reduce((value, item) => ({ revenue: value.revenue + item.revenue, paid: value.paid + item.paid, pending: value.pending + item.pending }), { revenue: 0, paid: 0, pending: 0 });
  return <div style={{ display: 'grid', gap: 24 }}><div><h1><TrendingUp color="var(--primary)" /> التحليلات</h1><p style={{ color: 'var(--text-secondary)' }}>إيرادات آخر 12 شهراً من الخادم. أزيلت خريطة الحرارة الوهمية لعدم وجود مصدر بيانات لها.</p></div>{error && <div role="alert">{error}</div>}<div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 16 }}><div className="card"><small>إجمالي الفواتير</small><h2>{totals.revenue.toLocaleString()}</h2></div><div className="card"><small>المحصل</small><h2>{totals.paid.toLocaleString()}</h2></div><div className="card"><small>المتبقي</small><h2>{totals.pending.toLocaleString()}</h2></div></div><div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>الشهر</th><th>الإجمالي</th><th>المحصل</th><th>المتبقي</th></tr></thead><tbody>{items.map((item, index) => <tr key={`${item.month}-${index}`}><td>{item.month}</td><td>{item.revenue.toLocaleString()}</td><td>{item.paid.toLocaleString()}</td><td>{item.pending.toLocaleString()}</td></tr>)}{!loading && items.length === 0 && <tr><td colSpan={4}>لا توجد بيانات.</td></tr>}{loading && <tr><td colSpan={4}>جاري التحميل…</td></tr>}</tbody></table></div></div></div>;
};
