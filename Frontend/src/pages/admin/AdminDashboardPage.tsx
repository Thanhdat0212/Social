import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { adminApi } from '@/api/adminApi';
import type {
  AdminShopItem,
  AdminShopDetail,
  AdminPayoutItem,
  AdminComplaintItem,
  AdminReportItem,
  ReconciliationSummary,
  AdminUserItem,
} from '@/types/admin';
import {
  ShieldIcon,
  ScaleIcon,
  StoreIcon,
  CheckCircleIcon,
  ClockIcon,
  AlertTriangleIcon,
  RefreshIcon,
  UsersIcon,
  SearchIcon,
  FileTextIcon,
} from '@/components/common/Icons';
import { AdminCategoriesTab } from './components/AdminCategoriesTab';
import { AdminProductsTab } from './components/AdminProductsTab';

type AdminTab = 'shops' | 'payouts' | 'complaints' | 'reports' | 'audit' | 'users' | 'categories' | 'products';

const getAdminTabFromPath = (pathname: string): AdminTab => {
  if (pathname.includes('/admin/payouts')) return 'payouts';
  if (pathname.includes('/admin/complaints')) return 'complaints';
  if (pathname.includes('/admin/reports')) return 'reports';
  if (pathname.includes('/admin/audit')) return 'audit';
  if (pathname.includes('/admin/users')) return 'users';
  if (pathname.includes('/admin/categories')) return 'categories';
  if (pathname.includes('/admin/products')) return 'products';
  return 'shops';
};

export const AdminDashboardPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = getAdminTabFromPath(location.pathname);

  const handleTabChange = (tab: AdminTab) => {
    navigate(`/admin/${tab}`);
  };
  const [loading, setLoading] = useState<boolean>(false);
  const [message, setMessage] = useState<{ text: string; type: 'success' | 'error' } | null>(null);

  // 1. Data States
  const [shops, setShops] = useState<AdminShopItem[]>([]);
  const [shopFilter, setShopFilter] = useState<string>('Pending');
  const [selectedShopDetail, setSelectedShopDetail] = useState<AdminShopDetail | null>(null);

  const [payouts, setPayouts] = useState<AdminPayoutItem[]>([]);
  const [payoutFilter, setPayoutFilter] = useState<string>('Requested');

  const [complaints, setComplaints] = useState<AdminComplaintItem[]>([]);
  const [reports, setReports] = useState<AdminReportItem[]>([]);

  const [reconciliation, setReconciliation] = useState<ReconciliationSummary | null>(null);
  const [reconciling, setReconciling] = useState<boolean>(false);

  const [users, setUsers] = useState<AdminUserItem[]>([]);
  const [userSearch, setUserSearch] = useState<string>('');

  // 2. Modals State
  const [rejectShopModal, setRejectShopModal] = useState<{ isOpen: boolean; shopId: string; reasonCode: string; reasonNote: string } | null>(null);
  const [sanctionModal, setSanctionModal] = useState<{ isOpen: boolean; shopId: string; type: number; reason: string; days?: number } | null>(null);
  const [markPaidModal, setMarkPaidModal] = useState<{ isOpen: boolean; payoutId: string; referenceCode: string; note: string } | null>(null);
  const [rejectPayoutModal, setRejectPayoutModal] = useState<{ isOpen: boolean; payoutId: string; reason: string } | null>(null);
  const [resolveComplaintModal, setResolveComplaintModal] = useState<{
    isOpen: boolean;
    complaintId: string;
    action: 'Refund' | 'Reject';
    bearer: 'Shop' | 'Platform' | 'Shared';
    decisionNote: string;
  } | null>(null);
  const [resolveReportModal, setResolveReportModal] = useState<{
    isOpen: boolean;
    reportId: string;
    action: 'Takedown' | 'WarnShop' | 'Dismiss';
    resolutionNote: string;
  } | null>(null);

  // Helper flash message
  const showFlash = (text: string, type: 'success' | 'error' = 'success') => {
    setMessage({ text, type });
    setTimeout(() => setMessage(null), 5000);
  };

  // Tab Loaders
  const loadTabData = async (tab: AdminTab) => {
    setLoading(true);
    try {
      if (tab === 'shops') {
        const res = await adminApi.getShops({ status: shopFilter || undefined, pageSize: 30 });
        setShops(res.items || []);
      } else if (tab === 'payouts') {
        const res = await adminApi.getPayouts({ status: payoutFilter || undefined, pageSize: 30 });
        setPayouts(res.items || []);
      } else if (tab === 'complaints') {
        const res = await adminApi.getComplaints({ pageSize: 30 });
        setComplaints(res.items || []);
      } else if (tab === 'reports') {
        const res = await adminApi.getReports({ pageSize: 30 });
        setReports(res.items || []);
      } else if (tab === 'audit') {
        const res = await adminApi.getReconciliation();
        setReconciliation(res);
      } else if (tab === 'users') {
        const res = await adminApi.getUsers({ search: userSearch || undefined, pageSize: 30 });
        setUsers(res.items || []);
      }
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Có lỗi xảy ra khi tải dữ liệu.', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTabData(activeTab);
  }, [activeTab, shopFilter, payoutFilter]);

  // Handlers: Shop
  const handleApproveShop = async (shopId: string) => {
    if (!window.confirm('Xác nhận phê duyệt cho gian hàng này đi vào hoạt động?')) return;
    try {
      await adminApi.approveShop(shopId);
      showFlash('Đã phê duyệt gian hàng thành công!');
      loadTabData('shops');
      if (selectedShopDetail?.id === shopId) setSelectedShopDetail(null);
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
      loadTabData('shops');
      if (selectedShopDetail?.id === rejectShopModal.shopId) setSelectedShopDetail(null);
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
      loadTabData('shops');
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi áp dụng chế tài.', 'error');
    }
  };

  const handleLiftSanction = async (shopId: string, sanctionId: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn dỡ bỏ quyết định xử phạt này trước thời hạn?')) return;
    try {
      await adminApi.liftSanction(shopId, sanctionId);
      showFlash('Đã gỡ bỏ chế tài xử phạt thành công.');
      loadTabData('shops');
      if (selectedShopDetail?.id === shopId) {
        const updated = await adminApi.getShopDetail(shopId);
        setSelectedShopDetail(updated);
      }
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Không thể dỡ bỏ chế tài.', 'error');
    }
  };

  // Handlers: Payout
  const handleApprovePayout = async (payoutId: string) => {
    if (!window.confirm('Xác nhận phê duyệt lệnh rút tiền này để tiến hành thanh toán?')) return;
    try {
      await adminApi.approvePayout(payoutId);
      showFlash('Đã phê duyệt lệnh rút tiền!');
      loadTabData('payouts');
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi duyệt lệnh rút tiền.', 'error');
    }
  };

  const handleConfirmMarkPaid = async () => {
    if (!markPaidModal) return;
    if (!markPaidModal.referenceCode.trim()) {
      alert('Vui lòng nhập mã giao dịch ngân hàng (FT/Trace code).');
      return;
    }
    try {
      await adminApi.markPaidPayout(markPaidModal.payoutId, markPaidModal.referenceCode, markPaidModal.note);
      showFlash('Xác nhận đã chuyển khoản thành công. Sổ cái đã ghi nhận giải phóng tiền!');
      setMarkPaidModal(null);
      loadTabData('payouts');
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi xác nhận thanh toán.', 'error');
    }
  };

  const handleConfirmRejectPayout = async () => {
    if (!rejectPayoutModal) return;
    if (!rejectPayoutModal.reason.trim()) {
      alert('Vui lòng nhập lý do từ chối rút tiền.');
      return;
    }
    try {
      await adminApi.rejectPayout(rejectPayoutModal.payoutId, rejectPayoutModal.reason);
      showFlash('Đã từ chối lệnh rút tiền và hoàn số dư khả dụng về ví shop.');
      setRejectPayoutModal(null);
      loadTabData('payouts');
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi từ chối lệnh rút.', 'error');
    }
  };

  // Handlers: Audit Fix Balance
  const handleFixBalance = async (shopId: string) => {
    try {
      await adminApi.fixShopBalance(shopId);
      showFlash('Đã đồng bộ và tự động sửa lệch số dư thành công từ Sổ cái kép!');
      const updated = await adminApi.getReconciliation();
      setReconciliation(updated);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi sửa lệch số dư.', 'error');
    }
  };

  // Handlers: User Status & Role
  const handleToggleUserStatus = async (user: AdminUserItem) => {
    const nextStatus = user.status === 'Active' ? 'Suspended' : 'Active';
    const actionLabel = nextStatus === 'Suspended' ? 'khóa' : 'mở khóa';
    if (!window.confirm(`Bạn có chắc muốn ${actionLabel} tài khoản của ${user.displayName}?`)) return;
    try {
      await adminApi.updateUserStatus(user.id, nextStatus);
      showFlash(`Đã ${actionLabel} tài khoản thành công!`);
      loadTabData('users');
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi cập nhật trạng thái.', 'error');
    }
  };

  const handleToggleUserRole = async (user: AdminUserItem) => {
    const nextRole = user.role === 'Admin' ? 'Member' : 'Admin';
    if (!window.confirm(`Chuyển vai trò của ${user.displayName} sang ${nextRole}?`)) return;
    try {
      await adminApi.updateUserRole(user.id, nextRole);
      showFlash(`Đã đổi vai trò sang ${nextRole}!`);
      loadTabData('users');
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi đổi quyền vai trò.', 'error');
    }
  };

  return (
    <div className="marketplace-container" style={{ paddingBottom: '4rem' }}>
      {/* Top Banner Header */}
      <div
        className="card-glass"
        style={{
          padding: '2rem 2.5rem',
          borderRadius: '20px',
          marginBottom: '2rem',
          border: '1px solid rgba(168, 85, 247, 0.3)',
          background: 'linear-gradient(135deg, rgba(168, 85, 247, 0.08) 0%, rgba(15, 23, 42, 0.8) 100%)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '1.5rem',
        }}
      >
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.5rem' }}>
            <span
              style={{
                width: '38px',
                height: '38px',
                borderRadius: '10px',
                background: 'rgba(168, 85, 247, 0.2)',
                color: '#c084fc',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <ShieldIcon size={22} />
            </span>
            <h1 style={{ fontSize: '1.85rem', fontWeight: 800, margin: 0, letterSpacing: '-0.02em', color: '#ffffff' }}>
              Trung Tâm Quản Trị Toàn Sàn
            </h1>
          </div>
          <p style={{ color: '#cbd5e1', margin: 0, fontSize: '0.95rem', maxWidth: '700px' }}>
            Giám sát vận hành sàn hàng số, phân xử tranh chấp, kiểm toán sổ cái kép bất biến và quản lý quyền truy cập.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '0.75rem', alignItems: 'center' }}>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={() => loadTabData(activeTab)}
            style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
          >
            <RefreshIcon size={14} /> Làm mới
          </button>
        </div>
      </div>

      {/* Flash Message Banner */}
      {message && (
        <div
          className={`alert ${message.type === 'success' ? 'alert-success' : 'alert-danger'}`}
          style={{ marginBottom: '1.5rem', borderRadius: '12px' }}
        >
          {message.text}
        </div>
      )}

      {/* Navigation Tabs Bar */}
      <div
        style={{
          display: 'flex',
          gap: '0.5rem',
          borderBottom: '1px solid var(--border-color)',
          marginBottom: '2rem',
          overflowX: 'auto',
          paddingBottom: '4px',
        }}
      >
        <button
          type="button"
          className={`tab-btn ${activeTab === 'shops' ? 'active' : ''}`}
          onClick={() => handleTabChange('shops')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'shops' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'shops' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <StoreIcon size={17} /> Gian hàng & Chế tài
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'payouts' ? 'active' : ''}`}
          onClick={() => handleTabChange('payouts')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'payouts' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'payouts' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <ClockIcon size={17} /> Hàng đợi Rút tiền
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'complaints' ? 'active' : ''}`}
          onClick={() => handleTabChange('complaints')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'complaints' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'complaints' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <ScaleIcon size={17} /> Phán quyết Khiếu nại
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'reports' ? 'active' : ''}`}
          onClick={() => handleTabChange('reports')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'reports' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'reports' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <AlertTriangleIcon size={17} /> Báo cáo Vi phạm
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'audit' ? 'active' : ''}`}
          onClick={() => handleTabChange('audit')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'audit' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'audit' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <FileTextIcon size={17} /> Kiểm toán Sổ cái kép
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'users' ? 'active' : ''}`}
          onClick={() => handleTabChange('users')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'users' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'users' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          <UsersIcon size={17} /> Người dùng & Phân quyền
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'categories' ? 'active' : ''}`}
          onClick={() => handleTabChange('categories')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'categories' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'categories' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          📁 Cây danh mục & Thuộc tính
        </button>

        <button
          type="button"
          className={`tab-btn ${activeTab === 'products' ? 'active' : ''}`}
          onClick={() => handleTabChange('products')}
          style={{
            padding: '0.75rem 1.25rem',
            background: 'none',
            border: 'none',
            borderBottom: activeTab === 'products' ? '3px solid #c084fc' : '3px solid transparent',
            color: activeTab === 'products' ? '#c084fc' : 'var(--text-muted)',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '8px',
          }}
        >
          📦 Toàn bộ Sản phẩm & Takedown
        </button>
      </div>

      {loading && (
        <div style={{ padding: '2rem', textAlign: 'center' }}>
          <div className="spinner" style={{ margin: '0 auto 0.5rem' }} />
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>Đang nạp dữ liệu điều hành...</p>
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 1: SHOPS & SANCTIONS                                                 */}
      {/* ========================================================================= */}
      {activeTab === 'shops' && !loading && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '1rem' }}>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              {(['Pending', 'Approved', 'Rejected', ''] as const).map((st) => (
                <button
                  key={st}
                  type="button"
                  className={`btn btn-sm ${shopFilter === st ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setShopFilter(st)}
                >
                  {st === 'Pending' ? 'Chờ duyệt' : st === 'Approved' ? 'Đã duyệt' : st === 'Rejected' ? 'Từ chối' : 'Tất cả'}
                </button>
              ))}
            </div>
            <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
              Tìm thấy {shops.length} gian hàng
            </span>
          </div>

          <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)' }}>
                    <th style={{ padding: '1rem' }}>Gian hàng</th>
                    <th style={{ padding: '1rem' }}>Liên hệ</th>
                    <th style={{ padding: '1rem' }}>Trạng thái</th>
                    <th style={{ padding: '1rem' }}>Ngày đăng ký</th>
                    <th style={{ padding: '1rem', textAlign: 'right' }}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {shops.length === 0 ? (
                    <tr>
                      <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                        Không có gian hàng nào trong danh sách lọc.
                      </td>
                    </tr>
                  ) : (
                    shops.map((s) => (
                      <tr key={s.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                        <td style={{ padding: '1rem' }}>
                          <div style={{ fontWeight: 700, color: '#fff' }}>{s.name}</div>
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>@{s.slug}</div>
                        </td>
                        <td style={{ padding: '1rem', color: 'var(--text-secondary)' }}>
                          {s.contactEmail}
                        </td>
                        <td style={{ padding: '1rem' }}>
                          <span
                            className={`badge ${
                              s.approvalStatus === 'Approved' || s.approvalStatus === 1
                                ? 'badge-success'
                                : s.approvalStatus === 'Rejected' || s.approvalStatus === 2
                                ? 'badge-danger'
                                : 'badge-warning'
                            }`}
                          >
                            {String(s.approvalStatus)}
                          </span>
                        </td>
                        <td style={{ padding: '1rem', color: 'var(--text-muted)' }}>
                          {new Date(s.createdAtUtc).toLocaleDateString('vi-VN')}
                        </td>
                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                          <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={async () => {
                                const detail = await adminApi.getShopDetail(s.id);
                                setSelectedShopDetail(detail);
                              }}
                            >
                              Xem hồ sơ
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
                                    type: 1, // TemporarySuspend
                                    reason: '',
                                    days: 7,
                                  })
                                }
                              >
                                Phạt chế tài
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

          {/* Shop Detail Inspector Drawer */}
          {selectedShopDetail && (
            <div
              className="card-glass"
              style={{
                marginTop: '1.5rem',
                padding: '1.5rem',
                borderRadius: '16px',
                border: '1px solid rgba(168, 85, 247, 0.4)',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
                <h3 style={{ margin: 0, fontWeight: 700 }}>Chi tiết hồ sơ nộp duyệt: {selectedShopDetail.name}</h3>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={() => setSelectedShopDetail(null)}
                >
                  Đóng
                </button>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))', gap: '1rem', marginBottom: '1.5rem' }}>
                <div>
                  <label style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Mô tả kinh doanh</label>
                  <p style={{ margin: '4px 0', color: 'var(--text-secondary)' }}>{selectedShopDetail.description || '(Không có mô tả)'}</p>
                </div>
                <div>
                  <label style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Mã Slug định danh</label>
                  <p style={{ margin: '4px 0', color: 'var(--text-secondary)' }}>/{selectedShopDetail.slug}</p>
                </div>
                <div>
                  <label style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Trạng thái hoạt động</label>
                  <p style={{ margin: '4px 0', color: 'var(--text-secondary)' }}>{selectedShopDetail.effectiveStatus}</p>
                </div>
              </div>

              {/* Sanctions List if any */}
              {selectedShopDetail.sanctions && selectedShopDetail.sanctions.length > 0 && (
                <div style={{ marginBottom: '1.5rem' }}>
                  <h4 style={{ fontSize: '0.95rem', fontWeight: 700, color: 'var(--accent-rose)', marginBottom: '0.5rem' }}>
                    Lịch sử Chế tài xử phạt ({selectedShopDetail.sanctions.length})
                  </h4>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                    {selectedShopDetail.sanctions.map((sc) => (
                      <div
                        key={sc.id}
                        style={{
                          padding: '0.75rem 1rem',
                          background: 'rgba(244, 63, 94, 0.1)',
                          borderRadius: '10px',
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                        }}
                      >
                        <div>
                          <strong>{sc.type}</strong>: {sc.reason} (Từ {new Date(sc.startAt).toLocaleDateString('vi-VN')}
                          {sc.endAt ? ` đến ${new Date(sc.endAt).toLocaleDateString('vi-VN')}` : ' - Vĩnh viễn'})
                          {sc.liftedAt && <span style={{ color: 'var(--accent-emerald)', marginLeft: '8px' }}>[Đã gỡ phạt]</span>}
                        </div>
                        {!sc.liftedAt && (
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
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 2: PAYOUT QUEUE                                                      */}
      {/* ========================================================================= */}
      {activeTab === 'payouts' && !loading && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '1rem' }}>
            <div style={{ display: 'flex', gap: '0.5rem' }}>
              {(['Requested', 'Approved', 'Succeeded', 'Rejected', ''] as const).map((st) => (
                <button
                  key={st}
                  type="button"
                  className={`btn btn-sm ${payoutFilter === st ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setPayoutFilter(st)}
                >
                  {st === 'Requested' ? 'Chờ duyệt' : st === 'Approved' ? 'Đã duyệt' : st === 'Succeeded' ? 'Đã trả' : st === 'Rejected' ? 'Từ chối' : 'Tất cả'}
                </button>
              ))}
            </div>
            <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
              Tổng cộng: {payouts.length} lệnh rút
            </span>
          </div>

          <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)' }}>
                    <th style={{ padding: '1rem' }}>Mã lệnh</th>
                    <th style={{ padding: '1rem' }}>Số tiền rút</th>
                    <th style={{ padding: '1rem' }}>Tài khoản thụ hưởng</th>
                    <th style={{ padding: '1rem' }}>Trạng thái</th>
                    <th style={{ padding: '1rem' }}>Thời gian gửi</th>
                    <th style={{ padding: '1rem', textAlign: 'right' }}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {payouts.length === 0 ? (
                    <tr>
                      <td colSpan={6} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                        Không có lệnh rút tiền nào ở trạng thái này.
                      </td>
                    </tr>
                  ) : (
                    payouts.map((p) => (
                      <tr key={p.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                        <td style={{ padding: '1rem', fontFamily: 'monospace', color: 'var(--accent-cyan)' }}>
                          {p.id.slice(0, 8)}...
                        </td>
                        <td style={{ padding: '1rem', fontWeight: 800, color: '#fff', fontSize: '1rem' }}>
                          {p.amount.toLocaleString('vi-VN')} đ
                        </td>
                        <td style={{ padding: '1rem' }}>
                          <div style={{ fontWeight: 600 }}>{p.bankAccountHolderName || 'Chủ gian hàng'}</div>
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                            {p.bankCode} • {p.bankAccountNumberMasked || 'STK'}
                          </div>
                        </td>
                        <td style={{ padding: '1rem' }}>
                          <span
                            className={`badge ${
                              p.status === 'Succeeded' || p.status === 3
                                ? 'badge-success'
                                : p.status === 'Approved' || p.status === 1
                                ? 'badge-info'
                                : p.status === 'Rejected' || p.status === 5
                                ? 'badge-danger'
                                : 'badge-warning'
                            }`}
                          >
                            {String(p.status)}
                          </span>
                        </td>
                        <td style={{ padding: '1rem', color: 'var(--text-muted)' }}>
                          {new Date(p.requestedAt).toLocaleString('vi-VN')}
                        </td>
                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                          <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                            {(p.status === 'Requested' || p.status === 0) && (
                              <>
                                <button
                                  type="button"
                                  className="btn btn-primary btn-sm"
                                  onClick={() => handleApprovePayout(p.id)}
                                >
                                  Duyệt lệnh
                                </button>
                                <button
                                  type="button"
                                  className="btn btn-danger btn-sm"
                                  onClick={() => setRejectPayoutModal({ isOpen: true, payoutId: p.id, reason: '' })}
                                >
                                  Từ chối
                                </button>
                              </>
                            )}

                            {(p.status === 'Approved' || p.status === 1) && (
                              <button
                                type="button"
                                className="btn btn-success btn-sm"
                                onClick={() =>
                                  setMarkPaidModal({
                                    isOpen: true,
                                    payoutId: p.id,
                                    referenceCode: '',
                                    note: '',
                                  })
                                }
                              >
                                Đã chuyển tiền
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
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 3: COMPLAINTS RESOLVER                                               */}
      {/* ========================================================================= */}
      {activeTab === 'complaints' && !loading && (
        <div>
          <div style={{ marginBottom: '1.25rem' }}>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700, margin: '0 0 0.25rem' }}>
              Hàng đợi Khiếu nại cần Ban Quản Trị phân xử
            </h3>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: 0 }}>
              Bao gồm các đơn khiếu nại bị Shop từ chối hoặc sau 24h Shop không phản hồi theo quy định.
            </p>
          </div>

          <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)' }}>
                    <th style={{ padding: '1rem' }}>Món hàng</th>
                    <th style={{ padding: '1rem' }}>Lý do khiếu nại</th>
                    <th style={{ padding: '1rem' }}>Mô tả lỗi</th>
                    <th style={{ padding: '1rem' }}>Trạng thái</th>
                    <th style={{ padding: '1rem', textAlign: 'right' }}>Phán quyết</th>
                  </tr>
                </thead>
                <tbody>
                  {complaints.length === 0 ? (
                    <tr>
                      <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                        Hiện không có tranh chấp khiếu nại nào chờ phân xử.
                      </td>
                    </tr>
                  ) : (
                    complaints.map((c) => (
                      <tr key={c.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                        <td style={{ padding: '1rem' }}>
                          <div style={{ fontWeight: 700 }}>{c.productName}</div>
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Shop: {c.shopName || c.shopId.slice(0, 8)}</div>
                        </td>
                        <td style={{ padding: '1rem', color: 'var(--accent-amber)', fontWeight: 600 }}>
                          {c.reasonCode}
                        </td>
                        <td style={{ padding: '1rem', color: 'var(--text-secondary)', maxWidth: '320px' }}>
                          {c.description}
                        </td>
                        <td style={{ padding: '1rem' }}>
                          <span className="badge badge-warning">{String(c.status)}</span>
                        </td>
                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                          <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                            <button
                              type="button"
                              className="btn btn-primary btn-sm"
                              onClick={() =>
                                setResolveComplaintModal({
                                  isOpen: true,
                                  complaintId: c.id,
                                  action: 'Refund',
                                  bearer: 'Shop',
                                  decisionNote: 'Chấp thuận khiếu nại do mã số lỗi không kích hoạt được.',
                                })
                              }
                            >
                              Hoàn tiền
                            </button>
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={() =>
                                setResolveComplaintModal({
                                  isOpen: true,
                                  complaintId: c.id,
                                  action: 'Reject',
                                  bearer: 'Shop',
                                  decisionNote: 'Bác bỏ khiếu nại do người mua không cung cấp đủ bằng chứng xác thực.',
                                })
                              }
                            >
                              Bác bỏ
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 4: ABUSE REPORTS                                                     */}
      {/* ========================================================================= */}
      {activeTab === 'reports' && !loading && (
        <div>
          <div style={{ marginBottom: '1.25rem' }}>
            <h3 style={{ fontSize: '1.1rem', fontWeight: 700, margin: '0 0 0.25rem' }}>
              Danh sách Báo cáo Vi phạm Bản quyền & Hàng số
            </h3>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: 0 }}>
              Tiếp nhận tố cáo lừa đảo, bản quyền giả mạo hoặc thông tin độc hại từ cộng đồng.
            </p>
          </div>

          <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)' }}>
                    <th style={{ padding: '1rem' }}>Sản phẩm bị tố cáo</th>
                    <th style={{ padding: '1rem' }}>Lý do vi phạm</th>
                    <th style={{ padding: '1rem' }}>Mô tả vi phạm</th>
                    <th style={{ padding: '1rem' }}>Bằng chứng</th>
                    <th style={{ padding: '1rem', textAlign: 'right' }}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {reports.length === 0 ? (
                    <tr>
                      <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                        Không có báo cáo vi phạm nào đang chờ xử lý.
                      </td>
                    </tr>
                  ) : (
                    reports.map((r) => (
                      <tr key={r.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                        <td style={{ padding: '1rem' }}>
                          <div style={{ fontWeight: 700 }}>{r.productName || r.productId.slice(0, 8)}</div>
                          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Mã SP: {r.productId.slice(0, 8)}</div>
                        </td>
                        <td style={{ padding: '1rem', color: 'var(--accent-rose)', fontWeight: 600 }}>
                          {r.reasonText || `Mã lý do: ${r.reason}`}
                        </td>
                        <td style={{ padding: '1rem', color: 'var(--text-secondary)', maxWidth: '300px' }}>
                          {r.description}
                        </td>
                        <td style={{ padding: '1rem' }}>
                          {r.evidenceUrl ? (
                            <a
                              href={r.evidenceUrl}
                              target="_blank"
                              rel="noreferrer"
                              style={{ color: 'var(--accent-cyan)', fontSize: '0.85rem' }}
                            >
                              Xem URL bằng chứng
                            </a>
                          ) : (
                            <span style={{ color: 'var(--text-muted)' }}>(Không có)</span>
                          )}
                        </td>
                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                          <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                            <button
                              type="button"
                              className="btn btn-danger btn-sm"
                              onClick={() =>
                                setResolveReportModal({
                                  isOpen: true,
                                  reportId: r.id,
                                  action: 'Takedown',
                                  resolutionNote: 'Gỡ bỏ sản phẩm do vi phạm nghiêm trọng chính sách sàn.',
                                })
                              }
                            >
                              Hạ gỡ SP
                            </button>
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={() =>
                                setResolveReportModal({
                                  isOpen: true,
                                  reportId: r.id,
                                  action: 'Dismiss',
                                  resolutionNote: 'Bác bỏ báo cáo do không có căn cứ vi phạm.',
                                })
                              }
                            >
                              Bác bỏ
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 5: DOUBLE-ENTRY LEDGER RECONCILIATION                                */}
      {/* ========================================================================= */}
      {activeTab === 'audit' && !loading && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem', flexWrap: 'wrap', gap: '1rem' }}>
            <div>
              <h3 style={{ fontSize: '1.2rem', fontWeight: 800, margin: '0 0 0.25rem' }}>
                Bảng Kiểm Toán Sổ Cái Kép Toàn Hệ Thống
              </h3>
              <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: 0 }}>
                Kiểm chứng bất biến toán học: Tổng Nợ (`Debit`) = Tổng Có (`Credit`), tự động phát hiện và khắc phục lệch số dư.
              </p>
            </div>
            <button
              type="button"
              className="btn btn-primary"
              disabled={reconciling}
              onClick={async () => {
                setReconciling(true);
                try {
                  const res = await adminApi.getReconciliation();
                  setReconciliation(res);
                  showFlash('Đã hoàn tất tiến trình kiểm toán đối soát sổ cái!');
                } catch {
                  showFlash('Lỗi khi chạy đối soát.', 'error');
                } finally {
                  setReconciling(false);
                }
              }}
              style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
            >
              <RefreshIcon size={16} /> Chạy đối soát ngay
            </button>
          </div>

          {reconciliation && (
            <div>
              {/* Metric Cards Grid */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '1.25rem', marginBottom: '2rem' }}>
                {/* 1. Sổ cái Balance */}
                <div
                  className="card-glass"
                  style={{
                    padding: '1.5rem',
                    borderRadius: '16px',
                    border: `1px solid ${reconciliation.isBalanced ? 'rgba(16, 185, 129, 0.4)' : 'rgba(244, 63, 94, 0.5)'}`,
                    background: reconciliation.isBalanced
                      ? 'linear-gradient(135deg, rgba(16, 185, 129, 0.08) 0%, transparent 100%)'
                      : 'linear-gradient(135deg, rgba(244, 63, 94, 0.08) 0%, transparent 100%)',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Cân bằng Nợ / Có Sổ cái</span>
                    {reconciliation.isBalanced ? (
                      <CheckCircleIcon size={20} className="text-success" />
                    ) : (
                      <AlertTriangleIcon size={20} className="text-danger" />
                    )}
                  </div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: reconciliation.isBalanced ? 'var(--accent-emerald)' : 'var(--accent-rose)' }}>
                    {reconciliation.isBalanced ? 'CÂN BẰNG 100%' : `LỆCH ${reconciliation.discrepancy.toLocaleString('vi-VN')} đ`}
                  </div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                    Nợ: {reconciliation.totalDebit.toLocaleString('vi-VN')} đ • Có: {reconciliation.totalCredit.toLocaleString('vi-VN')} đ
                  </div>
                </div>

                {/* 2. Quỹ Payouts */}
                <div
                  className="card-glass"
                  style={{
                    padding: '1.5rem',
                    borderRadius: '16px',
                    border: `1px solid ${reconciliation.payoutsMatch ? 'rgba(16, 185, 129, 0.4)' : 'rgba(244, 63, 94, 0.5)'}`,
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Quỹ Lệnh Rút PayoutPending</span>
                    {reconciliation.payoutsMatch ? (
                      <CheckCircleIcon size={20} className="text-success" />
                    ) : (
                      <AlertTriangleIcon size={20} className="text-danger" />
                    )}
                  </div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#fff' }}>
                    {reconciliation.payoutPendingBalance.toLocaleString('vi-VN')} đ
                  </div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                    Khớp 100% với tổng các lệnh rút đang mở ({reconciliation.activePayoutsTotal.toLocaleString('vi-VN')} đ)
                  </div>
                </div>

                {/* 3. Gian hàng Audited */}
                <div
                  className="card-glass"
                  style={{
                    padding: '1.5rem',
                    borderRadius: '16px',
                    border: '1px solid var(--border-color)',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Kiểm tra Ví Gian hàng</span>
                    <StoreIcon size={20} style={{ color: 'var(--accent-cyan)' }} />
                  </div>
                  <div style={{ fontSize: '1.35rem', fontWeight: 800, color: '#fff' }}>
                    {reconciliation.totalShopsAudited} gian hàng
                  </div>
                  <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>
                    {reconciliation.discrepantShopCount === 0 ? (
                      <span style={{ color: 'var(--accent-emerald)' }}>Toàn bộ cache số dư khớp hoàn hảo</span>
                    ) : (
                      <span style={{ color: 'var(--accent-rose)' }}>{reconciliation.discrepantShopCount} gian hàng bị lệch cache</span>
                    )}
                  </div>
                </div>
              </div>

              {/* Discrepant Shops Table & Self-Healing Action */}
              {reconciliation.discrepantShops && reconciliation.discrepantShops.length > 0 && (
                <div
                  className="card-glass"
                  style={{
                    borderRadius: '16px',
                    padding: '1.5rem',
                    border: '1px solid rgba(244, 63, 94, 0.4)',
                    background: 'rgba(244, 63, 94, 0.04)',
                  }}
                >
                  <h4 style={{ color: 'var(--accent-rose)', margin: '0 0 1rem', fontWeight: 700 }}>
                    Phát hiện sai lệch Cache ({reconciliation.discrepantShops.length} gian hàng)
                  </h4>
                  <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid var(--border-color)' }}>
                        <th style={{ padding: '0.75rem' }}>Gian hàng</th>
                        <th style={{ padding: '0.75rem' }}>Số dư Cache</th>
                        <th style={{ padding: '0.75rem' }}>Thực tính từ Sổ cái</th>
                        <th style={{ padding: '0.75rem' }}>Chênh lệch</th>
                        <th style={{ padding: '0.75rem', textAlign: 'right' }}>Khắc phục</th>
                      </tr>
                    </thead>
                    <tbody>
                      {reconciliation.discrepantShops.map((ds) => (
                        <tr key={ds.shopId} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                          <td style={{ padding: '0.75rem' }}>{ds.shopName}</td>
                          <td style={{ padding: '0.75rem' }}>{ds.cachedAvailable.toLocaleString('vi-VN')} đ</td>
                          <td style={{ padding: '0.75rem', fontWeight: 700 }}>{ds.computedAvailable.toLocaleString('vi-VN')} đ</td>
                          <td style={{ padding: '0.75rem', color: 'var(--accent-rose)' }}>{ds.difference.toLocaleString('vi-VN')} đ</td>
                          <td style={{ padding: '0.75rem', textAlign: 'right' }}>
                            <button
                              type="button"
                              className="btn btn-primary btn-sm"
                              onClick={() => handleFixBalance(ds.shopId)}
                            >
                              Sửa lệch (Self-healing)
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 6: USERS & ROLES                                                     */}
      {/* ========================================================================= */}
      {activeTab === 'users' && !loading && (
        <div>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.25rem', flexWrap: 'wrap', gap: '1rem' }}>
            <form
              onSubmit={(e) => {
                e.preventDefault();
                loadTabData('users');
              }}
              style={{ display: 'flex', gap: '0.5rem', maxWidth: '360px', width: '100%' }}
            >
              <div style={{ position: 'relative', width: '100%' }}>
                <SearchIcon
                  size={16}
                  style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }}
                />
                <input
                  type="text"
                  className="form-control"
                  placeholder="Tìm theo email hoặc tên..."
                  value={userSearch}
                  onChange={(e) => setUserSearch(e.target.value)}
                  style={{ paddingLeft: '36px', height: '38px', borderRadius: '10px' }}
                />
              </div>
              <button type="submit" className="btn btn-secondary btn-sm">Tìm</button>
            </form>
            <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
              Hiển thị {users.length} tài khoản
            </span>
          </div>

          <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.9rem' }}>
                <thead>
                  <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)' }}>
                    <th style={{ padding: '1rem' }}>Người dùng</th>
                    <th style={{ padding: '1rem' }}>Email</th>
                    <th style={{ padding: '1rem' }}>Vai trò</th>
                    <th style={{ padding: '1rem' }}>Trạng thái</th>
                    <th style={{ padding: '1rem', textAlign: 'right' }}>Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {users.length === 0 ? (
                    <tr>
                      <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                        Không tìm thấy người dùng nào phù hợp.
                      </td>
                    </tr>
                  ) : (
                    users.map((u) => (
                      <tr key={u.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                        <td style={{ padding: '1rem', fontWeight: 600 }}>{u.displayName}</td>
                        <td style={{ padding: '1rem', color: 'var(--text-secondary)' }}>{u.email}</td>
                        <td style={{ padding: '1rem' }}>
                          <span className={`badge ${u.role === 'Admin' ? 'badge-primary' : 'badge-secondary'}`}>
                            {u.role}
                          </span>
                        </td>
                        <td style={{ padding: '1rem' }}>
                          <span className={`badge ${u.status === 'Active' ? 'badge-success' : 'badge-danger'}`}>
                            {u.status === 'Active' ? 'Hoạt động' : 'Đã khóa'}
                          </span>
                        </td>
                        <td style={{ padding: '1rem', textAlign: 'right' }}>
                          <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                            <button
                              type="button"
                              className={`btn btn-sm ${u.status === 'Active' ? 'btn-danger' : 'btn-success'}`}
                              onClick={() => handleToggleUserStatus(u)}
                            >
                              {u.status === 'Active' ? 'Khóa' : 'Mở khóa'}
                            </button>
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={() => handleToggleUserRole(u)}
                            >
                              {u.role === 'Admin' ? 'Bỏ quyền Admin' : 'Cấp Admin'}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Categories & Attributes Tab Content */}
      {activeTab === 'categories' && <AdminCategoriesTab showFlash={showFlash} />}

      {/* Products & Moderation Tab Content */}
      {activeTab === 'products' && <AdminProductsTab showFlash={showFlash} />}

      {/* ========================================================================= */}
      {/* MODALS                                                                   */}
      {/* ========================================================================= */}

      {/* 1. Modal Từ chối Shop */}
      {rejectShopModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700 }}>Từ chối phê duyệt gian hàng</h3>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Mã lý do từ chối</label>
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
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Ghi chú giải trình chi tiết</label>
              <textarea
                className="form-control"
                rows={3}
                placeholder="Nhập hướng dẫn để người bán bổ sung nộp lại hồ sơ..."
                value={rejectShopModal.reasonNote}
                onChange={(e) => setRejectShopModal({ ...rejectShopModal, reasonNote: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setRejectShopModal(null)}>Hủy</button>
              <button type="button" className="btn btn-danger" onClick={handleConfirmRejectShop}>Xác nhận Từ chối</button>
            </div>
          </div>
        </div>
      )}

      {/* 2. Modal Chế tài Sanction */}
      {sanctionModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700, color: 'var(--accent-rose)' }}>Ban hành Chế tài Xử phạt</h3>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Loại hình chế tài</label>
              <select
                className="form-control"
                value={sanctionModal.type}
                onChange={(e) => setSanctionModal({ ...sanctionModal, type: Number(e.target.value) })}
              >
                <option value={0}>Cảnh cáo chính thức (Warning)</option>
                <option value={1}>Tạm đình chỉ hoạt động có thời hạn (TemporarySuspend)</option>
                <option value={2}>Cấm hoạt động vĩnh viễn (PermanentBan)</option>
              </select>
            </div>
            {sanctionModal.type === 1 && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Thời hạn phạt (ngày)</label>
                <input
                  type="number"
                  className="form-control"
                  value={sanctionModal.days || 7}
                  onChange={(e) => setSanctionModal({ ...sanctionModal, days: Number(e.target.value) })}
                />
              </div>
            )}
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Căn cứ & Lý do xử phạt</label>
              <textarea
                className="form-control"
                rows={3}
                placeholder="Nêu rõ hành vi vi phạm hoặc mã khiếu nại liên quan..."
                value={sanctionModal.reason}
                onChange={(e) => setSanctionModal({ ...sanctionModal, reason: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setSanctionModal(null)}>Hủy</button>
              <button type="button" className="btn btn-danger" onClick={handleConfirmSanction}>Ban hành quyết định</button>
            </div>
          </div>
        </div>
      )}

      {/* 3. Modal Mark Paid Payout */}
      {markPaidModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '460px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700, color: 'var(--accent-emerald)' }}>Xác nhận chuyển khoản ngân hàng</h3>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Mã tham chiếu giao dịch (FT / Trace No)</label>
              <input
                type="text"
                className="form-control"
                placeholder="VD: FT261012999812..."
                value={markPaidModal.referenceCode}
                onChange={(e) => setMarkPaidModal({ ...markPaidModal, referenceCode: e.target.value })}
              />
            </div>
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Ghi chú kế toán (Tùy chọn)</label>
              <input
                type="text"
                className="form-control"
                placeholder="VD: Đã đối soát qua tài khoản VCB..."
                value={markPaidModal.note}
                onChange={(e) => setMarkPaidModal({ ...markPaidModal, note: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setMarkPaidModal(null)}>Hủy</button>
              <button type="button" className="btn btn-success" onClick={handleConfirmMarkPaid}>Xác nhận & Ghi sổ</button>
            </div>
          </div>
        </div>
      )}

      {/* 4. Modal Reject Payout */}
      {rejectPayoutModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '460px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700 }}>Từ chối lệnh rút tiền</h3>
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Lý do từ chối (Hoàn tiền khả dụng về ví)</label>
              <textarea
                className="form-control"
                rows={3}
                placeholder="VD: Sai thông tin tên chủ tài khoản, ngân hàng từ chối..."
                value={rejectPayoutModal.reason}
                onChange={(e) => setRejectPayoutModal({ ...rejectPayoutModal, reason: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setRejectPayoutModal(null)}>Hủy</button>
              <button type="button" className="btn btn-danger" onClick={handleConfirmRejectPayout}>Xác nhận Từ chối</button>
            </div>
          </div>
        </div>
      )}

      {/* 5. Modal Resolve Complaint */}
      {resolveComplaintModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700 }}>
              Ban hành Phán quyết: {resolveComplaintModal.action === 'Refund' ? 'Hoàn tiền' : 'Bác bỏ'}
            </h3>
            {resolveComplaintModal.action === 'Refund' && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Bên chịu chi phí hoàn trả</label>
                <select
                  className="form-control"
                  value={resolveComplaintModal.bearer}
                  onChange={(e) => setResolveComplaintModal({ ...resolveComplaintModal, bearer: e.target.value as any })}
                >
                  <option value="Shop">Gian hàng chịu 100% chi phí</option>
                  <option value="Platform">Sàn hỗ trợ bồi thường (Lỗi kỹ thuật hệ thống)</option>
                  <option value="Shared">Chia sẻ rủi ro đôi bên</option>
                </select>
              </div>
            )}
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Lý lẽ phán quyết</label>
              <textarea
                className="form-control"
                rows={3}
                value={resolveComplaintModal.decisionNote}
                onChange={(e) => setResolveComplaintModal({ ...resolveComplaintModal, decisionNote: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setResolveComplaintModal(null)}>Hủy</button>
              <button
                type="button"
                className="btn btn-primary"
                onClick={async () => {
                  try {
                    await adminApi.resolveComplaint(resolveComplaintModal.complaintId, {
                      action: resolveComplaintModal.action,
                      decisionNote: resolveComplaintModal.decisionNote,
                      refundBearer: resolveComplaintModal.bearer,
                    });
                    showFlash('Đã ban hành phán quyết khiếu nại thành công!');
                    setResolveComplaintModal(null);
                    loadTabData('complaints');
                  } catch (err: any) {
                    showFlash(err.response?.data?.message || 'Lỗi khi ban hành phán quyết.', 'error');
                  }
                }}
              >
                Ban hành
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 6. Modal Resolve Report */}
      {resolveReportModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700, color: 'var(--accent-rose)' }}>
              Xử lý Tố cáo: {resolveReportModal.action === 'Takedown' ? 'Hạ gỡ sản phẩm' : 'Bác bỏ'}
            </h3>
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>Ghi chú quyết định</label>
              <textarea
                className="form-control"
                rows={3}
                value={resolveReportModal.resolutionNote}
                onChange={(e) => setResolveReportModal({ ...resolveReportModal, resolutionNote: e.target.value })}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setResolveReportModal(null)}>Hủy</button>
              <button
                type="button"
                className="btn btn-danger"
                onClick={async () => {
                  try {
                    await adminApi.resolveReport(resolveReportModal.reportId, {
                      action: resolveReportModal.action,
                      resolutionNote: resolveReportModal.resolutionNote,
                    });
                    showFlash('Đã xử lý tố cáo vi phạm thành công!');
                    setResolveReportModal(null);
                    loadTabData('reports');
                  } catch (err: any) {
                    showFlash(err.response?.data?.message || 'Lỗi khi xử lý báo cáo.', 'error');
                  }
                }}
              >
                Xác nhận
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
