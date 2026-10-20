import React from 'react';
import { Link } from 'react-router-dom';
import type { RecommendationProductCard as IRecommendationProductCard } from '@/types/marketplaceRecommendations';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';

interface Props {
  product: IRecommendationProductCard;
  className?: string;
}

export const RecommendationProductCard: React.FC<Props> = ({ product, className = '' }) => {
  const formatPrice = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const defaultThumb =
    'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600&auto=format&fit=crop&q=60';

  const handleClick = () => {
    // Ghi nhận hành vi SearchClick/RecommendationClick
    marketplaceRecommendationApi.trackEvent({
      productId: product.id,
      eventType: 'SearchClick',
    });
  };

  return (
    <Link
      to={`/marketplace/products/${product.slug}`}
      onClick={handleClick}
      className={`product-card ${className}`.trim()}
      aria-label={`${product.name}, giá từ ${formatPrice(product.minPrice)}`}
    >
      <div className="product-thumb-wrap">
        <img
          src={product.coverImageUrl || defaultThumb}
          alt={product.name}
          className="product-thumb"
          loading="lazy"
          onError={(e) => {
            const target = e.currentTarget;
            if (!target.dataset.hasFallback) {
              target.dataset.hasFallback = 'true';
              target.src = defaultThumb;
            }
          }}
        />
        {product.recommendationReason && (
          <span className="product-recommendation-badge" title={product.recommendationReason}>
            ✨ {product.recommendationReason}
          </span>
        )}
      </div>

      <div className="product-card-body">
        <span className="product-shop-name">{product.shopName || 'Gian hàng uy tín'}</span>
        <h3 className="product-title" title={product.name}>
          {product.name}
        </h3>

        <div className="product-price-row">
          <div>
            <span className="product-price-label">Giá từ </span>
            <span className="product-price">{formatPrice(product.minPrice)}</span>
          </div>
          {product.ratingAverage > 0 && (
            <span className="product-rating-badge">
              ⭐ {product.ratingAverage.toFixed(1)}
              {product.reviewCount > 0 && <span className="text-xs text-gray-400"> ({product.reviewCount})</span>}
            </span>
          )}
        </div>
      </div>
    </Link>
  );
};
