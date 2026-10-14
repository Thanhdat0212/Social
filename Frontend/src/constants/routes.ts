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
  MARKETPLACE: {
    INDEX: '/marketplace',
    PRODUCT_DETAIL: '/marketplace/products/:slug',
    SHOP_PROFILE: '/marketplace/shops/:slug',
    CART: '/cart',
    CHECKOUT: '/checkout/:id',
    ORDERS: '/orders',
    ORDER_DETAIL: '/orders/:id',
  },
  SELLER: {
    PORTAL: '/seller',
    PRODUCTS: '/seller/products',
    INVENTORY: '/seller/inventory',
    ORDERS: '/seller/orders',
    FINANCE: '/seller/finance',
    COMPLAINTS: '/seller/complaints',
  },
  ADMIN: {
    DASHBOARD: '/admin',
    SHOPS: '/admin/shops',
    PAYOUTS: '/admin/payouts',
    COMPLAINTS: '/admin/complaints',
    REPORTS: '/admin/reports',
    AUDIT: '/admin/audit',
    USERS: '/admin/users',
  },
} as const;

