import React, { useEffect, useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminShopItem, AdminShopDetail } from '@/types/admin';

interface AdminShopsTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminShopsTab: React.FC<AdminShopsTabProps> = ({ showFlash }) => {
  const [shops, setShops] = useState<AdminShopItem[]>([]);
  const [shopFilter, setShopFilter] = useState<string>('Pending');
  const [selectedShopDetail, setSelectedShopDetail] = useState<AdminShopDetail | null>(null);
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

  const loadShops = async (status: string) => {
    setLoading(true);
    try {
      const res = await adminApi.getShops({ status: status || undefined, pageSize: 30 });
      setShops(res.items || []);
    } catch (err) {
      console.error('Error loading shops:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadShops(shopFilter);
  }, [shopFilter]);

  const handleApproveShop = async (shopId: string) => {
    if (!window.confirm('Xác nhận phê duyệt cho gian hàng này đi vào hoạt động?')) return;
    try {
      await adminApi.approveShop(shopId);
      showFlash('Đã phê duyệt gian hàng thành công!');
      loadShops(shopFilter);
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
      loadShops(shopFilter);
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
      loadShops(shopFilter);
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
              {loading ? (
                <tr>
                  <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                    Đang tải danh sách gian hàng...
                  </td>
                </tr>
              ) : shops.length === 0 ? (
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

      {/* Modal Từ chối Shop */}
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

      {/* Modal Chế tài Sanction */}
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
    </div>
  );
};
