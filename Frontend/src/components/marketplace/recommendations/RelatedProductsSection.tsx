import React, { useEffect, useState } from 'react';
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
  limit = 6,
}) => {
  const [products, setProducts] = useState<IRecommendationProductCard[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    let isMounted = true;
    const fetchRelated = async () => {
      try {
        setLoading(true);
        const data = await marketplaceRecommendationApi.getRelatedProducts(productId, limit);
        if (isMounted) {
          setProducts(data);
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

  if (!loading && products.length === 0) {
    return null;
  }

  return (
    <section className={`marketplace-related-section ${className}`.trim()}>
      <div className="flex items-center justify-between mb-4">
        <div>
          <h2 className="text-lg font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
            🔍 Sản phẩm tương tự bạn có thể thích
          </h2>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Cùng danh mục và mức giá tương đương
          </p>
        </div>
      </div>

      {loading ? (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
          {Array.from({ length: 4 }).map((_, idx) => (
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
