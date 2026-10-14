import React, { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { marketplaceApi } from '@/api/marketplaceApi';
import { ROUTES } from '@/constants/routes';
import {
  Spinner,
  Button,
  PageContainer,
  AlertCircleIcon,
  CheckCheckIcon,
  CopyIcon,
  ClockIcon,
} from '@/components/common';
import { signalrService } from '@/services/signalrService';

export const CheckoutPage: React.FC = () => {
  const { id: checkoutId } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [paymentData, setPaymentData] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isVerifying, setIsVerifying] = useState(false);
  const [timeLeft, setTimeLeft] = useState(15 * 60); // 15 phút đếm ngược
  const [copySuccess, setCopySuccess] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [isMockPaying, setIsMockPaying] = useState(false);

  // Giả lập thanh toán M5.5 (Dev Mode)
  const handleMockPay = async (result: 'success' | 'fail' = 'success') => {
    if (!checkoutId) return;
    setIsMockPaying(true);
    try {
      await marketplaceApi.devMockPay(checkoutId, result);
      if (result === 'success') {
        setErrorMsg(null);
        setIsSuccess(true);
      } else {
        setErrorMsg('Thanh toán giả lập thất bại.');
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi gọi giả lập thanh toán.');
    } finally {
      setIsMockPaying(false);
    }
  };

  // Khởi tạo link thanh toán VietQR PayOS
  useEffect(() => {
    const initPayment = async () => {
      if (!checkoutId) return;
      setIsLoading(true);
      setErrorMsg(null);
      try {
        const data = await marketplaceApi.createPayOsPayment(checkoutId);
        setPaymentData(data);
      } catch (err: any) {
        setErrorMsg(err.response?.data?.message || 'Không thể tạo mã VietQR cho phiên thanh toán.');
      } finally {
        setIsLoading(false);
      }
    };

    initPayment();
  }, [checkoutId]);

  // Bộ đếm ngược 15 phút
  useEffect(() => {
    if (timeLeft <= 0) return;
    const timer = setInterval(() => {
      setTimeLeft((prev) => prev - 1);
    }, 1000);
    return () => clearInterval(timer);
  }, [timeLeft]);

  // Lắng nghe WebSocket Realtime từ SignalR khi thanh toán thành công
  useEffect(() => {
    if (!checkoutId) return;

    // Gia nhập group checkout để nhận thông báo thanh toán
    signalrService.joinCheckoutGroup(checkoutId);

    const handlePaymentSucceeded = (data: any) => {
      if (data?.checkoutId === checkoutId || data?.CheckoutId === checkoutId) {
        setIsSuccess(true);
      }
    };

    signalrService.on('PaymentSucceeded', handlePaymentSucceeded);

    return () => {
      signalrService.leaveCheckoutGroup(checkoutId);
      signalrService.off('PaymentSucceeded', handlePaymentSucceeded);
    };
  }, [checkoutId]);

  // Kiểm tra trạng thái thanh toán (Verify Poll)
  const checkPaymentStatus = useCallback(async () => {
    if (!checkoutId || isSuccess) return;
    try {
      const res = await marketplaceApi.verifyPayment(checkoutId);
      if (res.status === 'Succeeded' || String(res.status) === '2' || (res as any).status === 'Paid') {
        setIsSuccess(true);
      }
    } catch {
      // Đang chờ chuyển khoản
    }
  }, [checkoutId, isSuccess]);

  // Tự động kiểm tra mỗi 3 giây
  useEffect(() => {
    if (isSuccess || isLoading || !paymentData) return;
    const interval = setInterval(checkPaymentStatus, 3000);
    return () => clearInterval(interval);
  }, [checkPaymentStatus, isSuccess, isLoading, paymentData]);

  const handleManualCheck = async () => {
    setIsVerifying(true);
    await checkPaymentStatus();
    setIsVerifying(false);
  };

  const copyToClipboard = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    setCopySuccess(label);
    setTimeout(() => setCopySuccess(null), 2000);
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('vi-VN').format(price) + ' đ';
  };

  const minutes = Math.floor(timeLeft / 60);
  const seconds = timeLeft % 60;

  if (isLoading) {
    return (
      <PageContainer variant="narrow">
        <div
          style={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            padding: '5rem 0',
          }}
        >
          <Spinner size="lg" />
          <p style={{ marginTop: '1rem', color: 'var(--text-muted)' }}>
            Đang tạo mã VietQR bảo mật...
          </p>
        </div>
      </PageContainer>
    );
  }

  // Màn hình thành công
  if (isSuccess) {
    return (
      <PageContainer variant="narrow">
        <div
          className="qr-checkout-card"
          style={{
            borderColor: 'rgba(16, 185, 129, 0.4)',
            textAlign: 'center',
            padding: '2.5rem 2rem',
            marginTop: '2rem',
          }}
        >
          <div
            style={{
              width: '56px',
              height: '56px',
              borderRadius: '50%',
              background: 'rgba(16, 185, 129, 0.15)',
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              marginBottom: '1rem',
            }}
          >
            <CheckCheckIcon size={28} style={{ color: 'var(--success)' }} />
          </div>
          <h1
            style={{
              fontSize: '1.5rem',
              color: 'var(--success)',
              fontWeight: 800,
              marginBottom: '0.75rem',
            }}
          >
            Thanh toán thành công!
          </h1>
          <p style={{ color: 'var(--text-muted)', lineHeight: 1.6, marginBottom: '2rem', fontSize: '0.9rem' }}>
            Hệ thống đã tự động xuất kho mã kích hoạt số cho đơn hàng của bạn. Tiền thanh toán đang được giữ an toàn trong thời gian bảo hành 48 giờ.
          </p>
          <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
            <Button
              type="button"
              variant="primary"
              size="md"
              onClick={() => navigate(ROUTES.MARKETPLACE.ORDERS)}
            >
              Xem & Nhận mã bản quyền ngay
            </Button>
          </div>
        </div>
      </PageContainer>
    );
  }

  if (errorMsg) {
    return (
      <PageContainer variant="narrow">
        <div
          style={{
            textAlign: 'center',
            padding: '3rem 1.5rem',
            background: 'var(--bg-card)',
            border: '1px solid var(--border-color)',
            borderRadius: '16px',
            marginTop: '2rem',
          }}
        >
          <AlertCircleIcon size={44} style={{ color: 'var(--danger)', marginBottom: '1rem' }} />
          <h2 style={{ color: 'var(--text-main)', marginBottom: '0.5rem', fontSize: '1.25rem' }}>
            Không thể thanh toán
          </h2>
          <p style={{ color: 'var(--text-muted)', marginBottom: '1.5rem', fontSize: '0.9rem' }}>
            {errorMsg}
          </p>
          <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap' }}>
            <Link to={ROUTES.MARKETPLACE.CART} className="btn btn-secondary">
              Quay lại Giỏ hàng
            </Link>
            <Button
              type="button"
              variant="primary"
              onClick={() => handleMockPay('success')}
              loading={isMockPaying}
            >
              🧪 Thanh toán thử (Dev Mode)
            </Button>
          </div>
        </div>
      </PageContainer>
    );
  }

  return (
    <PageContainer variant="narrow">
      <div className="qr-checkout-card" style={{ marginTop: '1.5rem' }}>
        {/* Countdown Timer */}
        <div
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.4rem',
            background: 'rgba(245, 158, 11, 0.15)',
            border: '1px solid rgba(245, 158, 11, 0.3)',
            color: 'var(--warning)',
            padding: '0.35rem 0.85rem',
            borderRadius: '999px',
            fontSize: '0.82rem',
            fontWeight: 700,
            marginBottom: '1rem',
          }}
        >
          <ClockIcon size={14} />
          <span>
            Thời gian giữ mã: {minutes.toString().padStart(2, '0')}:{seconds.toString().padStart(2, '0')}
          </span>
        </div>

        <h1
          style={{
            fontSize: '1.4rem',
            fontWeight: 800,
            color: 'var(--text-main)',
            marginBottom: '0.25rem',
          }}
        >
          Quét mã VietQR để thanh toán
        </h1>
        <p style={{ color: 'var(--text-muted)', fontSize: '0.88rem' }}>
          Mở ứng dụng ngân hàng hoặc ví điện tử bất kỳ để quét mã QR bên dưới.
        </p>

        {/* Khung mã QR */}
        <div className="qr-frame">
          {paymentData?.qrCodeUrl ? (
            <img
              src={
                paymentData.qrCodeUrl.startsWith('http')
                  ? paymentData.qrCodeUrl
                  : `https://api.qrserver.com/v1/create-qr-code/?size=350x350&data=${encodeURIComponent(paymentData.qrCodeUrl)}`
              }
              alt="VietQR PayOS"
              className="qr-code-img"
            />
          ) : (
            <div
              style={{
                width: 240,
                height: 240,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--text-dim)',
              }}
            >
              Mã QR VietQR
            </div>
          )}
        </div>

        {copySuccess && (
          <div
            style={{
              color: 'var(--success)',
              fontSize: '0.82rem',
              fontWeight: 600,
              marginBottom: '0.5rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              gap: '0.3rem',
            }}
          >
            <CheckCheckIcon size={14} />
            <span>Đã sao chép {copySuccess} vào bộ nhớ tạm!</span>
          </div>
        )}

        {/* Bảng sao chép thông tin chuyển khoản thủ công */}
        <div className="payment-copy-grid">
          <div className="copy-field">
            <span className="copy-field-lbl">Số tiền cần chuyển:</span>
            <span className="copy-field-val" style={{ color: 'var(--accent-primary)' }}>
              {formatPrice(paymentData?.amount || 0)}
              <button
                type="button"
                onClick={() => copyToClipboard(String(paymentData?.amount || 0), 'Số tiền')}
                className="btn btn-ghost btn-sm"
                style={{ padding: '0.15rem 0.45rem', fontSize: '0.75rem', display: 'inline-flex', alignItems: 'center', gap: '0.25rem' }}
                aria-label="Sao chép số tiền"
              >
                <CopyIcon size={12} />
                Sao chép
              </button>
            </span>
          </div>

          <div className="copy-field">
            <span className="copy-field-lbl">Nội dung chuyển khoản (bắt buộc):</span>
            <span className="copy-field-val" style={{ color: 'var(--warning)' }}>
              {paymentData?.paymentCode || paymentData?.description || 'PAY'}
              <button
                type="button"
                onClick={() =>
                  copyToClipboard(
                    paymentData?.paymentCode || paymentData?.description || '',
                    'Nội dung CK'
                  )
                }
                className="btn btn-ghost btn-sm"
                style={{ padding: '0.15rem 0.45rem', fontSize: '0.75rem', display: 'inline-flex', alignItems: 'center', gap: '0.25rem' }}
                aria-label="Sao chép nội dung chuyển khoản"
              >
                <CopyIcon size={12} />
                Sao chép
              </button>
            </span>
          </div>

          <div className="copy-field">
            <span className="copy-field-lbl">Số tài khoản nhận:</span>
            <span className="copy-field-val">
              {paymentData?.accountNumber || 'PAYOS_ACC'}
              <button
                type="button"
                onClick={() => copyToClipboard(paymentData?.accountNumber || '', 'Số tài khoản')}
                className="btn btn-ghost btn-sm"
                style={{ padding: '0.15rem 0.45rem', fontSize: '0.75rem', display: 'inline-flex', alignItems: 'center', gap: '0.25rem' }}
                aria-label="Sao chép số tài khoản"
              >
                <CopyIcon size={12} />
                Sao chép
              </button>
            </span>
          </div>

          <div className="copy-field">
            <span className="copy-field-lbl">Tên người nhận:</span>
            <span className="copy-field-val" style={{ fontSize: '0.85rem' }}>
              {paymentData?.accountName || 'PAYOS GATEWAY'}
            </span>
          </div>
        </div>

        {/* Nút kiểm tra thủ công */}
        <div style={{ marginTop: '1.5rem' }}>
          <Button
            type="button"
            variant="outline"
            block
            onClick={handleManualCheck}
            loading={isVerifying}
          >
            Tôi đã chuyển khoản - Kiểm tra ngay
          </Button>
        </div>

        {/* Khung Thanh toán Thử nghiệm Dev (M5.5) */}
        <div
          style={{
            marginTop: '1.25rem',
            padding: '1rem',
            background: 'rgba(14, 165, 233, 0.08)',
            border: '1px dashed rgba(14, 165, 233, 0.35)',
            borderRadius: '12px',
            textAlign: 'center',
          }}
        >
          <div style={{ fontSize: '0.82rem', fontWeight: 700, color: 'var(--accent-primary)', marginBottom: '0.6rem' }}>
            🧪 Chế độ thử nghiệm Dev (Mock Payment M5.5)
          </div>
          <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap' }}>
            <Button
              type="button"
              variant="primary"
              size="sm"
              onClick={() => handleMockPay('success')}
              loading={isMockPaying}
            >
              Giả lập Thanh toán Thành công
            </Button>
            <Button
              type="button"
              variant="danger"
              size="sm"
              onClick={() => handleMockPay('fail')}
              loading={isMockPaying}
            >
              Giả lập Thất bại
            </Button>
          </div>
        </div>

        <div style={{ marginTop: '1rem', textAlign: 'center', fontSize: '0.75rem', color: 'var(--text-dim)' }}>
          Hệ thống đang tự động lắng nghe Webhook ngân hàng. Đơn hàng sẽ tự động xác nhận ngay khi tiền vào.
        </div>
      </div>
    </PageContainer>
  );
};
