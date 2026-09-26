import { useEffect, useMemo, useState } from 'react';
import { Search, Store } from 'lucide-react';
import { api } from '../../../services/api';

interface Booth { boothID: number; hallName: string; boothNumber: string; currentAreaSqM: number; status: string; isMerged: boolean }

export const BoothsPage = () => {
  const [items, setItems] = useState<Booth[]>([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<{ items: Booth[] }>('/api/booths?pageNumber=1&pageSize=100'); setItems(response.data.items ?? []); } catch { setError('تعذر تحميل الأجنحة.'); } finally { setLoading(false); } }; void load(); }, []);
  const filtered = useMemo(() => items.filter(item => `${item.boothNumber} ${item.hallName} ${item.status}`.toLowerCase().includes(query.toLowerCase())), [items, query]);
  return <div style={{ display: 'grid', gap: '24px' }}><div><h1><Store color="var(--primary)" /> سجل الأجنحة</h1><p style={{ color: 'var(--text-secondary)' }}>البيانات الحالية من الخادم.</p></div><div className="card" style={{ position: 'relative' }}><Search size={18} style={{ position: 'absolute', right: 28, top: '50%', transform: 'translateY(-50%)' }} /><input className="input-field" value={query} onChange={event => setQuery(event.target.value)} placeholder="بحث" style={{ paddingRight: 42 }} /></div>{error && <div role="alert">{error}</div>}<div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>رقم الجناح</th><th>القاعة</th><th>المساحة</th><th>الحالة</th><th>مدمج</th></tr></thead><tbody>{filtered.map(item => <tr key={item.boothID}><td>{item.boothNumber}</td><td>{item.hallName}</td><td>{item.currentAreaSqM} م²</td><td>{item.status}</td><td>{item.isMerged ? 'نعم' : 'لا'}</td></tr>)}{!loading && filtered.length === 0 && <tr><td colSpan={5}>لا توجد أجنحة.</td></tr>}{loading && <tr><td colSpan={5}>جاري التحميل…</td></tr>}</tbody></table></div></div></div>;
};
