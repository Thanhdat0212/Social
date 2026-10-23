import React, { useRef, useState } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { postApi } from '@/api/postApi';
import type { PostDto } from '@/types/post';
import type { ProductListItem } from '@/types/marketplace';
import { getApiErrorMessage } from '@/utils/error';
import {
  Avatar,
  Button,
  Alert,
  ImageIcon,
  CloseIcon,
  SendIcon,
  TagIcon,
} from '@/components/common';
import { ProductPickerModal } from './ProductPickerModal';

const THUMB_FALLBACK_SVG =
  "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 32 32'%3E%3Crect width='32' height='32' fill='%23e2e8f0'/%3E%3Cpath d='M10 20l4-5 3 4 5-7 6 8H6z' fill='%2394a3b8'/%3E%3C/svg%3E";

interface CreatePostBoxProps {
  onPostCreated: (post: PostDto) => void;
}

export const CreatePostBox: React.FC<CreatePostBoxProps> = ({ onPostCreated }) => {
  const { user } = useAuth();
  const [content, setContent] = useState('');
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [previewUrls, setPreviewUrls] = useState<string[]>([]);
  const [taggedProducts, setTaggedProducts] = useState<ProductListItem[]>([]);
  const [isProductPickerOpen, setIsProductPickerOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isFocused, setIsFocused] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return;

    const files = Array.from(e.target.files);
    if (selectedFiles.length + files.length > 5) {
      setError('Bạn chỉ có thể đính kèm tối đa 5 hình ảnh mỗi bài viết.');
      return;
    }

    const validFiles: File[] = [];
    const validPreviews: string[] = [];

    for (const file of files) {
      if (file.size > 10 * 1024 * 1024) {
        setError(`Ảnh "${file.name}" vượt quá 10MB.`);
        continue;
      }
      validFiles.push(file);
      validPreviews.push(URL.createObjectURL(file));
    }

    setSelectedFiles((prev) => [...prev, ...validFiles]);
    setPreviewUrls((prev) => [...prev, ...validPreviews]);
    setError(null);

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleRemoveImage = (index: number) => {
    URL.revokeObjectURL(previewUrls[index]);
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
    setPreviewUrls((prev) => prev.filter((_, i) => i !== index));
  };

  const handleRemoveTaggedProduct = (productId: string) => {
    setTaggedProducts((prev) => prev.filter((p) => p.id !== productId));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim() && selectedFiles.length === 0 && taggedProducts.length === 0) {
      setError('Vui lòng nhập nội dung, đính kèm hình ảnh hoặc gắn thẻ sản phẩm.');
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setUploadProgress(selectedFiles.length > 0 ? 'Đang tải hình ảnh lên...' : 'Đang đăng bài...');

    try {
      const uploadedMediaUrls: string[] = [];

      if (selectedFiles.length > 0) {
        for (let i = 0; i < selectedFiles.length; i++) {
          setUploadProgress(`Đang tải ảnh ${i + 1}/${selectedFiles.length}...`);
          const res = await postApi.uploadMedia(selectedFiles[i]);
          uploadedMediaUrls.push(res.url);
        }
      }

      setUploadProgress('Đang xuất bản bài viết...');
      const newPost = await postApi.createPost({
        content: content.trim(),
        mediaUrls: uploadedMediaUrls,
        taggedProductIds: taggedProducts.map((p) => p.id),
      });

      setContent('');
      if (textareaRef.current) textareaRef.current.style.height = 'auto';
      setIsFocused(false);
      previewUrls.forEach((url) => URL.revokeObjectURL(url));
      setSelectedFiles([]);
      setPreviewUrls([]);
      setTaggedProducts([]);
      setUploadProgress(null);

      onPostCreated(newPost);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setIsSubmitting(false);
      setUploadProgress(null);
    }
  };

  const formatPrice = (price: number) => {
    return new Intl.NumberFormat('vi-VN').format(price) + ' ₫';
  };

  return (
    <div className="composer-card">
      <div className="composer-main">
        <Avatar
          src={user?.avatarUrl}
          name={user?.displayName}
          size="md"
          className="composer-avatar"
        />

        <form onSubmit={handleSubmit} className="composer-form">
          <textarea
            ref={textareaRef}
            className={`composer-textarea ${isFocused || content.length > 0 ? 'is-expanded' : ''}`}
            rows={isFocused || content.length > 0 ? 3 : 1}
            placeholder="Bạn đang nghĩ gì hôm nay? Chia sẻ ý tưởng, góc nhìn hoặc sản phẩm số..."
            value={content}
            onFocus={() => setIsFocused(true)}
            onBlur={() => setIsFocused(false)}
            onChange={(e) => {
              setContent(e.target.value);
              if (error) setError(null);
              if (textareaRef.current) {
                textareaRef.current.style.height = 'auto';
                textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, 260)}px`;
              }
            }}
            disabled={isSubmitting}
            maxLength={5000}
          />

          {/* Xem trước ảnh đính kèm */}
          {previewUrls.length > 0 && (
            <div className={`composer-previews-grid count-${Math.min(previewUrls.length, 5)}`}>
              {previewUrls.map((url, index) => (
                <div key={index} className="composer-preview-item">
                  <img src={url} alt={`preview-${index}`} />
                  <button
                    type="button"
                    className="btn-remove-preview"
                    onClick={() => handleRemoveImage(index)}
                    title="Xóa ảnh này"
                    disabled={isSubmitting}
                    aria-label="Xóa ảnh"
                  >
                    <CloseIcon size={12} />
                  </button>
                </div>
              ))}
            </div>
          )}

          {/* Danh sách sản phẩm được gắn thẻ */}
          {taggedProducts.length > 0 && (
            <div
              style={{
                display: 'flex',
                flexWrap: 'wrap',
                gap: '0.5rem',
                margin: '0.5rem 0',
                padding: '0.5rem',
                borderRadius: '8px',
                background: 'rgba(24, 119, 242, 0.04)',
                border: '1px solid rgba(24, 119, 242, 0.15)',
              }}
            >
              <div
                style={{
                  width: '100%',
                  fontSize: '0.8rem',
                  fontWeight: 600,
                  color: 'var(--primary-color, #1877f2)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.35rem',
                }}
              >
                <span style={{ display: 'inline-flex', alignItems: 'center', gap: '0.35rem' }}>
                  <TagIcon size={14} />
                  <span>Sản phẩm đính kèm ({taggedProducts.length}/3):</span>
                </span>
              </div>
              {taggedProducts.map((p) => (
                <div
                  key={p.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.5rem',
                    padding: '0.35rem 0.6rem',
                    borderRadius: '20px',
                    background: 'var(--bg-surface)',
                    border: '1px solid var(--border-color)',
                    boxShadow: 'var(--shadow-sm)',
                  }}
                >
                  <img
                    src={p.coverImageUrl || THUMB_FALLBACK_SVG}
                    alt={p.name}
                    style={{ width: '22px', height: '22px', borderRadius: '4px', objectFit: 'cover' }}
                    onError={(e) => {
                      const target = e.currentTarget;
                      if (!target.dataset.hasFallback) {
                        target.dataset.hasFallback = 'true';
                        target.src = THUMB_FALLBACK_SVG;
                      }
                    }}
                  />
                  <span
                    style={{
                      fontSize: '0.825rem',
                      fontWeight: 600,
                      maxWidth: '140px',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {p.name}
                  </span>
                  <span style={{ fontSize: '0.8rem', color: 'var(--primary-color, #1877f2)', fontWeight: 600 }}>
                    {formatPrice(p.minPrice)}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleRemoveTaggedProduct(p.id)}
                    style={{
                      background: 'none',
                      border: 'none',
                      cursor: 'pointer',
                      padding: 0,
                      display: 'flex',
                      color: 'var(--text-secondary, #65676b)',
                    }}
                    title="Gỡ sản phẩm"
                  >
                    <CloseIcon size={12} />
                  </button>
                </div>
              ))}
            </div>
          )}

          {/* Trạng thái tải lên hoặc lỗi */}
          {uploadProgress && (
            <div className="composer-progress">
              <span className="spinner-inline" />
              <span>{uploadProgress}</span>
            </div>
          )}

          {error && <Alert type="error" message={error} className="composer-alert" />}

          {/* Footer toolbar */}
          <div className="composer-footer">
            <div className="composer-tools">
              <input
                type="file"
                ref={fileInputRef}
                onChange={handleFileChange}
                accept="image/jpeg,image/png,image/webp,image/gif"
                multiple
                style={{ display: 'none' }}
                disabled={isSubmitting}
              />
              <button
                type="button"
                className="composer-tool-btn"
                onClick={() => fileInputRef.current?.click()}
                disabled={isSubmitting}
                id="attach-image-btn"
                title="Đính kèm tối đa 5 hình ảnh"
              >
                <ImageIcon size={18} />
                <span>Ảnh {selectedFiles.length > 0 && `(${selectedFiles.length}/5)`}</span>
              </button>

              <button
                type="button"
                className={`composer-tool-btn ${taggedProducts.length > 0 ? 'active' : ''}`}
                onClick={() => setIsProductPickerOpen(true)}
                disabled={isSubmitting}
                id="attach-product-btn"
                title="Gắn thẻ sản phẩm số từ chợ"
                style={{
                  color: taggedProducts.length > 0 ? 'var(--accent-primary)' : undefined,
                  fontWeight: taggedProducts.length > 0 ? 600 : undefined,
                }}
              >
                <TagIcon size={16} />
                <span>Sản phẩm {taggedProducts.length > 0 && `(${taggedProducts.length}/3)`}</span>
              </button>
            </div>

            <div className="composer-submit-group">
              <span className="composer-counter">{content.length}/5000</span>
              <Button
                type="submit"
                variant="primary"
                size="sm"
                disabled={
                  isSubmitting ||
                  (!content.trim() && selectedFiles.length === 0 && taggedProducts.length === 0)
                }
                loading={isSubmitting}
                iconRight={<SendIcon size={14} />}
                id="submit-post-btn"
              >
                Đăng bài
              </Button>
            </div>
          </div>
        </form>
      </div>

      {/* Modal chọn sản phẩm */}
      <ProductPickerModal
        isOpen={isProductPickerOpen}
        onClose={() => setIsProductPickerOpen(false)}
        selectedProducts={taggedProducts}
        onSelectProducts={setTaggedProducts}
      />
    </div>
  );
};
