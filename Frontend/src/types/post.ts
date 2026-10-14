export interface PostAuthorDto {
  id: string;
  displayName: string;
  avatarUrl?: string | null;
}

export interface PostTopicDto {
  id: string;
  name: string;
  slug: string;
  icon?: string | null;
  confidence: number;
}

export interface PostTaggedProductDto {
  id: string;
  name: string;
  slug: string;
  minPrice: number;
  maxPrice: number;
  coverImageUrl?: string | null;
  shopId: string;
  shopName: string;
  shopSlug: string;
}

export interface PostDto {
  id: string;
  content: string;
  mediaUrls: string[];
  status: string;
  createdAtUtc: string;
  likeCount: number;
  commentCount: number;
  viewCount: number;
  isLikedByCurrentUser: boolean;
  author: PostAuthorDto;
  topics: PostTopicDto[];
  taggedProducts?: PostTaggedProductDto[];
}

export interface CreatePostRequestDto {
  content: string;
  mediaUrls?: string[];
  taggedProductIds?: string[];
}

export interface PostMediaUploadResponseDto {
  url: string;
  publicId: string;
}
