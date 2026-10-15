import React, { useEffect, useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminComplaintItem } from '@/types/admin';

interface AdminComplaintsTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminComplaintsTab: React.FC<AdminComplaintsTabProps> = ({ showFlash }) => {
  const [complaints, setComplaints] = useState<AdminComplaintItem[]>([]);
  const [loading, setLoading] = useState(false);

  // Modal Resolve Complaint
  const [resolveComplaintModal, setResolveComplaintModal] = useState<{
    isOpen: boolean;
    complaintId: string;
    action: 'Refund' | 'Reject';
    bearer: 'Shop' | 'Platform' | 'Shared';
    decisionNote: string;
  } | null>(null);

  const loadComplaints = async () => {
    setLoading(true);
    try {
      const res = await adminApi.getComplaints();
      setComplaints(res.items || []);
    } catch (err) {
      console.error('Error loading complaints:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadComplaints();
  }, []);

  const handleConfirmResolve = async () => {
    if (!resolveComplaintModal) return;
    try {
      await adminApi.resolveComplaint(resolveComplaintModal.complaintId, {
        action: resolveComplaintModal.action,
        decisionNote: resolveComplaintModal.decisionNote,
        refundBearer: resolveComplaintModal.bearer,
      });
      showFlash('Đã ban hành phán quyết khiếu nại thành công!');
      setResolveComplaintModal(null);
      loadComplaints();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi ban hành phán quyết.', 'error');
    }
  };

  return (
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
              {loading ? (
                <tr>
                  <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-muted)' }}>
                    Đang tải danh sách khiếu nại...
                  </td>
                </tr>
              ) : complaints.length === 0 ? (
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

      {/* Modal Resolve Complaint */}
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
                onClick={handleConfirmResolve}
              >
                Ban hành
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
