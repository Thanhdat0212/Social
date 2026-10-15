import React, { useEffect, useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminPayoutItem } from '@/types/admin';

interface AdminPayoutsTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminPayoutsTab: React.FC<AdminPayoutsTabProps> = ({ showFlash }) => {
  const [payouts, setPayouts] = useState<AdminPayoutItem[]>([]);
  const [payoutFilter, setPayoutFilter] = useState<string>('Requested');
  const [loading, setLoading] = useState(false);

  // Modals
  const [markPaidModal, setMarkPaidModal] = useState<{
    isOpen: boolean;
    payoutId: string;
    referenceCode: string;
    note: string;
  } | null>(null);

  const [rejectPayoutModal, setRejectPayoutModal] = useState<{
    isOpen: boolean;
    payoutId: string;
    reason: string;
  } | null>(null);

  const loadPayouts = async (status: string) => {
    setLoading(true);
    try {
      const res = await adminApi.getPayouts({ status: status || undefined, pageSize: 30 });
      setPayouts(res.items || []);
    } catch (err) {
      console.error('Error loading payouts:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPayouts(payoutFilter);
  }, [payoutFilter]);

  const handleApprovePayout = async (payoutId: string) => {
    if (!window.confirm('Xác nhận phê duyệt lệnh rút tiền này để tiến hành thanh toán?')) return;
    try {
      await adminApi.approvePayout(payoutId);
      showFlash('Đã phê duyệt lệnh rút tiền!');
      loadPayouts(payoutFilter);
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
      loadPayouts(payoutFilter);
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
      loadPayouts(payoutFilter);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi từ chối lệnh rút.', 'error');
    }
  };

  return (
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
              {loading ? (
                <tr>
                  <td colSpan={6} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                    Đang tải danh sách lệnh rút...
                  </td>
                </tr>
              ) : payouts.length === 0 ? (
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

      {/* Modal Mark Paid Payout */}
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

      {/* Modal Reject Payout */}
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
    </div>
  );
};
