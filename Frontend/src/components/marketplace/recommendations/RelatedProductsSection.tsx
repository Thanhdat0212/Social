import React, { useEffect, useRef, useState, useCallback } from 'react';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';
import type { RecommendationProductCard as IRecommendationProductCard } from '@/types/marketplaceRecommendations';
import { RecommendationProductCard } from './RecommendationProductCard';

interface Props {
  productId: string;
  className?: string;
  limit?: number;
}

export const RelatedProductsSection: React.FC<Props> = ({
  productId,
  className = '',
  limit = 12,
}) => {
  const [products, setProducts] = useState<IRecommendationProductCard[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [canScrollLeft, setCanScrollLeft] = useState<boolean>(false);
  const [canScrollRight, setCanScrollRight] = useState<boolean>(false);

  const carouselRef = useRef<HTMLDivElement>(null);

  const updateScrollButtons = useCallback(() => {
    const el = carouselRef.current;
    if (!el) return;
    const { scrollLeft, scrollWidth, clientWidth } = el;
    setCanScrollLeft(scrollLeft > 8);
    setCanScrollRight(scrollLeft + clientWidth < scrollWidth - 8);
  }, []);

  useEffect(() => {
    let isMounted = true;
    const fetchRelated = async () => {
      try {
        setLoading(true);
        const data = await marketplaceRecommendationApi.getRelatedProducts(productId, limit);
        if (isMounted) {
          setProducts(data || []);
        }
      } catch (err) {
        console.error('Không thể nạp danh sách sản phẩm tương tự:', err);
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    if (productId) {
      fetchRelated();
    }

    return () => {
      isMounted = false;
    };
  }, [productId, limit]);

  // Cập nhật trạng thái các nút cuộn khi danh sách sản phẩm thay đổi
  useEffect(() => {
    const el = carouselRef.current;
    if (!el) return;

    // Kiểm tra ngay sau khi render DOM
    const timer = setTimeout(updateScrollButtons, 100);

    el.addEventListener('scroll', updateScrollButtons, { passive: true });
    window.addEventListener('resize', updateScrollButtons);

    return () => {
      clearTimeout(timer);
      el.removeEventListener('scroll', updateScrollButtons);
      window.removeEventListener('resize', updateScrollButtons);
    };
  }, [products, loading, updateScrollButtons]);

  const handleScroll = (direction: 'left' | 'right') => {
    const el = carouselRef.current;
    if (!el) return;

    // Cuộn khoảng 80% bề rộng hiển thị của container
    const scrollAmount = Math.max(el.clientWidth * 0.75, 260);
    el.scrollBy({
      left: direction === 'left' ? -scrollAmount : scrollAmount,
      behavior: 'smooth',
    });
  };

  if (!loading && products.length === 0) {
    return null;
  }

  return (
    <section className={`marketplace-recommendations-section ${className}`.trim()}>
      {/* Tiêu đề & Mô tả section */}
      <div className="recommendations-header">
        <div>
          <h2 className="recommendations-header-title">
            <span>🔍</span>
            <span>Sản phẩm tương tự bạn có thể thích</span>
          </h2>
          <p className="recommendations-header-subtitle">
            Cùng danh mục và mức giá tương đương, sắp xếp theo độ phù hợp
          </p>
        </div>

        {/* Thông tin số lượng sản phẩm */}
        {!loading && products.length > 0 && (
          <span className="recommendations-count-badge">
            {products.length} gợi ý
          </span>
        )}
      </div>

      {/* Carousel Container với 2 nút mũi tên tròn ở hai bên mép */}
      <div className="recommendations-carousel-wrapper">
        {/* Nút lướt sang trái */}
        {canScrollLeft && (
          <button
            type="button"
            className="carousel-nav-btn carousel-nav-prev"
            onClick={() => handleScroll('left')}
            aria-label="Xem sản phẩm trước"
            title="Xem các sản phẩm trước"
          >
            ‹
          </button>
        )}

        {/* Nút lướt sang phải */}
        {canScrollRight && (
          <button
            type="button"
            className="carousel-nav-btn carousel-nav-next"
            onClick={() => handleScroll('right')}
            aria-label="Xem sản phẩm tiếp theo"
            title="Xem các sản phẩm tiếp theo"
          >
            ›
          </button>
        )}

        {/* Track cuộn ngang mượt mà */}
        {loading ? (
          <div className="recommendations-carousel-track" ref={carouselRef}>
            {Array.from({ length: 6 }).map((_, idx) => (
              <div key={idx} className="recommendation-skeleton-card carousel-card-skeleton animate-pulse">
                <div className="recommendation-skeleton-thumb" />
                <div className="recommendation-skeleton-body">
                  <div className="recommendation-skeleton-line" style={{ height: '10px', width: '40%' }} />
                  <div className="recommendation-skeleton-line" style={{ height: '14px', width: '85%' }} />
                  <div className="recommendation-skeleton-line" style={{ height: '14px', width: '60%' }} />
                  <div className="recommendation-skeleton-line" style={{ height: '12px', width: '50%', marginTop: '4px' }} />
                  <div className="recommendation-skeleton-line" style={{ height: '16px', width: '45%', marginTop: 'auto' }} />
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div
            className="recommendations-carousel-track"
            ref={carouselRef}
            tabIndex={0}
            role="region"
            aria-label="Danh sách sản phẩm tương tự"
          >
            {products.map((prod, index) => (
              <RecommendationProductCard
                key={prod.id}
                product={prod}
                isTopMatch={index === 0}
                className="carousel-product-item"
              />
            ))}
          </div>
        )}
      </div>
    </section>
  );
};
