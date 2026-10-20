import React, { useEffect, useState } from 'react';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';
import type { RecommendationProductCard as IRecommendationProductCard } from '@/types/marketplaceRecommendations';
import { RecommendationProductCard } from './RecommendationProductCard';

interface Props {
  className?: string;
  limit?: number;
  pageSize?: number;
}

export const ForYouSection: React.FC<Props> = ({
  className = '',
  limit = 18,
  pageSize = 12,
}) => {
  const [products, setProducts] = useState<IRecommendationProductCard[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [currentPage, setCurrentPage] = useState<number>(1);

  useEffect(() => {
    let isMounted = true;
    const fetchRecommendations = async () => {
      try {
        setLoading(true);
        setCurrentPage(1);
        const data = await marketplaceRecommendationApi.getForYouProducts(limit);
        if (isMounted) {
          setProducts(data || []);
        }
      } catch (err) {
        console.error('Không thể nạp danh sách đề xuất cá nhân hóa:', err);
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchRecommendations();

    return () => {
      isMounted = false;
    };
  }, [limit]);

  if (!loading && products.length === 0) {
    return null;
  }

  const totalPages = Math.ceil(products.length / pageSize) || 1;
  const currentProducts = products.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  return (
    <section className={`marketplace-recommendations-section ${className}`.trim()}>
      <div className="recommendations-header">
        <div>
          <h2 className="recommendations-header-title">
            <span>✨</span>
            <span>Dành riêng cho bạn</span>
            <span
              style={{
                fontSize: '0.7rem',
                fontWeight: 700,
                padding: '0.15rem 0.5rem',
                borderRadius: '999px',
                background: 'linear-gradient(135deg, #8b5cf6, #6366f1)',
                color: '#fff',
                marginLeft: '0.35rem',
              }}
            >
              AI Phân Tích
            </span>
          </h2>
          <p className="recommendations-header-subtitle">
            Đề xuất dựa trên sở thích và hành vi tương tác của bạn
          </p>
        </div>

        {totalPages > 1 && (
          <div className="recommendations-controls">
            <span className="recommendations-page-indicator">
              Trang {currentPage} / {totalPages}
            </span>
            <button
              type="button"
              className="recommendations-nav-btn"
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage <= 1}
              aria-label="Trang trước"
              title="Trang trước"
            >
              ◀
            </button>
            <button
              type="button"
              className="recommendations-nav-btn"
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage >= totalPages}
              aria-label="Trang sau"
              title="Trang sau"
            >
              ▶
            </button>
          </div>
        )}
      </div>

      {loading ? (
        <div className="recommendations-grid">
          {Array.from({ length: 6 }).map((_, idx) => (
            <div key={idx} className="recommendation-skeleton-card animate-pulse">
              <div className="recommendation-skeleton-thumb" />
              <div className="recommendation-skeleton-body">
                <div className="recommendation-skeleton-line" style={{ height: '10px', width: '40%' }} />
                <div className="recommendation-skeleton-line" style={{ height: '14px', width: '80%' }} />
                <div className="recommendation-skeleton-line" style={{ height: '14px', width: '50%', marginTop: '6px' }} />
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="recommendations-grid">
          {currentProducts.map((prod) => (
            <RecommendationProductCard key={prod.id} product={prod} />
          ))}
        </div>
      )}
    </section>
  );
};
