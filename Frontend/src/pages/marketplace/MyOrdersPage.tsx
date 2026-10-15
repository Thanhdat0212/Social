import React, { useEffect, useState, useMemo } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { OrderDto, OrderItemDto, DeliveredCredentialDto } from '@/types/marketplace';
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
  FormField,
  Input,
  Select,
  Textarea,
  ExternalLinkIcon,
  SearchIcon,
} from '@/components/common';
import { OrderStatusBadge } from '@/components/marketplace/OrderStatusBadge';

export const MyOrdersPage: React.FC = () => {
  const navigate = useNavigate();
  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [copiedKey, setCopiedKey] = useState<string | null>(null);
  const [revealedKeys, setRevealedKeys] = useState<Record<string, boolean>>({});

  // State cho modal khiếu nại
  const [complaintOrder, setComplaintOrder] = useState<{
    orderId: string;
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

  const loadOrders = async () => {
    setIsLoading(true);
    try {
      const paramStatus = statusFilter !== 'all' ? statusFilter : undefined;
      const res = await marketplaceApi.getMyOrders(1, 30, paramStatus);
      setOrders(res.items || []);
    } catch (err) {
      console.error('Lỗi khi tải đơn hàng:', err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadOrders();
  }, [statusFilter]);

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

  const handleSendComplaint = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!complaintOrder || !complaintDesc.trim()) return;

    setIsSubmittingComplaint(true);
    setComplaintFeedback(null);
    try {
      const evidenceList = evidenceUrl.trim() ? [evidenceUrl.trim()] : [];
      await marketplaceApi.createComplaint(complaintOrder.orderId, complaintOrder.itemId, {
        reason: complaintReason,
        description: complaintDesc.trim(),
        faultyQuantity: 1,
        evidenceUrls: evidenceList,
      });
      setComplaintFeedback({
        type: 'success',
        msg: 'Đã gửi khiếu nại tới Gian hàng thành công. Bạn sẽ nhận được thông báo phản hồi trong vòng 24 giờ.',
      });
      setTimeout(() => {
        setComplaintOrder(null);
        setComplaintDesc('');
        setEvidenceUrl('');
        setComplaintFeedback(null);
        loadOrders();
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


  // Lọc theo từ khóa tìm kiếm (Mã đơn hoặc tên mặt hàng)
  const filteredOrders = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return orders;

    return orders.filter((o) => {
      if (o.orderNo?.toLowerCase().includes(q)) return true;
      if (o.shopName?.toLowerCase().includes(q)) return true;
      return o.items?.some((i) => i.productName?.toLowerCase().includes(q));
    });
  }, [orders, searchQuery]);

  return (
    <PageContainer variant="wide" style={{ padding: '1.5rem 1rem 4rem' }}>
      {/* Tiêu đề & Action */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.5rem',
          flexWrap: 'wrap',
          gap: '0.75rem',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
          <PackageIcon size={24} style={{ color: 'var(--accent-primary)' }} />
          <h1 style={{ fontSize: '1.35rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
            Đơn hàng & Kho mã đã nhận
          </h1>
        </div>
        <Button variant="secondary" size="sm" onClick={() => navigate('/marketplace')}>
          Khám phá Chợ số
        </Button>
      </div>

      {/* Thanh bộ lọc & Tìm kiếm */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.25rem',
          flexWrap: 'wrap',
          gap: '0.75rem',
          background: 'var(--bg-card)',
          padding: '0.85rem 1.25rem',
          borderRadius: '12px',
          border: '1px solid var(--border-color)',
        }}
      >
        {/* Status filter tabs */}
        <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
          {[
            { key: 'all', label: 'Tất cả' },
            { key: 'Delivered', label: 'Đã giao mã' },
            { key: 'Completed', label: 'Hoàn tất' },
            { key: 'Cancelled', label: 'Đã hủy' },
          ].map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setStatusFilter(tab.key)}
              style={{
                padding: '0.4rem 0.85rem',
                borderRadius: '8px',
                border: '1px solid',
                borderColor: statusFilter === tab.key ? 'var(--accent-primary)' : 'var(--border-color)',
                background: statusFilter === tab.key ? 'rgba(99, 102, 241, 0.15)' : 'transparent',
                color: statusFilter === tab.key ? 'var(--accent-primary)' : 'var(--text-dim)',
                fontWeight: statusFilter === tab.key ? 700 : 500,
                fontSize: '0.84rem',
                cursor: 'pointer',
                transition: 'all 0.15s ease',
              }}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Ô tìm kiếm đơn mua */}
        <div style={{ position: 'relative', width: '100%', maxWidth: '280px' }}>
          <SearchIcon
            size={16}
            style={{
              position: 'absolute',
              left: '0.75rem',
              top: '50%',
              transform: 'translateY(-50%)',
              color: 'var(--text-dim)',
            }}
          />
          <input
            type="text"
            placeholder="Tìm theo mã đơn, sản phẩm..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            style={{
              width: '100%',
              padding: '0.45rem 0.75rem 0.45rem 2.2rem',
              borderRadius: '8px',
              border: '1px solid var(--border-color)',
              background: 'var(--bg-surface)',
              color: 'var(--text-main)',
              fontSize: '0.85rem',
              outline: 'none',
            }}
          />
        </div>
      </div>

      {isLoading ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <Skeleton height={140} count={3} />
        </div>
      ) : filteredOrders.length === 0 ? (
        <EmptyState
          icon={<PackageIcon size={44} />}
          title={searchQuery ? 'Không tìm thấy đơn hàng phù hợp' : 'Bạn chưa có đơn hàng nào'}
          description={
            searchQuery
              ? `Không có kết quả nào khớp với từ khóa "${searchQuery}".`
              : 'Các sản phẩm số và mã bản quyền đã mua sẽ hiển thị tức thì tại đây ngay khi hệ thống xác nhận thanh toán thành công.'
          }
          action={
            searchQuery ? (
              <Button variant="secondary" size="md" onClick={() => setSearchQuery('')}>
                Xóa từ khóa tìm kiếm
              </Button>
            ) : (
              <Button variant="primary" size="md" onClick={() => navigate('/marketplace')}>
                Khám phá Chợ số
              </Button>
            )
          }
        />
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          {filteredOrders.map((order) => {
            const orderTotal = order.totalAmount ?? order.total ?? 0;

            return (
              <div
                key={order.id}
                style={{
                  background: 'var(--bg-card)',
                  border: '1px solid var(--border-color)',
                  borderRadius: '12px',
                  padding: '1.25rem 1.5rem',
                }}
              >
                {/* Header đơn hàng */}
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    borderBottom: '1px solid var(--border-color)',
                    paddingBottom: '0.85rem',
                    marginBottom: '1rem',
                    flexWrap: 'wrap',
                    gap: '0.6rem',
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', flexWrap: 'wrap' }}>
                    <StoreIcon size={18} style={{ color: 'var(--accent-primary)' }} />
                    <span style={{ fontWeight: 700, color: 'var(--text-main)', fontSize: '0.95rem' }}>
                      {order.shopName}
                    </span>
                    <span style={{ color: 'var(--text-dim)', fontSize: '0.82rem' }}>
                      • Đơn #{order.orderNo}
                    </span>
                    {order.createdAtUtc && (
                      <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>
                        • {formatDate(order.createdAtUtc)}
                      </span>
                    )}
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.85rem' }}>
                    <OrderStatusBadge status={order.status} />
                    <span style={{ fontSize: '1.05rem', fontWeight: 800, color: 'var(--accent-primary)' }}>
                      {formatPrice(orderTotal)}
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => navigate(`/orders/${order.id}`)}
                      style={{ fontSize: '0.78rem', padding: '0.25rem 0.6rem' }}
                    >
                      Chi tiết đơn →
                    </Button>
                  </div>
                </div>

                {/* Danh sách các sản phẩm và Mã đã giải mã */}
                {order.items?.map((item) => {
                  const creds = getItemCredentials(item);
                  const itemVariant = item.variantName || item.variantLabel || 'Mặc định';

                  return (
                    <div key={item.id} style={{ marginBottom: '1.25rem' }}>
                      <div
                        style={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'baseline',
                          marginBottom: '0.5rem',
                          flexWrap: 'wrap',
                          gap: '0.4rem',
                        }}
                      >
                        <div>
                          <span style={{ fontWeight: 700, color: 'var(--text-main)', fontSize: '0.92rem' }}>
                            {item.productName}
                          </span>
                          <span style={{ color: 'var(--text-dim)', fontSize: '0.82rem', marginLeft: '0.5rem' }}>
                            ({itemVariant} x {item.quantity})
                          </span>
                        </div>

                        {/* Nút khiếu nại */}
                        {order.canComplaint && (
                          <button
                            type="button"
                            onClick={() => {
                              setComplaintFeedback(null);
                              setComplaintDesc('');
                              setComplaintOrder({
                                orderId: order.id,
                                itemId: item.id,
                                prodName: item.productName,
                              });
                            }}
                            style={{
                              background: 'none',
                              border: 'none',
                              color: '#f87171',
                              fontSize: '0.78rem',
                              fontWeight: 600,
                              cursor: 'pointer',
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '0.3rem',
                              textDecoration: 'underline',
                            }}
                          >
                            <AlertCircleIcon size={14} />
                            Báo mã lỗi / Khiếu nại
                          </button>
                        )}
                      </div>

                      {/* Hộp hiển thị mã đã nhận */}
                      {creds.length > 0 ? (
                        creds.map((cred, idx) => {
                          const keyId = `${item.id}-${idx}`;
                          const isRevealed = !!revealedKeys[keyId];

                          return (
                            <div key={idx} className="credential-box">
                              <div
                                style={{
                                  display: 'flex',
                                  justifyContent: 'space-between',
                                  alignItems: 'center',
                                  marginBottom: '0.35rem',
                                  flexWrap: 'wrap',
                                  gap: '0.4rem',
                                }}
                              >
                                <span style={{ fontSize: '0.8rem', color: '#34d399', fontWeight: 700 }}>
                                  Mã bản quyền / Thông tin số #{idx + 1}:
                                </span>
                                <div style={{ display: 'flex', gap: '0.4rem' }}>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => toggleReveal(keyId)}
                                    aria-label={isRevealed ? 'Ẩn mã số' : 'Hiện mã số'}
                                    style={{
                                      padding: '0.2rem 0.55rem',
                                      fontSize: '0.75rem',
                                      display: 'inline-flex',
                                      alignItems: 'center',
                                      gap: '0.3rem',
                                    }}
                                  >
                                    {isRevealed ? <EyeOffIcon size={14} /> : <EyeIcon size={14} />}
                                    {isRevealed ? 'Ẩn' : 'Hiện'}
                                  </Button>

                                  <Button
                                    variant="secondary"
                                    size="sm"
                                    onClick={() => copyCode(cred.codePlain, keyId)}
                                    aria-label="Sao chép mã"
                                    style={{
                                      padding: '0.2rem 0.55rem',
                                      fontSize: '0.75rem',
                                      display: 'inline-flex',
                                      alignItems: 'center',
                                      gap: '0.3rem',
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
                                  }}
                                >
                                  {isRevealed ? cred.codePlain : '••••••••••••••••••••'}
                                </span>
                              </div>

                              {cred.fileDownloadUrl && (
                                <div style={{ marginTop: '0.45rem' }}>
                                  <a
                                    href={cred.fileDownloadUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="btn btn-secondary btn-sm"
                                    style={{
                                      display: 'inline-flex',
                                      alignItems: 'center',
                                      gap: '0.35rem',
                                      fontSize: '0.76rem',
                                      textDecoration: 'none',
                                    }}
                                  >
                                    <ExternalLinkIcon size={14} />
                                    Tải tệp đính kèm
                                  </a>
                                </div>
                              )}
                            </div>
                          );
                        })
                      ) : (
                        <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)', fontStyle: 'italic', padding: '0.25rem 0' }}>
                          Bấm <Link to={`/orders/${order.id}`} style={{ color: 'var(--accent-primary)', fontWeight: 600 }}>Chi tiết đơn</Link> để xem đầy đủ kho mã số đã giải mã và tải tệp tin bản quyền.
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            );
          })}
        </div>
      )}

      {/* Modal Khiếu nại */}
      {complaintOrder && (
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
            if (e.target === e.currentTarget && !isSubmittingComplaint) {
              setComplaintOrder(null);
            }
          }}
        >
          <div
            style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border-color)',
              borderRadius: '16px',
              maxWidth: '500px',
              width: '100%',
              padding: '1.75rem',
            }}
          >
            <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '0.75rem' }}>
              Khiếu nại sản phẩm lỗi
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginBottom: '1.25rem' }}>
              Mặt hàng: <strong>{complaintOrder.prodName}</strong>. Gian hàng sẽ nhận được thông báo và có 24 giờ để cấp mã thay thế hoặc hoàn tiền.
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
              <FormField id="complaint-reason-order" label="Lý do khiếu nại" required>
                <Select
                  id="complaint-reason-order"
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
                id="complaint-desc-order"
                label="Mô tả chi tiết tình trạng lỗi"
                required
                hint="Mô tả rõ mã lỗi phần mềm hoặc tình trạng gặp phải."
              >
                <Textarea
                  id="complaint-desc-order"
                  rows={3}
                  placeholder="Ví dụ: Nhập key vào phần mềm báo 'Code already redeemed' lúc 14:00..."
                  value={complaintDesc}
                  onChange={(e) => setComplaintDesc(e.target.value)}
                  required
                />
              </FormField>

              <FormField
                id="complaint-evidence-order"
                label="Đường dẫn ảnh/video bằng chứng (nếu có)"
                hint="Dán link ảnh chụp màn hình lỗi (Imgur, Cloudinary, Drive...)"
              >
                <Input
                  id="complaint-evidence-order"
                  placeholder="https://..."
                  value={evidenceUrl}
                  onChange={(e) => setEvidenceUrl(e.target.value)}
                />
              </FormField>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => setComplaintOrder(null)}
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
    </PageContainer>
  );
};
