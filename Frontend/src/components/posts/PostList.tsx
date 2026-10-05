import React from 'react';
import type { PostDto } from '@/types/post';
import { PostCard } from './PostCard';
import { Button, RefreshIcon, SparklesIcon } from '@/components/common';

interface PostListProps {
  posts: PostDto[];
  loading: boolean;
  error?: string | null;
  hasMore?: boolean;
  loadingMore?: boolean;
  sentinelRef?: React.RefCallback<HTMLDivElement>;
  onRefresh?: () => void;
  onPostDeleted?: (postId: string) => void;
}

export const PostList: React.FC<PostListProps> = ({
  posts,
  loading,
  error,
  hasMore = false,
  loadingMore = false,
  sentinelRef,
  onRefresh,
  onPostDeleted,
}) => {
  // Skeleton Loading Initial State
  if (loading && posts.length === 0) {
    return (
      <div className="posts-skeleton-stack" aria-busy="true" aria-label="Đang tải bài viết">
        {[1, 2, 3].map((i) => (
          <div key={i} className="post-skeleton-item">
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
        ))}
      </div>
    );
  }

  // Error State
  if (error && posts.length === 0) {
    return (
      <div className="feed-status-card error-card">
        <h3>Không thể tải bài viết</h3>
        <p>{error}</p>
        {onRefresh && (
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={onRefresh}
            icon={<RefreshIcon size={14} />}
          >
            Thử lại
          </Button>
        )}
      </div>
    );
  }

  // Empty State
  if (posts.length === 0) {
    return (
      <div className="feed-status-card empty-card">
        <div className="empty-sparkle-wrap">
          <SparklesIcon size={32} />
        </div>
        <h3>Chưa có bài viết nào</h3>
        <p>
          Hãy là người đầu tiên chia sẻ cảm nghĩ, công nghệ hoặc dự án của bạn để bắt đầu thảo luận!
        </p>
        {onRefresh && (
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={onRefresh}
            icon={<RefreshIcon size={14} />}
          >
            Làm mới bảng tin
          </Button>
        )}
      </div>
    );
  }

  return (
    <div className="post-list-wrapper">
      <div className="posts-stream">
        {posts.map((post) => (
          <PostCard
            key={post.id}
            post={post}
            onPostDeleted={onPostDeleted}
          />
        ))}

        {/* Sentinel for Infinite Scroll */}
        {hasMore && sentinelRef && (
          <div ref={sentinelRef} className="infinite-scroll-sentinel" />
        )}

        {/* Loading more spinner at bottom */}
        {loadingMore && (
          <div className="feed-loading-more">
            <span className="spinner-inline" />
            <span>Đang tải thêm bài viết...</span>
          </div>
        )}

        {/* Caught up message */}
        {!hasMore && posts.length > 0 && (
          <div className="feed-end-banner">
            <div className="end-banner-dot" />
            <p className="end-banner-title">Bạn đã xem hết bài viết mới nhất</p>
            <span className="end-banner-subtitle">
              Bài viết mới sẽ tự động hiển thị khi cộng đồng đăng tải
            </span>
          </div>
        )}
      </div>
    </div>
  );
};
