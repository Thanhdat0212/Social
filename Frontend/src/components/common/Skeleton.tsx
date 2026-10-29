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
  <article className="post-entry skeleton-card" style={{ padding: '1.25rem', marginBottom: '1rem' }} aria-hidden="true">
    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1rem' }}>
      <Skeleton circle width={40} height={40} />
      <div style={{ flex: 1 }}>
        <Skeleton height={16} width="35%" style={{ marginBottom: '6px' }} />
        <Skeleton height={12} width="22%" />
      </div>
    </div>
    <Skeleton height={15} width="100%" style={{ marginBottom: '8px' }} />
    <Skeleton height={15} width="92%" style={{ marginBottom: '8px' }} />
    <Skeleton height={15} width="58%" style={{ marginBottom: '1.25rem' }} />
    
    {/* Media preview placeholder */}
    <Skeleton height={200} width="100%" borderRadius="10px" style={{ marginBottom: '1rem' }} />

    {/* Action buttons placeholder */}
    <div style={{ display: 'flex', gap: '1.5rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color, #e2e8f0)' }}>
      <Skeleton height={22} width={68} borderRadius={6} />
      <Skeleton height={22} width={68} borderRadius={6} />
      <Skeleton height={22} width={68} borderRadius={6} />
    </div>
  </article>
);

export const SidebarProductsSkeleton: React.FC = () => (
  <div className="aside-widget" style={{ marginBottom: 0 }} aria-hidden="true">
    <div className="aside-widget-header">
      <div className="aside-widget-title-wrap" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        <Skeleton circle width={24} height={24} />
        <Skeleton height={18} width={120} />
      </div>
      <Skeleton height={14} width={55} />
    </div>
    <div className="sidebar-product-list" style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '0.75rem' }}>
      {[1, 2, 3].map((i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
          <Skeleton width={42} height={42} borderRadius={8} />
          <div style={{ flex: 1, minWidth: 0 }}>
            <Skeleton height={14} width="85%" style={{ marginBottom: '6px' }} />
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Skeleton height={13} width="38%" />
              <Skeleton height={12} width="28%" />
            </div>
          </div>
        </div>
      ))}
    </div>
  </div>
);

export const SidebarTopicsSkeleton: React.FC = () => (
  <div className="aside-widget" style={{ marginBottom: 0 }} aria-hidden="true">
    <div className="aside-widget-header">
      <div className="aside-widget-title-wrap" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        <Skeleton circle width={24} height={24} />
        <Skeleton height={18} width={130} />
      </div>
    </div>
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '0.75rem' }}>
      {[70, 85, 60, 95, 75, 80].map((w, i) => (
        <Skeleton key={i} width={w} height={26} borderRadius={13} />
      ))}
    </div>
  </div>
);
