import React, { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { useCartStore } from '@/store/cartStore';
import { ROUTES } from '@/constants/routes';
import { NotificationBell } from './NotificationBell';
import {
  Avatar,
  Badge,
  DropdownMenu,
  LogoIcon,
  HomeIcon,
  LogoutIcon,
  MarketplaceIcon,
  CartIcon,
  PackageIcon,
  StoreIcon,
  ShieldIcon,
  UserIcon,
  ChevronDownIcon,
  PageContainer,
} from '@/components/common';

export const Navbar: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const { totalQuantity, fetchCart } = useCartStore();
  const location = useLocation();

  const isHomeActive = location.pathname === ROUTES.HOME;
  const isMarketplaceActive = location.pathname.startsWith('/marketplace');
  const isCartActive = location.pathname === ROUTES.MARKETPLACE.CART;
  const isOrdersActive = location.pathname.startsWith('/orders');
  const isSellerActive = location.pathname.startsWith('/seller');
  const isAdminActive = location.pathname.startsWith('/admin');

  useEffect(() => {
    if (isAuthenticated) {
      fetchCart();
    }
  }, [isAuthenticated, fetchCart]);

  const handleBrandOrHomeClick = (e: React.MouseEvent) => {
    if (isHomeActive) {
      e.preventDefault();
      window.scrollTo({ top: 0, behavior: 'smooth' });
      window.dispatchEvent(new CustomEvent('social_refresh_feed'));
    }
  };

  return (
    <header className="navbar">
      <PageContainer variant="wide" className="nav-container">
        {/* Brand Logo */}
        <Link
          to={ROUTES.HOME}
          className="nav-brand"
          onClick={handleBrandOrHomeClick}
          title="Tải lại trang để cập nhật bài viết mới"
          aria-label="Social - Trang chủ"
        >
          <LogoIcon size={26} className="brand-logo-icon" />
          <span className="brand-text">Social</span>
        </Link>

        {/* Desktop Navigation Links */}
        <nav className="nav-links" aria-label="Điều hướng chính">
          <Link
            to={ROUTES.HOME}
            className={`nav-link ${isHomeActive ? 'active' : ''}`}
            onClick={handleBrandOrHomeClick}
          >
            <HomeIcon size={17} />
            <span>Bảng tin</span>
          </Link>

          <Link
            to={ROUTES.MARKETPLACE.INDEX}
            className={`nav-link ${isMarketplaceActive && !isCartActive && !isOrdersActive ? 'active' : ''}`}
          >
            <MarketplaceIcon size={17} />
            <span>Chợ số</span>
          </Link>

          {isAuthenticated && (
            <>
              <Link
                to={ROUTES.MARKETPLACE.ORDERS}
                className={`nav-link ${isOrdersActive ? 'active' : ''}`}
              >
                <PackageIcon size={17} />
                <span>Đơn mua</span>
              </Link>

              <Link
                to={ROUTES.SELLER.PORTAL}
                className={`nav-link ${isSellerActive ? 'active' : ''}`}
              >
                <StoreIcon size={17} />
                <span>Kênh người bán</span>
              </Link>

              {user?.role === 'Admin' && (
                <Link
                  to={ROUTES.ADMIN.DASHBOARD}
                  className={`nav-link ${isAdminActive ? 'active' : ''}`}
                  title="Cổng quản trị toàn sàn"
                >
                  <ShieldIcon size={17} />
                  <span>Quản trị sàn</span>
                </Link>
              )}
            </>
          )}
        </nav>

        {/* Right Actions */}
        <div className="nav-actions">
          {/* Nút giỏ hàng */}
          <Link
            to={ROUTES.MARKETPLACE.CART}
            className={`nav-link cart-nav-btn ${isCartActive ? 'active' : ''}`}
            title="Giỏ hàng của bạn"
            aria-label={`Giỏ hàng${totalQuantity > 0 ? `, ${totalQuantity} sản phẩm` : ''}`}
            style={{ position: 'relative', padding: '0.45rem 0.75rem' }}
          >
            <CartIcon size={20} />
            {totalQuantity > 0 && (
              <span
                style={{
                  position: 'absolute',
                  top: '2px',
                  right: '2px',
                  background: 'var(--accent-primary)',
                  color: '#fff',
                  borderRadius: '999px',
                  fontSize: '0.7rem',
                  fontWeight: 700,
                  minWidth: '18px',
                  height: '18px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  padding: '0 4px',
                }}
              >
                {totalQuantity > 99 ? '99+' : totalQuantity}
              </span>
            )}
          </Link>

          {isAuthenticated ? (
            <div className="user-menu">
              <NotificationBell />

              {/* User Account Dropdown */}
              <DropdownMenu
                align="right"
                trigger={(isOpen) => (
                  <button
                    type="button"
                    className={`user-account-btn ${isOpen ? 'is-active' : ''}`}
                    title={user?.displayName || 'Tài khoản người dùng'}
                    aria-label="Menu tài khoản"
                  >
                    <Avatar
                      src={user?.avatarUrl}
                      name={user?.displayName}
                      size="sm"
                    />
                    <span className="user-account-name">{user?.displayName || 'Tài khoản'}</span>
                    <span className="user-account-arrow">
                      <ChevronDownIcon size={14} />
                    </span>
                  </button>
                )}
              >
                <div style={{ padding: '0.65rem 0.85rem', borderBottom: '1px solid var(--border-color)' }}>
                  <div style={{ fontWeight: 700, fontSize: '0.9rem', color: 'var(--text-main)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {user?.displayName}
                  </div>
                  <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                    {user?.email}
                  </div>
                  {user?.role === 'Admin' && (
                    <Badge variant="warning" size="sm" style={{ marginTop: '0.35rem' }}>
                      Quản trị viên
                    </Badge>
                  )}
                </div>

                <div style={{ padding: '0.25rem 0' }}>
                  <Link to={ROUTES.PROFILE} className="dropdown-item-link" id="dropdown-profile-link">
                    <UserIcon size={16} />
                    <span>Trang cá nhân</span>
                  </Link>

                  <Link to={ROUTES.MARKETPLACE.ORDERS} className="dropdown-item-link">
                    <PackageIcon size={16} />
                    <span>Đơn hàng đã mua</span>
                  </Link>

                  <Link to={ROUTES.SELLER.PORTAL} className="dropdown-item-link">
                    <StoreIcon size={16} />
                    <span>Kênh người bán</span>
                  </Link>

                  {user?.role === 'Admin' && (
                    <Link to={ROUTES.ADMIN.DASHBOARD} className="dropdown-item-link">
                      <ShieldIcon size={16} />
                      <span>Quản trị hệ thống</span>
                    </Link>
                  )}
                </div>

                <div className="dropdown-divider" />

                <button
                  type="button"
                  onClick={logout}
                  className="dropdown-item-btn is-danger"
                  id="dropdown-logout-btn"
                  aria-label="Đăng xuất"
                >
                  <LogoutIcon size={16} />
                  <span>Đăng xuất</span>
                </button>
              </DropdownMenu>
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
      </PageContainer>
    </header>
  );
};
