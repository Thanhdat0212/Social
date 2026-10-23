import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { ProductListItem } from '@/types/marketplace';
import {
  StoreIcon,
  SearchIcon,
  ShieldIcon,
  CheckCircleIcon,
  AlertTriangleIcon,
} from '@/components/common/Icons';
import { PageContainer, EmptyState, Button, Skeleton, Badge } from '@/components/common';

interface PublicShopInfo {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string;
  description?: string;
  contactEmail: string;
  approvalStatus: string;
  operatingStatus: string;
  effectiveStatus: string;
  createdAtUtc: string;
}

export const ShopProfilePage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>();
  const [shop, setShop] = useState<PublicShopInfo | null>(null);
  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState<string>('');
  const [page, setPage] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);

  useEffect(() => {
    if (!slug) return;
    const fetchShopAndProducts = async () => {
      try {
        setLoading(true);
        setError(null);
        const shopData = await marketplaceApi.getPublicShop(slug);
        setShop(shopData);

        const prodsData = await marketplaceApi.getShopPublicProducts(slug, {
          page,
          pageSize: 16,
          search: search || undefined,
        });
        setProducts(prodsData.items || []);
        setTotalCount(prodsData.totalCount || 0);
      } catch (err: any) {
        setError(err.response?.data?.message || 'Không thể tải thông tin gian hàng.');
      } finally {
        setLoading(false);
      }
    };

    fetchShopAndProducts();
  }, [slug, page, search]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
  };

  const getStatusBadge = (effectiveStatus?: string) => {
    switch (effectiveStatus) {
      case 'Active':
        return (
          <Badge variant="success" icon={<CheckCircleIcon size={14} />}>
            Gian hàng uy tín
          </Badge>
        );
      case 'Closed':
        return (
          <Badge variant="warning" icon={<AlertTriangleIcon size={14} />}>
            Tạm nghỉ nhận đơn
          </Badge>
        );
      case 'Suspended':
        return (
          <Badge variant="danger" icon={<ShieldIcon size={14} />}>
            Đang bị tạm khóa
          </Badge>
        );
      default:
        return <Badge variant="neutral">{effectiveStatus || 'Hoạt động'}</Badge>;
    }
  };

  if (loading && !shop) {
    return (
      <PageContainer variant="wide">
        <div style={{ paddingTop: '1.5rem', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          <Skeleton height={180} />
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))', gap: '1.25rem' }}>
            <Skeleton height={260} count={4} />
          </div>
        </div>
      </PageContainer>
    );
  }

  if (error || !shop) {
    return (
      <PageContainer variant="wide">
        <div style={{ maxWidth: '480px', margin: '4rem auto', background: 'var(--bg-card)', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)', textAlign: 'center' }}>
          <AlertTriangleIcon size={40} className="text-warning" style={{ margin: '0 auto 1rem', color: 'var(--warning)' }} />
          <h3 style={{ color: 'var(--text-main)', marginBottom: '0.5rem' }}>Không tìm thấy gian hàng</h3>
          <p style={{ color: 'var(--text-muted)', marginBottom: '1.5rem' }}>
            {error || 'Gian hàng không tồn tại hoặc đã ngừng kinh doanh trên sàn.'}
          </p>
          <Link to="/marketplace" className="btn btn-primary">
            Quay lại Chợ số
          </Link>
        </div>
      </PageContainer>
    );
  }

  return (
    <PageContainer variant="wide">
      {/* Shop Header Banner */}
      <div
        className="shop-profile-header card-glass"
        style={{
          padding: '2.5rem',
          borderRadius: '20px',
          marginBottom: '2rem',
          position: 'relative',
          overflow: 'hidden',
          border: '1px solid var(--border-color)',
        }}
      >
        <div
          style={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            height: '6px',
            background: 'linear-gradient(90deg, var(--accent-primary), var(--accent-cyan))',
          }}
        />

        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', flexWrap: 'wrap', gap: '1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem' }}>
            <div
              style={{
                width: '84px',
                height: '84px',
                borderRadius: '16px',
                background: 'linear-gradient(135deg, rgba(99, 102, 241, 0.2), rgba(6, 182, 212, 0.2))',
                border: '1px solid rgba(255, 255, 255, 0.1)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--accent-primary)',
              }}
            >
              {shop.logoUrl ? (
                <img
                  src={shop.logoUrl}
                  alt={shop.name}
                  style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: '16px' }}
                  onError={(e) => {
                    e.currentTarget.onerror = null;
                    e.currentTarget.src = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="84" height="84" viewBox="0 0 24 24" fill="none" stroke="%234f46e5" stroke-width="1.5"><path d="m2 7 4.41-4.41A2 2 0 0 1 7.83 2h8.34a2 2 0 0 1 1.42.59L22 7"/><path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8"/><path d="M15 22v-4a2 2 0 0 0-2-2h-2a2 2 0 0 0-2 2v4"/><path d="M2 7h20"/></svg>';
                  }}
                />
              ) : (
                <StoreIcon size={42} />
              )}
            </div>

            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.25rem' }}>
                <h1 style={{ fontSize: '1.75rem', fontWeight: 800, margin: 0 }}>{shop.name}</h1>
                {getStatusBadge(shop.effectiveStatus)}
              </div>
              <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem', marginBottom: '0.5rem' }}>
                @{shop.slug} • Tham gia từ {new Date(shop.createdAtUtc).toLocaleDateString('vi-VN')}
              </p>
              {shop.description && (
                <p style={{ color: 'var(--text-secondary)', maxWidth: '640px', fontSize: '0.95rem', margin: 0 }}>
                  {shop.description}
                </p>
              )}
            </div>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', alignItems: 'flex-end' }}>
            <a
              href={`mailto:${shop.contactEmail}`}
              className="btn btn-secondary btn-sm"
              style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
            >
              Liên hệ Người bán
            </a>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              Được bảo vệ bởi Cơ chế Giữ tiền 3 ngày của Sàn
            </span>
          </div>
        </div>
      </div>

      {/* Product Catalog Section */}
      <div style={{ marginBottom: '1.5rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
        <div>
          <h2 style={{ fontSize: '1.3rem', fontWeight: 700, margin: 0 }}>
            Sản phẩm của Gian hàng ({totalCount})
          </h2>
          <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
            Giao hàng tức thì qua hệ thống mã số & file tự động
          </span>
        </div>

        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.5rem', minWidth: '280px' }}>
          <div style={{ position: 'relative', width: '100%' }}>
            <SearchIcon
              size={16}
              style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }}
            />
            <input
              type="text"
              className="form-control"
              placeholder="Tìm kiếm trong shop..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ paddingLeft: '36px', height: '38px', borderRadius: '10px' }}
            />
          </div>
          <button type="submit" className="btn btn-secondary btn-sm" style={{ padding: '0 1rem' }}>
            Tìm
          </button>
        </form>
      </div>

      {/* Product Grid */}
      {products.length === 0 ? (
        <EmptyState
          icon={<StoreIcon size={44} />}
          title="Chưa có sản phẩm nào"
          description={
            search
              ? `Không tìm thấy sản phẩm phù hợp với từ khóa "${search}".`
              : 'Gian hàng hiện chưa đăng bán sản phẩm nào trên hệ thống.'
          }
          action={
            search ? (
              <Button variant="secondary" size="sm" onClick={() => setSearch('')}>
                Xóa bộ lọc tìm kiếm
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(240px, 1fr))',
            gap: '1.25rem',
            marginBottom: '2rem',
          }}
        >
          {products.map((product) => (
            <Link
              key={product.id}
              to={`/marketplace/products/${product.slug}`}
              className="product-card card-glass"
              style={{
                textDecoration: 'none',
                color: 'inherit',
                borderRadius: '16px',
                overflow: 'hidden',
                display: 'flex',
                flexDirection: 'column',
                transition: 'transform 0.2s ease, border-color 0.2s ease',
                border: '1px solid var(--border-color)',
              }}
            >
              <div style={{ aspectRatio: '1 / 1', background: 'var(--bg-surface, rgba(0,0,0,0.05))', position: 'relative', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center', borderBottom: '1px solid var(--border-color)' }}>
                {product.coverImageUrl ? (
                  <img
                    src={product.coverImageUrl}
                    alt={product.name}
                    style={{ width: '100%', height: '100%', objectFit: 'contain' }}
                    onError={(e) => {
                      e.currentTarget.onerror = null;
                      e.currentTarget.src = 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="100" height="100" viewBox="0 0 24 24" fill="none" stroke="%2394a3b8" stroke-width="1.5"><rect x="3" y="3" width="18" height="18" rx="2"/><path d="m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21"/><circle cx="9" cy="9" r="2"/></svg>';
                    }}
                  />
                ) : (
                  <div style={{ width: '100%', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-muted)' }}>
                    <StoreIcon size={36} />
                  </div>
                )}
                <div style={{ position: 'absolute', top: '8px', left: '8px' }}>
                  <Badge variant="success" size="sm">
                    Tự động 24/7
                  </Badge>
                </div>
              </div>

              <div style={{ padding: '1rem', display: 'flex', flexDirection: 'column', flexGrow: 1, justifyContent: 'space-between' }}>
                <div>
                  <h4
                    style={{
                      fontSize: '0.95rem',
                      fontWeight: 600,
                      marginBottom: '0.5rem',
                      display: '-webkit-box',
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: 'vertical',
                      overflow: 'hidden',
                      minHeight: '2.8rem',
                    }}
                  >
                    {product.name}
                  </h4>
                </div>

                <div style={{ marginTop: '0.75rem', display: 'flex', alignItems: 'baseline', justifyContent: 'space-between' }}>
                  <span style={{ fontSize: '1.1rem', fontWeight: 800, color: 'var(--accent-primary)' }}>
                    {product.minPrice?.toLocaleString('vi-VN')} đ
                  </span>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    {product.salesCount ? `${product.salesCount} đã bán` : 'Mới mở bán'}
                  </span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </PageContainer>
  );
};
