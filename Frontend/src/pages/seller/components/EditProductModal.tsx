import React, { useEffect, useState, useRef } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type {
  Category,
  SellerProductDetailDto,
  SellerProductImageDto,
  SellerProductVariantDto,
} from '@/types/marketplace';
import {
  Button,
  FormField,
  Input,
  Textarea,
  Select,
  Badge,
  Spinner,
  CloseIcon,
  EditIcon,
  TrashIcon,
  CheckIcon,
  ImageIcon,
  ExternalLinkIcon,
  AlertCircleIcon,
  CheckCircleIcon,
  PlusIcon,
  PackageIcon,
  TagIcon,
} from '@/components/common';

interface EditProductModalProps {
  productId: string;
  categories: Category[];
  isOpen: boolean;
  initialTab?: EditTab;
  onClose: () => void;
  onProductUpdated: () => void;
  onOpenAddStock?: (variantId: string) => void;
}

// Helper làm phẳng danh mục cây thành danh sách có thụt lề
interface FlatCategoryOption {
  id: string;
  name: string;
  level: number;
}

const flattenCategories = (cats: Category[], level = 0): FlatCategoryOption[] => {
  let result: FlatCategoryOption[] = [];
  for (const c of cats) {
    result.push({ id: c.id, name: c.name, level });
    if (c.children && c.children.length > 0) {
      result = result.concat(flattenCategories(c.children, level + 1));
    }
  }
  return result;
};

type EditTab = 'details' | 'variants' | 'images';

export const EditProductModal: React.FC<EditProductModalProps> = ({
  productId,
  categories,
  isOpen,
  initialTab = 'details',
  onClose,
  onProductUpdated,
  onOpenAddStock,
}) => {
  const [activeTab, setActiveTab] = useState<EditTab>(initialTab);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [product, setProduct] = useState<SellerProductDetailDto | null>(null);

  // Tab 1: Thông tin cơ bản & Giá bán sản phẩm đơn
  const [detailsForm, setDetailsForm] = useState({
    name: '',
    categoryId: '',
    description: '',
    price: 0,
    compareAtPrice: null as number | null,
  });
  const [isSavingDetails, setIsSavingDetails] = useState(false);

  // Tab 2: Biến thể & Giá
  const [variantList, setVariantList] = useState<
    Array<{
      id?: string;
      sku: string;
      name?: string;
      price: number;
      compareAtPrice?: number | null;
      deliveryType: string | number;
      trackInventory: boolean;
      optionValues?: string[];
      availableStock?: number;
    }>
  >([]);
  const [isSavingVariants, setIsSavingVariants] = useState(false);

  // Tab 3: Thư viện hình ảnh
  const [images, setImages] = useState<SellerProductImageDto[]>([]);
  const [imageInputMode, setImageInputMode] = useState<'upload' | 'url'>('upload');
  const [imageUrlInput, setImageUrlInput] = useState('');
  const [isCoverNewImage, setIsCoverNewImage] = useState(false);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [imageActionLoading, setImageActionLoading] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!isOpen) return;
    document.body.classList.add('modal-open');
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isSavingDetails && !isSavingVariants && !isUploadingImage) {
        onClose();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.body.classList.remove('modal-open');
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose, isSavingDetails, isSavingVariants, isUploadingImage]);

  // Thông báo phản hồi (Banner feedback)
  const [feedback, setFeedback] = useState<{
    type: 'success' | 'error';
    message: string;
  } | null>(null);

  const flatCategories = React.useMemo(() => flattenCategories(categories), [categories]);

  const loadProductDetail = async () => {
    setIsLoading(true);
    setLoadError(null);
    setFeedback(null);
    try {
      const data = await marketplaceApi.getShopProductById(productId);
      setProduct(data);
      const firstVar = data.variants?.[0];
      setDetailsForm({
        name: data.name || '',
        categoryId: data.categoryId || '',
        description: data.description || '',
        price: firstVar ? Number(firstVar.price) : 0,
        compareAtPrice: firstVar?.compareAtPrice ? Number(firstVar.compareAtPrice) : null,
      });
      setVariantList(
        data.variants.map((v: SellerProductVariantDto, idx: number) => {
          const vName = v.name || (v.optionValues && v.optionValues.length > 0 ? v.optionValues.join(' - ') : `Gói ${idx + 1}`);
          return {
            id: v.id,
            sku: v.sku || '',
            name: vName,
            price: Number(v.price) || 0,
            compareAtPrice: v.compareAtPrice ? Number(v.compareAtPrice) : null,
            deliveryType: v.deliveryType || 1,
            trackInventory: v.trackInventory ?? true,
            optionValues: [vName],
            availableStock: v.availableStock || 0,
          };
        })
      );
      setImages(data.images || []);
    } catch (err: any) {
      setLoadError(err.response?.data?.message || 'Không thể tải chi tiết sản phẩm.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen && productId) {
      loadProductDetail();
      setActiveTab(initialTab);
    }
  }, [isOpen, productId, initialTab]);

  const showFeedback = (type: 'success' | 'error', message: string) => {
    setFeedback({ type, message });
    setTimeout(() => {
      setFeedback((prev) => (prev?.message === message ? null : prev));
    }, 4500);
  };

  // 1. Lưu thông tin cơ bản & Giá bán
  const handleSaveDetails = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!detailsForm.name.trim()) {
      showFeedback('error', 'Tên sản phẩm không được để trống.');
      return;
    }

    if (variantList.length <= 1) {
      if (detailsForm.price <= 0) {
        showFeedback('error', 'Giá bán sản phẩm phải lớn hơn 0 đ.');
        return;
      }
      if (detailsForm.compareAtPrice && detailsForm.compareAtPrice < detailsForm.price) {
        showFeedback('error', 'Giá gốc (so sánh) không được nhỏ hơn giá bán.');
        return;
      }
    }

    setIsSavingDetails(true);
    setFeedback(null);
    try {
      const updated = await marketplaceApi.updateShopProductDetails(productId, {
        name: detailsForm.name.trim(),
        categoryId: detailsForm.categoryId ? detailsForm.categoryId : null,
        description: detailsForm.description.trim() || undefined,
        price: variantList.length <= 1 ? Number(detailsForm.price) : undefined,
        compareAtPrice: variantList.length <= 1 ? (detailsForm.compareAtPrice ? Number(detailsForm.compareAtPrice) : null) : undefined,
      });
      setProduct(updated);
      if (updated.variants && updated.variants.length > 0) {
        setVariantList((prev) => {
          if (prev.length <= 1 && updated.variants[0]) {
            return [{
              ...prev[0],
              id: updated.variants[0].id,
              price: Number(updated.variants[0].price),
              compareAtPrice: updated.variants[0].compareAtPrice ? Number(updated.variants[0].compareAtPrice) : null,
            }];
          }
          return prev;
        });
      }
      showFeedback('success', 'Đã cập nhật thông tin sản phẩm và giá bán thành công!');
      onProductUpdated();
    } catch (err: any) {
      showFeedback('error', err.response?.data?.message || 'Lưu thông tin thất bại.');
    } finally {
      setIsSavingDetails(false);
    }
  };

  // 2. Lưu danh sách biến thể & giá bán
  const handleSaveVariants = async () => {
    if (variantList.length === 0) {
      showFeedback('error', 'Sản phẩm phải có ít nhất 1 biến thể.');
      return;
    }

    for (let i = 0; i < variantList.length; i++) {
      const v = variantList[i];
      if (!v.name?.trim()) {
        showFeedback('error', `Vui lòng nhập tên cho biến thể #${i + 1} (VD: Gói 1 tháng, Gói 1 năm, Gói VIP).`);
        return;
      }
      if (v.price <= 0) {
        showFeedback('error', `Giá bán của biến thể #${i + 1} ("${v.name}") phải lớn hơn 0 đ.`);
        return;
      }
      if (v.compareAtPrice && v.compareAtPrice < v.price) {
        showFeedback(
          'error',
          `Giá gốc (so sánh) của biến thể #${i + 1} ("${v.name}") phải lớn hơn hoặc bằng giá bán.`
        );
        return;
      }
    }

    setIsSavingVariants(true);
    setFeedback(null);
    try {
      const cleanVariants = variantList.map((v, i) => {
        const vName = (v.name || '').trim() || `Gói ${i + 1}`;
        return {
          ...v,
          name: vName,
          optionValues: [vName],
        };
      });

      const uniqueOptionValues = Array.from(new Set(cleanVariants.map((v) => v.name)));

      const payloadVariants = cleanVariants.map((v) => ({
        id: v.id,
        sku: v.sku?.trim() || undefined,
        price: Number(v.price),
        compareAtPrice: v.compareAtPrice ? Number(v.compareAtPrice) : null,
        deliveryType: typeof v.deliveryType === 'number' ? v.deliveryType : 1,
        trackInventory: v.trackInventory,
        optionValues: [v.name],
      }));

      const updated = await marketplaceApi.updateShopProductVariants(productId, {
        options: [
          {
            name: 'Gói sản phẩm',
            position: 0,
            values: uniqueOptionValues,
          },
        ],
        variants: payloadVariants,
      });

      setProduct(updated);
      const mappedVariants = updated.variants.map((v: SellerProductVariantDto, idx: number) => {
        const vName = v.name || (v.optionValues && v.optionValues.length > 0 ? v.optionValues.join(' - ') : `Gói ${idx + 1}`);
        return {
          id: v.id,
          sku: v.sku || '',
          name: vName,
          price: Number(v.price) || 0,
          compareAtPrice: v.compareAtPrice ? Number(v.compareAtPrice) : null,
          deliveryType: v.deliveryType || 1,
          trackInventory: v.trackInventory ?? true,
          optionValues: [vName],
          availableStock: v.availableStock || 0,
        };
      });
      setVariantList(mappedVariants);
      if (mappedVariants[0]) {
        setDetailsForm((prev) => ({
          ...prev,
          price: mappedVariants[0].price,
          compareAtPrice: mappedVariants[0].compareAtPrice || null,
        }));
      }
      showFeedback('success', 'Đã cập nhật bảng giá và các biến thể thành công!');
      onProductUpdated();
    } catch (err: any) {
      showFeedback('error', err.response?.data?.message || 'Cập nhật biến thể thất bại.');
    } finally {
      setIsSavingVariants(false);
    }
  };

  const handleAddVariant = () => {
    const nextIdx = variantList.length + 1;
    const defaultName = nextIdx === 1 ? 'Gói 1 tháng' : nextIdx === 2 ? 'Gói 1 năm' : nextIdx === 3 ? 'Gói VIP' : `Gói ${nextIdx}`;
    setVariantList((prev) => [
      ...prev,
      {
        sku: '',
        name: defaultName,
        price: 50000,
        compareAtPrice: 100000,
        deliveryType: 1,
        trackInventory: true,
        optionValues: [defaultName],
        availableStock: 0,
      },
    ]);
  };

  const handleRemoveVariant = (index: number) => {
    if (variantList.length <= 1) {
      showFeedback('error', 'Sản phẩm phải có ít nhất một biến thể.');
      return;
    }
    setVariantList((prev) => prev.filter((_, i) => i !== index));
  };

  const handleUpdateVariantField = (index: number, field: string, value: any) => {
    setVariantList((prev) => {
      const copy = [...prev];
      copy[index] = { ...copy[index], [field]: value };
      return copy;
    });
  };

  // 3. Quản lý hình ảnh
  const handleUploadFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 10 * 1024 * 1024) {
      showFeedback('error', 'Kích thước tệp ảnh vượt quá 10MB.');
      return;
    }

    setIsUploadingImage(true);
    setFeedback(null);
    try {
      const nextPos = images.length;
      await marketplaceApi.uploadAndAddShopProductImage(
        productId,
        file,
        isCoverNewImage || images.length === 0,
        nextPos
      );

      // Tải lại chi tiết ảnh mới nhất
      const updated = await marketplaceApi.getShopProductById(productId);
      setImages(updated.images || []);
      setProduct(updated);
      showFeedback('success', 'Đã tải ảnh lên và lưu vào sản phẩm thành công!');
      onProductUpdated();
      if (fileInputRef.current) fileInputRef.current.value = '';
    } catch (err: any) {
      showFeedback('error', err.response?.data?.message || 'Tải ảnh lên thất bại.');
    } finally {
      setIsUploadingImage(false);
    }
  };

  const handleAddImageUrl = async () => {
    if (!imageUrlInput.trim()) {
      showFeedback('error', 'Vui lòng nhập đường dẫn URL ảnh.');
      return;
    }

    setIsUploadingImage(true);
    setFeedback(null);
    try {
      const nextPos = images.length;
      await marketplaceApi.addShopProductImage(productId, {
        url: imageUrlInput.trim(),
        position: nextPos,
        isCover: isCoverNewImage || images.length === 0,
      });

      const updated = await marketplaceApi.getShopProductById(productId);
      setImages(updated.images || []);
      setProduct(updated);
      setImageUrlInput('');
      showFeedback('success', 'Đã thêm hình ảnh thành công!');
      onProductUpdated();
    } catch (err: any) {
      showFeedback('error', err.response?.data?.message || 'Thêm ảnh thất bại.');
    } finally {
      setIsUploadingImage(false);
    }
  };

  const handleSetCoverImage = async (imageId: string) => {
    setImageActionLoading(imageId);
    setFeedback(null);
    try {
      const reordered = images.map((img, idx) => ({
        imageId: img.id,
        position: img.id === imageId ? 0 : idx + 1,
        isCover: img.id === imageId,
      }));

      await marketplaceApi.reorderShopProductImages(productId, reordered);
      const updated = await marketplaceApi.getShopProductById(productId);
      setImages(updated.images || []);
      setProduct(updated);
      showFeedback('success', 'Đã đổi ảnh đại diện thành công!');
      onProductUpdated();
    } catch (err: any) {
      showFeedback('error', err.response?.data?.message || 'Không thể đổi ảnh đại diện.');
    } finally {
      setImageActionLoading(null);
    }
  };

  const handleDeleteImage = async (imageId: string) => {
    if (!confirm('Bạn có chắc chắn muốn xóa hình ảnh này khỏi sản phẩm?')) return;

    setImageActionLoading(imageId);
    setFeedback(null);
    try {
      await marketplaceApi.deleteShopProductImage(productId, imageId);
      const updated = await marketplaceApi.getShopProductById(productId);
      setImages(updated.images || []);
      setProduct(updated);
      showFeedback('success', 'Đã xóa hình ảnh thành công!');
      onProductUpdated();
    } catch (err: any) {
      showFeedback('error', err.response?.data?.message || 'Xóa ảnh thất bại.');
    } finally {
      setImageActionLoading(null);
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="modal-overlay"
      onClick={(e) => {
        if (e.target === e.currentTarget && !isSavingDetails && !isSavingVariants && !isUploadingImage) {
          onClose();
        }
      }}
      role="dialog"
      aria-modal="true"
    >
      <div
        className="modal-content"
        style={{
          maxWidth: '720px',
          width: '100%',
          maxHeight: '90vh',
          display: 'flex',
          flexDirection: 'column',
          borderRadius: '16px',
          overflow: 'hidden',
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* MODAL HEADER */}
        <div
          style={{
            padding: '1.25rem 1.5rem',
            borderBottom: '1px solid var(--border-color)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            background: 'rgba(255, 255, 255, 0.02)',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem' }}>
            <div
              style={{
                width: '36px',
                height: '36px',
                borderRadius: '8px',
                background: 'rgba(99, 102, 241, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--accent-primary)',
              }}
            >
              <EditIcon size={20} />
            </div>
            <div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
                  {product?.name || 'Chỉnh sửa sản phẩm'}
                </h3>
                {product && (
                  <Badge
                    variant={
                      product.status === 'Listed'
                        ? 'success'
                        : product.status === 'Draft'
                        ? 'neutral'
                        : product.status === 'Hidden'
                        ? 'warning'
                        : 'danger'
                    }
                    size="sm"
                  >
                    {product.status === 'Listed'
                      ? 'Đang bán'
                      : product.status === 'Draft'
                      ? 'Bản nháp'
                      : product.status === 'Hidden'
                      ? 'Tạm ẩn'
                      : product.status}
                  </Badge>
                )}
              </div>
              {product && (
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem', marginTop: '0.2rem', fontSize: '0.8rem', color: 'var(--text-dim)' }}>
                  <span>
                    Slug: <code>{product.slug}</code>
                  </span>
                  <a
                    href={`/marketplace/products/${product.slug}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    style={{
                      color: 'var(--accent-primary)',
                      textDecoration: 'none',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '0.25rem',
                      fontWeight: 600,
                    }}
                  >
                    Xem trang bán <ExternalLinkIcon size={12} />
                  </a>
                </div>
              )}
            </div>
          </div>

          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={onClose}
            disabled={isSavingDetails || isSavingVariants || isUploadingImage}
            style={{ borderRadius: '50%', width: '34px', height: '34px', padding: 0 }}
          >
            <CloseIcon size={18} />
          </Button>
        </div>

        {/* FEEDBACK BANNER */}
        {feedback && (
          <div
            style={{
              padding: '0.75rem 1.25rem',
              background: feedback.type === 'success' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)',
              borderBottom: feedback.type === 'success' ? '1px solid rgba(16, 185, 129, 0.3)' : '1px solid rgba(239, 68, 68, 0.3)',
              color: feedback.type === 'success' ? '#34d399' : '#f87171',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem',
              fontSize: '0.86rem',
              fontWeight: 600,
            }}
          >
            {feedback.type === 'success' ? <CheckCircleIcon size={18} /> : <AlertCircleIcon size={18} />}
            <span>{feedback.message}</span>
          </div>
        )}

        {/* TABS NAVIGATION */}
        <div
          style={{
            display: 'flex',
            borderBottom: '1px solid var(--border-color)',
            background: 'var(--bg-surface)',
            padding: '0.4rem 1.25rem 0',
            gap: '0.5rem',
          }}
        >
          <button
            type="button"
            onClick={() => setActiveTab('details')}
            style={{
              padding: '0.65rem 1rem',
              background: 'none',
              border: 'none',
              borderBottom: activeTab === 'details' ? '2px solid var(--accent-primary)' : '2px solid transparent',
              color: activeTab === 'details' ? 'var(--accent-primary)' : 'var(--text-dim)',
              fontWeight: activeTab === 'details' ? 700 : 500,
              fontSize: '0.88rem',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '0.45rem',
              transition: 'all 0.2s ease',
            }}
          >
            <TagIcon size={16} /> 1. Thông tin chung
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('variants')}
            style={{
              padding: '0.65rem 1rem',
              background: 'none',
              border: 'none',
              borderBottom: activeTab === 'variants' ? '2px solid var(--accent-primary)' : '2px solid transparent',
              color: activeTab === 'variants' ? 'var(--accent-primary)' : 'var(--text-dim)',
              fontWeight: activeTab === 'variants' ? 700 : 500,
              fontSize: '0.88rem',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '0.45rem',
              transition: 'all 0.2s ease',
            }}
          >
            <PackageIcon size={16} /> 2. Bảng giá & Biến thể ({variantList.length})
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('images')}
            style={{
              padding: '0.65rem 1rem',
              background: 'none',
              border: 'none',
              borderBottom: activeTab === 'images' ? '2px solid var(--accent-primary)' : '2px solid transparent',
              color: activeTab === 'images' ? 'var(--accent-primary)' : 'var(--text-dim)',
              fontWeight: activeTab === 'images' ? 700 : 500,
              fontSize: '0.88rem',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '0.45rem',
              transition: 'all 0.2s ease',
            }}
          >
            <ImageIcon size={16} /> 3. Bộ sưu tập ảnh ({images.length})
          </button>
        </div>

        {/* MODAL BODY */}
        <div style={{ padding: '1.5rem', overflowY: 'auto', flex: 1 }}>
          {isLoading ? (
            <div style={{ textAlign: 'center', padding: '3.5rem 1rem', color: 'var(--text-dim)' }}>
              <Spinner size="md" />
              <div style={{ marginTop: '0.75rem', fontSize: '0.88rem' }}>Đang tải dữ liệu sản phẩm...</div>
            </div>
          ) : loadError ? (
            <div style={{ textAlign: 'center', padding: '3rem 1rem', color: '#f87171' }}>
              <AlertCircleIcon size={36} style={{ marginBottom: '0.5rem' }} />
              <div>{loadError}</div>
              <Button type="button" variant="secondary" size="sm" onClick={loadProductDetail} style={{ marginTop: '1rem' }}>
                Thử lại
              </Button>
            </div>
          ) : (
            <>
              {/* TAB 1: THÔNG TIN CƠ BẢN */}
              {activeTab === 'details' && (
                <form onSubmit={handleSaveDetails} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
                  <FormField id="edit-prod-name" label="Tên sản phẩm" required hint="Tối đa 200 ký tự">
                    <Input
                      id="edit-prod-name"
                      placeholder="Ví dụ: Tài khoản Netflix Premium 1 Tháng 4K..."
                      value={detailsForm.name}
                      onChange={(e) => setDetailsForm((prev) => ({ ...prev, name: e.target.value }))}
                      required
                    />
                  </FormField>

                  <FormField id="edit-prod-category" label="Danh mục sản phẩm" hint="Chọn danh mục chuẩn để khách hàng dễ tìm kiếm">
                    <Select
                      id="edit-prod-category"
                      value={detailsForm.categoryId}
                      onChange={(e) => setDetailsForm((prev) => ({ ...prev, categoryId: e.target.value }))}
                    >
                      <option value="">-- Chưa gắn danh mục --</option>
                      {flatCategories.map((c) => (
                        <option key={c.id} value={c.id}>
                          {'\u00A0'.repeat(c.level * 3)}
                          {c.level > 0 ? '└ ' : ''}
                          {c.name}
                        </option>
                      ))}
                    </Select>
                  </FormField>

                  {/* GIÁ BÁN SẢN PHẨM: Cho sản phẩm đơn hoặc banner điều hướng sang Tab Biến thể */}
                  {variantList.length <= 1 ? (
                    <div
                      style={{
                        padding: '1.25rem',
                        background: 'rgba(56, 189, 248, 0.05)',
                        border: '1px solid rgba(56, 189, 248, 0.2)',
                        borderRadius: '12px',
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '0.9rem',
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                        <span style={{ fontWeight: 700, fontSize: '0.92rem', color: 'var(--text-main)', display: 'flex', alignItems: 'center', gap: '6px' }}>
                          💰 Giá bán sản phẩm
                        </span>
                        <Badge variant="neutral" size="sm">Gói tiêu chuẩn</Badge>
                      </div>

                      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
                        <FormField id="edit-prod-price" label="Giá bán (VND) *" required hint="Số tiền khách hàng thanh toán">
                          <Input
                            id="edit-prod-price"
                            type="number"
                            min={1000}
                            step={1000}
                            placeholder="50000"
                            value={detailsForm.price || ''}
                            onChange={(e) => {
                              const val = Number(e.target.value);
                              setDetailsForm((prev) => ({ ...prev, price: val }));
                              setVariantList((prev) => {
                                if (prev.length === 0) return prev;
                                const copy = [...prev];
                                copy[0] = { ...copy[0], price: val };
                                return copy;
                              });
                            }}
                            required
                          />
                        </FormField>

                        <FormField id="edit-prod-compare-price" label="Giá gốc / So sánh (VND)" hint="Hiển thị gạch ngang (nếu có giảm giá)">
                          <Input
                            id="edit-prod-compare-price"
                            type="number"
                            min={0}
                            step={1000}
                            placeholder="100000"
                            value={detailsForm.compareAtPrice || ''}
                            onChange={(e) => {
                              const val = e.target.value ? Number(e.target.value) : null;
                              setDetailsForm((prev) => ({ ...prev, compareAtPrice: val }));
                              setVariantList((prev) => {
                                if (prev.length === 0) return prev;
                                const copy = [...prev];
                                copy[0] = { ...copy[0], compareAtPrice: val };
                                return copy;
                              });
                            }}
                          />
                        </FormField>
                      </div>
                    </div>
                  ) : (
                    <div
                      style={{
                        padding: '1rem 1.25rem',
                        background: 'rgba(99, 102, 241, 0.08)',
                        border: '1px solid rgba(99, 102, 241, 0.25)',
                        borderRadius: '12px',
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        gap: '1rem',
                        flexWrap: 'wrap',
                      }}
                    >
                      <div>
                        <div style={{ fontWeight: 700, fontSize: '0.92rem', color: 'var(--text-main)' }}>
                          💎 Sản phẩm có {variantList.length} gói biến thể khác nhau
                        </div>
                        <div style={{ fontSize: '0.82rem', color: 'var(--text-dim)', marginTop: '2px' }}>
                          Khoảng giá hiện tại: {Math.min(...variantList.map((v) => v.price)).toLocaleString('vi-VN')} đ ~ {Math.max(...variantList.map((v) => v.price)).toLocaleString('vi-VN')} đ
                        </div>
                      </div>
                      <Button
                        type="button"
                        variant="secondary"
                        size="sm"
                        onClick={() => setActiveTab('variants')}
                      >
                        Chỉnh sửa giá từng gói ➔
                      </Button>
                    </div>
                  )}

                  <FormField
                    id="edit-prod-desc"
                    label="Mô tả sản phẩm"
                    hint="Thông tin chi tiết về sản phẩm, tính năng, điều khoản sử dụng hoặc bảo hành"
                  >
                    <Textarea
                      id="edit-prod-desc"
                      rows={5}
                      placeholder="Mô tả chi tiết sản phẩm, cam kết bảo hành, lưu ý khi kích hoạt..."
                      value={detailsForm.description}
                      onChange={(e) => setDetailsForm((prev) => ({ ...prev, description: e.target.value }))}
                    />
                  </FormField>

                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '0.5rem' }}>
                    <Button type="button" variant="ghost" onClick={onClose} disabled={isSavingDetails}>
                      Đóng
                    </Button>
                    <Button type="submit" variant="primary" loading={isSavingDetails} icon={<CheckIcon size={16} />}>
                      Lưu thông tin & Giá bán
                    </Button>
                  </div>
                </form>
              )}

              {/* TAB 2: BIẾN THỂ & BẢNG GIÁ */}
              {activeTab === 'variants' && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                      <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)', margin: 0 }}>
                        Danh sách các gói & Biến thể
                      </h4>
                      <p style={{ margin: '0.2rem 0 0', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                        Quản lý tên gói (Gói 1 tháng, Gói 1 năm, Gói VIP...), giá bán và SKU. Tồn kho tự động tăng khi nạp kho mã số.
                      </p>
                    </div>
                    <Button
                      type="button"
                      variant="secondary"
                      size="xs"
                      icon={<PlusIcon size={14} />}
                      onClick={handleAddVariant}
                    >
                      + Thêm biến thể
                    </Button>
                  </div>

                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
                    {variantList.map((v, idx) => (
                      <div
                        key={v.id || `temp-${idx}`}
                        style={{
                          padding: '1.25rem',
                          background: 'var(--bg-surface)',
                          borderRadius: '12px',
                          border: '1px solid var(--border-color)',
                          display: 'flex',
                          flexDirection: 'column',
                          gap: '1rem',
                        }}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem' }}>
                            <span style={{ fontWeight: 800, fontSize: '0.95rem', color: 'var(--text-main)' }}>
                              #{idx + 1}. {v.name || `Biến thể #${idx + 1}`}
                            </span>
                            <Badge variant={v.availableStock && v.availableStock > 0 ? 'success' : 'danger'} size="sm">
                              Tồn kho: {v.availableStock || 0} mã
                            </Badge>
                            {v.id && (
                              <Badge variant="neutral" size="sm">
                                ID: {v.id.slice(0, 8)}
                              </Badge>
                            )}
                          </div>

                          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            {v.id && onOpenAddStock && (
                              <Button
                                type="button"
                                variant="secondary"
                                size="xs"
                                onClick={() => onOpenAddStock(v.id!)}
                                style={{ color: 'var(--accent-primary)', fontWeight: 600 }}
                              >
                                + Nạp tài khoản / kho
                              </Button>
                            )}
                            {variantList.length > 1 && (
                              <Button
                                type="button"
                                variant="danger"
                                size="xs"
                                icon={<TrashIcon size={14} />}
                                onClick={() => handleRemoveVariant(idx)}
                              >
                                Xóa
                              </Button>
                            )}
                          </div>
                        </div>

                        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '0.85rem' }}>
                          <FormField id={`var-name-${idx}`} label="Tên gói / Biến thể *" required hint="VD: Gói 1 tháng, Gói 1 năm, Gói VIP">
                            <Input
                              id={`var-name-${idx}`}
                              placeholder="VD: Gói 1 tháng, Gói VIP..."
                              value={v.name || ''}
                              onChange={(e) => handleUpdateVariantField(idx, 'name', e.target.value)}
                              required
                            />
                          </FormField>

                          <FormField id={`var-price-${idx}`} label="Giá bán (VND) *" required>
                            <Input
                              id={`var-price-${idx}`}
                              type="number"
                              min={1000}
                              step={1000}
                              placeholder="50000"
                              value={v.price || ''}
                              onChange={(e) => handleUpdateVariantField(idx, 'price', Number(e.target.value))}
                              required
                            />
                          </FormField>

                          <FormField id={`var-compare-${idx}`} label="Giá gốc / So sánh (VND)" hint="Hiển thị gạch ngang">
                            <Input
                              id={`var-compare-${idx}`}
                              type="number"
                              min={0}
                              step={1000}
                              placeholder="100000"
                              value={v.compareAtPrice || ''}
                              onChange={(e) =>
                                handleUpdateVariantField(
                                  idx,
                                  'compareAtPrice',
                                  e.target.value ? Number(e.target.value) : null
                                )
                              }
                            />
                          </FormField>

                          <FormField id={`var-sku-${idx}`} label="Mã SKU (Tùy chọn)">
                            <Input
                              id={`var-sku-${idx}`}
                              placeholder="VD: NF-1M-4K"
                              value={v.sku}
                              onChange={(e) => handleUpdateVariantField(idx, 'sku', e.target.value)}
                            />
                          </FormField>
                        </div>
                      </div>
                    ))}
                  </div>

                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '0.5rem' }}>
                    <Button type="button" variant="ghost" onClick={onClose} disabled={isSavingVariants}>
                      Đóng
                    </Button>
                    <Button
                      type="button"
                      variant="primary"
                      loading={isSavingVariants}
                      icon={<CheckIcon size={16} />}
                      onClick={handleSaveVariants}
                    >
                      Lưu bảng giá & Biến thể
                    </Button>
                  </div>
                </div>
              )}

              {/* TAB 3: BỘ SƯU TẬP HÌNH ẢNH */}
              {activeTab === 'images' && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
                  {/* Danh sách ảnh hiện tại */}
                  <div>
                    <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)', margin: '0 0 0.75rem' }}>
                      Hình ảnh hiện có ({images.length})
                    </h4>
                    {images.length === 0 ? (
                      <div
                        style={{
                          textAlign: 'center',
                          padding: '2rem 1rem',
                          background: 'var(--bg-surface)',
                          borderRadius: '12px',
                          border: '1px dashed var(--border-color)',
                          color: 'var(--text-dim)',
                          fontSize: '0.85rem',
                        }}
                      >
                        <ImageIcon size={32} style={{ marginBottom: '0.5rem', color: 'var(--text-dim)' }} />
                        <div>Sản phẩm này chưa có hình ảnh nào. Hãy tải ảnh lên bên dưới.</div>
                      </div>
                    ) : (
                      <div
                        style={{
                          display: 'grid',
                          gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))',
                          gap: '0.85rem',
                        }}
                      >
                        {images.map((img) => (
                          <div
                            key={img.id}
                            style={{
                              position: 'relative',
                              borderRadius: '10px',
                              overflow: 'hidden',
                              border: img.isCover ? '2px solid var(--accent-primary)' : '1px solid var(--border-color)',
                              background: 'var(--bg-surface)',
                              aspectRatio: '16/10',
                              display: 'flex',
                              flexDirection: 'column',
                            }}
                          >
                            <img
                              src={img.url}
                              alt="Ảnh sản phẩm"
                              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                            />

                            {/* Badge Cover */}
                            {img.isCover && (
                              <div
                                style={{
                                  position: 'absolute',
                                  top: '6px',
                                  left: '6px',
                                  background: 'var(--accent-primary)',
                                  color: '#fff',
                                  fontSize: '0.7rem',
                                  fontWeight: 700,
                                  padding: '2px 6px',
                                  borderRadius: '4px',
                                  boxShadow: '0 2px 4px rgba(0,0,0,0.5)',
                                }}
                              >
                                Ảnh bìa
                              </div>
                            )}

                            {/* Overlay nút thao tác */}
                            <div
                              style={{
                                position: 'absolute',
                                bottom: 0,
                                insetInline: 0,
                                background: 'rgba(0, 0, 0, 0.75)',
                                padding: '0.35rem 0.5rem',
                                display: 'flex',
                                justifyContent: 'space-between',
                                alignItems: 'center',
                              }}
                            >
                              {!img.isCover ? (
                                <button
                                  type="button"
                                  onClick={() => handleSetCoverImage(img.id)}
                                  disabled={imageActionLoading === img.id}
                                  style={{
                                    background: 'none',
                                    border: 'none',
                                    color: 'var(--accent-primary)',
                                    fontSize: '0.72rem',
                                    fontWeight: 700,
                                    cursor: 'pointer',
                                    padding: 0,
                                  }}
                                >
                                  Đặt làm bìa
                                </button>
                              ) : (
                                <span style={{ fontSize: '0.72rem', color: '#34d399', fontWeight: 600 }}>
                                  Mặc định
                                </span>
                              )}

                              <button
                                type="button"
                                onClick={() => handleDeleteImage(img.id)}
                                disabled={imageActionLoading === img.id}
                                style={{
                                  background: 'none',
                                  border: 'none',
                                  color: '#f87171',
                                  cursor: 'pointer',
                                  padding: 0,
                                  display: 'flex',
                                  alignItems: 'center',
                                }}
                                title="Xóa ảnh này"
                              >
                                <TrashIcon size={14} />
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* Khu vực tải thêm ảnh mới */}
                  <div
                    style={{
                      background: 'var(--bg-surface)',
                      padding: '1.25rem',
                      borderRadius: '12px',
                      border: '1px solid var(--border-color)',
                    }}
                  >
                    <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--text-main)', margin: '0 0 0.75rem' }}>
                      + Thêm hình ảnh mới
                    </h4>

                    {/* Mode selector */}
                    <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem' }}>
                      <Button
                        type="button"
                        variant={imageInputMode === 'upload' ? 'primary' : 'secondary'}
                        size="xs"
                        onClick={() => setImageInputMode('upload')}
                      >
                        Tải từ máy tính
                      </Button>
                      <Button
                        type="button"
                        variant={imageInputMode === 'url' ? 'primary' : 'secondary'}
                        size="xs"
                        onClick={() => setImageInputMode('url')}
                      >
                        Nhập link ảnh URL
                      </Button>
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
                      {imageInputMode === 'upload' ? (
                        <div>
                          <input
                            ref={fileInputRef}
                            type="file"
                            accept="image/png, image/jpeg, image/webp, image/gif"
                            onChange={handleUploadFile}
                            disabled={isUploadingImage}
                            style={{ display: 'none' }}
                          />
                          <div
                            onClick={() => fileInputRef.current?.click()}
                            style={{
                              border: '2px dashed var(--border-color)',
                              borderRadius: '10px',
                              padding: '1.5rem',
                              textAlign: 'center',
                              cursor: isUploadingImage ? 'not-allowed' : 'pointer',
                              background: 'rgba(255, 255, 255, 0.02)',
                              transition: 'all 0.2s ease',
                            }}
                          >
                            <ImageIcon size={30} style={{ color: 'var(--accent-primary)', marginBottom: '0.4rem' }} />
                            <div style={{ fontSize: '0.88rem', fontWeight: 600, color: 'var(--text-main)' }}>
                              {isUploadingImage ? 'Đang tải lên Cloudinary...' : 'Nhấp để chọn ảnh từ thiết bị'}
                            </div>
                            <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                              Hỗ trợ JPG, PNG, WebP (Tối đa 10MB)
                            </div>
                          </div>
                        </div>
                      ) : (
                        <div style={{ display: 'flex', gap: '0.65rem' }}>
                          <Input
                            placeholder="https://example.com/image.png"
                            value={imageUrlInput}
                            onChange={(e) => setImageUrlInput(e.target.value)}
                            disabled={isUploadingImage}
                          />
                          <Button
                            type="button"
                            variant="primary"
                            size="sm"
                            loading={isUploadingImage}
                            onClick={handleAddImageUrl}
                          >
                            Thêm ảnh
                          </Button>
                        </div>
                      )}

                      <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.82rem', color: 'var(--text-muted)', cursor: 'pointer' }}>
                        <input
                          type="checkbox"
                          checked={isCoverNewImage}
                          onChange={(e) => setIsCoverNewImage(e.target.checked)}
                        />
                        <span>Đặt ảnh này làm ảnh đại diện chính (Ảnh bìa)</span>
                      </label>
                    </div>
                  </div>

                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
                    <Button type="button" variant="ghost" onClick={onClose}>
                      Đóng
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};
