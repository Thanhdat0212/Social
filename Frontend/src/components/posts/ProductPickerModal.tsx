import React, { useState, useEffect } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { ProductListItem } from '@/types/marketplace';
import { Input, Button, Skeleton, EmptyState, CloseIcon } from '@/components/common';

interface ProductPickerModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedProducts: ProductListItem[];
  onSelectProducts: (products: ProductListItem[]) => void;
}

export const ProductPickerModal: React.FC<ProductPickerModalProps> = ({
  isOpen,
  onClose,
  selectedProducts,
  onSelectProducts,
}) => {
  const [search, setSearch] = useState('');
  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [tempSelected, setTempSelected] = useState<ProductListItem[]>(selectedProducts);

  useEffect(() => {
    if (isOpen) {
      setTempSelected(selectedProducts);
      loadProducts('');
    }
  }, [isOpen]);

  const loadProducts = async (query: string) => {
    setIsLoading(true);
    try {
      const res = await marketplaceApi.getProducts({
        search: query.trim() || undefined,
        pageSize: 12,
      });
      setProducts(res.items || []);
    } catch {
      setProducts([]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setSearch(val);
    loadProducts(val);
  };

  const handleToggleProduct = (product: ProductListItem) => {
    const isSelected = tempSelected.some((p) => p.id === product.id);
    if (isSelected) {
      setTempSelected((prev) => prev.filter((p) => p.id !== product.id));
    } else {
      if (tempSelected.length >= 3) {
        alert('Bạn chỉ có thể gắn thẻ tối đa 3 sản phẩm mỗi bài viết.');
        return;
      }
      setTempSelected((prev) => [...prev, product]);
    }
  };

  const handleConfirm = () => {
    onSelectProducts(tempSelected);
    onClose();
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('vi-VN').format(price) + ' ₫';
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div
        className="modal-content"
        style={{ maxWidth: '540px', width: '92%' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-header">
          <h2 style={{ fontSize: '1.15rem', fontWeight: 700, margin: 0 }}>
            🛍️ Gắn thẻ sản phẩm vào bài viết
          </h2>
          <button
            type="button"
            className="btn-close-modal"
            onClick={onClose}
            aria-label="Đóng"
          >
            <CloseIcon size={16} />
          </button>
        </div>

        <div className="modal-body" style={{ display: 'flex', flexDirection: 'column', gap: '1rem', padding: '1rem 0' }}>
          <Input
            placeholder="Tìm kiếm sản phẩm theo tên, shop..."
            value={search}
            onChange={handleSearchChange}
            autoFocus
          />

          <div
            style={{
              maxHeight: '340px',
              overflowY: 'auto',
              display: 'flex',
              flexDirection: 'column',
              gap: '0.5rem',
              paddingRight: '0.25rem',
            }}
          >
            {isLoading ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                <Skeleton height="60px" borderRadius="10px" />
                <Skeleton height="60px" borderRadius="10px" />
                <Skeleton height="60px" borderRadius="10px" />
              </div>
            ) : products.length === 0 ? (
              <EmptyState
                title="Không tìm thấy sản phẩm"
                description="Thử tìm kiếm với từ khóa khác trên chợ số"
              />
            ) : (
              products.map((product) => {
                const isSelected = tempSelected.some((p) => p.id === product.id);
                return (
                  <div
                    key={product.id}
                    onClick={() => handleToggleProduct(product)}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.75rem',
                      padding: '0.625rem 0.75rem',
                      borderRadius: '10px',
                      border: isSelected
                        ? '2px solid var(--accent-primary, #1877f2)'
                        : '1px solid var(--border-color, #e4e6eb)',
                      background: isSelected ? 'rgba(24, 119, 242, 0.05)' : 'var(--bg-card, #ffffff)',
                      cursor: 'pointer',
                      transition: 'all 0.15s ease',
                    }}
                  >
                    <img
                      src={product.coverImageUrl || 'https://via.placeholder.com/60'}
                      alt={product.name}
                      style={{
                        width: '48px',
                        height: '48px',
                        borderRadius: '8px',
                        objectFit: 'cover',
                        flexShrink: 0,
                      }}
                    />
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div
                        style={{
                          fontWeight: 600,
                          fontSize: '0.9rem',
                          whiteSpace: 'nowrap',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          color: 'var(--text-main, #050505)',
                        }}
                      >
                        {product.name}
                      </div>
                      <div
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.5rem',
                          fontSize: '0.8rem',
                          color: 'var(--text-muted, #65676b)',
                          marginTop: '0.125rem',
                        }}
                      >
                        <span style={{ fontWeight: 600, color: 'var(--accent-primary, #1877f2)' }}>
                          {formatPrice(product.minPrice)}
                        </span>
                        <span>•</span>
                        <span>{product.shopName}</span>
                      </div>
                    </div>
                    <div
                      style={{
                        width: '20px',
                        height: '20px',
                        borderRadius: '50%',
                        border: isSelected
                          ? '6px solid var(--accent-primary, #1877f2)'
                          : '2px solid var(--border-color, #ccd0d5)',
                        flexShrink: 0,
                      }}
                    />
                  </div>
                );
              })
            )}
          </div>
        </div>

        <div className="modal-footer" style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
          <Button variant="ghost" onClick={onClose}>
            Hủy
          </Button>
          <Button variant="primary" onClick={handleConfirm}>
            Xác nhận ({tempSelected.length}/3)
          </Button>
        </div>
      </div>
    </div>
  );
};
