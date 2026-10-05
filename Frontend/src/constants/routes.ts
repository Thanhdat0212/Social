/**
 * Hằng số định tuyến tập trung của ứng dụng Frontend
 * Giúp tránh magic strings và dễ dàng thay đổi đường dẫn khi mở rộng
 */
export const ROUTES = {
  HOME: '/',
  AUTH: {
    LOGIN: '/login',
    REGISTER: '/register',
    VERIFY_EMAIL: '/verify-email',
    FORGOT_PASSWORD: '/forgot-password',
    RESET_PASSWORD: '/reset-password',
  },
  PROFILE: '/profile',
  ONBOARDING: '/onboarding',
  POST_DETAIL: '/posts/:id',
} as const;
