import React, { useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { Category } from '@/types/marketplace';
import {
  Button,
  Badge,
  Spinner,
  PackageIcon,
  EditIcon,
} from '@/components/common';
import { EditProductModal } from '../components/EditProductModal';
import { CreateProductModal } from '../components/CreateProductModal';
import { InspectCodesModal } from '../components/InspectCodesModal';
import { BulkImportCodesModal } from '../components/BulkImportCodesModal';

interface ProductsTabProps {
  onProductCountChange?: (count: number) => void;
}

export const ProductsTab: React.FC<ProductsTabProps> = ({ onProductCountChange }) => {
  const [products, setProducts] = useState<any[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(false);

  // Nạp kho số
  const [selectedVariantId, setSelectedVariantId] = useState<string | null>(null);

  // Chỉnh sửa sản phẩm
  const [editingProductId, setEditingProductId] = useState<string | null>(null);

  // Tạo sản phẩm mới
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

  // Xem chi tiết kho mã
  const [inspectVariant, setInspectVariant] = useState<{ id: string; name: string } | null>(null);

  const loadProductsData = async () => {
    setLoading(true);
    try {
      const [prodRes, catRes] = await Promise.all([
        marketplaceApi.getShopProducts(),
        marketplaceApi.getCategoriesTree().catch(() => []),
      ]);
      const items = prodRes.items || prodRes || [];
      setProducts(items);
      setCategories(catRes || []);
      onProductCountChange?.(items.length);
    } catch (err) {
      console.error('Lỗi khi tải danh sách sản phẩm:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProductsData();
  }, []);

  const handlePublishProduct = async (id: string) => {
    try {
      await marketplaceApi.publishShopProduct(id);
      alert('Đã đăng bán sản phẩm lên Chợ số thành công!');
      loadProductsData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi đăng bán sản phẩm.');
    }
  };

  const handleHideProduct = async (id: string) => {
    try {
      await marketplaceApi.hideShopProduct(id);
      alert('Đã tạm ẩn sản phẩm khỏi sàn.');
      loadProductsData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi tạm ẩn sản phẩm.');
    }
  };

  const handleUnhideProduct = async (id: string) => {
    try {
      await marketplaceApi.unhideShopProduct(id);
      alert('Đã mở hiển thị lại sản phẩm.');
      loadProductsData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi bỏ ẩn sản phẩm.');
    }
  };

  const handleArchiveProduct = async (id: string) => {
    if (!window.confirm('Bạn có chắc muốn lưu trữ sản phẩm này? Sản phẩm sẽ ngừng kinh doanh.')) return;
    try {
      await marketplaceApi.archiveShopProduct(id);
      alert('Đã chuyển sản phẩm vào mục lưu trữ.');
      loadProductsData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi khi lưu trữ sản phẩm.');
    }
  };

  return (
    <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '0.75rem' }}>
        <div>
          <h2 style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
            Quản lý sản phẩm & Kho số tự động
          </h2>
          <p style={{ margin: '0.25rem 0 0', fontSize: '0.82rem', color: 'var(--text-muted)' }}>
            Tạo sản phẩm, nạp mã số tự động và thiết lập trạng thái kinh doanh.
          </p>
        </div>
        <Button
          type="button"
          variant="primary"
          size="sm"
          onClick={() => setIsCreateModalOpen(true)}
        >
          + Thêm sản phẩm số mới
        </Button>
      </div>

      {loading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <Spinner size="lg" />
          <p style={{ marginTop: '0.75rem', fontSize: '0.85rem', color: 'var(--text-muted)' }}>Đang tải danh sách sản phẩm...</p>
        </div>
      ) : products.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '3.5rem 1rem', background: 'var(--bg-surface)', borderRadius: '12px', border: '1px dashed var(--border-color)' }}>
          <PackageIcon size={44} style={{ color: 'var(--text-dim)', marginBottom: '0.75rem' }} />
          <h3 style={{ fontSize: '1.05rem', fontWeight: 700, margin: '0 0 0.35rem', color: 'var(--text-main)' }}>
            Chưa có sản phẩm số nào
          </h3>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', maxWidth: '420px', margin: '0 auto 1.25rem', lineHeight: '1.5' }}>
            Bắt đầu bán tài khoản phần mềm, key bản quyền hoặc khóa học số trên Chợ số để tiếp cận hàng nghìn khách hàng.
          </p>
          <Button variant="primary" size="sm" onClick={() => setIsCreateModalOpen(true)}>
            + Tạo sản phẩm đầu tiên
          </Button>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {products.map((p) => (
            <div
              key={p.id}
              style={{
                background: 'var(--bg-surface)',
                border: '1px solid var(--border-color)',
                borderRadius: '12px',
                padding: '1.25rem',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '0.75rem' }}>
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
                    <h3 style={{ margin: 0, fontSize: '1rem', fontWeight: 700, color: 'var(--text-main)' }}>
                      {p.name}
                    </h3>
                    <Badge
                      variant={p.status === 'Listed' ? 'success' : p.status === 'Draft' ? 'neutral' : 'warning'}
                      size="sm"
                    >
                      {p.status === 'Listed' ? 'Đang bán' : p.status === 'Draft' ? 'Bản nháp' : p.status === 'Hidden' ? 'Tạm ẩn' : p.status}
                    </Badge>
                  </div>
                  <p style={{ fontSize: '0.8rem', color: 'var(--text-dim)', margin: '0.25rem 0 0' }}>
                    Slug: <code>{p.slug}</code> • Danh mục: <strong>{p.categoryName || 'Sản phẩm số'}</strong>
                  </p>
                </div>

                {/* Cụm nút chuyển trạng thái & chỉnh sửa sản phẩm */}
                <div style={{ display: 'flex', gap: '0.4rem', flexWrap: 'wrap' }}>
                  <Button
                    type="button"
                    variant="secondary"
                    size="xs"
                    icon={<EditIcon size={13} />}
                    onClick={() => setEditingProductId(p.id)}
                  >
                    Chỉnh sửa
                  </Button>
                  {p.status === 'Draft' && (
                    <Button type="button" variant="primary" size="xs" onClick={() => handlePublishProduct(p.id)}>
                      Đăng bán
                    </Button>
                  )}
                  {p.status === 'Listed' && (
                    <Button type="button" variant="secondary" size="xs" onClick={() => handleHideProduct(p.id)}>
                      Tạm ẩn
                    </Button>
                  )}
                  {p.status === 'Hidden' && (
                    <Button type="button" variant="primary" size="xs" onClick={() => handleUnhideProduct(p.id)}>
                      Bỏ ẩn
                    </Button>
                  )}
                  {p.status !== 'Archived' && (
                    <Button type="button" variant="ghost" size="xs" onClick={() => handleArchiveProduct(p.id)}>
                      Lưu trữ
                    </Button>
                  )}
                </div>
              </div>

              {/* Danh sách biến thể */}
              <div style={{ marginTop: '1rem', display: 'flex', flexWrap: 'wrap', gap: '0.65rem' }}>
                {p.variants?.map((v: any) => (
                  <div
                    key={v.id}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      background: 'var(--bg-card)',
                      padding: '0.5rem 0.85rem',
                      borderRadius: '8px',
                      border: '1px solid var(--border-color)',
                      fontSize: '0.85rem',
                    }}
                  >
                    <span style={{ fontWeight: 700, color: 'var(--text-main)' }}>{v.name || 'Mặc định'}</span>
                    <span style={{ color: 'var(--accent-primary)', fontWeight: 700 }}>
                      {v.price ? Number(v.price).toLocaleString('vi-VN') + ' đ' : ''}
                    </span>
                    <Badge variant={v.availableStock > 0 ? 'success' : 'danger'} size="sm">
                      Kho: {v.availableStock || 0}
                    </Badge>
                    <Button
                      type="button"
                      variant="ghost"
                      size="xs"
                      onClick={() => setInspectVariant({ id: v.id, name: `${p.name} - ${v.name || ''}` })}
                    >
                      Xem kho mã
                    </Button>
                    <Button
                      type="button"
                      variant="secondary"
                      size="xs"
                      onClick={() => setSelectedVariantId(v.id)}
                    >
                      + Nạp kho
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* MODAL TẠO SẢN PHẨM MỚI 4 BƯỚC */}
      <CreateProductModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSuccess={loadProductsData}
        categories={categories}
      />

      {/* MODAL XEM CHI TIẾT KHO MÃ & HỦY MÃ LỖI */}
      <InspectCodesModal
        variant={inspectVariant}
        onClose={() => setInspectVariant(null)}
        onReloadStock={loadProductsData}
      />

      {/* MODAL NẠP KHO SỐ BULK */}
      <BulkImportCodesModal
        variantId={selectedVariantId}
        onClose={() => setSelectedVariantId(null)}
        onSuccess={loadProductsData}
      />

      {/* MODAL CHỈNH SỬA SẢN PHẨM */}
      {editingProductId && (
        <EditProductModal
          productId={editingProductId}
          categories={categories}
          isOpen={Boolean(editingProductId)}
          onClose={() => setEditingProductId(null)}
          onOpenAddStock={(vId) => {
            setEditingProductId(null);
            setSelectedVariantId(vId);
          }}
          onProductUpdated={loadProductsData}
        />
      )}
    </div>
  );
};
