import axios, { AxiosError, isAxiosError } from 'axios';
import { useAuthStore } from '@/store/authStore';

export const apiClient = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

// Request interceptor: Gắn Access Token từ Zustand store
apiClient.interceptors.request.use(
  (config) => {
    const accessToken = useAuthStore.getState().accessToken;
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
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
          '/api/auth/refresh',
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

/**
 * Trích xuất thông báo lỗi thân thiện cho ASP.NET Core Web API (ProblemDetails & ValidationProblemDetails)
 */
export function getApiErrorMessage(error: unknown): string {
  if (isAxiosError(error)) {
    return getAxiosErrorMessage(error);
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Đã có lỗi không xác định xảy ra. Vui lòng thử lại.';
}

function getAxiosErrorMessage(error: AxiosError): string {
  const data = error.response?.data as Record<string, unknown> | undefined;

  if (data && typeof data === 'object') {
    // 1. Kiểm tra validation errors từ FluentValidation / ASP.NET ValidationProblemDetails
    if (data.errors && typeof data.errors === 'object') {
      if (Array.isArray(data.errors) && data.errors.length > 0) {
        const first = data.errors[0];
        if (typeof first === 'string') return first;
        if (typeof first === 'object' && first !== null) {
          const obj = first as Record<string, unknown>;
          return (obj.errorMessage ?? obj.message ?? JSON.stringify(first)) as string;
        }
      } else {
        const values = Object.values(data.errors);
        if (values.length > 0 && Array.isArray(values[0]) && values[0].length > 0) {
          return String(values[0][0]);
        }
      }
    }

    // 2. ProblemDetails: detail hoặc title hoặc message
    if (typeof data.detail === 'string' && data.detail.trim()) {
      return data.detail;
    }

    if (typeof data.message === 'string' && data.message.trim()) {
      return data.message;
    }

    if (typeof data.title === 'string' && data.title.trim() && data.title !== 'One or more validation errors occurred.') {
      return data.title;
    }
  }

  // 3. Status-based mapping
  if (error.response?.status === 401) {
    return 'Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.';
  }

  if (error.response?.status === 403) {
    return 'Bạn không có quyền thực hiện hành động này.';
  }

  if (error.response?.status === 404) {
    return 'Không tìm thấy tài nguyên yêu cầu.';
  }

  if (error.response?.status === 500) {
    return 'Lỗi hệ thống máy chủ. Vui lòng thử lại sau.';
  }

  if (error.message) {
    const msg = error.message.toLowerCase();
    if (msg.includes('network error')) {
      return 'Lỗi kết nối mạng hoặc máy chủ không phản hồi. Vui lòng kiểm tra lại.';
    }
    if (msg.includes('timeout')) {
      return 'Yêu cầu quá thời gian chờ (timeout). Vui lòng thử lại.';
    }
    return error.message;
  }

  return 'Không thể kết nối máy chủ. Vui lòng kiểm tra lại.';
}
