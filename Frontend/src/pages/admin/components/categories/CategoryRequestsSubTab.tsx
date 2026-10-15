import React, { useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminCategoryRequestItem } from '@/types/admin';

interface CategoryRequestsSubTabProps {
  requests: AdminCategoryRequestItem[];
  allFlatCategories: { id: string; name: string; depth: number }[];
  loadRequests: () => Promise<void>;
  showFlash: (text: string, type?: 'success' | 'error') => void;
  requestFilter: string;
  setRequestFilter: (status: string) => void;
}

export const CategoryRequestsSubTab: React.FC<CategoryRequestsSubTabProps> = ({
  requests,
  allFlatCategories,
  loadRequests,
  showFlash,
  requestFilter,
  setRequestFilter,
}) => {
  const [reviewRequestModal, setReviewRequestModal] = useState<{
    isOpen: boolean;
    request: AdminCategoryRequestItem;
    isApproved: boolean;
    resolvedCategoryId: string;
    adminNote: string;
  } | null>(null);

  const handleConfirmReviewRequest = async () => {
    if (!reviewRequestModal) return;
    try {
      await adminApi.reviewCategoryRequest(reviewRequestModal.request.id, {
        isApproved: reviewRequestModal.isApproved,
        resolvedCategoryId: reviewRequestModal.resolvedCategoryId || undefined,
        adminNote: reviewRequestModal.adminNote.trim() || undefined,
      });
      showFlash(reviewRequestModal.isApproved ? 'Đã phê duyệt yêu cầu mở danh mục!' : 'Đã từ chối yêu cầu.');
      setReviewRequestModal(null);
      loadRequests();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi xử lý yêu cầu.', 'error');
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '1.25rem', alignItems: 'center' }}>
        <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Lọc trạng thái:</span>
        {['Pending', 'Approved', 'Rejected', ''].map((st) => (
          <button
            key={st || 'All'}
            type="button"
            className={`btn btn-sm ${requestFilter === st ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setRequestFilter(st)}
          >
            {st === 'Pending' ? 'Chờ duyệt' : st === 'Approved' ? 'Đã duyệt' : st === 'Rejected' ? 'Bị từ chối' : 'Tất cả'}
          </button>
        ))}
      </div>

      <div className="card-glass" style={{ borderRadius: '16px', overflow: 'hidden', border: '1px solid var(--border-color)' }}>
        <div style={{ overflowX: 'auto' }}>
          <table className="table" style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
            <thead>
              <tr style={{ background: 'rgba(255,255,255,0.03)', borderBottom: '1px solid var(--border-color)', textAlign: 'left' }}>
                <th style={{ padding: '1rem 1.25rem' }}>Gian hàng</th>
                <th style={{ padding: '1rem 1.25rem' }}>Tên danh mục đề xuất</th>
                <th style={{ padding: '1rem 1.25rem' }}>Danh mục cha gợi ý</th>
                <th style={{ padding: '1rem 1.25rem' }}>Lý do / Thuyết minh</th>
                <th style={{ padding: '1rem 1.25rem' }}>Trạng thái</th>
                <th style={{ padding: '1rem 1.25rem' }}>Thời gian</th>
                <th style={{ padding: '1rem 1.25rem', textAlign: 'right' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {requests.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                    Không có yêu cầu mở danh mục nào.
                  </td>
                </tr>
              ) : (
                requests.map((req) => (
                  <tr key={req.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                    <td style={{ padding: '0.85rem 1.25rem', fontWeight: 600 }}>{req.shopName}</td>
                    <td style={{ padding: '0.85rem 1.25rem', color: '#c084fc', fontWeight: 600 }}>{req.name}</td>
                    <td style={{ padding: '0.85rem 1.25rem', color: 'var(--text-muted)' }}>
                      {req.suggestedParentName || '(Gốc)'}
                    </td>
                    <td style={{ padding: '0.85rem 1.25rem', maxWidth: '280px', color: 'var(--text-secondary)' }}>
                      {req.reasonNote || 'Không có mô tả'}
                      {req.adminNote && (
                        <div style={{ fontSize: '0.75rem', color: 'var(--accent-rose)', marginTop: '4px' }}>
                          Phản hồi: {req.adminNote}
                        </div>
                      )}
                    </td>
                    <td style={{ padding: '0.85rem 1.25rem' }}>
                      <span
                        style={{
                          fontSize: '0.75rem',
                          padding: '2px 8px',
                          borderRadius: '6px',
                          fontWeight: 600,
                          background:
                            req.status === 'Approved'
                              ? 'rgba(34, 197, 94, 0.15)'
                              : req.status === 'Rejected'
                              ? 'rgba(239, 68, 68, 0.15)'
                              : 'rgba(245, 158, 11, 0.15)',
                          color:
                            req.status === 'Approved'
                              ? '#22c55e'
                              : req.status === 'Rejected'
                              ? '#ef4444'
                              : '#f59e0b',
                        }}
                      >
                        {req.status === 'Approved' ? 'Đã duyệt' : req.status === 'Rejected' ? 'Từ chối' : 'Chờ duyệt'}
                      </span>
                    </td>
                    <td style={{ padding: '0.85rem 1.25rem', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      {new Date(req.createdAtUtc).toLocaleDateString('vi-VN')}
                    </td>
                    <td style={{ padding: '0.85rem 1.25rem', textAlign: 'right' }}>
                      {req.status === 'Pending' && (
                        <div style={{ display: 'inline-flex', gap: '6px' }}>
                          <button
                            type="button"
                            className="btn btn-primary btn-sm"
                            onClick={() =>
                              setReviewRequestModal({
                                isOpen: true,
                                request: req,
                                isApproved: true,
                                resolvedCategoryId: '',
                                adminNote: '',
                              })
                            }
                            style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                          >
                            Duyệt
                          </button>
                          <button
                            type="button"
                            className="btn btn-danger btn-sm"
                            onClick={() =>
                              setReviewRequestModal({
                                isOpen: true,
                                request: req,
                                isApproved: false,
                                resolvedCategoryId: '',
                                adminNote: '',
                              })
                            }
                            style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                          >
                            Từ chối
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* MODAL: REVIEW CATEGORY REQUEST */}
      {reviewRequestModal && (
        <div className="modal-backdrop" style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000 }}>
          <div className="card-glass" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px', border: '1px solid var(--border-color)' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 700, color: reviewRequestModal.isApproved ? '#22c55e' : '#ef4444' }}>
              {reviewRequestModal.isApproved ? 'Phê duyệt yêu cầu mở danh mục' : 'Từ chối yêu cầu mở danh mục'}
            </h3>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1.25rem' }}>
              Gian hàng: <strong>{reviewRequestModal.request.shopName}</strong> | Đề xuất: <strong>{reviewRequestModal.request.name}</strong>
            </p>

            {reviewRequestModal.isApproved && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                  Gán vào danh mục thực tế (Resolved Category)
                </label>
                <select
                  className="form-control"
                  value={reviewRequestModal.resolvedCategoryId}
                  onChange={(e) => setReviewRequestModal({ ...reviewRequestModal, resolvedCategoryId: e.target.value })}
                >
                  <option value="">-- Chọn danh mục đã tồn tại nếu có --</option>
                  {allFlatCategories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {'- '.repeat(c.depth)} {c.name}
                    </option>
                  ))}
                </select>
                <p style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                  Nếu đây là danh mục hoàn toàn mới, hãy vào tab Cây danh mục tạo trước rồi chọn ở đây.
                </p>
              </div>
            )}

            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '4px', color: 'var(--text-muted)' }}>
                Ghi chú gửi cho Gian hàng {reviewRequestModal.isApproved ? '(Tùy chọn)' : '(Bắt buộc lý do)'}
              </label>
              <textarea
                className="form-control"
                rows={3}
                placeholder={reviewRequestModal.isApproved ? 'Danh mục đã được khởi tạo và sẵn sàng đăng bán.' : 'Lý do từ chối...'}
                value={reviewRequestModal.adminNote}
                onChange={(e) => setReviewRequestModal({ ...reviewRequestModal, adminNote: e.target.value })}
              />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setReviewRequestModal(null)}>Hủy</button>
              <button
                type="button"
                className={`btn ${reviewRequestModal.isApproved ? 'btn-primary' : 'btn-danger'}`}
                onClick={handleConfirmReviewRequest}
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
