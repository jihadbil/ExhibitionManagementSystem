import { useEffect, useState } from 'react';
import { Building2, Shield } from 'lucide-react';
import { getAllItems } from '../../../services/pagination';

interface Tenant {
  tenantID: number;
  companyName: string;
  subdomain: string;
  currentPlan?: string;
  isActive: boolean;
  trialEndsAt?: string;
  baseCurrency: string;
}

export const TenantsPage = () => {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const load = async () => {
      try {
        setTenants(await getAllItems<Tenant>('/api/tenants'));
      } catch {
        setError('تعذر تحميل المستأجرين من الخادم.');
      } finally {
        setIsLoading(false);
      }
    };
    void load();
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div><h1 style={{ display: 'flex', alignItems: 'center', gap: '10px' }}><Shield color="var(--primary)" />إدارة المستأجرين</h1><p style={{ color: 'var(--text-secondary)' }}>عرض مركزي لحسابات المنظمين وخططهم الفعلية.</p></div>
      {error && <div role="alert" style={{ color: 'var(--danger-text)' }}>{error}</div>}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '18px' }}>
        {tenants.map(tenant => <article key={tenant.tenantID} className="card" style={{ display: 'grid', gap: '12px' }}><div style={{ display: 'flex', justifyContent: 'space-between' }}><Building2 color="var(--primary)" /><span className={`badge ${tenant.isActive ? 'badge-success' : 'badge-danger'}`}>{tenant.isActive ? 'نشط' : 'موقوف'}</span></div><h2 style={{ fontSize: '17px' }}>{tenant.companyName}</h2><div dir="ltr">{tenant.subdomain || '—'}</div><div>{tenant.currentPlan || 'لا توجد خطة'}</div><div>{tenant.baseCurrency}</div>{tenant.trialEndsAt && <small>نهاية التجربة: {new Date(tenant.trialEndsAt).toLocaleDateString('ar-LY')}</small>}</article>)}
        {!isLoading && tenants.length === 0 && <div className="card">لا يوجد مستأجرون.</div>}
        {isLoading && <div className="card">جاري التحميل…</div>}
      </div>
    </div>
  );
};
