import React, { useState, useEffect } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminProductItem } from '@/types/admin';
import {
  SearchIcon,
  RefreshIcon,
  AlertTriangleIcon,
  CheckCircleIcon,
} from '@/components/common/Icons';


interface AdminProductsTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminProductsTab: React.FC<AdminProductsTabProps> = ({ showFlash }) => {
  const [products, setProducts] = useState<AdminProductItem[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [page, setPage] = useState<number>(1);
  const [pageSize] = useState<number>(20);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [totalPages, setTotalPages] = useState<number>(1);

  // Filters
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [onlyTakedown, setOnlyTakedown] = useState<boolean>(false);

  // Takedown Modal
  const [takedownModal, setTakedownModal] = useState<{
    isOpen: boolean;
    product: AdminProductItem;
    reason: string;
  } | null>(null);

  // Lift Takedown Modal
  const [liftModal, setLiftModal] = useState<{
    isOpen: boolean;
    product: AdminProductItem;
  } | null>(null);

  const loadProducts = async (targetPage = page) => {
    try {
      setLoading(true);
      const res = await adminApi.getAdminProducts({
        search: searchTerm.trim() || undefined,
        status: statusFilter ? Number(statusFilter) : undefined,
        page: targetPage,
        pageSize,
      });

      let items = res.items || [];
      if (onlyTakedown) {
        items = items.filter((p) => p.hasActiveTakedown);
      }

      setProducts(items);
      setTotalCount(res.totalCount || 0);
      setTotalPages(res.totalPages || 1);
      setPage(res.page || targetPage);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể tải danh sách sản phẩm.', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProducts(1);
  }, [statusFilter, onlyTakedown]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    loadProducts(1);
  };

  const handleConfirmTakedown = async () => {
    if (!takedownModal || !takedownModal.reason.trim()) {
      alert('Vui lòng nhập lý do gỡ bỏ sản phẩm vi phạm.');
      return;
    }
    try {
      await adminApi.takedownProduct(takedownModal.product.id, takedownModal.reason.trim());
      showFlash(`Đã gỡ bỏ sản phẩm "${takedownModal.product.name}" vi phạm chính sách!`);
      setTakedownModal(null);
      loadProducts(page);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi gỡ bỏ sản phẩm.', 'error');
    }
  };

  const handleConfirmLiftTakedown = async () => {
    if (!liftModal) return;
    try {
      await adminApi.liftTakedown(liftModal.product.id);
      showFlash(`Đã dỡ bỏ lệnh khóa cho sản phẩm "${liftModal.product.name}"!`);
      setLiftModal(null);
      loadProducts(page);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi dỡ bỏ lệnh khóa.', 'error');
    }
  };

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Listed':
        return { label: 'Đang mở bán', bg: 'rgba(34, 197, 94, 0.15)', color: '#22c55e' };
      case 'PendingReview':
        return { label: 'Chờ duyệt', bg: 'rgba(245, 158, 11, 0.15)', color: '#f59e0b' };
      case 'Hidden':
        return { label: 'Tạm ẩn', bg: 'rgba(234, 179, 8, 0.15)', color: '#eab308' };
      case 'Archived':
        return { label: 'Đã lưu trữ', bg: 'rgba(239, 68, 68, 0.15)', color: '#ef4444' };
      default:
        return { label: 'Bản nháp', bg: 'rgba(148, 163, 184, 0.15)', color: '#94a3b8' };
    }
  };

  return (
    <div>
      {/* Search & Filter Bar */}
      <div
        style={{
          background: '#ffffff',
          padding: '1.25rem 1.5rem',
          borderRadius: '16px',
          marginBottom: '1.5rem',
          border: '1px solid #e2e8f0',
          boxShadow: '0 1px 3px rgba(0,0,0,0.05)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
        <form
          onSubmit={handleSearchSubmit}
          style={{ display: 'flex', gap: '0.75rem', flex: 1, minWidth: '280px', maxWidth: '500px' }}
        >
          <div style={{ position: 'relative', flex: 1 }}>
            <input
              type="text"
              className="form-control"
              placeholder="Tìm theo tên sản phẩm, slug..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{
                paddingLeft: '2.5rem',
                background: '#ffffff',
                border: '1px solid #cbd5e1',
                color: '#0f172a',
                fontWeight: 500,
              }}
            />
            <span
              style={{
                position: 'absolute',
                left: '0.85rem',
                top: '50%',
                transform: 'translateY(-50%)',
                color: '#64748b',
              }}
            >
              <SearchIcon size={16} />
            </span>
          </div>
          <button type="submit" className="btn btn-secondary btn-sm" style={{ fontWeight: 600 }}>
            Tìm
          </button>
        </form>

        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <select
            className="form-control"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            style={{
              minWidth: '150px',
              background: '#ffffff',
              border: '1px solid #cbd5e1',
              color: '#0f172a',
              fontWeight: 500,
            }}
          >
            <option value="">Tất cả trạng thái</option>
            <option value="1">Bản nháp (Draft)</option>
            <option value="2">Chờ duyệt (PendingReview)</option>
            <option value="3">Đang bán (Listed)</option>
            <option value="4">Tạm ẩn (Hidden)</option>
            <option value="5">Lưu trữ (Archived)</option>
          </select>

          <label
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '6px',
              fontSize: '0.88rem',
              color: onlyTakedown ? '#e11d48' : '#334155',
              cursor: 'pointer',
              fontWeight: 600,
            }}
          >
            <input
              type="checkbox"
              checked={onlyTakedown}
              onChange={(e) => setOnlyTakedown(e.target.checked)}
            />
            Chỉ xem bị Takedown
          </label>

          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={() => loadProducts(page)}
            style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', fontWeight: 600 }}
          >
            <RefreshIcon size={14} /> Làm mới
          </button>
        </div>
      </div>

      {loading && (
        <div style={{ padding: '2rem', textAlign: 'center' }}>
          <div className="spinner" style={{ margin: '0 auto 0.5rem' }} />
          <p style={{ color: '#64748b', fontSize: '0.88rem', fontWeight: 500 }}>Đang nạp danh sách sản phẩm...</p>
        </div>
      )}

      {/* Products Table */}
      <div className="admin-table-card">
        <div style={{ overflowX: 'auto' }}>
          <table className="admin-table">
            <thead>
              <tr>
                <th>Sản phẩm</th>
                <th>Gian hàng</th>
                <th>Danh mục</th>
                <th>Khoảng giá (VNĐ)</th>
                <th style={{ textAlign: 'center' }}>Biến thể</th>
                <th>Trạng thái</th>
                <th>Tình trạng Vi phạm</th>
                <th style={{ textAlign: 'right' }}>Thao tác Admin</th>
              </tr>
            </thead>
            <tbody>
              {products.length === 0 ? (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '3.5rem', color: '#64748b', fontWeight: 500 }}>
                    Không tìm thấy sản phẩm nào phù hợp với bộ lọc.
                  </td>
                </tr>
              ) : (
                products.map((p) => {
                  const badge = getStatusBadge(p.status);
                  return (
                    <tr
                      key={p.id}
                      style={{
                        background: p.hasActiveTakedown ? '#fff1f2' : undefined,
                      }}
                    >
                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                          {p.coverImageUrl ? (
                            <img
                              src={p.coverImageUrl}
                              alt={p.name}
                              style={{ width: '40px', height: '40px', borderRadius: '8px', objectFit: 'cover', border: '1px solid #e2e8f0' }}
                            />
                          ) : (
                            <div
                              style={{
                                width: '40px',
                                height: '40px',
                                borderRadius: '8px',
                                background: '#f1f5f9',
                                border: '1px solid #e2e8f0',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                fontSize: '1.2rem',
                              }}
                            >
                              📦
                            </div>
                          )}
                          <div>
                            <div className="admin-cell-title">{p.name}</div>
                            <div className="admin-cell-subtitle" style={{ fontFamily: 'monospace' }}>
                              /{p.slug}
                            </div>
                          </div>
                        </div>
                      </td>

                      <td style={{ padding: '0.85rem 1.25rem', fontWeight: 600, color: '#0284c7' }}>
                        {p.shopName || '(Chưa xác định)'}
                      </td>

                      <td style={{ padding: '0.85rem 1.25rem', color: '#334155', fontWeight: 500 }}>
                        {p.categoryName || '-'}
                      </td>

                      <td style={{ padding: '0.85rem 1.25rem', fontWeight: 700, color: '#0f172a' }}>
                        {p.minPrice === p.maxPrice
                          ? formatCurrency(p.minPrice)
                          : `${formatCurrency(p.minPrice)} - ${formatCurrency(p.maxPrice)}`}
                      </td>

                      <td style={{ padding: '0.85rem 1.25rem', textAlign: 'center' }}>
                        <span
                          style={{
                            padding: '3px 10px',
                            borderRadius: '12px',
                            background: '#f1f5f9',
                            border: '1px solid #cbd5e1',
                            fontSize: '0.82rem',
                            fontWeight: 700,
                            color: '#0f172a',
                          }}
                        >
                          {p.variantCount}
                        </span>
                      </td>

                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        <span
                          style={{
                            fontSize: '0.8rem',
                            padding: '4px 9px',
                            borderRadius: '6px',
                            fontWeight: 700,
                            background: badge.bg,
                            color: badge.color,
                          }}
                        >
                          {badge.label}
                        </span>
                      </td>

                      <td style={{ padding: '0.85rem 1.25rem' }}>
                        {p.hasActiveTakedown ? (
                          <span
                            style={{
                              fontSize: '0.78rem',
                              padding: '4px 10px',
                              borderRadius: '6px',
                              fontWeight: 700,
                              background: '#fee2e2',
                              color: '#b91c1c',
                              border: '1px solid #fca5a5',
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '5px',
                            }}
                          >
                            <AlertTriangleIcon size={13} /> BỊ TAKEDOWN
                          </span>
                        ) : (
                          <span
                            style={{
                              fontSize: '0.78rem',
                              padding: '4px 10px',
                              borderRadius: '6px',
                              fontWeight: 600,
                              background: '#ecfdf5',
                              color: '#047857',
                              border: '1px solid #a7f3d0',
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '5px',
                            }}
                          >
                            <CheckCircleIcon size={13} /> Hợp lệ
                          </span>
                        )}
                      </td>

                      <td style={{ padding: '0.85rem 1.25rem', textAlign: 'right' }}>
                        <div style={{ display: 'inline-flex', gap: '6px' }}>
                          <a
                            href={`/marketplace/products/${p.slug}`}
                            target="_blank"
                            rel="noreferrer"
                            className="btn btn-secondary btn-sm"
                            style={{ padding: '4px 8px', fontSize: '0.78rem', fontWeight: 600 }}
                            title="Xem chi tiết trên sàn"
                          >
                            Xem sàn ↗
                          </a>

                          {p.hasActiveTakedown ? (
                            <button
                              type="button"
                              className="btn btn-success btn-sm"
                              style={{ padding: '4px 8px', fontSize: '0.78rem', fontWeight: 700 }}
                              onClick={() => setLiftModal({ isOpen: true, product: p })}
                            >
                              Dỡ khóa (Lift)
                            </button>
                          ) : (
                            <button
                              type="button"
                              className="btn btn-danger btn-sm"
                              style={{ padding: '4px 8px', fontSize: '0.78rem', fontWeight: 700 }}
                              onClick={() => setTakedownModal({ isOpen: true, product: p, reason: '' })}
                            >
                              Takedown
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination Bar */}
        {totalPages > 1 && (
          <div
            style={{
              padding: '1rem 1.5rem',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              borderTop: '1px solid #e2e8f0',
              background: '#f8fafc',
            }}
          >
            <span style={{ fontSize: '0.88rem', color: '#475569', fontWeight: 600 }}>
              Trang {page} / {totalPages} ({totalCount} sản phẩm)
            </span>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                disabled={page <= 1}
                onClick={() => loadProducts(page - 1)}
                style={{ fontWeight: 600 }}
              >
                Trước
              </button>
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                disabled={page >= totalPages}
                onClick={() => loadProducts(page + 1)}
                style={{ fontWeight: 600 }}
              >
                Tiếp
              </button>
            </div>
          </div>
        )}
      </div>

      {/* MODAL 1: TAKEDOWN PRODUCT */}
      {takedownModal && (
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px' }}>
            <h3 style={{ margin: '0 0 0.5rem', fontWeight: 800, color: '#e11d48' }}>
              Gỡ bỏ vi phạm (Takedown)
            </h3>
            <p style={{ fontSize: '0.9rem', color: '#334155', marginBottom: '1.25rem', lineHeight: 1.5 }}>
              Sản phẩm: <strong style={{ color: '#0f172a' }}>{takedownModal.product.name}</strong> (Shop: <span style={{ color: '#0284c7', fontWeight: 600 }}>{takedownModal.product.shopName}</span>)
            </p>

            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>
                Lý do vi phạm chính sách * (sẽ ghi vào hồ sơ kiểm duyệt và thông báo tới Shop)
              </label>
              <textarea
                className="form-control"
                rows={4}
                placeholder="Ví dụ: Bán tài khoản dùng chung trái phép, vi phạm bản quyền phần mềm, mô tả sai lệch nghiêm trọng..."
                value={takedownModal.reason}
                onChange={(e) => setTakedownModal({ ...takedownModal, reason: e.target.value })}
                autoFocus
                style={{
                  background: '#ffffff',
                  border: '1px solid #cbd5e1',
                  color: '#0f172a',
                  fontWeight: 500,
                }}
              />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setTakedownModal(null)} style={{ fontWeight: 600 }}>
                Hủy
              </button>
              <button type="button" className="btn btn-danger" onClick={handleConfirmTakedown} style={{ fontWeight: 700 }}>
                Xác nhận gỡ bỏ
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 2: LIFT TAKEDOWN */}
      {liftModal && (
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '460px', padding: '2rem', borderRadius: '16px' }}>
            <h3 style={{ margin: '0 0 0.5rem', fontWeight: 800, color: '#059669' }}>
              Dỡ bỏ lệnh khóa (Lift Takedown)
            </h3>
            <p style={{ fontSize: '0.9rem', color: '#334155', marginBottom: '1.25rem', lineHeight: 1.5 }}>
              Bạn có chắc chắn muốn dỡ bỏ lệnh khóa đối với sản phẩm <strong style={{ color: '#0f172a' }}>&quot;{liftModal.product.name}&quot;</strong>?
              Sản phẩm sẽ được khôi phục trạng thái hiển thị bình thường trên chợ nếu gian hàng đang mở cửa.
            </p>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setLiftModal(null)} style={{ fontWeight: 600 }}>
                Hủy
              </button>
              <button type="button" className="btn btn-primary" onClick={handleConfirmLiftTakedown} style={{ fontWeight: 700 }}>
                Dỡ bỏ lệnh khóa
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
