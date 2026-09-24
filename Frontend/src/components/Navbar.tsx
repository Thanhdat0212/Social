import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/api/authApi';

export const Navbar: React.FC = () => {
  const { user, isAuthenticated, clearAuth } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } catch (err) {
      console.error('Logout error:', err);
    } finally {
      clearAuth();
      navigate('/login');
    }
  };

  return (
    <header className="navbar">
      <div className="container nav-container">
        <Link to="/" className="nav-brand">
          <span className="brand-icon">⚡</span>
          <span className="brand-text">Social</span>
          <span className="brand-badge">MVP</span>
        </Link>

        <nav className="nav-links">
          <Link to="/" className="nav-link">
            Trang chủ
          </Link>
          {isAuthenticated && (
            <Link to="/profile" className="nav-link">
              Hồ sơ
            </Link>
          )}
        </nav>

        <div className="nav-actions">
          {isAuthenticated ? (
            <div className="user-menu">
              <Link to="/profile" className="user-profile-link">
                {user?.avatarUrl ? (
                  <img src={user.avatarUrl} alt={user.displayName} className="nav-avatar" />
                ) : (
                  <div className="nav-avatar-placeholder">
                    {user?.displayName ? user.displayName.charAt(0).toUpperCase() : 'U'}
                  </div>
                )}
                <span className="nav-username">{user?.displayName || 'Người dùng'}</span>
              </Link>
              <button onClick={handleLogout} className="btn btn-secondary btn-sm" id="logout-btn">
                Đăng xuất
              </button>
            </div>
          ) : (
            <div className="auth-buttons">
              <Link to="/login" className="btn btn-ghost btn-sm" id="login-nav-btn">
                Đăng nhập
              </Link>
              <Link to="/register" className="btn btn-primary btn-sm" id="register-nav-btn">
                Đăng ký
              </Link>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
