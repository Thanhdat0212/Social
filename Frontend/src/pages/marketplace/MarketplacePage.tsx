import React, { useCallback, useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { Category, ProductListItem } from '@/types/marketplace';
import {
  PageContainer,
  Input,
  Select,
  SearchIcon,
  ProductCardSkeleton,
  EmptyState,
  ErrorState,
  Button,
  KeyIcon,
  ShieldIcon,
  CheckCheckIcon,
  FilterIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
} from '@/components/common';
import { ProductCard } from '@/components/marketplace/ProductCard';
import { marketplaceRecommendationApi } from '@/api/marketplaceRecommendationApi';

type PricePreset = 'all' | 'under50k' | '50k-200k' | '200k-500k' | 'over500k' | 'custom';
type SortOption = 'newest' | 'price_asc' | 'price_desc' | 'name';

export const MarketplacePage: React.FC = () => {
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [products, setProducts] = useState<ProductListItem[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [totalCount, setTotalCount] = useState(0);

  // Bộ lọc khoảng giá
  const [pricePreset, setPricePreset] = useState<PricePreset>('all');
  const [customMin, setCustomMin] = useState<string>('');
  const [customMax, setCustomMax] = useState<string>('');

  // Sắp xếp & Phân trang
  const [sort, setSort] = useState<SortOption>('newest');
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(12);

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchInput.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  // Load danh mục
  useEffect(() => {
    const loadCategories = async () => {
      try {
        const tree = await marketplaceApi.getCategoriesTree();
        setCategories(tree || []);
      } catch (err) {
        console.error('Lỗi khi tải danh mục:', err);
      }
    };
    loadCategories();
  }, []);

  // Tính toán minPrice và maxPrice gửi lên backend
  const computePriceBounds = useCallback((): { minPrice?: number; maxPrice?: number } => {
    switch (pricePreset) {
      case 'under50k':
        return { maxPrice: 50000 };
      case '50k-200k':
        return { minPrice: 50000, maxPrice: 200000 };
      case '200k-500k':
        return { minPrice: 200000, maxPrice: 500000 };
      case 'over500k':
        return { minPrice: 500000 };
      case 'custom': {
        const min = customMin ? parseInt(customMin, 10) : undefined;
        const max = customMax ? parseInt(customMax, 10) : undefined;
        return {
          minPrice: min && !isNaN(min) ? min : undefined,
          maxPrice: max && !isNaN(max) ? max : undefined,
        };
      }
      case 'all':
      default:
        return {};
    }
  }, [pricePreset, customMin, customMax]);

  // Load sản phẩm
  const loadProducts = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const { minPrice, maxPrice } = computePriceBounds();
      const res = await marketplaceApi.getProducts({
        categoryId: selectedCategory || undefined,
        search: debouncedSearch || undefined,
        minPrice,
        maxPrice,
        sort,
        page,
        pageSize,
      });
      setProducts(res.items || []);
      setTotalCount(res.totalCount || 0);

      // Lưu từ khóa tìm kiếm vào hệ thống phân tích nhu cầu người dùng
      if (debouncedSearch && page === 1) {
        marketplaceRecommendationApi.recordSearch({
          queryText: debouncedSearch,
          resultCount: res.totalCount || 0,
        });
      }
    } catch (err: any) {
      console.error('Lỗi khi tải sản phẩm:', err);
      setError(err?.response?.data?.message || 'Không thể tải danh sách sản phẩm. Vui lòng thử lại sau.');
    } finally {
      setIsLoading(false);
    }
  }, [selectedCategory, debouncedSearch, computePriceBounds, sort, page, pageSize]);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  const handleCategorySelect = (catId: string | null) => {
    setSelectedCategory(catId);
    setPage(1);
  };

  const handlePricePresetSelect = (preset: PricePreset) => {
    setPricePreset(preset);
    setPage(1);
  };

  const handleSortChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    setSort(e.target.value as SortOption);
    setPage(1);
  };

  const handleResetFilters = () => {
    setSearchInput('');
    setDebouncedSearch('');
    setSelectedCategory(null);
    setPricePreset('all');
    setCustomMin('');
    setCustomMax('');
    setSort('newest');
    setPage(1);
  };

  const hasActiveFilters =
    Boolean(debouncedSearch) ||
    selectedCategory !== null ||
    pricePreset !== 'all' ||
    sort !== 'newest';


  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <PageContainer variant="wide" style={{ padding: '1.25rem 1rem 4rem' }}>
      {/* Compact Hero Banner */}
      <section
        className="market-hero"
        style={{
          padding: '1.75rem 2rem',
          borderRadius: '16px',
          background: 'linear-gradient(135deg, #0b1329 0%, #0f1f38 50%, #0d172a 100%)',
          border: '1px solid rgba(56, 189, 248, 0.25)',
          boxShadow: '0 8px 24px -4px rgba(15, 23, 42, 0.35)',
          marginBottom: '1.5rem',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1.25rem' }}>
          <div>
            <div
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.4rem',
                color: '#38bdf8',
                background: 'rgba(56, 189, 248, 0.12)',
                padding: '0.28rem 0.75rem',
                borderRadius: '999px',
                border: '1px solid rgba(56, 189, 248, 0.28)',
                fontSize: '0.75rem',
                fontWeight: 700,
                textTransform: 'uppercase',
                letterSpacing: '0.05em',
                marginBottom: '0.55rem',
              }}
            >
              <KeyIcon size={13} />
              <span>Chợ tài khoản & Bản quyền số</span>
            </div>
            <h1 style={{ fontSize: '1.75rem', fontWeight: 800, color: '#ffffff', margin: '0 0 0.45rem', letterSpacing: '-0.02em' }}>
              Chợ sản phẩm số
            </h1>
            <p style={{ color: '#cbd5e1', fontSize: '0.92rem', margin: 0, maxWidth: '580px', lineHeight: '1.55' }}>
              Khám phá bản quyền phần mềm, khóa học trực tuyến, key kích hoạt và tài khoản dịch vụ số với hệ thống kho tự động 24/7.
            </p>
          </div>

          {/* Service Commitment Badges */}
          <div style={{ display: 'flex', gap: '0.85rem', flexWrap: 'wrap', alignItems: 'center' }}>
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.65rem',
                background: 'rgba(255, 255, 255, 0.06)',
                padding: '0.6rem 0.95rem',
                borderRadius: '12px',
                border: '1px solid rgba(255, 255, 255, 0.12)',
                backdropFilter: 'blur(8px)',
              }}
            >
              <div
                style={{
                  width: '32px',
                  height: '32px',
                  borderRadius: '8px',
                  background: 'rgba(56, 189, 248, 0.15)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <KeyIcon size={16} style={{ color: '#38bdf8' }} />
              </div>
              <div>
                <div style={{ fontSize: '0.8rem', fontWeight: 700, color: '#ffffff', lineHeight: 1.25 }}>Giao tự động</div>
                <div style={{ fontSize: '0.72rem', color: '#94a3b8', fontWeight: 500, marginTop: '2px' }}>Nhận mã tức thì</div>
              </div>
            </div>

            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.65rem',
                background: 'rgba(255, 255, 255, 0.06)',
                padding: '0.6rem 0.95rem',
                borderRadius: '12px',
                border: '1px solid rgba(255, 255, 255, 0.12)',
                backdropFilter: 'blur(8px)',
              }}
            >
              <div
                style={{
                  width: '32px',
                  height: '32px',
                  borderRadius: '8px',
                  background: 'rgba(52, 211, 153, 0.15)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <ShieldIcon size={16} style={{ color: '#34d399' }} />
              </div>
              <div>
                <div style={{ fontSize: '0.8rem', fontWeight: 700, color: '#ffffff', lineHeight: 1.25 }}>Bảo vệ 48h</div>
                <div style={{ fontSize: '0.72rem', color: '#94a3b8', fontWeight: 500, marginTop: '2px' }}>Khiếu nại an toàn</div>
              </div>
            </div>

            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.65rem',
                background: 'rgba(255, 255, 255, 0.06)',
                padding: '0.6rem 0.95rem',
                borderRadius: '12px',
                border: '1px solid rgba(255, 255, 255, 0.12)',
                backdropFilter: 'blur(8px)',
              }}
            >
              <div
                style={{
                  width: '32px',
                  height: '32px',
                  borderRadius: '8px',
                  background: 'rgba(251, 191, 36, 0.15)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <CheckCheckIcon size={16} style={{ color: '#fbbf24' }} />
              </div>
              <div>
                <div style={{ fontSize: '0.8rem', fontWeight: 700, color: '#ffffff', lineHeight: 1.25 }}>VietQR Napas</div>
                <div style={{ fontSize: '0.72rem', color: '#94a3b8', fontWeight: 500, marginTop: '2px' }}>Khớp lệnh chuẩn xác</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Search Bar */}
      <div style={{ marginBottom: '1.25rem' }}>
        <Input
          leftIcon={<SearchIcon size={18} />}
          placeholder="Tìm kiếm tài khoản, khóa học, key phần mềm..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          clearable
          onClear={() => setSearchInput('')}
          inputSize="lg"
          aria-label="Tìm kiếm sản phẩm"
        />
      </div>

      {/* Category Pills (Horizontal scrollable) */}
      <div
        className="category-pill-list"
        role="tablist"
        aria-label="Danh mục sản phẩm"
        style={{ marginBottom: '0.85rem' }}
      >
        <button
          type="button"
          role="tab"
          aria-selected={selectedCategory === null}
          className={`category-pill ${selectedCategory === null ? 'active' : ''}`}
          onClick={() => handleCategorySelect(null)}
        >
          Tất cả danh mục
        </button>
        {categories.map((c) => {
          const isRootActive = selectedCategory === c.id || c.children?.some((sub) => sub.id === selectedCategory);
          return (
            <button
              key={c.id}
              type="button"
              role="tab"
              aria-selected={isRootActive}
              className={`category-pill ${isRootActive ? 'active' : ''}`}
              onClick={() => handleCategorySelect(c.id)}
            >
              {c.name}
            </button>
          );
        })}
      </div>

      {/* Subcategory Pills if active root category has children */}
      {(() => {
        const activeRoot = categories.find(
          (c) => c.id === selectedCategory || c.children?.some((sub) => sub.id === selectedCategory)
        );
        if (!activeRoot?.children || activeRoot.children.length === 0) return null;

        return (
          <div
            style={{
              display: 'flex',
              gap: '0.45rem',
              flexWrap: 'wrap',
              alignItems: 'center',
              marginBottom: '1.25rem',
              padding: '0.45rem 0.8rem',
              background: 'rgba(255, 255, 255, 0.03)',
              borderRadius: '12px',
              border: '1px solid var(--border-color)',
            }}
          >
            <span style={{ fontSize: '0.78rem', fontWeight: 600, color: 'var(--text-dim)', marginRight: '0.2rem' }}>
              Phân loại:
            </span>
            <button
              type="button"
              className={`category-pill ${selectedCategory === activeRoot.id ? 'active' : ''}`}
              style={{ fontSize: '0.78rem', padding: '0.24rem 0.7rem' }}
              onClick={() => handleCategorySelect(activeRoot.id)}
            >
              Tất cả {activeRoot.name}
            </button>
            {activeRoot.children.map((sub) => (
              <button
                key={sub.id}
                type="button"
                className={`category-pill ${selectedCategory === sub.id ? 'active' : ''}`}
                style={{ fontSize: '0.78rem', padding: '0.24rem 0.7rem' }}
                onClick={() => handleCategorySelect(sub.id)}
              >
                {sub.name}
              </button>
            ))}
          </div>
        );
      })()}

      {/* Advanced Filter & Sorting Toolbar */}
      <div
        style={{
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '14px',
          padding: '1rem 1.25rem',
          marginBottom: '1.5rem',
          display: 'flex',
          flexDirection: 'column',
          gap: '1rem',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.75rem' }}>
          {/* Price Range Presets */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
            <span style={{ fontSize: '0.82rem', fontWeight: 700, color: 'var(--text-dim)', display: 'inline-flex', alignItems: 'center', gap: '0.35rem', marginRight: '0.25rem' }}>
              <FilterIcon size={14} /> Mức giá:
            </span>
            {[
              { id: 'all', label: 'Tất cả' },
              { id: 'under50k', label: '< 50k' },
              { id: '50k-200k', label: '50k - 200k' },
              { id: '200k-500k', label: '200k - 500k' },
              { id: 'over500k', label: '> 500k' },
              { id: 'custom', label: 'Tùy chỉnh' },
            ].map((p) => (
              <button
                key={p.id}
                type="button"
                onClick={() => handlePricePresetSelect(p.id as PricePreset)}
                style={{
                  fontSize: '0.78rem',
                  fontWeight: 600,
                  padding: '0.3rem 0.75rem',
                  borderRadius: '20px',
                  border: pricePreset === p.id ? '1px solid var(--accent-primary)' : '1px solid var(--border-color)',
                  background: pricePreset === p.id ? 'rgba(14, 165, 233, 0.15)' : 'rgba(255, 255, 255, 0.03)',
                  color: pricePreset === p.id ? 'var(--accent-primary)' : 'var(--text-muted)',
                  cursor: 'pointer',
                  transition: 'all 0.15s ease',
                }}
              >
                {p.label}
              </button>
            ))}
          </div>

          {/* Sort & Reset Actions */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
              <span style={{ fontSize: '0.82rem', color: 'var(--text-dim)', whiteSpace: 'nowrap' }}>Sắp xếp:</span>
              <Select
                value={sort}
                onChange={handleSortChange}
                aria-label="Sắp xếp sản phẩm"
                style={{ padding: '0.35rem 0.65rem', fontSize: '0.82rem', minWidth: '150px' }}
              >
                <option value="newest">Mới nhất</option>
                <option value="price_asc">Giá: Thấp đến Cao</option>
                <option value="price_desc">Giá: Cao đến Thấp</option>
                <option value="name">Tên sản phẩm A-Z</option>
              </Select>
            </div>

            {hasActiveFilters && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleResetFilters}
                style={{ fontSize: '0.78rem', color: '#f87171' }}
              >
                Đặt lại
              </Button>
            )}
          </div>
        </div>

        {/* Custom Price Range Row (when 'custom' preset is selected) */}
        {pricePreset === 'custom' && (
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.75rem',
              paddingTop: '0.75rem',
              borderTop: '1px dashed var(--border-color)',
              flexWrap: 'wrap',
            }}
          >
            <span style={{ fontSize: '0.82rem', color: 'var(--text-muted)' }}>Khoảng giá mong muốn (VND):</span>
            <div style={{ width: '130px' }}>
              <Input
                type="number"
                placeholder="Từ (đ)"
                value={customMin}
                onChange={(e) => setCustomMin(e.target.value)}
                inputSize="sm"
              />
            </div>
            <span style={{ color: 'var(--text-dim)' }}>—</span>
            <div style={{ width: '130px' }}>
              <Input
                type="number"
                placeholder="Đến (đ)"
                value={customMax}
                onChange={(e) => setCustomMax(e.target.value)}
                inputSize="sm"
              />
            </div>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => {
                setPage(1);
                loadProducts();
              }}
              style={{ fontSize: '0.78rem' }}
            >
              Áp dụng giá
            </Button>
          </div>
        )}
      </div>

      {/* Products Content */}
      {isLoading ? (
        <div className="products-grid">
          {[1, 2, 3, 4, 5, 6, 7, 8].map((i) => (
            <ProductCardSkeleton key={i} />
          ))}
        </div>
      ) : error ? (
        <ErrorState
          title="Không thể tải sản phẩm"
          message={error}
          onRetry={loadProducts}
          retryText="Thử lại"
        />
      ) : products.length === 0 ? (
        <EmptyState
          title="Không tìm thấy sản phẩm phù hợp"
          description={
            hasActiveFilters
              ? 'Không có sản phẩm nào thỏa mãn các tiêu chí lọc hiện tại. Thử bỏ bớt bộ lọc hoặc chọn từ khóa khác.'
              : 'Hiện chưa có sản phẩm nào được bày bán trong danh mục này.'
          }
          action={
            hasActiveFilters ? (
              <Button variant="secondary" size="sm" onClick={handleResetFilters}>
                Xóa tất cả bộ lọc
              </Button>
            ) : undefined
          }
        />
      ) : (
        <>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', color: 'var(--text-dim)', fontSize: '0.82rem', flexWrap: 'wrap', gap: '0.75rem' }}>
            <div>
              Hiển thị <strong>{products.length}</strong> / <strong>{totalCount}</strong> sản phẩm số
            </div>
            
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-dim)' }}>Hiển thị:</span>
              <Select
                value={pageSize.toString()}
                onChange={(e) => {
                  setPageSize(Number(e.target.value));
                  setPage(1);
                }}
                aria-label="Số sản phẩm mỗi trang"
              >
                <option value="12">12 sản phẩm / trang</option>
                <option value="24">24 sản phẩm / trang</option>
                <option value="48">48 sản phẩm / trang</option>
              </Select>
            </div>
          </div>

          <div className="products-grid">
            {products.map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>

          {/* Phân trang số trang chuẩn xác */}
          {totalPages > 1 && (
            <div
              style={{
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                gap: '0.4rem',
                marginTop: '2.5rem',
                flexWrap: 'wrap',
              }}
            >
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  setPage((prev) => Math.max(1, prev - 1));
                  window.scrollTo({ top: 0, behavior: 'smooth' });
                }}
                disabled={page <= 1}
                icon={<ChevronLeftIcon size={16} />}
              >
                Trước
              </Button>

              {(() => {
                const pages: (number | string)[] = [];
                if (totalPages <= 7) {
                  for (let i = 1; i <= totalPages; i++) pages.push(i);
                } else {
                  pages.push(1);
                  if (page > 3) pages.push('...');
                  const start = Math.max(2, page - 1);
                  const end = Math.min(totalPages - 1, page + 1);
                  for (let i = start; i <= end; i++) pages.push(i);
                  if (page < totalPages - 2) pages.push('...');
                  pages.push(totalPages);
                }

                return pages.map((p, idx) => {
                  if (p === '...') {
                    return (
                      <span
                        key={`ellipsis-${idx}`}
                        style={{ padding: '0 0.35rem', color: 'var(--text-dim)', fontSize: '0.9rem' }}
                      >
                        …
                      </span>
                    );
                  }

                  const pageNum = Number(p);
                  const isActive = pageNum === page;

                  return (
                    <button
                      key={pageNum}
                      type="button"
                      onClick={() => {
                        setPage(pageNum);
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                      }}
                      style={{
                        minWidth: '36px',
                        height: '36px',
                        padding: '0 0.5rem',
                        borderRadius: '8px',
                        fontSize: '0.85rem',
                        fontWeight: isActive ? 700 : 500,
                        border: isActive ? '1px solid var(--accent-primary)' : '1px solid var(--border-color)',
                        background: isActive ? 'var(--accent-primary)' : 'rgba(255, 255, 255, 0.04)',
                        color: isActive ? '#ffffff' : 'var(--text-main)',
                        cursor: 'pointer',
                        transition: 'all 0.15s ease',
                      }}
                    >
                      {pageNum}
                    </button>
                  );
                });
              })()}

              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  setPage((prev) => Math.min(totalPages, prev + 1));
                  window.scrollTo({ top: 0, behavior: 'smooth' });
                }}
                disabled={page >= totalPages}
                icon={<ChevronRightIcon size={16} />}
              >
                Sau
              </Button>
            </div>
          )}
        </>
      )}
    </PageContainer>
  );
};

export default MarketplacePage;
