import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTitle } from '@/hooks/useTitle';
import { postApi } from '@/api/postApi';
import type { PostDto } from '@/types';
import { PostCard } from '@/components/posts/PostCard';
import { Button, RefreshIcon } from '@/components/common';
import { getApiErrorMessage } from '@/utils/error';
import { ROUTES } from '@/constants/routes';

export const PostDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  useTitle('Chi tiết bài viết');

  const [post, setPost] = useState<PostDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    const fetchPost = async () => {
      if (!id) return;
      setLoading(true);
      setError(null);

      try {
        const data = await postApi.getPostById(id);
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
    return () => {
      isMounted = false;
    };
  }, [id]);

  const handlePostDeleted = () => {
    navigate(ROUTES.HOME, { replace: true });
  };

  return (
    <div className="container post-detail-container">
      {/* Back button toolbar */}
      <div className="post-detail-toolbar">
        <button
          type="button"
          className="btn-back-link"
          onClick={() => {
            if (window.history.length > 1) {
              navigate(-1);
            } else {
              navigate(ROUTES.HOME);
            }
          }}
          aria-label="Quay lại"
        >
          <span className="arrow-left">←</span>
          <span>Quay lại</span>
        </button>
      </div>

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
          <p>{error || 'Bài viết này có thể đã bị xóa hoặc không tồn tại.'}</p>
          <div className="post-detail-error-actions">
            <Button
              type="button"
              variant="primary"
              size="sm"
              onClick={() => navigate(ROUTES.HOME)}
            >
              Về trang chủ
            </Button>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => window.location.reload()}
              icon={<RefreshIcon size={14} />}
            >
              Tải lại
            </Button>
          </div>
        </div>
      )}

      {!loading && post && (
        <div className="post-detail-card-wrap">
          <PostCard
            post={post}
            onPostDeleted={handlePostDeleted}
            initialShowComments={true}
          />
        </div>
      )}
    </div>
  );
};
