import React, { useState } from 'react';
import { marketplaceApi } from '@/api/marketplaceApi';
import { Button, FormField, Textarea } from '@/components/common';

interface OrderReviewModalProps {
  item: {
    itemId: string;
    prodName: string;
  } | null;
  onClose: () => void;
  onSuccess: (itemId: string) => void;
}

export const OrderReviewModal: React.FC<OrderReviewModalProps> = ({
  item,
  onClose,
  onSuccess,
}) => {
  const [rating, setRating] = useState(5);
  const [hoverRating, setHoverRating] = useState(0);
  const [comment, setComment] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; msg: string } | null>(null);

  if (!item) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setFeedback(null);
    try {
      await marketplaceApi.createProductReview(item.itemId, {
        orderItemId: item.itemId,
        rating,
        comment,
      });
      setFeedback({
        type: 'success',
        msg: 'Cảm ơn bạn! Đánh giá của bạn đã được ghi nhận thành công.',
      });
      setTimeout(() => {
        onSuccess(item.itemId);
        onClose();
      }, 1200);
    } catch (err: any) {
      setFeedback({
        type: 'error',
        msg: err?.response?.data?.message || 'Không thể gửi đánh giá. Vui lòng thử lại.',
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
          <span style={{ fontSize: '1.4rem' }}>⭐</span>
          <h3 style={{ fontSize: '1.15rem', fontWeight: 800, color: 'var(--text-main)', margin: 0 }}>
            Đánh giá sản phẩm
          </h3>
        </div>

        <p style={{ fontSize: '0.86rem', color: 'var(--text-muted)', marginBottom: '1.25rem', lineHeight: '1.5' }}>
          Mặt hàng: <strong style={{ color: 'var(--text-main)' }}>{item.prodName}</strong>. Nhận xét chân thực của bạn sẽ giúp những người mua khác lựa chọn được sản phẩm phù hợp.
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
          {/* Interactive Star Rating */}
          <div style={{ marginBottom: '1.25rem', textAlign: 'center' }}>
            <label style={{ display: 'block', fontSize: '0.88rem', fontWeight: 600, color: 'var(--text-main)', marginBottom: '0.5rem' }}>
              Chất lượng sản phẩm & dịch vụ
            </label>
            <div style={{ display: 'inline-flex', gap: '0.5rem', fontSize: '2.2rem', cursor: 'pointer' }}>
              {[1, 2, 3, 4, 5].map((star) => (
                <span
                  key={star}
                  style={{
                    color: star <= (hoverRating || rating) ? '#f59e0b' : 'var(--border-color)',
                    transition: 'transform 0.1s ease, color 0.1s ease',
                  }}
                  onMouseEnter={() => setHoverRating(star)}
                  onMouseLeave={() => setHoverRating(0)}
                  onClick={() => setRating(star)}
                >
                  ★
                </span>
              ))}
            </div>
            <div style={{ fontSize: '0.82rem', color: '#f59e0b', fontWeight: 600, marginTop: '0.25rem' }}>
              {rating === 5 && 'Tuyệt vời, cực kỳ hài lòng!'}
              {rating === 4 && 'Hài lòng, sản phẩm tốt.'}
              {rating === 3 && 'Bình thường, dùng tạm được.'}
              {rating === 2 && 'Không hài lòng, cần cải thiện.'}
              {rating === 1 && 'Rất tệ, trải nghiệm thất vọng.'}
            </div>
          </div>

          <FormField
            id="review-comment-input"
            label="Cảm nhận chi tiết của bạn"
            required
            hint="Chia sẻ về độ chính xác của thông tin, tốc độ giao hàng, thái độ hỗ trợ..."
          >
            <Textarea
              id="review-comment-input"
              rows={4}
              placeholder="Mã kích hoạt nhanh chóng, hoạt động tốt đúng như mô tả. Shop hỗ trợ nhiệt tình..."
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              required
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
              style={{ background: '#f59e0b', borderColor: '#f59e0b', color: '#000', fontWeight: 700 }}
            >
              Gửi đánh giá
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
