import React, { useEffect, useState, useMemo } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { OrderDto, OrderItemDto, DeliveredCredentialDto } from '@/types/marketplace';
import { ROUTES } from '@/constants/routes';
import {
  PageContainer,
  Button,
  StoreIcon,
  PackageIcon,
  Skeleton,
  EmptyState,
  EyeIcon,
  EyeOffIcon,
  CopyIcon,
  CheckCheckIcon,
  AlertCircleIcon,
  ExternalLinkIcon,
  ArrowLeftIcon,
  ShieldIcon,
  FileTextIcon,
  ClockIcon,
} from '@/components/common';
import { OrderStatusBadge } from '@/components/marketplace/OrderStatusBadge';
import { OrderReviewModal } from './components/OrderReviewModal';
import { OrderComplaintModal } from './components/OrderComplaintModal';
import { OrderItemCredentials } from './components/OrderItemCredentials';

export const OrderDetailPage: React.FC = () => {
  const { id: orderId } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [order, setOrder] = useState<OrderDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Kho mã số giải mã & trạng thái ẩn/hiện/sao chép
  const [copiedKey, setCopiedKey] = useState<string | null>(null);
  const [revealedKeys, setRevealedKeys] = useState<Record<string, boolean>>({});
  const [allRevealed, setAllRevealed] = useState(false);

  // Modal khiếu nại & đánh giá
  const [complaintItem, setComplaintItem] = useState<{ itemId: string; prodName: string } | null>(null);
  const [reviewItem, setReviewItem] = useState<{ itemId: string; prodName: string } | null>(null);
  const [reviewedItems, setReviewedItems] = useState<Record<string, boolean>>({});

  const fetchOrderDetail = async () => {
    if (!orderId) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await marketplaceApi.getOrderDetail(orderId);
      setOrder(data);
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Không thể tải chi tiết đơn hàng. Vui lòng thử lại sau.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchOrderDetail();
  }, [orderId]);

  const formatPrice = (price?: number | null) => {
    return new Intl.NumberFormat('vi-VN').format(price || 0) + ' đ';
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '';
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('vi-VN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  // Helper trích xuất mã số bản quyền hợp nhất
  const getItemCredentials = (item: OrderItemDto): DeliveredCredentialDto[] => {
    if (item.credentials && item.credentials.length > 0) {
      return item.credentials;
    }
    if (item.deliveredCodes && item.deliveredCodes.length > 0) {
      return item.deliveredCodes.map((code) => ({
        orderItemId: item.id,
        productName: item.productName,
        variantName: item.variantName || item.variantLabel || '',
        codePlain: code,
        fileDownloadUrl: item.downloadStoragePath || undefined,
        instructions: 'Nhập trực tiếp mã vào phần mềm hoặc liên kết bản quyền để kích hoạt.',
      }));
    }
    return [];
  };

  const copyCode = (code: string, keyId: string) => {
    navigator.clipboard.writeText(code);
    setCopiedKey(keyId);
    setTimeout(() => setCopiedKey(null), 2000);
  };

  const toggleReveal = (keyId: string) => {
    setRevealedKeys((prev) => ({
      ...prev,
      [keyId]: !prev[keyId],
    }));
  };

  // 1. Chuyển đổi Hiện tất cả / Ẩn tất cả mã số
  const handleToggleRevealAll = () => {
    const nextState = !allRevealed;
    setAllRevealed(nextState);
    const updated: Record<string, boolean> = {};
    order?.items?.forEach((item) => {
      const creds = getItemCredentials(item);
      creds.forEach((_, idx) => {
        updated[`${item.id}-${idx}`] = nextState;
      });
    });
    setRevealedKeys(updated);
  };

  // 2. Sao chép toàn bộ mã của đơn hàng
  const handleCopyAllCodes = () => {
    if (!order) return;
    const allCodes: string[] = [];
    order.items?.forEach((item) => {
      const creds = getItemCredentials(item);
      creds.forEach((c) => {
        if (c.codePlain) allCodes.push(c.codePlain);
      });
    });

    if (allCodes.length === 0) return;
    navigator.clipboard.writeText(allCodes.join('\n'));
    setCopiedKey('ALL');
    setTimeout(() => setCopiedKey(null), 2000);
  };

  // 3. Tải tệp sao lưu (.txt)
  const handleDownloadBackupTxt = () => {
    if (!order) return;
    let txt = `====================================================\n`;
    txt += `ĐƠN HÀNG #${order.orderNo}\n`;
    txt += `Thời gian: ${formatDate(order.createdAtUtc)}\n`;
    txt += `Gian hàng: ${order.shopName || 'Marketplace'}\n`;
    txt += `====================================================\n\n`;

    order.items?.forEach((item, idx) => {
      txt += `SẢN PHẨM ${idx + 1}: ${item.productName}\n`;
      txt += `Gói: ${item.variantName || item.variantLabel || 'Mặc định'}\n`;
      txt += `Số lượng: ${item.quantity}\n`;
      txt += `MÃ BẢN QUYỀN / TÀI KHOẢN:\n`;
      const creds = getItemCredentials(item);
      if (creds.length > 0) {
        creds.forEach((c, cIdx) => {
          txt += `  [${cIdx + 1}] ${c.codePlain}\n`;
          if (c.instructions) txt += `      Hướng dẫn: ${c.instructions}\n`;
        });
      } else {
        txt += `  (Đang xuất kho hoặc chưa hoàn tất)\n`;
      }
      txt += `----------------------------------------------------\n\n`;
    });

    const blob = new Blob([txt], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `DonHang_${order.orderNo}_SaoLuu.txt`;
    a.click();
    URL.revokeObjectURL(url);
  };

  // 4. In biên lai / hóa đơn
  const handlePrintReceipt = () => {
    window.print();
  };

  // 5. Tính toán thời hạn bảo vệ 48h
  const protectionInfo = useMemo(() => {
    if (!order) return null;
    let deadlineMs = 0;
    if (order.complaintDeadlineUtc) {
      deadlineMs = new Date(order.complaintDeadlineUtc).getTime();
    } else if (order.complaintWindowEndsAtUtc) {
      deadlineMs = new Date(order.complaintWindowEndsAtUtc).getTime();
    } else if (order.deliveredAtUtc) {
      deadlineMs = new Date(order.deliveredAtUtc).getTime() + 48 * 3600 * 1000;
    } else if (order.status === 'Delivered') {
      deadlineMs = new Date(order.createdAtUtc).getTime() + 48 * 3600 * 1000;
    }

    if (!deadlineMs) return null;

    const diffMs = deadlineMs - Date.now();
    const isProtected = diffMs > 0;
    const remainingHours = Math.max(0, Math.floor(diffMs / (3600 * 1000)));
    const remainingMins = Math.max(0, Math.floor((diffMs % (3600 * 1000)) / (60 * 1000)));

    return {
      isProtected,
      deadlineText: formatDate(new Date(deadlineMs).toISOString()),
      remainingText: isProtected ? `${remainingHours} giờ ${remainingMins} phút` : 'Đã kết thúc',
    };
  }, [order]);

  const canSubmitComplaint = useMemo(() => {
    if (!order) return false;
    if (order.canComplaint !== undefined) return order.canComplaint;
    return Boolean(protectionInfo?.isProtected && order.status === 'Delivered');
  }, [order, protectionInfo]);

  if (isLoading) {
    return (
      <PageContainer variant="wide" style={{ padding: '2rem 1rem 4rem' }}>
        <Skeleton width="180px" height="24px" style={{ marginBottom: '1.5rem' }} />
        <Skeleton width="100%" height="220px" style={{ borderRadius: '16px', marginBottom: '1.5rem' }} />
        <Skeleton width="100%" height="340px" style={{ borderRadius: '16px' }} />
      </PageContainer>
    );
  }

  if (error || !order) {
    return (
      <PageContainer variant="narrow" style={{ padding: '4rem 1rem' }}>
        <EmptyState
          title="Không tìm thấy đơn hàng"
          description={error || 'Đơn hàng không tồn tại hoặc bạn không có quyền truy cập.'}
          action={
            <Button variant="primary" onClick={() => navigate(ROUTES.MARKETPLACE.ORDERS)}>
              Quay lại danh sách đơn
            </Button>
          }
        />
      </PageContainer>
    );
  }

  const orderTotal = order.totalAmount ?? order.total ?? 0;

  return (
    <PageContainer variant="wide" style={{ padding: '1.5rem 1rem 4rem' }}>
      {/* Top Breadcrumb & Actions Bar */}
      <div
        className="no-print"
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.5rem',
          flexWrap: 'wrap',
          gap: '0.75rem',
        }}
      >
        <button
          type="button"
          onClick={() => navigate(ROUTES.MARKETPLACE.ORDERS)}
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.45rem',
            background: 'none',
            border: 'none',
            color: 'var(--text-dim)',
            fontSize: '0.9rem',
            cursor: 'pointer',
            padding: 0,
            fontWeight: 500,
          }}
        >
          <ArrowLeftIcon size={16} />
          Quay lại danh sách đơn hàng
        </button>

        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={handlePrintReceipt}
            icon={<FileTextIcon size={14} />}
          >
            In hóa đơn
          </Button>
        </div>
      </div>

      {/* Main Order Card */}
      <div
        className="card-glass"
        style={{
          borderRadius: '16px',
          border: '1px solid var(--border-color)',
          padding: '1.75rem',
          marginBottom: '1.75rem',
          background: 'var(--bg-card)',
        }}
      >
        {/* Header: Mã đơn + Trạng thái */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            borderBottom: '1px solid var(--border-color)',
            paddingBottom: '1.25rem',
            marginBottom: '1.25rem',
            flexWrap: 'wrap',
            gap: '1rem',
          }}
        >
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.4rem', flexWrap: 'wrap' }}>
              <PackageIcon size={22} style={{ color: 'var(--accent-primary)' }} />
              <h1 style={{ fontSize: '1.35rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                Đơn hàng #{order.orderNo}
              </h1>
              <OrderStatusBadge status={order.status} />
            </div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-dim)' }}>
              Đặt lúc: <strong style={{ color: 'var(--text-muted)' }}>{formatDate(order.createdAtUtc)}</strong>
            </div>
          </div>

          {/* Shop Reference Card */}
          {order.shopSlug ? (
            <Link
              to={`/shops/${order.shopSlug}`}
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.5rem',
                padding: '0.45rem 0.85rem',
                borderRadius: '10px',
                background: 'var(--bg-surface)',
                border: '1px solid var(--border-color)',
                fontSize: '0.85rem',
                color: 'var(--text-main)',
                fontWeight: 600,
              }}
            >
              <StoreIcon size={16} style={{ color: 'var(--accent-primary)' }} />
              <span>Gian hàng: {order.shopName || 'Cửa hàng'}</span>
              <ExternalLinkIcon size={12} style={{ color: 'var(--text-dim)' }} />
            </Link>
          ) : (
            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.5rem',
                padding: '0.45rem 0.85rem',
                borderRadius: '10px',
                background: 'var(--bg-surface)',
                border: '1px solid var(--border-color)',
                fontSize: '0.85rem',
                color: 'var(--text-main)',
                fontWeight: 600,
              }}
            >
              <StoreIcon size={16} style={{ color: 'var(--accent-primary)' }} />
              <span>{order.shopName || 'Gian hàng uy tín'}</span>
            </div>
          )}
        </div>

        {/* Banner Bảo Vệ Người Mua 48h */}
        <div
          style={{
            background: protectionInfo?.isProtected
              ? 'linear-gradient(135deg, rgba(16, 185, 129, 0.08) 0%, rgba(16, 185, 129, 0.02) 100%)'
              : 'rgba(255, 255, 255, 0.02)',
            border: `1px solid ${protectionInfo?.isProtected ? 'rgba(16, 185, 129, 0.3)' : 'var(--border-color)'}`,
            borderRadius: '12px',
            padding: '1rem 1.25rem',
            marginBottom: '1.5rem',
            display: 'flex',
            alignItems: 'flex-start',
            gap: '0.85rem',
          }}
        >
          <div
            style={{
              padding: '0.45rem',
              borderRadius: '8px',
              background: protectionInfo?.isProtected ? 'rgba(16, 185, 129, 0.15)' : 'rgba(255, 255, 255, 0.05)',
              color: protectionInfo?.isProtected ? '#10b981' : 'var(--text-dim)',
              display: 'flex',
            }}
          >
            <ShieldIcon size={20} />
          </div>

          <div style={{ flex: 1, fontSize: '0.86rem', lineHeight: '1.5' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.25rem', flexWrap: 'wrap', gap: '0.5rem' }}>
              <strong style={{ color: protectionInfo?.isProtected ? '#10b981' : 'var(--text-main)', fontSize: '0.92rem' }}>
                {protectionInfo?.isProtected
                  ? 'Chế độ Bảo vệ Người mua 48h đang kích hoạt'
                  : 'Thời hạn Bảo vệ Khiếu nại 48h đã kết thúc'}
              </strong>
              {protectionInfo && (
                <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem', color: 'var(--text-dim)', fontSize: '0.8rem' }}>
                  <ClockIcon size={14} />
                  <span>
                    {protectionInfo.isProtected
                      ? `Còn lại: ${protectionInfo.remainingText}`
                      : `Hạn bảo vệ: ${protectionInfo.deadlineText}`}
                  </span>
                </div>
              )}
            </div>
            <span style={{ color: 'var(--text-muted)' }}>
              {protectionInfo?.isProtected
                ? 'Tiền thanh toán được sàn giữ an toàn trong 48 giờ kể từ lúc giao hàng. Bạn có toàn quyền kích hoạt thử và gửi khiếu nại (báo mã lỗi/sai gói) để được đổi mới hoặc hoàn tiền 100%.'
                : 'Đơn hàng đã kết thúc thời hạn bảo vệ khiếu nại 48 giờ. Doanh thu đã được sàn giải phóng an toàn sang tài khoản của Gian hàng.'}
            </span>
          </div>
        </div>

        {/* Cụm công cụ thao tác hàng loạt cho mã số */}
        <div
          className="no-print"
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: '1.25rem',
            flexWrap: 'wrap',
            gap: '0.75rem',
            background: 'var(--bg-surface)',
            padding: '0.75rem 1rem',
            borderRadius: '10px',
            border: '1px solid var(--border-color)',
          }}
        >
          <div style={{ fontSize: '0.92rem', fontWeight: 700, color: 'var(--text-main)' }}>
            Kho mã số & Thông tin bàn giao
          </div>

          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            <Button
              type="button"
              variant="secondary"
              size="xs"
              onClick={handleToggleRevealAll}
              icon={allRevealed ? <EyeOffIcon size={14} /> : <EyeIcon size={14} />}
            >
              {allRevealed ? 'Ẩn tất cả mã' : 'Hiện tất cả mã'}
            </Button>

            <Button
              type="button"
              variant="secondary"
              size="xs"
              onClick={handleCopyAllCodes}
              icon={copiedKey === 'ALL' ? <CheckCheckIcon size={14} /> : <CopyIcon size={14} />}
            >
              {copiedKey === 'ALL' ? 'Đã sao chép tất cả' : 'Sao chép tất cả mã'}
            </Button>

            <Button
              type="button"
              variant="ghost"
              size="xs"
              onClick={handleDownloadBackupTxt}
              icon={<FileTextIcon size={14} />}
            >
              Tải tệp sao lưu (.txt)
            </Button>
          </div>
        </div>

        {/* Order Items & Credentials */}
        <div style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
            {order.items?.map((item) => {
              const itemPrice = item.price ?? item.unitPrice ?? 0;
              const itemTotal = item.total ?? item.lineTotal ?? itemPrice * item.quantity;
              const itemVariantLabel = item.variantName || item.variantLabel || 'Mặc định';

              return (
                <div
                  key={item.id}
                  style={{
                    background: 'var(--bg-surface)',
                    border: '1px solid var(--border-color)',
                    borderRadius: '12px',
                    padding: '1.25rem',
                  }}
                >
                  {/* Item Details Header */}
                  <div
                    style={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'baseline',
                      marginBottom: '0.85rem',
                      flexWrap: 'wrap',
                      gap: '0.5rem',
                    }}
                  >
                    <div>
                      <h3 style={{ fontSize: '1rem', fontWeight: 700, color: 'var(--text-main)', margin: '0 0 0.25rem' }}>
                        {item.productName}
                      </h3>
                      <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)' }}>
                        Gói: <strong style={{ color: 'var(--text-muted)' }}>{itemVariantLabel}</strong> • Số lượng:{' '}
                        <strong style={{ color: 'var(--text-muted)' }}>{item.quantity}</strong> • Đơn giá:{' '}
                        <strong style={{ color: 'var(--accent-primary)' }}>{formatPrice(itemPrice)}</strong>
                      </div>
                    </div>

                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.85rem' }}>
                      <span style={{ fontSize: '1.1rem', fontWeight: 800, color: 'var(--accent-primary)' }}>
                        {formatPrice(itemTotal)}
                      </span>

                      {/* Review Button */}
                      {(order.status === 'Delivered' || order.status === 'Completed') && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          disabled={!!reviewedItems[item.id]}
                          onClick={() => {
                            setReviewItem({
                              itemId: item.id,
                              prodName: item.productName,
                            });
                          }}
                          style={{
                            color: reviewedItems[item.id] ? '#10b981' : '#f59e0b',
                            borderColor: reviewedItems[item.id] ? 'rgba(16, 185, 129, 0.4)' : 'rgba(245, 158, 11, 0.4)',
                            fontSize: '0.8rem',
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '0.35rem',
                          }}
                        >
                          {reviewedItems[item.id] ? '✓ Đã đánh giá' : '⭐ Đánh giá sản phẩm'}
                        </Button>
                      )}

                      {/* Complaint Button */}
                      {canSubmitComplaint && (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => {
                            setComplaintItem({
                              itemId: item.id,
                              prodName: item.productName,
                            });
                          }}
                          style={{
                            color: '#f87171',
                            borderColor: 'rgba(239, 68, 68, 0.3)',
                            fontSize: '0.8rem',
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '0.35rem',
                          }}
                        >
                          <AlertCircleIcon size={14} />
                          Báo mã lỗi / Khiếu nại
                        </Button>
                      )}
                    </div>
                  </div>

                  {/* Decrypted Credentials Box */}
                  <OrderItemCredentials
                    item={item}
                    revealedKeys={revealedKeys}
                    toggleReveal={toggleReveal}
                    copyCode={copyCode}
                    copiedKey={copiedKey}
                    onDownloadCountIncrement={(orderItemId) => {
                      setOrder((prevOrder) => {
                        if (!prevOrder) return prevOrder;
                        return {
                          ...prevOrder,
                          items: prevOrder.items.map((it) => {
                            if (it.id === orderItemId) {
                              return {
                                ...it,
                                downloadCount: (it.downloadCount ?? 0) + 1,
                              };
                            }
                            return it;
                          }),
                        };
                      });
                    }}
                  />
                </div>
              );
            })}
          </div>
        </div>

        {/* Payment Summary Box */}
        <div
          style={{
            borderTop: '1px solid var(--border-color)',
            paddingTop: '1.25rem',
            display: 'flex',
            justifyContent: 'flex-end',
          }}
        >
          <div style={{ width: '100%', maxWidth: '360px', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.88rem', color: 'var(--text-dim)' }}>
              <span>Tổng tiền hàng:</span>
              <span style={{ color: 'var(--text-main)', fontWeight: 600 }}>{formatPrice(orderTotal)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.88rem', color: 'var(--text-dim)' }}>
              <span>Phí vận chuyển số:</span>
              <span style={{ color: '#34d399', fontWeight: 600 }}>Miễn phí (Giao tự động)</span>
            </div>
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                fontSize: '1.15rem',
                fontWeight: 800,
                color: 'var(--text-main)',
                borderTop: '1px dashed var(--border-color)',
                paddingTop: '0.75rem',
                marginTop: '0.25rem',
              }}
            >
              <span>Tổng thanh toán:</span>
              <span style={{ color: 'var(--accent-primary)' }}>{formatPrice(orderTotal)}</span>
            </div>
            <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)', textAlign: 'right', marginTop: '0.2rem' }}>
              Phương thức: Cổng VietQR Napas 24/7 (PayOS)
            </div>
          </div>
        </div>
      </div>

      {/* Modal Khiếu Nại (Báo Mã Lỗi) */}
      <OrderComplaintModal
        orderId={order.id}
        item={complaintItem}
        onClose={() => setComplaintItem(null)}
        onSuccess={fetchOrderDetail}
      />

      {/* Modal Đánh Giá Sản Phẩm (Product Review) */}
      <OrderReviewModal
        item={reviewItem}
        onClose={() => setReviewItem(null)}
        onSuccess={(itemId) => setReviewedItems((prev) => ({ ...prev, [itemId]: true }))}
      />
    </PageContainer>
  );
};
