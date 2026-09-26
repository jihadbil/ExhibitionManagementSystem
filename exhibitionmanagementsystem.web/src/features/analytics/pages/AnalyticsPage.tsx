import React, { useState } from 'react';
import {
  TrendingUp,
  Users,
  Building,
  Store,
  DollarSign,
  AlertCircle,
  ArrowUpRight,
  Sparkles,
  Flame,
  Download
} from 'lucide-react';
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer
} from 'recharts';

const trafficData = [
  { time: '09:00 ص', visitors: 420 },
  { time: '10:00 ص', visitors: 980 },
  { time: '11:00 ص', visitors: 1650 },
  { time: '12:00 م', visitors: 2200 },
  { time: '01:00 م', visitors: 1850 },
  { time: '02:00 م', visitors: 1400 },
  { time: '03:00 م', visitors: 2450 },
  { time: '04:00 م', visitors: 2890 },
  { time: '05:00 م', visitors: 2600 },
  { time: '06:00 م', visitors: 1900 },
  { time: '07:00 م', visitors: 1100 },
];

const heatmapBooths = [
  { id: 1, number: 'VIP-01', company: 'الشركة الليبية للاتصالات', category: 'VIP', visits: 3840, density: 96, rank: 1 },
  { id: 2, number: 'A-101', company: 'مؤسسة التقنية والشبكات', category: 'VIP', visits: 3420, density: 89, rank: 2 },
  { id: 3, number: 'A-102', company: 'شركة مسار للحلول الرقمية', category: 'Featured', visits: 2950, density: 78, rank: 3 },
  { id: 4, number: 'B-201', company: 'مؤسسة التقنية العالمية', category: 'Corner', visits: 2410, density: 67, rank: 4 },
  { id: 5, number: 'B-204', company: 'شركة الدواء الحديث', category: 'Standard', visits: 1890, density: 52, rank: 5 },
  { id: 6, number: 'C-301', company: 'مختبرات الأمل للتشخيص', category: 'Standard', visits: 1420, density: 41, rank: 6 },
];

export const AnalyticsPage: React.FC = () => {
  const [selectedExhibition, setSelectedExhibition] = useState('معرض طرابلس الدولي للتقنية 2026');

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <TrendingUp size={24} style={{ color: 'var(--primary)' }} />
            <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
              التحليلات المتقدمة والخريطة الحرارية
            </h1>
          </div>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)', marginTop: '2px' }}>
            رؤى تفاعلية لقياس كثافة حركة الزوار وتفاعل الحضور مع الأجنحة (SCREEN_13)
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <select
            value={selectedExhibition}
            onChange={(e) => setSelectedExhibition(e.target.value)}
            className="input-field"
            style={{ width: 'auto', height: '42px', fontWeight: 600 }}
          >
            <option value="معرض طرابلس الدولي للتقنية 2026">معرض طرابلس الدولي للتقنية 2026</option>
            <option value="المعرض الليبي الدولي للصحة (MedExpo)">المعرض الليبي الدولي للصحة (MedExpo)</option>
            <option value="الملتقى الصناعي والتصنيع المتقدم">الملتقى الصناعي والتصنيع المتقدم</option>
          </select>

          <button
            onClick={() => window.print()}
            className="btn-secondary"
            style={{ height: '42px', padding: '0 16px' }}
          >
            <Download size={16} />
            <span>تصدير تقرير PDF</span>
          </button>
        </div>
      </div>

      {/* 4 Glass-Styled Stat Cards with Progress Lines Underneath (SCREEN_13 Specification) */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
          gap: '20px'
        }}
      >
        {/* Card 1 */}
        <div
          className="card"
          style={{
            padding: '24px',
            backgroundColor: '#ffffff',
            position: 'relative'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 600 }}>إجمالي الزوار اليوم</span>
              <div style={{ fontSize: '28px', fontWeight: 800, color: 'var(--text-primary)', marginTop: '4px' }}>19,440</div>
            </div>
            <div style={{ width: '44px', height: '44px', borderRadius: '12px', background: '#dbeafe', color: '#2563eb', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Users size={22} />
            </div>
          </div>
          <div style={{ marginTop: '16px' }}>
            <div style={{ width: '100%', height: '6px', background: '#f1f5f9', borderRadius: '4px', overflow: 'hidden' }}>
              <div style={{ width: '85%', height: '100%', background: 'linear-gradient(to right, #3b82f6, #06b6d4)', borderRadius: '4px' }} />
            </div>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block', marginTop: '6px' }}>85% من الطاقة الاستيعابية اليومية</span>
          </div>
        </div>

        {/* Card 2 */}
        <div
          className="card"
          style={{
            padding: '24px',
            backgroundColor: '#ffffff',
            position: 'relative'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 600 }}>نسبة إشغال الأجنحة</span>
              <div style={{ fontSize: '28px', fontWeight: 800, color: '#4f46e5', marginTop: '4px' }}>92.4%</div>
            </div>
            <div style={{ width: '44px', height: '44px', borderRadius: '12px', background: '#e0e7ff', color: '#4f46e5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Store size={22} />
            </div>
          </div>
          <div style={{ marginTop: '16px' }}>
            <div style={{ width: '100%', height: '6px', background: '#f1f5f9', borderRadius: '4px', overflow: 'hidden' }}>
              <div style={{ width: '92.4%', height: '100%', background: 'var(--primary-gradient)', borderRadius: '4px' }} />
            </div>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block', marginTop: '6px' }}>56 من أصل 60 جناح محجوز</span>
          </div>
        </div>

        {/* Card 3 */}
        <div
          className="card"
          style={{
            padding: '24px',
            backgroundColor: '#ffffff',
            position: 'relative'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 600 }}>إجمالي الإيرادات المحققة</span>
              <div style={{ fontSize: '28px', fontWeight: 800, color: 'var(--success-text)', marginTop: '4px' }}>342,800 د.ل</div>
            </div>
            <div style={{ width: '44px', height: '44px', borderRadius: '12px', background: 'var(--success-bg)', color: 'var(--success-text)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <DollarSign size={22} />
            </div>
          </div>
          <div style={{ marginTop: '16px' }}>
            <div style={{ width: '100%', height: '6px', background: '#f1f5f9', borderRadius: '4px', overflow: 'hidden' }}>
              <div style={{ width: '78%', height: '100%', background: 'linear-gradient(to right, #10b981, #14b8a6)', borderRadius: '4px' }} />
            </div>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block', marginTop: '6px' }}>عقود أجنحة + رعاية وخدمات</span>
          </div>
        </div>

        {/* Card 4 */}
        <div
          className="card"
          style={{
            padding: '24px',
            backgroundColor: '#ffffff',
            position: 'relative'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
            <div>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)', fontWeight: 600 }}>متوسط وقت البقاء</span>
              <div style={{ fontSize: '28px', fontWeight: 800, color: '#d97706', marginTop: '4px' }}>2.4 ساعة</div>
            </div>
            <div style={{ width: '44px', height: '44px', borderRadius: '12px', background: 'var(--warning-bg)', color: 'var(--warning-text)', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Sparkles size={22} />
            </div>
          </div>
          <div style={{ marginTop: '16px' }}>
            <div style={{ width: '100%', height: '6px', background: '#f1f5f9', borderRadius: '4px', overflow: 'hidden' }}>
              <div style={{ width: '65%', height: '100%', background: 'linear-gradient(to right, #f59e0b, #ef4444)', borderRadius: '4px' }} />
            </div>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)', display: 'block', marginTop: '6px' }}>زيادة بنسبة 18% عن اليوم السابق</span>
          </div>
        </div>
      </div>

      {/* Main Section: Area Traffic Chart (2/3) + Actionable Insights Panel (1/3) */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '2fr 1fr',
          gap: '24px'
        }}
      >
        {/* Main Area Chart: Visitor Traffic */}
        <div className="card" style={{ height: '420px', display: 'flex', flexDirection: 'column' }}>
          <div style={{ marginBottom: '16px' }}>
            <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
              حركة وتدفق الزوار على مدار ساعات المعرض (Visitor Traffic)
            </h3>
            <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
              تتبع فترات الذروة وازدحام بوابات الدخول والقاعات الرئيسية
            </p>
          </div>

          <div style={{ flex: 1, width: '100%', minHeight: '300px' }}>
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={trafficData} margin={{ top: 10, right: 10, left: 10, bottom: 20 }}>
                <defs>
                  <linearGradient id="visitorAreaGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#6366f1" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0.02} />
                  </linearGradient>
                </defs>

                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
                <XAxis
                  dataKey="time"
                  tick={{ fill: '#64748b', fontSize: 12, fontFamily: 'Cairo, Work Sans' }}
                  axisLine={{ stroke: '#e2e8f0' }}
                  tickLine={false}
                />
                <YAxis
                  tick={{ fill: '#64748b', fontSize: 12, fontFamily: 'Cairo, Work Sans' }}
                  axisLine={{ stroke: '#e2e8f0' }}
                  tickLine={false}
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: '#ffffff',
                    border: '1px solid #e2e8f0',
                    borderRadius: '12px',
                    boxShadow: '0 10px 25px -5px rgba(0, 0, 0, 0.1)',
                    direction: 'rtl',
                    textAlign: 'right',
                    fontFamily: 'Cairo, Work Sans',
                    fontSize: '13px'
                  }}
                  formatter={(val: unknown) => [`${Number(val).toLocaleString()} زائر`, 'عدد الحضور']}
                />
                <Area
                  type="monotone"
                  dataKey="visitors"
                  stroke="#6366f1"
                  strokeWidth={3}
                  fillOpacity={1}
                  fill="url(#visitorAreaGrad)"
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Side Panel: Actionable Insights */}
        <div className="card" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div>
            <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>
              رؤى وتوصيات تشغيلية (Insights)
            </h3>
            <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
              تنبيهات تلقائية مستخلصة من نشاط الحضور
            </p>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            {/* Alert Insight */}
            <div
              style={{
                padding: '14px',
                borderRadius: 'var(--radius-lg)',
                backgroundColor: 'var(--danger-bg)',
                borderRight: '4px solid var(--danger)',
                display: 'flex',
                alignItems: 'flex-start',
                gap: '10px'
              }}
            >
              <AlertCircle size={20} color="var(--danger-text)" style={{ flexShrink: 0, marginTop: '2px' }} />
              <div>
                <div style={{ fontSize: '13px', fontWeight: 700, color: 'var(--danger-text)' }}>
                  ازدحام متوقع عند البوابة A
                </div>
                <div style={{ fontSize: '11px', color: '#7f1d1d', marginTop: '2px', lineHeight: 1.4 }}>
                  ساعة الذروة بين 04:00 و 05:00 م. يوصى بفتح مسارات كشك الاستقبال الإضافية.
                </div>
              </div>
            </div>

            {/* Increase Insight */}
            <div
              style={{
                padding: '14px',
                borderRadius: 'var(--radius-lg)',
                backgroundColor: 'var(--success-bg)',
                borderRight: '4px solid var(--success)',
                display: 'flex',
                alignItems: 'flex-start',
                gap: '10px'
              }}
            >
              <ArrowUpRight size={20} color="var(--success-text)" style={{ flexShrink: 0, marginTop: '2px' }} />
              <div>
                <div style={{ fontSize: '13px', fontWeight: 700, color: 'var(--success-text)' }}>
                  ارتفاع كثافة أجنحة VIP
                </div>
                <div style={{ fontSize: '11px', color: '#064e3b', marginTop: '2px', lineHeight: 1.4 }}>
                  سجل جناح VIP-01 و A-101 أعلى معدل استفسارات وتفاعل بين العارضين والزوار.
                </div>
              </div>
            </div>

            {/* Opportunity Insight */}
            <div
              style={{
                padding: '14px',
                borderRadius: 'var(--radius-lg)',
                backgroundColor: 'var(--info-bg)',
                borderRight: '4px solid var(--info)',
                display: 'flex',
                alignItems: 'flex-start',
                gap: '10px'
              }}
            >
              <Sparkles size={20} color="var(--info-text)" style={{ flexShrink: 0, marginTop: '2px' }} />
              <div>
                <div style={{ fontSize: '13px', fontWeight: 700, color: 'var(--info-text)' }}>
                  فرصة تسويق مساحات إضافية
                </div>
                <div style={{ fontSize: '11px', color: '#1e3a8a', marginTop: '2px', lineHeight: 1.4 }}>
                  الممر الشرقي يسجل حركة مرور عالية مناسبة للوحات الإعلانات الرقمية والرعاة.
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Bottom Table: Booth Engagement Heatmap (from SCREEN_13) */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div
          style={{
            padding: '20px 24px',
            borderBottom: '1px solid var(--border-main)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <Flame size={20} style={{ color: '#f97316' }} />
            <h3 style={{ fontSize: '16px', fontWeight: 800, color: 'var(--text-primary)' }}>
              خريطة حرارة تفاعل وإقبال الزوار على الأجنحة (Engagement Heatmap)
            </h3>
          </div>
          <span style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
            محدث لحظياً استناداً إلى مسح التذاكر والاستفسارات
          </span>
        </div>

        <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: '8%', textAlign: 'center' }}>الترتيب</th>
                <th style={{ width: '15%' }}>رقم الجناح</th>
                <th style={{ width: '25%' }}>الشركة العارضة</th>
                <th style={{ width: '12%' }}>فئة الجناح</th>
                <th style={{ width: '15%' }}>عدد الزيارات المسجلة</th>
                <th style={{ width: '25%' }}>مؤشر الكثافة والتفاعل (Heat Intensity)</th>
              </tr>
            </thead>
            <tbody>
              {heatmapBooths.map((b) => (
                <tr key={b.id}>
                  <td style={{ textAlign: 'center', fontWeight: 800, color: b.rank <= 3 ? 'var(--primary)' : 'var(--text-muted)' }}>
                    #{b.rank}
                  </td>
                  <td>
                    <span style={{ fontWeight: 800, color: 'var(--text-primary)', fontSize: '14px' }}>
                      {b.number}
                    </span>
                  </td>
                  <td>
                    <span style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '13px' }}>
                      {b.company}
                    </span>
                  </td>
                  <td>
                    <span className={`badge ${b.category === 'VIP' ? 'badge-vip' : b.category === 'Featured' ? 'badge-featured' : b.category === 'Corner' ? 'badge-corner' : 'badge-standard'}`}>
                      {b.category}
                    </span>
                  </td>
                  <td>
                    <span style={{ fontWeight: 700, color: 'var(--text-primary)', fontSize: '14px' }}>
                      {b.visits.toLocaleString()} زائر
                    </span>
                  </td>
                  <td>
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '11px', marginBottom: '4px', fontWeight: 700 }}>
                        <span style={{ color: b.density > 80 ? '#f97316' : 'var(--primary)' }}>
                          {b.density > 80 ? 'إقبال مرتفع جداً 🔥' : 'إقبال نشط'}
                        </span>
                        <span>{b.density}%</span>
                      </div>
                      <div style={{ width: '100%', height: '8px', backgroundColor: '#f1f5f9', borderRadius: '4px', overflow: 'hidden' }}>
                        <div
                          style={{
                            width: `${b.density}%`,
                            height: '100%',
                            background: b.density > 80 ? 'var(--industrial-gradient)' : 'var(--primary-gradient)',
                            borderRadius: '4px'
                          }}
                        />
                      </div>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
