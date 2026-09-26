import { useEffect, useMemo, useState } from 'react';
import { Building, Search } from 'lucide-react';
import { api } from '../../../services/api';

interface Exhibitor { exhibitorID: number; companyName: string; sector: string; nationality: string; exhibitorCategory: string; isActive: boolean }

export const CompaniesPage = () => {
  const [items, setItems] = useState<Exhibitor[]>([]); const [query, setQuery] = useState(''); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<{ items: Exhibitor[] }>('/api/exhibitors?page=1&pageSize=100'); setItems(response.data.items ?? []); } catch { setError('تعذر تحميل الشركات العارضة.'); } finally { setLoading(false); } }; void load(); }, []);
  const filtered = useMemo(() => items.filter(item => `${item.companyName} ${item.sector} ${item.nationality}`.toLowerCase().includes(query.toLowerCase())), [items, query]);
  return <div style={{ display: 'grid', gap: 24 }}><div><h1><Building color="var(--primary)" /> الشركات العارضة</h1><p style={{ color: 'var(--text-secondary)' }}>دليل العارضين المسجلين فعلياً.</p></div><div className="card" style={{ position: 'relative' }}><Search size={18} style={{ position: 'absolute', right: 28, top: '50%', transform: 'translateY(-50%)' }} /><input className="input-field" value={query} onChange={event => setQuery(event.target.value)} style={{ paddingRight: 42 }} placeholder="بحث" /></div>{error && <div role="alert">{error}</div>}<div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>الشركة</th><th>القطاع</th><th>الجنسية</th><th>الفئة</th><th>الحالة</th></tr></thead><tbody>{filtered.map(item => <tr key={item.exhibitorID}><td>{item.companyName}</td><td>{item.sector || '—'}</td><td>{item.nationality || '—'}</td><td>{item.exhibitorCategory || '—'}</td><td>{item.isActive ? 'نشط' : 'موقوف'}</td></tr>)}{!loading && filtered.length === 0 && <tr><td colSpan={5}>لا توجد شركات.</td></tr>}{loading && <tr><td colSpan={5}>جاري التحميل…</td></tr>}</tbody></table></div></div></div>;
};
