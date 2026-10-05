import React, { useEffect, useState } from 'react';
import { usePostModalStore } from '@/store';
import { postApi } from '@/api/postApi';
import type { PostDto } from '@/types';
import { PostCard } from './PostCard';
import { CloseIcon, Button, RefreshIcon } from '@/components/common';
import { getApiErrorMessage } from '@/utils/error';

export const PostDetailModal: React.FC = () => {
  const { activePostId, closePostModal } = usePostModalStore();

  const [post, setPost] = useState<PostDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!activePostId) {
      setPost(null);
      setError(null);
      return;
    }

    let isMounted = true;
    const fetchPost = async () => {
      setLoading(true);
      setError(null);

      try {
        const data = await postApi.getPostById(activePostId);
        if (isMounted) {
          setPost(data);
        }
      } catch (err) {
        if (isMounted) {
          setError(getApiErrorMessage(err));
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchPost();

    // Khóa cuộn trang nền khi mở modal
    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';

    // Đóng bằng phím Escape
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        closePostModal();
      }
    };
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      isMounted = false;
      document.body.style.overflow = originalOverflow;
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [activePostId, closePostModal]);

  if (!activePostId) return null;

  const handlePostDeleted = () => {
    closePostModal();
  };

  return (
    <div
      className="post-modal-overlay"
      onClick={closePostModal}
      role="dialog"
      aria-modal="true"
      aria-label="Chi tiết bài viết"
    >
      <div className="post-modal-container" onClick={(e) => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="post-modal-header">
          <div className="post-modal-title-col">
            <h2 className="post-modal-title">Chi tiết bài viết</h2>
          </div>
          <button
            type="button"
            className="post-modal-close-btn"
            onClick={closePostModal}
            aria-label="Đóng"
            title="Đóng (Esc)"
          >
            <CloseIcon size={20} />
          </button>
        </div>

        {/* Modal Body */}
        <div className="post-modal-body">
          {loading && (
            <div className="posts-skeleton-stack" aria-busy="true">
              <div className="post-skeleton-item">
                <div className="skeleton-header-row">
                  <div className="skeleton-avatar-circle skeleton-shimmer" />
                  <div className="skeleton-meta-col">
                    <div className="skeleton-bar bar-author skeleton-shimmer" />
                    <div className="skeleton-bar bar-time skeleton-shimmer" />
                  </div>
                </div>
                <div className="skeleton-body-block">
                  <div className="skeleton-bar bar-full skeleton-shimmer" />
                  <div className="skeleton-bar bar-mid skeleton-shimmer" />
                </div>
              </div>
            </div>
          )}

          {!loading && error && (
            <div className="feed-status-card error-card">
              <h3>Không tìm thấy bài viết</h3>
              <p>{error || 'Bài viết này có thể đã bị xóa hoặc bạn không có quyền xem.'}</p>
              <div className="post-detail-error-actions">
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  onClick={closePostModal}
                >
                  Đóng
                </Button>
              </div>
            </div>
          )}

          {!loading && post && (
            <div className="post-modal-card-wrap">
              <PostCard
                post={post}
                initialShowComments={true}
                onPostDeleted={handlePostDeleted}
              />
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
