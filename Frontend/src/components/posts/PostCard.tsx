import React, { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import type { PostDto } from '@/types';
import { useAuth } from '@/hooks/useAuth';
import { useNotificationStore } from '@/store';
import { postApi } from '@/api/postApi';
import { followApi } from '@/api/followApi';
import { interactionApi } from '@/api/interactionApi';
import { InteractionType } from '@/types/interaction';
import { seenPostsService } from '@/utils/seenPosts';
import { formatRelativeTime } from '@/utils/date';
import {
  Avatar,
  HeartIcon,
  CommentIcon,
  ShareIcon,
  TrashIcon,
  MoreVerticalIcon,
  CheckIcon,
  CloseIcon,
  ConfirmModal,
} from '@/components/common';
import { CommentSection } from './CommentSection';

interface PostCardProps {
  post: PostDto;
  onPostDeleted?: (postId: string) => void;
  initialShowComments?: boolean;
}

export const PostCard: React.FC<PostCardProps> = ({
  post,
  onPostDeleted,
  initialShowComments = false,
}) => {
  const { user, isAuthenticated } = useAuth();
  const { showToast } = useNotificationStore();

  const [isDeleting, setIsDeleting] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [showMenu, setShowMenu] = useState(false);
  const [previewImage, setPreviewImage] = useState<string | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  // Like state
  const [liked, setLiked] = useState(post.isLikedByCurrentUser || false);
  const [likeCount, setLikeCount] = useState(post.likeCount || 0);
  const [likeLoading, setLikeLoading] = useState(false);

  // Comment state
  const [showComments, setShowComments] = useState(initialShowComments);
  const [commentCount, setCommentCount] = useState(post.commentCount || 0);

  // Đồng bộ số lượng like, trạng thái like và comment khi có cập nhật từ cha hoặc realtime
  useEffect(() => {
    setLiked(post.isLikedByCurrentUser || false);
  }, [post.isLikedByCurrentUser]);

  useEffect(() => {
    setLikeCount(post.likeCount || 0);
  }, [post.likeCount]);

  useEffect(() => {
    setCommentCount(post.commentCount || 0);
  }, [post.commentCount]);

  // Follow state
  const [isFollowing, setIsFollowing] = useState(false);
  const [followLoading, setFollowLoading] = useState(false);

  // View tracking ref
  const cardRef = useRef<HTMLElement>(null);
  const hasTrackedView = useRef(false);

  const isAuthor = user?.id === post.author.id;

  // Đóng menu 3 chấm khi click ngoài
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
        setShowMenu(false);
      }
    };
    if (showMenu) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [showMenu]);

  // Tự động ghi nhận hành vi xem bài (View tracking) bằng IntersectionObserver
  useEffect(() => {
    if (hasTrackedView.current || !cardRef.current) return;

    let timer: ReturnType<typeof setTimeout> | null = null;

    const observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        if (entry.isIntersecting) {
          timer = setTimeout(() => {
            if (!hasTrackedView.current) {
              hasTrackedView.current = true;
              // Đánh dấu đã xem trên frontend
              seenPostsService.markAsSeen(post.id);

              if (isAuthenticated) {
                interactionApi
                  .track({
                    postId: post.id,
                    interactionType: InteractionType.View,
                    value: 1.0,
                  })
                  .catch(() => {});
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
      showToast('Vui lòng đăng nhập để thích bài viết.', 'warning');
      return;
    }

    if (likeLoading) return;

    const previousLiked = liked;
    const previousCount = likeCount;

    setLiked(!previousLiked);
    setLikeCount(previousLiked ? Math.max(0, previousCount - 1) : previousCount + 1);
    setLikeLoading(true);

    try {
      const res = await postApi.toggleLike(post.id);
      setLiked(res.isLiked);
      setLikeCount(res.likeCount);
      post.isLikedByCurrentUser = res.isLiked;
      post.likeCount = res.likeCount;
    } catch {
      setLiked(previousLiked);
      setLikeCount(previousCount);
    } finally {
      setLikeLoading(false);
    }
  };

  // Handle Follow/Unfollow Author
  const handleToggleFollow = async () => {
    if (!isAuthenticated) {
      showToast('Vui lòng đăng nhập để theo dõi tác giả.', 'warning');
      return;
    }

    if (followLoading || isAuthor) return;

    setFollowLoading(true);
    try {
      const res = await followApi.toggleFollow(post.author.id);
      setIsFollowing(res.isFollowing);
      showToast(
        res.isFollowing
          ? `Đã theo dõi ${post.author.displayName}`
          : `Đã hủy theo dõi ${post.author.displayName}`,
        'info'
      );
    } catch {
      showToast('Không thể cập nhật theo dõi.', 'error');
    } finally {
      setFollowLoading(false);
    }
  };

  // Handle Delete Post
  const handleConfirmDelete = async () => {
    setIsDeleting(true);
    try {
      await postApi.deletePost(post.id);
      setShowDeleteConfirm(false);
      showToast('Đã xóa bài viết thành công.', 'info');
      onPostDeleted?.(post.id);
    } catch {
      showToast('Không thể xóa bài viết. Vui lòng thử lại.', 'error');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleCopyLink = () => {
    setShowMenu(false);
    if (navigator.clipboard) {
      navigator.clipboard.writeText(window.location.href);
      showToast('Đã sao chép liên kết vào bộ nhớ tạm!', 'info');
    }
  };

  return (
    <>
      <article ref={cardRef} className="post-entry" id={`post-${post.id}`}>
        {/* Post Header */}
        <header className="post-entry-header">
          <div className="post-entry-author">
            <Avatar
              src={post.author.avatarUrl}
              name={post.author.displayName}
              size="md"
            />
            <div className="post-author-meta">
              <div className="author-headline">
                <span className="author-display-name">{post.author.displayName}</span>
                {isAuthenticated && !isAuthor && (
                  <button
                    type="button"
                    className={`btn-follow-chip ${isFollowing ? 'following' : ''}`}
                    onClick={handleToggleFollow}
                    disabled={followLoading}
                  >
                    {isFollowing ? (
                      <>
                        <CheckIcon size={12} />
                        <span>Đang theo dõi</span>
                      </>
                    ) : (
                      <span>+ Theo dõi</span>
                    )}
                  </button>
                )}
              </div>
              <Link to={`/posts/${post.id}`} className="post-entry-time-link" title="Xem chi tiết bài viết">
                <time className="post-entry-time" dateTime={post.createdAtUtc}>
                  {formatRelativeTime(post.createdAtUtc)}
                </time>
              </Link>
            </div>
          </div>

          {/* Context Menu 3-dots */}
          <div className="post-entry-menu" ref={menuRef}>
            <button
              type="button"
              className="btn-icon-ghost"
              onClick={() => setShowMenu((prev) => !prev)}
              aria-label="Tùy chọn bài viết"
              title="Tùy chọn"
            >
              <MoreVerticalIcon size={18} />
            </button>

            {showMenu && (
              <div className="dropdown-menu-popover">
                <Link
                  to={`/posts/${post.id}`}
                  className="dropdown-menu-item"
                  onClick={() => setShowMenu(false)}
                >
                  <ShareIcon size={16} />
                  <span>Xem chi tiết</span>
                </Link>
                <button type="button" className="dropdown-menu-item" onClick={handleCopyLink}>
                  <ShareIcon size={16} />
                  <span>Sao chép liên kết</span>
                </button>
                {isAuthor && (
                  <button
                    type="button"
                    className="dropdown-menu-item item-danger"
                    onClick={() => {
                      setShowMenu(false);
                      setShowDeleteConfirm(true);
                    }}
                  >
                    <TrashIcon size={16} />
                    <span>Xóa bài viết</span>
                  </button>
                )}
              </div>
            )}
          </div>
        </header>

        {/* Post Content */}
        {post.content && (
          <div className="post-entry-body">
            {post.content.split('\n').map((paragraph, index) => (
              <p key={index}>{paragraph || <br />}</p>
            ))}
          </div>
        )}

        {/* Post Media Gallery */}
        {post.mediaUrls && post.mediaUrls.length > 0 && (
          <div className={`post-gallery gallery-count-${Math.min(post.mediaUrls.length, 5)}`}>
            {post.mediaUrls.map((url, index) => (
              <div
                key={index}
                className="gallery-cell"
                onClick={() => setPreviewImage(url)}
                title="Bấm để xem ảnh kích thước đầy đủ"
              >
                <img src={url} alt={`post-media-${index}`} loading="lazy" />
              </div>
            ))}
          </div>
        )}

        {/* Interaction Toolbar */}
        <footer className="post-entry-toolbar">
          <button
            type="button"
            className={`post-toolbar-btn ${liked ? 'is-liked' : ''}`}
            onClick={handleToggleLike}
            disabled={likeLoading}
            aria-label={liked ? 'Bỏ thích' : 'Thích bài viết'}
          >
            <HeartIcon size={19} filled={liked} className="heart-icon-anim" />
            <span className="toolbar-counter">{likeCount > 0 ? likeCount : 'Thích'}</span>
          </button>

          <button
            type="button"
            className={`post-toolbar-btn ${showComments ? 'is-active' : ''}`}
            onClick={() => setShowComments((prev) => !prev)}
            aria-label="Xem bình luận"
          >
            <CommentIcon size={19} />
            <span className="toolbar-counter">
              {commentCount > 0 ? commentCount : 'Bình luận'}
            </span>
          </button>

          <button
            type="button"
            className="post-toolbar-btn"
            onClick={handleCopyLink}
            aria-label="Chia sẻ bài viết"
          >
            <ShareIcon size={19} />
            <span className="toolbar-counter">Chia sẻ</span>
          </button>
        </footer>

        {/* Expandable Comment Section */}
        {showComments && (
          <CommentSection
            postId={post.id}
            onCommentCountChange={(count) => {
              setCommentCount(count);
              post.commentCount = count;
            }}
          />
        )}
      </article>

      {/* Full-Screen Image Lightbox Modal */}
      {previewImage && (
        <div
          className="image-lightbox-overlay"
          onClick={() => setPreviewImage(null)}
          role="dialog"
          aria-modal="true"
        >
          <button
            type="button"
            className="lightbox-close-btn"
            onClick={() => setPreviewImage(null)}
            aria-label="Đóng ảnh"
          >
            <CloseIcon size={20} />
          </button>
          <div className="image-lightbox-container" onClick={(e) => e.stopPropagation()}>
            <img src={previewImage} alt="Full resolution view" />
          </div>
        </div>
      )}

      {/* In-app Delete Confirmation Modal */}
      <ConfirmModal
        isOpen={showDeleteConfirm}
        title="Xóa bài viết"
        message="Bạn có chắc chắn muốn xóa bài viết này không? Hành động này không thể hoàn tác."
        confirmText="Xóa vĩnh viễn"
        cancelText="Giữ lại"
        confirmVariant="danger"
        isLoading={isDeleting}
        onConfirm={handleConfirmDelete}
        onCancel={() => setShowDeleteConfirm(false)}
      />
    </>
  );
};
