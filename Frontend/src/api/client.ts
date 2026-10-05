import axios from 'axios';
import { useAuthStore } from '@/store/authStore';
import { ENV } from '@/config';
import { getApiErrorMessage } from '@/utils/error';
import { seenPostsService } from '@/utils/seenPosts';

export { getApiErrorMessage };

export const apiClient = axios.create({
  baseURL: ENV.API_BASE_URL,
  timeout: 60000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

// Request interceptor: Gắn Access Token từ Zustand store và danh sách bài đã xem
apiClient.interceptors.request.use(
  async (config) => {
    // Không chờ đối với các endpoint auth cơ bản để tránh deadlock
    const isAuthRoute =
      config.url?.includes('/auth/login') ||
      config.url?.includes('/auth/register') ||
      config.url?.includes('/auth/refresh') ||
      config.url?.includes('/auth/forgot-password') ||
      config.url?.includes('/auth/reset-password');

    // Nếu đang trong quá trình khôi phục phiên đăng nhập (F5/Reload trang),
    // tạm dừng request cho đến khi xác thực xong để đảm bảo luôn có Authorization header
    if (!isAuthRoute && useAuthStore.getState().isInitializing) {
      await new Promise<void>((resolve) => {
        if (!useAuthStore.getState().isInitializing) {
          resolve();
          return;
        }
        let timer: ReturnType<typeof setTimeout>;
        const unsubscribe = useAuthStore.subscribe((state) => {
          if (!state.isInitializing) {
            clearTimeout(timer);
            unsubscribe();
            resolve();
          }
        });
        timer = setTimeout(() => {
          unsubscribe();
          resolve();
        }, 5000);
      });
    }

    const accessToken = useAuthStore.getState().accessToken;
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }

    // Tự động gắn danh sách bài viết đã xem gần đây vào header đối với các request lấy bảng tin
    if (config.method === 'get' && config.url && config.url.includes('/posts')) {
      const recentSeen = seenPostsService.getRecentSeenIds(60);
      if (recentSeen.length > 0) {
        config.headers['X-Seen-Post-Ids'] = recentSeen.join(',');
      }
    }

    return config;
  },
  (error) => {
    console.error('[API Request Error]', error);
    return Promise.reject(error);
  }
);

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string | null) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Response interceptor: Bắt 401 để kích hoạt Refresh Token im lặng
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Không thử refresh nếu là các API auth cơ bản hoặc request đã retry
    const isAuthRoute =
      originalRequest?.url?.includes('/auth/login') ||
      originalRequest?.url?.includes('/auth/register') ||
      originalRequest?.url?.includes('/auth/refresh') ||
      originalRequest?.url?.includes('/auth/forgot-password') ||
      originalRequest?.url?.includes('/auth/reset-password');

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isAuthRoute) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((token) => {
          if (token) {
            originalRequest.headers.Authorization = `Bearer ${token}`;
          }
          return apiClient(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Refresh token được trình duyệt tự gửi qua cookie httpOnly
        const refreshResponse = await axios.post<{ accessToken: string; expiresAt: string }>(
          `${ENV.API_BASE_URL}/auth/refresh`,
          {},
          { withCredentials: true }
        );

        const newAccessToken = refreshResponse.data.accessToken;
        useAuthStore.getState().setAccessToken(newAccessToken);

        processQueue(null, newAccessToken);
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        return apiClient(originalRequest);
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        useAuthStore.getState().clearAuth();
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
