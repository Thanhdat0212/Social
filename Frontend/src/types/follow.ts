export interface FollowToggleResponseDto {
  targetUserId: string;
  isFollowing: boolean;
  followersCount: number;
}

export interface UserFollowDto {
  id: string;
  displayName: string;
  bio?: string | null;
  avatarUrl?: string | null;
  followedAtUtc: string;
}

export interface FollowStatsDto {
  userId: string;
  followersCount: number;
  followingCount: number;
  isFollowingByCurrentUser: boolean;
}
