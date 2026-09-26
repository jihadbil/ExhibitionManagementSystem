import React from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';

export const MainLayout: React.FC = () => {
  return (
    <div style={{ display: 'flex', minHeight: '100vh', backgroundColor: 'var(--bg-main)' }}>
      {/* Fixed RTL Sidebar */}
      <Sidebar />

      {/* Main Container */}
      <div
        style={{
          flex: 1,
          marginRight: 'var(--sidebar-width)',
          display: 'flex',
          flexDirection: 'column',
          minWidth: 0
        }}
      >
        {/* Fixed TopBar */}
        <TopBar />

        {/* Dynamic Page Content */}
        <main
          style={{
            flex: 1,
            marginTop: 'var(--topbar-height)',
            padding: '32px 36px 64px',
            backgroundColor: 'var(--bg-main)',
            maxWidth: '1600px',
            width: '100%',
            marginRight: 'auto',
            marginLeft: 'auto'
          }}
        >
          <Outlet />
        </main>
      </div>
    </div>
  );
};
