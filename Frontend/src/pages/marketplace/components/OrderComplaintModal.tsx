import React, { useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import { Button, FormField, Input, Select, Textarea, AlertCircleIcon } from '@/components/common';

interface OrderComplaintModalProps {
  orderId: string;
  item: {
    itemId: string;
    prodName: string;
  } | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const OrderComplaintModal: React.FC<OrderComplaintModalProps> = ({
  orderId,
  item,
  onClose,
  onSuccess,
}) => {
  const [reason, setReason] = useState(1);
  const [description, setDescription] = useState('');
  const [evidenceUrl, setEvidenceUrl] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);

  React.useEffect(() => {
    if (!item) return;

    document.body.classList.add('modal-open');
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.body.classList.remove('modal-open');
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [item, onClose]);

  if (!item) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!description.trim()) return;

    setIsSubmitting(true);
    setFeedback(null);
    try {
      const evidenceList = evidenceUrl.trim() ? [evidenceUrl.trim()] : [];
      await marketplaceApi.createComplaint(orderId, item.itemId, {
        reason,
        description: description.trim(),
        faultyQuantity: 1,
        evidenceUrls: evidenceList,
      });
      setFeedback({
        type: 'success',
        msg: 'Đã gửi yêu cầu khiếu nại tới Gian hàng thành công. Gian hàng có 24h để phản hồi giải quyết.',
      });
      setTimeout(() => {
        onSuccess();
        onClose();
      }, 1500);
    } catch (err: any) {
      setFeedback({
        type: 'error',
        msg: err?.response?.data?.message || 'Không thể gửi khiếu nại. Vui lòng thử lại sau.',
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop" style={{ zIndex: 1000 }}>
      <div
        className="modal-card"
        style={{
          maxWidth: '520px',
          width: '100%',
          background: 'var(--bg-card)',
          border: '1px solid var(--border-color)',
          borderRadius: '16px',
          padding: '1.75rem',
          boxShadow: '0 20px 48px rgba(0, 0, 0, 0.5)',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', marginBottom: '1rem' }}>
          <AlertCircleIcon size={22} style={{ color: '#f87171' }} />
          <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
            Khiếu nại sản phẩm lỗi
          </h3>
        </div>

        <p style={{ fontSize: '0.86rem', color: 'var(--text-muted)', marginBottom: '1.25rem', lineHeight: '1.5' }}>
          Mặt hàng: <strong style={{ color: 'var(--text-main)' }}>{item.prodName}</strong>. Gian hàng sẽ nhận được thông báo và có 24 giờ để cấp mã thay thế hoặc hoàn tiền.
        </p>

        {feedback && (
          <div
            style={{
              background:
                feedback.type === 'success'
                  ? 'rgba(16, 185, 129, 0.15)'
                  : 'rgba(239, 68, 68, 0.15)',
              border:
                feedback.type === 'success'
                  ? '1px solid rgba(16, 185, 129, 0.4)'
                  : '1px solid rgba(239, 68, 68, 0.4)',
              color: feedback.type === 'success' ? '#34d399' : '#f87171',
              padding: '0.75rem 1rem',
              borderRadius: '10px',
              marginBottom: '1rem',
              fontSize: '0.85rem',
            }}
          >
            {feedback.msg}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <FormField id="complaint-reason-select" label="Lý do khiếu nại" required>
            <Select
              id="complaint-reason-select"
              value={reason}
              onChange={(e) => setReason(Number(e.target.value))}
            >
              <option value={1}>Mã bản quyền không hoạt động / Báo lỗi kích hoạt</option>
              <option value={2}>Mã đã bị người khác sử dụng trước đó</option>
              <option value={3}>Tài khoản không đăng nhập được / Sai mật khẩu</option>
              <option value={4}>Mô tả sản phẩm không đúng với gói đã nhận</option>
              <option value={5}>Tệp tin tải về bị hỏng / Không thể giải nén</option>
            </Select>
          </FormField>

          <FormField
            id="complaint-desc-input"
            label="Mô tả chi tiết tình trạng lỗi"
            required
            hint="Mô tả rõ mã lỗi phần mềm hoặc tình trạng gặp phải."
          >
            <Textarea
              id="complaint-desc-input"
              rows={3}
              placeholder="Ví dụ: Nhập key vào phần mềm báo 'Code already redeemed' lúc 14:00..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              required
            />
          </FormField>

          <FormField
            id="complaint-evidence-input"
            label="Đường dẫn ảnh/video bằng chứng (nếu có)"
            hint="Dán link ảnh chụp màn hình lỗi (Imgur, Cloudinary, Drive...)"
          >
            <Input
              id="complaint-evidence-input"
              placeholder="https://..."
              value={evidenceUrl}
              onChange={(e) => setEvidenceUrl(e.target.value)}
            />
          </FormField>

          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.5rem' }}>
            <Button
              type="button"
              variant="ghost"
              onClick={onClose}
              disabled={isSubmitting}
            >
              Hủy bỏ
            </Button>
            <Button
              type="submit"
              variant="primary"
              loading={isSubmitting}
              style={{ background: '#ef4444', borderColor: '#ef4444' }}
            >
              Gửi khiếu nại
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
