import React, { useEffect } from 'react';
import axios from 'axios';
import { BrowserRouter } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/api/authApi';
import { profileApi } from '@/api/profileApi';
import { AppRoutes } from '@/routes/AppRoutes';
import { ErrorBoundary } from '@/components/feedback/ErrorBoundary';
import { signalrService } from '@/services/signalrService';

// Singleton promise để deduplicate quá trình khôi phục phiên khi F5 hoặc React StrictMode mount 2 lần
let restoreSessionPromise: Promise<void> | null = null;

export const App: React.FC = () => {
  const { setAuth, clearAuth, setInitializing, isInitializing, accessToken } = useAuthStore();

  useEffect(() => {
    let isMounted = true;

    // Khôi phục phiên làm việc im lặng (Silent Refresh) khi reload trang
    const restoreSession = async () => {
      if (!restoreSessionPromise) {
        restoreSessionPromise = (async () => {
          try {
            const refreshData = await authApi.refresh();
            if (refreshData?.accessToken) {
              // Gán tạm token để request profile đính kèm Authorization header
              useAuthStore.getState().setAccessToken(refreshData.accessToken);
              const profile = await profileApi.getMyProfile();
              if (isMounted) {
                setAuth(refreshData.accessToken, {
                  id: profile.id,
                  email: profile.email,
                  displayName: profile.displayName,
                  bio: profile.bio,
                  avatarUrl: profile.avatarUrl,
                  emailConfirmed: profile.emailConfirmed,
                  role: profile.role,
                  status: profile.status,
                });
              }
            } else if (isMounted) {
              clearAuth();
            }
          } catch (err: unknown) {
            // Nếu request bị trình duyệt hủy do người dùng reload tiếp (CanceledError) -> Không clearAuth
            if (
              axios.isCancel(err) ||
              (err as { name?: string })?.name === 'CanceledError' ||
              (err as { code?: string })?.code === 'ERR_CANCELED'
            ) {
              return;
            }
            // Không có cookie refresh token hoặc token thực sự hết hạn
            if (isMounted) {
              clearAuth();
            }
          } finally {
            if (isMounted) {
              setInitializing(false);
            }
            restoreSessionPromise = null;
          }
        })();
      }

      await restoreSessionPromise;
    };

    restoreSession();

    return () => {
      isMounted = false;
    };
  }, [setAuth, clearAuth, setInitializing]);

  useEffect(() => {
    if (!isInitializing) {
      signalrService.start();
    }
  }, [isInitializing, accessToken]);

  return (
    <ErrorBoundary>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </ErrorBoundary>
  );
};

export default App;
