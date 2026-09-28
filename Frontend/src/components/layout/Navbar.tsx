import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { ROUTES } from '@/constants/routes';

export const Navbar: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const location = useLocation();

  const handleBrandOrHomeClick = (e: React.MouseEvent) => {
    if (location.pathname === ROUTES.HOME) {
      e.preventDefault();
      // Cuộn lên đầu trang và reload lại trang để tải các bài đăng chưa xem
      window.scrollTo({ top: 0, behavior: 'smooth' });
      window.location.reload();
    }
  };

  return (
    <header className="navbar">
      <div className="container nav-container">
        <Link
          to={ROUTES.HOME}
          className="nav-brand"
          onClick={handleBrandOrHomeClick}
          title="Tải lại trang để cập nhật bài viết mới chưa xem"
        >
          <span className="brand-icon">⚡</span>
          <span className="brand-text">Social</span>
          <span className="brand-badge">MVP</span>
        </Link>

        <nav className="nav-links">
          <Link
            to={ROUTES.HOME}
            className="nav-link"
            onClick={handleBrandOrHomeClick}
            title="Trang chủ"
          >
            Trang chủ
          </Link>
          {isAuthenticated && (
            <Link to={ROUTES.PROFILE} className="nav-link">
              Hồ sơ
            </Link>
          )}
        </nav>

        <div className="nav-actions">
          {isAuthenticated ? (
            <div className="user-menu">
              <Link to={ROUTES.PROFILE} className="user-profile-link">
                {user?.avatarUrl ? (
                  <img src={user.avatarUrl} alt={user.displayName} className="nav-avatar" />
                ) : (
                  <div className="nav-avatar-placeholder">
                    {user?.displayName ? user.displayName.charAt(0).toUpperCase() : 'U'}
                  </div>
                )}
                <span className="nav-username">{user?.displayName || 'Người dùng'}</span>
              </Link>
              <button onClick={logout} className="btn btn-secondary btn-sm" id="logout-btn">
                Đăng xuất
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
