import React, { useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import {
  Button,
  Input,
  Textarea,
  Spinner,
  Alert,
} from '@/components/common';

interface CreateManualOrderModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const CreateManualOrderModal: React.FC<CreateManualOrderModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [products, setProducts] = useState<any[]>([]);
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  // Form states
  const [selectedProductId, setSelectedProductId] = useState<string>('');
  const [selectedVariantId, setSelectedVariantId] = useState<string>('');
  const [quantity, setQuantity] = useState<number>(1);
  const [customPrice, setCustomPrice] = useState<string>('');
  const [customerEmail, setCustomerEmail] = useState<string>('');
  const [customerName, setCustomerName] = useState<string>('');
  const [customerPhone, setCustomerPhone] = useState<string>('');
  const [note, setNote] = useState<string>('');

  const loadShopProducts = async () => {
    setLoadingProducts(true);
    try {
      const res = await marketplaceApi.getShopProducts(1, 100);
      const items = res.items || res || [];
      setProducts(items);

      if (items.length > 0) {
        setSelectedProductId(items[0].id);
        if (items[0].variants && items[0].variants.length > 0) {
          setSelectedVariantId(items[0].variants[0].id);
          setCustomPrice(items[0].variants[0].price.toString());
        }
      }
    } catch (err) {
      console.error('Error loading shop products:', err);
    } finally {
      setLoadingProducts(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      loadShopProducts();
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;
    document.body.classList.add('modal-open');
    return () => {
      document.body.classList.remove('modal-open');
    };
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !submitting) onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose, submitting]);

  if (!isOpen) return null;

  const currentProduct = products.find((p) => p.id === selectedProductId);
  const currentVariant = currentProduct?.variants?.find((v: any) => v.id === selectedVariantId);

  const handleProductChange = (prodId: string) => {
    setSelectedProductId(prodId);
    const prod = products.find((p) => p.id === prodId);
    if (prod?.variants && prod.variants.length > 0) {
      setSelectedVariantId(prod.variants[0].id);
      setCustomPrice(prod.variants[0].price.toString());
    } else {
      setSelectedVariantId('');
      setCustomPrice('');
    }
  };

  const handleVariantChange = (varId: string) => {
    setSelectedVariantId(varId);
    const v = currentProduct?.variants?.find((item: any) => item.id === varId);
    if (v) {
      setCustomPrice(v.price.toString());
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedVariantId) {
      setErrorMsg('Vui lòng chọn biến thể sản phẩm cần bán.');
      return;
    }
    if (!customerEmail.trim()) {
      setErrorMsg('Vui lòng nhập địa chỉ email người nhận.');
      return;
    }
    if (quantity <= 0) {
      setErrorMsg('Số lượng phải lớn hơn 0.');
      return;
    }

    setSubmitting(true);
    setErrorMsg('');

    try {
      await marketplaceApi.createManualOrder({
        variantId: selectedVariantId,
        quantity,
        customUnitPrice: customPrice ? parseInt(customPrice, 10) : undefined,
        customerEmail: customerEmail.trim(),
        customerName: customerName.trim() || undefined,
        customerPhone: customerPhone.trim() || undefined,
        note: note.trim() || undefined,
      });

      alert('Đã tạo đơn hàng bán lẻ thủ công thành công! Mã kho số đã được tự động trừ.');
      onSuccess();
      onClose();
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Có lỗi xảy ra khi tạo đơn hàng thủ công.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div
      className="modal-overlay"
      onClick={() => {
        if (!submitting) onClose();
      }}
      role="dialog"
      aria-modal="true"
    >
      <div
        className="modal-content"
        style={{
          maxWidth: '540px',
          width: '100%',
          maxHeight: '90vh',
          display: 'flex',
          flexDirection: 'column',
          borderRadius: '16px',
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div
          style={{
            padding: '1.25rem 1.5rem',
            borderBottom: '1px solid var(--border-color)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div>
            <h3 style={{ margin: 0, fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)' }}>
              Tạo Đơn Bán Lẻ Thủ Công (Quick POS)
            </h3>
            <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
              Nhập đơn cho khách mua trực tiếp, khách chuyển khoản ngoài hoặc bán qua tin nhắn
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              fontSize: '1.5rem',
              color: 'var(--text-dim)',
              cursor: 'pointer',
              lineHeight: 1,
            }}
          >
            ×
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} style={{ padding: '1.5rem', overflowY: 'auto', flex: 1 }}>
          {errorMsg && <Alert type="error" message={errorMsg} className="mb-4" />}

          {loadingProducts ? (
            <div style={{ textAlign: 'center', padding: '2rem' }}>
              <Spinner size="md" />
              <p style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: 'var(--text-dim)' }}>
                Đang nạp danh mục sản phẩm của gian hàng...
              </p>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {/* Chọn sản phẩm */}
              <div>
                <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                  Sản phẩm bán *
                </label>
                <select
                  value={selectedProductId}
                  onChange={(e) => handleProductChange(e.target.value)}
                  style={{
                    width: '100%',
                    padding: '0.65rem 0.85rem',
                    background: 'var(--bg-card-subtle, rgba(255,255,255,0.03))',
                    border: '1px solid var(--border-color)',
                    borderRadius: '10px',
                    color: 'var(--text-main)',
                    fontSize: '0.88rem',
                  }}
                >
                  {products.map((p) => (
                    <option key={p.id} value={p.id} style={{ background: '#1e293b', color: '#fff' }}>
                      {p.name}
                    </option>
                  ))}
                </select>
              </div>

              {/* Chọn biến thể */}
              {currentProduct?.variants && currentProduct.variants.length > 0 && (
                <div>
                  <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                    Biến thể / Gói SKU *
                  </label>
                  <select
                    value={selectedVariantId}
                    onChange={(e) => handleVariantChange(e.target.value)}
                    style={{
                      width: '100%',
                      padding: '0.65rem 0.85rem',
                      background: 'var(--bg-card-subtle, rgba(255,255,255,0.03))',
                      border: '1px solid var(--border-color)',
                      borderRadius: '10px',
                      color: 'var(--text-main)',
                      fontSize: '0.88rem',
                    }}
                  >
                    {currentProduct.variants.map((v: any) => (
                      <option key={v.id} value={v.id} style={{ background: '#1e293b', color: '#fff' }}>
                        {v.sku ? `[${v.sku}] ` : ''}{v.name || v.combinationKey || 'Mặc định'} - Giá: {v.price?.toLocaleString('vi-VN')} đ
                      </option>
                    ))}
                  </select>
                </div>
              )}

              {/* Số lượng & Đơn giá */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                    Số lượng xuất kho *
                  </label>
                  <Input
                    type="number"
                    min={1}
                    value={quantity}
                    onChange={(e) => setQuantity(Math.max(1, parseInt(e.target.value, 10) || 1))}
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                    Đơn giá thực thu (đ)
                  </label>
                  <Input
                    type="number"
                    value={customPrice}
                    placeholder="VD: 150000"
                    onChange={(e) => setCustomPrice(e.target.value)}
                  />
                </div>
              </div>

              {/* Thông tin khách hàng */}
              <div>
                <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                  Email người nhận *
                </label>
                <Input
                  type="email"
                  placeholder="khachhang@example.com"
                  value={customerEmail}
                  onChange={(e) => setCustomerEmail(e.target.value)}
                  required
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                <div>
                  <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                    Tên người mua
                  </label>
                  <Input
                    type="text"
                    placeholder="Nguyễn Văn A"
                    value={customerName}
                    onChange={(e) => setCustomerName(e.target.value)}
                  />
                </div>

                <div>
                  <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                    Số điện thoại
                  </label>
                  <Input
                    type="tel"
                    placeholder="0987654321"
                    value={customerPhone}
                    onChange={(e) => setCustomerPhone(e.target.value)}
                  />
                </div>
              </div>

              {/* Ghi chú */}
              <div>
                <label style={{ display: 'block', fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '0.35rem' }}>
                  Ghi chú đơn hàng (Tùy chọn)
                </label>
                <Textarea
                  rows={2}
                  placeholder="VD: Khách chuyển khoản VCB, bán qua Zalo..."
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </div>

              {/* Tính nhẩm tổng tiền */}
              <div
                style={{
                  background: 'rgba(56, 189, 248, 0.08)',
                  border: '1px solid rgba(56, 189, 248, 0.2)',
                  borderRadius: '10px',
                  padding: '0.75rem 1rem',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <span style={{ fontSize: '0.85rem', color: 'var(--text-dim)' }}>Tổng thanh toán đơn:</span>
                <span style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--accent-primary)' }}>
                  {((parseInt(customPrice, 10) || currentVariant?.price || 0) * quantity).toLocaleString('vi-VN')} đ
                </span>
              </div>
            </div>
          )}

          {/* Footer buttons */}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
            <Button type="button" variant="ghost" size="sm" onClick={onClose} disabled={submitting}>
              Hủy
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              loading={submitting}
              disabled={submitting || loadingProducts || !selectedVariantId}
            >
              Xác nhận tạo đơn & Trừ kho
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
