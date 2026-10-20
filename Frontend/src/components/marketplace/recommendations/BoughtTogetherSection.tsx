import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';
import type { RecommendationProductCard as IRecommendationProductCard } from '@/types/marketplaceRecommendations';

interface Props {
  productId: string;
  className?: string;
  limit?: number;
}

export const BoughtTogetherSection: React.FC<Props> = ({
  productId,
  className = '',
  limit = 3,
}) => {
  const [products, setProducts] = useState<IRecommendationProductCard[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    let isMounted = true;
    const fetchBoughtTogether = async () => {
      try {
        setLoading(true);
        const data = await marketplaceRecommendationApi.getBoughtTogetherProducts(productId, limit);
        if (isMounted) {
          setProducts(data);
        }
      } catch (err) {
        console.error('Không thể nạp danh sách sản phẩm mua cùng:', err);
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    if (productId) {
      fetchBoughtTogether();
    }

    return () => {
      isMounted = false;
    };
  }, [productId, limit]);

  if (!loading && products.length === 0) {
    return null;
  }

  const formatPrice = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const defaultThumb =
    'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600&auto=format&fit=crop&q=60';

  return (
    <section className={`marketplace-bought-together-section p-4 bg-purple-50/50 dark:bg-purple-950/20 border border-purple-200 dark:border-purple-800/40 rounded-xl ${className}`.trim()}>
      <div className="flex items-center gap-2 mb-3">
        <span className="text-xl">🤝</span>
        <div>
          <h2 className="text-base font-bold text-gray-900 dark:text-gray-100">
            Thường được mua cùng nhau (Frequently Bought Together)
          </h2>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Khách hàng mua sản phẩm này cũng thường sở hữu thêm các dịch vụ số bên dưới
          </p>
        </div>
      </div>

      {loading ? (
        <div className="flex gap-4">
          <div className="h-24 w-48 bg-gray-200 dark:bg-gray-700 animate-pulse rounded-lg" />
          <div className="h-24 w-48 bg-gray-200 dark:bg-gray-700 animate-pulse rounded-lg" />
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
          {products.map((item) => (
            <Link
              key={item.id}
              to={`/marketplace/products/${item.slug}`}
              className="flex items-center gap-3 p-2 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 hover:shadow-md transition-shadow group"
            >
              <img
                src={item.coverImageUrl || defaultThumb}
                alt={item.name}
                className="w-16 h-16 object-cover rounded-md flex-shrink-0"
                loading="lazy"
              />
              <div className="min-w-0 flex-1">
                <h4 className="text-xs font-semibold text-gray-800 dark:text-gray-200 truncate group-hover:text-purple-600 dark:group-hover:text-purple-400">
                  {item.name}
                </h4>
                <p className="text-xs text-purple-600 dark:text-purple-400 font-bold mt-1">
                  {formatPrice(item.minPrice)}
                </p>
                <span className="inline-block text-[10px] bg-purple-100 dark:bg-purple-900/40 text-purple-700 dark:text-purple-300 px-1.5 py-0.5 rounded mt-1">
                  {item.recommendationReason || 'Gợi ý mua kèm'}
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </section>
  );
};
