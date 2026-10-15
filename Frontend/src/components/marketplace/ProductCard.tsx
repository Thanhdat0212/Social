import React from 'react';
import { Link } from 'react-router-dom';
import type { ProductListItem } from '@/types/marketplace';

interface ProductCardProps {
  product: ProductListItem;
  className?: string;
  showShopName?: boolean;
}

export const ProductCard: React.FC<ProductCardProps> = ({
  product,
  className = '',
  showShopName = true,
}) => {
  const formatPrice = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const defaultThumb = 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=600&auto=format&fit=crop&q=60';

  return (
    <Link
      to={`/marketplace/products/${product.slug}`}
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
        <span className="product-instant-badge">⚡ Tự động</span>
      </div>
      <div className="product-card-body">
        {showShopName && (
          <span className="product-shop-name">{product.shopName || 'Gian hàng uy tín'}</span>
        )}
        <h3 className="product-title" title={product.name}>
          {product.name}
        </h3>
        <div className="product-price-row">
          <div>
            <span className="product-price-label">Giá từ </span>
            <span className="product-price">{formatPrice(product.minPrice)}</span>
          </div>
          {product.variantCount > 1 && (
            <span className="product-variants-count">{product.variantCount} gói</span>
          )}
        </div>
      </div>
    </Link>
  );
};
