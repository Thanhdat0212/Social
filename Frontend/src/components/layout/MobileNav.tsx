import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { useCartStore } from '@/store/cartStore';
import { ROUTES } from '@/constants/routes';
import {
  HomeIcon,
  MarketplaceIcon,
  CartIcon,
  StoreIcon,
  UserIcon,
  Avatar,
} from '@/components/common';

export const MobileNav: React.FC = () => {
  const { user, isAuthenticated } = useAuth();
  const { totalQuantity } = useCartStore();
  const location = useLocation();

  const isHome = location.pathname === ROUTES.HOME;
  const isMarketplace = location.pathname.startsWith('/marketplace') && !location.pathname.startsWith('/marketplace/cart');
  const isCart = location.pathname === ROUTES.MARKETPLACE.CART;
  const isSeller = location.pathname.startsWith('/seller');
  const isProfile = location.pathname === ROUTES.PROFILE;

  const handleHomeClick = (e: React.MouseEvent) => {
    if (isHome) {
      e.preventDefault();
      window.scrollTo({ top: 0, behavior: 'smooth' });
      window.dispatchEvent(new CustomEvent('social_refresh_feed'));
    }
  };

  return (
    <nav className="mobile-bottom-nav" aria-label="Điều hướng di động">
      <Link
        to={ROUTES.HOME}
        className={`mobile-nav-item ${isHome ? 'active' : ''}`}
        onClick={handleHomeClick}
        aria-label="Trang chủ"
      >
        <HomeIcon size={20} />
        <span>Bảng tin</span>
      </Link>

      <Link
        to={ROUTES.MARKETPLACE.INDEX}
        className={`mobile-nav-item ${isMarketplace ? 'active' : ''}`}
        aria-label="Chợ số"
      >
        <MarketplaceIcon size={20} />
        <span>Chợ số</span>
      </Link>

      <Link
        to={ROUTES.MARKETPLACE.CART}
        className={`mobile-nav-item ${isCart ? 'active' : ''}`}
        aria-label="Giỏ hàng"
        style={{ position: 'relative' }}
      >
        <CartIcon size={20} />
        {totalQuantity > 0 && (
          <span className="mobile-nav-badge">
            {totalQuantity > 99 ? '99+' : totalQuantity}
          </span>
        )}
        <span>Giỏ hàng</span>
      </Link>

      {isAuthenticated && (
        <Link
          to={ROUTES.SELLER.PORTAL}
          className={`mobile-nav-item ${isSeller ? 'active' : ''}`}
          aria-label="Kênh người bán"
        >
          <StoreIcon size={20} />
          <span>Người bán</span>
        </Link>
      )}

      {isAuthenticated ? (
        <Link
          to={ROUTES.PROFILE}
          className={`mobile-nav-item ${isProfile ? 'active' : ''}`}
          aria-label="Tài khoản cá nhân"
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
          aria-label="Đăng nhập"
        >
          <UserIcon size={20} />
          <span>Đăng nhập</span>
        </Link>
      )}
    </nav>
  );
};
