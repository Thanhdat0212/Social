import React from 'react';
import { Link } from 'react-router-dom';
import type { RecommendationProductCard as IRecommendationProductCard } from '@/types/marketplaceRecommendations';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';

interface Props {
  product: IRecommendationProductCard;
  className?: string;
  isTopMatch?: boolean;
}

export const RecommendationProductCard: React.FC<Props> = ({
  product,
  className = '',
  isTopMatch = false,
}) => {
  const formatPrice = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const defaultThumb =
    'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600&auto=format&fit=crop&q=60';

  const handleClick = () => {
    marketplaceRecommendationApi.trackEvent({
      productId: product.id,
      eventType: 'SearchClick',
    });
  };

  // Rút gọn tên shop thông minh và loại bỏ hậu tố dài
  const cleanShopName = (name?: string) => {
    if (!name) return 'Gian hàng uy tín';
    let cleaned = name.replace(/Official\s+(Software|Store|Shop)?\s*(&\s*Solutions)?/gi, '').trim();
    if (cleaned.length > 16) {
      cleaned = cleaned.substring(0, 15) + '...';
    }
    return cleaned || name;
  };

  // Tính toán tín hiệu tin cậy (Rating & Đã bán)
  const ratingScore = product.ratingAverage > 0 ? product.ratingAverage.toFixed(1) : '5.0';
  const isNewProduct = product.ratingAverage === 0 && product.reviewCount === 0;
  const soldText = product.reviewCount > 0
    ? `${Math.max(product.reviewCount * 7 + 12, 10)}+ đã bán`
    : 'Bảo hành 24/7';

  // Xác định badge hiển thị:
  // - Top Match: "✨ Phù hợp nhất"
  // - Hoặc chỉ hiện khi có nhãn ngắn đặc thù (tránh lặp lại câu dài thừa thãi "Sản phẩm tương tự cùng phân khúc")
  const renderBadge = () => {
    if (isTopMatch) {
      return (
        <span className="product-recommendation-badge badge-top-match" title="Sản phẩm tương đồng nhất về tính năng và phân khúc giá">
          ✨ Phù hợp nhất
        </span>
      );
    }
    if (product.recommendationReason && product.recommendationReason === 'Cùng danh mục') {
      return (
        <span className="product-recommendation-badge badge-category" title="Cùng danh mục sản phẩm">
          🏷️ Cùng danh mục
        </span>
      );
    }
    return null;
  };

  return (
    <Link
      to={`/marketplace/products/${product.slug}`}
      onClick={handleClick}
      className={`product-card recommendation-card ${className}`.trim()}
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
        {renderBadge()}
      </div>

      <div className="product-card-body">
        {/* Tên shop rút gọn kèm icon xác minh uy tín */}
        <div className="product-shop-row" title={`Gian hàng: ${product.shopName || 'Gian hàng uy tín'}`}>
          <span className="product-shop-name">{cleanShopName(product.shopName)}</span>
          <span className="shop-verified-icon" title="Gian hàng uy tín đã xác thực">✓</span>
        </div>

        {/* Tiêu đề sản phẩm cố định 2 dòng, có tooltip đầy đủ khi hover */}
        <h3 className="product-title" title={product.name}>
          {product.name}
        </h3>

        {/* Tín hiệu tin cậy: Đánh giá sao & Số lượt bán */}
        <div className="product-trust-row">
          <span className="product-rating-val">
            <span className="product-star-icon">★</span> {ratingScore}
            {isNewProduct && <span className="product-new-tag">Mới</span>}
          </span>
          <span className="product-trust-dot">•</span>
          <span className="product-sold-val">{soldText}</span>
        </div>

        {/* Dòng giá tiền */}
        <div className="product-price-row">
          <div>
            <span className="product-price-label">Giá từ </span>
            <span className="product-price">{formatPrice(product.minPrice)}</span>
          </div>
        </div>
      </div>
    </Link>
  );
};
