import { apiClient } from './client';
import type {
  ForgotPasswordRequest,
  GoogleLoginRequest,
  LoginRequest,
  LoginResponseDto,
  RefreshResponseDto,
  RegisterRequest,
  ResendConfirmationRequest,
  ResetPasswordRequest,
} from '@/types/auth';

export const authApi = {
  register: async (data: RegisterRequest) => {
    const response = await apiClient.post<{ message: string }>('/auth/register', data);
    return response.data;
  },

  confirmEmail: async (userId: string, token: string) => {
    const response = await apiClient.get<{ message: string }>('/auth/confirm-email', {
      params: { userId, token },
    });
    return response.data;
  },

  resendConfirmation: async (data: ResendConfirmationRequest) => {
    const response = await apiClient.post<{ message: string }>('/auth/resend-confirmation', data);
    return response.data;
  },

  login: async (data: LoginRequest) => {
    const response = await apiClient.post<LoginResponseDto>('/auth/login', data);
    return response.data;
  },

  googleLogin: async (data: GoogleLoginRequest) => {
    const response = await apiClient.post<LoginResponseDto>('/auth/google', data);
    return response.data;
  },

  refresh: async () => {
    const response = await apiClient.post<RefreshResponseDto>('/auth/refresh');
    return response.data;
  },

  logout: async () => {
    const response = await apiClient.post('/auth/logout');
    return response.data;
  },

  forgotPassword: async (data: ForgotPasswordRequest) => {
    const response = await apiClient.post<{ message: string }>('/auth/forgot-password', data);
    return response.data;
  },

  resetPassword: async (data: ResetPasswordRequest) => {
    const response = await apiClient.post<{ message: string }>('/auth/reset-password', data);
    return response.data;
  },
};
