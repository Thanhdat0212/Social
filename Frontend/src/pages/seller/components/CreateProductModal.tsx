import React, { useRef, useState, useEffect } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { Category } from '@/types/marketplace';
import {
  FormField,
  Input,
  Textarea,
  Select,
  Button,
  Alert,
  Spinner,
} from '@/components/common';

interface CreateProductModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  categories: Category[];
}

export const CreateProductModal: React.FC<CreateProductModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  categories,
}) => {
  const [createStep, setCreateStep] = useState<1 | 2 | 3 | 4>(1);

  useEffect(() => {
    if (!isOpen) return;
    document.body.classList.add('modal-open');
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.body.classList.remove('modal-open');
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose]);
  const [productForm, setProductForm] = useState({
    name: '',
    categoryId: '',
    description: '',
    variantName: 'Gói Tiêu Chuẩn',
    sku: '',
    price: 50000,
    compareAtPrice: 100000,
    imageUrl: '',
  });

  const [isCreatingProduct, setIsCreatingProduct] = useState(false);
  const [createProductError, setCreateProductError] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [imageUploadProgress, setImageUploadProgress] = useState<string | null>(null);
  const [imageInputMode, setImageInputMode] = useState<'upload' | 'url'>('upload');
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  // Helper lấy danh sách category lá
  const getLeafCategories = (tree: Category[]): Category[] => {
    const leaves: Category[] = [];
    const traverse = (nodes: Category[]) => {
      for (const node of nodes) {
        if (node.children && node.children.length > 0) {
          traverse(node.children);
        } else {
          leaves.push(node);
        }
      }
    };
    traverse(tree);
    return leaves;
  };

  const leafCategories = getLeafCategories(categories);

  const handleImageFileUpload = async (file: File) => {
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setCreateProductError('Vui lòng chọn tệp hình ảnh hợp lệ (JPG, PNG, WebP, GIF).');
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      setCreateProductError('Kích thước tệp ảnh không được vượt quá 10MB.');
      return;
    }

    setIsUploadingImage(true);
    setCreateProductError(null);
    setImageUploadProgress('Đang tải ảnh lên Cloudinary...');

    try {
      const res = await marketplaceApi.uploadShopProductImageFile(file);
      setProductForm((prev) => ({ ...prev, imageUrl: res.url }));
    } catch (err: any) {
      const msg = err.response?.data?.detail || err.response?.data?.message || err.message || 'Không thể tải ảnh lên Cloudinary.';
      setCreateProductError(`Lỗi tải ảnh lên Cloudinary: ${msg}`);
    } finally {
      setIsUploadingImage(false);
      setImageUploadProgress(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleImageDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      handleImageFileUpload(e.dataTransfer.files[0]);
    }
  };

  const handleCreateProduct = async (publishImmediate = false) => {
    if (!productForm.name.trim()) {
      setCreateProductError('Vui lòng nhập tên sản phẩm.');
      return;
    }
    if (!productForm.categoryId) {
      setCreateProductError('Vui lòng chọn danh mục cho sản phẩm.');
      return;
    }
    if (productForm.price <= 0) {
      setCreateProductError('Giá sản phẩm phải lớn hơn 0 đ.');
      return;
    }
    if (productForm.compareAtPrice && Number(productForm.compareAtPrice) < Number(productForm.price)) {
      setCreateProductError('Giá gốc so sánh (gạch ngang) không được nhỏ hơn giá bán.');
      return;
    }
    if (publishImmediate && !productForm.imageUrl.trim()) {
      setCreateProductError('Sản phẩm cần có ít nhất một hình ảnh bìa trước khi đăng bán ngay.');
      return;
    }

    setIsCreatingProduct(true);
    setCreateProductError(null);
    try {
      const priceVal = Number(productForm.price);
      const comparePriceVal = productForm.compareAtPrice ? Number(productForm.compareAtPrice) : undefined;
      const validComparePrice = comparePriceVal && comparePriceVal >= priceVal ? comparePriceVal : undefined;

      const created = await marketplaceApi.createShopProduct({
        name: productForm.name.trim(),
        categoryId: productForm.categoryId || undefined,
        description: productForm.description.trim() || undefined,
        defaultVariant: {
          price: priceVal,
          compareAtPrice: validComparePrice,
          sku: productForm.sku.trim() || undefined,
          deliveryType: 1, // 1: CodePool
          trackInventory: true,
        },
      });

      if (created?.id && productForm.variantName?.trim()) {
        const vName = productForm.variantName.trim();
        const defaultVar = created.variants?.[0];
        try {
          await marketplaceApi.updateShopProductVariants(created.id, {
            options: [{ name: 'Gói sản phẩm', position: 0, values: [vName] }],
            variants: [
              {
                id: defaultVar?.id,
                sku: productForm.sku.trim() || undefined,
                price: priceVal,
                compareAtPrice: validComparePrice,
                deliveryType: 1,
                trackInventory: true,
                optionValues: [vName],
              },
            ],
          });
        } catch {
          // Non-blocking fallback
        }
      }

      if (productForm.imageUrl.trim() && created?.id) {
        await marketplaceApi.addShopProductImage(created.id, {
          url: productForm.imageUrl.trim(),
          position: 0,
          isCover: true,
        });
      }

      if (publishImmediate && created?.id) {
        await marketplaceApi.publishShopProduct(created.id);
        alert('Đã tạo và đăng bán sản phẩm thành công lên Chợ số!');
      } else {
        alert('Đã lưu bản nháp sản phẩm thành công!');
      }

      onClose();
      setCreateStep(1);
      setProductForm({
        name: '',
        categoryId: '',
        description: '',
        variantName: 'Gói Tiêu Chuẩn',
        sku: '',
        price: 50000,
        compareAtPrice: 100000,
        imageUrl: '',
      });
      onSuccess();
    } catch (err: any) {
      const serverMsg =
        err.response?.data?.detail ||
        err.response?.data?.message ||
        (err.response?.data?.errors ? Object.values(err.response.data.errors).flat().join(', ') : null);
      setCreateProductError(serverMsg || 'Không thể tạo sản phẩm. Vui lòng kiểm tra lại.');
    } finally {
      setIsCreatingProduct(false);
    }
  };

  if (!isOpen) return null;

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
          maxWidth: '680px',
          width: '100%',
          padding: '2rem',
          maxHeight: '90vh',
          overflowY: 'auto',
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <div>
            <h3 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
              Tạo sản phẩm số mới
            </h3>
            <p style={{ margin: '0.2rem 0 0', fontSize: '0.85rem', color: 'var(--text-muted)' }}>
              Bước {createStep}/4: {createStep === 1 ? 'Thông tin cơ bản' : createStep === 2 ? 'Biến thể & Giá bán' : createStep === 3 ? 'Hình ảnh sản phẩm' : 'Xác nhận & Xuất bản'}
            </p>
          </div>
          <Button variant="ghost" size="xs" onClick={onClose}>Đóng</Button>
        </div>

        {createProductError && (
          <Alert type="error" message={createProductError} className="mb-4" />
        )}

        {/* BƯỚC 1: THÔNG TIN CƠ BẢN */}
        {createStep === 1 && (
          <div>
            <FormField label="Tên sản phẩm số" required hint="Tên rõ ràng, bao gồm thời hạn và phiên bản">
              <Input
                placeholder="VD: Tài khoản Netflix Premium 1 Tháng (Chính chủ)"
                value={productForm.name}
                onChange={(e) => setProductForm({ ...productForm, name: e.target.value })}
              />
            </FormField>

            <FormField label="Danh mục sản phẩm" required>
              <Select
                value={productForm.categoryId}
                onChange={(e) => setProductForm({ ...productForm, categoryId: e.target.value })}
              >
                <option value="">-- Chọn danh mục --</option>
                {leafCategories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </Select>
            </FormField>

            <FormField label="Mô tả sản phẩm & Cam kết bảo hành">
              <Textarea
                rows={4}
                placeholder="Mô tả chi tiết tính năng, quyền lợi và cam kết hỗ trợ khi khách mua mã..."
                value={productForm.description}
                onChange={(e) => setProductForm({ ...productForm, description: e.target.value })}
              />
            </FormField>

            <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1.5rem' }}>
              <Button
                variant="primary"
                size="sm"
                onClick={() => {
                  if (!productForm.name.trim()) {
                    setCreateProductError('Vui lòng nhập tên sản phẩm.');
                    return;
                  }
                  if (!productForm.categoryId) {
                    setCreateProductError('Vui lòng chọn danh mục cho sản phẩm.');
                    return;
                  }
                  setCreateProductError(null);
                  setCreateStep(2);
                }}
              >
                Tiếp theo: Biến thể & Giá →
              </Button>
            </div>
          </div>
        )}

        {/* BƯỚC 2: BIẾN THỂ & GIÁ BÁN */}
        {createStep === 2 && (
          <div>
            <FormField label="Tên biến thể / Gói mặc định" required hint="VD: Gói 1 Tháng, 1 Năm, Bản Vĩnh Viễn">
              <Input
                placeholder="VD: Gói 1 Tháng"
                value={productForm.variantName}
                onChange={(e) => setProductForm({ ...productForm, variantName: e.target.value })}
              />
            </FormField>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
              <FormField label="Giá bán (VNĐ)" required>
                <Input
                  type="number"
                  min={1000}
                  step={5000}
                  value={productForm.price}
                  onChange={(e) => setProductForm({ ...productForm, price: Number(e.target.value) })}
                />
              </FormField>

              <FormField label="Giá gốc so sánh (VNĐ)" hint="Để trống nếu không có giảm giá">
                <Input
                  type="number"
                  min={0}
                  step={5000}
                  value={productForm.compareAtPrice}
                  onChange={(e) => setProductForm({ ...productForm, compareAtPrice: Number(e.target.value) })}
                />
              </FormField>
            </div>

            <FormField label="Mã SKU quản lý kho (Tùy chọn)">
              <Input
                placeholder="VD: NF-PREM-1M"
                value={productForm.sku}
                onChange={(e) => setProductForm({ ...productForm, sku: e.target.value })}
              />
            </FormField>

            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '1.5rem' }}>
              <Button variant="secondary" size="sm" onClick={() => setCreateStep(1)}>
                ← Quay lại
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={() => {
                  if (productForm.price <= 0) {
                    setCreateProductError('Giá sản phẩm phải lớn hơn 0 đ.');
                    return;
                  }
                  if (productForm.compareAtPrice && Number(productForm.compareAtPrice) < Number(productForm.price)) {
                    setCreateProductError('Giá gốc so sánh không được nhỏ hơn giá bán.');
                    return;
                  }
                  setCreateProductError(null);
                  setCreateStep(3);
                }}
              >
                Tiếp theo: Hình ảnh →
              </Button>
            </div>
          </div>
        )}

        {/* BƯỚC 3: HÌNH ẢNH SẢN PHẨM */}
        {createStep === 3 && (
          <div>
            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.25rem' }}>
              <Button
                type="button"
                variant={imageInputMode === 'upload' ? 'primary' : 'secondary'}
                size="xs"
                onClick={() => setImageInputMode('upload')}
              >
                Tải ảnh từ máy (Cloudinary)
              </Button>
              <Button
                type="button"
                variant={imageInputMode === 'url' ? 'primary' : 'secondary'}
                size="xs"
                onClick={() => setImageInputMode('url')}
              >
                Dán URL ảnh trực tiếp
              </Button>
            </div>

            {imageInputMode === 'upload' ? (
              <div>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/png,image/jpeg,image/webp,image/gif"
                  style={{ display: 'none' }}
                  onChange={(e) => {
                    if (e.target.files && e.target.files.length > 0) {
                      handleImageFileUpload(e.target.files[0]);
                    }
                  }}
                />

                <div
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={handleImageDrop}
                  onClick={() => fileInputRef.current?.click()}
                  style={{
                    border: '2px dashed var(--border-color)',
                    borderRadius: '16px',
                    padding: '2.5rem 1.5rem',
                    textAlign: 'center',
                    cursor: isUploadingImage ? 'not-allowed' : 'pointer',
                    background: 'var(--bg-surface)',
                    transition: 'all 0.2s ease',
                  }}
                >
                  {isUploadingImage ? (
                    <div>
                      <Spinner size="md" />
                      <p style={{ marginTop: '0.75rem', fontSize: '0.9rem', color: 'var(--text-muted)' }}>
                        {imageUploadProgress || 'Đang xử lý tải ảnh...'}
                      </p>
                    </div>
                  ) : (
                    <div>
                      <div style={{ fontSize: '2.5rem', marginBottom: '0.5rem' }}>📸</div>
                      <p style={{ fontWeight: 700, margin: '0 0 0.25rem', color: 'var(--text-main)' }}>
                        Kéo thả ảnh vào đây, hoặc click để chọn tệp
                      </p>
                      <p style={{ fontSize: '0.8rem', color: 'var(--text-dim)', margin: 0 }}>
                        Định dạng: JPG, PNG, WEBP, GIF (Tối đa 10MB)
                      </p>
                    </div>
                  )}
                </div>
              </div>
            ) : (
              <FormField label="Đường dẫn URL hình ảnh" hint="VD: https://images.unsplash.com/...">
                <Input
                  placeholder="https://..."
                  value={productForm.imageUrl}
                  onChange={(e) => setProductForm({ ...productForm, imageUrl: e.target.value })}
                />
              </FormField>
            )}

            {productForm.imageUrl && (
              <div style={{ marginTop: '1.25rem', display: 'flex', alignItems: 'center', gap: '1rem', background: 'var(--bg-surface)', padding: '0.75rem', borderRadius: '12px', border: '1px solid var(--border-color)' }}>
                <img
                  src={productForm.imageUrl}
                  alt="Ảnh xem trước"
                  style={{ width: '64px', height: '64px', objectFit: 'contain', borderRadius: '8px', border: '1px solid var(--border-color)' }}
                  onError={(e) => {
                    (e.target as HTMLElement).style.display = 'none';
                  }}
                />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <p style={{ fontSize: '0.85rem', fontWeight: 600, margin: 0, color: 'var(--text-main)' }}>
                    Ảnh bìa đã chọn
                  </p>
                  <p style={{ fontSize: '0.75rem', color: 'var(--text-dim)', margin: 0, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {productForm.imageUrl}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="xs"
                  onClick={() => setProductForm({ ...productForm, imageUrl: '' })}
                >
                  Xóa ảnh
                </Button>
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '1.5rem' }}>
              <Button variant="secondary" size="sm" onClick={() => setCreateStep(2)}>
                ← Quay lại
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={() => {
                  setCreateProductError(null);
                  setCreateStep(4);
                }}
              >
                Tiếp theo: Xác nhận →
              </Button>
            </div>
          </div>
        )}

        {/* BƯỚC 4: XÁC NHẬN & TẠO SẢN PHẨM */}
        {createStep === 4 && (
          <div>
            <div style={{ background: 'var(--bg-surface)', padding: '1.25rem', borderRadius: '14px', border: '1px solid var(--border-color)', marginBottom: '1.5rem' }}>
              <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', marginBottom: '1rem' }}>
                {productForm.imageUrl ? (
                  <img
                    src={productForm.imageUrl}
                    alt={productForm.name}
                    style={{ width: '70px', height: '70px', objectFit: 'contain', borderRadius: '10px', border: '1px solid var(--border-color)' }}
                  />
                ) : (
                  <div style={{ width: '70px', height: '70px', borderRadius: '10px', background: 'var(--bg-subtle)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: '1.5rem' }}>
                    📦
                  </div>
                )}
                <div>
                  <h4 style={{ margin: 0, fontSize: '1.05rem', fontWeight: 800, color: 'var(--text-main)' }}>
                    {productForm.name}
                  </h4>
                  <p style={{ margin: '0.2rem 0 0', fontSize: '0.85rem', color: 'var(--text-dim)' }}>
                    Biến thể: <strong>{productForm.variantName}</strong> • Giá bán:{' '}
                    <strong style={{ color: 'var(--accent-primary)' }}>
                      {Number(productForm.price).toLocaleString('vi-VN')} đ
                    </strong>
                    {productForm.compareAtPrice ? (
                      <span style={{ textDecoration: 'line-through', color: 'var(--text-dim)', marginLeft: '0.4rem', fontSize: '0.8rem' }}>
                        {Number(productForm.compareAtPrice).toLocaleString('vi-VN')} đ
                      </span>
                    ) : null}
                  </p>
                </div>
              </div>

              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', borderTop: '1px solid var(--border-color)', paddingTop: '0.75rem' }}>
                <p style={{ margin: '0 0 0.25rem' }}>
                  Danh mục: <strong>{leafCategories.find((c) => c.id === productForm.categoryId)?.name || 'Chưa chọn'}</strong>
                </p>
                {productForm.sku && (
                  <p style={{ margin: '0 0 0.25rem' }}>Mã SKU: <code>{productForm.sku}</code></p>
                )}
                <p style={{ margin: 0 }}>
                  Kho hàng: <span style={{ color: '#f59e0b', fontWeight: 600 }}>0 mã (Nạp mã sau khi tạo)</span>
                </p>
              </div>
            </div>

            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Button variant="secondary" size="sm" onClick={() => setCreateStep(3)}>
                ← Quay lại
              </Button>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  loading={isCreatingProduct}
                  onClick={() => handleCreateProduct(false)}
                >
                  Lưu bản nháp
                </Button>
                <Button
                  type="button"
                  variant="primary"
                  size="sm"
                  loading={isCreatingProduct}
                  onClick={() => handleCreateProduct(true)}
                >
                  Đăng bán ngay lên Chợ
                </Button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
