import { apiClient } from './client';

export interface SystemNotificationDto {
  id: string;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  readAtUtc?: string;
  dataJson?: string;
  createdAtUtc: string;
}

export const notificationApi = {
  getMyNotifications: async (page = 1, pageSize = 20): Promise<SystemNotificationDto[]> => {
    const res = await apiClient.get<SystemNotificationDto[]>('/notifications', {
      params: { page, pageSize },
    });
    return res.data;
  },

  getUnreadCount: async (): Promise<number> => {
    const res = await apiClient.get<{ unreadCount: number }>('/notifications/unread-count');
    return res.data.unreadCount;
  },

  markAsRead: async (id: string): Promise<void> => {
    await apiClient.patch(`/notifications/${id}/read`);
  },

  markAllAsRead: async (): Promise<void> => {
    await apiClient.patch('/notifications/read-all');
  },
};
