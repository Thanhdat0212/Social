import { useEffect, useRef, useCallback } from 'react';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';
import type { ProductEventType } from '@/types/marketplaceRecommendations';

/**
 * Custom Hook theo dõi thời gian nán lại (Dwell Time) trên trang chi tiết sản phẩm
 * Tự động gửi sự kiện 'View' khi người dùng xem sản phẩm >= 3 giây
 * Chống trùng lặp (Anti-spam) bằng sessionStorage trong cùng phiên làm việc
 */
export function useProductDwellTracker(productId?: string) {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const startTimeRef = useRef<number>(0);
  const hasTrackedViewRef = useRef<boolean>(false);

  useEffect(() => {
    if (!productId) return;

    startTimeRef.current = Date.now();
    hasTrackedViewRef.current = false;

    // Kiểm tra xem sản phẩm đã được tính view trong phiên tab này chưa
    const storageKey = `mkt_view_${productId}`;
    const alreadyTracked = sessionStorage.getItem(storageKey);

    if (!alreadyTracked) {
      // Đặt hẹn giờ 3 giây. Nếu người dùng ở lại >= 3s, gửi tín hiệu View
      timerRef.current = setTimeout(() => {
        marketplaceRecommendationApi.trackEvent({
          productId,
          eventType: 'View',
          dwellTimeSeconds: 3,
        });
        hasTrackedViewRef.current = true;
        sessionStorage.setItem(storageKey, 'true');
      }, 3000);
    }

    return () => {
      // Hủy timer nếu người dùng rời trang trước 3 giây
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }
    };
  }, [productId]);

  /**
   * Bắn sự kiện hành động cụ thể (AddToCart, Wishlist, SearchClick, v.v.)
   */
  const trackAction = useCallback(
    (eventType: ProductEventType) => {
      if (!productId) return;
      marketplaceRecommendationApi.trackEvent({
        productId,
        eventType,
      });
    },
    [productId]
  );

  return { trackAction };
}
