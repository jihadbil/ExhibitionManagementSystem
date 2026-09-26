import React from 'react';
import { ArrowUpRight, ArrowDownRight } from 'lucide-react';

interface StatCardProps {
  title: string;
  value: string | number;
  icon: React.ElementType;
  iconColor: string;
  iconBg: string;
  trend?: {
    value: string;
    isPositive: boolean;
  };
  subtitle?: string;
}

export const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  icon: Icon,
  iconColor,
  iconBg,
  trend,
  subtitle
}) => {
  return (
    <div
      className="card"
      style={{
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        padding: '24px',
        position: 'relative',
        overflow: 'hidden'
      }}
    >
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', marginBottom: '16px' }}>
        <div>
          <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-secondary)' }}>
            {title}
          </span>
          <div style={{ fontSize: '28px', fontWeight: 800, color: 'var(--text-primary)', marginTop: '6px', lineHeight: 1.1 }}>
            {value}
          </div>
        </div>

        {/* 48px Icon with 12px rounded colored background */}
        <div
          style={{
            width: '48px',
            height: '48px',
            borderRadius: 'var(--radius-lg)',
            background: iconBg,
            color: iconColor,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            flexShrink: 0
          }}
        >
          <Icon size={24} />
        </div>
      </div>

      {/* Footer / Trend Info */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '12px' }}>
        {trend && (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '2px',
              fontWeight: 700,
              padding: '2px 8px',
              borderRadius: 'var(--radius-full)',
              background: trend.isPositive ? 'var(--success-bg)' : 'var(--danger-bg)',
              color: trend.isPositive ? 'var(--success-text)' : 'var(--danger-text)'
            }}
          >
            {trend.isPositive ? <ArrowUpRight size={14} /> : <ArrowDownRight size={14} />}
            {trend.value}
          </span>
        )}

        <span style={{ color: 'var(--text-muted)' }}>
          {subtitle || 'مقارنة بالشهر السابق'}
        </span>
      </div>
    </div>
  );
};
