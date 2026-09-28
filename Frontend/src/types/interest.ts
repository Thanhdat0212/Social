export interface InterestDto {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  icon?: string | null;
  isSelected?: boolean;
}

export interface SelectInterestsRequest {
  interestIds: string[];
}

export interface UserPreferenceDto {
  interestId: string;
  interestName: string;
  interestSlug: string;
  icon?: string | null;
  score: number;
  updatedAtUtc: string;
}

export interface OnboardingStatusDto {
  isOnboarded: boolean;
  selectedCount: number;
}
