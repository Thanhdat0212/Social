import React, { useEffect, useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminShopItem, AdminShopDetail, AdminShopListStats } from '@/types/admin';
import {
  StoreIcon,
  PackageIcon,
  EyeIcon,
  ExternalLinkIcon,
  AlertTriangleIcon,
  SearchIcon,
  RefreshIcon,
  CartIcon,
} from '@/components/common/Icons';

interface AdminShopsTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminShopsTab: React.FC<AdminShopsTabProps> = ({ showFlash }) => {
  const [shops, setShops] = useState<AdminShopItem[]>([]);
  const [stats, setStats] = useState<AdminShopListStats | null>(null);
  const [shopFilter, setShopFilter] = useState<string>(''); // Default 'Tất cả' để hiển thị toàn bộ
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [selectedShopDetail, setSelectedShopDetail] = useState<AdminShopDetail | null>(null);
  const [detailTab, setDetailTab] = useState<'orders' | 'products' | 'sanctions' | 'reviews'>('orders');
  const [loading, setLoading] = useState(false);

  // Modals
  const [rejectShopModal, setRejectShopModal] = useState<{
    isOpen: boolean;
    shopId: string;
    reasonCode: string;
    reasonNote: string;
  } | null>(null);

  const [sanctionModal, setSanctionModal] = useState<{
    isOpen: boolean;
    shopId: string;
    type: number;
    reason: string;
    days?: number;
  } | null>(null);

  const loadShops = async (status: string, search: string = searchTerm) => {
    setLoading(true);
    try {
      const res = await adminApi.getShops({
        status: status || undefined,
        search: search.trim() || undefined,
        pageSize: 50,
      });
      setShops(res.items || []);
      if (res.stats) {
        setStats(res.stats);
      }
    } catch (err) {
      console.error('Error loading shops:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadShops(shopFilter, searchTerm);
  }, [shopFilter]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    loadShops(shopFilter, searchTerm);
  };

  const handleViewDetail = async (shopId: string) => {
    try {
      const detail = await adminApi.getShopDetail(shopId);
      setSelectedShopDetail(detail);
      setDetailTab('orders');
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể tải thông tin chi tiết gian hàng.', 'error');
    }
  };

  const handleApproveShop = async (shopId: string) => {
    if (!window.confirm('Xác nhận phê duyệt cho gian hàng này đi vào hoạt động kinh doanh?')) return;
    try {
      await adminApi.approveShop(shopId);
      showFlash('Đã phê duyệt gian hàng thành công!');
      loadShops(shopFilter);
      if (selectedShopDetail?.id === shopId) {
        const updated = await adminApi.getShopDetail(shopId);
        setSelectedShopDetail(updated);
      }
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể duyệt gian hàng.', 'error');
    }
  };

  const handleConfirmRejectShop = async () => {
    if (!rejectShopModal) return;
    if (!rejectShopModal.reasonNote.trim()) {
      alert('Vui lòng nhập lý do từ chối chi tiết.');
      return;
    }
    try {
      await adminApi.rejectShop(rejectShopModal.shopId, {
        reasonCode: rejectShopModal.reasonCode,
        reasonNote: rejectShopModal.reasonNote,
        canResubmit: true,
      });
      showFlash('Đã từ chối gian hàng và gửi lý do tới chủ shop.');
      setRejectShopModal(null);
      loadShops(shopFilter);
      if (selectedShopDetail?.id === rejectShopModal.shopId) {
        const updated = await adminApi.getShopDetail(rejectShopModal.shopId);
        setSelectedShopDetail(updated);
      }
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Có lỗi xảy ra khi từ chối.', 'error');
    }
  };

  const handleConfirmSanction = async () => {
    if (!sanctionModal) return;
    if (!sanctionModal.reason.trim()) {
      alert('Vui lòng nhập lý do áp dụng chế tài.');
      return;
    }
    try {
      const endAt = sanctionModal.days
        ? new Date(Date.now() + sanctionModal.days * 24 * 60 * 60 * 1000).toISOString()
        : undefined;
      await adminApi.addSanction(sanctionModal.shopId, {
        type: sanctionModal.type,
        reason: sanctionModal.reason,
        endAt,
      });
      showFlash('Đã ban hành chế tài xử phạt gian hàng.');
      setSanctionModal(null);
      loadShops(shopFilter);
      if (selectedShopDetail?.id === sanctionModal.shopId) {
        const updated = await adminApi.getShopDetail(sanctionModal.shopId);
        setSelectedShopDetail(updated);
      }
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi áp dụng chế tài.', 'error');
    }
  };

  const handleLiftSanction = async (shopId: string, sanctionId: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn dỡ bỏ quyết định xử phạt này trước thời hạn?')) return;
    try {
      await adminApi.liftSanction(shopId, sanctionId);
      showFlash('Đã gỡ bỏ chế tài xử phạt thành công.');
      loadShops(shopFilter);
      if (selectedShopDetail?.id === shopId) {
        const updated = await adminApi.getShopDetail(shopId);
        setSelectedShopDetail(updated);
      }
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể dỡ bỏ chế tài.', 'error');
    }
  };

  return (
    <div>
      {/* 1. TOP METRIC STATS CARDS */}
      <div className="admin-stats-grid">
        <div className="admin-stat-card stat-blue">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="admin-stat-label">Tổng Gian Hàng</span>
            <span className="admin-stat-icon icon-blue">
              <StoreIcon size={18} />
            </span>
          </div>
          <div className="admin-stat-value val-blue">
            {stats ? stats.totalShops : shops.length}
          </div>
          <div className="admin-stat-sub">
            Đang hoạt động: <strong style={{ color: '#059669', fontWeight: 700 }}>{stats?.approvedShops ?? 0}</strong> | Chờ duyệt: <strong style={{ color: '#d97706', fontWeight: 700 }}>{stats?.pendingShops ?? 0}</strong>
          </div>
        </div>

        <div className="admin-stat-card stat-green">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="admin-stat-label">Tổng Số Đơn Đã Bán Ra</span>
            <span className="admin-stat-icon icon-green">
              <CartIcon size={18} />
            </span>
          </div>
          <div className="admin-stat-value val-green">
            {stats ? stats.totalOrdersSold.toLocaleString('vi-VN') : 0} <span style={{ fontSize: '1rem', fontWeight: 600, color: '#64748b' }}>đơn</span>
          </div>
          <div className="admin-stat-sub">
            Đơn hàng số hoàn tất giao dịch toàn sàn
          </div>
        </div>

        <div className="admin-stat-card stat-purple">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="admin-stat-label">Tổng Doanh Số Toàn Sàn</span>
            <span className="admin-stat-icon icon-purple">
              <PackageIcon size={18} />
            </span>
          </div>
          <div className="admin-stat-value val-purple">
            {stats ? Number(stats.totalRevenue).toLocaleString('vi-VN') : 0} <span style={{ fontSize: '0.95rem', fontWeight: 600, color: '#64748b' }}>đ</span>
          </div>
          <div className="admin-stat-sub">
            Tổng giá trị GMV thanh toán qua sàn
          </div>
        </div>

        <div className="admin-stat-card stat-amber">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span className="admin-stat-label">Chờ Admin Duyệt</span>
            <span className="admin-stat-icon icon-amber">
              <AlertTriangleIcon size={18} />
            </span>
          </div>
          <div className="admin-stat-value val-amber">
            {stats ? stats.pendingShops : 0} <span style={{ fontSize: '1rem', fontWeight: 600, color: '#64748b' }}>shop</span>
          </div>
          <div className="admin-stat-sub" style={{ color: stats && stats.pendingShops > 0 ? '#b45309' : '#059669', fontWeight: 600 }}>
            {stats && stats.pendingShops > 0 ? '● Cần xem xét phê duyệt ngay' : '✓ Đã duyệt hết toàn bộ hồ sơ'}
          </div>
        </div>
      </div>

      {/* 2. FILTER TABS & SEARCH BAR */}
      <div className="admin-filter-bar">
        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
          {[
            { key: '', label: 'Tất cả', count: stats?.totalShops },
            { key: 'Approved', label: 'Đã duyệt', count: stats?.approvedShops },
            { key: 'Pending', label: 'Chờ duyệt', count: stats?.pendingShops },
            { key: 'Rejected', label: 'Từ chối', count: stats?.rejectedShops },
          ].map((tab) => (
            <button
              key={tab.key}
              type="button"
              className={`admin-filter-btn ${shopFilter === tab.key ? 'active' : ''}`}
              onClick={() => setShopFilter(tab.key)}
            >
              <span>{tab.label}</span>
              {typeof tab.count === 'number' && (
                <span className="admin-filter-badge">
                  {tab.count}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* Search Input */}
        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <div style={{ position: 'relative' }}>
            <span style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: '#64748b' }}>
              <SearchIcon size={15} />
            </span>
            <input
              type="text"
              className="form-control"
              placeholder="Tìm theo tên shop, @slug..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{
                paddingLeft: '34px',
                height: '38px',
                fontSize: '0.88rem',
                width: '240px',
                background: '#ffffff',
                border: '1px solid #cbd5e1',
                color: '#0f172a',
                borderRadius: '10px',
              }}
            />
          </div>
          <button
            type="submit"
            className="btn btn-secondary btn-sm"
            style={{
              height: '38px',
              padding: '0 1rem',
              background: '#ffffff',
              border: '1px solid #cbd5e1',
              color: '#0f172a',
              fontWeight: 600,
              borderRadius: '10px',
            }}
          >
            Tìm kiếm
          </button>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            title="Làm mới danh sách"
            onClick={() => {
              setSearchTerm('');
              loadShops(shopFilter, '');
            }}
            style={{
              height: '38px',
              width: '38px',
              padding: 0,
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              background: '#ffffff',
              border: '1px solid #cbd5e1',
              color: '#334155',
              borderRadius: '10px',
            }}
          >
            <RefreshIcon size={15} />
          </button>
        </form>
      </div>

      {/* 3. SHOPS DATA TABLE */}
      <div className="admin-table-card">
        <div style={{ overflowX: 'auto' }}>
          <table className="admin-table">
            <thead>
              <tr>
                <th>Gian hàng</th>
                <th>Chủ sở hữu & Liên hệ</th>
                <th style={{ textAlign: 'center' }}>Đơn đã bán</th>
                <th style={{ textAlign: 'center' }}>Sản phẩm</th>
                <th style={{ textAlign: 'right' }}>Doanh số & Số dư</th>
                <th>Trạng thái</th>
                <th style={{ textAlign: 'right' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                    <div style={{ display: 'inline-flex', alignItems: 'center', gap: '8px' }}>
                      <RefreshIcon size={18} className="spin" />
                      <span>Đang tải danh sách gian hàng và tổng hợp số liệu...</span>
                    </div>
                  </td>
                </tr>
              ) : shops.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ padding: '3.5rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                    <StoreIcon size={36} style={{ margin: '0 auto 12px', opacity: 0.4, display: 'block' }} />
                    <p style={{ margin: 0, fontWeight: 500 }}>Không tìm thấy gian hàng nào trong danh sách lọc.</p>
                    {shopFilter !== '' && (
                      <button
                        type="button"
                        className="btn btn-sm btn-secondary"
                        onClick={() => setShopFilter('')}
                        style={{ marginTop: '12px' }}
                      >
                        Xem tất cả gian hàng
                      </button>
                    )}
                  </td>
                </tr>
              ) : (
                shops.map((s) => (
                  <tr key={s.id}>
                    {/* Gian hàng Logo & Tên */}
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                        {s.logoUrl ? (
                          <img
                            src={s.logoUrl}
                            alt={s.name}
                            style={{ width: '42px', height: '42px', borderRadius: '10px', objectFit: 'cover', border: '1px solid #e2e8f0' }}
                          />
                        ) : (
                          <div
                            style={{
                              width: '42px',
                              height: '42px',
                              borderRadius: '10px',
                              background: 'linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              fontWeight: 800,
                              color: '#ffffff',
                              fontSize: '1.05rem',
                            }}
                          >
                            {s.name.slice(0, 1).toUpperCase()}
                          </div>
                        )}
                        <div>
                          <div className="admin-cell-title">{s.name}</div>
                          <a
                            href={`/marketplace/shops/${s.slug}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="admin-cell-link"
                          >
                            @{s.slug}
                            <ExternalLinkIcon size={12} />
                          </a>
                        </div>
                      </div>
                    </td>

                    {/* Chủ sở hữu */}
                    <td>
                      <div style={{ color: '#0f172a', fontWeight: 600 }}>{s.ownerDisplayName || 'Chủ gian hàng'}</div>
                      <div className="admin-cell-subtitle">{s.ownerEmail || s.contactEmail}</div>
                    </td>

                    {/* Tổng số đơn đã bán ra */}
                    <td style={{ textAlign: 'center' }}>
                      <span className="admin-pill-badge admin-pill-blue">
                        {(s.totalOrders || 0).toLocaleString()} đơn
                      </span>
                    </td>

                    {/* Số lượng sản phẩm */}
                    <td style={{ textAlign: 'center', color: '#0f172a', fontWeight: 600 }}>
                      {(s.totalProducts || 0).toLocaleString()} SP
                    </td>

                    {/* Doanh số & Số dư */}
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ fontWeight: 800, color: '#059669', fontSize: '0.95rem' }}>
                        {Number(s.totalRevenue || 0).toLocaleString('vi-VN')} đ
                      </div>
                      <div className="admin-cell-subtitle">
                        Khả dụng: <strong style={{ color: '#0f172a' }}>{Number(s.availableBalance || 0).toLocaleString('vi-VN')} đ</strong>
                      </div>
                    </td>

                    {/* Trạng thái */}
                    <td>
                      <span
                        className={`badge ${
                          s.approvalStatus === 'Approved' || s.approvalStatus === 1
                            ? 'badge-success'
                            : s.approvalStatus === 'Rejected' || s.approvalStatus === 2
                            ? 'badge-danger'
                            : 'badge-warning'
                        }`}
                        style={{ fontSize: '0.78rem', padding: '4px 10px', fontWeight: 700 }}
                      >
                        {s.approvalStatus === 'Approved' || s.approvalStatus === 1
                          ? 'Đã duyệt'
                          : s.approvalStatus === 'Rejected' || s.approvalStatus === 2
                          ? 'Từ chối'
                          : 'Chờ duyệt'}
                      </span>
                    </td>

                    {/* Thao tác */}
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.45rem' }}>
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          style={{
                            display: 'inline-flex',
                            alignItems: 'center',
                            gap: '4px',
                            padding: '0.35rem 0.75rem',
                            background: '#ffffff',
                            border: '1px solid #cbd5e1',
                            color: '#0f172a',
                            fontWeight: 600,
                          }}
                          onClick={() => handleViewDetail(s.id)}
                        >
                          <EyeIcon size={14} />
                          <span>Chi tiết</span>
                        </button>

                        {(s.approvalStatus === 'Pending' || s.approvalStatus === 0) && (
                          <>
                            <button
                              type="button"
                              className="btn btn-primary btn-sm"
                              style={{ fontWeight: 700, padding: '0.35rem 0.75rem' }}
                              onClick={() => handleApproveShop(s.id)}
                            >
                              Duyệt
                            </button>
                            <button
                              type="button"
                              className="btn btn-danger btn-sm"
                              style={{ fontWeight: 700, padding: '0.35rem 0.75rem' }}
                              onClick={() =>
                                setRejectShopModal({
                                  isOpen: true,
                                  shopId: s.id,
                                  reasonCode: 'INSUFFICIENT_INFO',
                                  reasonNote: '',
                                })
                              }
                            >
                              Từ chối
                            </button>
                          </>
                        )}

                        {(s.approvalStatus === 'Approved' || s.approvalStatus === 1) && (
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            style={{
                              background: '#fffbeb',
                              border: '1px solid #fde68a',
                              color: '#b45309',
                              fontWeight: 700,
                              padding: '0.35rem 0.75rem',
                            }}
                            onClick={() =>
                              setSanctionModal({
                                isOpen: true,
                                shopId: s.id,
                                type: 0,
                                reason: '',
                                days: 7,
                              })
                            }
                          >
                            Chế tài
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* 4. MODAL / DRAWER CHI TIẾT TOÀN DIỆN CỦA GIAN HÀNG */}
      {selectedShopDetail && (
        <div
          className="modal-backdrop"
          onClick={(e) => {
            if (e.target === e.currentTarget) setSelectedShopDetail(null);
          }}
        >
          <div
            className="admin-modal-card"
            style={{
              width: '100%',
              maxWidth: '920px',
              maxHeight: '92vh',
              overflowY: 'auto',
              borderRadius: '20px',
              padding: '2rem',
            }}
          >
            {/* Header Modal */}
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'flex-start',
                marginBottom: '1.5rem',
                borderBottom: '1px solid #e2e8f0',
                paddingBottom: '1.25rem',
                flexWrap: 'wrap',
                gap: '1rem',
              }}
            >
              <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                {selectedShopDetail.logoUrl ? (
                  <img
                    src={selectedShopDetail.logoUrl}
                    alt={selectedShopDetail.name}
                    style={{ width: '64px', height: '64px', borderRadius: '16px', objectFit: 'cover', border: '2px solid #e2e8f0' }}
                  />
                ) : (
                  <div
                    style={{
                      width: '64px',
                      height: '64px',
                      borderRadius: '16px',
                      background: 'linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: '1.5rem',
                      fontWeight: 800,
                      color: '#ffffff',
                    }}
                  >
                    {selectedShopDetail.name.slice(0, 1).toUpperCase()}
                  </div>
                )}
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.65rem', flexWrap: 'wrap' }}>
                    <h2 style={{ margin: 0, fontSize: '1.4rem', fontWeight: 800, color: '#0f172a' }}>
                      {selectedShopDetail.name}
                    </h2>
                    <span
                      className={`badge ${
                        selectedShopDetail.approvalStatus === 'Approved' || selectedShopDetail.approvalStatus === '1'
                          ? 'badge-success'
                          : selectedShopDetail.approvalStatus === 'Rejected' || selectedShopDetail.approvalStatus === '2'
                          ? 'badge-danger'
                          : 'badge-warning'
                      }`}
                      style={{ fontWeight: 700, padding: '4px 10px' }}
                    >
                      {selectedShopDetail.approvalStatus}
                    </span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginTop: '6px', fontSize: '0.88rem' }}>
                    <a
                      href={`/marketplace/shops/${selectedShopDetail.slug}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="admin-cell-link"
                    >
                      /{selectedShopDetail.slug}
                      <ExternalLinkIcon size={12} />
                    </a>
                    <span style={{ color: '#94a3b8' }}>•</span>
                    <span style={{ color: '#64748b', fontWeight: 500 }}>
                      Ngày lập: {new Date(selectedShopDetail.createdAtUtc).toLocaleDateString('vi-VN')}
                    </span>
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }}
                  onClick={() => setSelectedShopDetail(null)}
                >
                  Đóng
                </button>
              </div>
            </div>

            {/* Thông tin chủ sở hữu & ngân hàng */}
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
                gap: '1rem',
                marginBottom: '1.5rem',
              }}
            >
              {/* Box Chủ shop */}
              <div
                style={{
                  padding: '1rem 1.25rem',
                  background: '#f8fafc',
                  borderRadius: '14px',
                  border: '1px solid #e2e8f0',
                }}
              >
                <div style={{ fontSize: '0.8rem', color: '#475569', fontWeight: 700, textTransform: 'uppercase', marginBottom: '8px', letterSpacing: '0.04em' }}>
                  Chủ Gian Hàng
                </div>
                <div style={{ fontWeight: 700, color: '#0f172a', fontSize: '1.05rem' }}>
                  {selectedShopDetail.ownerDisplayName || 'Chưa cập nhật'}
                </div>
                <div style={{ fontSize: '0.88rem', color: '#334155', marginTop: '4px' }}>
                  Email: <strong>{selectedShopDetail.ownerEmail || selectedShopDetail.contactEmail}</strong>
                </div>
                {selectedShopDetail.ownerPhoneNumber && (
                  <div style={{ fontSize: '0.88rem', color: '#334155', marginTop: '2px' }}>
                    SĐT: <strong>{selectedShopDetail.ownerPhoneNumber}</strong>
                  </div>
                )}
                <div style={{ fontSize: '0.78rem', color: '#64748b', marginTop: '6px' }}>
                  User ID: {selectedShopDetail.ownerUserId}
                </div>
              </div>

              {/* Box Tài khoản ngân hàng */}
              <div
                style={{
                  padding: '1rem 1.25rem',
                  background: '#f8fafc',
                  borderRadius: '14px',
                  border: '1px solid #e2e8f0',
                }}
              >
                <div style={{ fontSize: '0.8rem', color: '#475569', fontWeight: 700, textTransform: 'uppercase', marginBottom: '8px', letterSpacing: '0.04em' }}>
                  Tài Khoản Rút Tiền
                </div>
                {selectedShopDetail.bankAccount ? (
                  <div>
                    <div style={{ fontWeight: 700, color: '#0284c7', fontSize: '1rem' }}>
                      {selectedShopDetail.bankAccount.bankName} ({selectedShopDetail.bankAccount.bankCode})
                    </div>
                    <div style={{ fontSize: '0.88rem', color: '#0f172a', marginTop: '4px' }}>
                      STK: <strong>{selectedShopDetail.bankAccount.maskedAccountNumber}</strong>
                    </div>
                    <div style={{ fontSize: '0.88rem', color: '#334155', marginTop: '2px' }}>
                      Chủ TK: <strong>{selectedShopDetail.bankAccount.accountHolderName}</strong>
                    </div>
                  </div>
                ) : (
                  <div style={{ color: '#64748b', fontSize: '0.88rem', fontStyle: 'italic', marginTop: '8px' }}>
                    Gian hàng chưa liên kết tài khoản ngân hàng thụ hưởng.
                  </div>
                )}
              </div>
            </div>

            {/* 4 Thống kê bán hàng nổi bật của Shop */}
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
                gap: '0.75rem',
                marginBottom: '1.75rem',
              }}
            >
              <div style={{ padding: '0.9rem 1rem', background: '#f0f9ff', borderRadius: '12px', border: '1px solid #bae6fd' }}>
                <div style={{ fontSize: '0.75rem', color: '#0369a1', fontWeight: 700 }}>TỔNG ĐƠN ĐÃ BÁN</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#0f172a', marginTop: '2px' }}>
                  {(selectedShopDetail.totalOrders || 0).toLocaleString()} đơn
                </div>
              </div>

              <div style={{ padding: '0.9rem 1rem', background: '#ecfdf5', borderRadius: '12px', border: '1px solid #a7f3d0' }}>
                <div style={{ fontSize: '0.75rem', color: '#047857', fontWeight: 700 }}>DOANH THU ĐÃ BÁN</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#047857', marginTop: '2px' }}>
                  {Number(selectedShopDetail.totalRevenue || 0).toLocaleString('vi-VN')} đ
                </div>
              </div>

              <div style={{ padding: '0.9rem 1rem', background: '#f5f3ff', borderRadius: '12px', border: '1px solid #ddd6fe' }}>
                <div style={{ fontSize: '0.75rem', color: '#6d28d9', fontWeight: 700 }}>SỐ DƯ KHẢ DỤNG</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#6d28d9', marginTop: '2px' }}>
                  {Number(selectedShopDetail.availableBalance || 0).toLocaleString('vi-VN')} đ
                </div>
              </div>

              <div style={{ padding: '0.9rem 1rem', background: '#fffbeb', borderRadius: '12px', border: '1px solid #fde68a' }}>
                <div style={{ fontSize: '0.75rem', color: '#b45309', fontWeight: 700 }}>TIỀN TẠM GIỮ (PENDING)</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#b45309', marginTop: '2px' }}>
                  {Number(selectedShopDetail.pendingBalance || 0).toLocaleString('vi-VN')} đ
                </div>
              </div>
            </div>

            {/* Mô tả shop */}
            {selectedShopDetail.description && (
              <div style={{ marginBottom: '1.5rem', padding: '0.85rem 1.15rem', background: '#f8fafc', borderRadius: '12px', border: '1px solid #e2e8f0' }}>
                <span style={{ fontSize: '0.82rem', color: '#475569', fontWeight: 700, textTransform: 'uppercase' }}>Mô tả kinh doanh:</span>
                <p style={{ margin: '4px 0 0', color: '#1e293b', fontSize: '0.92rem', lineHeight: 1.6 }}>
                  {selectedShopDetail.description}
                </p>
              </div>
            )}

            {/* Sub-Tabs: Đơn bán gần đây / Sản phẩm / Chế tài / Xét duyệt */}
            <div style={{ display: 'flex', gap: '0.5rem', borderBottom: '2px solid #e2e8f0', marginBottom: '1.25rem', overflowX: 'auto' }}>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'orders' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('orders')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none', fontWeight: 700 }}
              >
                Đơn Hàng Gần Đây ({selectedShopDetail.recentOrders?.length || 0})
              </button>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'products' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('products')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none', fontWeight: 700 }}
              >
                Sản Phẩm ({selectedShopDetail.recentProducts?.length || 0})
              </button>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'sanctions' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('sanctions')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none', fontWeight: 700 }}
              >
                Chế Tài & Xử Phạt ({selectedShopDetail.sanctions?.length || 0})
              </button>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'reviews' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('reviews')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none', fontWeight: 700 }}
              >
                Lịch Sử Duyệt ({selectedShopDetail.reviews?.length || 0})
              </button>
            </div>

            {/* Sub-Tab Content: Recent Orders */}
            {detailTab === 'orders' && (
              <div>
                {!selectedShopDetail.recentOrders || selectedShopDetail.recentOrders.length === 0 ? (
                  <p style={{ color: '#64748b', textAlign: 'center', padding: '2rem 0', fontWeight: 500 }}>
                    Gian hàng này chưa phát sinh đơn hàng nào.
                  </p>
                ) : (
                  <div style={{ overflowX: 'auto', border: '1px solid #e2e8f0', borderRadius: '12px' }}>
                    <table className="admin-table">
                      <thead>
                        <tr>
                          <th>Mã đơn</th>
                          <th>Người mua</th>
                          <th style={{ textAlign: 'right' }}>Tổng thanh toán</th>
                          <th style={{ textAlign: 'right' }}>Shop thực nhận</th>
                          <th style={{ textAlign: 'center' }}>Trạng thái</th>
                          <th style={{ textAlign: 'right' }}>Thời gian</th>
                        </tr>
                      </thead>
                      <tbody>
                        {selectedShopDetail.recentOrders.map((ord) => (
                          <tr key={ord.id}>
                            <td style={{ fontWeight: 700, color: '#0284c7' }}>
                              #{ord.orderNo}
                            </td>
                            <td style={{ color: '#0f172a', fontWeight: 600 }}>{ord.buyerName}</td>
                            <td style={{ textAlign: 'right', fontWeight: 700, color: '#0f172a' }}>
                              {Number(ord.total).toLocaleString('vi-VN')} đ
                            </td>
                            <td style={{ textAlign: 'right', color: '#059669', fontWeight: 700 }}>
                              {Number(ord.shopNet).toLocaleString('vi-VN')} đ
                            </td>
                            <td style={{ textAlign: 'center' }}>
                              <span
                                className={`badge ${
                                  ord.status === 'Completed'
                                    ? 'badge-success'
                                    : ord.status === 'Delivered'
                                    ? 'badge-primary'
                                    : 'badge-danger'
                                }`}
                                style={{ fontSize: '0.75rem', fontWeight: 700 }}
                              >
                                {ord.status}
                              </span>
                            </td>
                            <td style={{ textAlign: 'right', color: '#64748b', fontSize: '0.82rem' }}>
                              {new Date(ord.createdAtUtc).toLocaleString('vi-VN')}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}

            {/* Sub-Tab Content: Recent Products */}
            {detailTab === 'products' && (
              <div>
                {!selectedShopDetail.recentProducts || selectedShopDetail.recentProducts.length === 0 ? (
                  <p style={{ color: '#64748b', textAlign: 'center', padding: '2rem 0', fontWeight: 500 }}>
                    Gian hàng chưa đăng sản phẩm nào.
                  </p>
                ) : (
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '0.75rem' }}>
                    {selectedShopDetail.recentProducts.map((p) => (
                      <div
                        key={p.id}
                        style={{
                          padding: '0.85rem 1rem',
                          background: '#f8fafc',
                          borderRadius: '12px',
                          border: '1px solid #e2e8f0',
                        }}
                      >
                        <div style={{ fontWeight: 700, color: '#0f172a', fontSize: '0.92rem' }}>{p.name}</div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '6px' }}>
                          <span style={{ color: '#059669', fontWeight: 800, fontSize: '0.95rem' }}>
                            {Number(p.price).toLocaleString('vi-VN')} đ
                          </span>
                          <span className="badge badge-secondary" style={{ fontSize: '0.75rem', fontWeight: 600 }}>
                            {p.status}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Sub-Tab Content: Sanctions */}
            {detailTab === 'sanctions' && (
              <div>
                {!selectedShopDetail.sanctions || selectedShopDetail.sanctions.length === 0 ? (
                  <p style={{ color: '#64748b', textAlign: 'center', padding: '2rem 0', fontWeight: 500 }}>
                    Gian hàng này chưa từng bị áp dụng chế tài xử phạt nào.
                  </p>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                    {selectedShopDetail.sanctions.map((sc) => (
                      <div
                        key={sc.id}
                        style={{
                          padding: '0.95rem 1.25rem',
                          background: '#fff1f2',
                          borderRadius: '12px',
                          border: '1px solid #fecdd3',
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          flexWrap: 'wrap',
                          gap: '0.75rem',
                        }}
                      >
                        <div>
                          <div style={{ fontWeight: 800, color: '#be123c' }}>{sc.type}</div>
                          <div style={{ color: '#1e293b', marginTop: '2px', fontSize: '0.92rem', fontWeight: 500 }}>{sc.reason}</div>
                          <div style={{ fontSize: '0.8rem', color: '#64748b', marginTop: '4px' }}>
                            Hiệu lực từ {new Date(sc.startAtUtc).toLocaleDateString('vi-VN')}
                            {sc.endAtUtc ? ` đến ${new Date(sc.endAtUtc).toLocaleDateString('vi-VN')}` : ' (Vĩnh viễn)'}
                            {sc.liftedAtUtc && <span style={{ color: '#059669', fontWeight: 700, marginLeft: '8px' }}>[Đã dỡ phạt]</span>}
                          </div>
                        </div>
                        {!sc.liftedAtUtc && (
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }}
                            onClick={() => handleLiftSanction(selectedShopDetail.id, sc.id)}
                          >
                            Gỡ phạt
                          </button>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Sub-Tab Content: Reviews */}
            {detailTab === 'reviews' && (
              <div>
                {!selectedShopDetail.reviews || selectedShopDetail.reviews.length === 0 ? (
                  <p style={{ color: '#64748b', textAlign: 'center', padding: '2rem 0', fontWeight: 500 }}>
                    Chưa có lịch sử phê duyệt trước đây.
                  </p>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                    {selectedShopDetail.reviews.map((rev) => (
                      <div
                        key={rev.id}
                        style={{
                          padding: '0.85rem 1.15rem',
                          background: '#f8fafc',
                          borderRadius: '12px',
                          border: '1px solid #e2e8f0',
                        }}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <span style={{ fontWeight: 700, color: '#0f172a' }}>Lần nộp #{rev.submissionNo}</span>
                          <span
                            className={`badge ${
                              rev.decision === 'Approved'
                                ? 'badge-success'
                                : rev.decision === 'Rejected'
                                ? 'badge-danger'
                                : 'badge-warning'
                            }`}
                            style={{ fontWeight: 700 }}
                          >
                            {rev.decision}
                          </span>
                        </div>
                        {rev.reasonNote && (
                          <p style={{ margin: '6px 0 0', fontSize: '0.88rem', color: '#dc2626', fontWeight: 500 }}>
                            Lý do: {rev.reasonNote}
                          </p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* Action Buttons in Modal Bottom */}
            <div
              style={{
                display: 'flex',
                justifyContent: 'flex-end',
                gap: '0.75rem',
                marginTop: '2rem',
                borderTop: '1px solid #e2e8f0',
                paddingTop: '1.25rem',
              }}
            >
              {(selectedShopDetail.approvalStatus === 'Pending' || selectedShopDetail.approvalStatus === '0') && (
                <>
                  <button
                    type="button"
                    className="btn btn-primary"
                    style={{ fontWeight: 700 }}
                    onClick={() => handleApproveShop(selectedShopDetail.id)}
                  >
                    Duyệt Mở Gian Hàng
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
                    style={{ fontWeight: 700 }}
                    onClick={() =>
                      setRejectShopModal({
                        isOpen: true,
                        shopId: selectedShopDetail.id,
                        reasonCode: 'INSUFFICIENT_INFO',
                        reasonNote: '',
                      })
                    }
                  >
                    Từ Chối
                  </button>
                </>
              )}

              {(selectedShopDetail.approvalStatus === 'Approved' || selectedShopDetail.approvalStatus === '1') && (
                <button
                  type="button"
                  className="btn btn-secondary"
                  style={{
                    background: '#fffbeb',
                    border: '1px solid #fde68a',
                    color: '#b45309',
                    fontWeight: 700,
                  }}
                  onClick={() =>
                    setSanctionModal({
                      isOpen: true,
                      shopId: selectedShopDetail.id,
                      type: 0,
                      reason: '',
                      days: 7,
                    })
                  }
                >
                  Ban Hành Chế Tài
                </button>
              )}

              <button
                type="button"
                className="btn btn-secondary"
                style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }}
                onClick={() => setSelectedShopDetail(null)}
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Từ chối Shop */}
      {rejectShopModal && (
        <div className="modal-backdrop">
          <div
            className="admin-modal-card"
            style={{
              width: '90%',
              maxWidth: '480px',
              padding: '2rem',
              borderRadius: '16px',
            }}
          >
            <h3 style={{ margin: '0 0 1rem', fontWeight: 800, color: '#0f172a' }}>Từ chối phê duyệt gian hàng</h3>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>
                Mã lý do từ chối
              </label>
              <select
                className="form-control"
                value={rejectShopModal.reasonCode}
                onChange={(e) => setRejectShopModal({ ...rejectShopModal, reasonCode: e.target.value })}
                style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a' }}
              >
                <option value="INSUFFICIENT_INFO">Thông tin cung cấp chưa đầy đủ</option>
                <option value="INVALID_BRAND">Thương hiệu không chính chủ hoặc vi phạm bản quyền</option>
                <option value="PROHIBITED_GOODS">Kinh doanh mặt hàng nằm trong danh mục cấm</option>
                <option value="FRAUD_RISK">Phát hiện dấu hiệu rủi ro gian lận</option>
              </select>
            </div>
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>
                Ghi chú giải trình chi tiết
              </label>
              <textarea
                className="form-control"
                rows={3}
                placeholder="Nhập hướng dẫn để người bán bổ sung nộp lại hồ sơ..."
                value={rejectShopModal.reasonNote}
                onChange={(e) => setRejectShopModal({ ...rejectShopModal, reasonNote: e.target.value })}
                style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a' }}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button
                type="button"
                className="btn btn-secondary"
                style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }}
                onClick={() => setRejectShopModal(null)}
              >
                Hủy
              </button>
              <button type="button" className="btn btn-danger" style={{ fontWeight: 700 }} onClick={handleConfirmRejectShop}>
                Xác nhận Từ chối
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Chế tài Sanction */}
      {sanctionModal && (
        <div className="modal-backdrop">
          <div
            className="admin-modal-card"
            style={{
              width: '90%',
              maxWidth: '480px',
              padding: '2rem',
              borderRadius: '16px',
            }}
          >
            <h3 style={{ margin: '0 0 1rem', fontWeight: 800, color: '#dc2626' }}>Ban hành Chế tài Xử phạt</h3>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>
                Loại hình chế tài
              </label>
              <select
                className="form-control"
                value={sanctionModal.type}
                onChange={(e) => setSanctionModal({ ...sanctionModal, type: Number(e.target.value) })}
                style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a' }}
              >
                <option value={2}>Cảnh cáo chính thức (Warning)</option>
                <option value={0}>Tạm đình chỉ hoạt động có thời hạn (TemporarySuspension)</option>
                <option value={1}>Cấm hoạt động vĩnh viễn (PermanentBan)</option>
              </select>
            </div>
            {sanctionModal.type === 0 && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>
                  Thời hạn phạt (ngày)
                </label>
                <input
                  type="number"
                  className="form-control"
                  value={sanctionModal.days || 7}
                  onChange={(e) => setSanctionModal({ ...sanctionModal, days: Number(e.target.value) })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a' }}
                />
              </div>
            )}
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>
                Căn cứ & Lý do xử phạt
              </label>
              <textarea
                className="form-control"
                rows={3}
                placeholder="Nêu rõ hành vi vi phạm hoặc mã khiếu nại liên quan..."
                value={sanctionModal.reason}
                onChange={(e) => setSanctionModal({ ...sanctionModal, reason: e.target.value })}
                style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a' }}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button
                type="button"
                className="btn btn-secondary"
                style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }}
                onClick={() => setSanctionModal(null)}
              >
                Hủy
              </button>
              <button type="button" className="btn btn-danger" style={{ fontWeight: 700 }} onClick={handleConfirmSanction}>
                Ban hành quyết định
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
