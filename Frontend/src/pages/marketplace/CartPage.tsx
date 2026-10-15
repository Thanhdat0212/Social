import React, { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useCartStore } from '@/store/cartStore';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { CartItemDto, CartGroupedShop } from '@/types/marketplace';
import { ROUTES } from '@/constants/routes';
import {
  PageContainer,
  Button,
  StoreIcon,
  CartIcon,
  TrashIcon,
  EmptyState,
  AlertCircleIcon,
  ShieldIcon,
} from '@/components/common';

export const CartPage: React.FC = () => {
  const navigate = useNavigate();
  const { items, updateItemQuantity, removeItem, isLoading } = useCartStore();
  const [isCheckingOut, setIsCheckingOut] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  // M5.1: Danh sách các mặt hàng được chọn thanh toán
  const [selectedIds, setSelectedIds] = useState<Set<string>>(() => new Set(items.map((i) => i.id)));

  // Đồng bộ khi số lượng items thay đổi
  React.useEffect(() => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      items.forEach((it) => {
        if (!prev.has(it.id)) next.add(it.id);
      });
      for (const id of next) {
        if (!items.some((it) => it.id === id)) next.delete(id);
      }
      return next;
    });
  }, [items]);

  const toggleSelectAll = () => {
    if (selectedIds.size === items.length) {
      setSelectedIds(new Set());
    } else {
      setSelectedIds(new Set(items.map((i) => i.id)));
    }
  };

  const toggleSelectItem = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  // Các mặt hàng được tick chọn
  const selectedItems = useMemo(() => items.filter((i) => selectedIds.has(i.id)), [items, selectedIds]);
  const selectedTotalAmount = useMemo(
    () =>
      selectedItems.reduce((acc, it) => {
        const p = it.unitPrice ?? it.price ?? 0;
        return acc + p * it.quantity;
      }, 0),
    [selectedItems]
  );

  // Gom nhóm sản phẩm theo từng Shop
  const groupedShops = useMemo<CartGroupedShop[]>(() => {
    const map = new Map<string, CartGroupedShop>();
    items.forEach((item) => {
      const shopId = item.shopId || 'default-shop';
      if (!map.has(shopId)) {
        map.set(shopId, {
          shopId,
          shopName: item.shopName || 'Gian hàng',
          items: [],
          subtotal: 0,
        });
      }
      const group = map.get(shopId)!;
      group.items.push(item);
      const p = item.unitPrice ?? item.price ?? 0;
      group.subtotal += p * item.quantity;
    });
    return Array.from(map.values());
  }, [items]);

  const formatPrice = (price?: number | null) => {
    if (price === undefined || price === null || isNaN(price)) return '0 đ';
    return new Intl.NumberFormat('vi-VN').format(price) + ' đ';
  };

  const handleCheckout = async () => {
    if (selectedItems.length === 0) {
      setErrorMsg('Vui lòng chọn ít nhất một sản phẩm để thanh toán.');
      return;
    }
    setIsCheckingOut(true);
    setErrorMsg(null);
    try {
      const selectedVariantIds = selectedItems.map((i) => i.variantId);
      const checkoutItems = selectedItems.map((i) => ({
        variantId: i.variantId,
        quantity: i.quantity,
      }));

      const res = await marketplaceApi.createCheckout({
        selectedVariantIds,
        items: checkoutItems,
      });

      const checkoutId = (res as any).id || (res as any).checkoutId;
      if (!checkoutId) {
        throw new Error('Hệ thống không trả về mã phiên thanh toán hợp lệ.');
      }

      navigate(`/checkout/${checkoutId}`);
    } catch (err: any) {
      setErrorMsg(
        err.response?.data?.detail ||
          err.response?.data?.message ||
          'Không thể tạo phiên thanh toán. Một số sản phẩm có thể đã hết hàng.'
      );
    } finally {
      setIsCheckingOut(false);
    }
  };

  return (
    <PageContainer variant="wide">
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', marginBottom: '1.5rem' }}>
        <CartIcon size={24} style={{ color: 'var(--accent-primary)' }} />
        <h1 style={{ fontSize: '1.35rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
          Giỏ hàng của bạn
        </h1>
      </div>

      {errorMsg && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.6rem',
            background: 'rgba(239, 68, 68, 0.15)',
            border: '1px solid var(--danger)',
            color: 'var(--danger)',
            padding: '0.85rem 1.25rem',
            borderRadius: '12px',
            marginBottom: '1.5rem',
            fontWeight: 600,
            fontSize: '0.9rem',
          }}
        >
          <AlertCircleIcon size={18} />
          <span>{errorMsg}</span>
        </div>
      )}

      {items.length === 0 ? (
        <EmptyState
          icon={<CartIcon size={44} />}
          title="Giỏ hàng của bạn đang trống"
          description="Hãy dạo quanh Chợ số để chọn các sản phẩm số, mã bản quyền và tài khoản yêu thích."
          action={
            <Button
              variant="primary"
              size="md"
              onClick={() => navigate(ROUTES.MARKETPLACE.INDEX)}
            >
              Khám phá Chợ số ngay
            </Button>
          }
        />
      ) : (
        <div className="cart-layout">
          {/* Cột danh sách mặt hàng */}
          <div>
            {/* Thanh Chọn Tất Cả */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.75rem',
                background: 'var(--bg-card)',
                border: '1px solid var(--border-color)',
                borderRadius: '12px',
                padding: '0.75rem 1rem',
                marginBottom: '1rem',
                fontSize: '0.9rem',
                fontWeight: 600,
                color: 'var(--text-main)',
              }}
            >
              <input
                type="checkbox"
                id="select-all-cart"
                checked={items.length > 0 && selectedIds.size === items.length}
                onChange={toggleSelectAll}
                style={{ width: 18, height: 18, cursor: 'pointer', accentColor: 'var(--accent-primary)' }}
              />
              <label htmlFor="select-all-cart" style={{ cursor: 'pointer' }}>
                Chọn tất cả ({selectedItems.length}/{items.length} món)
              </label>
            </div>

            {groupedShops.map((group) => (
              <div key={group.shopId} className="cart-shop-group">
                <div className="cart-shop-header">
                  <StoreIcon size={18} style={{ color: 'var(--accent-primary)' }} />
                  <span>{group.shopName}</span>
                </div>

                {group.items.map((item: CartItemDto) => {
                  const isChecked = selectedIds.has(item.id);
                  return (
                    <div key={item.id} className="cart-item-row" style={{ opacity: isChecked ? 1 : 0.65 }}>
                      {/* Checkbox từng món */}
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => toggleSelectItem(item.id)}
                        style={{ width: 18, height: 18, cursor: 'pointer', accentColor: 'var(--accent-primary)' }}
                        aria-label={`Chọn món ${item.productName}`}
                      />

                      <img
                        src={
                          item.coverImageUrl ||
                          item.imageUrl ||
                          'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=200&auto=format&fit=crop&q=60'
                        }
                        alt={item.productName}
                        className="cart-item-thumb"
                        onError={(e) => {
                          const target = e.currentTarget;
                          if (!target.dataset.hasFallback) {
                            target.dataset.hasFallback = 'true';
                            target.src = 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=200&auto=format&fit=crop&q=60';
                          }
                        }}
                      />

                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div className="cart-item-title">{item.productName}</div>
                        <div className="cart-item-variant">
                          Phiên bản: <strong>{item.variantLabel || item.variantName || 'Bản tiêu chuẩn'}</strong>
                        </div>
                        <div className="cart-item-price">{formatPrice(item.unitPrice ?? item.price ?? 0)}</div>
                        {item.isAvailable === false && (
                          <div style={{ fontSize: '0.8rem', color: 'var(--color-danger, #ef4444)', marginTop: '0.25rem', fontWeight: 600 }}>
                            ⚠️ {item.unavailableReason || 'Tạm thời không khả dụng'}
                          </div>
                        )}
                      </div>

                      {/* Số lượng */}
                      <div className="cart-qty-control">
                        <button
                          type="button"
                          onClick={() => updateItemQuantity(item.id, item.quantity - 1)}
                          disabled={item.quantity <= 1 || isLoading}
                          aria-label="Giảm số lượng"
                        >
                          -
                        </button>
                        <span>{item.quantity}</span>
                        <button
                          type="button"
                          onClick={() => updateItemQuantity(item.id, item.quantity + 1)}
                          disabled={isLoading || (item.availableStock !== undefined && item.availableStock > 0 && item.quantity >= item.availableStock)}
                          aria-label="Tăng số lượng"
                        >
                          +
                        </button>
                      </div>

                      {/* Xóa */}
                      <button
                        type="button"
                        onClick={() => removeItem(item.id)}
                        disabled={isLoading}
                        style={{
                          background: 'none',
                          border: 'none',
                          color: 'var(--text-dim)',
                          cursor: 'pointer',
                          padding: '0.4rem',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          borderRadius: '6px',
                          transition: 'color 0.2s ease',
                        }}
                        title="Xóa khỏi giỏ"
                        aria-label="Xóa mặt hàng khỏi giỏ"
                      >
                        <TrashIcon size={16} />
                      </button>
                    </div>
                  );
                })}
              </div>
            ))}
          </div>

          {/* Cột tóm tắt thanh toán */}
          <div>
            <div className="cart-summary-card">
              <h3
                style={{
                  fontSize: '1.1rem',
                  fontWeight: 700,
                  color: 'var(--text-main)',
                  marginBottom: '1rem',
                }}
              >
                Tóm tắt đơn hàng
              </h3>

              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  marginBottom: '0.75rem',
                  fontSize: '0.9rem',
                  color: 'var(--text-muted)',
                }}
              >
                <span>Đã chọn:</span>
                <span style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                  {selectedItems.length} / {items.length} món
                </span>
              </div>

              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  marginBottom: '1.25rem',
                  fontSize: '0.9rem',
                  color: 'var(--text-muted)',
                }}
              >
                <span>Phương thức giao hàng:</span>
                <span style={{ color: 'var(--success)', fontWeight: 600 }}>
                  Giao mã tức thì qua kho số
                </span>
              </div>

              <div
                style={{
                  borderTop: '1px solid var(--border-color)',
                  paddingTop: '1rem',
                  marginBottom: '1.5rem',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'baseline',
                }}
              >
                <span style={{ fontWeight: 700, color: 'var(--text-main)' }}>
                  Tổng thanh toán:
                </span>
                <span style={{ fontSize: '1.45rem', fontWeight: 800, color: 'var(--accent-primary)' }}>
                  {formatPrice(selectedTotalAmount)}
                </span>
              </div>

              <Button
                type="button"
                variant="primary"
                block
                onClick={handleCheckout}
                loading={isCheckingOut}
                disabled={selectedItems.length === 0}
              >
                Tiến hành thanh toán VietQR ({selectedItems.length})
              </Button>

              <div
                style={{
                  marginTop: '1rem',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: '0.35rem',
                  fontSize: '0.78rem',
                  color: 'var(--text-dim)',
                }}
              >
                <ShieldIcon size={14} style={{ color: 'var(--accent-primary)' }} />
                <span>Thanh toán bảo mật chuẩn VietQR NAPAS 247</span>
              </div>
            </div>
          </div>
        </div>
      )}
    </PageContainer>
  );
};
