import React, { useEffect } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { ROUTES } from '@/constants/routes';
import { Navbar } from './Navbar';
import { Footer } from './Footer';
import { RealtimeToast } from './RealtimeToast';
import { MobileNav } from './MobileNav';
import { PostDetailModal } from '@/components/posts/PostDetailModal';

export const Layout: React.FC = () => {
  const { pathname, search } = useLocation();

  // Tự động đưa vị trí cuộn lên đầu trang khi chuyển sang các trang ngoài Bảng tin (Chợ số, Chi tiết SP, Đơn mua...)
  useEffect(() => {
    if (pathname !== ROUTES.HOME) {
      window.scrollTo({ top: 0, left: 0, behavior: 'instant' });
    }
  }, [pathname, search]);

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

