import { apiClient } from './client';
import type {
  InterestDto,
  OnboardingStatusDto,
  SelectInterestsRequest,
  UserPreferenceDto,
} from '@/types/interest';

export const interestApi = {
  /**
   * Lấy toàn bộ danh sách sở thích đang hoạt động trên hệ thống
   */
  getAllInterests: async () => {
    const response = await apiClient.get<InterestDto[]>('/interests');
    return response.data;
  },

  /**
   * Lấy danh sách sở thích của người dùng hiện tại
   */
  getMyInterests: async () => {
    const response = await apiClient.get<InterestDto[]>('/users/me/interests');
    return response.data;
  },

  /**
   * Chọn sở thích ban đầu hoặc cập nhật sở thích (Onboarding - tối thiểu 3)
   */
  selectMyInterests: async (data: SelectInterestsRequest) => {
    const response = await apiClient.post<{ message: string }>('/users/me/interests', data);
    return response.data;
  },

  /**
   * Lấy hồ sơ trọng số sở thích thuật toán của người dùng
   */
  getMyPreferences: async () => {
    const response = await apiClient.get<UserPreferenceDto[]>('/users/me/preferences');
    return response.data;
  },

  /**
   * Kiểm tra người dùng đã hoàn thành onboarding sở thích chưa
   */
  getOnboardingStatus: async () => {
    const response = await apiClient.get<OnboardingStatusDto>('/users/me/onboarding-status');
    return response.data;
  },
};
