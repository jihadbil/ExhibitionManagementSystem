import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '../providers/AuthContext';
import { MainLayout } from '../layout/MainLayout';

// Features Pages
import { LoginPage } from '../../features/auth/pages/LoginPage';
import { DashboardPage } from '../../features/dashboard/pages/DashboardPage';
import { ExhibitionsPage } from '../../features/exhibitions/pages/ExhibitionsPage';
import { BoothsPage } from '../../features/booths/pages/BoothsPage';
import { BoothDesignerPage } from '../../features/booths/pages/BoothDesignerPage';
import { AnalyticsPage } from '../../features/analytics/pages/AnalyticsPage';
import { CompaniesPage } from '../../features/companies/pages/CompaniesPage';
import { EventsPage } from '../../features/events/pages/EventsPage';
import { TicketsPage } from '../../features/tickets/pages/TicketsPage';
import { BadgeDesignerPage } from '../../features/badges/pages/BadgeDesignerPage';
import { CheckInKioskPage } from '../../features/kiosk/pages/CheckInKioskPage';
import { ReservationsPage } from '../../features/reservations/pages/ReservationsPage';
import { InvoicesPage } from '../../features/financial/pages/InvoicesPage';
import { VenuesPage } from '../../features/venues/pages/VenuesPage';
import { SponsorshipPage } from '../../features/sponsorship/pages/SponsorshipPage';
import { ServicesPage } from '../../features/services-mgmt/pages/ServicesPage';
import { UsersPage } from '../../features/users/pages/UsersPage';
import { TenantsPage } from '../../features/tenants/pages/TenantsPage';
import { AdminAuditPage } from '../../features/admin-audit/pages/AdminAuditPage';
import { SettingsPage } from '../../features/settings/pages/SettingsPage';

// Protected Route Component
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div style={{ height: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: 'var(--bg-main)' }}>
        <div style={{ width: '36px', height: '36px', border: '3px solid #e2e8f0', borderTopColor: 'var(--primary)', borderRadius: '50%', animation: 'spin 0.8s linear infinite' }} />
      </div>
    );
  }

  // If not logged in, redirect to login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
};

export const AppRoutes: React.FC = () => {
  return (
    <Routes>
      {/* Public Route */}
      <Route path="/login" element={<LoginPage />} />

      {/* Protected Routes wrapped in MainLayout */}
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
        <Route path="analytics" element={<AnalyticsPage />} />
        <Route path="exhibitions" element={<ExhibitionsPage />} />
        <Route path="booths" element={<BoothsPage />} />
        <Route path="booths/designer" element={<BoothDesignerPage />} />
        <Route path="venues" element={<VenuesPage />} />
        <Route path="companies" element={<CompaniesPage />} />
        <Route path="events" element={<EventsPage />} />
        <Route path="tickets" element={<TicketsPage />} />
        <Route path="badges/designer" element={<BadgeDesignerPage />} />
        <Route path="kiosk" element={<CheckInKioskPage />} />
        <Route path="reservations" element={<ReservationsPage />} />
        <Route path="financial" element={<InvoicesPage />} />
        <Route path="sponsorship" element={<SponsorshipPage />} />
        <Route path="services-mgmt" element={<ServicesPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="tenants" element={<TenantsPage />} />
        <Route path="admin/audit" element={<AdminAuditPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
};
