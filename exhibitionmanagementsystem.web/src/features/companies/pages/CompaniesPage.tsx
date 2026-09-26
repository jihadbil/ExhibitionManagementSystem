import React, { useState } from 'react';
import { Building, Search, Plus, MapPin, Globe, Mail, Phone, Store, ExternalLink } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface Company {
  id: number;
  name: string;
  industry: string;
  email: string;
  phone: string;
  city: string;
  allocatedBooths: string[];
  exhibitions: string[];
  contractStatus: 'Active' | 'Pending';
}

const mockCompanies: Company[] = [
  { id: 1, name: 'الشركة الليبية للاتصالات والتقنية', industry: 'الاتصالات والحوسبة', email: 'info@ltt.ly', phone: '+218 21 3600000', city: 'طرابلس', allocatedBooths: ['VIP-01', 'A-101'], exhibitions: ['معرض طرابلس للتقنية 2026'], contractStatus: 'Active' },
  { id: 2, name: 'شركة مسار للحلول الرقمية وتطوير البرمجيات', industry: 'تطوير البرمجيات', email: 'contact@masar.ly', phone: '+218 91 2223344', city: 'بنغازي', allocatedBooths: ['A-102'], exhibitions: ['معرض طرابلس للتقنية 2026'], contractStatus: 'Active' },
  { id: 3, name: 'مؤسسة التقنية والشبكات الذكية', industry: 'الشبكات والأمن السيبراني', email: 'sales@tech-networks.ly', phone: '+218 92 5556677', city: 'طرابلس', allocatedBooths: ['B-201'], exhibitions: ['معرض طرابلس للتقنية 2026'], contractStatus: 'Pending' },
  { id: 4, name: 'شركة الدواء الحديث للمستلزمات والمعدات الطبية', industry: 'الرعاية الصحية', email: 'info@medmodern.ly', phone: '+218 51 667788', city: 'مصراتة', allocatedBooths: ['C-301'], exhibitions: ['المعرض الطبي الدولي 2026'], contractStatus: 'Active' },
];

export const CompaniesPage: React.FC = () => {
  const { showToast } = useToast();
  const [companies, setCompanies] = useState<Company[]>(mockCompanies);
  const [search, setSearch] = useState('');

  const filtered = companies.filter(c =>
    c.name.toLowerCase().includes(search.toLowerCase()) ||
    c.industry.toLowerCase().includes(search.toLowerCase()) ||
    c.city.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Building size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              دليل الشركات العارضة
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            إدارة بيانات العارضين، الأجنحة المحجوزة، والعقود والملفات المعتمدة
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج تسجيل شركة عارضة جديدة متاح', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إضافة شركة عارضة</span>
        </button>
      </div>

      <div className="card" style={{ padding: '16px 20px', display: 'flex', alignItems: 'center', gap: '16px' }}>
        <div style={{ position: 'relative', width: '380px', maxWidth: '100%' }}>
          <Search size={18} style={{ position: 'absolute', right: '14px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="بحث باسم الشركة، المجال، أو المدينة..."
            className="input-field"
            style={{ paddingRight: '42px', height: '42px' }}
          />
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))', gap: '24px' }}>
        {filtered.map(company => (
          <div key={company.id} className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'var(--primary-light)', color: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                  <Building size={24} />
                </div>
                <div>
                  <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>{company.name}</h3>
                  <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{company.industry} — {company.city}</span>
                </div>
              </div>

              <span className={`badge ${company.contractStatus === 'Active' ? 'badge-success' : 'badge-warning'}`}>
                {company.contractStatus === 'Active' ? 'عقد معتمد' : 'قيد المراجعة'}
              </span>
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', fontSize: '13px', color: 'var(--text-secondary)' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <Mail size={15} style={{ color: 'var(--primary)' }} />
                <span>{company.email}</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <Phone size={15} style={{ color: 'var(--primary)' }} />
                <span>{company.phone}</span>
              </div>
            </div>

            <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '12px', borderRadius: 'var(--radius-lg)' }}>
              <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginBottom: '4px', fontWeight: 600 }}>الأجنحة المخصصة:</div>
              <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
                {company.allocatedBooths.map(b => (
                  <span key={b} className="badge badge-featured">
                    <Store size={12} />
                    {b}
                  </span>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
