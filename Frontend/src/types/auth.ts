export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
  emailConfirmed: boolean;
}

export interface LoginResponseDto {
  accessToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface RefreshResponseDto {
  accessToken: string;
  expiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  displayName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface GoogleLoginRequest {
  idToken: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  userId: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ResendConfirmationRequest {
  email: string;
}
