import React, { useState } from 'react';
import { Search, Bell, ShieldCheck, User } from 'lucide-react';
import { useAuth } from '../providers/AuthContext';
import { useLocation } from 'react-router-dom';

const pageTitles: Record<string, string> = {
  '/dashboard': 'لوحة التحكم والمؤشرات الرئيسية',
  '/analytics': 'التحليلات المتقدمة والخريطة الحرارية',
  '/exhibitions': 'إدارة وتنظيم المعارض',
  '/booths': 'سجل الأجنحة وقوائم الحجز',
  '/booths/designer': 'مصمم المخططات البصري 2D',
  '/venues': 'مراكز المعارض والقاعات',
  '/companies': 'دليل الشركات العارضة',
  '/events': 'جدول الفعاليات والمؤتمرات',
  '/tickets': 'إدارة التذاكر والزوار',
  '/badges/designer': 'مصمم شارات الدخول والبطاقات',
  '/kiosk': 'كشك الاستقبال والطباعة الفورية',
  '/reservations': 'طلبات الحجز وعقود الأجنحة',
  '/financial': 'الفواتير والتحصيل المالي',
  '/sponsorship': 'الرعاة والمساحات الإعلانية',
  '/services-mgmt': 'الخدمات الإضافية والتسعير',
  '/users': 'إدارة المستخدمين والصلاحيات',
  '/tenants': 'إدارة المستأجرين والاشتراكات',
  '/admin/audit': 'سجلات الرقابة والعمليات',
  '/settings': 'إعدادات النظام العامة',
};

export const TopBar: React.FC = () => {
  const { user } = useAuth();
  const location = useLocation();
  const [searchValue, setSearchValue] = useState('');

  const currentTitle = pageTitles[location.pathname] || 'ExpoManager';

  return (
    <header
      className="glass-surface"
      style={{
        height: 'var(--topbar-height)',
        position: 'fixed',
        top: 0,
        right: 'var(--sidebar-width)',
        left: 0,
        zIndex: 40,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        padding: '0 28px',
        borderBottom: '1px solid var(--border-main)',
        boxShadow: 'var(--shadow-xs)'
      }}
    >
      {/* Right Side: Page Title */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
        <h1 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)' }}>
          {currentTitle}
        </h1>
      </div>

      {/* Center: Global Search Bar (400px width) */}
      <div
        style={{
          position: 'relative',
          width: '400px',
          maxWidth: '100%'
        }}
      >
        <Search
          size={18}
          style={{
            position: 'absolute',
            right: '14px',
            top: '50%',
            transform: 'translateY(-50%)',
            color: 'var(--text-muted)'
          }}
        />
        <input
          type="text"
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="بحث سريع عن معرض، جناح، أو شركة عارضة..."
          className="input-field"
          style={{
            paddingRight: '42px',
            height: '40px',
            borderRadius: 'var(--radius-full)',
            background: 'var(--bg-secondary)',
            fontSize: '13px'
          }}
        />
      </div>

      {/* Left Side: Actions & Profile */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
        {/* Tenant Badge */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '6px',
            padding: '5px 12px',
            background: 'var(--primary-light)',
            color: 'var(--primary)',
            borderRadius: 'var(--radius-full)',
            fontSize: '12px',
            fontWeight: 600
          }}
        >
          <ShieldCheck size={14} />
          <span>{user?.tenantName || 'المستأجر الافتراضي'}</span>
        </div>

        {/* Notifications Icon Button */}
        <button
          className="btn-icon"
          title="الإشعارات"
          style={{ position: 'relative' }}
        >
          <Bell size={18} />
          <span
            style={{
              position: 'absolute',
              top: '8px',
              left: '8px',
              width: '8px',
              height: '8px',
              background: 'var(--danger)',
              borderRadius: '50%',
              border: '2px solid #ffffff'
            }}
          />
        </button>

        {/* User Profile Tile */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '10px',
            padding: '4px 10px',
            borderRadius: 'var(--radius-lg)',
            cursor: 'pointer'
          }}
        >
          <div
            style={{
              width: '36px',
              height: '36px',
              borderRadius: 'var(--radius-full)',
              background: 'var(--primary-gradient)',
              color: '#ffffff',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 700,
              fontSize: '14px',
              boxShadow: 'var(--shadow-sm)'
            }}
          >
            {user?.fullName ? user.fullName[0].toUpperCase() : <User size={18} />}
          </div>

          <div style={{ display: 'flex', flexDirection: 'column' }}>
            <span style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)', lineHeight: 1.2 }}>
              {user?.fullName || 'مدير النظام'}
            </span>
            <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
              {user?.roles?.[0] || 'مسؤول رئيسي'}
            </span>
          </div>
        </div>
      </div>
    </header>
  );
};
