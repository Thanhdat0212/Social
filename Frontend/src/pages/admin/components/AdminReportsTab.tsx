import React, { useEffect, useState } from 'react';
import { adminApi } from '@/api/adminApi';
import type { AdminReportItem } from '@/types/admin';

interface AdminReportsTabProps {
  showFlash: (text: string, type?: 'success' | 'error') => void;
}

export const AdminReportsTab: React.FC<AdminReportsTabProps> = ({ showFlash }) => {
  const [reports, setReports] = useState<AdminReportItem[]>([]);
  const [loading, setLoading] = useState(false);

  const [resolveReportModal, setResolveReportModal] = useState<{
    isOpen: boolean;
    reportId: string;
    action: 'Takedown' | 'WarnShop' | 'Dismiss';
    resolutionNote: string;
  } | null>(null);

  const loadReports = async () => {
    setLoading(true);
    try {
      const res = await adminApi.getReports({ pageSize: 30 });
      setReports(res.items || []);
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Có lỗi xảy ra khi tải danh sách báo cáo.', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadReports();
  }, []);

  const handleConfirmResolve = async () => {
    if (!resolveReportModal) return;
    try {
      await adminApi.resolveReport(resolveReportModal.reportId, {
        action: resolveReportModal.action,
        resolutionNote: resolveReportModal.resolutionNote,
      });
      showFlash('Đã xử lý tố cáo vi phạm thành công!');
      setResolveReportModal(null);
      loadReports();
    } catch (err: any) {
      showFlash(err.response?.data?.message || 'Lỗi khi xử lý báo cáo.', 'error');
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
        Đang tải danh sách báo cáo vi phạm...
      </div>
    );
  }

  return (
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

      {/* Modal Resolve Report */}
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
                onClick={handleConfirmResolve}
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
