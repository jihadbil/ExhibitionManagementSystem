import React, { useState } from 'react';
import { Search, ShieldCheck, User, Menu } from 'lucide-react';
import { useAuth } from '../providers/AuthContext';
import { useLocation, useNavigate } from 'react-router-dom';

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
  '/services-mgmt': 'الخدمات المساندة',
  '/users': 'إدارة المستخدمين والصلاحيات',
  '/tenants': 'إدارة المستأجرين والاشتراكات',
  '/admin/audit': 'سجلات الرقابة والعمليات',
  '/settings': 'إعدادات النظام العامة',
};

interface TopBarProps {
  onMenuClick?: () => void;
}

export const TopBar: React.FC<TopBarProps> = ({ onMenuClick }) => {
  const { user } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [searchValue, setSearchValue] = useState('');

  const currentTitle = pageTitles[location.pathname]
    || (location.pathname.startsWith('/venues/') && location.pathname.endsWith('/designer')
      ? 'المصمم البصري لمخطط الموقع'
      : location.pathname.startsWith('/exhibitions/') ? 'إدارة أجنحة المعرض' : 'ExpoManager');

  const handleSearch = (event: React.FormEvent) => {
    event.preventDefault();
    const query = searchValue.trim();
    if (query) navigate(`/exhibitions?search=${encodeURIComponent(query)}`);
  };

  return (
    <header
      className="glass-surface app-topbar"
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
        <button className="btn-icon mobile-menu-button" aria-label="فتح القائمة" onClick={onMenuClick}>
          <Menu size={20} />
        </button>
        <h1 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)' }}>
          {currentTitle}
        </h1>
      </div>

      {/* Center: Global Search Bar (400px width) */}
      <form
        className="topbar-search"
        onSubmit={handleSearch}
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
      </form>

      {/* Left Side: Actions & Profile */}
      <div className="topbar-actions" style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
        {/* Tenant Badge */}
        <div
          className="topbar-tenant"
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
