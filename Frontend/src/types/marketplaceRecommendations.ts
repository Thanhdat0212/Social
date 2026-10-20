export type ProductEventType = 'View' | 'SearchClick' | 'Wishlist' | 'AddToCart' | 'Purchase';

export interface RecommendationProductCard {
  id: string;
  name: string;
  slug: string;
  shopId: string;
  shopName: string;
  shopSlug?: string;
  categoryId: string;
  categoryName: string;
  coverImageUrl?: string;
  minPrice: number;
  maxPrice: number;
  ratingAverage: number;
  reviewCount: number;
  createdAtUtc: string;
  recommendationReason?: string;
  recommendationScore: number;
  strategy: string;
}

export interface ProductEventCreateDto {
  productId: string;
  eventType: ProductEventType;
  dwellTimeSeconds?: number;
  sessionId?: string;
}

export interface SearchHistoryItem {
  id: string;
  queryText: string;
  resultCount: number;
  createdAtUtc: string;
}

export interface RecordSearchQueryRequest {
  queryText: string;
  resultCount?: number;
  sessionId?: string;
}
