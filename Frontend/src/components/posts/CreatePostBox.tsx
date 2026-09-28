import React, { useRef, useState } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { postApi } from '@/api/postApi';
import type { PostDto } from '@/types/post';
import { getApiErrorMessage } from '@/utils/error';

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

    // Reset input để có thể chọn lại cùng 1 file
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

      // Tải hình ảnh lên Cloudinary
      if (selectedFiles.length > 0) {
        for (let i = 0; i < selectedFiles.length; i++) {
          setUploadProgress(`Đang tải ảnh ${i + 1}/${selectedFiles.length}...`);
          const res = await postApi.uploadMedia(selectedFiles[i]);
          uploadedMediaUrls.push(res.url);
        }
      }

      setUploadProgress('Đang xử lý nội dung & gửi vào hàng đợi AI...');
      const newPost = await postApi.createPost({
        content: content.trim(),
        mediaUrls: uploadedMediaUrls,
      });

      // Dọn dẹp trạng thái
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
    <div className="card create-post-card">
      <div className="create-post-header">
        <div className="avatar user-avatar">
          {user?.avatarUrl ? (
            <img src={user.avatarUrl} alt={user.displayName} />
          ) : (
            <span className="avatar-fallback">{user?.displayName?.charAt(0) || 'U'}</span>
          )}
        </div>
        <div className="user-info">
          <span className="user-name">{user?.displayName || 'Thành viên'}</span>
          <span className="post-privacy-badge">🌐 Công khai</span>
        </div>
      </div>

      <form onSubmit={handleSubmit} className="create-post-form">
        <textarea
          className="create-post-textarea"
          rows={3}
          placeholder="Bạn đang nghĩ gì hôm nay? Chia sẻ ý tưởng, công nghệ hoặc dự án..."
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
          <div className="media-preview-grid">
            {previewUrls.map((url, index) => (
              <div key={index} className="preview-item">
                <img src={url} alt={`preview-${index}`} />
                <button
                  type="button"
                  className="btn-remove-image"
                  onClick={() => handleRemoveImage(index)}
                  title="Xóa ảnh"
                  disabled={isSubmitting}
                >
                  ✕
                </button>
              </div>
            ))}
          </div>
        )}

        {/* Thông báo tiến trình hoặc lỗi */}
        {uploadProgress && (
          <div className="post-progress-status">
            <span className="spinner-sm"></span>
            <span>{uploadProgress}</span>
          </div>
        )}

        {error && <div className="alert alert-error create-post-error">{error}</div>}

        <div className="create-post-footer">
          <div className="create-post-actions">
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
              className="btn btn-outline btn-sm action-btn"
              onClick={() => fileInputRef.current?.click()}
              disabled={isSubmitting}
              id="attach-image-btn"
            >
              🖼️ Đính kèm ảnh {selectedFiles.length > 0 && `(${selectedFiles.length}/5)`}
            </button>
            <div className="ai-notice-badge" title="Bài viết sẽ được AI phân loại sau khi đăng">
              ✨ Gemini Auto-Topic
            </div>
          </div>

          <div className="create-post-submit">
            <span className="char-count">{content.length}/5000</span>
            <button
              type="submit"
              className="btn btn-primary btn-sm submit-post-btn"
              disabled={isSubmitting || (!content.trim() && selectedFiles.length === 0)}
              id="submit-post-btn"
            >
              {isSubmitting ? 'Đang đăng...' : 'Đăng bài 🚀'}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
};
