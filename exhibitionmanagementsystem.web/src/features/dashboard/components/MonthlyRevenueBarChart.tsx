import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend
} from 'recharts';
import { MonthlyRevenuePoint } from '../types/dashboard.types';

interface MonthlyRevenueBarChartProps {
  data: MonthlyRevenuePoint[];
}

export const MonthlyRevenueBarChart: React.FC<MonthlyRevenueBarChartProps> = ({ data }) => {
  return (
    <div className="card" style={{ height: '400px', display: 'flex', flexDirection: 'column' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <div>
          <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
            مؤشرات الإيرادات والتحصيل المالي الشهري
          </h3>
          <p style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
            إجمالي الفواتير والمبالغ المسددة خلال آخر 6 أشهر (د.ل)
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '16px', fontSize: '12px', fontWeight: 600 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <span style={{ width: '10px', height: '10px', borderRadius: '2px', background: 'url(#primaryGrad) #6366f1' }} />
            <span>المحصل (Paid)</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
            <span style={{ width: '10px', height: '10px', borderRadius: '2px', background: '#cbd5e1' }} />
            <span>المتبقي (Pending)</span>
          </div>
        </div>
      </div>

      <div style={{ flex: 1, width: '100%', minHeight: '280px' }}>
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} margin={{ top: 10, right: 10, left: 10, bottom: 20 }}>
            <defs>
              <linearGradient id="primaryGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stopColor="#8b5cf6" stopOpacity={1} />
                <stop offset="100%" stopColor="#6366f1" stopOpacity={0.9} />
              </linearGradient>
            </defs>

            <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#e2e8f0" />
            <XAxis
              dataKey="month"
              tick={{ fill: '#64748b', fontSize: 12, fontFamily: 'Cairo, Work Sans' }}
              axisLine={{ stroke: '#e2e8f0' }}
              tickLine={false}
            />
            <YAxis
              tick={{ fill: '#64748b', fontSize: 12, fontFamily: 'Cairo, Work Sans' }}
              axisLine={{ stroke: '#e2e8f0' }}
              tickLine={false}
              tickFormatter={(val) => `${(val / 1000).toFixed(0)}k`}
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
              formatter={(value: unknown) => [`${Number(value).toLocaleString()} د.ل`, '']}
            />
            <Legend wrapperStyle={{ display: 'none' }} />
            <Bar
              dataKey="paid"
              name="المبلغ المحصل"
              fill="url(#primaryGrad)"
              radius={[8, 8, 0, 0]}
              barSize={24}
            />
            <Bar
              dataKey="pending"
              name="المبلغ المتبقي"
              fill="#e2e8f0"
              radius={[8, 8, 0, 0]}
              barSize={24}
            />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};
