import { apiClient } from './client';
import type {
  RecommendationProductCard,
  ProductEventCreateDto,
  SearchHistoryItem,
  RecordSearchQueryRequest,
} from '@/types/marketplaceRecommendations';

export const marketplaceRecommendationApi = {
  /**
   * Lấy danh sách sản phẩm đề xuất "Dành riêng cho bạn" trên trang chủ Marketplace
   */
  getForYouProducts: async (limit: number = 12): Promise<RecommendationProductCard[]> => {
    const response = await apiClient.get<RecommendationProductCard[]>(
      '/marketplace/recommendations/for-you',
      { params: { limit } }
    );
    return response.data;
  },

  /**
   * Lấy danh sách sản phẩm tương tự trên trang chi tiết sản phẩm
   */
  getRelatedProducts: async (productId: string, limit: number = 8): Promise<RecommendationProductCard[]> => {
    const response = await apiClient.get<RecommendationProductCard[]>(
      `/marketplace/recommendations/products/${productId}/related`,
      { params: { limit } }
    );
    return response.data;
  },

  /**
   * Lấy danh sách sản phẩm thường được mua kèm (Frequently Bought Together)
   */
  getBoughtTogetherProducts: async (productId: string, limit: number = 4): Promise<RecommendationProductCard[]> => {
    const response = await apiClient.get<RecommendationProductCard[]>(
      `/marketplace/recommendations/products/${productId}/bought-together`,
      { params: { limit } }
    );
    return response.data;
  },

  /**
   * Lấy danh sách sản phẩm thịnh hành (Trending)
   */
  getTrendingProducts: async (categoryId?: string, limit: number = 8): Promise<RecommendationProductCard[]> => {
    const response = await apiClient.get<RecommendationProductCard[]>(
      '/marketplace/recommendations/trending',
      { params: { categoryId, limit } }
    );
    return response.data;
  },

  /**
   * Bắn sự kiện tương tác sản phẩm (View, DwellTime, Wishlist, AddToCart, v.v.)
   */
  trackEvent: async (payload: ProductEventCreateDto): Promise<void> => {
    try {
      await apiClient.post('/marketplace/events', payload);
    } catch {
      // Fire-and-forget: Lỗi tracking không làm gián đoạn trải nghiệm người dùng
    }
  },

  /**
   * Lưu từ khóa tìm kiếm của người dùng
   */
  recordSearch: async (request: RecordSearchQueryRequest): Promise<void> => {
    try {
      await apiClient.post('/marketplace/search-history', request);
    } catch {
      // Bỏ qua lỗi fire-and-forget
    }
  },

  /**
   * Lấy lịch sử tìm kiếm gần đây
   */
  getRecentSearches: async (limit: number = 10): Promise<SearchHistoryItem[]> => {
    const response = await apiClient.get<SearchHistoryItem[]>('/marketplace/search-history/recent', {
      params: { limit },
    });
    return response.data;
  },
};
