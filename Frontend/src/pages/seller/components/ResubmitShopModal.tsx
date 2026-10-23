import React, { useState, useEffect } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import { FormField, Textarea, Button } from '@/components/common';

interface ResubmitShopModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
}

export const ResubmitShopModal: React.FC<ResubmitShopModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
}) => {
  const [resubmitNotes, setResubmitNotes] = useState('');
  const [resubmitLoading, setResubmitLoading] = useState(false);

  useEffect(() => {
    if (!isOpen) return;
    document.body.classList.add('modal-open');
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.body.classList.remove('modal-open');
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const handleResubmit = async () => {
    setResubmitLoading(true);
    try {
      await marketplaceApi.resubmitMyShop({ notes: resubmitNotes.trim() });
      alert('Đã nộp lại hồ sơ xét duyệt tới Quản trị viên!');
      onClose();
      onSuccess();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Không thể nộp lại hồ sơ xét duyệt.');
    } finally {
      setResubmitLoading(false);
    }
  };

  return (
    <div
      className="modal-overlay"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
    >
      <div
        className="modal-content"
        style={{
          maxWidth: '500px',
          width: '100%',
          padding: '1.75rem',
        }}
        onClick={(e) => e.stopPropagation()}
      >
        <h3 style={{ fontSize: '1.2rem', fontWeight: 800, color: 'var(--text-main)', marginBottom: '0.5rem' }}>
          Nộp lại hồ sơ xét duyệt gian hàng
        </h3>
        <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginBottom: '1rem' }}>
          Giải trình việc khắc phục các thiếu sót mà Quản trị viên đã nêu trong lần xét duyệt trước.
        </p>

        <FormField label="Ghi chú giải trình" required>
          <Textarea
            rows={4}
            placeholder="Tôi đã cập nhật chính sách bảo hành rõ ràng và cam kết tuân thủ..."
            value={resubmitNotes}
            onChange={(e) => setResubmitNotes(e.target.value)}
          />
        </FormField>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.75rem', marginTop: '1.25rem' }}>
          <Button variant="ghost" size="sm" onClick={onClose} disabled={resubmitLoading}>
            Đóng
          </Button>
          <Button
            variant="danger"
            size="sm"
            onClick={handleResubmit}
            loading={resubmitLoading}
            disabled={resubmitLoading || !resubmitNotes.trim()}
          >
            Xác nhận nộp lại
          </Button>
        </div>
      </div>
    </div>
  );
};
