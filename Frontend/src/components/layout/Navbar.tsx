import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { ROUTES } from '@/constants/routes';
import { NotificationBell } from './NotificationBell';
import { Avatar, LogoIcon, HomeIcon, UserIcon, LogoutIcon } from '@/components/common';

export const Navbar: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const location = useLocation();

  const isHomeActive = location.pathname === ROUTES.HOME;
  const isProfileActive = location.pathname === ROUTES.PROFILE;

  const handleBrandOrHomeClick = (e: React.MouseEvent) => {
    if (isHomeActive) {
      e.preventDefault();
      window.scrollTo({ top: 0, behavior: 'smooth' });
      window.dispatchEvent(new CustomEvent('social_refresh_feed'));
    }
  };

  return (
    <header className="navbar">
      <div className="container nav-container">
        {/* Brand Logo */}
        <Link
          to={ROUTES.HOME}
          className="nav-brand"
          onClick={handleBrandOrHomeClick}
          title="Tải lại trang để cập nhật bài viết mới"
        >
          <LogoIcon size={26} className="brand-logo-icon" />
          <span className="brand-text">Social</span>
        </Link>

        {/* Desktop Navigation Links */}
        <nav className="nav-links">
          <Link
            to={ROUTES.HOME}
            className={`nav-link ${isHomeActive ? 'active' : ''}`}
            onClick={handleBrandOrHomeClick}
          >
            <HomeIcon size={17} />
            <span>Trang chủ</span>
          </Link>

          {isAuthenticated && (
            <Link
              to={ROUTES.PROFILE}
              className={`nav-link ${isProfileActive ? 'active' : ''}`}
            >
              <UserIcon size={17} />
              <span>Hồ sơ</span>
            </Link>
          )}
        </nav>

        {/* Right Actions */}
        <div className="nav-actions">
          {isAuthenticated ? (
            <div className="user-menu">
              <NotificationBell />

              <Link
                to={ROUTES.PROFILE}
                className={`user-profile-link ${isProfileActive ? 'active' : ''}`}
                title="Trang cá nhân của bạn"
              >
                <Avatar
                  src={user?.avatarUrl}
                  name={user?.displayName}
                  size="sm"
                />
                <span className="nav-username">{user?.displayName || 'Người dùng'}</span>
              </Link>

              <button
                type="button"
                onClick={logout}
                className="btn-nav-logout"
                id="logout-btn"
                title="Đăng xuất"
                aria-label="Đăng xuất"
              >
                <LogoutIcon size={17} />
                <span className="logout-text">Đăng xuất</span>
              </button>
            </div>
          ) : (
            <div className="auth-buttons">
              <Link to={ROUTES.AUTH.LOGIN} className="btn btn-ghost btn-sm" id="login-nav-btn">
                Đăng nhập
              </Link>
              <Link to={ROUTES.AUTH.REGISTER} className="btn btn-primary btn-sm" id="register-nav-btn">
                Đăng ký
              </Link>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
