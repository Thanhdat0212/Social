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
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
          gap: '1rem',
          marginBottom: '1.75rem',
        }}
      >
        <div
          className="card-glass"
          style={{
            padding: '1.25rem 1.5rem',
            borderRadius: '16px',
            border: '1px solid rgba(14, 165, 233, 0.25)',
            background: 'linear-gradient(135deg, rgba(14, 165, 233, 0.08) 0%, rgba(15, 23, 42, 0.6) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Tổng Gian Hàng</span>
            <span style={{ color: '#38bdf8', background: 'rgba(56, 189, 248, 0.15)', padding: '6px', borderRadius: '8px' }}>
              <StoreIcon size={18} />
            </span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, color: '#ffffff' }}>
            {stats ? stats.totalShops : shops.length}
          </div>
          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
            Đang hoạt động: <strong style={{ color: '#34d399' }}>{stats?.approvedShops ?? 0}</strong> | Chờ duyệt: <strong style={{ color: '#fbbf24' }}>{stats?.pendingShops ?? 0}</strong>
          </div>
        </div>

        <div
          className="card-glass"
          style={{
            padding: '1.25rem 1.5rem',
            borderRadius: '16px',
            border: '1px solid rgba(52, 211, 153, 0.25)',
            background: 'linear-gradient(135deg, rgba(52, 211, 153, 0.08) 0%, rgba(15, 23, 42, 0.6) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Tổng Số Đơn Đã Bán Ra</span>
            <span style={{ color: '#34d399', background: 'rgba(52, 211, 153, 0.15)', padding: '6px', borderRadius: '8px' }}>
              <CartIcon size={18} />
            </span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, color: '#34d399' }}>
            {stats ? stats.totalOrdersSold.toLocaleString('vi-VN') : 0} <span style={{ fontSize: '1rem', fontWeight: 500, color: 'var(--text-muted)' }}>đơn</span>
          </div>
          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
            Đơn hàng số hoàn tất giao dịch
          </div>
        </div>

        <div
          className="card-glass"
          style={{
            padding: '1.25rem 1.5rem',
            borderRadius: '16px',
            border: '1px solid rgba(168, 85, 247, 0.25)',
            background: 'linear-gradient(135deg, rgba(168, 85, 247, 0.08) 0%, rgba(15, 23, 42, 0.6) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Tổng Doanh Số Toàn Sàn</span>
            <span style={{ color: '#c084fc', background: 'rgba(192, 132, 252, 0.15)', padding: '6px', borderRadius: '8px' }}>
              <PackageIcon size={18} />
            </span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, color: '#c084fc' }}>
            {stats ? Number(stats.totalRevenue).toLocaleString('vi-VN') : 0} <span style={{ fontSize: '0.9rem', color: 'var(--text-muted)' }}>đ</span>
          </div>
          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
            Tổng giá trị GMV thanh toán qua sàn
          </div>
        </div>

        <div
          className="card-glass"
          style={{
            padding: '1.25rem 1.5rem',
            borderRadius: '16px',
            border: '1px solid rgba(251, 191, 36, 0.25)',
            background: 'linear-gradient(135deg, rgba(251, 191, 36, 0.08) 0%, rgba(15, 23, 42, 0.6) 100%)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
            <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)', fontWeight: 600 }}>Chờ Admin Duyệt</span>
            <span style={{ color: '#fbbf24', background: 'rgba(251, 191, 36, 0.15)', padding: '6px', borderRadius: '8px' }}>
              <AlertTriangleIcon size={18} />
            </span>
          </div>
          <div style={{ fontSize: '1.75rem', fontWeight: 800, color: '#fbbf24' }}>
            {stats ? stats.pendingShops : 0} <span style={{ fontSize: '1rem', fontWeight: 500, color: 'var(--text-muted)' }}>shop</span>
          </div>
          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
            {stats && stats.pendingShops > 0 ? 'Cần xem xét phê duyệt' : 'Đã duyệt hết hồ sơ'}
          </div>
        </div>
      </div>

      {/* 2. FILTER TABS & SEARCH BAR */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.25rem',
          flexWrap: 'wrap',
          gap: '1rem',
        }}
      >
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
              className={`btn btn-sm ${shopFilter === tab.key ? 'btn-primary' : 'btn-secondary'}`}
              style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', padding: '0.45rem 0.9rem' }}
              onClick={() => setShopFilter(tab.key)}
            >
              <span>{tab.label}</span>
              {typeof tab.count === 'number' && (
                <span
                  style={{
                    fontSize: '0.75rem',
                    padding: '1px 6px',
                    borderRadius: '999px',
                    background: shopFilter === tab.key ? 'rgba(255,255,255,0.25)' : 'rgba(255,255,255,0.1)',
                  }}
                >
                  {tab.count}
                </span>
              )}
            </button>
          ))}
        </div>

        {/* Search Input */}
        <form onSubmit={handleSearchSubmit} style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
          <div style={{ position: 'relative' }}>
            <span style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }}>
              <SearchIcon size={14} />
            </span>
            <input
              type="text"
              className="form-control"
              placeholder="Tìm theo tên shop, @slug..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{ paddingLeft: '32px', height: '36px', fontSize: '0.85rem', width: '220px' }}
            />
          </div>
          <button type="submit" className="btn btn-secondary btn-sm" style={{ height: '36px' }}>
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
            style={{ height: '36px' }}
          >
            <RefreshIcon size={14} />
          </button>
        </form>
      </div>

      {/* 3. SHOPS DATA TABLE */}
      <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.875rem' }}>
            <thead>
              <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)' }}>
                <th style={{ padding: '1rem 1.25rem' }}>Gian hàng</th>
                <th style={{ padding: '1rem' }}>Chủ sở hữu & Liên hệ</th>
                <th style={{ padding: '1rem', textAlign: 'center' }}>Đơn đã bán</th>
                <th style={{ padding: '1rem', textAlign: 'center' }}>Sản phẩm</th>
                <th style={{ padding: '1rem', textAlign: 'right' }}>Doanh số & Số dư</th>
                <th style={{ padding: '1rem' }}>Trạng thái</th>
                <th style={{ padding: '1rem', textAlign: 'right' }}>Thao tác</th>
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
                  <tr key={s.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', transition: 'background 0.2s' }}>
                    {/* Gian hàng Logo & Tên */}
                    <td style={{ padding: '1rem 1.25rem' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                        {s.logoUrl ? (
                          <img
                            src={s.logoUrl}
                            alt={s.name}
                            style={{ width: '40px', height: '40px', borderRadius: '10px', objectFit: 'cover', border: '1px solid var(--border-color)' }}
                          />
                        ) : (
                          <div
                            style={{
                              width: '40px',
                              height: '40px',
                              borderRadius: '10px',
                              background: 'linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)',
                              display: 'flex',
                              alignItems: 'center',
                              justifyContent: 'center',
                              fontWeight: 700,
                              color: '#fff',
                              fontSize: '1rem',
                            }}
                          >
                            {s.name.slice(0, 1).toUpperCase()}
                          </div>
                        )}
                        <div>
                          <div style={{ fontWeight: 700, color: '#ffffff', fontSize: '0.95rem' }}>{s.name}</div>
                          <a
                            href={`/marketplace/shops/${s.slug}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            style={{
                              fontSize: '0.78rem',
                              color: '#38bdf8',
                              textDecoration: 'none',
                              display: 'inline-flex',
                              alignItems: 'center',
                              gap: '3px',
                            }}
                          >
                            @{s.slug}
                            <ExternalLinkIcon size={12} />
                          </a>
                        </div>
                      </div>
                    </td>

                    {/* Chủ sở hữu */}
                    <td style={{ padding: '1rem' }}>
                      <div style={{ color: '#e2e8f0', fontWeight: 600 }}>{s.ownerDisplayName || 'Chủ gian hàng'}</div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{s.ownerEmail || s.contactEmail}</div>
                    </td>

                    {/* Tổng số đơn đã bán ra */}
                    <td style={{ padding: '1rem', textAlign: 'center' }}>
                      <div
                        style={{
                          display: 'inline-block',
                          padding: '4px 10px',
                          borderRadius: '8px',
                          background: (s.totalOrders || 0) > 0 ? 'rgba(56, 189, 248, 0.15)' : 'rgba(255,255,255,0.05)',
                          color: (s.totalOrders || 0) > 0 ? '#38bdf8' : 'var(--text-muted)',
                          fontWeight: 700,
                        }}
                      >
                        {(s.totalOrders || 0).toLocaleString()} đơn
                      </div>
                    </td>

                    {/* Số lượng sản phẩm */}
                    <td style={{ padding: '1rem', textAlign: 'center', color: '#cbd5e1' }}>
                      {(s.totalProducts || 0).toLocaleString()} SP
                    </td>

                    {/* Doanh số & Số dư */}
                    <td style={{ padding: '1rem', textAlign: 'right' }}>
                      <div style={{ fontWeight: 700, color: '#34d399' }}>
                        {Number(s.totalRevenue || 0).toLocaleString('vi-VN')} đ
                      </div>
                      <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>
                        Khả dụng: {Number(s.availableBalance || 0).toLocaleString('vi-VN')} đ
                      </div>
                    </td>

                    {/* Trạng thái */}
                    <td style={{ padding: '1rem' }}>
                      <span
                        className={`badge ${
                          s.approvalStatus === 'Approved' || s.approvalStatus === 1
                            ? 'badge-success'
                            : s.approvalStatus === 'Rejected' || s.approvalStatus === 2
                            ? 'badge-danger'
                            : 'badge-warning'
                        }`}
                        style={{ fontSize: '0.75rem', padding: '3px 8px' }}
                      >
                        {s.approvalStatus === 'Approved' || s.approvalStatus === 1
                          ? 'Đã duyệt'
                          : s.approvalStatus === 'Rejected' || s.approvalStatus === 2
                          ? 'Từ chối'
                          : 'Chờ duyệt'}
                      </span>
                    </td>

                    {/* Thao tác */}
                    <td style={{ padding: '1rem', textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.4rem' }}>
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', padding: '0.35rem 0.65rem' }}
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
                              onClick={() => handleApproveShop(s.id)}
                            >
                              Duyệt
                            </button>
                            <button
                              type="button"
                              className="btn btn-danger btn-sm"
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
                            className="btn btn-secondary btn-sm text-warning"
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
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0, 0, 0, 0.75)',
            backdropFilter: 'blur(5px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1050,
            padding: '1rem',
          }}
          onClick={(e) => {
            if (e.target === e.currentTarget) setSelectedShopDetail(null);
          }}
        >
          <div
            className="card-glass"
            style={{
              width: '100%',
              maxWidth: '900px',
              maxHeight: '92vh',
              overflowY: 'auto',
              borderRadius: '20px',
              border: '1px solid rgba(168, 85, 247, 0.4)',
              background: 'linear-gradient(135deg, #131127 0%, #0d1222 100%)',
              padding: '2rem',
              boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.65)',
            }}
          >
            {/* Header Modal */}
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'flex-start',
                marginBottom: '1.5rem',
                borderBottom: '1px solid rgba(255,255,255,0.08)',
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
                    style={{ width: '64px', height: '64px', borderRadius: '16px', objectFit: 'cover', border: '2px solid rgba(168, 85, 247, 0.5)' }}
                  />
                ) : (
                  <div
                    style={{
                      width: '64px',
                      height: '64px',
                      borderRadius: '16px',
                      background: 'linear-gradient(135deg, #6366f1 0%, #a855f7 100%)',
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
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <h2 style={{ margin: 0, fontSize: '1.4rem', fontWeight: 800, color: '#fff' }}>
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
                    >
                      {selectedShopDetail.approvalStatus}
                    </span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginTop: '4px', fontSize: '0.85rem' }}>
                    <a
                      href={`/marketplace/shops/${selectedShopDetail.slug}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      style={{ color: '#38bdf8', textDecoration: 'none', display: 'inline-flex', alignItems: 'center', gap: '4px' }}
                    >
                      /{selectedShopDetail.slug}
                      <ExternalLinkIcon size={12} />
                    </a>
                    <span style={{ color: 'var(--text-muted)' }}>•</span>
                    <span style={{ color: 'var(--text-muted)' }}>
                      Ngày lập: {new Date(selectedShopDetail.createdAtUtc).toLocaleDateString('vi-VN')}
                    </span>
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
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
                  background: 'rgba(255, 255, 255, 0.03)',
                  borderRadius: '12px',
                  border: '1px solid rgba(255,255,255,0.06)',
                }}
              >
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: '8px' }}>
                  Chủ Gian Hàng
                </div>
                <div style={{ fontWeight: 700, color: '#f8fafc', fontSize: '1rem' }}>
                  {selectedShopDetail.ownerDisplayName || 'Chưa cập nhật'}
                </div>
                <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                  Email: {selectedShopDetail.ownerEmail || selectedShopDetail.contactEmail}
                </div>
                {selectedShopDetail.ownerPhoneNumber && (
                  <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                    SĐT: {selectedShopDetail.ownerPhoneNumber}
                  </div>
                )}
                <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                  User ID: {selectedShopDetail.ownerUserId}
                </div>
              </div>

              {/* Box Tài khoản ngân hàng */}
              <div
                style={{
                  padding: '1rem 1.25rem',
                  background: 'rgba(255, 255, 255, 0.03)',
                  borderRadius: '12px',
                  border: '1px solid rgba(255,255,255,0.06)',
                }}
              >
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', marginBottom: '8px' }}>
                  Tài Khoản Rút Tiền
                </div>
                {selectedShopDetail.bankAccount ? (
                  <div>
                    <div style={{ fontWeight: 700, color: '#38bdf8', fontSize: '0.95rem' }}>
                      {selectedShopDetail.bankAccount.bankName} ({selectedShopDetail.bankAccount.bankCode})
                    </div>
                    <div style={{ fontSize: '0.85rem', color: '#f8fafc', marginTop: '2px' }}>
                      STK: <strong>{selectedShopDetail.bankAccount.maskedAccountNumber}</strong>
                    </div>
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                      Chủ TK: {selectedShopDetail.bankAccount.accountHolderName}
                    </div>
                  </div>
                ) : (
                  <div style={{ color: 'var(--text-muted)', fontSize: '0.85rem', fontStyle: 'italic' }}>
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
              <div style={{ padding: '0.9rem 1rem', background: 'rgba(56, 189, 248, 0.08)', borderRadius: '12px', border: '1px solid rgba(56, 189, 248, 0.2)' }}>
                <div style={{ fontSize: '0.75rem', color: '#38bdf8', fontWeight: 600 }}>TỔNG ĐƠN ĐÃ BÁN</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#ffffff', marginTop: '2px' }}>
                  {(selectedShopDetail.totalOrders || 0).toLocaleString()} đơn
                </div>
              </div>

              <div style={{ padding: '0.9rem 1rem', background: 'rgba(52, 211, 153, 0.08)', borderRadius: '12px', border: '1px solid rgba(52, 211, 153, 0.2)' }}>
                <div style={{ fontSize: '0.75rem', color: '#34d399', fontWeight: 600 }}>DOANH THU ĐÃ BÁN</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#34d399', marginTop: '2px' }}>
                  {Number(selectedShopDetail.totalRevenue || 0).toLocaleString('vi-VN')} đ
                </div>
              </div>

              <div style={{ padding: '0.9rem 1rem', background: 'rgba(168, 85, 247, 0.08)', borderRadius: '12px', border: '1px solid rgba(168, 85, 247, 0.2)' }}>
                <div style={{ fontSize: '0.75rem', color: '#c084fc', fontWeight: 600 }}>SỐ DƯ KHẢ DỤNG</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#c084fc', marginTop: '2px' }}>
                  {Number(selectedShopDetail.availableBalance || 0).toLocaleString('vi-VN')} đ
                </div>
              </div>

              <div style={{ padding: '0.9rem 1rem', background: 'rgba(251, 191, 36, 0.08)', borderRadius: '12px', border: '1px solid rgba(251, 191, 36, 0.2)' }}>
                <div style={{ fontSize: '0.75rem', color: '#fbbf24', fontWeight: 600 }}>TIỀN TẠM GIỮ (PENDING)</div>
                <div style={{ fontSize: '1.4rem', fontWeight: 800, color: '#fbbf24', marginTop: '2px' }}>
                  {Number(selectedShopDetail.pendingBalance || 0).toLocaleString('vi-VN')} đ
                </div>
              </div>
            </div>

            {/* Mô tả shop */}
            {selectedShopDetail.description && (
              <div style={{ marginBottom: '1.5rem', padding: '0.75rem 1rem', background: 'rgba(255,255,255,0.02)', borderRadius: '10px' }}>
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Mô tả kinh doanh:</span>
                <p style={{ margin: '4px 0 0', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                  {selectedShopDetail.description}
                </p>
              </div>
            )}

            {/* Sub-Tabs: Đơn bán gần đây / Sản phẩm / Chế tài / Xét duyệt */}
            <div style={{ display: 'flex', gap: '0.5rem', borderBottom: '1px solid rgba(255,255,255,0.1)', marginBottom: '1rem' }}>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'orders' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('orders')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none' }}
              >
                Đơn Hàng Gần Đây ({selectedShopDetail.recentOrders?.length || 0})
              </button>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'products' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('products')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none' }}
              >
                Sản Phẩm ({selectedShopDetail.recentProducts?.length || 0})
              </button>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'sanctions' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('sanctions')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none' }}
              >
                Chế Tài & Xử Phạt ({selectedShopDetail.sanctions?.length || 0})
              </button>
              <button
                type="button"
                className={`btn btn-sm ${detailTab === 'reviews' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => setDetailTab('reviews')}
                style={{ borderRadius: '8px 8px 0 0', borderBottom: 'none' }}
              >
                Lịch Sử Duyệt ({selectedShopDetail.reviews?.length || 0})
              </button>
            </div>

            {/* Sub-Tab Content: Recent Orders */}
            {detailTab === 'orders' && (
              <div>
                {!selectedShopDetail.recentOrders || selectedShopDetail.recentOrders.length === 0 ? (
                  <p style={{ color: 'var(--text-muted)', textAlign: 'center', padding: '1.5rem 0' }}>
                    Gian hàng này chưa phát sinh đơn hàng nào.
                  </p>
                ) : (
                  <div style={{ overflowX: 'auto' }}>
                    <table style={{ width: '100%', fontSize: '0.85rem', borderCollapse: 'collapse' }}>
                      <thead>
                        <tr style={{ background: 'rgba(255,255,255,0.03)', color: 'var(--text-muted)' }}>
                          <th style={{ padding: '8px 12px' }}>Mã đơn</th>
                          <th style={{ padding: '8px 12px' }}>Người mua</th>
                          <th style={{ padding: '8px 12px', textAlign: 'right' }}>Tổng thanh toán</th>
                          <th style={{ padding: '8px 12px', textAlign: 'right' }}>Shop thực nhận</th>
                          <th style={{ padding: '8px 12px', textAlign: 'center' }}>Trạng thái</th>
                          <th style={{ padding: '8px 12px', textAlign: 'right' }}>Thời gian</th>
                        </tr>
                      </thead>
                      <tbody>
                        {selectedShopDetail.recentOrders.map((ord) => (
                          <tr key={ord.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.04)' }}>
                            <td style={{ padding: '8px 12px', fontWeight: 700, color: '#38bdf8' }}>
                              #{ord.orderNo}
                            </td>
                            <td style={{ padding: '8px 12px', color: '#e2e8f0' }}>{ord.buyerName}</td>
                            <td style={{ padding: '8px 12px', textAlign: 'right', fontWeight: 600 }}>
                              {Number(ord.total).toLocaleString('vi-VN')} đ
                            </td>
                            <td style={{ padding: '8px 12px', textAlign: 'right', color: '#34d399', fontWeight: 600 }}>
                              {Number(ord.shopNet).toLocaleString('vi-VN')} đ
                            </td>
                            <td style={{ padding: '8px 12px', textAlign: 'center' }}>
                              <span
                                className={`badge ${
                                  ord.status === 'Completed'
                                    ? 'badge-success'
                                    : ord.status === 'Delivered'
                                    ? 'badge-primary'
                                    : 'badge-danger'
                                }`}
                                style={{ fontSize: '0.72rem' }}
                              >
                                {ord.status}
                              </span>
                            </td>
                            <td style={{ padding: '8px 12px', textAlign: 'right', color: 'var(--text-muted)' }}>
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
                  <p style={{ color: 'var(--text-muted)', textAlign: 'center', padding: '1.5rem 0' }}>
                    Gian hàng chưa đăng sản phẩm nào.
                  </p>
                ) : (
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '0.75rem' }}>
                    {selectedShopDetail.recentProducts.map((p) => (
                      <div
                        key={p.id}
                        style={{
                          padding: '0.75rem 1rem',
                          background: 'rgba(255,255,255,0.03)',
                          borderRadius: '10px',
                          border: '1px solid rgba(255,255,255,0.06)',
                        }}
                      >
                        <div style={{ fontWeight: 600, color: '#fff', fontSize: '0.9rem' }}>{p.name}</div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '6px' }}>
                          <span style={{ color: '#34d399', fontWeight: 700, fontSize: '0.95rem' }}>
                            {Number(p.price).toLocaleString('vi-VN')} đ
                          </span>
                          <span className="badge badge-secondary" style={{ fontSize: '0.72rem' }}>
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
                  <p style={{ color: 'var(--text-muted)', textAlign: 'center', padding: '1.5rem 0' }}>
                    Gian hàng này chưa từng bị áp dụng chế tài xử phạt nào.
                  </p>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                    {selectedShopDetail.sanctions.map((sc) => (
                      <div
                        key={sc.id}
                        style={{
                          padding: '0.9rem 1.25rem',
                          background: 'rgba(244, 63, 94, 0.08)',
                          borderRadius: '12px',
                          border: '1px solid rgba(244, 63, 94, 0.25)',
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          flexWrap: 'wrap',
                          gap: '0.75rem',
                        }}
                      >
                        <div>
                          <div style={{ fontWeight: 700, color: 'var(--accent-rose)' }}>{sc.type}</div>
                          <div style={{ color: '#f1f5f9', marginTop: '2px', fontSize: '0.9rem' }}>{sc.reason}</div>
                          <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                            Hiệu lực từ {new Date(sc.startAtUtc).toLocaleDateString('vi-VN')}
                            {sc.endAtUtc ? ` đến ${new Date(sc.endAtUtc).toLocaleDateString('vi-VN')}` : ' (Vĩnh viễn)'}
                            {sc.liftedAtUtc && <span style={{ color: '#34d399', marginLeft: '8px' }}>[Đã dỡ phạt]</span>}
                          </div>
                        </div>
                        {!sc.liftedAtUtc && (
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
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
                  <p style={{ color: 'var(--text-muted)', textAlign: 'center', padding: '1.5rem 0' }}>
                    Chưa có lịch sử phê duyệt trước đây.
                  </p>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                    {selectedShopDetail.reviews.map((rev) => (
                      <div
                        key={rev.id}
                        style={{
                          padding: '0.75rem 1rem',
                          background: 'rgba(255,255,255,0.03)',
                          borderRadius: '10px',
                          border: '1px solid rgba(255,255,255,0.06)',
                        }}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <span style={{ fontWeight: 600 }}>Lần nộp #{rev.submissionNo}</span>
                          <span
                            className={`badge ${
                              rev.decision === 'Approved'
                                ? 'badge-success'
                                : rev.decision === 'Rejected'
                                ? 'badge-danger'
                                : 'badge-warning'
                            }`}
                          >
                            {rev.decision}
                          </span>
                        </div>
                        {rev.reasonNote && (
                          <p style={{ margin: '6px 0 0', fontSize: '0.85rem', color: '#f87171' }}>
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
                borderTop: '1px solid rgba(255,255,255,0.08)',
                paddingTop: '1.25rem',
              }}
            >
              {(selectedShopDetail.approvalStatus === 'Pending' || selectedShopDetail.approvalStatus === '0') && (
                <>
                  <button
                    type="button"
                    className="btn btn-primary"
                    onClick={() => handleApproveShop(selectedShopDetail.id)}
                  >
                    Duyệt Mở Gian Hàng
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
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
                  className="btn btn-secondary text-warning"
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
        <div
          className="modal-backdrop"
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.7)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1100,
          }}
        >
          <div
            className="card-glass"
            style={{
              width: '90%',
              maxWidth: '480px',
              padding: '2rem',
              borderRadius: '16px',
              border: '1px solid var(--border-color)',
            }}
          >
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700 }}>Từ chối phê duyệt gian hàng</h3>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                Mã lý do từ chối
              </label>
              <select
                className="form-control"
                value={rejectShopModal.reasonCode}
                onChange={(e) => setRejectShopModal({ ...rejectShopModal, reasonCode: e.target.value })}
              >
                <option value="INSUFFICIENT_INFO">Thông tin cung cấp chưa đầy đủ</option>
                <option value="INVALID_BRAND">Thương hiệu không chính chủ hoặc vi phạm bản quyền</option>
                <option value="PROHIBITED_GOODS">Kinh doanh mặt hàng nằm trong danh mục cấm</option>
                <option value="FRAUD_RISK">Phát hiện dấu hiệu rủi ro gian lận</option>
              </select>
            </div>
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                Ghi chú giải trình chi tiết
              </label>
              <textarea
                className="form-control"
                rows={3}
                placeholder="Nhập hướng dẫn để người bán bổ sung nộp lại hồ sơ..."
                value={rejectShopModal.reasonNote}
                onChange={(e) => setRejectShopModal({ ...rejectShopModal, reasonNote: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setRejectShopModal(null)}>
                Hủy
              </button>
              <button type="button" className="btn btn-danger" onClick={handleConfirmRejectShop}>
                Xác nhận Từ chối
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal Chế tài Sanction */}
      {sanctionModal && (
        <div
          className="modal-backdrop"
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.7)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1100,
          }}
        >
          <div
            className="card-glass"
            style={{
              width: '90%',
              maxWidth: '480px',
              padding: '2rem',
              borderRadius: '16px',
              border: '1px solid var(--border-color)',
            }}
          >
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700, color: 'var(--accent-rose)' }}>Ban hành Chế tài Xử phạt</h3>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                Loại hình chế tài
              </label>
              <select
                className="form-control"
                value={sanctionModal.type}
                onChange={(e) => setSanctionModal({ ...sanctionModal, type: Number(e.target.value) })}
              >
                <option value={2}>Cảnh cáo chính thức (Warning)</option>
                <option value={0}>Tạm đình chỉ hoạt động có thời hạn (TemporarySuspension)</option>
                <option value={1}>Cấm hoạt động vĩnh viễn (PermanentBan)</option>
              </select>
            </div>
            {sanctionModal.type === 0 && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Thời hạn phạt (ngày)
                </label>
                <input
                  type="number"
                  className="form-control"
                  value={sanctionModal.days || 7}
                  onChange={(e) => setSanctionModal({ ...sanctionModal, days: Number(e.target.value) })}
                />
              </div>
            )}
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                Căn cứ & Lý do xử phạt
              </label>
              <textarea
                className="form-control"
                rows={3}
                placeholder="Nêu rõ hành vi vi phạm hoặc mã khiếu nại liên quan..."
                value={sanctionModal.reason}
                onChange={(e) => setSanctionModal({ ...sanctionModal, reason: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setSanctionModal(null)}>
                Hủy
              </button>
              <button type="button" className="btn btn-danger" onClick={handleConfirmSanction}>
                Ban hành quyết định
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
