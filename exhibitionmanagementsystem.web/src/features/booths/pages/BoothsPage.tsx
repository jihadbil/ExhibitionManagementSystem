import React, { useState } from 'react';
import { NavLink } from 'react-router-dom';
import {
  Store,
  Compass,
  Search,
  Plus,
  CheckCircle2,
  Clock,
  BookmarkCheck,
  Building2,
  DollarSign
} from 'lucide-react';
import { useToast } from '../../../app/providers/ToastContext';

interface BoothItem {
  id: number;
  boothNumber: string;
  exhibitionName: string;
  companyName?: string;
  hallName: string;
  dimensions: string;
  category: 'VIP' | 'Featured' | 'Corner' | 'Standard';
  price: number;
  status: 'Available' | 'Reserved' | 'PendingReview';
}

const initialBooths: BoothItem[] = [
  { id: 1, boothNumber: 'A-101', exhibitionName: 'معرض طرابلس للتقنية 2026', companyName: 'الشركة الليبية للاتصالات', hallName: 'القاعة الرئيسية', dimensions: '6 × 4 م (24 م²)', category: 'VIP', price: 4800, status: 'Reserved' },
  { id: 2, boothNumber: 'A-102', exhibitionName: 'معرض طرابلس للتقنية 2026', companyName: 'شركة مسار للحلول الرقمية', hallName: 'القاعة الرئيسية', dimensions: '4 × 3 م (12 م²)', category: 'Featured', price: 2800, status: 'Reserved' },
  { id: 3, boothNumber: 'A-103', exhibitionName: 'معرض طرابلس للتقنية 2026', hallName: 'القاعة الرئيسية', dimensions: '3 × 3 م (9 م²)', category: 'Standard', price: 1800, status: 'Available' },
  { id: 4, boothNumber: 'B-201', exhibitionName: 'معرض طرابلس للتقنية 2026', companyName: 'مؤسسة التقنية العالمية', hallName: 'صالة الابتكار', dimensions: '5 × 4 م (20 م²)', category: 'Corner', price: 3600, status: 'PendingReview' },
  { id: 5, boothNumber: 'B-202', exhibitionName: 'معرض طرابلس للتقنية 2026', hallName: 'صالة الابتكار', dimensions: '3 × 3 م (9 م²)', category: 'Standard', price: 1800, status: 'Available' },
  { id: 6, boothNumber: 'C-301', exhibitionName: 'المعرض الطبي الدولي 2026', companyName: 'شركة الدواء الحديث للمستلزمات', hallName: 'الصالة الطبية A', dimensions: '6 × 5 م (30 م²)', category: 'VIP', price: 6000, status: 'Reserved' },
  { id: 7, boothNumber: 'C-302', exhibitionName: 'المعرض الطبي الدولي 2026', hallName: 'الصالة الطبية A', dimensions: '4 × 4 م (16 م²)', category: 'Corner', price: 3200, status: 'Available' },
  { id: 8, boothNumber: 'C-303', exhibitionName: 'المعرض الطبي الدولي 2026', companyName: 'مختبرات الأمل للتشخيص', hallName: 'الصالة الطبية A', dimensions: '3 × 3 م (9 م²)', category: 'Standard', price: 1800, status: 'PendingReview' }
];

export const BoothsPage: React.FC = () => {
  const { showToast } = useToast();
  const [booths, setBooths] = useState<BoothItem[]>(initialBooths);
  const [activeTab, setActiveTab] = useState<'All' | 'Available' | 'Reserved' | 'PendingReview'>('All');
  const [searchQuery, setSearchQuery] = useState('');

  const filteredBooths = booths.filter(b => {
    const matchesTab = activeTab === 'All' || b.status === activeTab;
    const matchesSearch = b.boothNumber.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          b.exhibitionName.toLowerCase().includes(searchQuery.toLowerCase()) ||
                          (b.companyName && b.companyName.toLowerCase().includes(searchQuery.toLowerCase()));
    return matchesTab && matchesSearch;
  });

  const availableCount = booths.filter(b => b.status === 'Available').length;
  const reservedCount = booths.filter(b => b.status === 'Reserved').length;
  const pendingCount = booths.filter(b => b.status === 'PendingReview').length;

  const getCategoryBadgeClass = (category: string) => {
    switch (category) {
      case 'VIP': return 'badge-vip';
      case 'Featured': return 'badge-featured';
      case 'Corner': return 'badge-corner';
      default: return 'badge-standard';
    }
  };

  const getCategoryArabic = (category: string) => {
    switch (category) {
      case 'VIP': return 'VIP خاص';
      case 'Featured': return 'مميز';
      case 'Corner': return 'زاوية';
      default: return 'قياسي';
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      {/* Header with Designer CTA */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)' }}>
            سجل وإدارة الأجنحة
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
            استعراض جداول الأجنحة وحالات الحجز والأسعار بحسب القاعات والمعارض
          </p>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <NavLink
            to="/booths/designer"
            className="btn-primary"
            style={{ height: '44px', padding: '0 20px' }}
          >
            <Compass size={18} />
            <span>فتح مصمم المخططات 2D</span>
          </NavLink>
        </div>
      </div>

      {/* 4 Summary Stat Mini-Cards */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: '16px'
        }}
      >
        <div
          className="card"
          onClick={() => setActiveTab('All')}
          style={{
            padding: '16px 20px',
            cursor: 'pointer',
            borderRight: activeTab === 'All' ? '4px solid var(--primary)' : '1px solid var(--border-main)',
            backgroundColor: activeTab === 'All' ? '#fbfaff' : '#ffffff'
          }}
        >
          <div style={{ fontSize: '12px', color: 'var(--text-secondary)', fontWeight: 600 }}>إجمالي الأجنحة</div>
          <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--text-primary)', marginTop: '4px' }}>{booths.length}</div>
        </div>

        <div
          className="card"
          onClick={() => setActiveTab('Available')}
          style={{
            padding: '16px 20px',
            cursor: 'pointer',
            borderRight: activeTab === 'Available' ? '4px solid var(--success)' : '1px solid var(--border-main)',
            backgroundColor: activeTab === 'Available' ? 'var(--success-bg)' : '#ffffff'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '12px', color: 'var(--success-text)', fontWeight: 600 }}>أجنحة متاحة</span>
            <CheckCircle2 size={16} color="var(--success-text)" />
          </div>
          <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--success-text)', marginTop: '4px' }}>{availableCount}</div>
        </div>

        <div
          className="card"
          onClick={() => setActiveTab('Reserved')}
          style={{
            padding: '16px 20px',
            cursor: 'pointer',
            borderRight: activeTab === 'Reserved' ? '4px solid #6366f1' : '1px solid var(--border-main)',
            backgroundColor: activeTab === 'Reserved' ? '#f5f3ff' : '#ffffff'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '12px', color: '#4f46e5', fontWeight: 600 }}>أجنحة محجوزة</span>
            <BookmarkCheck size={16} color="#4f46e5" />
          </div>
          <div style={{ fontSize: '24px', fontWeight: 800, color: '#4f46e5', marginTop: '4px' }}>{reservedCount}</div>
        </div>

        <div
          className="card"
          onClick={() => setActiveTab('PendingReview')}
          style={{
            padding: '16px 20px',
            cursor: 'pointer',
            borderRight: activeTab === 'PendingReview' ? '4px solid var(--warning)' : '1px solid var(--border-main)',
            backgroundColor: activeTab === 'PendingReview' ? 'var(--warning-bg)' : '#ffffff'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <span style={{ fontSize: '12px', color: 'var(--warning-text)', fontWeight: 600 }}>قيد المراجعة</span>
            <Clock size={16} color="var(--warning-text)" />
          </div>
          <div style={{ fontSize: '24px', fontWeight: 800, color: 'var(--warning-text)', marginTop: '4px' }}>{pendingCount}</div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="card" style={{ padding: '16px 20px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '16px', flexWrap: 'wrap' }}>
        <div style={{ position: 'relative', width: '380px', maxWidth: '100%' }}>
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
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="بحث برقم الجناح، اسم المعرض، أو الشركة..."
            className="input-field"
            style={{ paddingRight: '42px', height: '42px' }}
          />
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          {(['All', 'Available', 'Reserved', 'PendingReview'] as const).map(tab => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              style={{
                padding: '8px 16px',
                borderRadius: 'var(--radius-lg)',
                fontSize: '13px',
                fontWeight: 600,
                background: activeTab === tab ? 'var(--primary-gradient)' : 'var(--bg-secondary)',
                color: activeTab === tab ? '#ffffff' : 'var(--text-secondary)',
                boxShadow: activeTab === tab ? 'var(--primary-glow)' : 'none',
                transition: 'all var(--transition-normal)'
              }}
            >
              {tab === 'All' ? 'جميع الحالات' : tab === 'Available' ? 'متاح فقط' : tab === 'Reserved' ? 'محجوز' : 'قيد المراجعة'}
            </button>
          ))}
        </div>
      </div>

      {/* Full Comprehensive Table (from DESIGN.md section 9.3) */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <div className="table-container" style={{ border: 'none', borderRadius: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th style={{ width: '12%' }}>رقم الجناح</th>
                <th style={{ width: '22%' }}>المعرض التابع له</th>
                <th style={{ width: '18%' }}>الشركة العارضة</th>
                <th style={{ width: '14%' }}>الموقع / القاعة</th>
                <th style={{ width: '12%' }}>المساحة والأبعاد</th>
                <th style={{ width: '10%' }}>الفئة</th>
                <th style={{ width: '12%' }}>السعر</th>
                <th style={{ width: '10%' }}>الحالة</th>
              </tr>
            </thead>
            <tbody>
              {filteredBooths.map((booth) => {
                const isAvailable = booth.status === 'Available';
                const isReserved = booth.status === 'Reserved';

                return (
                  <tr key={booth.id}>
                    {/* Booth Number with colored box */}
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                        <div
                          style={{
                            width: '36px',
                            height: '36px',
                            borderRadius: 'var(--radius-md)',
                            background: isAvailable ? 'var(--success-bg)' : isReserved ? '#e0e7ff' : 'var(--warning-bg)',
                            color: isAvailable ? 'var(--success-text)' : isReserved ? '#4f46e5' : 'var(--warning-text)',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            fontWeight: 800,
                            fontSize: '13px'
                          }}
                        >
                          <Store size={18} />
                        </div>
                        <span style={{ fontWeight: 800, fontSize: '14px', color: 'var(--text-primary)' }}>
                          {booth.boothNumber}
                        </span>
                      </div>
                    </td>

                    {/* Exhibition */}
                    <td>
                      <span style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '13px' }}>
                        {booth.exhibitionName}
                      </span>
                    </td>

                    {/* Company */}
                    <td>
                      {booth.companyName ? (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--text-primary)', fontSize: '13px', fontWeight: 600 }}>
                          <Building2 size={14} style={{ color: 'var(--primary)' }} />
                          <span>{booth.companyName}</span>
                        </div>
                      ) : (
                        <span style={{ color: 'var(--text-muted)', fontSize: '12px' }}>غير مخصص بعد</span>
                      )}
                    </td>

                    {/* Hall */}
                    <td>
                      <span style={{ color: 'var(--text-secondary)', fontSize: '13px' }}>
                        {booth.hallName}
                      </span>
                    </td>

                    {/* Dimensions */}
                    <td>
                      <span style={{ color: 'var(--text-primary)', fontSize: '13px', fontWeight: 500 }}>
                        {booth.dimensions}
                      </span>
                    </td>

                    {/* Category */}
                    <td>
                      <span className={`badge ${getCategoryBadgeClass(booth.category)}`}>
                        {getCategoryArabic(booth.category)}
                      </span>
                    </td>

                    {/* Price */}
                    <td>
                      <div style={{ fontWeight: 700, color: 'var(--text-primary)', fontSize: '14px' }}>
                        {booth.price.toLocaleString()} د.ل
                      </div>
                    </td>

                    {/* Status Badge */}
                    <td>
                      <span
                        className={`badge ${
                          isAvailable
                            ? 'badge-success'
                            : isReserved
                            ? 'badge-info'
                            : 'badge-warning'
                        }`}
                      >
                        {isAvailable ? 'متاح' : isReserved ? 'محجوز' : 'قيد المراجعة'}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
