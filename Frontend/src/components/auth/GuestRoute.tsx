import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { LoadingScreen } from '@/components/feedback/LoadingScreen';
import { ROUTES } from '@/constants/routes';

export interface GuestRouteProps {
  children?: React.ReactNode;
}

/**
 * Route chỉ dành cho khách chưa đăng nhập.
 * Nếu đã đăng nhập thì tự động điều hướng sang trang Hồ sơ cá nhân.
 */
export const GuestRoute: React.FC<GuestRouteProps> = ({ children }) => {
  const { isAuthenticated, isInitializing } = useAuthStore();

  if (isInitializing) {
    return <LoadingScreen message="Đang khởi tạo..." />;
  }

  if (isAuthenticated) {
    return <Navigate to={ROUTES.PROFILE} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
};
