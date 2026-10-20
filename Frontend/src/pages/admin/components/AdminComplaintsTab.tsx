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

      <div className="admin-table-card">
        <div style={{ overflowX: 'auto' }}>
          <table className="admin-table">
            <thead>
              <tr>
                <th>Món hàng</th>
                <th>Lý do khiếu nại</th>
                <th>Mô tả lỗi</th>
                <th>Trạng thái</th>
                <th style={{ textAlign: 'right' }}>Phán quyết</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: '#64748b' }}>
                    Đang tải danh sách khiếu nại...
                  </td>
                </tr>
              ) : complaints.length === 0 ? (
                <tr>
                  <td colSpan={5} style={{ padding: '3rem', textAlign: 'center', color: '#64748b' }}>
                    Hiện không có tranh chấp khiếu nại nào chờ phân xử.
                  </td>
                </tr>
              ) : (
                complaints.map((c) => (
                  <tr key={c.id}>
                    <td>
                      <div className="admin-cell-title">{c.productName}</div>
                      <div className="admin-cell-subtitle">Shop: {c.shopName || c.shopId.slice(0, 8)}</div>
                    </td>
                    <td style={{ color: '#b45309', fontWeight: 700 }}>
                      {c.reasonCode}
                    </td>
                    <td style={{ color: '#334155', maxWidth: '320px', lineHeight: 1.5 }}>
                      {c.description}
                    </td>
                    <td>
                      <span className="badge badge-warning" style={{ fontWeight: 700 }}>{String(c.status)}</span>
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '0.5rem' }}>
                        <button
                          type="button"
                          className="btn btn-primary btn-sm"
                          style={{ fontWeight: 700 }}
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
                          style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }}
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
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 800, color: '#0f172a' }}>
              Ban hành Phán quyết: {resolveComplaintModal.action === 'Refund' ? 'Hoàn tiền' : 'Bác bỏ'}
            </h3>
            {resolveComplaintModal.action === 'Refund' && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>Bên chịu chi phí hoàn trả</label>
                <select
                  className="form-control"
                  value={resolveComplaintModal.bearer}
                  onChange={(e) => setResolveComplaintModal({ ...resolveComplaintModal, bearer: e.target.value as any })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a' }}
                >
                  <option value="Shop">Gian hàng chịu 100% chi phí</option>
                  <option value="Platform">Sàn hỗ trợ bồi thường (Lỗi kỹ thuật hệ thống)</option>
                  <option value="Shared">Chia sẻ rủi ro đôi bên</option>
                </select>
              </div>
            )}
            <div style={{ marginBottom: '1.5rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '6px', color: '#334155', fontWeight: 600 }}>Lý lẽ phán quyết</label>
              <textarea
                className="form-control"
                rows={3}
                value={resolveComplaintModal.decisionNote}
                onChange={(e) => setResolveComplaintModal({ ...resolveComplaintModal, decisionNote: e.target.value })}
                style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a' }}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" style={{ background: '#f1f5f9', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 600 }} onClick={() => setResolveComplaintModal(null)}>Hủy</button>
              <button
                type="button"
                className="btn btn-primary"
                style={{ fontWeight: 700 }}
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
