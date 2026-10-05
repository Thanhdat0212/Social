import React from 'react';
import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';
import { Footer } from './Footer';
import { RealtimeToast } from './RealtimeToast';
import { MobileNav } from './MobileNav';

export const Layout: React.FC = () => {
  return (
    <div className="app-layout">
      <Navbar />
      <main className="main-content">
        <Outlet />
      </main>
      <Footer />
      <MobileNav />
      <RealtimeToast />
    </div>
  );
};
