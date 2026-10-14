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
  Badge,
  EyeIcon,
  EyeOffIcon,
  CopyIcon,
  CheckCheckIcon,
  AlertCircleIcon,
  FormField,
  Input,
  Select,
  Textarea,
  ExternalLinkIcon,
  ArrowLeftIcon,
  ShieldIcon,
  FileTextIcon,
  ClockIcon,
} from '@/components/common';

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

  // Modal khiếu nại
  const [complaintItem, setComplaintItem] = useState<{
    itemId: string;
    prodName: string;
  } | null>(null);
  const [complaintReason, setComplaintReason] = useState(1);
  const [complaintDesc, setComplaintDesc] = useState('');
  const [evidenceUrl, setEvidenceUrl] = useState('');
  const [isSubmittingComplaint, setIsSubmittingComplaint] = useState(false);
  const [complaintFeedback, setComplaintFeedback] = useState<{
    type: 'success' | 'error';
    msg: string;
  } | null>(null);

  // Modal đánh giá sản phẩm (Product Review)
  const [reviewItem, setReviewItem] = useState<{
    itemId: string;
    prodName: string;
  } | null>(null);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewHoverRating, setReviewHoverRating] = useState(0);
  const [reviewComment, setReviewComment] = useState('');
  const [isSubmittingReview, setIsSubmittingReview] = useState(false);
  const [reviewFeedback, setReviewFeedback] = useState<{
    type: 'success' | 'error';
    msg: string;
  } | null>(null);
  const [reviewedItems, setReviewedItems] = useState<Record<string, boolean>>({});

  // Tải file số an toàn qua Signed Expiring URL
  const [downloadingItemId, setDownloadingItemId] = useState<string | null>(null);
  const [downloadFeedback, setDownloadFeedback] = useState<{
    itemId: string;
    msg: string;
    type: 'success' | 'error';
  } | null>(null);

  const handleDownloadFile = async (orderItemId: string, item: OrderItemDto) => {
    if (item.maxDownloads && (item.downloadCount ?? 0) >= item.maxDownloads) {
      setDownloadFeedback({
        itemId: orderItemId,
        type: 'error',
        msg: `Đã đạt giới hạn tối đa ${item.maxDownloads} lần tải cho sản phẩm này.`,
      });
      return;
    }

    setDownloadingItemId(orderItemId);
    setDownloadFeedback(null);
    try {
      const res = await marketplaceApi.getSignedDownloadUrl(orderItemId);
      if (res?.downloadUrl) {
        window.open(res.downloadUrl, '_blank');
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
        setDownloadFeedback({
          itemId: orderItemId,
          type: 'success',
          msg: 'Đang mở liên kết tải về bảo mật (hết hạn trong 30 phút).',
        });
      }
    } catch (err: any) {
      setDownloadFeedback({
        itemId: orderItemId,
        type: 'error',
        msg: err?.response?.data?.message || 'Không thể tạo liên kết tải file. Vui lòng thử lại.',
      });
    } finally {
      setDownloadingItemId(null);
    }
  };

  const handleSendReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reviewItem) return;
    setIsSubmittingReview(true);
    setReviewFeedback(null);
    try {
      await marketplaceApi.createProductReview(reviewItem.itemId, {
        orderItemId: reviewItem.itemId,
        rating: reviewRating,
        comment: reviewComment,
      });
      setReviewedItems((prev) => ({ ...prev, [reviewItem.itemId]: true }));
      setReviewFeedback({
        type: 'success',
        msg: 'Cảm ơn bạn! Đánh giá của bạn đã được ghi nhận thành công.',
      });
      setTimeout(() => {
        setReviewItem(null);
        setReviewFeedback(null);
        setReviewComment('');
      }, 1500);
    } catch (err: any) {
      setReviewFeedback({
        type: 'error',
        msg: err?.response?.data?.message || 'Không thể gửi đánh giá. Vui lòng thử lại.',
      });
    } finally {
      setIsSubmittingReview(false);
    }
  };

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

  // Helper trích xuất mã số bản quyền hợp nhất (tương thích cả deliveredCodes mảng chuỗi và credentials)
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

    if (allCodes.length > 0) {
      navigator.clipboard.writeText(allCodes.join('\n'));
      setCopiedKey('ALL');
      setTimeout(() => setCopiedKey(null), 2500);
    }
  };

  // 3. Tải tệp dự phòng sao lưu (.txt)
  const handleDownloadBackupTxt = () => {
    if (!order) return;
    let content = `========================================================\n`;
    content += `CHI TIẾT ĐƠN HÀNG SẢN PHẨM SỐ: #${order.orderNo}\n`;
    content += `Thời gian mua hàng: ${formatDate(order.createdAtUtc)}\n`;
    content += `Gian hàng cung cấp: ${order.shopName}\n`;
    content += `Tổng thanh toán: ${formatPrice(order.totalAmount ?? order.total)}\n`;
    content += `Trạng thái: ${order.status}\n`;
    content += `========================================================\n\n`;

    order.items?.forEach((item, idx) => {
      content += `[MẶT HÀNG #${idx + 1}]: ${item.productName}\n`;
      content += `Biến thể: ${item.variantName || item.variantLabel || 'Mặc định'}\n`;
      content += `Số lượng: ${item.quantity}\n`;
      content += `Đơn giá: ${formatPrice(item.price ?? item.unitPrice)}\n`;
      content += `KHO MÃ SỐ BẢN QUYỀN / TÀI KHOẢN ĐÃ NHẬN:\n`;

      const creds = getItemCredentials(item);
      if (creds.length === 0) {
        content += `  (Đang xuất kho hoặc chưa có mã)\n`;
      } else {
        creds.forEach((c, cIdx) => {
          content += `  - Mã #${cIdx + 1}: ${c.codePlain}\n`;
          if (c.fileDownloadUrl) content += `    Tệp đính kèm: ${c.fileDownloadUrl}\n`;
          if (c.instructions) content += `    Hướng dẫn kích hoạt: ${c.instructions}\n`;
        });
      }
      content += `\n--------------------------------------------------------\n\n`;
    });

    content += `LƯU Ý BẢO MẬT:\n`;
    content += `- Giữ bí mật mã số và mật khẩu tài khoản bản quyền của bạn.\n`;
    content += `- Trong vòng 48h kể từ lúc nhận mã, nếu gặp sự cố kích hoạt, bạn có quyền gửi khiếu nại để được hỗ trợ hoặc hoàn tiền 100%.\n`;

    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Don-Hang-${order.orderNo}.txt`;
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

  const handleSendComplaint = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!order || !complaintItem || !complaintDesc.trim()) return;

    setIsSubmittingComplaint(true);
    setComplaintFeedback(null);
    try {
      const evidenceList = evidenceUrl.trim() ? [evidenceUrl.trim()] : [];
      await marketplaceApi.createComplaint(order.id, complaintItem.itemId, {
        reason: complaintReason,
        description: complaintDesc.trim(),
        faultyQuantity: 1,
        evidenceUrls: evidenceList,
      });
      setComplaintFeedback({
        type: 'success',
        msg: 'Đã gửi yêu cầu khiếu nại tới Gian hàng thành công. Gian hàng có 24h để phản hồi giải quyết.',
      });
      setTimeout(() => {
        setComplaintItem(null);
        setComplaintDesc('');
        setEvidenceUrl('');
        setComplaintFeedback(null);
        fetchOrderDetail();
      }, 1500);
    } catch (err: any) {
      setComplaintFeedback({
        type: 'error',
        msg: err?.response?.data?.message || 'Không thể gửi khiếu nại. Vui lòng thử lại sau.',
      });
    } finally {
      setIsSubmittingComplaint(false);
    }
  };

  const renderStatusBadge = (status: string) => {
    switch (status) {
      case 'Delivered':
        return <Badge variant="success" dot>Đã giao mã</Badge>;
      case 'Completed':
        return <Badge variant="primary">Đã hoàn tất</Badge>;
      case 'Paid':
        return <Badge variant="warning">Đang xuất kho</Badge>;
      case 'Pending':
        return <Badge variant="neutral">Chờ thanh toán</Badge>;
      case 'Cancelled':
        return <Badge variant="danger">Đã hủy</Badge>;
      case 'Refunded':
        return <Badge variant="warning">Đã hoàn tiền</Badge>;
      default:
        return <Badge variant="neutral">{status}</Badge>;
    }
  };

  if (isLoading) {
    return (
      <PageContainer variant="wide" style={{ padding: '2rem 1rem 4rem' }}>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <Skeleton height={36} width={260} />
          <Skeleton height={180} />
          <Skeleton height={280} />
        </div>
      </PageContainer>
    );
  }

  if (error || !order) {
    return (
      <PageContainer variant="wide" style={{ padding: '3rem 1rem' }}>
        <EmptyState
          icon={<AlertCircleIcon size={48} />}
          title="Không tìm thấy đơn hàng"
          description={error || 'Đơn hàng không tồn tại hoặc bạn không có quyền truy cập.'}
          action={
            <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
              <Button variant="secondary" size="md" onClick={() => navigate(ROUTES.MARKETPLACE.ORDERS)}>
                Quay lại Đơn mua
              </Button>
              <Button variant="primary" size="md" onClick={() => navigate(ROUTES.MARKETPLACE.INDEX)}>
                Khám phá Chợ số
              </Button>
            </div>
          }
        />
      </PageContainer>
    );
  }

  const orderTotal = order.totalAmount ?? order.total ?? 0;

  return (
    <PageContainer variant="wide" style={{ padding: '1.5rem 1rem 4rem' }}>
      {/* CSS In ấn (Print Stylesheet) */}
      <style>{`
        @media print {
          nav, header, footer, .no-print, button, a.btn {
            display: none !important;
          }
          body, .page-container {
            background: #fff !important;
            color: #000 !important;
            padding: 0 !important;
            margin: 0 !important;
          }
          .credential-box {
            border: 1px solid #ccc !important;
            background: #f9f9f9 !important;
            color: #000 !important;
            break-inside: avoid;
          }
          .credential-code-val span {
            color: #000 !important;
            letter-spacing: normal !important;
            font-size: 1rem !important;
          }
        }
      `}</style>

      {/* Breadcrumb & Navigation */}
      <div
        className="no-print"
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.25rem',
          flexWrap: 'wrap',
          gap: '0.75rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.88rem', color: 'var(--text-dim)' }}>
          <button
            type="button"
            onClick={() => navigate(ROUTES.MARKETPLACE.ORDERS)}
            className="btn btn-ghost btn-sm"
            style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem', padding: '0.35rem 0.65rem' }}
          >
            <ArrowLeftIcon size={16} />
            <span>Tất cả đơn mua</span>
          </button>
          <span>/</span>
          <span style={{ color: 'var(--text-main)', fontWeight: 600 }}>Chi tiết đơn #{order.orderNo}</span>
        </div>

        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          <Button variant="secondary" size="sm" onClick={handlePrintReceipt}>
            In biên lai
          </Button>
          <Button variant="ghost" size="sm" onClick={() => navigate(ROUTES.MARKETPLACE.INDEX)}>
            Tiếp tục mua sắm
          </Button>
        </div>
      </div>

      {/* Main Order Card */}
      <div
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          padding: '1.5rem 1.75rem',
          boxShadow: '0 8px 24px rgba(0, 0, 0, 0.25)',
          marginBottom: '1.5rem',
        }}
      >
        {/* Order Header */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            borderBottom: '1px solid var(--border-color)',
            paddingBottom: '1.25rem',
            marginBottom: '1.5rem',
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
              {renderStatusBadge(order.status)}
            </div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-dim)' }}>
              Đặt lúc: <strong style={{ color: 'var(--text-muted)' }}>{formatDate(order.createdAtUtc)}</strong>
            </div>
          </div>

          {/* Shop Reference Card */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.85rem',
              background: 'var(--bg-surface)',
              border: '1px solid var(--border-color)',
              padding: '0.65rem 1rem',
              borderRadius: '12px',
            }}
          >
            <div
              style={{
                width: '38px',
                height: '38px',
                borderRadius: '10px',
                background: 'var(--gradient-brand)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: '#fff',
                fontWeight: 800,
                fontSize: '1rem',
              }}
            >
              <StoreIcon size={20} />
            </div>
            <div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-dim)', textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                Gian hàng cung cấp
              </div>
              <div style={{ fontWeight: 700, color: 'var(--text-main)', fontSize: '0.92rem' }}>
                {order.shopSlug ? (
                  <Link
                    to={`/marketplace/shops/${order.shopSlug}`}
                    style={{ color: 'var(--text-main)', textDecoration: 'none' }}
                  >
                    {order.shopName}
                  </Link>
                ) : (
                  order.shopName
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Protection Alert / 48h Window Banner */}
        <div
          style={{
            background: protectionInfo?.isProtected
              ? 'linear-gradient(135deg, rgba(16, 185, 129, 0.08) 0%, rgba(14, 165, 233, 0.08) 100%)'
              : 'rgba(255, 255, 255, 0.02)',
            border: protectionInfo?.isProtected
              ? '1px solid rgba(16, 185, 129, 0.3)'
              : '1px solid var(--border-color)',
            borderRadius: '12px',
            padding: '1rem 1.25rem',
            marginBottom: '1.75rem',
            display: 'flex',
            alignItems: 'flex-start',
            gap: '0.85rem',
          }}
        >
          <ShieldIcon
            size={22}
            style={{
              color: protectionInfo?.isProtected ? '#34d399' : 'var(--text-dim)',
              flexShrink: 0,
              marginTop: '2px',
            }}
          />
          <div style={{ fontSize: '0.86rem', lineHeight: '1.5', flex: 1 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem', marginBottom: '0.3rem' }}>
              <strong style={{ color: protectionInfo?.isProtected ? '#34d399' : 'var(--text-main)' }}>
                {protectionInfo?.isProtected
                  ? 'Chính sách Gian Hàng Số: 48 Giờ Bảo Vệ Khiếu Nại Hoàn Tiền'
                  : 'Thời hạn bảo vệ 48h đã hoàn tất'}
              </strong>
              {protectionInfo && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
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

        {/* Cụm công cụ thao tác hàng loạt cho mã số (Batch Action Toolbar) */}
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

        {/* Order Items & Decrypted Credentials */}
        <div style={{ marginBottom: '1.5rem' }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
            {order.items?.map((item) => {
              const creds = getItemCredentials(item);
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
                            setReviewFeedback(null);
                            setReviewComment('');
                            setReviewRating(5);
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
                            setComplaintFeedback(null);
                            setComplaintDesc('');
                            setEvidenceUrl('');
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
                  {creds.length > 0 ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginTop: '1rem' }}>
                      {creds.map((cred, idx) => {
                        const keyId = `${item.id}-${idx}`;
                        const isRevealed = !!revealedKeys[keyId];

                        return (
                          <div key={idx} className="credential-box">
                            <div
                              style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                alignItems: 'center',
                                marginBottom: '0.45rem',
                                flexWrap: 'wrap',
                                gap: '0.5rem',
                              }}
                            >
                              <span style={{ fontSize: '0.82rem', color: '#34d399', fontWeight: 700 }}>
                                Mã kích hoạt / Tài khoản #{idx + 1}:
                              </span>
                              <div className="no-print" style={{ display: 'flex', gap: '0.45rem' }}>
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  onClick={() => toggleReveal(keyId)}
                                  aria-label={isRevealed ? 'Ẩn mã số' : 'Hiện mã số'}
                                  style={{
                                    padding: '0.25rem 0.65rem',
                                    fontSize: '0.78rem',
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    gap: '0.35rem',
                                  }}
                                >
                                  {isRevealed ? <EyeOffIcon size={14} /> : <EyeIcon size={14} />}
                                  {isRevealed ? 'Ẩn' : 'Hiện mã'}
                                </Button>

                                <Button
                                  variant="secondary"
                                  size="sm"
                                  onClick={() => copyCode(cred.codePlain, keyId)}
                                  aria-label="Sao chép mã"
                                  style={{
                                    padding: '0.25rem 0.65rem',
                                    fontSize: '0.78rem',
                                    display: 'inline-flex',
                                    alignItems: 'center',
                                    gap: '0.35rem',
                                  }}
                                >
                                  {copiedKey === keyId ? <CheckCheckIcon size={14} /> : <CopyIcon size={14} />}
                                  {copiedKey === keyId ? 'Đã sao chép' : 'Sao chép'}
                                </Button>
                              </div>
                            </div>

                            <div className="credential-code-val">
                              <span
                                style={{
                                  letterSpacing: isRevealed ? 'normal' : '0.15em',
                                  userSelect: isRevealed ? 'all' : 'none',
                                  fontFamily: 'monospace',
                                  fontSize: '0.95rem',
                                  wordBreak: 'break-all',
                                }}
                              >
                                {isRevealed ? cred.codePlain : '••••••••••••••••••••••••••••'}
                              </span>
                            </div>

                            {cred.fileDownloadUrl && (
                              <div style={{ marginTop: '0.75rem', padding: '0.65rem 0.85rem', background: 'rgba(56, 189, 248, 0.05)', border: '1px solid rgba(56, 189, 248, 0.15)', borderRadius: '8px' }}>
                                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '0.75rem', flexWrap: 'wrap' }}>
                                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.45rem' }}>
                                    <ShieldIcon size={16} style={{ color: '#38bdf8' }} />
                                    <span style={{ fontSize: '0.8rem', fontWeight: 600, color: 'var(--text-main)' }}>
                                      Tệp tin số bản quyền:
                                    </span>
                                  </div>

                                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                                    <span style={{ fontSize: '0.75rem', color: 'var(--text-dim)' }}>
                                      Đã tải:{' '}
                                      <strong style={{ color: item.maxDownloads && (item.downloadCount ?? 0) >= item.maxDownloads ? '#ef4444' : '#38bdf8' }}>
                                        {item.downloadCount ?? 0}
                                      </strong>
                                      {item.maxDownloads ? ` / ${item.maxDownloads} lần` : ' (Vô hạn)'}
                                    </span>

                                    <Button
                                      variant="secondary"
                                      size="sm"
                                      disabled={
                                        downloadingItemId === item.id ||
                                        (!!item.maxDownloads && (item.downloadCount ?? 0) >= item.maxDownloads)
                                      }
                                      onClick={() => handleDownloadFile(item.id, item)}
                                      style={{
                                        display: 'inline-flex',
                                        alignItems: 'center',
                                        gap: '0.4rem',
                                        fontSize: '0.78rem',
                                        padding: '0.35rem 0.75rem',
                                      }}
                                    >
                                      <ExternalLinkIcon size={14} />
                                      {downloadingItemId === item.id ? 'Đang tạo link an toàn...' : 'Tải về (Link ký 30p)'}
                                    </Button>
                                  </div>
                                </div>

                                {downloadFeedback && downloadFeedback.itemId === item.id && (
                                  <div
                                    style={{
                                      marginTop: '0.45rem',
                                      fontSize: '0.76rem',
                                      color: downloadFeedback.type === 'error' ? '#f87171' : '#34d399',
                                    }}
                                  >
                                    {downloadFeedback.msg}
                                  </div>
                                )}
                              </div>
                            )}

                            {cred.instructions && (
                              <div
                                style={{
                                  fontSize: '0.8rem',
                                  color: 'var(--text-dim)',
                                  marginTop: '0.6rem',
                                  background: 'rgba(0, 0, 0, 0.25)',
                                  padding: '0.5rem 0.75rem',
                                  borderRadius: '8px',
                                  borderLeft: '3px solid var(--accent-primary)',
                                }}
                              >
                                <strong style={{ color: 'var(--text-muted)' }}>Hướng dẫn kích hoạt:</strong>{' '}
                                {cred.instructions}
                              </div>
                            )}
                          </div>
                        );
                      })}
                    </div>
                  ) : (
                    <div
                      style={{
                        background: 'rgba(255, 255, 255, 0.03)',
                        padding: '0.85rem',
                        borderRadius: '8px',
                        color: 'var(--text-muted)',
                        fontSize: '0.85rem',
                        marginTop: '0.75rem',
                        textAlign: 'center',
                      }}
                    >
                      Đơn hàng đang trong tiến trình xuất kho tự động hoặc chưa hoàn tất thanh toán.
                    </div>
                  )}
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
      {complaintItem && (
        <div className="modal-backdrop" style={{ zIndex: 1000 }}>
          <div
            className="modal-card"
            style={{
              maxWidth: '520px',
              width: '100%',
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '16px',
              padding: '1.75rem',
              boxShadow: '0 20px 48px rgba(0, 0, 0, 0.5)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', marginBottom: '1rem' }}>
              <AlertCircleIcon size={22} style={{ color: '#f87171' }} />
              <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                Khiếu nại sản phẩm lỗi
              </h3>
            </div>

            <p style={{ fontSize: '0.86rem', color: 'var(--text-muted)', marginBottom: '1.25rem', lineHeight: '1.5' }}>
              Mặt hàng: <strong style={{ color: 'var(--text-main)' }}>{complaintItem.prodName}</strong>. Gian hàng sẽ nhận được thông báo và có 24 giờ để cấp mã thay thế hoặc hoàn tiền.
            </p>

            {complaintFeedback && (
              <div
                style={{
                  background:
                    complaintFeedback.type === 'success'
                      ? 'rgba(16, 185, 129, 0.15)'
                      : 'rgba(239, 68, 68, 0.15)',
                  border:
                    complaintFeedback.type === 'success'
                      ? '1px solid rgba(16, 185, 129, 0.4)'
                      : '1px solid rgba(239, 68, 68, 0.4)',
                  color: complaintFeedback.type === 'success' ? '#34d399' : '#f87171',
                  padding: '0.75rem 1rem',
                  borderRadius: '10px',
                  marginBottom: '1rem',
                  fontSize: '0.85rem',
                }}
              >
                {complaintFeedback.msg}
              </div>
            )}

            <form onSubmit={handleSendComplaint}>
              <FormField id="complaint-reason-select" label="Lý do khiếu nại" required>
                <Select
                  id="complaint-reason-select"
                  value={complaintReason}
                  onChange={(e) => setComplaintReason(Number(e.target.value))}
                >
                  <option value={1}>Mã bản quyền không hoạt động / Báo lỗi kích hoạt</option>
                  <option value={2}>Mã đã bị người khác sử dụng trước đó</option>
                  <option value={3}>Tài khoản không đăng nhập được / Sai mật khẩu</option>
                  <option value={4}>Mô tả sản phẩm không đúng với gói đã nhận</option>
                  <option value={5}>Tệp tin tải về bị hỏng / Không thể giải nén</option>
                </Select>
              </FormField>

              <FormField
                id="complaint-desc-input"
                label="Mô tả chi tiết tình trạng lỗi"
                required
                hint="Mô tả rõ mã lỗi phần mềm hoặc tình trạng gặp phải."
              >
                <Textarea
                  id="complaint-desc-input"
                  rows={3}
                  placeholder="Ví dụ: Nhập key vào phần mềm báo 'Code already redeemed' lúc 14:00..."
                  value={complaintDesc}
                  onChange={(e) => setComplaintDesc(e.target.value)}
                  required
                />
              </FormField>

              <FormField
                id="complaint-evidence-input"
                label="Đường dẫn ảnh/video bằng chứng (nếu có)"
                hint="Dán link ảnh chụp màn hình lỗi (Imgur, Cloudinary, Drive...)"
              >
                <Input
                  id="complaint-evidence-input"
                  placeholder="https://..."
                  value={evidenceUrl}
                  onChange={(e) => setEvidenceUrl(e.target.value)}
                />
              </FormField>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setComplaintItem(null);
                    setComplaintFeedback(null);
                  }}
                  disabled={isSubmittingComplaint}
                >
                  Hủy bỏ
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  loading={isSubmittingComplaint}
                  style={{ background: '#ef4444', borderColor: '#ef4444' }}
                >
                  Gửi khiếu nại
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Đánh Giá Sản Phẩm (Product Review) */}
      {reviewItem && (
        <div className="modal-backdrop" style={{ zIndex: 1000 }}>
          <div
            className="modal-card"
            style={{
              maxWidth: '520px',
              width: '100%',
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '16px',
              padding: '1.75rem',
              boxShadow: '0 20px 48px rgba(0, 0, 0, 0.5)',
            }}
          >
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', marginBottom: '1rem' }}>
              <span style={{ fontSize: '1.4rem' }}>⭐</span>
              <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                Đánh giá sản phẩm
              </h3>
            </div>

            <p style={{ fontSize: '0.86rem', color: 'var(--text-muted)', marginBottom: '1.25rem', lineHeight: '1.5' }}>
              Mặt hàng: <strong style={{ color: 'var(--text-main)' }}>{reviewItem.prodName}</strong>. Nhận xét chân thực của bạn sẽ giúp những người mua khác lựa chọn được sản phẩm phù hợp.
            </p>

            {reviewFeedback && (
              <div
                style={{
                  background:
                    reviewFeedback.type === 'success'
                      ? 'rgba(16, 185, 129, 0.15)'
                      : 'rgba(239, 68, 68, 0.15)',
                  border:
                    reviewFeedback.type === 'success'
                      ? '1px solid rgba(16, 185, 129, 0.4)'
                      : '1px solid rgba(239, 68, 68, 0.4)',
                  color: reviewFeedback.type === 'success' ? '#34d399' : '#f87171',
                  padding: '0.75rem 1rem',
                  borderRadius: '10px',
                  marginBottom: '1rem',
                  fontSize: '0.85rem',
                }}
              >
                {reviewFeedback.msg}
              </div>
            )}

            <form onSubmit={handleSendReview}>
              {/* Chọn số sao tương tác */}
              <div style={{ marginBottom: '1.25rem', textAlign: 'center' }}>
                <label style={{ display: 'block', fontSize: '0.88rem', fontWeight: 600, color: 'var(--text-main)', marginBottom: '0.5rem' }}>
                  Chất lượng sản phẩm & dịch vụ
                </label>
                <div style={{ display: 'inline-flex', gap: '0.5rem', fontSize: '2.2rem', cursor: 'pointer' }}>
                  {[1, 2, 3, 4, 5].map((star) => (
                    <span
                      key={star}
                      style={{
                        color: star <= (reviewHoverRating || reviewRating) ? '#f59e0b' : 'var(--border-color)',
                        transition: 'transform 0.1s ease, color 0.1s ease',
                      }}
                      onMouseEnter={() => setReviewHoverRating(star)}
                      onMouseLeave={() => setReviewHoverRating(0)}
                      onClick={() => setReviewRating(star)}
                    >
                      ★
                    </span>
                  ))}
                </div>
                <div style={{ fontSize: '0.82rem', color: '#f59e0b', fontWeight: 600, marginTop: '0.25rem' }}>
                  {reviewRating === 5 && 'Tuyệt vời, cực kỳ hài lòng!'}
                  {reviewRating === 4 && 'Hài lòng, sản phẩm tốt.'}
                  {reviewRating === 3 && 'Bình thường, dùng tạm được.'}
                  {reviewRating === 2 && 'Không hài lòng, cần cải thiện.'}
                  {reviewRating === 1 && 'Rất tệ, trải nghiệm thất vọng.'}
                </div>
              </div>

              <FormField
                id="review-comment-input"
                label="Cảm nhận chi tiết của bạn"
                required
                hint="Chia sẻ về độ chính xác của thông tin, tốc độ giao hàng, thái độ hỗ trợ..."
              >
                <Textarea
                  id="review-comment-input"
                  rows={4}
                  placeholder="Mã kích hoạt nhanh chóng, hoạt động tốt đúng như mô tả. Shop hỗ trợ nhiệt tình..."
                  value={reviewComment}
                  onChange={(e) => setReviewComment(e.target.value)}
                  required
                />
              </FormField>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setReviewItem(null);
                    setReviewFeedback(null);
                  }}
                  disabled={isSubmittingReview}
                >
                  Hủy bỏ
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  loading={isSubmittingReview}
                  style={{ background: '#f59e0b', borderColor: '#f59e0b', color: '#000', fontWeight: 700 }}
                >
                  Gửi đánh giá
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </PageContainer>
  );
};
