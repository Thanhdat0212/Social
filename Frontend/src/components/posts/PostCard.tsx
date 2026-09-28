import React, { useEffect, useRef, useState } from 'react';
import type { PostDto } from '@/types';
import { useAuth } from '@/hooks/useAuth';
import { postApi } from '@/api/postApi';
import { followApi } from '@/api/followApi';
import { interactionApi } from '@/api/interactionApi';
import { InteractionType } from '@/types/interaction';
import { seenPostsService } from '@/utils/seenPosts';
import { CommentSection } from './CommentSection';

interface PostCardProps {
  post: PostDto;
  onPostDeleted?: (postId: string) => void;
}

const formatRelativeTime = (utcDateString: string): string => {
  const date = new Date(utcDateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return 'Vừa xong';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)} phút trước`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)} giờ trước`;
  if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)} ngày trước`;

  return date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
};

export const PostCard: React.FC<PostCardProps> = ({ post, onPostDeleted }) => {
  const { user, isAuthenticated } = useAuth();
  const [isDeleting, setIsDeleting] = useState(false);
  
  // Like state
  const [liked, setLiked] = useState(post.isLikedByCurrentUser || false);
  const [likeCount, setLikeCount] = useState(post.likeCount || 0);
  const [likeLoading, setLikeLoading] = useState(false);

  // Comment state
  const [showComments, setShowComments] = useState(false);
  const [commentCount, setCommentCount] = useState(post.commentCount || 0);

  // Follow state
  const [isFollowing, setIsFollowing] = useState(false);
  const [followLoading, setFollowLoading] = useState(false);

  // View tracking ref
  const cardRef = useRef<HTMLElement>(null);
  const hasTrackedView = useRef(false);

  const isAuthor = user?.id === post.author.id;

  // Tự động ghi nhận hành vi xem bài (View tracking) bằng IntersectionObserver
  useEffect(() => {
    if (hasTrackedView.current || !cardRef.current) return;

    let timer: ReturnType<typeof setTimeout> | null = null;

    const observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        if (entry.isIntersecting) {
          // Người dùng dừng xem bài viết trên 600ms -> ghi nhận view
          timer = setTimeout(() => {
            if (!hasTrackedView.current) {
              hasTrackedView.current = true;
              // 1. Lưu vào danh sách đã xem cục bộ (hoạt động cho cả khách vãng lai và user)
              seenPostsService.markAsSeen(post.id);

              // 2. Ghi nhận tương tác lên Backend nếu đã đăng nhập
              if (isAuthenticated) {
                interactionApi.track({
                  postId: post.id,
                  interactionType: InteractionType.View,
                  value: 1.0,
                }).catch(() => {});
              }
            }
          }, 600);
        } else {
          if (timer) {
            clearTimeout(timer);
            timer = null;
          }
        }
      },
      { threshold: 0.5 }
    );

    observer.observe(cardRef.current);

    return () => {
      observer.disconnect();
      if (timer) clearTimeout(timer);
    };
  }, [isAuthenticated, post.id]);

  // Handle Like/Unlike
  const handleToggleLike = async () => {
    if (!isAuthenticated) {
      alert('Vui lòng đăng nhập để thích bài viết.');
      return;
    }

    if (likeLoading) return;

    // Optimistic UI update
    const previousLiked = liked;
    const previousCount = likeCount;

    setLiked(!previousLiked);
    setLikeCount(previousLiked ? Math.max(0, previousCount - 1) : previousCount + 1);
    setLikeLoading(true);

    try {
      const res = await postApi.toggleLike(post.id);
      setLiked(res.isLiked);
      setLikeCount(res.likeCount);
    } catch {
      // Revert if error
      setLiked(previousLiked);
      setLikeCount(previousCount);
    } finally {
      setLikeLoading(false);
    }
  };

  // Handle Follow/Unfollow Author
  const handleToggleFollow = async () => {
    if (!isAuthenticated) {
      alert('Vui lòng đăng nhập để theo dõi tác giả.');
      return;
    }

    if (followLoading || isAuthor) return;

    setFollowLoading(true);
    try {
      const res = await followApi.toggleFollow(post.author.id);
      setIsFollowing(res.isFollowing);
    } catch {
      alert('Không thể cập nhật theo dõi.');
    } finally {
      setFollowLoading(false);
    }
  };

  // Handle Delete Post
  const handleDelete = async () => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa bài viết này không?')) {
      return;
    }

    setIsDeleting(true);
    try {
      await postApi.deletePost(post.id);
      onPostDeleted?.(post.id);
    } catch {
      alert('Không thể xóa bài viết. Vui lòng thử lại.');
      setIsDeleting(false);
    }
  };

  return (
    <article ref={cardRef} className="card post-card" id={`post-${post.id}`}>
      {/* Post Header */}
      <header className="post-header">
        <div className="post-author-info">
          <div className="avatar author-avatar">
            {post.author.avatarUrl ? (
              <img src={post.author.avatarUrl} alt={post.author.displayName} />
            ) : (
              <span className="avatar-fallback">{post.author.displayName.charAt(0)}</span>
            )}
          </div>
          <div>
            <div className="author-name-row">
              <span className="author-name">{post.author.displayName}</span>
              {isAuthenticated && !isAuthor && (
                <button
                  type="button"
                  className={`btn-follow-inline ${isFollowing ? 'following' : ''}`}
                  onClick={handleToggleFollow}
                  disabled={followLoading}
                >
                  {isFollowing ? '✓ Đang theo dõi' : '+ Theo dõi'}
                </button>
              )}
            </div>
            <time className="post-time" dateTime={post.createdAtUtc}>
              {formatRelativeTime(post.createdAtUtc)}
            </time>
          </div>
        </div>

        {isAuthor && (
          <div className="post-menu">
            <button
              type="button"
              className="btn-icon btn-delete-post"
              onClick={handleDelete}
              disabled={isDeleting}
              title="Xóa bài viết"
            >
              🗑️
            </button>
          </div>
        )}
      </header>

      {/* Post Content */}
      {post.content && (
        <div className="post-content">
          {post.content.split('\n').map((paragraph, index) => (
            <p key={index}>{paragraph || <br />}</p>
          ))}
        </div>
      )}

      {/* Post Media Grid */}
      {post.mediaUrls && post.mediaUrls.length > 0 && (
        <div className={`post-media-gallery count-${Math.min(post.mediaUrls.length, 4)}`}>
          {post.mediaUrls.map((url, index) => (
            <div key={index} className="gallery-item">
              <img src={url} alt={`post-media-${index}`} loading="lazy" />
            </div>
          ))}
        </div>
      )}


      {/* Interaction Action Bar */}
      <footer className="post-footer">
        <button
          type="button"
          className={`post-action-btn ${liked ? 'active liked' : ''}`}
          onClick={handleToggleLike}
          disabled={likeLoading}
          aria-label="Thích bài viết"
        >
          <span className="action-icon">{liked ? '❤️' : '🤍'}</span>
          <span className="action-label">
            {liked ? 'Đã thích' : 'Thích'} {likeCount > 0 && `(${likeCount})`}
          </span>
        </button>

        <button
          type="button"
          className={`post-action-btn ${showComments ? 'active' : ''}`}
          onClick={() => setShowComments((prev) => !prev)}
          aria-label="Bình luận"
        >
          <span className="action-icon">💬</span>
          <span className="action-label">
            Bình luận {commentCount > 0 && `(${commentCount})`}
          </span>
        </button>

        <button
          type="button"
          className="post-action-btn"
          onClick={() => {
            navigator.clipboard?.writeText(window.location.href);
            alert('Đã sao chép link bài viết vào bộ nhớ tạm!');
          }}
          aria-label="Chia sẻ"
        >
          <span className="action-icon">🔗</span>
          <span className="action-label">Chia sẻ</span>
        </button>
      </footer>

      {/* Expandable Comment Section */}
      {showComments && (
        <CommentSection
          postId={post.id}
          onCommentCountChange={(count) => setCommentCount(count)}
        />
      )}
    </article>
  );
};
