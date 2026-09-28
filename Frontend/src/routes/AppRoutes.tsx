import React from 'react';
import { Route, Routes } from 'react-router-dom';
import { Layout } from '@/components/layout/Layout';
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { GuestRoute } from '@/components/auth/GuestRoute';
import { ROUTES } from '@/constants/routes';

// Import Pages
import { HomePage } from '@/pages/home/HomePage';
import { LoginPage } from '@/pages/auth/LoginPage';
import { RegisterPage } from '@/pages/auth/RegisterPage';
import { VerifyEmailPage } from '@/pages/auth/VerifyEmailPage';
import { ForgotPasswordPage } from '@/pages/auth/ForgotPasswordPage';
import { ResetPasswordPage } from '@/pages/auth/ResetPasswordPage';
import { ProfilePage } from '@/pages/profile/ProfilePage';
import { OnboardingPage } from '@/pages/onboarding/OnboardingPage';
import { NotFoundPage } from '@/pages/not-found/NotFoundPage';

export const AppRoutes: React.FC = () => {
  return (
    <Routes>
      <Route path={ROUTES.HOME} element={<Layout />}>
        {/* Public Routes */}
        <Route index element={<HomePage />} />
        <Route path={ROUTES.AUTH.VERIFY_EMAIL.slice(1)} element={<VerifyEmailPage />} />

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

        {/* 404 Route */}
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
};
