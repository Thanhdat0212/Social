import React from 'react';
import type { PostDto } from '@/types/post';
import { PostCard } from './PostCard';

interface PostListProps {
  posts: PostDto[];
  loading: boolean;
  error?: string | null;
  onRefresh?: () => void;
  onPostDeleted?: (postId: string) => void;
}

export const PostList: React.FC<PostListProps> = ({
  posts,
  loading,
  error,
  onRefresh,
  onPostDeleted,
}) => {
  if (loading && posts.length === 0) {
    return (
      <div className="posts-skeleton-container">
        {[1, 2, 3].map((i) => (
          <div key={i} className="card skeleton-post-card">
            <div className="skeleton-header">
              <div className="skeleton-avatar skeleton-pulse"></div>
              <div className="skeleton-lines">
                <div className="skeleton-line skeleton-pulse line-short"></div>
                <div className="skeleton-line skeleton-pulse line-tiny"></div>
              </div>
            </div>
            <div className="skeleton-body">
              <div className="skeleton-line skeleton-pulse"></div>
              <div className="skeleton-line skeleton-pulse line-medium"></div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (error && posts.length === 0) {
    return (
      <div className="card empty-feed-card error-card">
        <div className="empty-icon">⚠️</div>
        <h3>Không thể tải bài viết</h3>
        <p className="text-muted">{error}</p>
        {onRefresh && (
          <button type="button" className="btn btn-outline btn-sm" onClick={onRefresh}>
            🔄 Thử lại
          </button>
        )}
      </div>
    );
  }

  if (posts.length === 0) {
    return (
      <div className="card empty-feed-card">
        <div className="empty-icon">📝</div>
        <h3>Chưa có bài viết nào trên bảng tin</h3>
        <p className="text-muted">
          Hãy là người đầu tiên chia sẻ cảm nghĩ, công nghệ hoặc dự án của bạn để hệ thống AI phân tích và gợi ý đến mọi người!
        </p>
        {onRefresh && (
          <button type="button" className="btn btn-outline btn-sm" onClick={onRefresh}>
            🔄 Làm mới bảng tin
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="post-list-wrapper">
      <div className="feed-header-bar">
        <h2 className="feed-title">📰 Bảng tin mới nhất</h2>
        {onRefresh && (
          <button
            type="button"
            className="btn btn-outline btn-sm btn-refresh-feed"
            onClick={onRefresh}
            disabled={loading}
            title="Làm mới để cập nhật chủ đề AI mới nhất"
          >
            {loading ? 'Đang cập nhật...' : '🔄 Làm mới bảng tin'}
          </button>
        )}
      </div>

      <div className="posts-container">
        {posts.map((post) => (
          <PostCard
            key={post.id}
            post={post}
            onPostDeleted={onPostDeleted}
          />
        ))}
      </div>
    </div>
  );
};
