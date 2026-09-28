/**
 * Module quản lý và kiểm chuẩn tập trung các biến môi trường cho Frontend
 */

export const ENV = {
  /**
   * Base URL dùng cho các API request (mặc định '/api' để đi qua Vite proxy / Vercel rewrite)
   */
  API_BASE_URL: import.meta.env.VITE_API_BASE_URL || '/api',

  /**
   * URL trực tiếp tới Backend ASP.NET Core (chủ yếu dùng cho Vite Proxy hoặc Fallback)
   */
  BACKEND_URL: import.meta.env.VITE_BACKEND_URL || 'http://localhost:5126',

  /**
   * Google OAuth 2.0 Client ID dùng cho Google Login
   */
  GOOGLE_CLIENT_ID: import.meta.env.VITE_GOOGLE_CLIENT_ID || '',

  /**
   * Tên hiển thị của ứng dụng
   */
  APP_NAME: import.meta.env.VITE_APP_NAME || 'Social Platform',

  /**
   * Môi trường thực thi
   */
  APP_ENV: import.meta.env.VITE_APP_ENV || (import.meta.env.DEV ? 'development' : 'production'),

  /**
   * Trợ giúp kiểm tra môi trường
   */
  IS_DEV: import.meta.env.DEV,
  IS_PROD: import.meta.env.PROD,
} as const;

// Cảnh báo trong chế độ phát triển nếu thiếu các cấu hình thiết yếu
if (ENV.IS_DEV) {
  if (!ENV.GOOGLE_CLIENT_ID) {
    console.warn(
      '[Config Warning] VITE_GOOGLE_CLIENT_ID chưa được thiết lập trong .env. Tính năng đăng nhập Google có thể không hoạt động.'
    );
  }
}
