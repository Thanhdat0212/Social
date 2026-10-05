import React, { useRef, useState } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { postApi } from '@/api/postApi';
import type { PostDto } from '@/types/post';
import { getApiErrorMessage } from '@/utils/error';
import {
  Avatar,
  Button,
  Alert,
  ImageIcon,
  CloseIcon,
  SendIcon,
} from '@/components/common';

interface CreatePostBoxProps {
  onPostCreated: (post: PostDto) => void;
}

export const CreatePostBox: React.FC<CreatePostBoxProps> = ({ onPostCreated }) => {
  const { user } = useAuth();
  const [content, setContent] = useState('');
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [previewUrls, setPreviewUrls] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);

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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim() && selectedFiles.length === 0) {
      setError('Vui lòng nhập nội dung hoặc đính kèm hình ảnh.');
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
      });

      setContent('');
      previewUrls.forEach((url) => URL.revokeObjectURL(url));
      setSelectedFiles([]);
      setPreviewUrls([]);
      setUploadProgress(null);

      onPostCreated(newPost);
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setIsSubmitting(false);
      setUploadProgress(null);
    }
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
            className="composer-textarea"
            rows={3}
            placeholder="Bạn đang nghĩ gì hôm nay? Chia sẻ ý tưởng, góc nhìn hoặc kiến thức..."
            value={content}
            onChange={(e) => {
              setContent(e.target.value);
              if (error) setError(null);
            }}
            disabled={isSubmitting}
            maxLength={5000}
          />

          {/* Xem trước ảnh đính kèm */}
          {previewUrls.length > 0 && (
            <div className="composer-previews-grid count-${Math.min(previewUrls.length, 5)}">
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
            </div>

            <div className="composer-submit-group">
              <span className="composer-counter">{content.length}/5000</span>
              <Button
                type="submit"
                variant="primary"
                size="sm"
                disabled={isSubmitting || (!content.trim() && selectedFiles.length === 0)}
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
    </div>
  );
};
