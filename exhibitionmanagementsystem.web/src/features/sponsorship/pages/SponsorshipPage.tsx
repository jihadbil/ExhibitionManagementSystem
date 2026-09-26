import { useEffect, useState } from 'react';
import { Sparkles } from 'lucide-react';
import { api } from '../../../services/api';

interface Sponsor { sponsorID: number; companyName: string; contactPerson?: string; phone?: string; email?: string; websiteUrl?: string; isActive: boolean; totalContractsCount: number }

export const SponsorshipPage = () => {
  const [items, setItems] = useState<Sponsor[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<Sponsor[]>('/api/sponsorship/sponsors'); setItems(response.data); } catch { setError('تعذر تحميل الرعاة.'); } finally { setLoading(false); } }; void load(); }, []);
  return <div style={{ display: 'grid', gap: 24 }}><div><h1><Sparkles color="var(--primary)" /> الرعاة</h1><p style={{ color: 'var(--text-secondary)' }}>الشركات الراعية وعقودها الفعلية.</p></div>{error && <div role="alert">{error}</div>}<div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>الشركة</th><th>جهة الاتصال</th><th>الهاتف</th><th>البريد</th><th>العقود</th><th>الحالة</th></tr></thead><tbody>{items.map(item => <tr key={item.sponsorID}><td>{item.companyName}</td><td>{item.contactPerson || '—'}</td><td>{item.phone || '—'}</td><td>{item.email || '—'}</td><td>{item.totalContractsCount}</td><td>{item.isActive ? 'نشط' : 'موقوف'}</td></tr>)}{!loading && items.length === 0 && <tr><td colSpan={6}>لا يوجد رعاة.</td></tr>}{loading && <tr><td colSpan={6}>جاري التحميل…</td></tr>}</tbody></table></div></div></div>;
};
