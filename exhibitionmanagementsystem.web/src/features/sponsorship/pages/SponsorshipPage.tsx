import React, { useState } from 'react';
import { Sparkles, Plus, Award, DollarSign, Target, CheckCircle2 } from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface SponsorItem {
  id: number;
  name: string;
  tier: 'Platinum' | 'Gold' | 'Silver' | 'Bronze';
  exhibitionName: string;
  contributionAmount: number;
  assignedSpaces: string[];
}

const mockSponsors: SponsorItem[] = [
  { id: 1, name: 'مصرف ليبيا المركزي', tier: 'Platinum', exhibitionName: 'معرض طرابلس للتقنية 2026', contributionAmount: 60000, assignedSpaces: ['البوابة الرئيسية', 'مسرح المؤتمرات'] },
  { id: 2, name: 'شركة المدار الجديد', tier: 'Platinum', exhibitionName: 'معرض طرابلس للتقنية 2026', contributionAmount: 50000, assignedSpaces: ['صالة كبار الشخصيات', 'الممر الشرقي'] },
  { id: 3, name: 'شركة ليبيانا للهاتف المحمول', tier: 'Gold', exhibitionName: 'معرض طرابلس للتقنية 2026', contributionAmount: 35000, assignedSpaces: ['لوحات الإعلانات الرقمية'] },
  { id: 4, name: 'مجموعة الأمل الطبية', tier: 'Gold', exhibitionName: 'المعرض الطبي الدولي', contributionAmount: 30000, assignedSpaces: ['مدخل القاعة الطبية'] }
];

export const SponsorshipPage: React.FC = () => {
  const { showToast } = useToast();
  const [sponsors, setSponsors] = useState<SponsorItem[]>(mockSponsors);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Sparkles size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              الرعاة والمساحات الإعلانية
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            إدارة باقات الرعاية (بلاتيني، ذهبي، فضي) والمساحات الإعلانية داخل المعارض
          </p>
        </div>

        <button
          onClick={() => showToast('نموذج إضافة راعي أو باقة رعاية جديدة', 'info')}
          className="btn-primary"
          style={{ height: '42px', padding: '0 20px' }}
        >
          <Plus size={16} />
          <span>إضافة راعي جديد</span>
        </button>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '20px' }}>
        {sponsors.map(sponsor => (
          <div key={sponsor.id} className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '14px' }}>
            <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
              <div style={{ width: '44px', height: '44px', borderRadius: '12px', background: 'var(--primary-light)', color: 'var(--primary)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                <Award size={22} />
              </div>
              <span className={`badge ${sponsor.tier === 'Platinum' ? 'badge-vip' : sponsor.tier === 'Gold' ? 'badge-corner' : 'badge-standard'}`}>
                الراعي {sponsor.tier}
              </span>
            </div>

            <div>
              <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>{sponsor.name}</h3>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '2px' }}>{sponsor.exhibitionName}</div>
            </div>

            <div style={{ backgroundColor: 'var(--bg-secondary)', padding: '12px', borderRadius: 'var(--radius-md)' }}>
              <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>مبلغ الرعاية والمساهمة:</div>
              <div style={{ fontSize: '18px', fontWeight: 800, color: 'var(--text-primary)', marginTop: '2px' }}>
                {sponsor.contributionAmount.toLocaleString()} د.ل
              </div>
            </div>

            <div>
              <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginBottom: '4px', fontWeight: 600 }}>المساحات الممنوحة:</div>
              <div style={{ display: 'flex', gap: '6px', flexWrap: 'wrap' }}>
                {sponsor.assignedSpaces.map(s => (
                  <span key={s} className="badge badge-featured">{s}</span>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
