import { useEffect, useState } from 'react';
import { MapPin } from 'lucide-react';
import { api } from '../../../services/api';

interface Venue { venueID: number; name: string; address: string; city: string; country: string; totalCapacity?: number; isActive: boolean; hallsCount: number }

export const VenuesPage = () => {
  const [items, setItems] = useState<Venue[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<Venue[]>('/api/venues'); setItems(response.data); } catch { setError('تعذر تحميل المواقع والقاعات.'); } finally { setLoading(false); } }; void load(); }, []);
  return <div style={{ display: 'grid', gap: 24 }}><div><h1><MapPin color="var(--primary)" /> المواقع والقاعات</h1><p style={{ color: 'var(--text-secondary)' }}>المواقع المسجلة لدى المستأجر.</p></div>{error && <div role="alert">{error}</div>}<div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(300px,1fr))', gap: 18 }}>{items.map(item => <article className="card" key={item.venueID}><h2 style={{ fontSize: 17 }}>{item.name}</h2><p>{item.address}</p><p>{item.city}، {item.country}</p><p>القاعات: {item.hallsCount} · السعة: {item.totalCapacity?.toLocaleString() ?? '—'}</p><span className={`badge ${item.isActive ? 'badge-success' : 'badge-danger'}`}>{item.isActive ? 'نشط' : 'موقوف'}</span></article>)}{!loading && items.length === 0 && <div className="card">لا توجد مواقع.</div>}{loading && <div className="card">جاري التحميل…</div>}</div></div>;
};
