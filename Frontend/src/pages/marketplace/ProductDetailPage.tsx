import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { marketplaceApi } from '@/api/marketplaceApi';
import type {
  ProductDetail,
  ProductVariant,
  ProductReviewDto,
  ProductReviewSummaryDto,
} from '@/types/marketplace';
import { useCartStore } from '@/store/cartStore';
import { signalrService } from '@/services/signalrService';
import { ROUTES } from '@/constants/routes';
import {
  Spinner,
  Button,
  StoreIcon,
  PageContainer,
  ShieldIcon,
  AlertCircleIcon,
  FormField,
  Select,
  Textarea,
  CheckCheckIcon,
  ShareIcon,
} from '@/components/common';

export const ProductDetailPage: React.FC = () => {
  const { slug } = useParams<{ slug: string }>();
  const navigate = useNavigate();
  const { addItem, isLoading: isCartLoading } = useCartStore();

  const [product, setProduct] = useState<ProductDetail | null>(null);
  const [selectedVariant, setSelectedVariant] = useState<ProductVariant | null>(null);
  const [selectedImage, setSelectedImage] = useState<string>('');
  const [quantity, setQuantity] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [message, setMessage] = useState<string | null>(null);
  const [showReportModal, setShowReportModal] = useState(false);
  const [reportReason, setReportReason] = useState(1);
  const [reportDesc, setReportDesc] = useState('');
  const [isReporting, setIsReporting] = useState(false);
  const [reportFeedback, setReportFeedback] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);
  const [copiedShare, setCopiedShare] = useState(false);

  // States quản lý Đánh giá sản phẩm
  const [reviews, setReviews] = useState<ProductReviewDto[]>([]);
  const [reviewSummary, setReviewSummary] = useState<ProductReviewSummaryDto | null>(null);
  const [reviewPage, setReviewPage] = useState(1);
  const [reviewTotalPages, setReviewTotalPages] = useState(1);
  const [isReviewsLoading, setIsReviewsLoading] = useState(false);

  const handleShareProduct = () => {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(window.location.href);
      setCopiedShare(true);
      setTimeout(() => setCopiedShare(false), 2000);
    }
  };

  useEffect(() => {
    const loadDetail = async () => {
      if (!slug) return;
      setIsLoading(true);
      try {
        const data = await marketplaceApi.getProductBySlug(slug);
        setProduct(data);
        if (data.variants && data.variants.length > 0) {
          setSelectedVariant(data.variants[0]);
        } else {
          setSelectedVariant(null);
        }
        setQuantity(1);
        if (data.images && data.images.length > 0) {
          const cover = data.images.find((i) => i.isCover) || data.images[0];
          setSelectedImage(cover.url);
        }
      } catch (err) {
        console.error('Lỗi khi tải chi tiết sản phẩm:', err);
      } finally {
        setIsLoading(false);
      }
    };
    loadDetail();
  }, [slug]);

  // Tải danh sách đánh giá & thống kê sao của sản phẩm
  useEffect(() => {
    if (!product?.id) return;
    const fetchReviews = async () => {
      setIsReviewsLoading(true);
      try {
        const [reviewsRes, summaryRes] = await Promise.all([
          marketplaceApi.getProductReviews(product.id, reviewPage, 5),
          marketplaceApi.getProductReviewSummary(product.id),
        ]);
        setReviews(reviewsRes.items || []);
        setReviewTotalPages(reviewsRes.totalPages || 1);
        setReviewSummary(summaryRes);
      } catch (err) {
        console.error('Lỗi khi tải đánh giá sản phẩm:', err);
      } finally {
        setIsReviewsLoading(false);
      }
    };
    fetchReviews();
  }, [product?.id, reviewPage]);

  // Lắng nghe cập nhật tồn kho Realtime từ SignalR (WebSocket)
  useEffect(() => {
    signalrService.start();

    const handleStockUpdate = (payload: {
      productId?: string;
      productSlug?: string;
      variantId?: string;
      availableStock: number;
    }) => {
      if (!payload) return;

      setProduct((prev) => {
        if (!prev) return prev;
        const isMatch =
          (payload.productId && payload.productId === prev.id) ||
          (payload.productSlug && payload.productSlug === prev.slug);
        if (!isMatch) return prev;

        const updatedVariants = prev.variants.map((v) =>
          v.id === payload.variantId ? { ...v, availableStock: payload.availableStock } : v
        );
        return { ...prev, variants: updatedVariants };
      });

      setSelectedVariant((prev) => {
        if (!prev || prev.id !== payload.variantId) return prev;
        return { ...prev, availableStock: payload.availableStock };
      });
    };

    signalrService.on('ProductStockUpdated', handleStockUpdate);

    return () => {
      signalrService.off('ProductStockUpdated', handleStockUpdate);
    };
  }, []);

  const getVariantLabel = (v: ProductVariant) => {
    if (v.name && v.name.trim()) return v.name;
    if (v.optionValues && v.optionValues.length > 0) return v.optionValues.join(' / ');
    if (v.combinationKey && v.combinationKey !== 'default') return v.combinationKey;
    if (v.sku) return v.sku;
    return 'Bản tiêu chuẩn';
  };

  const formatPrice = (price?: number | null) => {
    if (price === undefined || price === null || isNaN(price)) return '0 đ';
    return new Intl.NumberFormat('vi-VN').format(price) + ' đ';
  };

  const handleAddToCart = async () => {
    if (!selectedVariant) return;
    const success = await addItem(selectedVariant.id, quantity);
    if (success) {
      setMessage(`Đã thêm ${quantity}x "${getVariantLabel(selectedVariant)}" vào giỏ hàng thành công!`);
      setTimeout(() => setMessage(null), 3500);
    }
  };

  const handleBuyNow = async () => {
    if (!selectedVariant) return;
    const success = await addItem(selectedVariant.id, quantity);
    if (success) {
      navigate(ROUTES.MARKETPLACE.CART);
    }
  };

  const handleSendReport = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!product || !reportDesc.trim()) return;
    setIsReporting(true);
    setReportFeedback(null);
    try {
      await marketplaceApi.reportProduct(product.id, {
        reason: reportReason,
        description: reportDesc.trim(),
      });
      setReportFeedback({
        type: 'success',
        msg: 'Đã gửi báo cáo vi phạm thành công. Quản trị viên sẽ sớm xem xét.',
      });
      setTimeout(() => {
        setShowReportModal(false);
        setReportDesc('');
        setReportFeedback(null);
      }, 1500);
    } catch (err: any) {
      setReportFeedback({
        type: 'error',
        msg: err.response?.data?.message || 'Không thể gửi báo cáo vi phạm.',
      });
    } finally {
      setIsReporting(false);
    }
  };

  if (isLoading) {
    return (
      <PageContainer variant="wide">
        <div style={{ display: 'flex', justifyContent: 'center', padding: '5rem 0' }}>
          <Spinner size="lg" />
        </div>
      </PageContainer>
    );
  }

  if (!product) {
    return (
      <PageContainer variant="wide">
        <div style={{ textAlign: 'center', padding: '4rem 1rem' }}>
          <h2>Không tìm thấy sản phẩm</h2>
          <Link to={ROUTES.MARKETPLACE.INDEX} className="btn btn-primary" style={{ marginTop: '1rem' }}>
            Quay lại Chợ số
          </Link>
        </div>
      </PageContainer>
    );
  }

  const isOutOfStock = Boolean(
    !selectedVariant ||
      (selectedVariant.trackInventory &&
        selectedVariant.availableStock !== undefined &&
        selectedVariant.availableStock <= 0)
  );
  const maxStock =
    selectedVariant?.trackInventory && selectedVariant.availableStock !== undefined
      ? Math.max(1, selectedVariant.availableStock)
      : 99;

  return (
    <PageContainer variant="wide">
      {/* Breadcrumb */}
      <nav style={{ marginBottom: '1.25rem', fontSize: '0.88rem', color: 'var(--text-dim)' }}>
        <Link to={ROUTES.MARKETPLACE.INDEX} style={{ color: 'var(--text-muted)' }}>
          Chợ số
        </Link>
        {' / '}
        <span>{product.categoryName || 'Sản phẩm số'}</span>
        {' / '}
        <span style={{ color: 'var(--text-main)' }}>{product.name}</span>
      </nav>

      {message && (
        <div
          style={{
            background: 'rgba(16, 185, 129, 0.15)',
            border: '1px solid rgba(16, 185, 129, 0.4)',
            color: '#34d399',
            padding: '0.75rem 1rem',
            borderRadius: '10px',
            marginBottom: '1.5rem',
            fontWeight: 600,
          }}
        >
          ✓ {message}
        </div>
      )}

      <div className="product-detail-layout">
        {/* Cột trái: Thư viện ảnh */}
        <div className="detail-gallery">
          <div className="main-image-frame">
            <img
              src={
                selectedImage ||
                'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=800&auto=format&fit=crop&q=80'
              }
              alt={product.name}
              className="main-image"
            />
          </div>
          {product.images && product.images.length > 1 && (
            <div className="thumbnails-row">
              {product.images.map((img) => (
                <button
                  key={img.id}
                  type="button"
                  className={`thumb-btn ${selectedImage === img.url ? 'active' : ''}`}
                  onClick={() => setSelectedImage(img.url)}
                >
                  <img src={img.url} alt="thumbnail" />
                </button>
              ))}
            </div>
          )}

          {/* Cam kết an toàn */}
          <div
            style={{
              background: 'var(--bg-surface)',
              border: '1px solid var(--border-color)',
              borderRadius: '12px',
              padding: '1rem',
              marginTop: '1rem',
            }}
          >
            <div
              style={{
                fontWeight: 700,
                color: 'var(--text-main)',
                marginBottom: '0.5rem',
                fontSize: '0.9rem',
                display: 'flex',
                alignItems: 'center',
                gap: '0.4rem',
              }}
            >
              <ShieldIcon size={18} style={{ color: 'var(--accent-primary)' }} />
              <span>Chính sách bảo vệ người mua</span>
            </div>
            <ul style={{ fontSize: '0.82rem', color: 'var(--text-muted)', lineHeight: 1.6, paddingLeft: '1.2rem', margin: 0 }}>
              <li>Tiền thanh toán được hệ thống tạm giữ an toàn trong 48 giờ.</li>
              <li>Mã số bản quyền / tài khoản được giao tức thì sau khi chuyển khoản.</li>
              <li>Hỗ trợ khiếu nại hoàn tiền 100% nếu mã bị lỗi hoặc sai mô tả.</li>
            </ul>
          </div>
        </div>

        {/* Cột phải: Thông tin & Đặt mua */}
        <div className="detail-info">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '1rem', marginBottom: '0.75rem' }}>
            <h1 className="detail-title" style={{ margin: 0 }}>{product.name}</h1>
            <Button
              variant="secondary"
              size="sm"
              onClick={handleShareProduct}
              icon={copiedShare ? <CheckCheckIcon size={14} /> : <ShareIcon size={14} />}
              style={{ flexShrink: 0, fontSize: '0.78rem' }}
              title="Sao chép liên kết chia sẻ sản phẩm"
            >
              {copiedShare ? 'Đã sao chép' : 'Chia sẻ'}
            </Button>
          </div>

          {/* Shop Card */}
          {product.shopSlug ? (
            <Link
              to={`/marketplace/shops/${product.shopSlug}`}
              className="detail-shop-card"
              style={{ textDecoration: 'none', transition: 'transform 0.15s ease, border-color 0.15s ease' }}
              title="Xem tất cả sản phẩm của gian hàng này"
            >
              <StoreIcon size={22} style={{ color: 'var(--accent-primary)' }} />
              <div>
                <div style={{ fontWeight: 700, color: 'var(--text-main)', fontSize: '0.95rem' }}>
                  {product.shopName} <span style={{ fontSize: '0.75rem', color: 'var(--accent-primary)' }}>↗</span>
                </div>
                <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)' }}>Gian hàng đối tác chính thức • Xem shop</div>
              </div>
            </Link>
          ) : (
            <div className="detail-shop-card">
              <StoreIcon size={22} style={{ color: 'var(--accent-primary)' }} />
              <div>
                <div style={{ fontWeight: 700, color: 'var(--text-main)', fontSize: '0.95rem' }}>
                  {product.shopName}
                </div>
                <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)' }}>Gian hàng đối tác chính thức</div>
              </div>
            </div>
          )}

          {/* Giá tiền */}
          <div style={{ margin: '1rem 0 1.5rem', padding: '1rem', background: 'var(--bg-surface)', borderRadius: '12px', border: '1px solid var(--border-color)' }}>
            <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)', marginBottom: '0.25rem' }}>Giá bán gói đã chọn:</div>
            <div style={{ fontSize: '1.85rem', fontWeight: 800, color: 'var(--accent-primary)' }}>
              {selectedVariant ? formatPrice(selectedVariant.price) : 'Chọn gói để xem giá'}
            </div>
            <div style={{ fontSize: '0.82rem', color: isOutOfStock ? 'var(--color-danger)' : '#10b981', fontWeight: 600, marginTop: '0.25rem' }}>
              {isOutOfStock ? '⚠️ Tạm hết hàng trong kho' : `✓ Sẵn sàng giao ngay${selectedVariant?.availableStock !== undefined ? ` (Còn ${selectedVariant.availableStock} mã)` : ''}`}
            </div>
          </div>

          {/* Chọn biến thể */}
          {product.variants && product.variants.length > 0 && (
            <div className="variant-selector-group">
              <div className="variant-selector-title">Chọn gói sản phẩm / thời hạn:</div>
              <div className="variant-options-grid">
                {product.variants.map((v) => (
                  <button
                    key={v.id}
                    type="button"
                    className={`variant-option-chip ${selectedVariant?.id === v.id ? 'selected' : ''}`}
                    onClick={() => {
                      setSelectedVariant(v);
                      setQuantity(1);
                    }}
                  >
                    <span className="chip-name">{getVariantLabel(v)}</span>
                    <span className="chip-price">{formatPrice(v.price)}</span>
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Chọn số lượng */}
          {!isOutOfStock && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
              <span style={{ fontSize: '0.9rem', color: 'var(--text-muted)', fontWeight: 600 }}>Số lượng:</span>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  onClick={() => setQuantity(Math.max(1, quantity - 1))}
                  disabled={quantity <= 1}
                  aria-label="Giảm số lượng"
                >
                  -
                </button>
                <span style={{ fontWeight: 700, minWidth: '30px', textAlign: 'center', color: 'var(--text-main)' }}>
                  {quantity}
                </span>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  onClick={() => setQuantity(Math.min(maxStock, quantity + 1))}
                  disabled={quantity >= maxStock}
                  aria-label="Tăng số lượng"
                >
                  +
                </button>
              </div>
            </div>
          )}

          {/* Nút hành động */}
          <div className="detail-actions-bar">
            <Button
              type="button"
              variant="outline"
              onClick={handleAddToCart}
              disabled={isOutOfStock || isCartLoading}
              style={{ flex: 1 }}
            >
              Thêm vào giỏ
            </Button>
            <Button
              type="button"
              variant="primary"
              onClick={handleBuyNow}
              disabled={isOutOfStock || isCartLoading}
              style={{ flex: 1 }}
            >
              Mua ngay
            </Button>
          </div>

          {/* Nút Báo cáo vi phạm */}
          <div style={{ marginTop: '1.5rem', textAlign: 'right' }}>
            <button
              type="button"
              onClick={() => {
                setReportFeedback(null);
                setReportDesc('');
                setShowReportModal(true);
              }}
              style={{
                background: 'none',
                border: 'none',
                color: 'var(--text-dim)',
                fontSize: '0.8rem',
                cursor: 'pointer',
                textDecoration: 'underline',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.35rem',
              }}
            >
              <AlertCircleIcon size={14} />
              Báo cáo sản phẩm vi phạm
            </button>
          </div>
        </div>
      </div>

      {/* Mô tả chi tiết sản phẩm */}
      <section
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          padding: '2rem',
        }}
      >
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem' }}>
          Mô tả sản phẩm & Hướng dẫn sử dụng
        </h2>
        <div
          style={{ color: 'var(--text-muted)', lineHeight: 1.8, fontSize: '0.95rem', whiteSpace: 'pre-line' }}
        >
          {product.description || 'Chưa có mô tả chi tiết cho sản phẩm này.'}
        </div>
      </section>

      {/* Đánh giá & Nhận xét từ người mua */}
      <section
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          padding: '2rem',
          marginTop: '1.5rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1.5rem' }}>
          <div>
            <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
              Đánh giá từ người mua
            </h2>
            <p style={{ margin: '0.25rem 0 0', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
              Nhận xét thực tế từ những khách hàng đã mua và thanh toán đơn hàng thành công
            </p>
          </div>
        </div>

        {/* Thẻ thống kê điểm & phân bổ sao */}
        {reviewSummary && (
          <div
            style={{
              display: 'flex',
              flexWrap: 'wrap',
              gap: '2rem',
              alignItems: 'center',
              padding: '1.25rem',
              background: 'var(--bg-subtle, rgba(0,0,0,0.02))',
              borderRadius: '12px',
              border: '1px solid var(--border-color)',
              marginBottom: '2rem',
            }}
          >
            {/* Cột điểm trung bình */}
            <div style={{ textAlign: 'center', minWidth: '130px' }}>
              <div style={{ fontSize: '3rem', fontWeight: 800, color: '#f59e0b', lineHeight: 1 }}>
                {reviewSummary.averageRating > 0 ? reviewSummary.averageRating.toFixed(1) : '5.0'}
              </div>
              <div style={{ color: '#f59e0b', fontSize: '1.2rem', margin: '0.35rem 0' }}>
                {'★'.repeat(Math.round(reviewSummary.averageRating || 5))}
                {'☆'.repeat(5 - Math.round(reviewSummary.averageRating || 5))}
              </div>
              <div style={{ fontSize: '0.82rem', color: 'var(--text-muted)' }}>
                {reviewSummary.totalReviews} lượt đánh giá
              </div>
            </div>

            {/* Cột biểu đồ phân bổ sao */}
            <div style={{ flex: 1, minWidth: '220px' }}>
              {[5, 4, 3, 2, 1].map((star) => {
                const count = reviewSummary.starDistribution[star] || 0;
                const percent = reviewSummary.totalReviews > 0 ? Math.round((count / reviewSummary.totalReviews) * 100) : 0;
                return (
                  <div key={star} style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.4rem', fontSize: '0.85rem' }}>
                    <span style={{ width: '45px', color: 'var(--text-main)', fontWeight: 500 }}>
                      {star} sao
                    </span>
                    <div style={{ flex: 1, height: '8px', background: 'var(--border-color)', borderRadius: '999px', overflow: 'hidden' }}>
                      <div
                        style={{
                          height: '100%',
                          width: `${percent}%`,
                          background: '#f59e0b',
                          borderRadius: '999px',
                          transition: 'width 0.3s ease',
                        }}
                      />
                    </div>
                    <span style={{ width: '35px', textAlign: 'right', color: 'var(--text-dim)', fontSize: '0.8rem' }}>
                      {count}
                    </span>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* Danh sách các bài đánh giá */}
        {isReviewsLoading ? (
          <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>
            <Spinner />
          </div>
        ) : reviews.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--text-muted)' }}>
            <div style={{ fontSize: '2.5rem', marginBottom: '0.5rem' }}>💬</div>
            <p style={{ margin: 0, fontWeight: 500 }}>Chưa có đánh giá nào cho sản phẩm này.</p>
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.85rem', color: 'var(--text-dim)' }}>
              Hãy là người đầu tiên trải nghiệm và để lại đánh giá sau khi hoàn tất đơn mua!
            </p>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
            {reviews.map((rev) => (
              <div
                key={rev.id}
                style={{
                  padding: '1.25rem',
                  border: '1px solid var(--border-color)',
                  borderRadius: '12px',
                  background: 'var(--bg-main)',
                }}
              >
                {/* Header người mua */}
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                    {rev.buyerAvatar ? (
                      <img
                        src={rev.buyerAvatar}
                        alt={rev.buyerName}
                        style={{ width: '36px', height: '36px', borderRadius: '50%', objectFit: 'cover' }}
                      />
                    ) : (
                      <div
                        style={{
                          width: '36px',
                          height: '36px',
                          borderRadius: '50%',
                          background: 'var(--primary, #1877f2)',
                          color: '#fff',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontWeight: 700,
                          fontSize: '0.9rem',
                        }}
                      >
                        {rev.buyerName.charAt(0).toUpperCase()}
                      </div>
                    )}
                    <div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <span style={{ fontWeight: 600, color: 'var(--text-main)', fontSize: '0.92rem' }}>
                          {rev.buyerName}
                        </span>
                        {rev.isVerifiedPurchase && (
                          <span
                            style={{
                              background: 'rgba(16, 185, 129, 0.12)',
                              color: 'var(--success, #10b981)',
                              fontSize: '0.7rem',
                              fontWeight: 600,
                              padding: '0.15rem 0.45rem',
                              borderRadius: '4px',
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '0.2rem',
                            }}
                          >
                            ✓ Đã mua hàng
                          </span>
                        )}
                      </div>
                      <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', marginTop: '0.15rem' }}>
                        {new Date(rev.createdAtUtc).toLocaleDateString('vi-VN', {
                          year: 'numeric',
                          month: 'short',
                          day: 'numeric',
                        })}
                      </div>
                    </div>
                  </div>

                  {/* Số sao */}
                  <div style={{ color: '#f59e0b', fontSize: '1rem', letterSpacing: '2px' }}>
                    {'★'.repeat(rev.rating)}
                    {'☆'.repeat(5 - rev.rating)}
                  </div>
                </div>

                {/* Phân loại đã mua */}
                {rev.variantLabel && (
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '0.5rem' }}>
                    Phân loại: <span style={{ color: 'var(--text-main)' }}>{rev.variantLabel}</span>
                  </div>
                )}

                {/* Nội dung nhận xét */}
                <div style={{ color: 'var(--text-main)', fontSize: '0.92rem', lineHeight: 1.6, whiteSpace: 'pre-line' }}>
                  {rev.comment}
                </div>

                {/* Phản hồi từ chủ Shop (nếu có) */}
                {rev.sellerReply && (
                  <div
                    style={{
                      marginTop: '0.85rem',
                      padding: '0.85rem 1rem',
                      background: 'var(--bg-subtle, rgba(0,0,0,0.02))',
                      borderRadius: '8px',
                      borderLeft: '3px solid var(--primary, #1877f2)',
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', marginBottom: '0.25rem' }}>
                      <StoreIcon size={14} style={{ color: 'var(--primary, #1877f2)' }} />
                      <span style={{ fontSize: '0.82rem', fontWeight: 600, color: 'var(--primary, #1877f2)' }}>
                        Phản hồi từ người bán
                      </span>
                      {rev.sellerRepliedAtUtc && (
                        <span style={{ fontSize: '0.75rem', color: 'var(--text-dim)', marginLeft: 'auto' }}>
                          {new Date(rev.sellerRepliedAtUtc).toLocaleDateString('vi-VN')}
                        </span>
                      )}
                    </div>
                    <p style={{ margin: 0, fontSize: '0.88rem', color: 'var(--text-muted)', lineHeight: 1.5 }}>
                      {rev.sellerReply}
                    </p>
                  </div>
                )}
              </div>
            ))}

            {/* Phân trang */}
            {reviewTotalPages > 1 && (
              <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: '1rem', marginTop: '1rem' }}>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={reviewPage <= 1 || isReviewsLoading}
                  onClick={() => setReviewPage((p) => Math.max(1, p - 1))}
                >
                  ← Trang trước
                </Button>
                <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                  Trang {reviewPage} / {reviewTotalPages}
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={reviewPage >= reviewTotalPages || isReviewsLoading}
                  onClick={() => setReviewPage((p) => Math.min(reviewTotalPages, p + 1))}
                >
                  Trang sau →
                </Button>
              </div>
            )}
          </div>
        )}
      </section>

      {/* Modal Báo cáo vi phạm */}
      {showReportModal && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0, 0, 0, 0.75)',
            backdropFilter: 'blur(6px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 9999,
            padding: '1rem',
          }}
          onClick={(e) => {
            if (e.target === e.currentTarget && !isReporting) {
              setShowReportModal(false);
            }
          }}
        >
          <div
            style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '16px',
              padding: '1.75rem',
              maxWidth: '480px',
              width: '100%',
              boxShadow: 'var(--shadow-lg)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1rem' }}>
              <AlertCircleIcon size={20} style={{ color: 'var(--danger)' }} />
              <h3 style={{ fontSize: '1.15rem', color: 'var(--text-main)', margin: 0, fontWeight: 700 }}>
                Báo cáo vi phạm sản phẩm
              </h3>
            </div>

            {reportFeedback && (
              <div
                style={{
                  padding: '0.65rem 0.85rem',
                  borderRadius: '8px',
                  fontSize: '0.84rem',
                  marginBottom: '1rem',
                  background:
                    reportFeedback.type === 'success'
                      ? 'rgba(16, 185, 129, 0.15)'
                      : 'rgba(239, 68, 68, 0.15)',
                  color: reportFeedback.type === 'success' ? 'var(--success)' : 'var(--danger)',
                  border: `1px solid ${
                    reportFeedback.type === 'success' ? 'var(--success)' : 'var(--danger)'
                  }`,
                }}
              >
                {reportFeedback.msg}
              </div>
            )}

            <form onSubmit={handleSendReport}>
              <FormField label="Lý do vi phạm:">
                <Select
                  value={reportReason}
                  onChange={(e) => setReportReason(Number(e.target.value))}
                >
                  <option value={1}>Hàng giả / Hàng nhái</option>
                  <option value={2}>Lừa đảo / Chiếm đoạt</option>
                  <option value={3}>Vi phạm bản quyền / Sở hữu trí tuệ</option>
                  <option value={4}>Hàng hóa trong danh mục cấm</option>
                  <option value={5}>Mô tả sai lệch, gian lận</option>
                  <option value={6}>Tài khoản hack / Đánh cắp</option>
                  <option value={99}>Lý do khác</option>
                </Select>
              </FormField>

              <FormField
                label="Chi tiết vi phạm:"
                hint="Mô tả cụ thể hành vi vi phạm để ban quản trị kiểm tra"
              >
                <Textarea
                  rows={4}
                  required
                  placeholder="Mô tả cụ thể hành vi vi phạm..."
                  value={reportDesc}
                  onChange={(e) => setReportDesc(e.target.value)}
                />
              </FormField>

              <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end', marginTop: '1.25rem' }}>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setShowReportModal(false)}
                  disabled={isReporting}
                >
                  Hủy
                </Button>
                <Button type="submit" variant="primary" size="sm" loading={isReporting}>
                  Gửi báo cáo
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </PageContainer>
  );
};
