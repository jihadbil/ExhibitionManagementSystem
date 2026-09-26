import { useEffect, useState } from 'react';
import { Layers } from 'lucide-react';
import { api } from '../../../services/api';

interface Service { serviceID: number; serviceName: string; category: string; unit: string; defaultPrice?: number; isMandatory: boolean; description: string; isActive: boolean }

export const ServicesPage = () => {
  const [items, setItems] = useState<Service[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { const load = async () => { try { const response = await api.get<Service[]>('/api/services'); setItems(response.data); } catch { setError('تعذر تحميل الخدمات.'); } finally { setLoading(false); } }; void load(); }, []);
  return <div style={{ display: 'grid', gap: 24 }}><div><h1><Layers color="var(--primary)" /> الخدمات المساندة</h1><p style={{ color: 'var(--text-secondary)' }}>كتالوج الخدمات والأسعار الافتراضية من الخادم.</p></div>{error && <div role="alert">{error}</div>}<div className="card" style={{ padding: 0 }}><div className="table-container"><table className="data-table"><thead><tr><th>الخدمة</th><th>الفئة</th><th>الوحدة</th><th>السعر</th><th>إلزامية</th><th>الحالة</th></tr></thead><tbody>{items.map(item => <tr key={item.serviceID}><td>{item.serviceName}</td><td>{item.category}</td><td>{item.unit}</td><td>{item.defaultPrice?.toLocaleString() ?? '—'}</td><td>{item.isMandatory ? 'نعم' : 'لا'}</td><td>{item.isActive ? 'نشطة' : 'متوقفة'}</td></tr>)}{!loading && items.length === 0 && <tr><td colSpan={6}>لا توجد خدمات.</td></tr>}{loading && <tr><td colSpan={6}>جاري التحميل…</td></tr>}</tbody></table></div></div></div>;
};
