import React, { useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { DigitalCodeItemDto } from '@/types/marketplace';
import { Button, Badge, Spinner } from '@/components/common';

interface InspectCodesModalProps {
  variant: { id: string; name: string } | null;
  onClose: () => void;
  onReloadStock: () => void;
}

export const InspectCodesModal: React.FC<InspectCodesModalProps> = ({
  variant,
  onClose,
  onReloadStock,
}) => {
  const [variantCodes, setVariantCodes] = useState<DigitalCodeItemDto[]>([]);
  const [codesLoading, setCodesLoading] = useState(false);

  const loadVariantCodes = async (variantId: string) => {
    setCodesLoading(true);
    try {
      const res = await marketplaceApi.getVariantCodes(variantId);
      setVariantCodes(res.items || res || []);
    } catch (err) {
      console.error('Lỗi nạp mã kho số:', err);
    } finally {
      setCodesLoading(false);
    }
  };

  const variantId = variant?.id;

  useEffect(() => {
    if (!variantId) return;
    loadVariantCodes(variantId);
    document.body.classList.add('modal-open');
    return () => {
      document.body.classList.remove('modal-open');
    };
  }, [variantId]);

  useEffect(() => {
    if (!variantId) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [variantId, onClose]);

  const handleInvalidateCode = async (codeId: string) => {
    if (!variant) return;
    if (!window.confirm('Xác nhận đánh dấu mã này là BỊ LỖI (Invalid) để chặn xuất kho cho khách?')) return;
    try {
      await marketplaceApi.invalidateCode(variant.id, codeId);
      alert('Đã vô hiệu hóa mã số thành công.');
      loadVariantCodes(variant.id);
      onReloadStock();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi vô hiệu hóa mã.');
    }
  };

  if (!variant) return null;

  return (
    <div
      className="modal-overlay"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
    >
      <div
        className="modal-content"
        style={{
          maxWidth: '650px',
          width: '100%',
          padding: '1.75rem',
          maxHeight: '85vh',
          overflowY: 'auto',
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <div>
            <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
              Kho mã số bảo mật
            </h3>
            <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              Biến thể: <strong>{variant.name}</strong>
            </p>
          </div>
          <Button variant="ghost" size="xs" onClick={onClose}>Đóng</Button>
        </div>

        {codesLoading ? (
          <div style={{ textAlign: 'center', padding: '2rem' }}><Spinner size="md" /></div>
        ) : variantCodes.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>
            Chưa có mã nào trong kho của biến thể này. Bấm <strong>+ Nạp kho</strong> để thêm mã.
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.85rem' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-color)', textAlign: 'left', color: 'var(--text-dim)' }}>
                  <th style={{ padding: '0.6rem 0.5rem' }}>Mã đã che</th>
                  <th style={{ padding: '0.6rem 0.5rem' }}>Trạng thái</th>
                  <th style={{ padding: '0.6rem 0.5rem' }}>Ngày nạp</th>
                  <th style={{ padding: '0.6rem 0.5rem', textAlign: 'right' }}>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {variantCodes.map((code) => (
                  <tr key={code.id} style={{ borderBottom: '1px solid var(--border-color)' }}>
                    <td style={{ padding: '0.6rem 0.5rem', fontFamily: 'monospace', fontWeight: 700 }}>
                      {code.maskedValue}
                    </td>
                    <td style={{ padding: '0.6rem 0.5rem' }}>
                      <Badge
                        variant={code.status === 'Available' ? 'success' : code.status === 'Delivered' ? 'primary' : code.status === 'Reserved' ? 'warning' : 'danger'}
                        size="sm"
                      >
                        {code.status}
                      </Badge>
                    </td>
                    <td style={{ padding: '0.6rem 0.5rem', color: 'var(--text-dim)', fontSize: '0.78rem' }}>
                      {new Date(code.createdAtUtc).toLocaleDateString('vi-VN')}
                    </td>
                    <td style={{ padding: '0.6rem 0.5rem', textAlign: 'right' }}>
                      {code.status === 'Available' && (
                        <Button
                          type="button"
                          variant="danger"
                          size="xs"
                          onClick={() => handleInvalidateCode(code.id)}
                        >
                          Báo lỗi (Hủy)
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
