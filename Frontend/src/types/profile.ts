export interface ProfileDto {
  id: string;
  email: string;
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
  emailConfirmed: boolean;
  role?: string;
  status?: string;
  createdAtUtc: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  bio?: string | null;
}

export interface AvatarUploadResponseDto {
  avatarUrl: string;
}
