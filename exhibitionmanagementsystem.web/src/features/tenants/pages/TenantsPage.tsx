import React, { useState } from 'react';
import { Shield, Plus, Building2, Globe, CheckCircle2, Zap } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface Tenant {
  id: number;
  name: string;
  subdomain: string;
  plan: string;
  maxExhibitions: number;
  currentExhibitions: number;
  status: 'Active' | 'Suspended';
}

const mockTenants: Tenant[] = [
  { id: 1, name: 'الهيئة العامة للمعارض الليبية', subdomain: 'expo-gov', plan: 'Enterprise Unlimited', maxExhibitions: 50, currentExhibitions: 8, status: 'Active' },
  { id: 2, name: 'شركة تريبولي إكسبو للمؤتمرات', subdomain: 'tripoli-expo', plan: 'Professional Pro', maxExhibitions: 15, currentExhibitions: 4, status: 'Active' },
  { id: 3, name: 'مؤسسة بنغازي للمعارض الدولية', subdomain: 'benghazi-fairs', plan: 'Professional Pro', maxExhibitions: 15, currentExhibitions: 3, status: 'Active' },
];

export const TenantsPage: React.FC = () => {
  const { showToast } = useToast();
  const [tenants, setTenants] = useState<Tenant[]>(mockTenants);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Shield size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              إدارة المستأجرين والاشتراكات (Multi-Tenancy)
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            إدارة حسابات المنظمين والشركات المنظمة للمعارض وحصص الاستهلاك
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج إضافة مستأجر جديد', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إضافة مستأجر جديد</span>
        </button>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '20px' }}>
        {tenants.map(t => (
          <div key={t.id} className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
              <div style={{ width: '44px', height: '44px', borderRadius: '12px', background: 'var(--primary-light)', color: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Building2 size={24} />
              </div>
              <span className="badge badge-success">نشط ومعتمد</span>
            </div>

            <div>
              <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>{t.name}</h3>
              <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '12px', color: 'var(--text-muted)', marginTop: '2px' }}>
                <Globe size={13} />
                <span>{t.subdomain}.expomanager.ly</span>
              </div>
            </div>

            <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '14px', borderRadius: 'var(--radius-lg)' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', marginBottom: '6px' }}>
                <span style={{ color: 'var(--text-secondary)' }}>الخطة الحالية:</span>
                <span style={{ fontWeight: 700, color: 'var(--primary)' }}>{t.plan}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '12px', marginBottom: '8px' }}>
                <span style={{ color: 'var(--text-secondary)' }}>المعارض المستهلكة:</span>
                <span style={{ fontWeight: 800, color: 'var(--text-primary)' }}>{t.currentExhibitions} / {t.maxExhibitions}</span>
              </div>
              <div style={{ width: '100%', height: '6px', backgroundColor: '#e2e8f0', borderRadius: '4px', overflow: 'hidden' }}>
                <div style={{ width: `${(t.currentExhibitions / t.maxExhibitions) * 100}%`, height: '100%', background: 'var(--primary-gradient)' }} />
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
