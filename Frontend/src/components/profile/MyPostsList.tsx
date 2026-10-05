import React, { useCallback, useEffect, useState } from 'react';
import type { PostDto } from '@/types/post';
import { postApi } from '@/api/postApi';
import { useInfiniteScroll } from '@/hooks/useInfiniteScroll';
import { useSignalR } from '@/hooks/useSignalR';
import { PostCard } from '@/components/posts/PostCard';
import { CreatePostBox } from '@/components/posts/CreatePostBox';
import {
  Button,
  FileTextIcon,
  RefreshIcon,
  SparklesIcon,
} from '@/components/common';
import { getApiErrorMessage } from '@/utils/error';

const PAGE_SIZE = 15;

interface MyPostsListProps {
  onPostCountChange?: (delta: number) => void;
}

export const MyPostsList: React.FC<MyPostsListProps> = ({
  onPostCountChange,
}) => {
  const [posts, setPosts] = useState<PostDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [showCreateBox, setShowCreateBox] = useState(false);

  // Fetch initial posts
  const fetchMyPosts = useCallback(async () => {
    setLoading(true);
    setError(null);
    setPage(1);
    setHasMore(true);

    try {
      const data = await postApi.getMyPosts(1, PAGE_SIZE);
      setPosts(data);
      if (data.length < PAGE_SIZE) {
        setHasMore(false);
      }
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchMyPosts();
  }, [fetchMyPosts]);

  // Load more posts on scroll
  const handleLoadMore = useCallback(async () => {
    if (loading || loadingMore || !hasMore) return;

    setLoadingMore(true);
    const nextPage = page + 1;

    try {
      const nextBatch = await postApi.getMyPosts(nextPage, PAGE_SIZE);
      if (nextBatch.length > 0) {
        setPosts((prev) => {
          const existingIds = new Set(prev.map((p) => p.id));
          const unique = nextBatch.filter((p) => !existingIds.has(p.id));
          return [...prev, ...unique];
        });
        setPage(nextPage);
      }
      if (nextBatch.length < PAGE_SIZE) {
        setHasMore(false);
      }
    } catch {
      setHasMore(false);
    } finally {
      setLoadingMore(false);
    }
  }, [loading, loadingMore, hasMore, page]);

  const { sentinelRef } = useInfiniteScroll({
    onLoadMore: handleLoadMore,
    hasMore,
    isLoading: loading || loadingMore,
  });

  // Handle post deletion
  const handlePostDeleted = useCallback(
    (deletedPostId: string) => {
      setPosts((prev) => prev.filter((p) => p.id !== deletedPostId));
      onPostCountChange?.(-1);
    },
    [onPostCountChange]
  );

  // Listen to realtime post deleted
  useSignalR<string>('PostDeleted', (deletedPostId) => {
    handlePostDeleted(deletedPostId);
  });

  // Handle post created
  const handlePostCreated = (newPost: PostDto) => {
    setPosts((prev) => [newPost, ...prev]);
    setShowCreateBox(false);
    onPostCountChange?.(1);
  };

  return (
    <div className="my-posts-container">
      {/* Quick Action Header */}
      <div className="my-posts-header-row">
        <div className="my-posts-header-info">
          <h3>Bài viết đã đăng</h3>
          <p>Quản lý và xem lại tất cả bài viết bạn đã chia sẻ với cộng đồng</p>
        </div>
        <div className="my-posts-header-actions">
          <Button
            type="button"
            variant={showCreateBox ? 'ghost' : 'primary'}
            size="sm"
            onClick={() => setShowCreateBox((prev) => !prev)}
            icon={<SparklesIcon size={15} />}
          >
            {showCreateBox ? 'Ẩn khung soạn' : 'Viết bài mới'}
          </Button>
        </div>
      </div>

      {/* Optional In-profile Post Creation Box */}
      {showCreateBox && (
        <div className="my-posts-create-wrapper">
          <CreatePostBox onPostCreated={handlePostCreated} />
        </div>
      )}

      {/* Skeletons on initial loading */}
      {loading && posts.length === 0 && (
        <div className="posts-skeleton-stack" aria-busy="true">
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
      )}

      {/* Error state */}
      {!loading && error && posts.length === 0 && (
        <div className="feed-status-card error-card">
          <h3>Không thể tải bài viết</h3>
          <p>{error}</p>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={fetchMyPosts}
            icon={<RefreshIcon size={14} />}
          >
            Thử lại
          </Button>
        </div>
      )}

      {/* Empty state */}
      {!loading && !error && posts.length === 0 && (
        <div className="my-posts-empty-card">
          <div className="empty-icon-wrap">
            <FileTextIcon size={36} />
          </div>
          <h4>Bạn chưa có bài viết nào</h4>
          <p>
            Chia sẻ ý tưởng, chia sẻ kinh nghiệm lập trình hoặc đặt câu hỏi để kết nối với mọi người.
          </p>
          {!showCreateBox && (
            <Button
              type="button"
              variant="primary"
              size="md"
              onClick={() => setShowCreateBox(true)}
              icon={<SparklesIcon size={16} />}
            >
              Tạo bài viết đầu tiên
            </Button>
          )}
        </div>
      )}

      {/* Posts Stream */}
      {posts.length > 0 && (
        <div className="my-posts-stream">
          {posts.map((post) => (
            <PostCard
              key={post.id}
              post={post}
              onPostDeleted={handlePostDeleted}
            />
          ))}

          {/* Sentinel for Infinite Scroll */}
          {hasMore && (
            <div ref={sentinelRef} className="infinite-scroll-sentinel" />
          )}

          {/* Loading more indicator */}
          {loadingMore && (
            <div className="feed-loading-more">
              <span className="spinner-inline" />
              <span>Đang tải thêm bài viết...</span>
            </div>
          )}

          {/* End of posts banner */}
          {!hasMore && (
            <div className="feed-end-banner">
              <div className="end-banner-dot" />
              <p className="end-banner-title">Đã hiển thị tất cả bài viết của bạn</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
