import React, { useState } from 'react';
import type { OrderItemDto, DeliveredCredentialDto } from '@/types/marketplace';
import { marketplaceApi } from '@/api/marketplaceApi';
import {
  Button,
  EyeIcon,
  EyeOffIcon,
  CopyIcon,
  CheckCheckIcon,
  ShieldIcon,
  ExternalLinkIcon,
} from '@/components/common';

interface OrderItemCredentialsProps {
  item: OrderItemDto;
  revealedKeys: Record<string, boolean>;
  toggleReveal: (keyId: string) => void;
  copyCode: (code: string, keyId: string) => void;
  copiedKey: string | null;
  onDownloadCountIncrement?: (orderItemId: string) => void;
}

export const OrderItemCredentials: React.FC<OrderItemCredentialsProps> = ({
  item,
  revealedKeys,
  toggleReveal,
  copyCode,
  copiedKey,
  onDownloadCountIncrement,
}) => {
  const [downloading, setDownloading] = useState(false);
  const [downloadFeedback, setDownloadFeedback] = useState<{ msg: string; type: 'success' | 'error' } | null>(null);

  // Helper trích xuất credentials
  const getItemCredentials = (it: OrderItemDto): DeliveredCredentialDto[] => {
    if (it.credentials && it.credentials.length > 0) {
      return it.credentials;
    }
    if (it.deliveredCodes && it.deliveredCodes.length > 0) {
      return it.deliveredCodes.map((code) => ({
        orderItemId: it.id,
        productName: it.productName,
        variantName: it.variantName || it.variantLabel || '',
        codePlain: code,
        fileDownloadUrl: it.downloadStoragePath || undefined,
        instructions: 'Nhập trực tiếp mã vào phần mềm hoặc liên kết bản quyền để kích hoạt.',
      }));
    }
    return [];
  };

  const creds = getItemCredentials(item);

  const handleDownloadFile = async () => {
    if (item.maxDownloads && (item.downloadCount ?? 0) >= item.maxDownloads) {
      setDownloadFeedback({
        type: 'error',
        msg: `Đã đạt giới hạn tối đa ${item.maxDownloads} lần tải cho sản phẩm này.`,
      });
      return;
    }

    setDownloading(true);
    setDownloadFeedback(null);
    try {
      const res = await marketplaceApi.getSignedDownloadUrl(item.id);
      if (res?.downloadUrl) {
        window.open(res.downloadUrl, '_blank');
        onDownloadCountIncrement?.(item.id);
        setDownloadFeedback({
          type: 'success',
          msg: 'Đang mở liên kết tải về bảo mật (hết hạn trong 30 phút).',
        });
      }
    } catch (err: any) {
      setDownloadFeedback({
        type: 'error',
        msg: err?.response?.data?.message || 'Không thể tạo liên kết tải file. Vui lòng thử lại.',
      });
    } finally {
      setDownloading(false);
    }
  };

  if (creds.length === 0) {
    return (
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
    );
  }

  return (
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
                      disabled={downloading || (!!item.maxDownloads && (item.downloadCount ?? 0) >= item.maxDownloads)}
                      onClick={handleDownloadFile}
                      style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '0.4rem',
                        fontSize: '0.78rem',
                        padding: '0.35rem 0.75rem',
                      }}
                    >
                      <ExternalLinkIcon size={14} />
                      {downloading ? 'Đang tạo link an toàn...' : 'Tải về (Link ký 30p)'}
                    </Button>
                  </div>
                </div>

                {downloadFeedback && (
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
  );
};
