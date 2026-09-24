import { apiClient } from './client';
import type { AvatarUploadResponseDto, ProfileDto, UpdateProfileRequest } from '@/types/profile';

export const profileApi = {
  getMyProfile: async () => {
    const response = await apiClient.get<ProfileDto>('/profile/me');
    return response.data;
  },

  updateProfile: async (data: UpdateProfileRequest) => {
    const response = await apiClient.put<ProfileDto>('/profile/me', data);
    return response.data;
  },

  uploadAvatar: async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await apiClient.post<AvatarUploadResponseDto>('/profile/me/avatar', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};
