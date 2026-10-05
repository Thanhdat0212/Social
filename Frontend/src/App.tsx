import React, { useEffect } from 'react';
import { BrowserRouter } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/api/authApi';
import { profileApi } from '@/api/profileApi';
import { AppRoutes } from '@/routes/AppRoutes';
import { ErrorBoundary } from '@/components/feedback/ErrorBoundary';
import { signalrService } from '@/services/signalrService';

export const App: React.FC = () => {
  const { setAuth, clearAuth, setInitializing } = useAuthStore();

  useEffect(() => {
    // Khôi phục phiên làm việc im lặng (Silent Refresh) khi reload trang
    const restoreSession = async () => {
      try {
        const refreshData = await authApi.refresh();
        if (refreshData?.accessToken) {
          // Gán tạm token để request profile đính kèm Authorization header
          useAuthStore.getState().setAccessToken(refreshData.accessToken);
          const profile = await profileApi.getMyProfile();
          setAuth(refreshData.accessToken, {
            id: profile.id,
            email: profile.email,
            displayName: profile.displayName,
            bio: profile.bio,
            avatarUrl: profile.avatarUrl,
            emailConfirmed: profile.emailConfirmed,
          });
        } else {
          clearAuth();
        }
      } catch {
        // Không có cookie refresh token hoặc token hết hạn
        clearAuth();
      } finally {
        setInitializing(false);
      }
    };

    restoreSession();
  }, [setAuth, clearAuth, setInitializing]);

  const { isInitializing, accessToken } = useAuthStore();

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
