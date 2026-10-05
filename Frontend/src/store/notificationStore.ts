import { create } from 'zustand';
import type { UserNotificationEvent } from '@/types';

interface NotificationState {
  notifications: UserNotificationEvent[];
  unreadCount: number;
  activeToast: UserNotificationEvent | null;

  addNotification: (notification: UserNotificationEvent) => void;
  showToast: (message: string, type?: string) => void;
  markAllAsRead: () => void;
  clearAll: () => void;
  clearActiveToast: () => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
  notifications: [],
  unreadCount: 0,
  activeToast: null,

  addNotification: (notification) =>
    set((state) => ({
      notifications: [notification, ...state.notifications].slice(0, 30),
      unreadCount: state.unreadCount + 1,
      activeToast: notification,
    })),

  showToast: (message: string, type: string = 'info') =>
    set({
      activeToast: {
        id: `toast-${Date.now()}`,
        type,
        message,
        createdAtUtc: new Date().toISOString(),
      },
    }),

  markAllAsRead: () =>
    set({
      unreadCount: 0,
    }),

  clearAll: () =>
    set({
      notifications: [],
      unreadCount: 0,
    }),

  clearActiveToast: () =>
    set({
      activeToast: null,
    }),
}));
