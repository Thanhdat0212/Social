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
  limit = 4,
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
          setProducts(data || []);
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
    <section className={`marketplace-bought-together-section ${className}`.trim()}>
      <div className="bought-together-header">
        <span style={{ fontSize: '1.25rem' }}>🤝</span>
        <div>
          <h2 className="bought-together-title">
            Thường được mua cùng nhau (Frequently Bought Together)
          </h2>
          <p className="bought-together-subtitle">
            Khách hàng mua sản phẩm này cũng thường chọn các dịch vụ số bên dưới
          </p>
        </div>
      </div>

      {loading ? (
        <div className="bought-together-grid">
          {Array.from({ length: 3 }).map((_, idx) => (
            <div
              key={idx}
              className="bought-together-item animate-pulse"
              style={{ minHeight: '68px' }}
            >
              <div
                className="bought-together-thumb"
                style={{ background: 'var(--bg-surface)' }}
              />
              <div className="bought-together-info">
                <div
                  style={{
                    height: '12px',
                    width: '70%',
                    background: 'var(--bg-card)',
                    borderRadius: '4px',
                    marginBottom: '6px',
                  }}
                />
                <div
                  style={{
                    height: '12px',
                    width: '40%',
                    background: 'var(--bg-card)',
                    borderRadius: '4px',
                  }}
                />
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="bought-together-grid">
          {products.map((item) => (
            <Link
              key={item.id}
              to={`/marketplace/products/${item.slug}`}
              className="bought-together-item"
            >
              <img
                src={item.coverImageUrl || defaultThumb}
                alt={item.name}
                className="bought-together-thumb"
                loading="lazy"
                onError={(e) => {
                  const target = e.currentTarget;
                  if (!target.dataset.hasFallback) {
                    target.dataset.hasFallback = 'true';
                    target.src = defaultThumb;
                  }
                }}
              />
              <div className="bought-together-info">
                <h4 className="bought-together-name" title={item.name}>
                  {item.name}
                </h4>
                <div className="bought-together-price">
                  {formatPrice(item.minPrice)}
                </div>
                {item.recommendationReason && (
                  <span
                    style={{
                      display: 'inline-block',
                      fontSize: '0.68rem',
                      fontWeight: 600,
                      color: '#7c3aed',
                      marginTop: '2px',
                    }}
                  >
                    ✨ {item.recommendationReason}
                  </span>
                )}
              </div>
            </Link>
          ))}
        </div>
      )}
    </section>
  );
};
