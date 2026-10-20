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
      <div className="admin-filter-bar">
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <span style={{ fontSize: '0.88rem', color: '#475569', fontWeight: 600 }}>Lọc trạng thái:</span>
          {[
            { key: 'Pending', label: 'Chờ duyệt' },
            { key: 'Approved', label: 'Đã duyệt' },
            { key: 'Rejected', label: 'Bị từ chối' },
            { key: '', label: 'Tất cả' },
          ].map((item) => (
            <button
              key={item.key || 'All'}
              type="button"
              className={`admin-filter-btn ${requestFilter === item.key ? 'active' : ''}`}
              onClick={() => setRequestFilter(item.key)}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      <div className="admin-table-card">
        <div style={{ overflowX: 'auto' }}>
          <table className="admin-table">
            <thead>
              <tr>
                <th>Gian hàng</th>
                <th>Tên danh mục đề xuất</th>
                <th>Danh mục cha gợi ý</th>
                <th>Lý do / Thuyết minh</th>
                <th>Trạng thái</th>
                <th>Thời gian</th>
                <th style={{ textAlign: 'right' }}>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {requests.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: '3.5rem', color: '#64748b', fontWeight: 500 }}>
                    Không có yêu cầu mở danh mục nào.
                  </td>
                </tr>
              ) : (
                requests.map((req) => (
                  <tr key={req.id}>
                    <td style={{ padding: '0.95rem 1.25rem', fontWeight: 700, color: '#0f172a' }}>{req.shopName}</td>
                    <td style={{ padding: '0.95rem 1.25rem', color: '#0284c7', fontWeight: 700 }}>{req.name}</td>
                    <td style={{ padding: '0.95rem 1.25rem', color: '#475569', fontWeight: 500 }}>
                      {req.suggestedParentName || '(Gốc)'}
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem', maxWidth: '280px', color: '#1e293b', fontSize: '0.88rem' }}>
                      {req.reasonNote || 'Không có mô tả'}
                      {req.adminNote && (
                        <div
                          style={{
                            fontSize: '0.78rem',
                            color: '#b91c1c',
                            background: '#fef2f2',
                            border: '1px solid #fecaca',
                            padding: '3px 8px',
                            borderRadius: '6px',
                            marginTop: '6px',
                            fontWeight: 600,
                          }}
                        >
                          Phản hồi: {req.adminNote}
                        </div>
                      )}
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem' }}>
                      <span
                        className={`admin-pill-badge ${
                          req.status === 'Approved'
                            ? 'admin-pill-green'
                            : req.status === 'Rejected'
                            ? 'admin-pill-red'
                            : 'admin-pill-amber'
                        }`}
                      >
                        {req.status === 'Approved' ? 'Đã duyệt' : req.status === 'Rejected' ? 'Từ chối' : 'Chờ duyệt'}
                      </span>
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem', fontSize: '0.85rem', color: '#64748b', fontWeight: 500 }}>
                      {new Date(req.createdAtUtc).toLocaleDateString('vi-VN')}
                    </td>
                    <td style={{ padding: '0.95rem 1.25rem', textAlign: 'right' }}>
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
                            style={{ padding: '4px 10px', fontSize: '0.78rem', fontWeight: 700 }}
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
                            style={{ padding: '4px 10px', fontSize: '0.78rem', fontWeight: 700 }}
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
        <div className="modal-backdrop">
          <div className="admin-modal-card" style={{ width: '90%', maxWidth: '480px', padding: '2rem', borderRadius: '16px' }}>
            <h3 style={{ margin: '0 0 1rem', fontWeight: 800, color: reviewRequestModal.isApproved ? '#059669' : '#b91c1c' }}>
              {reviewRequestModal.isApproved ? 'Phê duyệt yêu cầu mở danh mục' : 'Từ chối yêu cầu mở danh mục'}
            </h3>
            <p style={{ fontSize: '0.9rem', color: '#334155', marginBottom: '1.25rem', lineHeight: 1.5 }}>
              Gian hàng: <strong style={{ color: '#0f172a' }}>{reviewRequestModal.request.shopName}</strong> | Đề xuất: <strong style={{ color: '#0284c7' }}>{reviewRequestModal.request.name}</strong>
            </p>

            {reviewRequestModal.isApproved && (
              <div style={{ marginBottom: '1rem' }}>
                <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                  Gán vào danh mục thực tế (Resolved Category)
                </label>
                <select
                  className="form-control"
                  value={reviewRequestModal.resolvedCategoryId}
                  onChange={(e) => setReviewRequestModal({ ...reviewRequestModal, resolvedCategoryId: e.target.value })}
                  style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
                >
                  <option value="">-- Chọn danh mục đã tồn tại nếu có --</option>
                  {allFlatCategories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {'- '.repeat(c.depth)} {c.name}
                    </option>
                  ))}
                </select>
                <p style={{ fontSize: '0.78rem', color: '#64748b', marginTop: '4px' }}>
                  Nếu đây là danh mục hoàn toàn mới, hãy vào tab Cây danh mục tạo trước rồi chọn ở đây.
                </p>
              </div>
            )}

            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', marginBottom: '5px', color: '#334155', fontWeight: 600 }}>
                Ghi chú gửi cho Gian hàng {reviewRequestModal.isApproved ? '(Tùy chọn)' : '(Bắt buộc lý do)'}
              </label>
              <textarea
                className="form-control"
                rows={3}
                placeholder={reviewRequestModal.isApproved ? 'Danh mục đã được khởi tạo và sẵn sàng đăng bán.' : 'Lý do từ chối...'}
                value={reviewRequestModal.adminNote}
                onChange={(e) => setReviewRequestModal({ ...reviewRequestModal, adminNote: e.target.value })}
                style={{ background: '#ffffff', border: '1px solid #cbd5e1', color: '#0f172a', fontWeight: 500 }}
              />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button type="button" className="btn btn-secondary" onClick={() => setReviewRequestModal(null)} style={{ fontWeight: 600 }}>Hủy</button>
              <button
                type="button"
                className={`btn ${reviewRequestModal.isApproved ? 'btn-primary' : 'btn-danger'}`}
                onClick={handleConfirmReviewRequest}
                style={{ fontWeight: 700 }}
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
