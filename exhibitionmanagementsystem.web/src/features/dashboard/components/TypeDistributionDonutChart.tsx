import React from 'react';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip } from 'recharts';
import { ExhibitionTypeItem } from '../types/dashboard.types';

interface TypeDistributionDonutChartProps {
  data: ExhibitionTypeItem[];
}

export const TypeDistributionDonutChart: React.FC<TypeDistributionDonutChartProps> = ({ data }) => {
  const total = data.reduce((acc, curr) => acc + curr.count, 0);

  return (
    <div className="card" style={{ height: '400px', display: 'flex', flexDirection: 'column' }}>
      <div style={{ marginBottom: '12px' }}>
        <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
          توزيع المعارض حسب التخصص
        </h3>
        <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
          نسبة المعارض النشطة والمجدولة لكل قطاع
        </p>
      </div>

      <div style={{ position: 'relative', flex: 1, minHeight: '190px' }}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
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
              formatter={(value: any, name: any) => [`${value} معارض (${((Number(value) / total) * 100).toFixed(0)}%)`, String(name)]}
            />
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              innerRadius={55}
              outerRadius={80}
              paddingAngle={4}
              dataKey="count"
              nameKey="nameArabic"
            >
              {data.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={entry.color} stroke="#ffffff" strokeWidth={2} />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>

        {/* Center Total Count Overlay */}
        <div
          style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            textAlign: 'center',
            pointerEvents: 'none'
          }}
        >
          <div style={{ fontSize: '22px', fontWeight: 800, color: 'var(--text-primary)', lineHeight: 1 }}>
            {total}
          </div>
          <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontWeight: 600 }}>إجمالي</span>
        </div>
      </div>

      {/* Categorized Legend List */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(2, 1fr)',
          gap: '8px',
          paddingTop: '12px',
          borderTop: '1px solid var(--border-main)',
          fontSize: '12px'
        }}
      >
        {data.map((item) => (
          <div key={item.type} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
              <span
                style={{
                  width: '8px',
                  height: '8px',
                  borderRadius: '50%',
                  backgroundColor: item.color
                }}
              />
              <span style={{ color: 'var(--text-secondary)' }}>{item.nameArabic}</span>
            </div>
            <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{item.count}</span>
          </div>
        ))}
      </div>
    </div>
  );
};
