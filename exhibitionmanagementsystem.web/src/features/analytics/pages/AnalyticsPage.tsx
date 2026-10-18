import { useEffect, useState } from 'react';
import { TrendingUp } from 'lucide-react';
import { api } from '../../../services/api';
import { useAuth } from '../../../app/providers/AuthContext';
import { calculateRevenueTotals, type RevenuePoint } from '../utils/revenueSummary';

export const AnalyticsPage = () => {
  const { user } = useAuth();
  const [items, setItems] = useState<RevenuePoint[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<RevenuePoint[]>('/api/dashboard/revenue-chart?period=12m'); setItems(response.data); } catch { setError('تعذر تحميل التحليلات المالية.'); } finally { setLoading(false); } }; void load(); }, []);
  const totals = calculateRevenueTotals(items);
  const money = (amount: number) => `${amount.toLocaleString('ar-LY', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${user?.baseCurrency ?? ''}`;
  return <div style={{ display: 'grid', gap: 24 }}>
    <div><h1><TrendingUp color="var(--primary)" /> التحليلات</h1><p>حركات آخر 12 شهراً بالعملة الأساسية. المتبقي هو رصيد نهاية آخر شهر، وليس مجموع الأرصدة الشهرية.</p></div>
    {error && <div role="alert">{error}</div>}
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3,1fr)', gap: 16 }}>
      <div className="card"><small>صافي الفواتير خلال الفترة</small><h2>{money(totals.revenue)}</h2></div>
      <div className="card"><small>صافي المحصل خلال الفترة</small><h2>{money(totals.paid)}</h2></div>
      <div className="card"><small>الرصيد المتبقي الحالي</small><h2>{money(totals.pending)}</h2></div>
    </div>
    <div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table">
      <thead><tr><th>الشهر</th><th>صافي الفواتير</th><th>المحصل</th><th>رصيد نهاية الشهر</th></tr></thead>
      <tbody>{items.map((item, index) => <tr key={`${item.month}-${index}`}><td>{item.month}</td><td>{money(item.revenue)}</td><td>{money(item.paid)}</td><td>{money(item.pending)}</td></tr>)}
        {!loading && !error && items.length === 0 && <tr><td colSpan={4}>لا توجد بيانات.</td></tr>}
        {loading && <tr><td colSpan={4}>جاري التحميل…</td></tr>}
      </tbody></table></div></div>
  </div>;
};
