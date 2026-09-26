import React, { useState } from 'react';
import { Layers, Plus, Zap, Wifi, Sparkles, Tv, ShieldCheck } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface ServiceItem {
  id: number;
  name: string;
  category: string;
  unit: string;
  price: number;
  description: string;
  isActive: boolean;
}

const mockServices: ServiceItem[] = [
  { id: 1, name: 'تغذية كهربائية 3 فاز (3-Phase Power 32A)', category: 'Electricity', unit: 'لكل جناح', price: 1000, description: 'كابل ومفتاح حماية مخصص للمعدات والآلات الثقيلة', isActive: true },
  { id: 2, name: 'إنترنت فائق السرعة مخصص للأجنحة (Dedicated 50 Mbps)', category: 'Internet', unit: 'لكل جناح / يوم', price: 600, description: 'خط ألياف بصرية مباشر مع راوتر Wi-Fi 6 للأجنحة', isActive: true },
  { id: 3, name: 'خدمات النظافة والتعقيم اليومي للأجنحة', category: 'Cleaning', unit: 'لكل جناح / كامل المعرض', price: 400, description: 'تنظيف وتطهير الجناح صباحاً ومساءً طيلة أيام المعرض', isActive: true },
  { id: 4, name: 'شاشة عرض تلفزيونية ذكية 65 بوصة 4K مع حامل', category: 'AV & Screens', unit: 'لكل شاشة / أسبوع', price: 800, description: 'شاشة ذكية لعرض الفيديوهات الترويجية مع حامل أرضي أنيق', isActive: true },
];

export const ServicesPage: React.FC = () => {
  const { showToast } = useToast();
  const [services, setServices] = useState<ServiceItem[]>(mockServices);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Layers size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              الخدمات المساندة وقواعد التسعير
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            كتالوج الخدمات الإضافية المتاحة للأجنحة والشركات العارضة (كهرباء، إنترنت، ضيافة)
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج إضافة خدمة مساندة جديدة', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إضافة خدمة جديدة</span>
        </button>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(320px, 1fr))', gap: '20px' }}>
        {services.map(svc => (
          <div key={svc.id} className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
              <div style={{ width: '44px', height: '44px', borderRadius: '12px', background: 'var(--primary-light)', color: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Zap size={22} />
              </div>
              <span className="badge badge-success">نشطة ومتاحة</span>
            </div>

            <div>
              <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>{svc.name}</h3>
              <p style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '4px', lineHeight: 1.5 }}>{svc.description}</p>
            </div>

            <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '12px', borderRadius: 'var(--radius-md)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 'auto' }}>
              <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{svc.unit}</span>
              <span style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>{svc.price.toLocaleString()} د.ل</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
