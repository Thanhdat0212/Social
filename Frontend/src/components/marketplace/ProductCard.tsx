import React from 'react';
import { Link } from 'react-router-dom';
import type { ProductListItem } from '@/types/marketplace';
import { KeyIcon } from '@/components/common';

interface ProductCardProps {
  product: ProductListItem;
  className?: string;
  showShopName?: boolean;
}

const DEFAULT_PRODUCT_THUMB =
  "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='400' height='400' viewBox='0 0 400 400'%3E%3Crect width='400' height='400' fill='%23f1f5f9'/%3E%3Cpath d='M150 250l35-45 30 35 45-60 65 70H100z' fill='%23cbd5e1'/%3E%3Ccircle cx='160' cy='150' r='20' fill='%23cbd5e1'/%3E%3C/svg%3E";

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

  return (
    <Link
      to={`/marketplace/products/${product.slug}`}
      className={`product-card ${className}`.trim()}
      aria-label={`${product.name}, giá từ ${formatPrice(product.minPrice)}`}
    >
      <div className="product-thumb-wrap">
        <img
          src={product.coverImageUrl || DEFAULT_PRODUCT_THUMB}
          alt={product.name}
          className="product-thumb"
          loading="lazy"
          onError={(e) => {
            const target = e.currentTarget;
            if (!target.dataset.hasFallback) {
              target.dataset.hasFallback = 'true';
              target.src = DEFAULT_PRODUCT_THUMB;
            }
          }}
        />
        <span className="product-instant-badge">
          <KeyIcon size={12} />
          <span>Tự động 24/7</span>
        </span>
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
