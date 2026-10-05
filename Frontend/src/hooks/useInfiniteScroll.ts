import { useCallback, useRef } from 'react';

interface UseInfiniteScrollOptions {
  onLoadMore: () => void;
  hasMore: boolean;
  isLoading: boolean;
  rootMargin?: string;
  threshold?: number;
}

/**
 * Custom hook phát hiện người dùng lướt gần đáy trang để nạp thêm nội dung (Infinite Scroll)
 * Tương tự cơ chế của Threads và Instagram.
 */
export function useInfiniteScroll({
  onLoadMore,
  hasMore,
  isLoading,
  rootMargin = '400px', // Kích hoạt nạp trước khi người dùng chạm đáy 400px
  threshold = 0,
}: UseInfiniteScrollOptions) {
  const observer = useRef<IntersectionObserver | null>(null);

  const sentinelRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (isLoading) return;

      if (observer.current) {
        observer.current.disconnect();
      }

      if (!node || !hasMore) return;

      observer.current = new IntersectionObserver(
        (entries) => {
          const [entry] = entries;
          if (entry.isIntersecting && hasMore && !isLoading) {
            onLoadMore();
          }
        },
        {
          root: null, // viewport trình duyệt
          rootMargin,
          threshold,
        }
      );

      observer.current.observe(node);
    },
    [onLoadMore, hasMore, isLoading, rootMargin, threshold]
  );

  return { sentinelRef };
}
