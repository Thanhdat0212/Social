import React from 'react';
import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';
import { Footer } from './Footer';
import { RealtimeToast } from './RealtimeToast';
import { MobileNav } from './MobileNav';
import { PostDetailModal } from '@/components/posts/PostDetailModal';

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
      <PostDetailModal />
    </div>
  );
};

