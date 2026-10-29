import React from 'react';
import type { PostDto } from '@/types/post';
import { PostCard } from './PostCard';
import {
  Button,
  RefreshIcon,
  PostCardSkeleton,
  ErrorState,
  EmptyState,
} from '@/components/common';

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
  // Skeleton Loading Initial State (2 bài đầu tiên theo micro-batching)
  if (loading && posts.length === 0) {
    return (
      <div className="posts-skeleton-stack" aria-busy="true" aria-label="Đang tải bài viết" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {[1, 2].map((i) => (
          <PostCardSkeleton key={i} />
        ))}
      </div>
    );
  }

  // Error State
  if (error && posts.length === 0) {
    return (
      <ErrorState
        title="Không thể tải bài viết"
        message={error}
        onRetry={onRefresh}
        retryText="Thử lại ngay"
      />
    );
  }

  // Empty State
  if (posts.length === 0) {
    return (
      <EmptyState
        title="Chưa có bài viết nào"
        description="Hãy là người đầu tiên chia sẻ cảm nghĩ, công nghệ hoặc dự án của bạn để bắt đầu thảo luận!"
        action={
          onRefresh ? (
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={onRefresh}
              icon={<RefreshIcon size={14} />}
            >
              Làm mới bảng tin
            </Button>
          ) : undefined
        }
      />
    );
  }

  return (
    <div className="post-list-wrapper">
      <div className="posts-stream">
        {posts.map((post, idx) => (
          <PostCard
            key={post.id}
            post={post}
            priority={idx === 0}
            onPostDeleted={onPostDeleted}
          />
        ))}

        {/* Sentinel for Infinite Scroll */}
        {hasMore && sentinelRef && (
          <div ref={sentinelRef} className="infinite-scroll-sentinel" />
        )}

        {/* Loading more skeleton at bottom */}
        {loadingMore && (
          <div className="feed-loading-more-wrapper" style={{ marginTop: '0.75rem' }} aria-busy="true">
            <PostCardSkeleton />
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
