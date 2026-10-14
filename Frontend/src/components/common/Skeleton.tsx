import React from 'react';

export interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  width?: string | number;
  height?: string | number;
  borderRadius?: string | number;
  circle?: boolean;
  count?: number;
}

export const Skeleton: React.FC<SkeletonProps> = ({
  width,
  height,
  borderRadius,
  circle = false,
  count = 1,
  className = '',
  style,
  ...props
}) => {
  const customStyles: React.CSSProperties = {
    ...style,
    width: width !== undefined ? (typeof width === 'number' ? `${width}px` : width) : undefined,
    height: height !== undefined ? (typeof height === 'number' ? `${height}px` : height) : undefined,
    borderRadius: circle ? '50%' : borderRadius !== undefined ? (typeof borderRadius === 'number' ? `${borderRadius}px` : borderRadius) : undefined,
  };

  if (count > 1) {
    return (
      <>
        {Array.from({ length: count }).map((_, i) => (
          <div
            key={i}
            className={`app-skeleton ${circle ? 'is-circle' : ''} ${className}`.trim()}
            style={customStyles}
            aria-hidden="true"
            {...props}
          />
        ))}
      </>
    );
  }

  return (
    <div
      className={`app-skeleton ${circle ? 'is-circle' : ''} ${className}`.trim()}
      style={customStyles}
      aria-hidden="true"
      {...props}
    />
  );
};

export const ProductCardSkeleton: React.FC = () => (
  <div className="product-card skeleton-card">
    <div className="product-thumb-wrap">
      <Skeleton height="100%" borderRadius="12px 12px 0 0" />
    </div>
    <div className="product-card-body" style={{ padding: '0.85rem' }}>
      <Skeleton height={14} width="50%" style={{ marginBottom: '8px' }} />
      <Skeleton height={18} width="90%" style={{ marginBottom: '6px' }} />
      <Skeleton height={18} width="70%" style={{ marginBottom: '16px' }} />
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Skeleton height={22} width="40%" />
        <Skeleton height={16} width="30%" />
      </div>
    </div>
  </div>
);

export const PostCardSkeleton: React.FC = () => (
  <div className="post-entry skeleton-card" style={{ padding: '1.25rem' }}>
    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1rem' }}>
      <Skeleton circle width={40} height={40} />
      <div style={{ flex: 1 }}>
        <Skeleton height={16} width="35%" style={{ marginBottom: '6px' }} />
        <Skeleton height={12} width="20%" />
      </div>
    </div>
    <Skeleton height={16} width="100%" style={{ marginBottom: '8px' }} />
    <Skeleton height={16} width="92%" style={{ marginBottom: '8px' }} />
    <Skeleton height={16} width="60%" style={{ marginBottom: '1rem' }} />
    <div style={{ display: 'flex', gap: '1.5rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)' }}>
      <Skeleton height={20} width={60} />
      <Skeleton height={20} width={60} />
    </div>
  </div>
);
