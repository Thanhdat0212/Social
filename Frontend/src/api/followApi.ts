import { apiClient } from './client';
import type {
  FollowStatsDto,
  FollowToggleResponseDto,
  UserFollowDto,
} from '@/types';

export const followApi = {
  /**
   * Theo dõi hoặc hủy theo dõi người dùng
   */
  toggleFollow: async (userId: string) => {
    const response = await apiClient.post<FollowToggleResponseDto>(`/users/${userId}/follow`);
    return response.data;
  },

  /**
   * Lấy thống kê Followers / Following của người dùng
   */
  getFollowStats: async (userId: string) => {
    const response = await apiClient.get<FollowStatsDto>(`/users/${userId}/follow-stats`);
    return response.data;
  },

  /**
   * Lấy danh sách người đang theo dõi
   */
  getFollowers: async (userId: string, page = 1, pageSize = 50) => {
    const response = await apiClient.get<UserFollowDto[]>(`/users/${userId}/followers`, {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Lấy danh sách người mà tài khoản này đang theo dõi
   */
  getFollowing: async (userId: string, page = 1, pageSize = 50) => {
    const response = await apiClient.get<UserFollowDto[]>(`/users/${userId}/following`, {
      params: { page, pageSize },
    });
    return response.data;
  },
};
