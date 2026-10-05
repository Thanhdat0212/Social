import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { ROUTES } from '@/constants/routes';
import { HomeIcon, UserIcon, Avatar } from '@/components/common';

export const MobileNav: React.FC = () => {
  const { user, isAuthenticated } = useAuth();
  const location = useLocation();

  const isHome = location.pathname === ROUTES.HOME;
  const isProfile = location.pathname === ROUTES.PROFILE;

  const handleHomeClick = () => {
    if (isHome) {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  };

  return (
    <nav className="mobile-bottom-nav" aria-label="Điều hướng di động">
      <Link
        to={ROUTES.HOME}
        className={`mobile-nav-item ${isHome ? 'active' : ''}`}
        onClick={handleHomeClick}
      >
        <HomeIcon size={22} />
        <span>Trang chủ</span>
      </Link>

      {isAuthenticated ? (
        <Link
          to={ROUTES.PROFILE}
          className={`mobile-nav-item ${isProfile ? 'active' : ''}`}
        >
          <Avatar
            src={user?.avatarUrl}
            name={user?.displayName}
            size="xs"
            className="mobile-nav-avatar"
          />
          <span>Hồ sơ</span>
        </Link>
      ) : (
        <Link
          to={ROUTES.AUTH.LOGIN}
          className={`mobile-nav-item ${location.pathname === ROUTES.AUTH.LOGIN ? 'active' : ''}`}
        >
          <UserIcon size={22} />
          <span>Đăng nhập</span>
        </Link>
      )}
    </nav>
  );
};
