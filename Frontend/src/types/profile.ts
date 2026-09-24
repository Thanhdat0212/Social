export interface ProfileDto {
  id: string;
  email: string;
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
  emailConfirmed: boolean;
  createdAtUtc: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  bio?: string | null;
}

export interface AvatarUploadResponseDto {
  avatarUrl: string;
}
