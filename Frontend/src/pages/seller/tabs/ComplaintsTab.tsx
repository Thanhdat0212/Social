import React, { useEffect, useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import type { ComplaintDto } from '@/types/marketplace';
import { Button, Badge, Textarea, Spinner } from '@/components/common';

export const ComplaintsTab: React.FC = () => {
  const [complaints, setComplaints] = useState<ComplaintDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [respondingId, setRespondingId] = useState<string | null>(null);
  const [responseNote, setResponseNote] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  const loadComplaints = async () => {
    setLoading(true);
    try {
      const res = await marketplaceApi.getShopComplaints();
      setComplaints(res.items || res || []);
    } catch (err) {
      console.error('Error loading shop complaints:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadComplaints();
  }, []);

  const handleRespondComplaint = async (complaintId: string, acceptRefund: boolean) => {
    if (!responseNote.trim()) {
      alert('Vui lòng nhập ghi chú phản hồi.');
      return;
    }
    setActionLoading(true);
    try {
      await marketplaceApi.respondComplaint(complaintId, acceptRefund, responseNote.trim());
      alert(acceptRefund ? 'Đã chấp thuận hoàn tiền cho người mua.' : 'Đã từ chối khiếu nại. Quản trị viên sàn sẽ can thiệp.');
      setRespondingId(null);
      setResponseNote('');
      loadComplaints();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể phản hồi khiếu nại.');
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) {
    return (
      <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '3rem', textAlign: 'center' }}>
        <Spinner size="md" />
        <p style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: 'var(--text-dim)' }}>Đang tải danh sách khiếu nại...</p>
      </div>
    );
  }

  return (
    <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '16px', padding: '1.5rem' }}>
      <h2 style={{ fontSize: '1.1rem', fontWeight: 700, color: 'var(--text-main)', marginBottom: '1rem' }}>
        Khiếu nại từ khách hàng (Thời hạn phản hồi 24h)
      </h2>
      {complaints.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--text-muted)' }}>
          Hiện tại không có khiếu nại nào từ khách hàng.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {complaints.map((c) => (
            <div key={c.id} style={{ padding: '1.25rem', background: 'var(--bg-surface)', borderRadius: '12px', border: '1px solid var(--border-color)' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
                <div>
                  <div style={{ fontWeight: 700, fontSize: '0.95rem', color: 'var(--text-main)' }}>
                    Lý do: {c.reason} • Đơn: #{c.shopOrderId?.slice(0, 8)}
                  </div>
                  <div style={{ fontSize: '0.78rem', color: 'var(--text-dim)', marginTop: '0.2rem' }}>
                    Gửi lúc: {new Date(c.createdAtUtc).toLocaleString('vi-VN')}
                  </div>
                </div>
                <Badge variant={c.status === 'Resolved' || c.status === 'Refunded' ? 'success' : 'warning'} size="sm">
                  {c.status}
                </Badge>
              </div>

              <p style={{ fontSize: '0.88rem', margin: '0.5rem 0', background: 'var(--bg-card)', padding: '0.75rem', borderRadius: '8px', color: 'var(--text-main)' }}>
                <strong>Mô tả của khách:</strong> {c.description}
              </p>

              {c.status === 'Open' && (
                <div style={{ marginTop: '0.85rem', paddingTop: '0.85rem', borderTop: '1px solid var(--border-color)' }}>
                  {respondingId === c.id ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
                      <Textarea
                        rows={3}
                        placeholder="Nhập ghi chú giải thích hoặc đồng ý hoàn tiền..."
                        value={responseNote}
                        onChange={(e) => setResponseNote(e.target.value)}
                      />
                      <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => setRespondingId(null)}
                          disabled={actionLoading}
                        >
                          Hủy
                        </Button>
                        <Button
                          type="button"
                          variant="danger"
                          size="sm"
                          onClick={() => handleRespondComplaint(c.id, false)}
                          disabled={actionLoading}
                        >
                          Từ chối khiếu nại
                        </Button>
                        <Button
                          type="button"
                          variant="primary"
                          size="sm"
                          onClick={() => handleRespondComplaint(c.id, true)}
                          disabled={actionLoading}
                        >
                          Chấp thuận hoàn tiền
                        </Button>
                      </div>
                    </div>
                  ) : (
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      onClick={() => {
                        setRespondingId(c.id);
                        setResponseNote('');
                      }}
                    >
                      Xử lý khiếu nại này
                    </Button>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
