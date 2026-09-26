import React, { useState, useEffect } from 'react';
import { NavLink } from 'react-router-dom';
import {
  Building2,
  Store,
  Building,
  Users,
  Clock,
  Calendar as CalendarIcon,
  PlusCircle,
  FileSpreadsheet,
  Compass
} from 'lucide-react';
import { useAuth } from '../../../app/providers/AuthContext';
import { dashboardService } from '../services/dashboard.service';
import { DashboardStatsDto, MonthlyRevenuePoint, ExhibitionTypeItem, UpcomingExhibitionItem } from '../types/dashboard.types';
import { StatCard } from '../components/StatCard';
import { MonthlyRevenueBarChart } from '../components/MonthlyRevenueBarChart';
import { TypeDistributionDonutChart } from '../components/TypeDistributionDonutChart';
import { UpcomingExhibitionsTable } from '../components/UpcomingExhibitionsTable';

export const DashboardPage: React.FC = () => {
  const { user } = useAuth();

  const [stats, setStats] = useState<DashboardStatsDto>({
    activeExhibitions: 8,
    occupiedBooths: 142,
    totalExhibitors: 86,
    totalVisitors: 12450,
    totalRevenue: 245800
  });

  const [monthlyRevenue, setMonthlyRevenue] = useState<MonthlyRevenuePoint[]>([]);
  const [typeDistribution, setTypeDistribution] = useState<ExhibitionTypeItem[]>([]);
  const [upcomingExhibitions, setUpcomingExhibitions] = useState<UpcomingExhibitionItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Real-time Clock & Date
  const [currentTime, setCurrentTime] = useState('');
  const [currentDate, setCurrentDate] = useState('');
  const [greeting, setGreeting] = useState('');

  useEffect(() => {
    const updateTime = () => {
      const now = new Date();
      setCurrentTime(now.toLocaleTimeString('ar-LY', { hour: '2-digit', minute: '2-digit', second: '2-digit' }));
      setCurrentDate(now.toLocaleDateString('ar-LY', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }));

      const hour = now.getHours();
      if (hour < 12) {
        setGreeting('صباح الخير والإنتاجية');
      } else if (hour < 17) {
        setGreeting('طاب مساؤك');
      } else {
        setGreeting('مساء الخير');
      }
    };

    updateTime();
    const timer = setInterval(updateTime, 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    const loadDashboardData = async () => {
      setIsLoading(true);
      try {
        const [statsData, revenueData, typesData, upcomingData] = await Promise.all([
          dashboardService.getStats(),
          dashboardService.getMonthlyRevenue(),
          dashboardService.getTypeDistribution(),
          dashboardService.getUpcomingExhibitions()
        ]);

        setStats(statsData);
        setMonthlyRevenue(revenueData);
        setTypeDistribution(typesData);
        setUpcomingExhibitions(upcomingData);
      } catch (err) {
        console.error('Error loading dashboard data', err);
      } finally {
        setIsLoading(false);
      }
    };

    loadDashboardData();
  }, []);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '28px' }}>
      {/* Page Header with Greeting & Live Clock */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '16px'
        }}
      >
        <div>
          <h1 style={{ fontSize: '26px', fontWeight: 800, color: 'var(--text-primary)', marginBottom: '4px' }}>
            {greeting}، {user?.fullName || 'مدير المنظومة'} 👋
          </h1>
          <p style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>
            إليك ملخص النشاط التشغيلي والإحصائي لكافة المعارض والأجنحة اليوم
          </p>
        </div>

        {/* Clock & Date Badge + Quick Action */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
          <div
            className="card"
            style={{
              padding: '8px 16px',
              display: 'flex',
              alignItems: 'center',
              gap: '12px',
              borderRadius: 'var(--radius-lg)',
              backgroundColor: '#ffffff'
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--primary)', fontSize: '13px', fontWeight: 600 }}>
              <Clock size={16} />
              <span>{currentTime}</span>
            </div>
            <span style={{ color: 'var(--border-main)' }}>|</span>
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--text-secondary)', fontSize: '13px' }}>
              <CalendarIcon size={16} />
              <span>{currentDate}</span>
            </div>
          </div>

          <NavLink
            to="/exhibitions?action=create"
            className="btn-primary"
            style={{ height: '42px', fontSize: '13px' }}
          >
            <PlusCircle size={16} />
            <span>معرض جديد</span>
          </NavLink>
        </div>
      </div>

      {/* 4 Stat Cards Row (SCREEN_23 from DESIGN.md) */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
          gap: '20px'
        }}
      >
        <StatCard
          title="المعارض النشطة"
          value={stats.activeExhibitions}
          icon={Building2}
          iconColor="#3b82f6"
          iconBg="#dbeafe"
          trend={{ value: '+12.5%', isPositive: true }}
          subtitle="مقارنة بالربع الماضي"
        />

        <StatCard
          title="الأجنحة المحجوزة"
          value={`${stats.occupiedBooths} جناح`}
          icon={Store}
          iconColor="#6366f1"
          iconBg="#e0e7ff"
          trend={{ value: '+8.3%', isPositive: true }}
          subtitle="نسبة إشغال إجمالية 84%"
        />

        <StatCard
          title="الشركات العارضة"
          value={`${stats.totalExhibitors} شركة`}
          icon={Building}
          iconColor="#f59e0b"
          iconBg="#fef3c7"
          trend={{ value: '+15.2%', isPositive: true }}
          subtitle="من 12 قطاع تجاري"
        />

        <StatCard
          title="الزوار المسجلين"
          value={stats.totalVisitors.toLocaleString()}
          icon={Users}
          iconColor="#10b981"
          iconBg="#d1fae5"
          trend={{ value: '+24.1%', isPositive: true }}
          subtitle="تذاكر وبطاقات مصدرة"
        />
      </div>

      {/* Quick Launchpad Strip */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
          gap: '16px'
        }}
      >
        <NavLink
          to="/booths/designer"
          className="card"
          style={{
            padding: '16px 20px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            textDecoration: 'none',
            background: 'linear-gradient(to right, #fbfaff, #ffffff)',
            borderRight: '4px solid var(--primary)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div
              style={{
                width: '40px',
                height: '40px',
                borderRadius: 'var(--radius-lg)',
                background: 'var(--primary-light)',
                color: 'var(--primary)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}
            >
              <Compass size={22} />
            </div>
            <div>
              <div style={{ fontWeight: 700, fontSize: '14px', color: 'var(--text-primary)' }}>
                مصمم المخططات ثنائي الأبعاد
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                سحب وإفلات وتوزيع الأجنحة وكشف التصادم
              </div>
            </div>
          </div>
          <span style={{ fontSize: '18px', color: 'var(--primary)' }}>←</span>
        </NavLink>

        <NavLink
          to="/kiosk"
          className="card"
          style={{
            padding: '16px 20px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            textDecoration: 'none',
            background: 'linear-gradient(to right, #f8fff9, #ffffff)',
            borderRight: '4px solid var(--success)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div
              style={{
                width: '40px',
                height: '40px',
                borderRadius: 'var(--radius-lg)',
                background: 'var(--success-bg)',
                color: 'var(--success-text)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}
            >
              <Store size={22} />
            </div>
            <div>
              <div style={{ fontWeight: 700, fontSize: '14px', color: 'var(--text-primary)' }}>
                كشك الاستقبال وطباعة الشارات
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                تسجيل الحضور الفوري ومسح أكواد QR
              </div>
            </div>
          </div>
          <span style={{ fontSize: '18px', color: 'var(--success)' }}>←</span>
        </NavLink>

        <NavLink
          to="/analytics"
          className="card"
          style={{
            padding: '16px 20px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            textDecoration: 'none',
            background: 'linear-gradient(to right, #fffbf5, #ffffff)',
            borderRight: '4px solid var(--warning)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div
              style={{
                width: '40px',
                height: '40px',
                borderRadius: 'var(--radius-lg)',
                background: 'var(--warning-bg)',
                color: 'var(--warning-text)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center'
              }}
            >
              <FileSpreadsheet size={22} />
            </div>
            <div>
              <div style={{ fontWeight: 700, fontSize: '14px', color: 'var(--text-primary)' }}>
                التقارير التحليلية والخريطة الحرارية
              </div>
              <div style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
                معدلات الإشغال وكثافة حركة الزوار
              </div>
            </div>
          </div>
          <span style={{ fontSize: '18px', color: 'var(--warning)' }}>←</span>
        </NavLink>
      </div>

      {/* Charts Section: 2/3 Monthly Revenue Bar Chart + 1/3 Type Distribution Donut Chart */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '2fr 1fr',
          gap: '24px'
        }}
      >
        <MonthlyRevenueBarChart data={monthlyRevenue} />
        <TypeDistributionDonutChart data={typeDistribution} />
      </div>

      {/* Upcoming Exhibitions Table (Full Width) */}
      <UpcomingExhibitionsTable exhibitions={upcomingExhibitions} />
    </div>
  );
};
