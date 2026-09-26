import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  TrendingUp,
  Building2,
  Store,
  Compass,
  MapPin,
  Building,
  Calendar,
  Ticket,
  IdCard,
  QrCode,
  FileCheck2,
  Receipt,
  Sparkles,
  Layers,
  Users,
  Shield,
  FileText,
  Settings,
  ChevronDown,
  LogOut,
  PlusCircle
} from 'lucide-react';
import { useAuth } from '../providers/AuthContext';

interface NavItem {
  to: string;
  label: string;
  icon: React.ElementType;
}

interface NavCategory {
  title: string;
  items: NavItem[];
}

export const Sidebar: React.FC = () => {
  const { logout, user } = useAuth();
  const [collapsedCategories, setCollapsedCategories] = useState<Record<string, boolean>>({});

  const toggleCategory = (title: string) => {
    setCollapsedCategories(prev => ({
      ...prev,
      [title]: !prev[title]
    }));
  };

  const navCategories: NavCategory[] = [
    {
      title: 'الرئيسية والمؤشرات',
      items: [
        { to: '/dashboard', label: 'لوحة التحكم', icon: LayoutDashboard },
        { to: '/analytics', label: 'التحليلات المتقدمة', icon: TrendingUp },
      ]
    },
    {
      title: 'المعارض والأجنحة',
      items: [
        { to: '/exhibitions', label: 'إدارة المعارض', icon: Building2 },
        { to: '/booths', label: 'سجل الأجنحة', icon: Store },
        { to: '/booths/designer', label: 'مصمم المخططات 2D', icon: Compass },
        { to: '/venues', label: 'المواقع والقاعات', icon: MapPin },
      ]
    },
    {
      title: 'العمليات والزوار',
      items: [
        { to: '/companies', label: 'الشركات العارضة', icon: Building },
        { to: '/events', label: 'جدول الفعاليات', icon: Calendar },
        { to: '/tickets', label: 'التذاكر والزوار', icon: Ticket },
        { to: '/badges/designer', label: 'مصمم الشارات', icon: IdCard },
        { to: '/kiosk', label: 'كشك الاستقبال الذاتي', icon: QrCode },
      ]
    },
    {
      title: 'المالية والتسويق',
      items: [
        { to: '/reservations', label: 'الحجوزات والعقود', icon: FileCheck2 },
        { to: '/financial', label: 'الفواتير والمالية', icon: Receipt },
        { to: '/sponsorship', label: 'الرعاة والمساحات', icon: Sparkles },
        { to: '/services-mgmt', label: 'الخدمات المساندة', icon: Layers },
      ]
    },
    {
      title: 'الإدارة والرقابة',
      items: [
        { to: '/users', label: 'المستخدمون والصلاحيات', icon: Users },
        { to: '/tenants', label: 'إدارة المستأجرين', icon: Shield },
        { to: '/admin/audit', label: 'سجلات الرقابة', icon: FileText },
        { to: '/settings', label: 'إعدادات النظام', icon: Settings },
      ]
    }
  ];

  return (
    <aside
      style={{
        width: 'var(--sidebar-width)',
        height: '100vh',
        backgroundColor: '#ffffff',
        borderLeft: '1px solid var(--border-main)',
        display: 'flex',
        flexDirection: 'column',
        position: 'fixed',
        top: 0,
        right: 0,
        zIndex: 50,
        userSelect: 'none',
        overflow: 'hidden'
      }}
    >
      {/* Brand Section */}
      <div
        style={{
          padding: '24px 20px 16px',
          display: 'flex',
          alignItems: 'center',
          gap: '12px',
          borderBottom: '1px solid var(--border-main)'
        }}
      >
        <div
          style={{
            width: '40px',
            height: '40px',
            borderRadius: 'var(--radius-lg)',
            background: 'var(--primary-gradient)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#ffffff',
            boxShadow: 'var(--primary-glow)',
            flexShrink: 0
          }}
        >
          <Building2 size={22} />
        </div>
        <div>
          <h2
            style={{
              fontSize: '18px',
              fontWeight: 800,
              background: 'var(--primary-gradient)',
              WebkitBackgroundClip: 'text',
              WebkitTextFillColor: 'transparent',
              letterSpacing: '-0.5px'
            }}
          >
            ExpoManager
          </h2>
          <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontWeight: 500 }}>
            {user?.tenantName || 'إدارة المعارض الدولية'}
          </span>
        </div>
      </div>

      {/* Navigation Groups List */}
      <div
        style={{
          flex: 1,
          overflowY: 'auto',
          padding: '16px 12px',
          display: 'flex',
          flexDirection: 'column',
          gap: '16px'
        }}
      >
        {navCategories.map(cat => {
          const isCollapsed = collapsedCategories[cat.title];

          return (
            <div key={cat.title}>
              {/* Category Header */}
              <button
                onClick={() => toggleCategory(cat.title)}
                style={{
                  width: '100%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  padding: '6px 12px',
                  color: 'var(--text-muted)',
                  fontSize: '11px',
                  fontWeight: 700,
                  textTransform: 'uppercase',
                  letterSpacing: '0.5px',
                  borderRadius: 'var(--radius-md)'
                }}
              >
                <span>{cat.title}</span>
                <ChevronDown
                  size={14}
                  style={{
                    transform: isCollapsed ? 'rotate(90deg)' : 'none',
                    transition: 'transform 0.2s ease'
                  }}
                />
              </button>

              {/* Items */}
              {!isCollapsed && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', marginTop: '4px' }}>
                  {cat.items.map(item => {
                    const Icon = item.icon;
                    return (
                      <NavLink
                        key={item.to}
                        to={item.to}
                        style={({ isActive }) => ({
                          display: 'flex',
                          alignItems: 'center',
                          gap: '12px',
                          padding: '10px 14px',
                          borderRadius: 'var(--radius-lg)',
                          fontSize: '13px',
                          fontWeight: isActive ? 600 : 500,
                          color: isActive ? '#ffffff' : 'var(--text-secondary)',
                          background: isActive ? 'var(--primary-gradient)' : 'transparent',
                          boxShadow: isActive ? 'var(--primary-glow)' : 'none',
                          transition: 'all var(--transition-normal)'
                        })}
                      >
                        <Icon size={18} />
                        <span>{item.label}</span>
                      </NavLink>
                    );
                  })}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* Footer Section */}
      <div
        style={{
          padding: '16px',
          borderTop: '1px solid var(--border-main)',
          backgroundColor: '#fafbfc'
        }}
      >
        <NavLink
          to="/exhibitions?action=create"
          className="btn-primary"
          style={{
            width: '100%',
            marginBottom: '10px',
            fontSize: '13px',
            padding: '8px 12px'
          }}
        >
          <PlusCircle size={16} />
          <span>معرض جديد</span>
        </NavLink>

        <button
          onClick={logout}
          style={{
            width: '100%',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '8px',
            padding: '8px 12px',
            borderRadius: 'var(--radius-md)',
            color: 'var(--danger)',
            fontSize: '13px',
            fontWeight: 600,
            background: 'transparent'
          }}
        >
          <LogOut size={16} />
          <span>تسجيل الخروج</span>
        </button>
      </div>
    </aside>
  );
};
