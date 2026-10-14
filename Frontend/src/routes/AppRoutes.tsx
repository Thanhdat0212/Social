import React, { Suspense } from 'react';
import { Route, Routes } from 'react-router-dom';
import { Layout } from '@/components/layout/Layout';
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { GuestRoute } from '@/components/auth/GuestRoute';
import { AdminRoute } from '@/components/auth/AdminRoute';
import { RouteLoadingFallback } from '@/components/common';
import { ROUTES } from '@/constants/routes';

// Helper lazy-loading named exports
const lazyPage = (importFn: () => Promise<any>, name: string) =>
  React.lazy(() => importFn().then((mod) => ({ default: mod[name] })));

// Core Pages (Lazy loaded)
const HomePage = lazyPage(() => import('@/pages/home/HomePage'), 'HomePage');
const ProfilePage = lazyPage(() => import('@/pages/profile/ProfilePage'), 'ProfilePage');
const OnboardingPage = lazyPage(() => import('@/pages/onboarding/OnboardingPage'), 'OnboardingPage');
const PostDetailPage = lazyPage(() => import('@/pages/post-detail/PostDetailPage'), 'PostDetailPage');
const NotFoundPage = lazyPage(() => import('@/pages/not-found/NotFoundPage'), 'NotFoundPage');

// Auth Pages (Lazy loaded)
const LoginPage = lazyPage(() => import('@/pages/auth/LoginPage'), 'LoginPage');
const RegisterPage = lazyPage(() => import('@/pages/auth/RegisterPage'), 'RegisterPage');
const VerifyEmailPage = lazyPage(() => import('@/pages/auth/VerifyEmailPage'), 'VerifyEmailPage');
const ForgotPasswordPage = lazyPage(() => import('@/pages/auth/ForgotPasswordPage'), 'ForgotPasswordPage');
const ResetPasswordPage = lazyPage(() => import('@/pages/auth/ResetPasswordPage'), 'ResetPasswordPage');

// Marketplace Pages (Lazy loaded)
const MarketplacePage = lazyPage(() => import('@/pages/marketplace/MarketplacePage'), 'MarketplacePage');
const ProductDetailPage = lazyPage(() => import('@/pages/marketplace/ProductDetailPage'), 'ProductDetailPage');
const ShopProfilePage = lazyPage(() => import('@/pages/marketplace/ShopProfilePage'), 'ShopProfilePage');
const CartPage = lazyPage(() => import('@/pages/marketplace/CartPage'), 'CartPage');
const CheckoutPage = lazyPage(() => import('@/pages/marketplace/CheckoutPage'), 'CheckoutPage');
const MyOrdersPage = lazyPage(() => import('@/pages/marketplace/MyOrdersPage'), 'MyOrdersPage');
const OrderDetailPage = lazyPage(() => import('@/pages/marketplace/OrderDetailPage'), 'OrderDetailPage');
const SellerPortalPage = lazyPage(() => import('@/pages/seller/SellerPortalPage'), 'SellerPortalPage');

// Admin Pages (Lazy loaded)
const AdminDashboardPage = lazyPage(() => import('@/pages/admin/AdminDashboardPage'), 'AdminDashboardPage');

export const AppRoutes: React.FC = () => {
  return (
    <Suspense fallback={<RouteLoadingFallback />}>
      <Routes>
        <Route path={ROUTES.HOME} element={<Layout />}>
          {/* Public Routes */}
          <Route index element={<HomePage />} />
          <Route path={ROUTES.AUTH.VERIFY_EMAIL.slice(1)} element={<VerifyEmailPage />} />
          <Route path="posts/:id" element={<PostDetailPage />} />

          {/* Public Marketplace Routes */}
          <Route path={ROUTES.MARKETPLACE.INDEX.slice(1)} element={<MarketplacePage />} />
          <Route path={ROUTES.MARKETPLACE.PRODUCT_DETAIL.slice(1)} element={<ProductDetailPage />} />
          <Route path={ROUTES.MARKETPLACE.SHOP_PROFILE.slice(1)} element={<ShopProfilePage />} />

          {/* Guest Only Routes (chuyển hướng sang Profile nếu đã đăng nhập) */}
          <Route
            path={ROUTES.AUTH.LOGIN.slice(1)}
            element={
              <GuestRoute>
                <LoginPage />
              </GuestRoute>
            }
          />
          <Route
            path={ROUTES.AUTH.REGISTER.slice(1)}
            element={
              <GuestRoute>
                <RegisterPage />
              </GuestRoute>
            }
          />
          <Route
            path={ROUTES.AUTH.FORGOT_PASSWORD.slice(1)}
            element={
              <GuestRoute>
                <ForgotPasswordPage />
              </GuestRoute>
            }
          />
          <Route
            path={ROUTES.AUTH.RESET_PASSWORD.slice(1)}
            element={
              <GuestRoute>
                <ResetPasswordPage />
              </GuestRoute>
            }
          />

          {/* Protected Routes (yêu cầu đã đăng nhập) */}
          <Route
            path={ROUTES.PROFILE.slice(1)}
            element={
              <ProtectedRoute>
                <ProfilePage />
              </ProtectedRoute>
            }
          />
          <Route
            path={ROUTES.ONBOARDING.slice(1)}
            element={
              <ProtectedRoute>
                <OnboardingPage />
              </ProtectedRoute>
            }
          />

          {/* Protected Marketplace & Seller Routes */}
          <Route
            path={ROUTES.MARKETPLACE.CART.slice(1)}
            element={
              <ProtectedRoute>
                <CartPage />
              </ProtectedRoute>
            }
          />
          <Route
            path={ROUTES.MARKETPLACE.CHECKOUT.slice(1)}
            element={
              <ProtectedRoute>
                <CheckoutPage />
              </ProtectedRoute>
            }
          />
          <Route
            path={ROUTES.MARKETPLACE.ORDERS.slice(1)}
            element={
              <ProtectedRoute>
                <MyOrdersPage />
              </ProtectedRoute>
            }
          />
          <Route
            path={ROUTES.MARKETPLACE.ORDER_DETAIL.slice(1)}
            element={
              <ProtectedRoute>
                <OrderDetailPage />
              </ProtectedRoute>
            }
          />
          <Route
            path={ROUTES.SELLER.PORTAL.slice(1)}
            element={
              <ProtectedRoute>
                <SellerPortalPage />
              </ProtectedRoute>
            }
          />
          <Route
            path={`${ROUTES.SELLER.PORTAL.slice(1)}/*`}
            element={
              <ProtectedRoute>
                <SellerPortalPage />
              </ProtectedRoute>
            }
          />

          {/* Protected Admin Routes */}
          <Route
            path={ROUTES.ADMIN.DASHBOARD.slice(1)}
            element={
              <AdminRoute>
                <AdminDashboardPage />
              </AdminRoute>
            }
          />
          <Route
            path={`${ROUTES.ADMIN.DASHBOARD.slice(1)}/*`}
            element={
              <AdminRoute>
                <AdminDashboardPage />
              </AdminRoute>
            }
          />

          {/* 404 Route */}
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </Suspense>
  );
};
