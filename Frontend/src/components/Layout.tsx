import React from 'react';
import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';

export const Layout: React.FC = () => {
  return (
    <div className="app-layout">
      <Navbar />
      <main className="main-content">
        <Outlet />
      </main>
      <footer className="footer">
        <div className="container footer-content">
          <p>© 2026 Social Platform — Phase 1 MVP (Auth & Profile)</p>
          <div className="footer-links">
            <span className="api-status">
              <span className="status-dot"></span> API: <code>/api</code> (Vite Proxy)
            </span>
          </div>
        </div>
      </footer>
    </div>
  );
};
