import { lazy, Suspense, type ComponentType, type ReactNode } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { MainLayout } from '../layout/MainLayout';
import { useAuth } from '../providers/AuthContext';

const lazyNamed = <T extends Record<string, unknown>, K extends keyof T>(
  importer: () => Promise<T>,
  exportName: K,
) => lazy(async () => ({ default: (await importer())[exportName] as ComponentType }));

const LoginPage = lazyNamed(() => import('../../features/auth/pages/LoginPage'), 'LoginPage');
const DashboardPage = lazyNamed(() => import('../../features/dashboard/pages/DashboardPage'), 'DashboardPage');
const ExhibitionsPage = lazyNamed(() => import('../../features/exhibitions/pages/ExhibitionsPage'), 'ExhibitionsPage');
const BoothsPage = lazyNamed(() => import('../../features/booths/pages/BoothsPage'), 'BoothsPage');
const BoothDesignerPage = lazyNamed(() => import('../../features/booths/pages/BoothDesignerPage'), 'BoothDesignerPage');
const AnalyticsPage = lazyNamed(() => import('../../features/analytics/pages/AnalyticsPage'), 'AnalyticsPage');
const CompaniesPage = lazyNamed(() => import('../../features/companies/pages/CompaniesPage'), 'CompaniesPage');
const EventsPage = lazyNamed(() => import('../../features/events/pages/EventsPage'), 'EventsPage');
const TicketsPage = lazyNamed(() => import('../../features/tickets/pages/TicketsPage'), 'TicketsPage');
const BadgeDesignerPage = lazyNamed(() => import('../../features/badges/pages/BadgeDesignerPage'), 'BadgeDesignerPage');
const CheckInKioskPage = lazyNamed(() => import('../../features/kiosk/pages/CheckInKioskPage'), 'CheckInKioskPage');
const ReservationsPage = lazyNamed(() => import('../../features/reservations/pages/ReservationsPage'), 'ReservationsPage');
const InvoicesPage = lazyNamed(() => import('../../features/financial/pages/InvoicesPage'), 'InvoicesPage');
const VenuesPage = lazyNamed(() => import('../../features/venues/pages/VenuesPage'), 'VenuesPage');
const SponsorshipPage = lazyNamed(() => import('../../features/sponsorship/pages/SponsorshipPage'), 'SponsorshipPage');
const ServicesPage = lazyNamed(() => import('../../features/services-mgmt/pages/ServicesPage'), 'ServicesPage');
const UsersPage = lazyNamed(() => import('../../features/users/pages/UsersPage'), 'UsersPage');
const TenantsPage = lazyNamed(() => import('../../features/tenants/pages/TenantsPage'), 'TenantsPage');
const AdminAuditPage = lazyNamed(() => import('../../features/admin-audit/pages/AdminAuditPage'), 'AdminAuditPage');
const SettingsPage = lazyNamed(() => import('../../features/settings/pages/SettingsPage'), 'SettingsPage');

const TENANT_ADMINS = ['Admin', 'TenantAdmin'] as const;
const EVENT_MANAGERS = ['Admin', 'TenantAdmin', 'EventManager'] as const;
const FINANCE_MANAGERS = ['Admin', 'TenantAdmin', 'Finance'] as const;
const GATE_OPERATORS = ['Admin', 'TenantAdmin', 'EventManager', 'GateOperator'] as const;

const LoadingScreen = () => (
  <div
    aria-label="جاري تحميل الصفحة"
    style={{ height: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'var(--bg-main)' }}
  >
    <div style={{ width: '36px', height: '36px', border: '3px solid #e2e8f0', borderTopColor: 'var(--primary)', borderRadius: '50%', animation: 'spin 0.8s linear infinite' }} />
  </div>
);

const ProtectedRoute = ({ children, allowedRoles }: { children: ReactNode; allowedRoles?: readonly string[] }) => {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) return <LoadingScreen />;
  if (!isAuthenticated) return <Navigate to="/login" replace />;

  if (allowedRoles && !allowedRoles.some(role => user?.roles.includes(role))) {
    return <Navigate to="/unauthorized" replace />;
  }

  return children;
};

const RoleRoute = ({ children, roles }: { children: ReactNode; roles: readonly string[] }) => (
  <ProtectedRoute allowedRoles={roles}>{children}</ProtectedRoute>
);

const UnauthorizedPage = () => (
  <div className="card" style={{ padding: '40px', textAlign: 'center', maxWidth: '600px', margin: '80px auto' }}>
    <h1 style={{ marginBottom: '12px' }}>لا تملك صلاحية الوصول</h1>
    <p style={{ color: 'var(--text-secondary)', marginBottom: '20px' }}>
      هذا القسم يتطلب دوراً إدارياً مختلفاً. لم تُرسل أي عملية إلى الخادم.
    </p>
    <a className="btn-primary" href="/dashboard">العودة إلى لوحة التحكم</a>
  </div>
);

export const AppRoutes = () => (
  <Suspense fallback={<LoadingScreen />}>
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        path="/"
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="unauthorized" element={<UnauthorizedPage />} />
        <Route path="analytics" element={<RoleRoute roles={FINANCE_MANAGERS}><AnalyticsPage /></RoleRoute>} />
        <Route path="exhibitions" element={<ExhibitionsPage />} />
        <Route path="booths" element={<BoothsPage />} />
        <Route path="booths/designer" element={<RoleRoute roles={TENANT_ADMINS}><BoothDesignerPage /></RoleRoute>} />
        <Route path="venues" element={<VenuesPage />} />
        <Route path="companies" element={<CompaniesPage />} />
        <Route path="events" element={<RoleRoute roles={EVENT_MANAGERS}><EventsPage /></RoleRoute>} />
        <Route path="tickets" element={<RoleRoute roles={GATE_OPERATORS}><TicketsPage /></RoleRoute>} />
        <Route path="badges/designer" element={<RoleRoute roles={EVENT_MANAGERS}><BadgeDesignerPage /></RoleRoute>} />
        <Route path="kiosk" element={<RoleRoute roles={GATE_OPERATORS}><CheckInKioskPage /></RoleRoute>} />
        <Route path="reservations" element={<RoleRoute roles={EVENT_MANAGERS}><ReservationsPage /></RoleRoute>} />
        <Route path="financial" element={<RoleRoute roles={FINANCE_MANAGERS}><InvoicesPage /></RoleRoute>} />
        <Route path="sponsorship" element={<RoleRoute roles={EVENT_MANAGERS}><SponsorshipPage /></RoleRoute>} />
        <Route path="services-mgmt" element={<RoleRoute roles={TENANT_ADMINS}><ServicesPage /></RoleRoute>} />
        <Route path="users" element={<RoleRoute roles={TENANT_ADMINS}><UsersPage /></RoleRoute>} />
        <Route path="tenants" element={<RoleRoute roles={['SuperAdmin']}><TenantsPage /></RoleRoute>} />
        <Route path="admin/audit" element={<RoleRoute roles={TENANT_ADMINS}><AdminAuditPage /></RoleRoute>} />
        <Route path="settings" element={<RoleRoute roles={TENANT_ADMINS}><SettingsPage /></RoleRoute>} />
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  </Suspense>
);
