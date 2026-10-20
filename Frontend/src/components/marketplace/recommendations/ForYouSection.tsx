import React, { useEffect, useState } from 'react';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';
import type { RecommendationProductCard as IRecommendationProductCard } from '@/types/marketplaceRecommendations';
import { RecommendationProductCard } from './RecommendationProductCard';

interface Props {
  className?: string;
  limit?: number;
}

export const ForYouSection: React.FC<Props> = ({ className = '', limit = 12 }) => {
  const [products, setProducts] = useState<IRecommendationProductCard[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    let isMounted = true;
    const fetchRecommendations = async () => {
      try {
        setLoading(true);
        const data = await marketplaceRecommendationApi.getForYouProducts(limit);
        if (isMounted) {
          setProducts(data);
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

  return (
    <section className={`marketplace-recommendations-section ${className}`.trim()}>
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <span className="text-2xl" role="img" aria-label="sparkles">✨</span>
          <div>
            <h2 className="text-xl font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
              Dành riêng cho bạn
              <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-gradient-to-r from-purple-500 to-indigo-500 text-white shadow-sm">
                AI Phân Tích
              </span>
            </h2>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Đề xuất dựa trên sở thích mạng xã hội và nhu cầu tương tác của bạn
            </p>
          </div>
        </div>
      </div>

      {loading ? (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {Array.from({ length: 6 }).map((_, idx) => (
            <div key={idx} className="product-card animate-pulse">
              <div className="product-thumb-wrap bg-gray-200 dark:bg-gray-700" />
              <div className="product-card-body space-y-2">
                <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2" />
                <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4" />
                <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/3" />
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {products.map((prod) => (
            <RecommendationProductCard key={prod.id} product={prod} />
          ))}
        </div>
      )}
    </section>
  );
};
